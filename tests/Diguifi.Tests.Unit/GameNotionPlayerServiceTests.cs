using Diguifi.Application.DTOs.GameNotionPlayers;
using Diguifi.Domain.Entities;
using Diguifi.Infrastructure.Services;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class GameNotionPlayerServiceTests
{
    private static readonly DateTime FixedPing = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_NoPlayers_ReturnsEmptyCollection()
    {
        await using var db = DbContextFactory.Create();

        var result = await new GameNotionPlayerService(db).GetAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_PlayersExist_ReturnsAll()
    {
        await using var db = DbContextFactory.Create();
        db.GameNotionPlayers.AddRange(
            new GameNotionPlayer { PlayerId = "player-1", LastPing = FixedPing },
            new GameNotionPlayer { PlayerId = "player-2", LastPing = FixedPing }
        );
        await db.SaveChangesAsync();

        var result = await new GameNotionPlayerService(db).GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.PlayerId == "player-1");
        result.Should().Contain(p => p.PlayerId == "player-2");
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_PlayerExists_ReturnsPlayer()
    {
        await using var db = DbContextFactory.Create();
        db.GameNotionPlayers.Add(new GameNotionPlayer { PlayerId = "player-a", LastPing = FixedPing });
        await db.SaveChangesAsync();

        var result = await new GameNotionPlayerService(db).GetByIdAsync("player-a", CancellationToken.None);

        result.Should().NotBeNull();
        result!.PlayerId.Should().Be("player-a");
        result.LastPing.Should().Be(FixedPing);
    }

    [Fact]
    public async Task GetByIdAsync_PlayerNotFound_ReturnsNull()
    {
        await using var db = DbContextFactory.Create();

        var result = await new GameNotionPlayerService(db).GetByIdAsync("ghost", CancellationToken.None);

        result.Should().BeNull();
    }

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_NewPlayer_CreatesAndReturnsRecord()
    {
        await using var db = DbContextFactory.Create();

        var result = await new GameNotionPlayerService(db)
            .CreateAsync(new CreateGameNotionPlayerRequest { PlayerId = "p-new", LastPing = FixedPing }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PlayerId.Should().Be("p-new");
        result.Value.LastPing.Should().Be(FixedPing);
        db.GameNotionPlayers.Should().ContainSingle(p => p.PlayerId == "p-new");
    }

    [Fact]
    public async Task CreateAsync_DuplicatePlayerId_ReturnsPlayerAlreadyExists()
    {
        await using var db = DbContextFactory.Create();
        db.GameNotionPlayers.Add(new GameNotionPlayer { PlayerId = "p-dup", LastPing = FixedPing });
        await db.SaveChangesAsync();

        var result = await new GameNotionPlayerService(db)
            .CreateAsync(new CreateGameNotionPlayerRequest { PlayerId = "p-dup", LastPing = FixedPing }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("player_already_exists");
    }

    // ── UpdateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PlayerExists_UpdatesLastPing()
    {
        await using var db = DbContextFactory.Create();
        db.GameNotionPlayers.Add(new GameNotionPlayer { PlayerId = "p-upd", LastPing = FixedPing });
        await db.SaveChangesAsync();

        var newPing = new DateTime(2026, 6, 15, 8, 30, 0, DateTimeKind.Utc);
        var result = await new GameNotionPlayerService(db)
            .UpdateAsync("p-upd", new UpdateGameNotionPlayerRequest { LastPing = newPing }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.LastPing.Should().Be(newPing);
        db.GameNotionPlayers.Single(p => p.PlayerId == "p-upd").LastPing.Should().Be(newPing);
    }

    [Fact]
    public async Task UpdateAsync_PlayerNotFound_ReturnsPlayerNotFound()
    {
        await using var db = DbContextFactory.Create();

        var result = await new GameNotionPlayerService(db)
            .UpdateAsync("ghost", new UpdateGameNotionPlayerRequest { LastPing = FixedPing }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("player_not_found");
    }

    // ── DeleteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_PlayerExists_RemovesRecord()
    {
        await using var db = DbContextFactory.Create();
        db.GameNotionPlayers.Add(new GameNotionPlayer { PlayerId = "p-del", LastPing = FixedPing });
        await db.SaveChangesAsync();

        var result = await new GameNotionPlayerService(db).DeleteAsync("p-del", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        db.GameNotionPlayers.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_PlayerNotFound_ReturnsPlayerNotFound()
    {
        await using var db = DbContextFactory.Create();

        var result = await new GameNotionPlayerService(db).DeleteAsync("ghost", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("player_not_found");
    }
}
