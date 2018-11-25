// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace ReactiveMachine.Compiler
{

    internal class DefaultDeepCopier : IDeepCopier
    {
        private readonly IHostServices hostServices;
        private readonly Dictionary<Type, DataContractSerializer> byType;

        public DefaultDeepCopier(IHostServices hostServices)
        {
            this.hostServices = hostServices;
            byType = new Dictionary<Type, DataContractSerializer>();
        }

        private void GetSerializerForType(Type type, out DataContractSerializer serializer)
        {
            if (!byType.TryGetValue(type, out serializer))
            {
                byType[type] = serializer = new DataContractSerializer(type, hostServices.SerializableTypes);
            }
        }

        public T DeepCopy<T>(T other)
        {
            var type = typeof(T);

            if (IsSimple(type))
                return other;

            GetSerializerForType(type, out var serializer);

            var inStream = new MemoryStream();

            using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(inStream))
            {
                serializer.WriteObject(binaryDictionaryWriter, other);
                inStream.Flush();
            }

            var outStream = new MemoryStream(inStream.ToArray());

            using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(outStream, XmlDictionaryReaderQuotas.Max))
            {
                return (T)serializer.ReadObject(binaryDictionaryReader);
            }
        }

        bool IsSimple(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

    }
}
