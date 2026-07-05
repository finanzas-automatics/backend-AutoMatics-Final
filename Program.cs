using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using AutoMatics.Infrastructure.Data;
using AutoMatics.Infrastructure.Repositories;
using AutoMatics.Domain.IAM.Repositories;
using AutoMatics.Domain.Clientes.Repositories;
using AutoMatics.Domain.Creditos.Repositories;
using AutoMatics.Domain.Common;
using AutoMatics.Application.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DB relacional MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AutoMaticsDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46)))
);

// 2. Seguridad - Autenticación JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"]
        };
    });

// 3. Inyección del patrón Repositorio y Unit of Work de Infraestructura
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<ICreditoRepository, CreditoRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. Inyección de las capas internas Command/Query Services mediante extensión
builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 5. Configurar Swagger para admitir Authorization Header en las pruebas de API
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "Autenticación JWT. Escribe: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        new string[] { }
    }});
});

builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// ✅ CORS absolutamente primero
app.UseCors("AllowAll");

// ✅ Captura excepciones no manejadas y devuelve JSON en vez de cerrar la conexión
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        // ✅ Mostrar el error real durante desarrollo
        var exceptionFeature = context.Features
            .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var mensaje = exceptionFeature?.Error?.Message ?? "Error desconocido";
        var inner   = exceptionFeature?.Error?.InnerException?.Message ?? "";
        
        await context.Response.WriteAsync(
            $"{{\"exito\":false,\"mensaje\":\"{mensaje}\",\"inner\":\"{inner}\"}}"
        );
    });
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();