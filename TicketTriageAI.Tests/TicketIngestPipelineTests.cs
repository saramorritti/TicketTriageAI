using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TicketTriageAI.Core.Models;
using TicketTriageAI.Core.Services;
using TicketTriageAI.Core.Services.Ingest;
using TicketTriageAI.Core.Services.Messaging;
using TicketTriageAI.Core.Services.Factories;


namespace TicketTriageAI.Tests
{
    public sealed class TicketIngestPipelineTests
    {
        [Fact]
        public async Task ExecuteAsync_Publishes_TicketIngested_With_CorrelationId_And_MessageId()
        {
            // Arrange
            var publisher = new Mock<ITicketQueuePublisher>(MockBehavior.Strict);
            var factory = new Mock<ITicketIngestedFactory>();

            factory
            .Setup(f => f.Create(
                It.IsAny<TicketIngestedRequest>(),
                "corr-123",
                null))
            .Returns((TicketIngestedRequest r, string corr, string? raw) =>
                new TicketIngested
                {
                    MessageId = r.MessageId,
                    CorrelationId = corr,
                    From = r.From,
                    Subject = r.Subject,
                    Body = r.Body,
                    ReceivedAt = r.ReceivedAt,
                    Source = r.Source,
                    RawMessage = raw
                });




            TicketIngested? published = null;

            publisher
                .Setup(p => p.PublishAsync(It.IsAny<TicketIngested>(), default))
                .Callback<TicketIngested, System.Threading.CancellationToken>((t, _) => published = t)
                .Returns(Task.CompletedTask);

            var pipeline = new TicketIngestPipeline(
                publisher.Object,
                factory.Object);

            var req = new TicketIngestedRequest
            {
                MessageId = "msg-001",
                From = "test@example.com",
                Subject = "Login issue",
                Body = "Non riesco ad accedere",
                ReceivedAt = new DateTime(2026, 1, 28),
                Source = "email"
            };

            var correlationId = "corr-123";

            // Act
            await pipeline.ExecuteAsync(req, correlationId);

            // Assert
            publisher.Verify(p => p.PublishAsync(It.IsAny<TicketIngested>(), default), Times.Once);

            Assert.NotNull(published);
            Assert.Equal("msg-001", published!.MessageId);
            Assert.Equal("corr-123", published.CorrelationId);
            Assert.Equal("test@example.com", published.From);
            Assert.Equal("Login issue", published.Subject);
            Assert.Equal("Non riesco ad accedere", published.Body);
            Assert.Equal("email", published.Source);
            Assert.Equal(new DateTime(2026, 1, 28), published.ReceivedAt);
        }
    }
}
