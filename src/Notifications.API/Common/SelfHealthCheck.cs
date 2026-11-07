using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notifications.API.Common;

/// <summary>
/// Health check "self": si este código se ejecuta, el servicio está levantado.
/// Se usa en /health y /health/ready.
/// </summary>
public class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        HealthCheckResult resultado = HealthCheckResult.Healthy("Notifications.API está funcionando.");
        return Task.FromResult(resultado);
    }
}
