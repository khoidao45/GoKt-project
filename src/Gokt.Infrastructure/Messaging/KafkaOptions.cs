namespace Gokt.Infrastructure.Messaging;

public class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "gokt-matching-worker";
}
