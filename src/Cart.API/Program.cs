using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
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

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ============================================================
    // 2. CONTROLLERS
    //    Desactivamos el filtro automático de ModelState para
    //    capturarlo en el controller y lanzar ValidationException (CRT-004).
    // ============================================================
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    // ============================================================
    // 3. HTTPCLIENT para llamar a Products.API
    //    La URL base se lee de appsettings.json ("Servicios:ProductsApi").
    // ============================================================
    string productsApiUrl = builder.Configuration["Servicios:ProductsApi"] ?? "http://localhost:5001";
    builder.Services.AddHttpClient("ProductsAPI", client =>
    {
        client.BaseAddress = new Uri(productsApiUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    // ============================================================
    // 4. INYECCIÓN DE DEPENDENCIAS
    //    CartRepository es Singleton para persistencia en memoria.
    // ============================================================
    builder.Services.AddSingleton<CartRepository>();
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
    // ============================================================
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Cart.API está funcionando."));

    // ============================================================
    // 7. SWAGGER con comentarios XML
    // ============================================================
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Cart.API - ECommerce Microservices",
            Version = "v1",
            Description = "Microservicio de carrito de compras. Valida stock en tiempo real consultando Products.API."
        });

        string rutaXml = Path.Combine(AppContext.BaseDirectory, "Cart.API.xml");
        if (File.Exists(rutaXml))
        {
            options.IncludeXmlComments(rutaXml);
        }
    });

    var app = builder.Build();

    // ============================================================
    // 8. PIPELINE DE MIDDLEWARES
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart.API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapControllers();

    // Health Checks estructurados en JSON
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = check => false,
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
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
