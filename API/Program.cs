using API.Middleware;
using API.SignalR;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<StoreContext>(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure());
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddCors();

builder.Services.AddSingleton<IConnectionMultiplexer>(config =>
{
    string? connString = builder.Configuration.GetConnectionString("Redis")
        ?? throw new Exception("Cannot get redis connection string");
    ConfigurationOptions configuration = ConfigurationOptions.Parse(connString, true);
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton<ICartService, CartService>();
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICouponService, CouponService>();
builder.Services.AddSignalR();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

app.UseHttpsRedirection();

// app.UseAuthorization();
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

app.UseAuthentication(); // Authenticate for hub
app.UseAuthorization(); // Authorize for hub

app.UseDefaultFiles(); // For hosting wwwroot angular client
app.UseStaticFiles(); // For hosting wwwroot angular client

app.MapControllers();
app.MapGroup("api").MapIdentityApi<AppUser>(); // api/login
app.MapHub<NotificationHub>("/hub/notifications");
app.MapFallbackToController("Index", "Fallback");

try
{
    using IServiceScope scope = app.Services.CreateScope();
    IServiceProvider services = scope.ServiceProvider;
    StoreContext context = services.GetRequiredService<StoreContext>();
    UserManager<AppUser> userManager = services.GetRequiredService<UserManager<AppUser>>();
    await context.Database.MigrateAsync();
    await StoreContextSeed.SeedAsync(context, userManager);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    throw;
}

app.Run();
