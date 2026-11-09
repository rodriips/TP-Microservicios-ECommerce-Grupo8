using Dapper;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Cart.API.Common;
using Cart.API.Data;
using Cart.API.ExceptionHandlers;
using Cart.API.Middleware;
using Cart.API.Services;

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
    .Enrich.WithProperty("Servicio", "Cart.API")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/cart-api-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Cart.API...");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // 2. CONTROLLERS
    //    Desactivamos el filtro automático de ModelState para
    //    capturarlo en el controller y lanzar ValidationException (CRT-004).
    // ============================================================
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(Configuracion.ConfigurarValidacion);

    // ============================================================
    // 3. HTTPCLIENT para llamar a Products.API
    //    Solo se registra el nombre. La URL base (appsettings.json, "Servicios:ProductsApi")
    //    y el timeout se configuran en CartService, al crear el cliente.
    // ============================================================
    builder.Services.AddHttpClient("ProductsAPI");

    // ============================================================
    // 4. INYECCIÓN DE DEPENDENCIAS
    //    CartRepository guarda los carritos en SQLite (ver Data/).
    //    Es Singleton porque no guarda nada en memoria: cada método
    //    abre su propia conexión a la base.
    // ============================================================
    builder.Services.AddSingleton<CartRepository>();
    builder.Services.AddSingleton<DatabaseInitializer>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddHttpContextAccessor();

    // ============================================================
    // 5. MANEJO DE ERRORES CON IExceptionHandler (1 por tipo)
    //    Orden: específicos primero, GlobalExceptionHandler al final.
    // ============================================================
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<InsufficientStockExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // ============================================================
    // 6. HEALTH CHECKS
    //    "self" solo confirma que el servicio está levantado (Common/SelfHealthCheck.cs).
    //    "sqlite" verifica que la base responda (Common/SqliteHealthCheck.cs).
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck<SelfHealthCheck>("self")
        .AddCheck<SqliteHealthCheck>("sqlite");

    // ============================================================
    // 7. SWAGGER con comentarios XML
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(Configuracion.ConfigurarSwagger);

    WebApplication app = builder.Build();

    // ============================================================
    // 8. BASE DE DATOS SQLite
    //    - Le decimos a Dapper cómo guardar y leer Guid y fechas
    //      (SQLite no tiene esos tipos, se guardan como texto).
    //    - Creamos las tablas Carts y CartItems si no existen.
    // ============================================================
    SqlMapper.RemoveTypeMap(typeof(Guid));
    SqlMapper.AddTypeHandler(new GuidTypeHandler());
    SqlMapper.RemoveTypeMap(typeof(DateTime));
    SqlMapper.AddTypeHandler(new FechaTypeHandler());

    DatabaseInitializer inicializador = app.Services.GetRequiredService<DatabaseInitializer>();
    inicializador.Inicializar();

    // ============================================================
    // 9. PIPELINE DE MIDDLEWARES
    // ============================================================
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseSwagger(Configuracion.ConfigurarSwaggerJson);
    app.UseSwaggerUI(Configuracion.ConfigurarSwaggerUI);

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
        Predicate = Configuracion.NingunCheck,
        ResponseWriter = HealthCheckResponseWriter.EscribirRespuesta
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Cart.API finalizó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}
