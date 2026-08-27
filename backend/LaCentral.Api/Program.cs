using Microsoft.EntityFrameworkCore;
using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Data.Servicios; 
using LaCentral.UseCases.Puertos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LaCentralDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IServicioHash, ServicioHash>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IServicioHash, ServicioHash>(); 
builder.Services.AddScoped<IGeneradorToken, GeneradorToken>(); 

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

// Endpoint temporal para probar la conexión a la base (Tu objetivo del Martes)
app.MapGet("/api/test-db", async (LaCentralDbContext context) => 
{
    var usuarios = await context.Usuarios.Select(u => u.NombreUsuario).ToListAsync();
    return Results.Ok(usuarios);
});

app.MapControllers();
app.Run();