// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Azure.EventHubs;

namespace FunctionsHost
{

    [DataContract]
    public class DoorbellMessage
    {
        [DataMember]
        public uint ProcessId;

        [DataMember]
        public Guid Guid;

        public override string ToString()
        {
            return $"doorbell{ProcessId}.{Guid}";
        }

        private static DataContractSerializer Serializer = new DataContractSerializer(typeof(DoorbellMessage));

        public static DoorbellMessage Deserialize(byte[] messageBytes)
        {
            MemoryStream stream = new MemoryStream(messageBytes);
            using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return (DoorbellMessage)Serializer.ReadObject(binaryDictionaryReader);
            }
        }

        public static byte[] Serialize(DoorbellMessage message)
        {
            MemoryStream stream = new MemoryStream();
            using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                Serializer.WriteObject(binaryDictionaryWriter, message);
                stream.Flush();
            }
            return stream.ToArray();
        }
    }

}
