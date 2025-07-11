using System.Text.Json;
using Core.Entities;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context)
    {
        if (!context.Products.Any())
        {
            string productsData = await
                File.ReadAllTextAsync("../Infrastructure/Data/SeedData/products.json");
            List<Product>? products = JsonSerializer.Deserialize<List<Product>>(productsData);
            if (products == null) return;

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        if (!context.DeliveryMethods.Any())
        {
            string deliveryMethodsData = await
                File.ReadAllTextAsync("../Infrastructure/Data/SeedData/delivery.json");
            List<DeliveryMethod>? deliveryMethods = JsonSerializer
                .Deserialize<List<DeliveryMethod>>(deliveryMethodsData);
            if (deliveryMethods == null) return;

            context.DeliveryMethods.AddRange(deliveryMethods);
            await context.SaveChangesAsync();
        }
    }
}
