using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Compiler;

namespace ETH.Zonnon.Compute {
    public class OperationRegistry {
        Dictionary<String, Class> Classes = new Dictionary<String, Class>();

        public void SetNamespace(Namespace space) {
            foreach (var generatedClass in Classes) {
                space.Types.Add(generatedClass.Value);
                generatedClass.Value.Namespace = space.Name;
                generatedClass.Value.DeclaringModule = CONTEXT.symbolTable;
                CONTEXT.symbolTable.Types.Add(generatedClass.Value);
            }
        }

        public Class RegisterOperation(String completeExpression, Class generatedClass) {
            Class foundClass;
            if (!Classes.TryGetValue(completeExpression, out foundClass)) {
                generatedClass.Name = Identifier.For("Operation" + Classes.Count);
                generatedClass.GetField(Identifier.For("CompleteExpression")).Initializer = new Literal(completeExpression, SystemTypes.String);
                Classes.Add(completeExpression, generatedClass);
                foundClass = generatedClass;
            }
            return foundClass;
        }
    }
}
