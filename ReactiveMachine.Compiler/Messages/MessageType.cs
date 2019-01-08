// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine.Compiler
{

    internal enum MessageType
    {
        None,

        ForkOrchestration,
        RequestOrchestration,
        RespondToOrchestration,

        ForkLocal,
        RequestLocal,
        RespondToLocal,

        ForkEvent,
        PerformEvent,
        AckEvent,
        CommitEvent,

        AcquireLock,
        GrantLock,
        ReleaseLock,

        RequestExternal,
        RespondToActivity,

        RequestFinish,
        AckFinish,

        RequestPing,
        RespondToPing,

        AckInitialization,

        EnqueueStartup,
        ExternalRequest
    }

    internal static class MessageTypeExtensions
    {
        public static bool IsResponse(this MessageType type)
        {
            switch (type)
            {
                case MessageType.RespondToOrchestration:
                case MessageType.RespondToActivity:
                case MessageType.RespondToLocal:
                case MessageType.AckEvent:
                case MessageType.AckFinish:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFork(this MessageType type)
        {
            switch (type)
            {
                case MessageType.ForkOrchestration:
                case MessageType.ForkLocal:
                case MessageType.ForkEvent:
                    return true;
                default:
                    return false;
            }
        }

    }


}
