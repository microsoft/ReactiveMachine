using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsHost
{
    internal static class Compilation
    {
        public static ICompiledApplication Compile<TStaticApplicationInfo>(TStaticApplicationInfo info, FunctionsHostConfiguration configuration)
                 where TStaticApplicationInfo : IStaticApplicationInfo, new()
        {
            var compiler = new Compiler<TStaticApplicationInfo>();
            compiler.SetConfiguration(configuration);
            return info.Build(compiler);
        }

        internal class Compiler<TStaticApplicationInfo> : ApplicationCompiler, IDerivedDefiner
                 where TStaticApplicationInfo : IStaticApplicationInfo, new()
        { 
            public override ICompiledApplication Compile(uint numberProcesses)
            {
                AddBuildStep(serviceBuilder => serviceBuilder.DefineDerived(this));
                return base.Compile(numberProcesses);
            }

            public void DefineForEachOrchestration<TRequest, TResult>(IServiceBuilder serviceBuilder)
            {
                serviceBuilder
                       .DefineOrchestration<ClientRequestOrchestration<TStaticApplicationInfo, TResult>, UnitType>()
                       .DefineActivity<ClientRequestResponseNotification<TStaticApplicationInfo, TResult>, UnitType>()
                       .RegisterSerializableType(typeof(ResponseMessage<TResult>))
                       ;
            }
        }
    }
}