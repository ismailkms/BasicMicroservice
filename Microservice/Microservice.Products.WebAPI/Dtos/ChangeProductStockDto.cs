namespace Microservice.Products.WebAPI.Dtos
{
    public sealed record ChangeProductStockDto(Guid ProductId, int Quantity);
}
