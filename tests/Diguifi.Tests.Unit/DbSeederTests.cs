using Diguifi.Infrastructure.Persistence;
using Diguifi.Tests.Unit.Helpers;
using FluentAssertions;
using Xunit;

namespace Diguifi.Tests.Unit;

public sealed class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesDefaultProduct()
    {
        await using var db = DbContextFactory.Create();

        await DbSeeder.SeedAsync(db, CancellationToken.None);

        db.Products.Should().ContainSingle();
    }

    [Fact]
    public async Task SeedAsync_DatabaseAlreadyHasProducts_DoesNotSeedAgain()
    {
        await using var db = DbContextFactory.Create();
        await DbSeeder.SeedAsync(db, CancellationToken.None);
        var countAfterFirst = db.Products.Count();

        await DbSeeder.SeedAsync(db, CancellationToken.None);

        db.Products.Should().HaveCount(countAfterFirst);
    }

    [Fact]
    public async Task SeedAsync_CreatesProductWithExpectedFields()
    {
        await using var db = DbContextFactory.Create();

        await DbSeeder.SeedAsync(db, CancellationToken.None);

        var product = db.Products.Single();
        product.Id.Should().Be("supporter-pack");
        product.IsActive.Should().BeTrue();
        product.Currency.Should().Be("BRL");
        product.Price.Should().BePositive();
    }
}
