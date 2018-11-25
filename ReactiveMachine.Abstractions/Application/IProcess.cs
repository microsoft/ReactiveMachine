// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// Abstraction of an application process. These are constructed using an <see cref="ICompiledApplication"/>}.
    /// </summary>
    public interface IProcess
    {
        /// <summary>
        /// Starts this process, from the initial state.
        /// </summary>
        void FirstStart();

        /// <summary>
        /// Resumes this process from a a previous snapshot.
        /// </summary>
        /// <param name="snapshot">the bytes representing the snapshot</param>
        /// <param name="label">the label of the snapshot, suitable for tracing</param>
        void Restore(byte[] snapshot, out string label);

        /// <summary>
        /// Called at the end of recovery, after replaying messages, when this process becomes the primary.
        /// </summary>
        void BecomePrimary();

        /// <summary>
        /// makes the process process a message.
        /// </summary>
        /// <param name="message"></param>
        void ProcessMessage(IMessage message);

        /// <summary>
        /// Creates a snapshot of the current state of this process.
        /// </summary>
        /// <param name="snapshot">the byte array representing the snapshot</param>
        /// <param name="label">a label for the snapshot, suitable for tracing</param>
        void SaveState(out byte[] snapshot, out string label);

        /// <summary>
        /// Checks if there are any pending activities.
        /// </summary>
        /// <returns>true if there are activities pending</returns>
        bool RequestsOutstanding();

        /// <summary>
        /// A unique identifier for this process.
        /// </summary>
        Guid InstanceId { get; }
    }
}
