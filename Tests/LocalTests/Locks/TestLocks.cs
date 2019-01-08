// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LocalTests.Locks
{


    public class BadReadModifyWriteIncrement : IOrchestration<UnitType>
    {
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var read = await context.PerformRead(new GetBalance { Place = Places.B });

            await context.PerformUpdate(new SetBalance { Place = Places.B, NewBalance = read + 1 });

            return UnitType.Value;
        }
    }

    public class GoodReadModifyWriteIncrement1 : IOrchestration<UnitType>, IPlaceAffinity
    {
        public Places Place { get; set; }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var read = await context.PerformRead(new GetBalance { Place = Place });

            await context.PerformUpdate(new SetBalance { Place = Place, NewBalance = read + 1 });

            return UnitType.Value;
        }
    }

    public class GoodReadModifyWriteIncrement2 : IOrchestration<UnitType>, IMultiple<IPlaceAffinity, Places>
    {
        public IEnumerable<Places> DeclareAffinities()
        {
            yield return Places.A;
            yield return Places.B;
            yield return Places.C;
        }

        public Places Place { get; set; }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var read = await context.PerformRead(new GetBalance { Place = Place });

            await context.PerformUpdate(new SetBalance { Place = Place, NewBalance = read + 1 });

            return UnitType.Value;
        }
    }

    public class MoveTwo : IOrchestration<UnitType>, IMultiple<IPlaceAffinity, Places>
    {

        public IEnumerable<Places> DeclareAffinities()
        {
            yield return Places.A;
            yield return Places.B;
        }

        [Lock]
        public async Task<UnitType> Execute(IOrchestrationContext context)
        {
            var readA = context.PerformRead(new GetBalance { Place = Places.B });
            var readB = context.PerformRead(new GetBalance { Place = Places.B });

            await Task.WhenAll(
              context.PerformOrchestration(new BadReadModifyWriteIncrement { }),
              context.PerformOrchestration(new GoodReadModifyWriteIncrement1 { Place = Places.B })
            );

            return UnitType.Value;
        }
    }


    public class TestLocks : TestOrchestration
    {
        public IEnumerable<Places> AllPlaces()
        {
            yield return Places.A;
            yield return Places.B;
            yield return Places.C;
        }


        protected override async Task Run(IOrchestrationContext context)
        {
            // nonsafe version: increments will interleave, final result is 1

            await Task.WhenAll(
                context.PerformOrchestration(new BadReadModifyWriteIncrement()),
                context.PerformOrchestration(new BadReadModifyWriteIncrement())
            );

            var val = await context.PerformRead(new GetBalance() { Place = Places.B });

            Assert.Equal(1, val);
            await context.PerformUpdate(new SetBalance() { Place = Places.B, NewBalance = 0 });

            // safe version: increments do not interleave

            foreach (var x in AllPlaces())
            {
                await Task.WhenAll(
                    context.PerformOrchestration(new GoodReadModifyWriteIncrement1() { Place = x }),
                    context.PerformOrchestration(new GoodReadModifyWriteIncrement1() { Place = x })
                );

                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(2, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });



            }

            //safe version: increments do not interleave

            foreach (var x in AllPlaces())
            {
                await Task.WhenAll(
                    context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = x }),
                    context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = x })
                );

                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(2, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }

            //safe version: increments do not interleave

            await Task.WhenAll(
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.A }),
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.B }),
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.C }),
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.A }),
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.B }),
                context.PerformOrchestration(new GoodReadModifyWriteIncrement2() { Place = Places.C }),
                context.PerformUpdate(new IncrementBalance() { Place = Places.A }),
                context.PerformUpdate(new IncrementBalance() { Place = Places.B }),
                context.PerformUpdate(new IncrementBalance() { Place = Places.C })
             );

            foreach (var x in AllPlaces())
            {
                val = await context.PerformRead(new GetBalance() { Place = x });
                Assert.Equal(3, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }


            //safe version: increments do not interleave

            await Task.WhenAll(
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
                Assert.Equal(4, val);
                await context.PerformUpdate(new SetBalance() { Place = x, NewBalance = 0 });
            }
        }
    }
}
