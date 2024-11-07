using Microservice.Gateway.YARP.Context;
using Microservice.Gateway.YARP.Dtos;
using Microservice.Gateway.YARP.Models;
using Microservice.Gateway.YARP.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration.GetSection("JWT:Issuer").Value,
        ValidAudience = builder.Configuration.GetSection("JWT:Audience").Value,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("JWT:SecretKey").Value ?? ""))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.MapGet("/", () => "Hello World!");

app.MapPost("/auth/register", async (RegisterDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    bool isUserNameExists = await context.Users.AnyAsync(u => u.UserName == request.UserName, cancellationToken);
    if (!isUserNameExists)
    {
        User user = new()
        {
            Password = request.Password,
            UserName = request.UserName
        };
        await context.AddAsync(user, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return Results.Ok("Kullanýcý kayýdý baþarýlý");
    }
    return Results.BadRequest("Kullanýcý adý önceden alýnmýþ");
});

app.MapPost("/auth/login", async (LoginDto request, ApplicationDbContext context, CancellationToken cancellationToken) =>
{
    User? user = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName && u.Password == request.Password, cancellationToken);
    if (user is not null)
    {
        JwtProvider jwtProvider = new(builder.Configuration);

        string token = jwtProvider.CreateToken(user);

        return Results.Ok(token);
    }
    return Results.BadRequest("Kullanýcý bulunamadý");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

using (var scoped = app.Services.CreateScope())
{
    var srv = scoped.ServiceProvider;
    var context = srv.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
