using Microservice.Gateway.YARP.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Gateway.YARP.Context
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
