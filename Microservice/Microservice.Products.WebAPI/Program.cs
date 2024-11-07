using Microservice.Products.WebAPI.Context;
using Microservice.Products.WebAPI.Dtos;
using Microservice.Products.WebAPI.Models;
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
    var products = await context.Products.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    return Results.Ok(products);
});

app.MapPost("/create", async (CreateProductDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    bool isNameExists = await context.Products.AnyAsync(p => p.Name == request.Name, cancellationToken);

    if (isNameExists)
    {
        return Results.BadRequest("Ürün adý daha önce oluþturulmuþ");
    }

    Product product = new()
    {
        Name = request.Name,
        Price = request.Price,
        Stock = request.Stock
    };

    await context.AddAsync(product, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return Results.Ok("Ürün baþarýyla eklendi");
});

app.MapPost("/change-product-stock", async (List<ChangeProductStockDto> request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    foreach(var item in  request)
    {
        Product? product = await context.Products.FindAsync(item.ProductId, cancellationToken);
        if(product is not null)
        {
            product.Stock -= item.Quantity;
        }
    }

    await context.SaveChangesAsync(cancellationToken);

    return Results.NoContent();
});

using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}
//Uygulama her çalýþtýðýnda migrate edilecek bir migration varsa otomatik olarak migrate eder.

app.Run();
