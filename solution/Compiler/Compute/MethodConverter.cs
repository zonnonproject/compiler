namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;
    using System.Linq;
    using System.Diagnostics;
    using System.Collections.Generic;

    static class MethodConverter {
        public struct MethodStruct {
            public String Name { get; set; }
            public String KernelSource { get; set; }
            public TYPE Type { get; set; }
            public MethodConverter.ConvertFunction Func { get; set; }
            public String Identity { get; set; }
            public String Operation { get; set; }
        }

        public delegate ConversionResult ConvertFunction(ConversionState state, ConversionResult arg, MethodStruct methodStruct, Identifier receiveBufferIdentifier);

        public static Expression ConvertExpression(EXPRESSION expression, TYPE type) {
            ConversionState state = new ConversionState();
            ConversionResult conversionResult;
            if (ExpressionConverter.Convert(state, expression, true).TryGetValue(out conversionResult)) {
                Expression node = null;
                if (expression is CALL) {
                    // call can be directly converted
                    node = expression.convert() as MethodCall;
                } else if (expression is INSTANCE) {
                    // if the result should be data, return plain indetifier
                    if (type is ARRAY_TYPE) {
                        return expression.name;
                    }
                    // otherwise don't convert single instance
                    node = null;
                } else {
                    // everything else convert with empty convert function
                    node = MethodConverter.Convert(
                        state,
                        conversionResult,
                        new MethodConverter.MethodStruct {
                            Type = type,
                        }
                    );
                }
                return node;
            }
            return null;
        }

        public static Expression Convert(ConversionState state, ConversionResult expressionResult, MethodStruct methodStruct) {
            #region // result will be stored in a temporary value
            bool complexIndexer;
            ComputeType receiverComputeType;
            if (methodStruct.Type is ARRAY_TYPE) {
                // result is array
                ComputeType.FromType(methodStruct.Type).TryGetValue(out receiverComputeType);
            } else {
                // result is scalar
                receiverComputeType = new ComputeType(ComputeScalarType.Single, 1);
            }
            Identifier receiverIdentifier = Identifier.For("ret");
            
            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(null, ref receiverComputeType, out complexIndexer);
 
            complexIndexer |= expressionResult.HasComplexIndexer;

            #endregion

            List<ConversionHelper.Argument> sortedArguments = ConversionHelper.ExtractArguments(state, false);
            List<ConversionHelper.Argument> scalarArguments = ConversionHelper.ExtractArguments(state, true);
            
            //ExpressionArgument receiverArgument;
            //ConversionHelper.ProcessReceiver(state, expressionResult, receiverIdentifier, receiverComputeType, indices, null, ref sortedArguments, complexIndexer, out receiverArgument);
            
            #region // add the temporary variable as a receiver
            ExpressionArgument receiverArgument = new ExpressionArgument(sortedArguments.Count, indices, null, -1);
            sortedArguments.Add(new ConversionHelper.Argument {
                Index = receiverArgument.Index,
                Name = receiverIdentifier.ToString(),
                Indexers = receiverArgument.Indexers,
                IsRead = false,
                IsWritten = true,
                IsCall = receiverArgument.IsCall,
                CallRank = receiverArgument.CallRank,
            });

            Identifier receiverDataIdentifier = Identifier.For("data" + receiverArgument.Index);
            if (!expressionResult.HasComplexIndexer) {
                // allocate receiver (1D array with size 1 for scalar result, matrix (vector) in other cases)
                ExpressionList ctorExpr = new ExpressionList();
                for (int index = 0; index < receiverComputeType.Rank; index++) {
                    ctorExpr.Add(NodeHelper.GetConstantIndexerForExpression(expressionResult.SizeIdentifier, index));
                }
                state.Constructor.Body.Statements.Add(
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
                                    Rank = (methodStruct.Type is ARRAY_TYPE) ? receiverComputeType.Rank : 1,
                                    Operands = (methodStruct.Type is ARRAY_TYPE) ? ctorExpr : new ExpressionList(Literal.Int32One),
                                }
                            ),
                        }
                    }
                );
            } else {
                state.Constructor.Body.Statements.Add(new Block {
                    Statements = ConversionHelper.GenerateIndexedSize(expressionResult, receiverComputeType, receiverDataIdentifier, state.GetNextTempSizeIdentifier())
                });
            }
            #endregion
            
            #region // generate kernel
            ConversionResult? result = GenereteKernel(state, expressionResult, Identifier.For("buffer" + receiverArgument.Index.ToString()), methodStruct);
            ConversionResult finalConversionResult;
            if (result.HasValue)
                finalConversionResult = result.Value;
            else return null;
            #endregion

            #region // generate delegates
            DelegateNode getOpenCLTimeDelegateType;
            DelegateNode startOpenCLDelegateType;
            ConversionHelper.GenerateOpenCLDelegates(state, out getOpenCLTimeDelegateType, out startOpenCLDelegateType);
            #endregion

            #region // generate get time method
            ConversionHelper.GenerateTimeMethod(state);
            #endregion

            #region // generate the codelets
            Identifier codeletsIdentifier;
            ConversionHelper.GenerateCodelets(state, getOpenCLTimeDelegateType, startOpenCLDelegateType, out codeletsIdentifier);
            #endregion

            #region // process all arguments - add them to parameters and data uses
            ExpressionList ctorArgs = new ExpressionList();
            ExpressionList callArgs = new ExpressionList();
            ParameterList callParams = new ParameterList();
            List<TypeNode> typeList = new List<TypeNode>();

            Identifier dataUsesIdentifier;
            ConversionHelper.ProcessArguments(state, sortedArguments, scalarArguments, complexIndexer, startOpenCLDelegateType, out dataUsesIdentifier, ref callParams, ref callArgs, ref state.Constructor.Parameters, ref ctorArgs, ref typeList);

            #endregion

            ConversionHelper.GenerateSubmitCodelet(state, codeletsIdentifier, dataUsesIdentifier);
            ConversionHelper.FinishOperation(state, finalConversionResult);
            
            Class actualOperationClass = CONTEXT._operationRegistry.RegisterOperation(finalConversionResult.CompleteExpression, state.Class);
            
            #region // create method 'call'
            Identifier callIdentifier = Identifier.For("call");
            Method callMethod = actualOperationClass.GetMethod(callIdentifier, typeList.ToArray());
            Expression returnExp;
            if (methodStruct.Type is ARRAY_TYPE) {
                returnExp = receiverIdentifier;
            } else {
                returnExp = new BinaryExpression {
                    NodeType = NodeType.Castclass,
                    Operand2 = new MemberBinding {
                        BoundMember = expressionResult.Type.ScalarType.TypeNode.GetArrayType(1)
                    },
                    Operand1 = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = receiverIdentifier,
                            BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetHostArray")),
                        },
                    }
                };
            }
            if (methodStruct.Type is ARRAY_TYPE) {
            } else {
                returnExp = NodeHelper.GetConstantIndexerForExpression(returnExp, 0);
                if (methodStruct.Type is BOOLEAN_TYPE) {
                    // if it was a boolean operation invert the value (1 is false in OpenCL kernel)
                    returnExp = new BinaryExpression {
                        NodeType = NodeType.Eq,
                        Operand1 = returnExp,
                        Operand2 = Literal.Int32Zero
                    };
                }
            }
            if (callMethod == null)
                actualOperationClass.Members.Add(new Method {
                    DeclaringType = state.Class,
                    Flags = MethodFlags.Static,
                    ReturnType = (methodStruct.Type is ARRAY_TYPE) ? STANDARD.Data : methodStruct.Type.convert() as TypeNode /*receiverComputeType.ScalarType.TypeNode*/,
                    Parameters = callParams,
                    Name = callIdentifier,
                    Body = new Block {
                        Statements = new StatementList(
                            new VariableDeclaration {
                                Name = receiverIdentifier,
                                Type = STANDARD.Data,
                            },
                            new ExpressionStatement(new Construct {
                                Constructor = new MemberBinding {
                                    BoundMember = actualOperationClass.GetConstructors()[0],
                                },
                                Operands = ctorArgs,
                            }),

                            new Return(returnExp)
                        )
                    }
                });
            #endregion

            #region // return call to 'call'
            return new MethodCall {
                Operands = callArgs,
                Callee = new MemberBinding(null, actualOperationClass.GetMethod(callIdentifier, typeList.ToArray())),
                Type = expressionResult.Type.ScalarType.TypeNode,
            };
            #endregion
        }

        private static ConversionResult? GenereteKernel(ConversionState state, ConversionResult arg, Identifier receiverBufferIdentifier, MethodStruct methodStruct) {
            // generate kernel for argument if needed
            if (arg.KernelDetails != null) {
                if (methodStruct.Func != null) {
                    arg = ConversionHelper.GenerateKernelExecution(state, arg, null);
                } else {
                    return ConversionHelper.GenerateKernelExecution(state, arg, receiverBufferIdentifier);
                }
            }
            Debug.Assert(arg.KernelDetails == null);
            ConversionResult? result = null;
            if (methodStruct.Func != null)
                result = methodStruct.Func(state, arg, methodStruct, receiverBufferIdentifier);
            if (!result.HasValue) return null;
            return ConversionHelper.GenerateKernelExecution(state, result.Value, receiverBufferIdentifier, arg.HasComplexIndexer);
        }

        public static ConversionResult ConvertPPS(ConversionState state, ConversionResult arg, MethodStruct methodStruct, Identifier receiveBufferIdentifier) {
            #region // 1st stage
            // store new size in variable
            Int32 kernelIndex = state.GetNextKernelIndex();
            // get problem global size
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, arg.Type.Rank)
            );
            for (Int32 index = 0; index < arg.Type.Rank; index++) {
                state.Constructor.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, index, arg.SizeIdentifier, index)
                );
            }
            Dictionary<String, KernelArgumentDetails> arguments = arg.KernelArguments;
            String kernelSource = ComputeKernelTemplates.pps1;
            kernelSource = kernelSource.Replace("{type}", arg.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", arg.AccessExpression);
            kernelSource = kernelSource.Replace("{operation}", methodStruct.Operation);
            kernelSource = kernelSource.Replace("{identity}", methodStruct.Identity);

            List<Identifier> waitHandles = arg.WaitHandles;
            
            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                Identifier globalSizeIdent = Identifier.For("globalSize");
                state.CodeletMethod.Body.Statements.Add(new VariableDeclaration {
                    Name = globalSizeIdent,
                    Type = SystemTypes.UInt64.GetArrayType(1),
                    Initializer = new ConstructArray {
                        ElementType = SystemTypes.UInt64,
                        Rank = 1,
                        Operands = new ExpressionList(Literal.Int32One),
                        Initializers = new ExpressionList(new Literal(2048 /* FIXME */, SystemTypes.UInt64))
                    }
                });
                Expression localSize = new ConstructArray {
                    ElementType = SystemTypes.UInt64,
                    Rank = 1,
                    Operands = new ExpressionList(Literal.Int32One),
                    Initializers = new ExpressionList(new Literal(64 /* FIXME */, SystemTypes.UInt64))
                };
                // localSize is 64 elements
                Expression localSizeMem = new BinaryExpression {
                    NodeType = NodeType.Mul,
                    Operand1 = new Literal(64 /* FIXME */, SystemTypes.UInt64),
                    Operand2 = new Literal(arg.Type.ScalarType.ByteSize, SystemTypes.UInt64)
                };
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetSizeMultiplicationClosure(kernelSizeIdentifier, arg.Type.Rank));
                ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index++, localSizeMem);
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }
                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalSizeIdent, localSize, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };
            String completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
            ConversionResult firstStage = new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                arg.Type,
                kernelSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
            arg = ConversionHelper.GenerateKernelExecution(state, firstStage, null);
            #endregion

            #region // 2nd stage
            // store new size in variable
            kernelIndex = state.GetNextKernelIndex();
            kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            // get problem global size
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(new AssignmentStatement {
                Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                Source = new BinaryExpression {
                    NodeType = NodeType.Div,
                    Operand1 = new Literal(2048 /* FIXME */, SystemTypes.UInt64),
                    Operand2 = new Literal(64 /* FIXME */, SystemTypes.UInt64)
                }
            });
            arguments = arg.KernelArguments;
            kernelSource = ComputeKernelTemplates.pps2;
            kernelSource = kernelSource.Replace("{type}", arg.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", arg.AccessExpression);
            kernelSource = kernelSource.Replace("{operation}", methodStruct.Operation);
            kernelSource = kernelSource.Replace("{identity}", methodStruct.Identity);

            waitHandles = arg.WaitHandles;

            generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                //ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index++, localSize);
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }
                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, kernelSizeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };
            completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                arg.Type,
                kernelSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
            #endregion
        }

        public static ConversionResult ConvertApply(ConversionState state, ConversionResult arg, MethodStruct methodStruct, Identifier receiveBufferIdentifier) {
            if (arg.HasComplexIndexer) {
                #region // argument has a complex indexer
                // store new size in variable
                Int32 kernelIndex = state.GetNextKernelIndex();
                Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
                // get problem global size
                ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
                state.Constructor.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 1)
                );
                if (arg.Type.Rank == 2) {
                    state.Constructor.Body.Statements.Add(new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                        Source = new BinaryExpression {
                            NodeType = NodeType.Mul,
                            Operand1 = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 0),
                            Operand2 = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 1)
                        }
                    });
                } else {
                    state.Constructor.Body.Statements.Add(new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                        Source = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 0)
                    });
                }
                Dictionary<String, KernelArgumentDetails> arguments = arg.KernelArguments;
                String kernelSource = ComputeKernelTemplates.apply_indexer;
                kernelSource = kernelSource.Replace("{type}", arg.Type.ScalarType.OpenCLTypeName);
                kernelSource = kernelSource.Replace("{expression}", arg.AccessExpression);
                kernelSource = kernelSource.Replace("{operation}", methodStruct.Operation);

                List<Identifier> waitHandles = arg.WaitHandles;
                
                Identifier kernelGlobalRangeIdentifier = Identifier.For(String.Format("kernel{0}GlobalRange", kernelIndex));
                
                Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                    Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
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
                    ConversionHelper.GenerateSizeFromBufferIdentifier(state, state.CodeletMethod, String.Format("_buffer{0}", arg.ArgumentIndex), kernelGlobalRangeIdentifier);
                    // get kernel
                    ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                    // set kernel arguments
                    Int32 index = 0;
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                    foreach (var buffer in buffers) {
                        ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                        index += 1;
                        #region Complex Indexer
                        if (arg.HasComplexIndexer) {
                            ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}n", buffer.Name)));
                            index++;
                            for (int dimension = 0; dimension < 2 /* FIXME */; dimension++) {
                                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}from{1}", buffer.Name, dimension)));
                                index++;
                                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, Identifier.For(String.Format("_{0}by{1}", buffer.Name, dimension)));
                                index++;
                            }
                        }
                        #endregion
                    }
                    Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                    ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                    Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                    ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, kernelGlobalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                    ConversionHelper.GenerateFlushCommandQueue(state);
                    return waitHandleIdentifier;
                };
                String completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
                return new ConversionResult(
                    new KernelDetails(kernelSource, generate),
                    "value",
                    arg.Type,
                    kernelGlobalRangeIdentifier,
                    arguments,
                    new List<Identifier>(),
                    completeExpression
                );
                #endregion
            } else {
                // store new size in variable
                Int32 kernelIndex = state.GetNextKernelIndex();
                Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
                // get problem global size
                ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
                state.Constructor.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 1)
                );
                if (arg.Type.Rank == 2) {
                    state.Constructor.Body.Statements.Add(new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                        Source = new BinaryExpression {
                            NodeType = NodeType.Mul,
                            Operand1 = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 0),
                            Operand2 = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 1)
                        }
                    });
                } else {
                    state.Constructor.Body.Statements.Add(new AssignmentStatement {
                        Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                        Source = NodeHelper.GetConstantIndexerForExpression(arg.SizeIdentifier, 0)
                    });
                }
                Dictionary<String, KernelArgumentDetails> arguments = arg.KernelArguments;
                String kernelSource = methodStruct.KernelSource;
                kernelSource = kernelSource.Replace("{type}", arg.Type.ScalarType.OpenCLTypeName);
                kernelSource = kernelSource.Replace("{expression}", arg.AccessExpression);
                kernelSource = kernelSource.Replace("{operation}", methodStruct.Operation);

                List<Identifier> waitHandles = arg.WaitHandles;

                Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                    Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                    // get kernel
                    ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                    // set kernel arguments
                    Int32 index = 0;
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                    //ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index++, localSize);
                    foreach (var buffer in buffers) {
                        ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                        index += 1;
                    }
                    Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                    ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                    Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                    ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, kernelSizeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                    ConversionHelper.GenerateFlushCommandQueue(state);
                    return waitHandleIdentifier;
                };
                String completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
                return new ConversionResult(
                    new KernelDetails(kernelSource, generate),
                    "value",
                    arg.Type,
                    kernelSizeIdentifier,
                    arguments,
                    new List<Identifier>(),
                    completeExpression
                );
            }
        }
        public static ConversionResult ConvertEval(ConversionState state, ConversionResult arg, MethodStruct methodStruct, Identifier receiveBufferIdentifier) {
            String completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
            return new ConversionResult(
                arg.KernelDetails,
                arg.AccessExpression,
                arg.Type,
                arg.SizeIdentifier,
                arg.KernelArguments,
                arg.WaitHandles,
                completeExpression
            );
        }

        public static ConversionResult ConvertDummy(ConversionState state, ConversionResult arg, MethodStruct methodStruct, Identifier receiveBufferIdentifier) {

            Dictionary<String, KernelArgumentDetails> arguments = arg.KernelArguments;
            String kernelSource = ComputeKernelTemplates.ElementWiseCopy;
            kernelSource = kernelSource.Replace("{type}", arg.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", arg.AccessExpression);
            
            List<Identifier> waitHandles = arg.WaitHandles;

            String completeExpression = string.Format("{0}({1})", methodStruct.Name, arg.CompleteExpression);
            return new ConversionResult(
                arg.KernelDetails,
                arg.AccessExpression,
                arg.Type,
                arg.SizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
        }
    }
}
