using Counter;
using ReactiveMachine;
using System;

namespace Counter.Tests
{
    public class CounterTestsService : IService
    {
        public void Build(IServiceBuilder builder)
        {
            builder.Build<CounterService>();
            builder.ScanThisDLL();
        }
    }
}