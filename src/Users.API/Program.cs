using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using Users.API.Common;
using Users.API.ExceptionHandlers;
using Users.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Serilog estructurado (Consola y Archivo)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Microservice", "Users.API")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Microservice}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/users-api-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{Microservice}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. Controladores con filtro de validación de ModelState adaptado al catálogo (USR-002)
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);

            var correlationId = context.HttpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                                ?? context.HttpContext.TraceIdentifier;

            var problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = 400,
                detail = "Los datos del usuario son inválidos.",
                instance = context.HttpContext.Request.Path.Value,
                errorCode = ErrorCodes.USR_002,
                errorMessage = string.Join("; ", errors),
                correlationId = correlationId
            };

            return new BadRequestObjectResult(problemDetails);
        };
    });

// 3. Inyección de Dependencias
builder.Services.AddSingleton<IUserService, UserService>();

// 4. Manejo global de excepciones con IExceptionHandler
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 5. Health Checks
builder.Services.AddHealthChecks();

// 6. Swagger con documentación XML y OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Users.API - ECommerce Microservices",
        Version = "v1",
        Description = "Microservicio de autenticación y gestión de usuarios para la plataforma de E-Commerce."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Middleware: Manejo de Correlation ID
app.useCorrelationIdMiddleware();

// Middleware: Serilog Request Logging
app.UseSerilogRequestLogging();

// Manejador de excepciones del framework
app.UseExceptionHandler();

// Swagger UI disponible en raíz o /swagger
app.UseSwagger(c =>
{
    c.SerializeAsV2 = false;
});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users.API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();

// Rutas de controladores
app.MapControllers();

// Health Checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready");
app.MapHealthChecks("/health/live");

try
{
    Log.Information("Iniciando Users.API en el puerto correspondiente...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Users.API finalizó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Extensión para inyectar y propagar X-Correlation-Id
/// </summary>
public static class CorrelationIdExtensions
{
    public static IApplicationBuilder useCorrelationIdMiddleware(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            const string correlationHeader = "X-Correlation-Id";

            if (!context.Request.Headers.TryGetValue(correlationHeader, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers[correlationHeader] = correlationId;
            }

            context.Response.Headers[correlationHeader] = correlationId;

            using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId.ToString()))
            {
                await next();
            }
        });
    }
}
