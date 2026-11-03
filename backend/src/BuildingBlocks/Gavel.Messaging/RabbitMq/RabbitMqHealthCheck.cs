using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Gavel.Messaging.RabbitMq;

internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var amqp = await connection.GetAsync(cancellationToken);
            return amqp.IsOpen ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("The RabbitMQ connection is closed.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", exception);
        }
    }
}
