using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ProductsFunction;

public sealed class SaveProductFunction
{
    private readonly ProductsDbContext _dbContext;

    public SaveProductFunction(ProductsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Function("CreateProduct")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "products")] HttpRequestData req)
    {
        var product = await req.ReadFromJsonAsync<Product>();
        if (product is null || string.IsNullOrWhiteSpace(product.Name))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        product.Id = 0;
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(product);
        return response;
    }

    [Function("UpdateProduct")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "products/{id:int}")] HttpRequestData req,
        int id)
    {
        var input = await req.ReadFromJsonAsync<Product>();
        if (input is null || string.IsNullOrWhiteSpace(input.Name))
        {
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var existing = await _dbContext.Products.FindAsync(id);
        if (existing is null)
        {
            return req.CreateResponse(HttpStatusCode.NotFound);
        }

        existing.Name = input.Name;
        existing.Price = input.Price;

        await _dbContext.SaveChangesAsync();

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(existing);
        return response;
    }
}
