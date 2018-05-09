using Orleans.Providers.RabbitMQ.Tests.Host.Interfaces;
using Orleans.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Orleans.Providers.RabbitMQ.Tests.Host.StartupTasks
{
    internal class RabbitMQTestStartupTask : IStartupTask
    {
        private readonly IGrainFactory grainFactory;

        public RabbitMQTestStartupTask(IGrainFactory grainFactory)
        {
            this.grainFactory = grainFactory;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            var producer = grainFactory.GetGrain<IProducerGrain>(Guid.Empty);
            await producer.Simulate();
        }
    }
}