using System;
using System.Threading.Tasks;
using Orleans.Providers.RabbitMQ.Tests.Host.Interfaces;
using Orleans.Streams;
using Orleans.Runtime;
using Microsoft.Extensions.Logging;

namespace Orleans.Providers.RabbitMQ.Tests.Host.Grains
{
    public class ProducerGrain : Grain, IProducerGrain
    {
        private readonly ILogger<ProducerGrain> logger;
        private int _counter;
        private IAsyncStream<string> _stream;

        public ProducerGrain(ILogger<ProducerGrain> logger)
        {
            this.logger = logger;
        }

        public override Task OnActivateAsync()
        {
            var provider = GetStreamProvider("Default");
            _stream = provider.GetStream<string>(this.GetPrimaryKey(), "TestNamespace");
            return Task.CompletedTask;
        }

        public Task Simulate()
        {
            RegisterTimer(OnSimulationTick, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
            return Task.CompletedTask;
        }

        public async Task Tick()
        {
            await OnSimulationTick(null);
        }

        private async Task OnSimulationTick(object state)
        {
            await SendMessages(_counter++.ToString());
            await SendMessages(_counter++.ToString(), _counter++.ToString());
        }

        private async Task SendMessages(params string[] messages)
        {
            logger.Info(">>>Sending message{0} '{1}'...",
                messages.Length > 1 ? "s" : "", string.Join(",", messages));
            
            if (messages.Length == 1)
            { 
                await _stream.OnNextAsync(messages[0]);
                return;
            }
            await _stream.OnNextBatchAsync(messages);
        }
    }
}
