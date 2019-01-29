// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveMachine
{
    public static class ContextExtensions
    {
        private static void CheckTimeArgument(TimeSpan delay)
        {
            if (delay.Ticks < 0)
                throw new System.ArgumentOutOfRangeException(nameof(delay), "The delay cannot be negative");
            if (delay.TotalMilliseconds > int.MaxValue)
                throw new System.ArgumentOutOfRangeException(nameof(delay), "The delay cannot exceed total milliseconds larger than Int32.MaxValue");
        }

        public static void ScheduleOrchestration<TReturn>(this IContextWithForks context, TimeSpan delay, IOrchestration<TReturn> orchestration)
        {
            CheckTimeArgument(delay);
            context.ForkOrchestration(new Extensions.ForkedOrchestration<TReturn>()
            {
                Delay = delay,
                Orchestration = orchestration
            });
        }

        public static void ScheduleLocalUpdate<TState, TReturn>(this IContextWithForks context, TimeSpan delay, IUpdate<TState, TReturn> update)
            where TState: IState
        {
            CheckTimeArgument(delay);
            context.ForkOrchestration(new Extensions.ForkedUpdate<TState, TReturn>()
            {
                Delay = delay,
                Update = update
            });
        }

        public static void ScheduleEvent<TEvent>(this IContextWithForks context, TimeSpan delay, IEvent evt)
        {
            CheckTimeArgument(delay);
            context.ForkOrchestration(new Extensions.ForkedEvent()
            {
                Delay = delay,
                Event = evt
            });
        }

        public static void ScheduleActivity<TReturn>(this IContextWithForks context, TimeSpan delay, IActivity<TReturn> activity)
        {
            CheckTimeArgument(delay);
            context.ForkOrchestration(new Extensions.ForkedActivity<TReturn>()
            {
                Delay = delay,
                Activity = activity
            });
        }

        public static async Task DelayBy(this IOrchestrationContext context, TimeSpan delay)
        {
            CheckTimeArgument(delay);
            if (delay != TimeSpan.Zero)
            {
                var currentTime = await context.ReadDateTimeUtcNow();
                await context.PerformActivity(new Extensions.StableDelay()
                {
                    TargetTime = currentTime + delay,
                });
            }
        }

        public static async Task DelayUntil(this IOrchestrationContext context, DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
                throw new ArgumentException("must use UTC time", nameof(utcTime));
            if ((utcTime - DateTime.UtcNow).TotalMilliseconds > int.MaxValue)
                throw new System.ArgumentOutOfRangeException(nameof(utcTime), "The scheduled time cannot be later than Int32.MaxValue milliseconds from now");

            await context.PerformActivity(new Extensions.StableDelay()
            {
                TargetTime = utcTime,
            });

        }

    }
}
