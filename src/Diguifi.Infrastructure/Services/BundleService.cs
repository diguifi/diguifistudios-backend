using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Bundles;
using Diguifi.Application.DTOs.Orders;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class BundleService(AppDbContext dbContext) : IBundleService
{
    public async Task<Result<bool>> UpsertAsync(string productId, UpsertBundleRequest request, CancellationToken ct)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);
        if (product is null)
            return Result<bool>.Failure("product_not_found", "Produto nao encontrado.");

        if (product.Category != ProductCategory.Bundle)
            return Result<bool>.Failure("not_bundle", "Produto nao e do tipo Bundle.");

        var bundle = await dbContext.Bundles.FirstOrDefaultAsync(b => b.ProductId == productId, ct);
        if (bundle is null)
        {
            dbContext.Bundles.Add(new Bundle { ProductId = productId, DriveUrl = request.DriveUrl, FileName = request.FileName });
        }
        else
        {
            bundle.DriveUrl = request.DriveUrl;
            bundle.FileName = request.FileName;
            bundle.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<BundleDownloadResponse>> GetAsync(string productId, CancellationToken ct)
    {
        var bundle = await dbContext.Bundles.FirstOrDefaultAsync(b => b.ProductId == productId, ct);
        if (bundle is null)
            return Result<BundleDownloadResponse>.Failure("bundle_not_configured", "Bundle nao configurado para este produto.");

        return Result<BundleDownloadResponse>.Success(new BundleDownloadResponse
        {
            DownloadUrl = bundle.DriveUrl,
            FileName = bundle.FileName
        });
    }
}
