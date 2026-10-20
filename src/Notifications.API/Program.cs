using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Notifications.API.Common;
using Notifications.API.Data;
using Notifications.API.ExceptionHandlers;
using Notifications.API.Middleware;
using Notifications.API.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

// ============================================================
// 1. SERILOG (la consigna pide configurarlo antes que todo)
//    - Consola: formato legible para leer durante la demo.
//    - Archivo: formato JSON estructurado, uno nuevo por día en logs/.
// ============================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "Notifications.API")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/notifications-api-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Notifications.API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // 2. CONTROLLERS
    //    Desactivamos la respuesta 400 automática de ASP.NET para
    //    validar nosotros en el controller y devolver NTF-002 con el
    //    mismo formato de error que el resto.
    // ============================================================
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // ============================================================
    // 3. INYECCIÓN DE DEPENDENCIAS
    //    NotificationRepository es Singleton porque guarda la lista en
    //    memoria (cuando la cátedra dé su librería, se reemplaza ahí).
    // ============================================================
    builder.Services.AddSingleton<NotificationRepository>();
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddHttpContextAccessor();

    // Cliente HTTP para validar el destinatario contra Users.API (NTF-001)
    string urlUsersApi = builder.Configuration["Servicios:UsersApi"] ?? "http://localhost:5002";
    builder.Services.AddHttpClient("UsersApi", client =>
    {
        client.BaseAddress = new Uri(urlUsersApi);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    // ============================================================
    // 4. MANEJO DE ERRORES CON IExceptionHandler
    //    Orden: primero los específicos, último el genérico.
    // ============================================================
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ============================================================
    // 5. HEALTH CHECKS
    //    "self" solo confirma que el servicio está levantado.
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Notifications.API está funcionando."));

    // ============================================================
    // 6. SWAGGER con los comentarios XML de controllers y DTOs
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Notifications.API - ECommerce Microservices",
            Version = "v1",
            Description = "Microservicio de notificaciones (envío simulado por Email, Push o SMS) de la plataforma de E-Commerce."
        });

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Notifications.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }
    });

    var app = builder.Build();

    // ============================================================
    // 7. PIPELINE (el orden importa)
    // ============================================================
    app.UseMiddleware<CorrelationIdMiddleware>(); // primero: así todos los logs tienen el Correlation ID
    app.UseSerilogRequestLogging();               // loguea el fin de cada request con su duración
    app.UseExceptionHandler();                    // usa los IExceptionHandler registrados arriba

    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true; // igual que en Users.API y Products.API
    });
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Notifications.API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapControllers();

    // /health y /health/ready corren todos los checks.
    // /health/live no corre ninguno: solo responde si el proceso está vivo.
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
    Log.Fatal(ex, "Notifications.API finalizó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
