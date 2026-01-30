using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Processing;
using TicketTriageAI.Core.Services.Factories;


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
            var pipeline = new TicketProcessingPipeline(
                classifier.Object,
                repository.Object,
                docFactory.Object,
                logger);

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
            classifier.Verify(c => c.ClassifyAsync(
                It.Is<TicketIngested>(t =>
                    t.MessageId == "msg-001" &&
                    t.CorrelationId == "corr-123" &&
                    t.Subject.Contains("URGENTE")),
                It.IsAny<CancellationToken>()),
                Times.Once);

            repository.Verify(r => r.UpsertAsync(
                It.Is<TicketDocument>(d =>
                    d.Id == "msg-001" &&
                    d.MessageId == "msg-001" &&
                    d.Category == "support" &&
                    d.Severity == "P1" &&
                    d.NeedsHumanReview == false),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
