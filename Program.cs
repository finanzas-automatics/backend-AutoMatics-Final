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

// Soporte para despliegue en Railway (Variables de Entorno)
var railwayMySqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");
if (!string.IsNullOrEmpty(railwayMySqlUrl))
{
    var isParsed = Uri.TryCreate(railwayMySqlUrl, UriKind.Absolute, out Uri? dbUri);
    if (isParsed && dbUri != null)
    {
        var userInfo = dbUri.UserInfo.Split(':');
        var user = userInfo[0];
        var pass = userInfo.Length > 1 ? userInfo[1] : "";
        connectionString = $"Server={dbUri.Host};Port={dbUri.Port};Database={dbUri.LocalPath.TrimStart('/')};Uid={user};Pwd={pass};SslMode=Required;"
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddDbContext<AutoMaticsDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 46)))
);

// 2. Seguridad - Autenticación JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.MapInboundClaims = false;  
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

// =========================================================================
// ✅ TRUCO: Auto-creación y migración de la base de datos al ejecutar la app
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AutoMaticsDbContext>();
        // Ejecuta las migraciones pendientes y crea la BD MySQL si no existe
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al intentar crear o migrar la base de datos.");
    }
}
// =========================================================================

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
       
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
