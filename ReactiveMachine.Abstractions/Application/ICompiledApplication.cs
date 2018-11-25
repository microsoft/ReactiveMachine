// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// An application that was compiled into a deterministic state machine.
    /// Is the output of <see cref="IApplicationCompiler"/>}.
    /// Can be used to create stateful processes.
    /// </summary>
    public interface ICompiledApplication
    {
        /// <summary>
        ///  indicates the number of processes in the application
        /// </summary>
        uint NumberProcesses { get; }

        /// <summary>
        /// creates a blank process object
        /// </summary>
        IProcess MakeProcess(uint processId);

        /// <summary>
        /// encapsulates host services
        /// </summary>
        IHostServices HostServices { get; }

        /// <summary>
        /// lists all registered serializable classes
        /// </summary>
        IEnumerable<Type> SerializableTypes { get; }

        /// <summary>
        /// lists all configuration objects, organized by type
        /// </summary>
        IReadOnlyDictionary<Type, object> Configurations { get; }
    }
}
