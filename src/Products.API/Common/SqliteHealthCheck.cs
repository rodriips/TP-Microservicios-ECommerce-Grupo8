using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.Common;

/// <summary>
/// Health check "sqlite": abre la base y ejecuta un SELECT 1 (igual que el ejemplo de la cátedra).
/// Si falla, /health y /health/ready devuelven Unhealthy (503).
/// </summary>
public class SqliteHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public SqliteHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        string? connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=products.db";
        }

        HealthCheckResult resultado;
        try
        {
            using (SqliteConnection conexion = new SqliteConnection(connectionString))
            {
                conexion.Open();
                conexion.ExecuteScalar<int>("SELECT 1");
            }
            resultado = HealthCheckResult.Healthy("La base SQLite responde.");
        }
        catch (Exception ex)
        {
            resultado = HealthCheckResult.Unhealthy("No se pudo conectar a la base SQLite.", ex);
        }

        return Task.FromResult(resultado);
    }
}
