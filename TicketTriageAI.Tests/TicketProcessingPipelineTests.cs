using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services.Processing;

namespace TicketTriageAI.Tests
{

    public sealed class TicketProcessingPipelineTests
    {
        [Fact]
        public async Task ExecuteAsync_CallsClassifier_Once()
        {
            // Arrange
            var classifier = new Mock<ITicketClassifier>(MockBehavior.Strict);

            var expected = new TicketTriageResult
            {
                Category = "support",
                Severity = "P1",
                Confidence = 0.90,
                NeedsHumanReview = false
            };

            classifier
                .Setup(c => c.ClassifyAsync(It.IsAny<TicketIngested>(), default))
                .ReturnsAsync(expected);

            var logger = NullLogger<TicketProcessingPipeline>.Instance;
            var pipeline = new TicketProcessingPipeline(classifier.Object, logger);

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
            classifier.Verify(c => c.ClassifyAsync(It.Is<TicketIngested>(t =>
                t.MessageId == "msg-001" &&
                t.CorrelationId == "corr-123" &&
                t.Subject.Contains("URGENTE")
            ), default), Times.Once);
        }
    }
}
