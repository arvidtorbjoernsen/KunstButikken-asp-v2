using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using KunstButikken.AuctionService.Domain.Models;
using KunstButikken.AuctionService.IntegrationEvents.Handlers;
using KunstButikken.AuctionService.Tests.TestDoubles;
using KunstButikken.Common.Logging;
using KunstButikken.IntegrationEvents.Contracts.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace KunstButikken.AuctionService.Tests.IntegrationEvents;

public class ArtIntegrationEventHandlerTests
{
    [Fact]
    public async Task ArtCreated_AddsArtAndSaves()
    {
        var repository = new FakeAuctionRepository();
        var handler = new ArtCreatedIntegrationEventHandler(repository);
        var ev = new ArtCreatedIntegrationEvent("En", "Nb", "Artist", Guid.NewGuid(), "Seller", 123m, new Uri("https://example.com/art.jpg"));

        await handler.Handle(ev);

        repository.Arts.Should().ContainSingle(a => a.Id == ev.Id && a.Title == ev.TitleEn);
        repository.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ArtDeleted_RemovesArtWhenPresent()
    {
        var repository = new FakeAuctionRepository();
        var art = new Art { Id = Guid.NewGuid(), Title = "Art", Artist = "Artist", Price = 1m, SellerId = "seller" };
        repository.SeedArt(art);
        var handler = new ArtDeletedIntegrationEventHandler(repository);

        await handler.Handle(new ArtDeletedIntegrationEvent(art.Id));

        repository.Arts.Should().BeEmpty();
        repository.SaveCallCount.Should().Be(1);
    }

    [Fact]
    public async Task ArtUpdated_WhenArtMissing_LogsWarning()
    {
        var repository = new FakeAuctionRepository();
        var logger = new TestLogger<ArtUpdatedIntegrationEventHandler>();
        var handler = new ArtUpdatedIntegrationEventHandler(repository, logger);
        var ev = new ArtUpdatedIntegrationEvent(Guid.NewGuid(), "Title", "Titel", "Artist", Guid.NewGuid(), "Seller", 200m, new Uri("https://example.com"), "Published", true);

        await handler.Handle(ev);

        logger.Logs.Should().ContainSingle(log => log.EventId.Id == 3029 && log.Level == LogLevel.Warning);
        repository.SaveCallCount.Should().Be(0);
    }

    [Fact]
    public async Task ArtUpdated_UpdatesArt_CreatesAuctionWhenPublished()
    {
        var repository = new FakeAuctionRepository();
        var handler = new ArtUpdatedIntegrationEventHandler(repository, NullLogger<ArtUpdatedIntegrationEventHandler>.Instance);
        var art = new Art { Id = Guid.NewGuid(), Title = "Old", Artist = "Artist", Price = 100m, SellerId = "seller" };
        repository.SeedArt(art);
        var ev = new ArtUpdatedIntegrationEvent(art.Id, "TitleEn", "TitleNb", "Artist", Guid.NewGuid(), "Seller", 200m, new Uri("https://example.com/2"), "Published", true);

        await handler.Handle(ev);

        var saved = await repository.GetArtByIdAsync(art.Id);
        saved.Should().NotBeNull();
        saved!.Title.Should().Be(ev.TitleEn);
        saved.Price.Should().Be(ev.Price);
        repository.Auctions.Should().ContainSingle(a => a.ArtId == art.Id);
        repository.SaveCallCount.Should().Be(2);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Logs { get; } = new();

        IDisposable ILogger.BeginScope<TState>(TState state) => new DisposableScope();
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Logs.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
        }

        public sealed class LogEntry
        {
            public LogEntry(LogLevel level, EventId eventId, string message)
            {
                Level = level;
                EventId = eventId;
                Message = message;
            }

            public LogLevel Level { get; }
            public EventId EventId { get; }
            public string Message { get; }
        }

        private sealed class DisposableScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
