namespace Microservice.ShoppingCarts.WebAPI.Dtos
{
    public sealed record ChangeProductStockDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
