using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Users.API.Common;
using Users.API.Data;
using Users.API.ExceptionHandlers;
using Users.API.Middleware;
using Users.API.Services;

// ============================================================
// 1. SERILOG
//    - Consola: formato legible para ver durante la demo.
//    - Archivo: formato JSON estructurado, uno nuevo por día en logs/.
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "Users.API")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/users-api-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Users.API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // 2. CONTROLLERS
    //    Desactivamos el filtro automático de ModelState para
    //    capturarlo en el controller y lanzar ValidationException (USR-002).
    // ============================================================
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // ============================================================
    // 3. INYECCIÓN DE DEPENDENCIAS
    //    UserRepository es Singleton para persistencia en memoria.
    // ============================================================
    builder.Services.AddSingleton<UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddHttpContextAccessor();

    // ============================================================
    // 4. MANEJO DE ERRORES CON IExceptionHandler (1 por tipo)
    //    Orden: específicos primero, GlobalExceptionHandler al final.
    // ============================================================
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<ConflictExceptionHandler>();
    builder.Services.AddExceptionHandler<UnauthorizedExceptionHandler>();
    builder.Services.AddExceptionHandler<ForbiddenExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ============================================================
    // 5. HEALTH CHECKS
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Users.API está funcionando."));

    // ============================================================
    // 6. SWAGGER con comentarios XML
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Users.API - ECommerce Microservices",
            Version = "v1",
            Description = "Microservicio de autenticación y gestión de usuarios para la plataforma de E-Commerce."
        });

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Users.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }
    });

    var app = builder.Build();

    // ============================================================
    // 7. PIPELINE DE MIDDLEWARES
    // ============================================================
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true;
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Users.API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapControllers();

    // Health Checks estructurados en JSON
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.EscribirRespuesta
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.EscribirRespuesta
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => false,
        ResponseWriter = HealthCheckResponseWriter.EscribirRespuesta
    });

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
