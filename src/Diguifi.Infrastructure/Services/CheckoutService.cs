using Diguifi.Application.Common;
using Diguifi.Application.DTOs.Checkout;
using Diguifi.Application.Interfaces;
using Diguifi.Domain.Entities;
using Diguifi.Domain.Enums;
using Diguifi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Diguifi.Infrastructure.Services;

public sealed class CheckoutService(
    AppDbContext dbContext,
    IStripeCheckoutGateway stripeCheckoutGateway) : ICheckoutService
{
    public async Task<Result<CheckoutSessionResponse>> CreateSessionAsync(Guid userId, CreateCheckoutSessionRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Result<CheckoutSessionResponse>.Failure("user_not_found", "Usuário autenticado não encontrado.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return Result<CheckoutSessionResponse>.Failure("product_not_found", "O produto solicitado não foi encontrado.");
        }

        var order = new Order
        {
            UserId = userId,
            ProductId = product.Id,
            Status = OrderStatus.Pending,
            Amount = product.Price,
            Currency = product.Currency
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        var session = await stripeCheckoutGateway.CreateCheckoutSessionAsync(new StripeCheckoutSessionRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            Amount = product.Price,
            Currency = product.Currency,
            ReturnUrl = request.ReturnUrl,
            CancelUrl = request.CancelUrl,
            StripePriceId = product.StripePriceId,
            CustomerEmail = user.Email
        }, cancellationToken);

        order.StripeCheckoutSessionId = session.CheckoutSessionId;
        order.StripePaymentIntentId = session.PaymentIntentId;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CheckoutSessionResponse>.Success(new CheckoutSessionResponse
        {
            CheckoutUrl = session.CheckoutUrl
        });
    }
}
