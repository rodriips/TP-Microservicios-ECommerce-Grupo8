using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Orders.API.Common;
using Orders.API.Data;
using Orders.API.ExceptionHandlers;
using Orders.API.Middleware;
using Orders.API.Services;
using Serilog;
using Serilog.Formatting.Json;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Servicio", "Orders.API")
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Servicio}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(new JsonFormatter(), "logs/orders-api-.json", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando Orders.API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Controllers + validación manual (ValidarDatos arma ORD-002 en vez de dejar que
    // [ApiController] devuelva un 400 automático sin nuestro formato de error).
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    builder.Services.AddSingleton<OrderRepository>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddHttpContextAccessor();

    // Clientes HTTP hacia los otros microservicios. Las URLs se leen de
    // appsettings.json (sección "Servicios"); si no están, usan estos puertos por defecto.
    builder.Services.AddHttpClient("UsersApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Servicios:UsersApi"] ?? "http://localhost:5002");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    builder.Services.AddHttpClient("ProductsApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Servicios:ProductsApi"] ?? "http://localhost:5001");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

    // IExceptionHandler, del más específico al más genérico. GlobalExceptionHandler
    // va siempre último: es la red de seguridad.
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<UnprocessableEntityExceptionHandler>();
    builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Orders.API está funcionando."));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Orders.API - ECommerce Microservices",
            Version = "v1",
            Description = "Gestión de órdenes de compra. Valida usuario y stock contra Users.API y Products.API."
        });

        var xmlFile = Path.Combine(AppContext.BaseDirectory, "Orders.API.xml");
        if (File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }
    });

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseSwagger(options => options.SerializeAsV2 = true);
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders.API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapControllers();

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
    Log.Fatal(ex, "Orders.API no pudo iniciar");
}
finally
{
    Log.CloseAndFlush();
}
