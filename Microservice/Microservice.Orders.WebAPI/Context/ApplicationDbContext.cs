﻿using Microservice.Orders.WebAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservice.Orders.WebAPI.Context
{
    public sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
    }
}
