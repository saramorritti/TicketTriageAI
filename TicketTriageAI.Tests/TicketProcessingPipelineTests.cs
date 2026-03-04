using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TicketTriageAI.Core.Configuration;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Factories;
using TicketTriageAI.Core.Services.Notifications;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Services.Text;

namespace TicketTriageAI.Tests
{
    public sealed class TicketProcessingPipelineTests
    {
        [Fact]
        public async Task ExecuteAsync_CallsClassifier_Once_And_UpsertsDocument()
        {
            // Arrange
            var classifier = new Mock<ITicketClassifier>(MockBehavior.Strict);
            var repository = new Mock<ITicketRepository>(MockBehavior.Strict);
            var docFactory = new Mock<ITicketDocumentFactory>();
            var statusRepo = new Mock<ITicketStatusRepository>(MockBehavior.Strict);

            // NEW: notifier + notification options
            var notifier = new Mock<ITicketNotificationService>(MockBehavior.Strict);
            var notificationOptions = Options.Create(new NotificationOptions
            {
                DashboardBaseUrl = "https://dashboard.test"
            });

            // NEW: normalizer
            var normalizer = new Mock<ITextNormalizer>(MockBehavior.Strict);
            normalizer
                .Setup(n => n.Normalize("Non riesco ad accedere"))
                .Returns("Non riesco ad accedere"); // per questo test non cambia nulla, ma serve la dependency

            // options (già c'erano, ma ora restano "processing options")
            var options = Options.Create(new TicketProcessingOptions
            {
                ConfidenceThreshold = 0.7,
                ForceReviewOnP1 = true
            });

            statusRepo
                .Setup(s => s.PatchProcessingAsync("msg-001", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            statusRepo
                .Setup(s => s.PatchProcessedAsync("msg-001", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // In questo test vogliamo restare nel ramo "Processed",
            // quindi NON deve notificare né patchare NeedsReview
            notifier
                .Setup(n => n.NotifyNeedsReviewAsync(It.IsAny<TicketDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("NotifyNeedsReviewAsync should not be called in this test"));

            statusRepo
                .Setup(s => s.PatchNeedsReviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("PatchNeedsReviewAsync should not be called in this test"));

            docFactory
                .Setup(f => f.Create(
                    It.IsAny<TicketIngested>(),
                    It.IsAny<TicketTriageResult>(),
                    It.IsAny<ClassifierMetadata>()))
                .Returns((TicketIngested t, TicketTriageResult r, ClassifierMetadata m) =>
                    new TicketDocument
                    {
                        Id = t.MessageId,
                        MessageId = t.MessageId,
                        CorrelationId = t.CorrelationId,
                        From = t.From,
                        Subject = t.Subject,
                        Body = t.Body,
                        ReceivedAt = t.ReceivedAt,
                        Source = t.Source,

                        Category = r.Category,
                        Severity = r.Severity,
                        Confidence = r.Confidence,
                        NeedsHumanReview = r.NeedsHumanReview,

                        Status = TicketStatus.Processed
                    });

            var expected = new TicketTriageResult
            {
                Category = "support",
                Severity = "P1",
                Confidence = 0.90,
                NeedsHumanReview = false
            };

            classifier
                .Setup(c => c.ClassifyAsync(It.IsAny<TicketIngested>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            repository
                .Setup(r => r.UpsertAsync(It.IsAny<TicketDocument>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var logger = NullLogger<TicketProcessingPipeline>.Instance;

            // NEW: constructor args updated
            var pipeline = new TicketProcessingPipeline(
                classifier.Object,
                repository.Object,
                docFactory.Object,
                statusRepo.Object,
                notifier.Object,
                notificationOptions,
                options,
                logger,
                normalizer.Object);

            var ticket = new TicketIngested
            {
                MessageId = "msg-001",
                CorrelationId = "corr-123",
                From = "test@example.com",
                Subject = "URGENTE - problema accesso",
                Body = "Non riesco ad accedere",
                ReceivedAt = new DateTime(2026, 1, 28),
                Source = "email"
            };

            // Act
            await pipeline.ExecuteAsync(ticket);

            // Assert

            // NEW: normalizer called
            normalizer.Verify(n => n.Normalize("Non riesco ad accedere"), Times.Once);

            // UPDATED: classifier receives the normalized ticket (Body = normalized)
            classifier.Verify(c => c.ClassifyAsync(
                It.Is<TicketIngested>(t =>
                    t.MessageId == "msg-001" &&
                    t.CorrelationId == "corr-123" &&
                    t.Subject.Contains("URGENTE") &&
                    t.Body == "Non riesco ad accedere"),
                It.IsAny<CancellationToken>()),
                Times.Once);

            // UPDATED: doc now gets CleanBody set AFTER Create + status logic, so verify that too
            repository.Verify(r => r.UpsertAsync(
                It.Is<TicketDocument>(d =>
                    d.Id == "msg-001" &&
                    d.MessageId == "msg-001" &&
                    d.Category == "support" &&
                    d.Severity == "P1" &&
                    d.NeedsHumanReview == false &&
                    d.CleanBody == "Non riesco ad accedere" &&
                    d.Status == TicketStatus.Processed),
                It.IsAny<CancellationToken>()),
                Times.Once);

            statusRepo.Verify(s => s.PatchProcessingAsync("msg-001", It.IsAny<CancellationToken>()), Times.Once);
            statusRepo.Verify(s => s.PatchProcessedAsync("msg-001", It.IsAny<CancellationToken>()), Times.Once);

            // Optional: assert non chiamate (più pulito del Throws sopra)
            notifier.Verify(n => n.NotifyNeedsReviewAsync(It.IsAny<TicketDocument>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            statusRepo.Verify(s => s.PatchNeedsReviewAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}