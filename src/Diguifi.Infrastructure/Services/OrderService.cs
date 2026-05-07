using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Orders;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class OrderService(AppDbContext dbContext, IStripeBillingPortalGateway portalGateway) : IOrderService
{
    public async Task<Result<CancelSubscriptionResponse>> CancelSubscriptionAsync(
        Guid orderId, Guid userId, string returnUrl, CancellationToken ct)
    {
        var order = await dbContext.Orders
            .Include(o => o.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order is null)
            return Result<CancelSubscriptionResponse>.Failure("order_not_found", "Pedido nao encontrado.");

        if (order.Status != OrderStatus.Paid)
            return Result<CancelSubscriptionResponse>.Failure("order_not_paid", "Pedido nao foi pago.");

        if (order.Product?.Category != ProductCategory.Subscription)
            return Result<CancelSubscriptionResponse>.Failure("not_subscription", "Produto nao e uma assinatura.");

        var customerId = order.User?.StripeCustomerId;
        var subscriptionId = order.StripeSubscriptionId;

        if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(subscriptionId))
        {
            customerId = await portalGateway.GetCustomerIdFromSubscriptionAsync(subscriptionId, ct);
        }

        if (string.IsNullOrWhiteSpace(customerId) && !string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId))
        {
            var (fetchedCustomerId, fetchedSubscriptionId) =
                await portalGateway.GetIdsFromCheckoutSessionAsync(order.StripeCheckoutSessionId, ct);

            customerId = fetchedCustomerId;
            if (!string.IsNullOrWhiteSpace(fetchedSubscriptionId))
                subscriptionId = fetchedSubscriptionId;
        }

        if (!string.IsNullOrWhiteSpace(customerId) || !string.IsNullOrWhiteSpace(subscriptionId))
        {
            if (order.User is not null && string.IsNullOrWhiteSpace(order.User.StripeCustomerId) && !string.IsNullOrWhiteSpace(customerId))
                order.User.StripeCustomerId = customerId;
            if (string.IsNullOrWhiteSpace(order.StripeSubscriptionId) && !string.IsNullOrWhiteSpace(subscriptionId))
                order.StripeSubscriptionId = subscriptionId;
            await dbContext.SaveChangesAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(customerId))
            return Result<CancelSubscriptionResponse>.Failure("no_customer", "Usuario nao possui customer ID no Stripe.");

        var portalUrl = await portalGateway.CreatePortalSessionAsync(
            customerId,
            subscriptionId ?? string.Empty,
            returnUrl,
            ct);

        return Result<CancelSubscriptionResponse>.Success(new CancelSubscriptionResponse { PortalUrl = portalUrl });
    }

    public async Task<Result<BundleDownloadResponse>> GetBundleDownloadAsync(
        Guid orderId, Guid userId, CancellationToken ct)
    {
        var order = await dbContext.Orders
            .Include(o => o.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order is null)
            return Result<BundleDownloadResponse>.Failure("order_not_found", "Pedido nao encontrado.");

        if (order.Status != OrderStatus.Paid)
            return Result<BundleDownloadResponse>.Failure("order_not_paid", "Pedido nao foi pago.");

        if (order.Product?.Category != ProductCategory.Bundle)
            return Result<BundleDownloadResponse>.Failure("not_bundle", "Produto nao e do tipo Bundle.");

        var bundle = await dbContext.Bundles.FirstOrDefaultAsync(b => b.ProductId == order.ProductId, ct);
        if (bundle is null)
            return Result<BundleDownloadResponse>.Failure("bundle_not_configured", "Bundle nao configurado para este produto.");

        return Result<BundleDownloadResponse>.Success(new BundleDownloadResponse
        {
            DownloadUrl = bundle.DriveUrl,
            FileName = bundle.FileName
        });
    }
}
