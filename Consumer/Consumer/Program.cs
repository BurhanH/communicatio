using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Consumer;

using Confluent.Kafka;

class Program
{
    public static void Main(string[] args)
    {
        Action<ResourceBuilder> configureResource = r => r.AddService(
            serviceName: "Communicatio-Consumer",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);
        
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(configureResource)
            .AddConsoleExporter()
            .AddMeter("Consumer")
            .Build();
        
        var meter = new Meter("Consumer");
        var pCounter = meter.CreateCounter<int>("consumer.messages.received");
        
        var conf = new ConsumerConfig
        {
            GroupId = "test-consumer-group",
            BootstrapServers = "redpanda:29092", // replace to localhost:9092 for local testing
            // Note: The AutoOffsetReset property determines the start offset in the event
            // there are not yet any committed offsets for the consumer group for the
            // topic/partitions of interest. By default, offsets are committed
            // automatically, so in this example, consumption will only start from the
            // earliest message in the topic 'my-topic' the first time you run the program.
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using (var c = new ConsumerBuilder<Ignore, string>(conf).Build())
        {
            c.Subscribe("test_topic");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };

            try
            {
                while (true)
                {
                    try
                    {
                        var cr = c.Consume(cts.Token);
                        Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        
                        pCounter.Add(1);
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Error occured: {e.Error.Reason}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                c.Close();
            }
        }
        
        meterProvider.ForceFlush();
    }
}