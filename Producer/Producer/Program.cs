using System.Diagnostics.Metrics;
using OpenTelemetry;
using OpenTelemetry.Exporter.InfluxDB;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Producer;

using System;
using Confluent.Kafka;

class Program
{
    public static void Main(string[] args)
    {
        /*Action<ResourceBuilder> configureResource = r => r.AddService(
            serviceName: "influx-exporter-test",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);
        
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(configureResource)
            .AddInfluxDBMetricsExporter(options =>
            {
                options.Org = "communicatio";
                options.Bucket = "communicatio-bucket";
                options.Token = "DOCKER_INFLUXDB_INIT_ADMIN_TOKEN";
                options.Endpoint = new Uri("http://influxdb:8086");
                options.MetricsSchema = MetricsSchema.TelegrafPrometheusV2;
            })
            .AddMeter("Producer")
            .Build();
        */

        Action<ResourceBuilder> configureResource = r => r.AddService(
            serviceName: "Communicatio-Producer",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
            serviceInstanceId: Environment.MachineName);
        
        var meterProvider = Sdk.CreateMeterProviderBuilder()
            .ConfigureResource(configureResource)
            .AddConsoleExporter()
            .AddMeter("Producer")
            .Build();
        
        var meter = new Meter("Producer");
        var pCounter = meter.CreateCounter<int>("producer.messages.sent");
        
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
                
                pCounter.Add(1);
                
                Thread.Sleep(100);
            }
            // wait for up to 10 seconds for any inflight messages to be delivered.
            p.Flush(TimeSpan.FromSeconds(10));
        }

        meterProvider.ForceFlush();
    }
}