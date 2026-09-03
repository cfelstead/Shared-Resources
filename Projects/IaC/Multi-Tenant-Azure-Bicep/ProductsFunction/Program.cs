using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductsFunction;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var connectionString = context.Configuration["SqlConnectionString"]
            ?? throw new InvalidOperationException("SqlConnectionString setting is required.");

        services.AddDbContext<ProductsDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    if (!await dbContext.Products.AnyAsync())
    {
        dbContext.Products.AddRange(
            new Product { Name = "Laptop", Price = 999.99m },
            new Product { Name = "Mouse", Price = 24.99m },
            new Product { Name = "Keyboard", Price = 79.50m },
            new Product { Name = "Monitor", Price = 299.00m });

        await dbContext.SaveChangesAsync();
    }
}

await host.RunAsync();
