using System.Reflection;
using System.Text.Json;
using Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Data;

public class StoreContextSeed
{
    public static async Task SeedAsync(StoreContext context, UserManager<AppUser> userManager)
    {
        if (!userManager.Users.Any(x => x.UserName == "admin@test.com"))
        {
            AppUser user = new()
            {
                UserName = "admin@test.com",
                Email = "admin@test.com"
            };

            await userManager.CreateAsync(user, "Pa$$w0rd");
            await userManager.AddToRoleAsync(user, "Admin");
        }

        string? path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (!context.Products.Any())
        {
            string productsData = await
                File.ReadAllTextAsync(path + @"/Data/SeedData/products.json");
            List<Product>? products = JsonSerializer.Deserialize<List<Product>>(productsData);
            if (products == null) return;

            context.Products.AddRange(products);
            await context.SaveChangesAsync();
        }

        if (!context.DeliveryMethods.Any())
        {
            string deliveryMethodsData = await
                File.ReadAllTextAsync(path + @"/Data/SeedData/delivery.json");
            List<DeliveryMethod>? deliveryMethods = JsonSerializer
                .Deserialize<List<DeliveryMethod>>(deliveryMethodsData);
            if (deliveryMethods == null) return;

            context.DeliveryMethods.AddRange(deliveryMethods);
            await context.SaveChangesAsync();
        }
    }
}
