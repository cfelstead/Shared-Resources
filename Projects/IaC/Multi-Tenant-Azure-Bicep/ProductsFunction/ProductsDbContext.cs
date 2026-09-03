using Microsoft.EntityFrameworkCore;

namespace ProductsFunction;

public sealed class ProductsDbContext : DbContext
{
    public ProductsDbContext(DbContextOptions<ProductsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
}
