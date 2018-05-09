using System;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.RabbitMQ.Streams;

namespace Orleans.Providers.RabbitMQ.Hosting
{
    public static class SiloBuilderExtensions
    {

        /// <summary>
        /// Configure silo to use azure queue persistent streams. 
        /// </summary>
        public static ISiloHostBuilder AddRabbitMQStreams<TDataAdapter>(this ISiloHostBuilder builder, string name, Action<SiloRabbitMQStreamConfigurator<TDataAdapter>> configure)
           where TDataAdapter : IRabbitMQMapper
        {
            var configurator = new SiloRabbitMQStreamConfigurator<TDataAdapter>(name, builder);
            configure?.Invoke(configurator);
            return builder;
        }

        /// <summary>
        /// Configure silo to use azure queue persistent streams with default settings
        /// </summary>
        public static ISiloHostBuilder AddRabbitMQStreams<TDataAdapter>(this ISiloHostBuilder builder, string name, Action<OptionsBuilder<RabbitMQStreamProviderOptions>> configureOptions)
           where TDataAdapter : IRabbitMQMapper
        {
            builder.AddRabbitMQStreams<TDataAdapter>(name, b =>
                 b.ConfigureRabbitMQ(configureOptions));
            return builder;
        }
    }
}
