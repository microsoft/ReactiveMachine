// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using ReactiveMachine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Harness
{
    [DataContract]
    public class GeneratorState :
        IPartitionedState<IGeneratorAffinity, uint>
    {
        [DataMember]
        public bool ExperimentIsFinished;
    }
}
