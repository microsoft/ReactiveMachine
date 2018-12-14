using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.CodeUpdate
{
    [AddedInVersionAttribute(1)]
    public interface IAffinity2 : IPartitionedAffinity<IAffinity2, int>
    {
        int Key { get; set; }
    }
}
