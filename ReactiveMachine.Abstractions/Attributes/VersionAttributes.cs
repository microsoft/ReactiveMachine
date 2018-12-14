using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{


  

    /// <summary>
    /// Placed on entities that have been replaced by a code update.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public class ReplacedInVersionAttribute : System.Attribute
    {
        public ReplacedInVersionAttribute(int version)
        {
            this.Version = version;
        }

        public int Version;
    }

    /// <summary>
    /// Placed on entities that have been added in a code update.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public class AddedInVersionAttributeAttribute : System.Attribute
    {
        public AddedInVersionAttributeAttribute(int version)
        {
            this.Version = version;
        }

        public int Version;
    }

   
}
