// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionsHost
{
    /// <summary>
    /// Static information about the application that is used by the functions host
    /// </summary>
    public interface IStaticApplicationInfo
    {
        /// <summary>
        /// A function that constructs a deployment id string from a deployment timestamp
        /// This is used as a directory path by the azure blob telemetry utility
        /// </summary>
        /// <param name="deploymentTimestamp">The creation timestamp of the deployment</param>
        /// <returns></returns>
        string GetDeploymentId(DateTime deploymentTimestamp);

        /// <summary>
        /// Configuration parameters to be used for this deployment
        /// </summary>
        /// <returns></returns>
        FunctionsHostConfiguration GetHostConfiguration();

        /// <summary>
        /// A list of types that may be returned by requests, and that must
        /// therefore be serializable
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetResultTypes();

        /// <summary>
        /// The build recipe for creating the application
        /// </summary>
        /// <param name="compiler"></param>
        /// <returns></returns>
        ICompiledApplication Build(IApplicationCompiler compiler);
    }
}
