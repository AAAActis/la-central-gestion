using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Data.Servicios; 
using LaCentral.UseCases;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Puertos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using LaCentral.Api.Seguridad;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LaCentralDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddScoped<IServicioHash, ServicioHash>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IGeneradorToken, GeneradorToken>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IContextoUsuario, ContextoUsuario>();

// Casos de uso: se registran como clase concreta, sin interfaz.
// Los controladores los reciben por constructor.
builder.Services.AddScoped<CrearUsuarioUseCase>();
builder.Services.AddScoped<AutenticarUsuarioUseCase>();
builder.Services.AddScoped<CrearClienteUseCase>();

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

builder.Services.AddOpenApi(opciones =>
{
    // Suma el esquema Bearer al documento: es lo que habilita el
    // botón Authorize de Swagger para probar endpoints protegidos.
    opciones.AddDocumentTransformer<SeguridadOpenApi>();
});
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
}).RequireAuthorization();

app.MapControllers();
app.Run();