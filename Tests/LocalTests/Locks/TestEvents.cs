// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests.Locks
{
    public class TestEvents : TestOrchestration
    {
        public IEnumerable<Places> AllPlaces()
        {
            yield return Places.A;
            yield return Places.B;
            yield return Places.C;
        }


        protected override async Task Run(IOrchestrationContext context)
        {
            int val;

            // simple one-target events

            foreach (var x in AllPlaces())
            {
                await Task.WhenAll(
                    context.PerformEvent(new IncrementEvent() { Places = new Places[] { x } }),
                    context.PerformEvent(new IncrementEvent() { Places = new Places[] { x } })
                );

                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(2, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

            // simple two-target events

            await Task.WhenAll(
                   context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A, Places.B } }),
                   context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B, Places.C } }),
                   context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C, Places.A } })
               );

            foreach (var x in AllPlaces())
            {
                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(2, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

            // many conflicting events

            await Task.WhenAll(
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A, Places.B } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B, Places.C } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C, Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A, Places.B, Places.C } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B, Places.C, Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C, Places.B, Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B, Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C, Places.B } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A, Places.C } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.A } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.B } }),
                context.PerformEvent(new IncrementEvent() { Places = new Places[] { Places.C } })
              );

            foreach (var x in AllPlaces())
            {
                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(9, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

            // same but randomized
            await Task.WhenAll(
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.B, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.C, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.B, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C } } })
            );

            foreach (var x in AllPlaces())
            {
                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(9, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

            // now mix with locks
            await Task.WhenAll(
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.B, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.C, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.B, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B, Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C, Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A, Places.C } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.A } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.B } } }),
                context.PerformOrchestration(new RandomizeEvent() { Event = new IncrementEvent() { Places = new Places[] { Places.C } } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.A } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.B } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.C } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.A } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.B } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.C } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.A } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.B } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.C } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.A } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.B } }),
                context.PerformOrchestration(new RandomizeOrchestration() { Request = new GoodReadModifyWriteIncrement1() { Place = Places.C } })
            );

            foreach (var x in AllPlaces())
            {
                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(13, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

        }
    }
}
