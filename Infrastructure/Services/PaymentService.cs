using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Product = Core.Entities.Product;

namespace Infrastructure.Services;

public class PaymentService(IConfiguration config, ICartService cartService,
    IGenericRepository<Product> productRepo,
    IGenericRepository<DeliveryMethod> dmRepo) : IPaymentService
{
    public async Task<ShoppingCart?> CreateOrUpdatePaymentIntent(string cartId)
    {
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];
        ShoppingCart? cart = await cartService.GetCartAsync(cartId);
        if (cart == null) return null;

        decimal shippingPrice = 0m;
        if (cart.DeliveryMethodId.HasValue)
        {
            DeliveryMethod? deliveryMethod = await dmRepo.GetByIdAsync((int)cart.DeliveryMethodId);
            if (deliveryMethod == null) return null;
            shippingPrice = deliveryMethod.Price;
        }

        foreach (CartItem item in cart.Items)
        {
            Product? productItem = await productRepo.GetByIdAsync(item.ProductId);
            if (productItem == null) return null;
            if (item.Price != productItem.Price) item.Price = productItem.Price;
        }

        PaymentIntentService service = new();
        PaymentIntent? intent = null;

        if (string.IsNullOrEmpty(cart.PaymentIntentId))
        {
            PaymentIntentCreateOptions options = new()
            {
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100))
                    + (long)shippingPrice * 100,
                Currency = "usd",
                PaymentMethodTypes = ["card"]
            };
            intent = await service.CreateAsync(options);
            cart.PaymentIntentId = intent.Id;
            cart.ClientSecret = intent.ClientSecret;
        }
        else
        {
            PaymentIntentUpdateOptions options = new()
            {
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100))
                    + (long)shippingPrice * 100
            };
            intent = await service.UpdateAsync(cart.PaymentIntentId, options);
        }

        await cartService.SetCartAsync(cart);
        return cart;
    }
}
