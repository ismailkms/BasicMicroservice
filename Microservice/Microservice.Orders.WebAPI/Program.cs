using Microservice.Orders.WebAPI.Context;
using Microservice.Orders.WebAPI.Dtos;
using Microservice.Orders.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/getall", async (ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    List<Order> orders = await context.Orders.ToListAsync(cancellationToken);

    HttpClient client = new HttpClient();

    var message = await client.GetAsync("http://products:8080/getall", cancellationToken);

    List<ProductDto>? products = new();
    if (message.IsSuccessStatusCode)
    {
        products = await message.Content.ReadFromJsonAsync<List<ProductDto>>(cancellationToken);
    }

    List<OrderDto> ordersDto = orders.Select(o => new OrderDto()
    {
        Id = o.Id,
        CreateAt = o.CreateAt,
        Price = o.Price,
        ProductId = o.ProductId,
        Quantity = o.Quantity,
        ProductName = products.FirstOrDefault(p => p.Id == o.ProductId).Name
    }).ToList();

    return Results.Ok(ordersDto);
});

app.MapPost("/create", async (List<CreateOrderDto> request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    List<Order> orders = new();
    foreach(var item in request)
    {
        Order order = new()
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price,
            CreateAt = DateTime.Now
        };

        orders.Add(order);
    }
    await context.AddRangeAsync(orders, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok("Sipariþ baþarýyla oluþturuldu");
});

using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
