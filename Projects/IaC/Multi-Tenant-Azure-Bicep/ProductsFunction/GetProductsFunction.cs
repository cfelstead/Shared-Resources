using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;

namespace ProductsFunction;

public sealed class GetProductsFunction
{
    private readonly ProductsDbContext _dbContext;

    public GetProductsFunction(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function("GetProducts")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(products);
        return response;
    }
}
