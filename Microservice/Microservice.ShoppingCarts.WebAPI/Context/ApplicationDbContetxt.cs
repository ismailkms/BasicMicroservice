using Microservice.ShoppingCarts.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.ShoppingCarts.WebAPI.Context
{
    public sealed class ApplicationDbContetxt : DbContext
    {
        public ApplicationDbContetxt(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    }
}
