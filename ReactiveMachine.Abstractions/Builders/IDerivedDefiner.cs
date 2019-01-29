using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public interface IDerivedDefiner
    {
        void DefineForEachOrchestration<TRequest, TResponse>(IServiceBuilder serviceBuilder);
    }
}
