using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orders.API.Common;

/// <summary>Chequeo de salud "self": si la API puede responder, está sana.</summary>
public class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        HealthCheckResult resultado = HealthCheckResult.Healthy("Orders.API está funcionando.");
        return Task.FromResult(resultado);
    }
}
