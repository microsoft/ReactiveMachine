// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    public interface ITelemetryListener
    {
        void OnApplicationEvent(uint processId, string id, string name, string parent, OperationSide opSide, OperationType opType, double duration);
    }

    public enum OperationSide
    {
        Caller,
        Callee,
        Fork,
    }

    public enum OperationType
    {
       Orchestration,
       Lock,
       Local,       
       Activity,
       Finish,
       Event,
       Host
    }

   
}
