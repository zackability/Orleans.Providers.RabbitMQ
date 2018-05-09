using System.Threading.Tasks;
using Orleans.Providers.RabbitMQ.Tests.Host.Interfaces;
using System;
using Orleans.Streams;
using Orleans.Runtime;
using Microsoft.Extensions.Logging;

namespace Orleans.Providers.RabbitMQ.Tests.Host.Grains
{
    [ImplicitStreamSubscription("TestNamespace")]
    public class ImplicitGrain : Grain, IImplicitGrain, IAsyncObserver<string>
    {
        private readonly ILogger<ImplicitGrain> logger;
        private StreamSubscriptionHandle<string> _subscription;

        public ImplicitGrain(ILogger<ImplicitGrain> logger)
        {
            this.logger = logger;
        }

        public async override Task OnActivateAsync()
        {
            var provider = GetStreamProvider("Default");
            var stream = provider.GetStream<string>(this.GetPrimaryKey(), "TestNamespace");
            _subscription = await stream.SubscribeAsync(this);
        }

        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }

        public Task OnNextAsync(string item, StreamSequenceToken token = null)
        {
            logger.Info("<<<Received message '{0}'!", item);
            return Task.CompletedTask;
        }
    }
}
