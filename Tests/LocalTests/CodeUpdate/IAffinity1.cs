using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocalTests.CodeUpdate
{
    public interface IAffinity1 : ISingletonAffinity<IAffinity1>
    {
         
    }
}
