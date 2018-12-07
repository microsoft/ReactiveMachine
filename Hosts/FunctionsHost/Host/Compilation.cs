using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsHost
{
    internal static class Compilation
    {
        internal static ICompiledApplication Compile<TApplicationInfo>(IStaticApplicationInfo info, FunctionsHostConfiguration configuration)
         where TApplicationInfo : IStaticApplicationInfo, new()
        {
            var compiler = new ReactiveMachine.ApplicationCompiler();
            compiler
                .SetConfiguration(configuration)
                .AddBuildStep(serviceBuilder =>
                {
                    var m = typeof(Compilation).GetMethod(nameof(Compilation.DefineForResultType));
                    foreach (var t in info.GetResultTypes())
                    {
                        var mg = m.MakeGenericMethod(typeof(TApplicationInfo), t);
                        mg.Invoke(null, new object[] { serviceBuilder });
                    }
                })
                ;
            return info.Build(compiler);
        }

        public static void DefineForResultType<TStaticApplicationInfo, TResult>(IServiceBuilder serviceBuilder)
            where TStaticApplicationInfo : IStaticApplicationInfo, new()
        {
            serviceBuilder
                   .DefineOrchestration<ClientRequestOrchestration<TStaticApplicationInfo, TResult>, UnitType>()
                   .DefineAtLeastOnceActivity<ClientRequestResponseNotification<TStaticApplicationInfo, TResult>, UnitType>()
                   .RegisterSerializableType(typeof(ResponseMessage<TResult>))
                   ;
        }
    }
}
