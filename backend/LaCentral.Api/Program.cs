using Microsoft.EntityFrameworkCore;
using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Data.Servicios; 
using LaCentral.UseCases.Puertos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using LaCentral.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LaCentralDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IServicioHash, ServicioHash>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IServicioHash, ServicioHash>(); 
builder.Services.AddScoped<IGeneradorToken, GeneradorToken>();
builder.Services.AddScoped<AutenticarUsuarioUseCase>(); 

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true, // Acá se cumple tu prueba de token vencido
            ValidateIssuerSigningKey = true,
            
            // Mapeo directo a tu appsettings.json
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty))
        };
    });

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "La Central · API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseAuthentication(); 
app.UseAuthorization();

// Endpoint temporal para probar la conexión a la base (Tu objetivo del Martes)
app.MapGet("/api/test-db", async (LaCentralDbContext context) => 
{
    var usuarios = await context.Usuarios.Select(u => u.NombreUsuario).ToListAsync();
    return Results.Ok(usuarios);
});

app.MapControllers();
app.Run();