namespace Microservice.Products.WebAPI.Dtos
{
    public record class CreateProductDto(string Name, decimal Price, int Stock);
}
