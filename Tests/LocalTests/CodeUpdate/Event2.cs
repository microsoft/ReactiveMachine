using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.CodeUpdate
{
    [AddedInVersionAttribute(1)]
    public class Event2 : IEvent, IAffinity1
    {

    }
}
