using Microsoft.EntityFrameworkCore;
using LaCentral.Data; // Acá vive ahora tu DbContext
using LaCentral.Data.Repositorios;
using LaCentral.Data.Servicios; // Para el hash de BCrypt (Miércoles)
using LaCentral.UseCases.Puertos;

var builder = WebApplication.CreateBuilder(args);

// 1. Registrar DbContext leyendo el appsettings.json
builder.Services.AddDbContext<LaCentralDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// 2. Inyectar dependencias (Puertos -> Implementaciones en Data)
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IServicioHash, ServicioHash>(); // Lo tuyo de hoy

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint temporal para probar la conexión a la base (Objetivo del Martes)
app.MapGet("/api/test-db", async (LaCentralDbContext context) => 
{
    var usuarios = await context.Usuarios.Select(u => u.NombreUsuario).ToListAsync();
    return Results.Ok(usuarios);
});

app.MapControllers();
app.Run();