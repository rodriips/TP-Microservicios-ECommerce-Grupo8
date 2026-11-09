using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
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

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Controllers + validación manual (ValidarDatos arma ORD-002 en vez de dejar que
    // [ApiController] devuelva un 400 automático sin nuestro formato de error).
    builder.Services.AddControllers();
    builder.Services.Configure<ApiBehaviorOptions>(Configuracion.ConfigurarValidacion);

    builder.Services.AddSingleton<OrderRepository>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddHttpContextAccessor();

    // Clientes HTTP hacia los otros microservicios. La URL y el timeout
    // se configuran en OrderService, al crear cada cliente.
    builder.Services.AddHttpClient("UsersApi");
    builder.Services.AddHttpClient("ProductsApi");

    // IExceptionHandler, del más específico al más genérico. GlobalExceptionHandler
    // va siempre último: es la red de seguridad.
    builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
    builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
    builder.Services.AddExceptionHandler<UnprocessableEntityExceptionHandler>();
    builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services.AddHealthChecks()
        .AddCheck<SelfHealthCheck>("self");

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(Configuracion.ConfigurarSwagger);

    WebApplication app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();

    app.UseSwagger(Configuracion.ConfigurarSwaggerJson);
    app.UseSwaggerUI(Configuracion.ConfigurarSwaggerUI);

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
        Predicate = Configuracion.NingunCheck,
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
