using Microservice.ShoppingCarts.WebAPI.Context;
using Microservice.ShoppingCarts.WebAPI.Dtos;
using Microservice.ShoppingCarts.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContetxt>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/getall", async (ApplicationDbContetxt context, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    List<ShoppingCart> shoppingCarts = await context.ShoppingCarts.ToListAsync(cancellationToken);

    HttpClient client = new HttpClient();

    string productsEndpoint = $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";
    var message = await client.GetAsync(productsEndpoint);

    List<ProductDto>? products = new();
    if (message.IsSuccessStatusCode)
    {
        products = await message.Content.ReadFromJsonAsync<List<ProductDto>>();
    }

    List<ShoppingCartDto> response = shoppingCarts.Select(s => new ShoppingCartDto()
    {
        Id = s.Id,
        ProductId = s.ProductId,
        Quantity = s.Quantity,
        ProductName = products.FirstOrDefault(p => p.Id == s.ProductId).Name,
        ProductPrice = products.FirstOrDefault(p => p.Id == s.ProductId).Price
    }).ToList();

    return Results.Ok(response);
});

app.MapPost("/create", async (CreateShoppingCartDto request, ApplicationDbContetxt context, CancellationToken cancellationToken) =>
{
    ShoppingCart shoppingCart = new()
    {
        ProductId = request.ProductId,
        Quantity = request.Quantity
    };

    await context.AddAsync(shoppingCart, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok("Ürün sepete baþarýyla eklendi");
});

app.MapGet("/createOrder", async (ApplicationDbContetxt context, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    List<ShoppingCart> shoppingCarts = await context.ShoppingCarts.ToListAsync(cancellationToken);

    HttpClient client = new HttpClient();

    string productsEndpoint = $"http://{configuration.GetSection("HttpRequest:Products").Value}/getall";
    var message = await client.GetAsync(productsEndpoint);

    List<ProductDto>? products = new();
    if (message.IsSuccessStatusCode)
    {
        products = await message.Content.ReadFromJsonAsync<List<ProductDto>>();
    }

    List<CreateOrderDto> response = shoppingCarts.Select(s => new CreateOrderDto()
    {
        ProductId = s.ProductId,
        Quantity = s.Quantity,
        Price = products.FirstOrDefault(p => p.Id == s.ProductId).Price
    }).ToList();

    string ordersEndpoint = $"http://{configuration.GetSection("HttpRequest:Orders").Value}/create";

    string stringJson = JsonSerializer.Serialize(response);
    var content = new StringContent(stringJson, Encoding.UTF8, "application/json");

    var orderMessage = await client.PostAsync(ordersEndpoint, content);

    if(orderMessage.IsSuccessStatusCode)
    {
        List<ChangeProductStockDto> changeProductStockDtos = shoppingCarts.Select(s => new ChangeProductStockDto()
        {
            ProductId=s.ProductId,
            Quantity = s.Quantity,
        }).ToList();

        productsEndpoint = $"http://{configuration.GetSection("HttpRequest:Products").Value}/change-product-stock";

        string productStringJson = JsonSerializer.Serialize(changeProductStockDtos);
        var productContent = new StringContent(productStringJson, Encoding.UTF8, "application/json");

        await client.PostAsync(productsEndpoint, productContent);

        context.RemoveRange(shoppingCarts);
        await context.SaveChangesAsync(cancellationToken);
    }

    return Results.Ok("Sipariþ baþarýyla oluþturuldu");
});

using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContetxt>();
    context.Database.Migrate();
}

app.Run();
