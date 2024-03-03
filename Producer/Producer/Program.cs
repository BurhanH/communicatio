namespace Producer;

using System;
using Confluent.Kafka;

class Program
{
    public static void Main(string[] args)
    {
        var conf = new ProducerConfig { BootstrapServers = "redpanda:29092" }; // replace to localhost:9092 for local testing

        Action<DeliveryReport<Null, string>> handler = r =>
            Console.WriteLine(!r.Error.IsError
                ? $"Delivered message to {r.TopicPartitionOffset}"
                : $"Delivery Error: {r.Error.Reason}");

        using (var p = new ProducerBuilder<Null, string>(conf).Build())
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // prevent the process from terminating.
                cts.Cancel();
            };
            
            while (true)
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }
                
                p.Produce("test_topic", new Message<Null, string> { Value = DateTime.UtcNow.Microsecond.ToString() }, handler);
                
                Thread.Sleep(100);
            }
            // wait for up to 10 seconds for any inflight messages to be delivered.
            p.Flush(TimeSpan.FromSeconds(10));
        }
    }
}