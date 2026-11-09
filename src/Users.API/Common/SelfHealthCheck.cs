using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.API.Common;

/// <summary>
/// Check "self": confirma que el servicio está levantado y respondiendo.
/// </summary>
public class SelfHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        HealthCheckResult resultado = HealthCheckResult.Healthy("Users.API está funcionando.");
        return Task.FromResult(resultado);
    }
}
