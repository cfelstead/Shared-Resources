using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProductsWeb.Models;
using System.Net.Http.Json;

namespace ProductsWeb.Pages;

public class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IndexModel(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IReadOnlyList<Product> Products { get; private set; } = [];

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("ProductsApi");
            Products = await client.GetFromJsonAsync<List<Product>>("api/products") ?? [];
        }
        catch
        {
            ErrorMessage = "Could not load products from the function app.";
        }
    }
}
