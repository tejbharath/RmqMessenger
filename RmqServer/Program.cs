using System;
using System.IO;
using EasyNetQ;
using EasyNetQ.Topology;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace RmqServer
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            BuildConfig(builder);
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Build())
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information($"Application Starting");

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IRmqServer, RmqServer>();
                })
                .UseSerilog()
                .Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var rmqServer = serviceScope.ServiceProvider.GetService<IRmqServer>();

                try
                {
                    var advancedBus = Configuration.Bus.Advanced;
                    var exchange = await advancedBus.ExchangeDeclareAsync(Configuration.ExchangeName, ExchangeType.Direct);
                    var responseQueue = await advancedBus.QueueDeclareAsync(Configuration.RequestQueueName, config =>
                    {
                        config.AsAutoDelete(false)
                            .AsDurable(true)
                            .AsExclusive(false)
                            .WithArgument("expires", Configuration.ExpiryTime)
                            .WithArgument("perQueueMessageTtl", Configuration.ExpiryTime);
                    });
                    advancedBus.Bind(exchange, responseQueue, Configuration.RequestRoutingKey);

                    Console.WriteLine("Responding requests using Advanced API");
                    rmqServer?.RespondRequests(responseQueue, advancedBus, exchange);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        private static void BuildConfig(IConfigurationBuilder builder)
        {
            builder.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
                .AddEnvironmentVariables();
        }
    }
}
