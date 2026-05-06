using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Products.Add(new Product
        {
            Id = "supporter-pack",
            Slug = "supporter-pack",
            Name = "Supporter Pack",
            Description = "Pacote base para apoiar a Diguifi Studios.",
            Category = ProductCategory.Bundle,
            Price = 10.0m,
            Currency = "BRL",
            IsActive = true
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
