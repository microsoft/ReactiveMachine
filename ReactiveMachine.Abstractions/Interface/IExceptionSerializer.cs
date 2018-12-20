using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveMachine
{
    /// <summary>
    /// Used to provide assistance for explicitly serializing and deserializing exceptions at the application level
    /// </summary>
    public interface IExceptionSerializer
    {
        object SerializeException(Exception e);

        bool DeserializeException(object o, out Exception e);
    }
}
