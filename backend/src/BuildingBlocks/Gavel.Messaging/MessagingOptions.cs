using System.ComponentModel.DataAnnotations;

namespace Gavel.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string ConnectionStringName = "rabbitmq";

    [Required]
    public string Exchange { get; set; } = "gavel.events";

    [Required]
    public string ServiceName { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int PublishBatchSize { get; set; } = 100;

    [Range(50, 60_000)]
    public int OutboxPollingMilliseconds { get; set; } = 1000;

    [Range(1, 1000)]
    public ushort PrefetchCount { get; set; } = 16;

    [Range(1, 100)]
    public int MaxDeliveryAttempts { get; set; } = 5;

    [Range(1, 365)]
    public int RetentionDays { get; set; } = 7;

    public string QueueName => $"{Exchange}.{ServiceName}";

    public string DeadLetterExchange => $"{Exchange}.dead-letter";

    public string DeadLetterQueueName => $"{QueueName}.dead-letter";
}
