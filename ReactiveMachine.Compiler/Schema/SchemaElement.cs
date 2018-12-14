using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveMachine.Compiler
{

    internal interface ISchemaElement
    {
        string TypeName { get; }

        int AddedInVersion { get; }

        int? ReplacedInVersion { get; }
    }

    internal abstract class SchemaElement<TType> : ISchemaElement
    {
        public string TypeName => typeof(TType).FullName; 

        public int AddedInVersion { get; protected set; }

        public int? ReplacedInVersion { get; protected set; }

        public abstract bool AllowVersionReplace { get; }

        public SchemaElement()
        {
            var addedInVersion = (AddedInVersionAttributeAttribute) typeof(TType).GetCustomAttributes(typeof(AddedInVersionAttributeAttribute), false).FirstOrDefault();
            var replacedInVersion = (ReplacedInVersionAttribute)typeof(TType).GetCustomAttributes(typeof(ReplacedInVersionAttribute), false).FirstOrDefault();
            AddedInVersion = addedInVersion?.Version ?? 0;
            ReplacedInVersion = replacedInVersion?.Version;

            if (ReplacedInVersion.HasValue && !AllowVersionReplace)
            {
                throw new BuilderException("invalid attribute: replacement version not supported for this type of entity");
            }
        }
    }
}
