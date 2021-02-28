using System;
using System.IO;
using EasyNetQ;
using EasyNetQ.Topology;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace RmqClient
{
    internal class Program
    {
        private static async System.Threading.Tasks.Task Main(string[] args)
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
                    services.AddTransient<IRmqClient, RmqClient>();
                })
                .UseSerilog()
                .Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var rmqClient = serviceScope.ServiceProvider.GetService<IRmqClient>();

                try
                {
                    var advancedBus = Configuration.Bus.Advanced;
                    var exchange =
                        await advancedBus.ExchangeDeclareAsync(Configuration.ExchangeName, ExchangeType.Direct);
                    var responseQueue = await advancedBus.QueueDeclareAsync(Configuration.ResponseQueueName, config =>
                    {
                        config.AsAutoDelete(false)
                            .AsDurable(true)
                            .AsExclusive(false)
                            .WithArgument("expires", Configuration.ExpiryTime)
                            .WithArgument("perQueueMessageTtl", Configuration.ExpiryTime);
                    });
                    advancedBus.Bind(exchange, responseQueue, Configuration.ResponseRoutingKey);

                    Console.WriteLine("Sending requests using Advanced API");
                    await rmqClient?.SendRequests(advancedBus, responseQueue, exchange);
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
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",true)
                .AddEnvironmentVariables();
        }
    }
}