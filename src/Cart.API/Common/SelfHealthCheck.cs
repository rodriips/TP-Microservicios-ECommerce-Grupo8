using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cart.API.Common;

/// <summary>
/// Check "self": confirma que el servicio está levantado y respondiendo.
/// </summary>
public class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        HealthCheckResult resultado = HealthCheckResult.Healthy("Cart.API está funcionando.");
        return Task.FromResult(resultado);
    }
}
