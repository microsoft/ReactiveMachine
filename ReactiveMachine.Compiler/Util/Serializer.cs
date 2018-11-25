// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace ReactiveMachine.Compiler
{
    internal class Serializer
    {
        private readonly DataContractSerializer snapshotSerializer;
        private readonly DataContractSerializer exceptionSerializer;
        private readonly DataContractSerializer objectSerializer;

        public Serializer(IEnumerable<Type> classes)
        {
            snapshotSerializer = new DataContractSerializer(typeof(Snapshot), classes);
            exceptionSerializer = new DataContractSerializer(typeof(Exception), classes);
            objectSerializer = new DataContractSerializer(typeof(Object), classes);
        }

        public byte[] SerializeSnapshot(Snapshot snapshot)
        {
            var stream = new MemoryStream();
            snapshotSerializer.WriteObject(stream, snapshot);
            return stream.ToArray();
        }

        public Snapshot DeserializeSnapshot(byte[] snapshot)
        {
            var stream = new MemoryStream(snapshot);
            return (Snapshot)snapshotSerializer.ReadObject(stream);
        }
        public byte[] SerializeObject(Object o)
        {
            var stream = new MemoryStream();
            snapshotSerializer.WriteObject(stream, o);
            return stream.ToArray();
        }

        public Object DeserializeObject(byte[] snapshot)
        {
            var stream = new MemoryStream(snapshot);
            return snapshotSerializer.ReadObject(stream);
        }

        public object SerializeException(Exception e)
        {
            // does best-effort on exception serialization
            // if exception cannot be serialized successfully, we will serialize
            // just the description string
            try
            {
                var stream = new MemoryStream();
                exceptionSerializer.WriteObject(stream, e);
                return new DataContractSerializedExceptionResult() { Content = stream.ToArray() };
            }
            catch
            {
            }

            try
            {
                var stream = new MemoryStream();
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, e);
                stream.Flush();
                return new ClassicallySerializedExceptionResult() { Content = stream.ToArray() };
            }
            catch
            {
            }

            return new NonserializedExceptionResult { Description = e.ToString() }; 
        }

        public bool DeserializeException(Object o, out Exception e)
        {
            if (o == null || !(o is ExceptionResult er))
            {
                e = null;
                return false;
            }
            else
            {
                if (o is DataContractSerializedExceptionResult dse)
                {
                    try
                    {
                        var stream = new MemoryStream(dse.Content);
                        e = (Exception)exceptionSerializer.ReadObject(stream);
                        return true;
                    }
                    catch
                    {
                    }
                }
                else if (o is ClassicallySerializedExceptionResult cse)
                {
                    try
                    {
                        var stream = new MemoryStream(cse.Content);
                        IFormatter formatter = new BinaryFormatter();
                        e = (Exception)formatter.Deserialize(stream);
                        return true;
                    }
                    catch
                    {
                    }
                }

                e = new ApplicationException($"the application threw a non-serializable exception: {er.Description}");
                return true;
            }
        }
    }


    [Serializable]
    public class ApplicationException : Exception
    {
        public ApplicationException(string msg) : base(msg) { }
        public ApplicationException(string msg, Exception inner) : base(msg, inner) { }
    }
}
