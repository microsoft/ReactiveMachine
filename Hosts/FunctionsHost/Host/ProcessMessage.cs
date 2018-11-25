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
    public class ProcessMessage
    {
        [DataMember]
        public DateTime DeploymentTimestamp;

        [DataMember]
        public uint Source;

        [DataMember]
        public long? LastClock;

        [DataMember]
        public byte[] Payload;

        private static DataContractSerializer Serializer = new DataContractSerializer(typeof(ProcessMessage));

        public static ProcessMessage Deserialize(byte[] messageBytes)
        {
            MemoryStream stream = new MemoryStream(messageBytes);
            using (var binaryDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
            {
                return (ProcessMessage)Serializer.ReadObject(binaryDictionaryReader);
            }
        }

        public static byte[] Serialize(ProcessMessage message)
        {
            MemoryStream stream = new MemoryStream();
            using (var binaryDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                Serializer.WriteObject(binaryDictionaryWriter, message);
                stream.Flush();
            }
            return stream.ToArray();
        }

        public override string ToString()
        {
            return LastClock.HasValue ? $"Sequenced p{Source:D3}.{LastClock}" : $"Loopback";
        }
    }
   
}
