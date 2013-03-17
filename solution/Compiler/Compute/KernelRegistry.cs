namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;
    using System.Collections.Generic;

    public class KernelRegistry {
        Class _class;
        Dictionary<String, Field> _registeredKernels;
        StaticInitializer _initalizer;

        public KernelRegistry() {
            _class = new Class {
                Name = Identifier.For("ComputeKernels"),
                Members = new MemberList(),
                Flags = TypeFlags.Sealed,
            };
            _initalizer = new StaticInitializer {
                DeclaringType = _class,
                Body = new Block {
                    Statements = new StatementList(),
                }
            };
            _class.Members.Add(_initalizer);
            _registeredKernels = new Dictionary<String, Field>();
        }

        public void SetNamespace(Namespace space) {
            space.Types.Add(_class);
            _class.Namespace = space.Name;
            _class.DeclaringModule = CONTEXT.symbolTable;
            CONTEXT.symbolTable.Types.Add(_class);
        }

        public Expression RegisterKernel(String source) {
            Field field;
            if (!_registeredKernels.TryGetValue(source, out field)) {
                Identifier identifier = Identifier.For("Kernel" + _registeredKernels.Count.ToString());
                field = new Field {
                    Name = identifier,
                    Type = SystemTypes.String,
                    Flags = FieldFlags.Static | FieldFlags.Public,
                    DeclaringType = _class,
                };
                _initalizer.Body.Statements.Add(
                    new AssignmentStatement {
                        Target = identifier,
                        Source = new Literal(source, SystemTypes.String),
                    }
                );
                _registeredKernels.Add(source, field);
                _class.Members.Add(field);
            }
            return new MemberBinding {
                BoundMember = field,
            };
        }
    }
}
