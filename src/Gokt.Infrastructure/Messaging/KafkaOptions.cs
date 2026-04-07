namespace Gokt.Infrastructure.Messaging;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "gokt-matching-worker";
    public bool EnableIdempotence { get; set; } = true;
    public int MessageTimeoutMs { get; set; } = 5000;
}
