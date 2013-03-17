namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    static class ConversionHelper {

        public struct Argument {
            public Int32 Index { get; set; }
            public String Name { get; set; }
            public List<Expression[]> Indexers { get; set; }
            public INDEXER ComplexIndexer { get; set; }
            public Boolean IsRead { get; set; }
            public Boolean IsWritten { get; set; }
            public Boolean IsCall { get; set; }
            public Int32 CallRank { get; set; }
            public Boolean IsScalar { 
                get {
                    return (Indexers.Count == 1 && Indexers[0].Length == 0) || (IsCall && CallRank == 0);
                }
            }
        }

        public static ConversionResult GenerateKernelExecution(ConversionState state, ConversionResult result, Identifier target) {
            return GenerateKernelExecution(state, result, target, false);
        }
        public static ConversionResult GenerateKernelExecution(ConversionState state, ConversionResult result, Identifier target, bool complexIndexer) {
            KernelDetails kernelDetails;

            if (!result.KernelDetails.TryGetValue(out kernelDetails)) {
                // generation of kernel execution requested, but no kernel is specified. use a simple elementwise copy kernel

                String source = (complexIndexer) ? ComputeKernelTemplates.ElementWiseCopy_indexer : ComputeKernelTemplates.ElementWiseCopy;
                Int32 kernelIndex = state.GetNextKernelIndex();
                Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
                state.Class.Members.Add(
                    new Field {
                        Name = kernelSizeIdentifier,
                        Type = SystemTypes.UInt64,
                        DeclaringType = state.Class,
                    }
                );
                state.Constructor.Body.Statements.Add(
                    new AssignmentStatement {
                        Target = kernelSizeIdentifier,
                        Source = NodeHelper.GetSizeMultiplicationClosure(result.SizeIdentifier, result.Type.Rank),
                    }
                );
                #region Func generateKernelFunc
                Func<List<Identifier>, Expression, Identifier> generateKernelFunc = (buffers, kernelExpression) => {
                    Identifier kernelGlobalRangeIdentifier = Identifier.For(String.Format("kernel{0}GlobalRange", kernelIndex));
                    if (complexIndexer) {
                        state.CodeletMethod.Body.Statements.Add(new VariableDeclaration {
                            Name = kernelGlobalRangeIdentifier,
                            Type = SystemTypes.UInt64.GetArrayType(1),
                            Initializer = new ConstructArray {
                                ElementType = SystemTypes.UInt64,
                                Rank = 1,
                                Operands = new ExpressionList(
                                    Literal.Int32Two
                                )
                            }
                        });
                        ConversionHelper.GenerateSizeFromBufferIdentifier(state, state.CodeletMethod, String.Format("_{0}", target.Name), kernelGlobalRangeIdentifier);
                    } else {
                        ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, kernelGlobalRangeIdentifier, SystemTypes.UInt64, 1);
                        ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, kernelGlobalRangeIdentifier, kernelSizeIdentifier);
                    }
                    Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                    GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                    // set kernel size argument
                    GenerateKernelSetValueArgument(state, kernelIdentifier, 0, kernelSizeIdentifier);
                    // set buffer arguments
                    Int32 index = 1;
                    foreach (var buffer in buffers) {
                        GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                        index += 1;
                        #region Complex Indexer
                        if (complexIndexer) {
                            GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}n", buffer.Name)));
                            index++;
                            for (int dimension = 0; dimension < 2 /* FIXME */; dimension++) {
                                GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}from{1}", buffer.Name, dimension)));
                                index++;
                                GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}by{1}", buffer.Name, dimension)));
                                index++;
                            }
                        }
                        #endregion
                    }
                    Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}Predecessors", kernelIndex));
                    GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, result.WaitHandles);
                    Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                    // invoke kernel
                    GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, kernelGlobalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                    GenerateFlushCommandQueue(state);
                    return waitHandleIdentifier;
                };
                #endregion
                kernelDetails = new KernelDetails(source, generateKernelFunc);
            }
            if (target != null) {
                // target of kernel was supplied. assert that is is not used in a non-elementwise fashion in the arguments
                KernelArgumentDetails targetDetails;
                if (result.KernelArguments.TryGetValue(target.ToString(), out targetDetails)) {
                    Debug.Assert(targetDetails.IsElementwise);
                }
            } else {
                GenerateTemporaryBuffer(state, result, out target);
            }
            // now that we know all the arguments, compose kernel argument buffer list
            KeyValuePair<String, KernelArgumentDetails>[] arguments = new KeyValuePair<String, KernelArgumentDetails>[result.KernelArguments.Count];
            foreach (var argument in result.KernelArguments) {
                arguments[argument.Value.OpenCLArgumentId] = argument;
            }
            String kernelSource = kernelDetails.Source;
            /* TODO -- more expressions, some indexed some not ??? */
            kernelSource = kernelSource.Replace("{resultExpression}", result.AccessExpression);
            List<Identifier> identifiers = new List<Identifier>();
            Boolean targetPlaced = false;
            for (Int32 index = 0; index < arguments.Length; index += 1) {
                var argument = arguments[index];
                String kernelArgumentName = "global" + index.ToString();
                if (target.ToString() == argument.Key) {
                    Debug.Assert(!targetPlaced);
                    targetPlaced = true;
                    if (complexIndexer) {
                        kernelSource = kernelSource.Replace("{target}", String.Format("Access({0},{0}n,{0}f0,{0}b0,{0}f1,{0}b1)", kernelArgumentName));
                    } else {
                        kernelSource = kernelSource.Replace("{target}", String.Format("Access({0})", kernelArgumentName));
                    }
                }
                if (argument.Value.MathType.Rank == 0) {
                    kernelSource = kernelSource.Replace("{arguments}", String.Format(",\r\n\t\t{0} {1}{{arguments}}", argument.Value.MathType.ScalarType.OpenCLTypeName, kernelArgumentName));
                } else {
                    if (complexIndexer) {
                        kernelSource = kernelSource.Replace("{arguments}", String.Format(",\r\n\t\tglobal {0} * {1},\r\n\t\tulong {1}n,\r\n\t\tulong {1}f0,\r\n\t\tulong {1}b0,\r\n\t\tulong {1}f1,\r\n\t\tulong {1}b1{{arguments}}", argument.Value.MathType.ScalarType.OpenCLTypeName, kernelArgumentName));
                    } else {
                        kernelSource = kernelSource.Replace("{arguments}", String.Format(",\r\n\t\tglobal {0} * {1}{{arguments}}", argument.Value.MathType.ScalarType.OpenCLTypeName, kernelArgumentName));
                    }
                }
                if (complexIndexer) {
                    kernelSource = kernelSource.Replace("{" + index.ToString() + "}", String.Format("{0},{0}n,{0}f0,{0}b0,{0}f1,{0}b1", kernelArgumentName));
                } else {
                    kernelSource = kernelSource.Replace("{" + index.ToString() + "}", kernelArgumentName);
                }
                identifiers.Add(Identifier.For(argument.Key));
            }

            if (!targetPlaced) {
                String kernelArgumentName = "global" + identifiers.Count.ToString();
                identifiers.Add(target);
                if (complexIndexer) {
                    kernelSource = kernelSource.Replace("{target}", String.Format("Access({0},{0}n,{0}f0,{0}b0,{0}f1,{0}b1)", kernelArgumentName));
                    kernelSource = kernelSource.Replace("{arguments}", String.Format(",\r\n\t\tglobal {0} * {1},\r\n\t\tulong {1}n,\r\n\t\tulong {1}f0,\r\n\t\tulong {1}b0,\r\n\t\tulong {1}f1,\r\n\t\tulong {1}b1", result.Type.ScalarType.OpenCLTypeName, kernelArgumentName));
                } else {
                    kernelSource = kernelSource.Replace("{target}", String.Format("Access({0})", kernelArgumentName));
                    kernelSource = kernelSource.Replace("{arguments}", String.Format(",\r\n\t\tglobal {0} * {1}", result.Type.ScalarType.OpenCLTypeName, kernelArgumentName));
                }
            } else {
                kernelSource = kernelSource.Replace("{arguments}", "");
            }
            Expression kernelSourceExpression = CONTEXT._kernelRegistry.RegisterKernel(kernelSource);

            // finally generate the kernel call code
            List<Identifier> waitHandles = new List<Identifier>();
            waitHandles.Add(kernelDetails.Generate(identifiers, kernelSourceExpression));

            Dictionary<String, KernelArgumentDetails> newArguments = new Dictionary<String, KernelArgumentDetails>();
            newArguments.Add(target.ToString(), new KernelArgumentDetails(0, result.Type, true));

            return new ConversionResult(
                null,
                "Access({0})",
                result.Type,
                result.SizeIdentifier,
                newArguments,
                waitHandles,
                result.CompleteExpression
            );
        }

        public static void GenerateTemporaryBuffer(ConversionState state, ConversionResult result, out Identifier target) {
            // create temporary buffer to hold result
            Int32 tempBufferIndex = state.GetNextTempBufferIndex();
            target = Identifier.For("_tempBuffer" + tempBufferIndex.ToString());
            Identifier tempBufferSizeIdentifier = Identifier.For("_tempBufferSize" + tempBufferIndex.ToString());
            // create field to hold size of buffer and assign it
            state.Class.Members.Add(
                new Field {
                    Name = tempBufferSizeIdentifier,
                    Type = SystemTypes.UInt64,
                    DeclaringType = state.Class,
                }
            );
            Expression multiplies = NodeHelper.GetSizeMultiplicationClosure(result.SizeIdentifier, result.Type.Rank);
            state.Constructor.Body.Statements.Add(
                new AssignmentStatement {
                    Target = tempBufferSizeIdentifier,
                    Source = multiplies
                }
            );
            state.Class.Members.Add(
                new Field {
                    Name = target,
                    Type = STANDARD.Buffer,
                    DeclaringType = state.Class,
                }
            );
            state.CodeletMethod.Body.Statements.Add(
                new AssignmentStatement {
                    Target = target,
                    Source = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = new MemberBinding {
                                TargetObject = new MemberBinding {
                                    TargetObject = Identifier.For("device"),
                                    BoundMember = STANDARD.OpenCLComputeDevice.GetProperty(Identifier.For("CommandQueue"))
                                },
                                BoundMember = STANDARD.CommandQueue.GetProperty(Identifier.For("Context"))
                            },
                            BoundMember = STANDARD.Context.GetMethod(Identifier.For("CreateBuffer"), SystemTypes.UInt64, STANDARD.BufferFlags)
                        },
                        Operands = new ExpressionList(
                            new BinaryExpression {
                                NodeType = NodeType.Mul,
                                Operand1 = new Literal(result.Type.ScalarType.ByteSize, SystemTypes.Int32),
                                Operand2 = tempBufferSizeIdentifier
                            },
                            new MemberBinding {
                                BoundMember = STANDARD.BufferFlags.GetField(Identifier.For("ReadWrite"))
                            }
                        ),
                    }
                }
            );
            // dispose it afterwards
            state.CompleteMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = target,
                            BoundMember = STANDARD.Buffer.GetMethod(Identifier.For("Dispose")),
                        },
                    }
                }
            );
        }

        public static void GenerateSizeFromBufferIdentifier(ConversionState state, Method method, String target, Identifier identifier) {
            ConversionHelper.GenerateArrayInitialisation(state, method, identifier,
                NodeHelper.GetIndexedSize(target, 1),
                NodeHelper.GetIndexedSize(target, 0));
        }

        public static void GenerateFlushCommandQueue(ConversionState state) {
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = new MemberBinding {
                                TargetObject = Identifier.For("device"),
                                BoundMember = STANDARD.OpenCLComputeDevice.GetProperty(Identifier.For("CommandQueue"))
                            },
                            BoundMember = STANDARD.CommandQueue.GetMethod(Identifier.For("Flush")),
                        },
                    }
                }
            );
        }

        public static void GenerateGetKernelForProgram(ConversionState state, Identifier kernelIdentifier, Expression kernelExpression) {
            // add field to hold kernel instance
            state.Class.Members.Add(
                new Field {
                    Name = kernelIdentifier,
                    Type = STANDARD.Kernel,
                    DeclaringType = state.Class,
                }
            );
            // get compiled kernel and assign it
            state.CodeletMethod.Body.Statements.Add(
                new AssignmentStatement {
                    Target = kernelIdentifier,
                    Source = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.KernelManager.GetMethod(Identifier.For("GetKernelForProgram"), SystemTypes.String),
                        },
                        Operands = new ExpressionList(
                            kernelExpression
                        )
                    }
                }
            );
        }

        public static void GenerateKernelSetValueArgument(ConversionState state, Identifier kernelIdentifier, Int32 index, Expression expression) {
            GenerateKernelSetValueArgument(state, kernelIdentifier, index, expression, SystemTypes.UInt64);
        }

        public static void GenerateKernelSetValueArgument(ConversionState state, Identifier kernelIdentifier, Int32 index, Expression expression, TypeNode expressionType) {
            Method setValueArgumentMethod = ((Method)STANDARD.Kernel.GetMembersNamed(Identifier.For("SetValueArgument"))[0]).GetTemplateInstance(STANDARD.Kernel, expressionType);
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = setValueArgumentMethod,
                            TargetObject = kernelIdentifier,
                        },
                        Operands = new ExpressionList(
                            new Literal(index, expressionType),
                            expression
                        ),
                    }
                }
            );
        }

        public static void GenerateKernelSetLocalArgument(ConversionState state, Identifier kernelIdentifier, Int32 index, Expression localArgumentSizeExpression) {
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = kernelIdentifier,
                            BoundMember = STANDARD.Kernel.GetMethod(Identifier.For("SetLocalArgument"), SystemTypes.Int32, SystemTypes.UInt64)
                        },
                        Operands = new ExpressionList(
                            new Literal(index, SystemTypes.Int32),
                            localArgumentSizeExpression
                        )
                    }
                }
            );
        }

        public static void GenerateKernelSetGlobalArgument(ConversionState state, Identifier kernelIdentifier, Int32 index, Identifier buffer) {
            if (buffer.Name.StartsWith("_scalar")) {
                GenerateKernelSetValueArgument(state, kernelIdentifier, index, buffer, SystemTypes.Single /* FIXME */);
                return;
            }
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = kernelIdentifier,
                            BoundMember = STANDARD.Kernel.GetMethod(Identifier.For("SetGlobalArgument"), SystemTypes.Int32, STANDARD.Buffer),
                        },
                        Operands = new ExpressionList(
                            new Literal(index, SystemTypes.UInt32),
                            buffer
                        ),
                    }
                }
            );
        }

        public static void GenerateKernelPredecessors(ConversionState state, Identifier kernelPredecessorsIdentifier, List<Identifier> waitHandles) {
            state.CodeletMethod.Body.Statements.Add(
                new VariableDeclaration {
                    Name = kernelPredecessorsIdentifier,
                    Type = STANDARD.EventObject.GetArrayType(1),
                    Initializer = new ConstructArray {
                        ElementType = STANDARD.EventObject,
                        Rank = 1,
                        Operands = new ExpressionList(
                            new Literal(waitHandles.Count, SystemTypes.Int32)
                        ),
                    }
                }
            );
            for (Int32 waithandleIndex = 0; waithandleIndex < waitHandles.Count; waithandleIndex += 1) {
                state.CodeletMethod.Body.Statements.Add(
                    new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(kernelPredecessorsIdentifier, waithandleIndex),
                        Source = waitHandles[waithandleIndex],
                    }
                );
            }
        }

        public static void GenerateEventObjectAndStartKernel(ConversionState state, Identifier waitHandleIdentifier, Identifier kernelIdentifier, Identifier globalRangeIdentifier, Expression localRangeExpression, Expression kernelPredecessorsIdentifier) {
            // add wait handle identifier as field
            state.Class.Members.Add(
                new Field {
                    Name = waitHandleIdentifier,
                    Type = STANDARD.EventObject,
                    DeclaringType = state.Class,
                }
            );
            // execute kernel
            state.CodeletMethod.Body.Statements.Add(
                new AssignmentStatement {
                    Target = waitHandleIdentifier,
                    Source = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = new MemberBinding {
                                TargetObject = Identifier.For("device"),
                                BoundMember = STANDARD.OpenCLComputeDevice.GetProperty(Identifier.For("CommandQueue"))
                            },
                            BoundMember = STANDARD.CommandQueue.GetMethod(Identifier.For("StartKernel"), STANDARD.Kernel, SystemTypes.UInt64.GetArrayType(1), SystemTypes.UInt64.GetArrayType(1), STANDARD.EventObject.GetArrayType(1)),
                        },
                        Operands = new ExpressionList(
                            kernelIdentifier,
                            globalRangeIdentifier,
                            localRangeExpression,
                            kernelPredecessorsIdentifier
                        ),
                    }
                }
            );
            // free kernel in complete method
            state.CompleteMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = kernelIdentifier,
                            BoundMember = STANDARD.Kernel.GetMethod(Identifier.For("Dispose"))
                        }
                    }
                }
            );
            // free wait handle in complete method
            state.CompleteMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = waitHandleIdentifier,
                            BoundMember = STANDARD.EventObject.GetMethod(Identifier.For("Dispose"))
                        }
                    }
                }
            );
        }

        public static void GenerateArrayInitialisation(ConversionState state, Method method, Identifier name, params Expression[] initializers) {
            for (Int32 index = 0; index < initializers.Length; index++) {
                method.Body.Statements.Add(
                    new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(name, index),
                        Source = initializers[index],
                    }
                );
            }
        }

        public static void GenerateLocalArrayVariable(Method method, Identifier name, TypeNode type, Int32 count) {
            method.Body.Statements.Add(
                new VariableDeclaration {
                    Name = name,
                    Type = type.GetArrayType(1),
                    Initializer = new ConstructArray {
                        ElementType = type,
                        Rank = 1,
                        Operands = new ExpressionList(
                            new Literal(count, SystemTypes.Int32)
                        ),
                    }
                }
            );
        }

        public static void GenerateKernelSizeArrayField(ConversionState state, Identifier kernelSizeIdentifier) {
            Field kernelSizeField = new Field {
                Name = kernelSizeIdentifier,
                Type = SystemTypes.UInt64.GetArrayType(1),
                DeclaringType = state.Class,
            };
            state.Class.Members.Add(
                kernelSizeField
            );
        }

        public static void GenerateKernelSizeField(ConversionState state, Identifier kernelSizeIdentifier) {
            Field kernelSizeField = new Field {
                Name = kernelSizeIdentifier,
                Type = SystemTypes.UInt64,
                DeclaringType = state.Class,
            };
            state.Class.Members.Add(
                kernelSizeField
            );
        }

        public static void GenerateConsoleWriteLine(Method method, String value) {
            method.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.Console.GetMethod(Identifier.For("WriteLine"), SystemTypes.String),
                        },
                        Operands = new ExpressionList(
                            new Literal(value, SystemTypes.String)
                        ),
                    }
                }
            );
        }
        public static Expression[] GetExpressionsFromIndexer(INDEXER indexer, ref ComputeType computeType) {
            bool complexIndexer;
            return GetExpressionsFromIndexer(indexer, ref computeType, out complexIndexer);
        }

        public static Expression[] GetExpressionsFromIndexer(INDEXER indexer, ref ComputeType computeType, out bool complexIndexer) {
            complexIndexer = false;
            Expression[] expressions;
            if (indexer == null) {
                expressions = new Expression[computeType.Rank];
            } else if (indexer.indices.Length != computeType.Rank) {
                return null;
            } else {
                expressions = new Expression[indexer.indices.Length];
                for (Int32 index = 0; index < indexer.indices.Length; index += 1) {
                    EXPRESSION expression = indexer.indices[index];
                    ARRAY_RANGE arrayRange;
                    if (expression.type is INTEGER_TYPE || expression.type is CARDINAL_TYPE) {
                        Expression convertedExpression = (Expression)expression.convert();
                        if (convertedExpression == null) {
                            return null;
                        }
                        expressions[index] = convertedExpression;
                        if (computeType.Rank == 1) {
                            return null;
                        }
                        computeType = new ComputeType(computeType.ScalarType, computeType.Rank - 1);
                    } else if (expression.Is(out arrayRange)) {
                        INTEGER_LITERAL from;
                        INTEGER_LITERAL to;
                        INTEGER_LITERAL by;
                        if (!arrayRange.from.Is(out from) || !arrayRange.to.Is(out to) || !arrayRange.by.Is(out by) || from.integer != 0 || to.integer != -1 || by.integer != 1) {
                            complexIndexer = true;
                            return null;
                        }
                    } else if (expression is INSTANCE) {
                        complexIndexer = true;
                        return null;
                    } else {
                        return null;
                    }
                }
            }
            return expressions;
        }

        public static Boolean GenerateNonFirstIndexerCheckAndAddItToArgument(Method method, ExpressionArgument argument, Expression[] indices) {
            Expression[] otherIndices = argument.Indexers[0];
            for (Int32 index = 0; index < indices.Length; index += 1) {
                if (indices[index] != null && otherIndices[index] != null) {
                    // generate runtime size check
                    method.Body.Statements.Add(
                        new If {
                            Condition = new BinaryExpression {
                                NodeType = NodeType.Ne,
                                Operand1 = Identifier.For(String.Format("data{0}occurrence0index{1}", argument.Index, index)),
                                Operand2 = Identifier.For(String.Format("data{0}occurrence{1}index{2}", argument.Index, argument.Indexers.Count, index)),
                            },
                            TrueBlock = new Block {
                                Statements = new StatementList(
                                    NodeHelper.GetThrowFor(SystemTypes.ArgumentException)
                                ),
                            },
                        }
                    );
                } else if (indices[index] != null || otherIndices[index] != null) {
                    // both indexes must exist or not exist
                    return false;
                }
            }
            argument.Indexers.Add(indices);
            return true;
        }

        public static void GenerateComplexIndexerPart(ConversionState state, ref ParameterList param, ref ParameterList helperParam, ref ExpressionList helperArg, ref List<TypeNode> typeList, String varName, Int32 index, Int32 dimension) {
            GenerateComplexIndexerPart(state, ref param, ref helperParam, ref helperArg, ref typeList, varName, index, dimension, Identifier.For(String.Format("_buffer{0}{1}{2}", index, varName, dimension)));
        }

        public static void GenerateComplexIndexerPart(ConversionState state, ref ParameterList param, ref ParameterList helperParam, ref ExpressionList helperArg, ref List<TypeNode> typeList, String varName, Int32 index, Int32 dimension, Identifier identifierG) {
            Identifier identifierL = Identifier.For(String.Format("data{0}{1}{2}", index, varName, dimension));
            param.Add(new Parameter {
                Name = identifierL,
                Type = SystemTypes.Int64
            });
            helperParam.Add(new Parameter {
                Name = identifierL,
                Type = SystemTypes.Int64
            });
            helperArg.Add(identifierL);
            typeList.Add(SystemTypes.Int64);
            state.Class.Members.Add(new Field {
                Type = SystemTypes.UInt64,
                Name = identifierG,
                DeclaringType = state.Class,
            });
            if (varName.Equals("to")) {
                state.Constructor.Body.Statements.Add(NodeHelper.GetIndexerPart(Identifier.For("data" + index), identifierL, identifierG, dimension, true));
            } else {
                state.Constructor.Body.Statements.Add(NodeHelper.GetIndexerPart(Identifier.For("data" + index), identifierL, identifierG, dimension, false));
            }
        }



        public static void GenerateIndexerToVariableAssignment(StatementList stmlist, String varName, String dataName, String suffix, Int32 dimension) {
            Identifier source = Identifier.For(String.Format("{0}{1}{2}", dataName, suffix, dimension));
            Identifier target = Identifier.For(String.Format("{0}{1}{2}", varName, suffix, dimension));;
            stmlist.Add(new VariableDeclaration {
                Name = target,
                Type = SystemTypes.UInt64
            });
            stmlist.Add(NodeHelper.GetIndexerPart(Identifier.For(dataName), source, target, dimension, suffix.Equals("to") ? true : false));
        }

        public static String FormatIdentifierExpression(ComputeType computeType, Expression[] indices, Int32 argumentIndex) {
            return FormatIdentifierExpression(computeType, indices, argumentIndex, false);
        }

        public static String FormatIdentifierExpression(ComputeType computeType, Expression[] indices, Int32 argumentIndex, bool complexIndexer) {
            return FormatIdentifierExpression(computeType, indices, argumentIndex, complexIndexer, false);
        }

        public static String FormatIdentifierExpression(ComputeType computeType, Expression[] indices, Int32 argumentIndex, bool complexIndexer, bool scalarValue) {
            StringBuilder builder = new StringBuilder();
            builder.Append(argumentIndex);
            builder.Append(':');
            builder.Append(computeType.ScalarType.OpenCLTypeName);
            if (!scalarValue) {
                builder.Append('[');
                for (Int32 index = 0; index < indices.Length; index += 1) {
                    if (index > 0) {
                        builder.Append(',');
                    }
                    if (complexIndexer) {
                        builder.Append("x");
                    } else if (indices[index] != null) {
                        builder.Append("1");
                    }
                }
                builder.Append(']');
            }
            return builder.ToString();
        }

        public static void GenerateOpenCLDelegates(ConversionState state, out DelegateNode getOpenCLTimeDelegateType, out DelegateNode startOpenCLDelegateType) {
            getOpenCLTimeDelegateType = new DelegateNode {
                Name = Identifier.For("GetOpenCLTimeCallback"),
                Flags = TypeFlags.Sealed | TypeFlags.NestedPrivate,
                DeclaringType = state.Class,
                ReturnType = SystemTypes.TimeSpan,
                Parameters = new ParameterList(
                    new Parameter {
                        Name = Identifier.For("device"),
                        Type = STANDARD.OpenCLComputeDevice,
                    }
                ),
            };
            state.Class.Members.Add(getOpenCLTimeDelegateType);
            startOpenCLDelegateType = new DelegateNode {
                Name = Identifier.For("StartOpenCLCallback"),
                Flags = TypeFlags.Sealed | TypeFlags.NestedPrivate,
                DeclaringType = state.Class,
                ReturnType = SystemTypes.Object,
                Parameters = new ParameterList(
                    new Parameter {
                        Name = Identifier.For("device"),
                        Type = STANDARD.OpenCLComputeDevice,
                    }
                ),
            };
            state.Class.Members.Add(startOpenCLDelegateType);
        }

        public static void GenerateTimeMethod(ConversionState state) {
            Method getTimeMethod = new Method {
                Name = Identifier.For("GetOpenCLTime"),
                Parameters = new ParameterList(
                    new Parameter {
                        Name = Identifier.For("device"),
                        Type = STANDARD.OpenCLComputeDevice,
                    }
                ),
                ReturnType = SystemTypes.TimeSpan,
                Body = new Block {
                    Statements = new StatementList(
                        new Return {
                            Expression = new MethodCall {
                                Callee = new MemberBinding {
                                    BoundMember = SystemTypes.TimeSpan.GetMethod(Identifier.For("FromSeconds"), SystemTypes.Double),
                                },
                                Operands = new ExpressionList(
                                    new Literal(1.0d, SystemTypes.Double)
                                ),
                            }
                        }
                    ),
                },
                DeclaringType = state.Class,
                CallingConvention = CallingConventionFlags.HasThis,
            };
            state.Class.Members.Add(getTimeMethod);
        }

        public static void GenerateCodelets(ConversionState state, DelegateNode getOpenCLTimeDelegateType, DelegateNode startOpenCLDelegateType, out Identifier codeletsIdentifier) {
            codeletsIdentifier = Identifier.For("codelets");
            ConversionHelper.GenerateLocalArrayVariable(state.Constructor, codeletsIdentifier, STANDARD.Codelet, 1);
            state.Constructor.Body.Statements.Add(
                new AssignmentStatement {
                    Target = NodeHelper.GetConstantIndexerForExpression(codeletsIdentifier, 0),
                    Source = new Construct {
                        Type = STANDARD.Codelet,
                        Constructor = new MemberBinding {
                            BoundMember = STANDARD.Codelet.GetConstructor(SystemTypes.Type, SystemTypes.Delegate, SystemTypes.Delegate),
                        },
                        Operands = new ExpressionList(
                            new MemberBinding {
                                BoundMember = STANDARD.OpenCLComputeDevice.GetProperty(Identifier.For("Type")),
                                Type = SystemTypes.Type,
                            },
                            new ConstructDelegate {
                                DelegateType = getOpenCLTimeDelegateType,
                                TargetObject = new This(),
                                MethodName = Identifier.For("GetOpenCLTime")
                            },
                            new ConstructDelegate {
                                DelegateType = startOpenCLDelegateType,
                                TargetObject = new This(),
                                MethodName = Identifier.For("StartOpenCL")
                            }
                        ),
                    },
                }
            );
        }

        public static void ProcessArguments(ConversionState state, List<Argument> sortedArguments, List<Argument> scalarArguments, bool complexIndexer, DelegateNode startOpenCLDelegateType, out Identifier dataUsesIdentifier, ref ParameterList ctorParams, ref ExpressionList ctorArgs) {
            ExpressionList helperArgs = new ExpressionList();
            ParameterList helperParams = new ParameterList();
            List<TypeNode> typeList = new List<TypeNode>();
            ProcessArguments(state, sortedArguments, scalarArguments, complexIndexer, startOpenCLDelegateType, out dataUsesIdentifier, ref ctorParams, ref ctorArgs, false, ref helperParams, ref helperArgs, ref typeList);
        }
        public static void ProcessArguments(ConversionState state, List<Argument> sortedArguments, List<Argument> scalarArguments, bool complexIndexer, DelegateNode startOpenCLDelegateType, out Identifier dataUsesIdentifier, ref ParameterList callParams, ref ExpressionList callArgs, ref ParameterList helperParams, ref ExpressionList helperArgs, ref List<TypeNode> typeList) {
            ProcessArguments(state, sortedArguments, scalarArguments, complexIndexer, startOpenCLDelegateType, out dataUsesIdentifier, ref callParams, ref callArgs, true, ref helperParams, ref helperArgs, ref typeList);
        }
        private static void ProcessArguments(ConversionState state, List<Argument> sortedArguments, List<Argument> scalarArguments, bool complexIndexer, DelegateNode startOpenCLDelegateType, out Identifier dataUsesIdentifier, ref ParameterList callParams, ref ExpressionList callArgs, bool needsHelper, ref ParameterList helperParams, ref ExpressionList helperArgs, ref List<TypeNode> typeList) {
            // generate dataUses which is supplied to codelet
            dataUsesIdentifier = Identifier.For("dataUses");
            ConversionHelper.GenerateLocalArrayVariable(state.Constructor, dataUsesIdentifier, STANDARD.DataUse, sortedArguments.Count - scalarArguments.Count);
            
            // process all arguments - add them to parameters and data uses
            #region // scalar arguments
            foreach (var argument in scalarArguments) {
                Identifier scalarIdentifier = Identifier.For("scalar" + argument.Index.ToString());
                Identifier globalIdentifier = Identifier.For("_scalar" + argument.Index.ToString());
                state.Class.Members.Add(new Field {
                    Type = SystemTypes.Single,
                    Name = globalIdentifier,
                    DeclaringType = state.Class,
                });
                state.Constructor.Body.Statements.Add(new AssignmentStatement {
                    Source = scalarIdentifier,
                    Target = globalIdentifier,
                });
                callParams.Add(new Parameter {
                    Name = scalarIdentifier,
                    Type = SystemTypes.Single,
                });
                if (argument.IsCall && argument.Indexers != null && argument.Indexers[0] != null) {
                    // if argument is a function call
                    callArgs.Add(argument.Indexers[0][0]);
                } else {
                    // and if not
                    callArgs.Add(new Identifier(argument.Name));
                }
                helperParams.Add(new Parameter {
                    Name = scalarIdentifier,
                    Type = SystemTypes.Single,
                });
                helperArgs.Add(scalarIdentifier);
                typeList.Add(SystemTypes.Single);
            }
            #endregion

            #region // data arguments
            Int32 scalarOffset = 0;
            foreach (var argument in sortedArguments) {
                // skip scalars
                if (argument.IsScalar) {
                    scalarOffset++;
                    continue;
                }

                //argument is data
                Identifier dataIdentifier = Identifier.For("data" + argument.Index.ToString());
                
                // compose data access
                String dataAcces;
                Boolean dataIsRef;
                if (argument.IsRead) {
                    if (argument.IsWritten) {
                        dataAcces = "ReadWrite";
                    } else {
                        dataAcces = "Read";
                    }
                    dataIsRef = false;
                } else {
                    Debug.Assert(argument.IsWritten);
                    dataAcces = "Write";
                    dataIsRef = true;
                }

                // create region
                Identifier dataRangeIdentifier = Identifier.For("range" + argument.Index.ToString());

                #region // argument is function call
                if (argument.IsCall && argument.Indexers != null && argument.Indexers[0] != null) {
                    callParams.Add(new Parameter {
                        Name = dataIdentifier,
                        Type = STANDARD.Data,
                    });
                    callArgs.Add(argument.Indexers[0][0]);
                    helperParams.Add(new Parameter {
                        Name = dataIdentifier,
                        Type = STANDARD.Data,
                    });
                    helperArgs.Add(dataIdentifier);
                    typeList.Add(STANDARD.Data);
                    state.CodeletMethod.Parameters.Add(
                        new Parameter {
                            Name = Identifier.For("buffer" + argument.Index.ToString()),
                            Type = STANDARD.Buffer
                        }
                    );
                    startOpenCLDelegateType.Parameters.Add(
                        new Parameter {
                            Name = Identifier.For("buffer" + argument.Index.ToString()),
                            Type = STANDARD.Buffer
                        }
                    );
                    state.Constructor.Body.Statements.Add(
                        new VariableDeclaration {
                            Name = dataRangeIdentifier,
                            Type = STANDARD.DataRangeIndex.GetArrayType(1),
                            Initializer = new ConstructArray {
                                ElementType = STANDARD.DataRangeIndex,
                                Rank = 1,
                                Operands = new ExpressionList(
                                    new Literal(argument.CallRank, SystemTypes.Int32)
                                ),
                            }
                        }
                    );

                    // add data use with corresponding data access
                    state.Constructor.Body.Statements.Add(
                        new AssignmentStatement {
                            Target = NodeHelper.GetConstantIndexerForExpression(dataUsesIdentifier, argument.Index - scalarOffset),
                            Source = new Construct {
                                Constructor = new MemberBinding {
                                    BoundMember = STANDARD.DataUse.GetConstructor(STANDARD.Data, STANDARD.DataRangeIndex.GetArrayType(1), STANDARD.DataAccess),
                                },
                                Operands = new ExpressionList(
                                    dataIdentifier,
                                    dataRangeIdentifier,
                                    new MemberBinding {
                                        BoundMember = STANDARD.DataAccess.GetField(Identifier.For(dataAcces))
                                    }
                                ),
                            }
                        }
                    );
                    continue;
                }
                #endregion

                #region // argument is data variable
                Expression[] argumentIndices = argument.Indexers[0];
                state.Constructor.Body.Statements.Add(
                    new VariableDeclaration {
                        Name = dataRangeIdentifier,
                        Type = STANDARD.DataRangeIndex.GetArrayType(1),
                        Initializer = new ConstructArray {
                            ElementType = STANDARD.DataRangeIndex,
                            Rank = 1,
                            Operands = new ExpressionList(
                                new Literal(argumentIndices.Length, SystemTypes.Int32)
                            ),
                        }
                    }
                );
                for (Int32 index = 0; index < argumentIndices.Length; index += 1) {
                    if (argumentIndices[index] != null) {
                        state.Constructor.Body.Statements.Add(
                            new AssignmentStatement {
                                Target = NodeHelper.GetConstantIndexerForExpression(dataRangeIdentifier, index),
                                Source = new Construct {
                                    Constructor = new MemberBinding {
                                        BoundMember = STANDARD.DataRangeIndex.GetConstructor(SystemTypes.Int64),
                                    },
                                    Operands = new ExpressionList(
                                        Identifier.For(String.Format("data{0}occurrence0index{1}", argument.Index, index))
                                    ),
                                }
                            }
                        );
                    }
                }

                #region // add data use with corresponding data access
                state.Constructor.Body.Statements.Add(
                    new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(dataUsesIdentifier, argument.Index - scalarOffset),
                        Source = new Construct {
                            Constructor = new MemberBinding {
                                BoundMember = STANDARD.DataUse.GetConstructor(STANDARD.Data, STANDARD.DataRangeIndex.GetArrayType(1), STANDARD.DataAccess),
                            },
                            Operands = new ExpressionList(
                                dataIdentifier,
                                dataRangeIdentifier,
                                new MemberBinding {
                                    BoundMember = STANDARD.DataAccess.GetField(Identifier.For(dataAcces))
                                }
                            ),
                        }
                    }
                );
                #endregion 

                #region // add to all paramaters
                if (dataIsRef) {
                    if (needsHelper) {
                        helperParams.Add(new Parameter {
                            Name = dataIdentifier,
                            Type = new ReferenceTypeExpression(STANDARD.Data),
                        });
                        helperArgs.Add(new UnaryExpression {
                            NodeType = NodeType.RefAddress,
                            Operand = new Identifier(argument.Name),
                        });
                    } else {
                        callParams.Add(new Parameter {
                            Name = dataIdentifier,
                            Type = new ReferenceTypeExpression(STANDARD.Data),
                        });
                        callArgs.Add(new UnaryExpression {
                            NodeType = NodeType.RefAddress,
                            Operand = new Identifier(argument.Name),
                        });
                    }
                } else {
                    callParams.Add(new Parameter {
                        Name = dataIdentifier,
                        Type = STANDARD.Data,
                    });
                    callArgs.Add(new Identifier(argument.Name));
                    helperParams.Add(new Parameter {
                        Name = dataIdentifier,
                        Type = STANDARD.Data,
                    });
                    helperArgs.Add(dataIdentifier);
                    typeList.Add(STANDARD.Data);
                }
                #endregion

                #region // data occurrencies for simple indexer
                for (Int32 occurrenceIndex = 0; occurrenceIndex < argument.Indexers.Count; occurrenceIndex += 1) {
                    argumentIndices = argument.Indexers[occurrenceIndex];
                    for (Int32 indicesIndex = 0; indicesIndex < argumentIndices.Length; indicesIndex += 1) {
                        if (argumentIndices[indicesIndex] != null) {
                            callParams.Add(new Parameter {
                                Name = Identifier.For(String.Format("data{0}occurrence{1}index{2}", argument.Index, occurrenceIndex, indicesIndex)),
                                Type = SystemTypes.Int64,
                            });
                            callArgs.Add(argumentIndices[indicesIndex]);
                            helperParams.Add(new Parameter {
                                Name = Identifier.For(String.Format("data{0}occurrence{1}index{2}", argument.Index, occurrenceIndex, indicesIndex)),
                                Type = SystemTypes.Int64,
                            });
                            helperArgs.Add(Identifier.For(String.Format("data{0}occurrence{1}index{2}", argument.Index, occurrenceIndex, indicesIndex)));
                            typeList.Add(SystemTypes.Int64);
                        }
                    }
                }
                #endregion

                #region // if a complex indexer is used
                if (complexIndexer) {
                    // currently we have ElementWiseCopy kernel for 2 dimensions
                    for (int dimension = 0; dimension < 2 /* FIXME */; dimension++) {
                        // this argument has own complex indexer for this dimension
                        if (argument.ComplexIndexer != null && dimension < argument.ComplexIndexer.indices.Length) {
                            EXPRESSION ind = argument.ComplexIndexer.indices[dimension];
                            if (ind is ARRAY_RANGE) {
                                ARRAY_RANGE range = ind as ARRAY_RANGE;
                                // create variables and fill with the range
                                GenerateComplexIndexerVariables(state, argument.Index, dimension, ref callParams, ref callArgs, ref helperParams, ref helperArgs, ref typeList, range.from.convert() as Expression, range.to.convert() as Expression, range.by.convert() as Expression);
                            } else if (ind is INTEGER_LITERAL) {
                                // it is not a range, it is a literal
                                INTEGER_LITERAL lit = ind as INTEGER_LITERAL;
                                // create variables and fill with the range
                                GenerateComplexIndexerVariables(state, argument.Index, dimension, ref callParams, ref callArgs, ref helperParams, ref helperArgs, ref typeList, lit.convert() as Literal, lit.convert() as Literal, Literal.Int32One);
                            } else if (ind is INSTANCE) {
                                Expression expA = new MemberBinding {
                                    TargetObject = ind.convert() as Expression,
                                    BoundMember = STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]
                                };
                                Expression expB = new MemberBinding {
                                    TargetObject = ind.convert() as Expression,
                                    BoundMember = STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]
                                };
                                Expression expC = new MemberBinding {
                                    TargetObject = ind.convert() as Expression,
                                    BoundMember = STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]
                                };
                                GenerateComplexIndexerVariables(state, argument.Index, dimension, ref callParams, ref callArgs, ref helperParams, ref helperArgs, ref typeList, expA, expB, expC);
                            } else {
                                // Unknown indexer type
                                throw new NotImplementedException();
                            }
                        } else if (argument.ComplexIndexer == null && dimension < argument.Indexers[0].Length) {
                            // this argument has not a complex indexer for this dimension
                            // create variables with default values (from 0 to -1 by 1)
                            GenerateComplexIndexerVariables(state, argument.Index, dimension, ref callParams, ref callArgs, ref helperParams, ref helperArgs, ref typeList, Literal.Int32Zero, Literal.Int32MinusOne, Literal.Int32One);
                        } else {
                            // this argument doesn't have this dimension
                            // create variables with default values (from 0 to 0 by 1)
                            GenerateComplexIndexerVariables(state, argument.Index, dimension, ref callParams, ref callArgs, ref helperParams, ref helperArgs, ref typeList, Literal.Int32Zero, Literal.Int32Zero, Literal.Int32One);
                        }
                    }
                    // rowLength variable
                    Identifier bufferSizeIdent = Identifier.For(String.Format("_buffer{0}n", argument.Index));
                    state.Class.Members.Add(new Field {
                        Type = SystemTypes.UInt64,
                        Name = bufferSizeIdent,
                        DeclaringType = state.Class,
                    });
                    if (argument.Indexers[0].Length == 2) {
                        // length of row is the length of the 2nd dimension (i.e. number of columns)
                        state.Constructor.Body.Statements.Add(new AssignmentStatement {
                            Target = bufferSizeIdent,
                            Source = NodeHelper.GetConstantIndexerForExpression(new MethodCall {
                                Callee = new MemberBinding {
                                    TargetObject = Identifier.For("data" + argument.Index),
                                    BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                                }
                            }, argument.Indexers[0].Length - 1)
                        });
                    } else {
                        // or 1 for 1D vectors
                        state.Constructor.Body.Statements.Add(new AssignmentStatement {
                            Target = bufferSizeIdent,
                            Source = Literal.Int32One
                        });
                    }
                }
                #endregion

                state.CodeletMethod.Parameters.Add(
                    new Parameter {
                        Name = Identifier.For("buffer" + argument.Index.ToString()),
                        Type = STANDARD.Buffer
                    }
                );
                startOpenCLDelegateType.Parameters.Add(
                    new Parameter {
                        Name = Identifier.For("buffer" + argument.Index.ToString()),
                        Type = STANDARD.Buffer
                    }
                );
                #endregion
            }
            #endregion
        }

        private static void GenerateComplexIndexerVariables(ConversionState state, Int32 index, Int32 dimension, ref ParameterList callParams, ref ExpressionList callArgs, ref ParameterList helperParams, ref ExpressionList helperArgs, ref List<TypeNode> typeList, params Expression[] values) {
            Debug.Assert(values.Length == 3);
            GenerateComplexIndexerPart(state, ref callParams, ref helperParams, ref helperArgs, ref typeList, "from", index, dimension);
            callArgs.Add(values[0]);
            GenerateComplexIndexerPart(state, ref callParams, ref helperParams, ref helperArgs, ref typeList, "to", index, dimension);
            callArgs.Add(values[1]);
            GenerateComplexIndexerPart(state, ref callParams, ref helperParams, ref helperArgs, ref typeList, "by", index, dimension);
            callArgs.Add(values[2]);
        }

        public static void GenerateSubmitCodelet(ConversionState state, Identifier codeletsIdentifier, Identifier dataUsesIdentifier) {
            state.Constructor.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.ComputeManager.GetMethod(Identifier.For("SubmitTask"), STANDARD.Codelet.GetArrayType(1), STANDARD.DataUse.GetArrayType(1)),
                        },
                        Operands = new ExpressionList(
                            codeletsIdentifier,
                            dataUsesIdentifier
                        ),
                    }
                }
            );
        }

        public static void FinishOperation(ConversionState state, ConversionResult finalConversionResult) {
            Identifier operationIdentifier = Identifier.For("_operation");
            
            state.Class.Members.Add(
                    new Field {
                        Name = operationIdentifier,
                        Type = SystemTypes.Object,
                        DeclaringType = state.Class,
                    }
                );
            state.CodeletMethod.Body.Statements.Add(
                new AssignmentStatement {
                    Target = operationIdentifier,
                    Source = new Construct {
                        Constructor = new MemberBinding {
                            BoundMember = SystemTypes.Object.GetConstructor(),
                        }
                    }
                }
            );
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.DependencyManager.GetMethod(Identifier.For("RegisterOperation"), SystemTypes.Object),
                        },
                        Operands = new ExpressionList(
                            operationIdentifier
                        ),
                    }
                }
            );
            Debug.Assert(finalConversionResult.WaitHandles.Count == 1);
            state.CodeletMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = finalConversionResult.WaitHandles[0],
                            BoundMember = STANDARD.EventObject.GetMethod(Identifier.For("RegisterCompletionCallback"), STANDARD.EventObjectCompletionCallback),
                        },
                        Operands = new ExpressionList(
                            new ConstructDelegate {
                                DelegateType = STANDARD.EventObjectCompletionCallback,
                                TargetObject = new This(),
                                MethodName = Identifier.For("CompleteOpenCL")
                            }
                        ),
                    }
                }
            );
            state.CompleteMethod.Body.Statements.Add(
                new ExpressionStatement {
                    Expression = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.DependencyManager.GetMethod(Identifier.For("FinishOperation"), SystemTypes.Object),
                        },
                        Operands = new ExpressionList(
                            operationIdentifier
                        ),
                    }
                }
            );
            //ConversionHelper.GenerateConsoleWriteLine(state.CodeletMethod, "CODELET METHOD");
            state.CodeletMethod.Body.Statements.Add(
                new Return {
                    Expression = operationIdentifier,
                }
            );
        }

        public static void ProcessReceiver(ConversionState state, ConversionResult expressionResult, Identifier receiverIdentifier, ComputeType receiverComputeType, Expression[] indices, INDEXER indexer, ref List<Argument> sortedArguments, bool complexIndexer, out ExpressionArgument receiverArgument) {
            if (state.Arguments.TryGetValue(receiverIdentifier.ToString(), out receiverArgument)) {
                #region // receiver is also used in expression

                // check that indexers used on receiver match pre-existing ones (do not allow different indexers in the same assignment)
                ConversionHelper.GenerateNonFirstIndexerCheckAndAddItToArgument(state.Constructor, receiverArgument, indices);

                Debug.Assert(!complexIndexer);
                /* FIXME -- complex indexer on both side, temp variable needed */

                var argument = sortedArguments[receiverArgument.Index];
                sortedArguments[receiverArgument.Index] = new ConversionHelper.Argument {
                    Index = argument.Index,
                    Name = argument.Name,
                    Indexers = argument.Indexers,
                    ComplexIndexer = argument.ComplexIndexer,
                    IsRead = true,
                    IsWritten = true,
                    IsCall = argument.IsCall,
                    CallRank = argument.CallRank
                };

                KernelArgumentDetails argumentDetails;
                if (expressionResult.KernelArguments.TryGetValue("buffer" + receiverArgument.Index.ToString(), out argumentDetails)) {
                    // receiver is also used in current kernel. careful!

                    // (WRONG) ATI OpenCL implementation does not support read_write modifier. we need temporary storage anyway
                    if (!argumentDetails.IsElementwise) {
                        expressionResult = ConversionHelper.GenerateKernelExecution(state, expressionResult, null);
                    }
                }
                // since receiver is in expression, null check has already been generated. only need to check that sizes are equal
                state.Constructor.Body.Statements.Add(
                    NodeHelper.GetThrowIfDimensionsNotEqual(Identifier.For("size" + receiverArgument.Index), expressionResult.SizeIdentifier)
                );
                #endregion
            } else {
                #region // receiver is not used in expression
                receiverArgument = new ExpressionArgument(sortedArguments.Count, indices, complexIndexer ? indexer : null, -1);
                sortedArguments.Add(
                    new ConversionHelper.Argument {
                        Index = receiverArgument.Index,
                        Name = receiverIdentifier.ToString(),
                        Indexers = receiverArgument.Indexers,
                        ComplexIndexer = receiverArgument.ComplexIndexer,
                        IsRead = false,
                        IsWritten = true,
                        IsCall = receiverArgument.IsCall,
                        CallRank = receiverArgument.CallRank
                    }
                );
                Identifier receiverDataIdentifier = Identifier.For("data" + receiverArgument.Index);
                MethodCall getDimensionsMethodCall = new MethodCall {
                    Callee = new MemberBinding {
                        TargetObject = receiverDataIdentifier,
                        BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                    }
                };

                Block isNullBlock;
                Block isNotNullBlock;
                if (!indices.All(element => element == null)) {
                    #region // simple indexer is used
                    // indexer is used on receiver, so receiver must not be null
                    isNullBlock = new Block {
                        Statements = new StatementList(
                            NodeHelper.GetThrowFor(SystemTypes.ArgumentNullException)
                        ),
                    };
                    // and non-indexed dimensions must match
                    Identifier receiverSizeIdentifier = Identifier.For("size" + receiverArgument.Index);
                    Expression checkSizesExpression = null;
                    Int32 sizeIndex = 0;
                    for (Int32 indicesIndex = 0; indicesIndex < indices.Length; indicesIndex += 1) {
                        if (indices[indicesIndex] == null) {
                            Expression checkSizeExpression = new BinaryExpression {
                                NodeType = NodeType.Ne,
                                Operand1 = NodeHelper.GetConstantIndexerForExpression(expressionResult.SizeIdentifier, sizeIndex),
                                Operand2 = NodeHelper.GetConstantIndexerForExpression(receiverSizeIdentifier, indicesIndex),
                            };
                            if (checkSizesExpression == null) {
                                checkSizesExpression = checkSizeExpression;
                            } else {
                                Expression oldCheckSizesExpression = checkSizesExpression;
                                checkSizesExpression = new BinaryExpression {
                                    NodeType = NodeType.LogicalOr,
                                    Operand1 = oldCheckSizesExpression,
                                    Operand2 = checkSizeExpression,
                                };
                            }
                            sizeIndex += 1;
                        }
                    }
                    Debug.Assert(checkSizesExpression != null);
                    isNotNullBlock = new Block {
                        Statements = new StatementList(
                            new VariableDeclaration {
                                Name = receiverSizeIdentifier,
                                Type = SystemTypes.UInt64.GetArrayType(1),
                                Initializer = getDimensionsMethodCall,
                            },
                            new If {
                                Condition = checkSizesExpression,
                                TrueBlock = new Block {
                                    Statements = new StatementList(
                                        NodeHelper.GetThrowFor(SystemTypes.ArgumentException)
                                    ),
                                }
                            }
                        )
                    };
                    #endregion
                } else {
                    if (complexIndexer) {
                        #region// if there is a complex indexer, no dimensions check can be done directly
                        if (receiverArgument.ComplexIndexer != null) {
                            // receiver has a complex indexer, therefore cannot be null
                            isNullBlock = new Block {
                                Statements = new StatementList(NodeHelper.GetThrowFor(SystemTypes.ArgumentNullException)),
                            };
                        } else {
                            #region // receiver is not indexed, can therefore be null and needs to be allocated if so
                            if (expressionResult.ArgumentIndex.HasValue) {
                                #region // expression is indexed, we need to compute the range
                                Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
                                StatementList stmlist = GenerateIndexedSize(expressionResult, receiverComputeType, receiverDataIdentifier, resultSizeIdentifier);
                                isNullBlock = new Block {
                                    Statements = stmlist
                                };
                                #endregion
                            } else {
                                #region // expression is not indexed
                                ExpressionList constructArrayExpressions = new ExpressionList();
                                for (int index = 0; index < expressionResult.Type.Rank; index++) {
                                    constructArrayExpressions.Add(NodeHelper.GetConstantIndexerForExpression(expressionResult.SizeIdentifier, index));
                                }
                                /* 
                                 * if (receiver == null)
                                 *     receiver = new Data(size0[0], ...);
                                 */
                                isNullBlock = new Block {
                                    Statements = new StatementList(
                                        new AssignmentStatement {
                                            Target = new AddressDereference {
                                                Address = receiverDataIdentifier,
                                                Type = STANDARD.Data,
                                            },
                                            Source = new Construct {
                                                Constructor = new MemberBinding {
                                                    BoundMember = STANDARD.Data.GetConstructor(SystemTypes.Array),
                                                },
                                                Operands = new ExpressionList(
                                                    new ConstructArray {
                                                        ElementType = expressionResult.Type.ScalarType.TypeNode,
                                                        Rank = receiverComputeType.Rank,
                                                        Operands = constructArrayExpressions,
                                                    }
                                                ),
                                            }
                                        }
                                    ),
                                };
                                #endregion
                            }
                            #endregion
                        }
                        // the idexers must have the same range
                        isNotNullBlock = new Block {
                            /* FIXME -- range check */
                        };
                        #endregion
                    } else {
                        #region // receiver is not indexed, can therefore be null and needs to be allocated if so

                        ExpressionList constructArrayExpressions = new ExpressionList();
                        for (int index = 0; index < expressionResult.Type.Rank; index++) {
                            constructArrayExpressions.Add(NodeHelper.GetConstantIndexerForExpression(expressionResult.SizeIdentifier, index));
                        }
                        isNullBlock = new Block {
                            Statements = new StatementList(
                                new AssignmentStatement {
                                    Target = new AddressDereference {
                                        Address = receiverDataIdentifier,
                                        Type = STANDARD.Data,
                                    },
                                    Source = new Construct {
                                        Constructor = new MemberBinding {
                                            BoundMember = STANDARD.Data.GetConstructor(SystemTypes.Array),
                                        },
                                        Operands = new ExpressionList(
                                            new ConstructArray {
                                                ElementType = expressionResult.Type.ScalarType.TypeNode,
                                                Rank = receiverComputeType.Rank,
                                                Operands = constructArrayExpressions,
                                            }
                                        ),
                                    }
                                }
                            ),
                        };
                        isNotNullBlock = new Block {
                            Statements = new StatementList(
                                NodeHelper.GetThrowIfDimensionsNotEqual(
                                    getDimensionsMethodCall,
                                    expressionResult.SizeIdentifier
                                )
                            ),
                        };
                        #endregion
                    }
                }
                state.Constructor.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Eq,
                            Operand1 = receiverDataIdentifier,
                            Operand2 = Literal.Null,
                        },
                        TrueBlock = isNullBlock,
                        FalseBlock = isNotNullBlock,
                    }
                );
                #endregion
            }
        }

        public static List<Argument> ExtractArguments(ConversionState state, Boolean onlyScalars) {
            return (from pair in state.Arguments
                    where onlyScalars ? ((pair.Value.Indexers != null && pair.Value.Indexers.Count == 1 && pair.Value.Indexers[0].Length == 0) || (pair.Value.IsCall && pair.Value.CallRank == 0)) : true
                    orderby pair.Value.Index
                    select new Argument {
                        Index = pair.Value.Index,
                        Name = pair.Key,
                        Indexers = pair.Value.Indexers,
                        ComplexIndexer = pair.Value.ComplexIndexer,
                        IsRead = true,
                        IsWritten = false,
                        IsCall = pair.Value.IsCall,
                        CallRank = pair.Value.CallRank,
                    }).ToList();
        }


        public static StatementList GenerateIndexedSize(ConversionResult expressionResult, ComputeType receiverComputeType, Identifier receiverDataIdentifier, Identifier sizeIdentifier) {
            ExpressionList constructArrayExpressions = new ExpressionList();
            for (int index = 0; index < expressionResult.Type.Rank; index++) {
                constructArrayExpressions.Add(NodeHelper.GetIndexedSize(sizeIdentifier.Name, index));
            }
            StatementList stmlist = new StatementList();
            for (int dimension = 0; dimension < expressionResult.Type.Rank; dimension++) {
                ConversionHelper.GenerateIndexerToVariableAssignment(stmlist, sizeIdentifier.Name, String.Format("data{0}", expressionResult.ArgumentIndex), "from", dimension);
                ConversionHelper.GenerateIndexerToVariableAssignment(stmlist, sizeIdentifier.Name, String.Format("data{0}", expressionResult.ArgumentIndex), "to", dimension);
                ConversionHelper.GenerateIndexerToVariableAssignment(stmlist, sizeIdentifier.Name, String.Format("data{0}", expressionResult.ArgumentIndex), "by", dimension);
            }
            /*
             * if (receiver == null)
             *     receiver = new Data((size0to0-size0from0/size0by0) + ~~((size0to0-size0from0)%size0by0), ...);
             */
            stmlist.Add(new AssignmentStatement {
                Target = new AddressDereference {
                    Address = receiverDataIdentifier,
                    Type = STANDARD.Data,
                },
                Source = new Construct {
                    Constructor = new MemberBinding {
                        BoundMember = STANDARD.Data.GetConstructor(SystemTypes.Array),
                    },
                    Operands = new ExpressionList(new ConstructArray {
                        ElementType = expressionResult.Type.ScalarType.TypeNode,
                        Rank = receiverComputeType.Rank,
                        Operands = constructArrayExpressions,
                    }),
                }
            });
            return stmlist;
        }

    }
}
