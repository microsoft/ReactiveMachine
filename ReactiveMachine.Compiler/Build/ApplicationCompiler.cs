// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Process = ReactiveMachine.Compiler.Process;
using ReactiveMachine.Compiler;

namespace ReactiveMachine
{
    public class ApplicationCompiler : IApplicationCompiler, ICompiledApplication
    {
        private readonly List<Action<IServiceBuilder>> buildSteps = new List<Action<IServiceBuilder>>();
        
        public uint NumberProcesses { get; private set; }

        internal HostServices Host { get; } = new HostServices();

        public IHostServices HostServices => Host;

        public IEnumerable<Type> SerializableTypes => Host.SerializableTypes;

        public IReadOnlyDictionary<Type, object> Configurations { get; private set; }


        public IApplicationCompiler AddService<TService>()
            where TService : IServiceBuildDefinition,new()
        {
            buildSteps.Add((b) => b.BuildService<TService>());
            return this;
        }

        public IApplicationCompiler AddBuildStep(Action<IServiceBuilder> buildStep)
        {
            buildSteps.Add(buildStep);
            return this;
        }

        public IApplicationCompiler SetConfiguration<TConfiguration>(TConfiguration configuration)
        {
            buildSteps.Add(serviceBuilder => serviceBuilder.SetConfiguration(configuration));
            return this;
        }

        public IApplicationCompiler OverridePlacement(Action<IPlacementBuilder> placement)
        {
            buildSteps.Add(serviceBuilder => serviceBuilder.OverridePlacement(placement));
            return this;
        }

        public ICompiledApplication Compile(uint numberProcesses)
        {
            NumberProcesses = numberProcesses;

            buildSteps.Add(Extensions.Register.DefineInternalExtensions);

            // build (and then discard) a process for the sake of catching errors now
            var p = (Process)MakeProcess(0);
            Configurations = p.Configurations;

            return this;
        }

        public ApplicationCompiler()
        {
            // built-in definitions
            buildSteps.Add(Extensions.Register.DefineVisibleExtensions);
        }

        public IProcess MakeProcess(uint processId)
        {
            Process process = new Process(processId, Host);

            ServiceBuilder serviceBuilder = new ServiceBuilder(process);
            foreach(var b in buildSteps)
            {
                b(serviceBuilder);
            }

            process.DefineExtensions(serviceBuilder);

            PlacementBuilder placementBuilder = new PlacementBuilder(process, NumberProcesses);
            foreach (var p in serviceBuilder.Placements)
            {
                p(placementBuilder);
            }


            process.FinalizePlacement();
            process.Serializer = new Serializer(Host.SerializableTypes);
            process.DeepCopier = new DefaultDeepCopier(Host);
            process.Telemetry = Host.GetTelemetryListener();
            process.ConfigureLogging();
            process.ClearState();

            return process;
        }

    
    
    }
}
