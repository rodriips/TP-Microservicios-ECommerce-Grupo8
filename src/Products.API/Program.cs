using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Products.API.Common;
using Products.API.Data;
using Products.API.ExceptionHandlers;
using Products.API.Middleware;
using Products.API.Services;
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
    .Enrich.WithProperty("Servicio", "Products.API")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/products-api-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Products.API...");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // 2. CONTROLLERS
    //    Desactivamos la respuesta 400 automática de ASP.NET para
    //    validar nosotros en el controller y devolver PRD-002 con el
    //    mismo formato de error que el resto.
    // ============================================================
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(Configuracion.ConfigurarValidacion);

    // ============================================================
    // 3. INYECCIÓN DE DEPENDENCIAS
    //    ProductRepository es Singleton porque guarda la lista en
    //    memoria (cuando la cátedra dé su librería, se reemplaza ahí).
    // ============================================================
    builder.Services.AddSingleton<ProductRepository>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddHttpContextAccessor();

    // Cliente HTTP para consultar Orders.API antes de eliminar (PRD-004).
    // La URL y el timeout se configuran en ProductService al crear el cliente.
    builder.Services.AddHttpClient("OrdersApi");

    // ============================================================
    // 4. MANEJO DE ERRORES CON IExceptionHandler
    //    Orden: primero los específicos, último el genérico.
    // ============================================================
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ============================================================
    // 5. HEALTH CHECKS
    //    "self" solo confirma que el servicio está levantado (Common/SelfHealthCheck.cs).
    // ============================================================
    builder.Services.AddHealthChecks().AddCheck<SelfHealthCheck>("self");

    // ============================================================
    // 6. SWAGGER con los comentarios XML de controllers y DTOs
    //    (la configuración está en Common/Configuracion.cs)
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(Configuracion.ConfigurarSwagger);

    WebApplication app = builder.Build();

    // ============================================================
    // 7. PIPELINE (el orden importa)
    // ============================================================
    app.UseMiddleware<CorrelationIdMiddleware>(); // primero: así todos los logs tienen el Correlation ID
    app.UseSerilogRequestLogging();               // loguea el fin de cada request con su duración
    app.UseExceptionHandler();                    // usa los IExceptionHandler registrados arriba

    app.UseSwagger(Configuracion.ConfigurarSwaggerJson);
    app.UseSwaggerUI(Configuracion.ConfigurarSwaggerUI);

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
        Predicate = Configuracion.NingunCheck,
        ResponseWriter = HealthCheckResponseWriter.EscribirRespuesta
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Products.API finalizó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
