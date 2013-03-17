namespace ETH.Zonnon.Compute {
    using System;
    using System.Collections.Generic;
    using System.Compiler;
    using System.Diagnostics;
    using System.Linq;

    struct ExpressionArgument {
        public Int32 Index { get; private set; }
        public List<Expression[]> Indexers { get; private set; }
        public INDEXER ComplexIndexer { get; private set; }
        public Boolean IsCall { get { return CallRank >= 0; } }
        public Int32 CallRank { get; private set; }

        public ExpressionArgument(Int32 index, Expression[] indices, INDEXER indexer, Int32 callRank)
            : this() {
            Index = index;
            Indexers = new List<Expression[]>();
            Indexers.Add(indices);
            ComplexIndexer = indexer;
            CallRank = callRank;
        }
    }

    class ConversionState {
        public Class Class { get; private set; }
        public Dictionary<String, ExpressionArgument> Arguments { get; private set; }
        Int32 _tempSizeCount;
        Int32 _kernelCount;
        Int32 _tempBufferCount;
        public InstanceInitializer Constructor { get; private set; }
        public Method CodeletMethod { get; private set; }
        public Method CompleteMethod { get; private set; }
        public Statement ResultStatement { get; private set; }

        public ConversionState() {
            Arguments = new Dictionary<String, ExpressionArgument>();
            Class = new Class();
            Class.Members.Add(
                new Field {
                    Name = Identifier.For("CompleteExpression"),
                    Type = SystemTypes.String,
                    Flags = FieldFlags.Static,
                    DeclaringType = Class,
                }
            );
            Constructor = new InstanceInitializer {
                Body = new Block {
                    Statements = new StatementList(
                        new ExpressionStatement {
                            Expression = new MethodCall(new QualifiedIdentifier(new Base(), StandardIds.Ctor), null, NodeType.Call)
                        }
                    ),
                },
                DeclaringType = Class,
                Flags = MethodFlags.Public | MethodFlags.SpecialName | MethodFlags.RTSpecialName,
                Parameters = new ParameterList(),
                ReturnType = SystemTypes.Void,
                CallingConvention = CallingConventionFlags.HasThis,
            };
            Class.Members.Add(Constructor);
            CodeletMethod = new Method {
                Name = Identifier.For("StartOpenCL"),
                Parameters = new ParameterList(
                    new Parameter {
                        Name = Identifier.For("device"),
                        Type = STANDARD.OpenCLComputeDevice,
                    }
                ),
                ReturnType = SystemTypes.Object,
                Body = new Block {
                    Statements = new StatementList(),
                    HasLocals = true,
                },
                DeclaringType = Class,
                CallingConvention = CallingConventionFlags.HasThis,
            };
            Class.Members.Add(CodeletMethod);
            CompleteMethod = new Method {
                Name = Identifier.For("CompleteOpenCL"),
                Parameters = new ParameterList(
                    new Parameter {
                        Name = Identifier.For("_"),
                        Type = STANDARD.EventObject,
                    }
                ),
                ReturnType = SystemTypes.Void,
                Body = new Block {
                    Statements = new StatementList(),
                    HasLocals = true,
                },
                DeclaringType = Class,
                CallingConvention = CallingConventionFlags.HasThis,
            };
            Class.Members.Add(CompleteMethod);
            //ConversionHelper.GenerateConsoleWriteLine(CompleteMethod, "COMPLETE METHOD");
        }

        public Int32 GetNextKernelIndex() {
            Int32 result = _kernelCount;
            _kernelCount += 1;
            return result;
        }

        internal int GetNextTempBufferIndex() {
            Int32 result = _tempBufferCount;
            _tempBufferCount += 1;
            return result;
        }

        internal Identifier GetNextTempSizeIdentifier() {
            Int32 result = _tempSizeCount;
            _tempSizeCount += 1;
            return Identifier.For("tempsize" + result.ToString());
        }
    }

    static class ExpressionConverter {
        static List<Class> _generatedClasses = new List<Class>();

        public static void SetNamespace(Namespace space) {
            foreach (Class c in _generatedClasses) {
                c.Namespace = space.Name;
                space.Types.Add(c);
                c.DeclaringModule = CONTEXT.symbolTable;
                CONTEXT.symbolTable.Types.Add(c);
            }
        }

        static String ReplaceTemplatePlaceholder(String kernel, String current, String other) {
            return kernel.Replace("{" + current + "}", "{" + other + "}");
        }

        public static ConversionResult? Convert(ConversionState state, NODE node) {
            return Convert(state, node, false, false);
        }

        public static ConversionResult? Convert(ConversionState state, NODE node, bool withScalars) {
            return Convert(state, node, withScalars, false);
        }

        public static ConversionResult? Convert(ConversionState state, NODE node, bool withScalars, bool withIndexer) {
            LITERAL literal;
            if (node.Is(out literal)) {
                return ConvertLiteral(state, literal);
            }
            INSTANCE instance;
            if (node.Is(out instance)) {
                return ConvertInstance(state, instance, null, withScalars);
            }
            CALL call;
            if (node.Is(out call)) {
                MethodCall m = call.convert() as MethodCall;
                if (m == null) return null;
                return ConvertCall(state, call, m);
            }
            INDEXER indexer;
            if (node.Is(out indexer)) {
                if (indexer.left_part.Is(out instance)) {
                    return ConvertInstance(state, instance, indexer, withScalars, withIndexer);
                }
            }
            UNARY unary;
            if (node.Is(out unary)) {
                ConversionResult? leftResult = Convert(state, unary.operand);
                if (leftResult != null) {
                    ConversionResult left = leftResult.Value;
                    UNARY_MINUS minus = unary as UNARY_MINUS;
                    if (minus != null) {
                        return ConvertElementWiseUnary(state, "-", "-", left);
                    }
                    TRANSPOSE transpose = unary as TRANSPOSE;
                    if (transpose != null) {
                        if (left.Type.Rank == 2) {
                            return ConvertMatrixTranspose(state, left);
                        } else {
                            return null;
                        }
                    }
                }
            }
            BINARY binary;
            if (node.Is(out binary)) {
                ConversionResult? leftResult = Convert(state, binary.left_operand, true);
                ConversionResult? rightResult = Convert(state, binary.right_operand, true);
                if (leftResult != null && rightResult != null) {
                    ConversionResult left = leftResult.Value;
                    ConversionResult right = rightResult.Value;
                    if (left.Type.ScalarType == right.Type.ScalarType) {
                        PLUS plus = node as PLUS;
                        if (plus != null) {
                            return ConvertElementWiseBinary(state, "+", "+", left, right);
                        }
                        MINUS minus = node as MINUS;
                        if (minus != null) {
                            return ConvertElementWiseBinary(state, "-", "-", left, right);
                        }
                        MULTIPLY_ELEMENTWISE multiplyElementwise = node as MULTIPLY_ELEMENTWISE;
                        if (multiplyElementwise != null) {
                            return ConvertElementWiseBinary(state, "*", ".*", left, right);
                        }
                        DIVIDE_ELEMENTWISE divideElementwise = node as DIVIDE_ELEMENTWISE;
                        if (divideElementwise != null) {
                            return ConvertElementWiseBinary(state, "/", "./", left, right);
                        }
                        MULTIPLY multiply = node as MULTIPLY;
                        if (multiply != null) {
                            if (left.Type.Rank == 2 && right.Type.Rank == 2) {
                                return ConvertMatrixMatrixMultiplication(state, left, right);
                            } else if (left.Type.Rank == 2 && right.Type.Rank == 1) {
                                return ConvertMatrixVectorMultiplication(state, left, right);
                            } else if (left.Type.Rank == 1 && right.Type.Rank == 2) {
                                return ConvertVectorMatrixMultiplication(state, left, right);
                            } else if (left.Type.Rank == 0 || right.Type.Rank == 0) {
                                return ConvertElementWiseBinary(state, "*", "*", left, right);
                                //} else if (left.Type.Rank == 0 && right.Type.Rank == 0) {
                                //    return null;
                                //} else if (left.Type.Rank == 0) {
                                //    return ConvertScalarArrayMultiplication(state, left, right);
                            } else {
                                return null;
                            }
                        }
                        /*DIVIDE divide = node as DIVIDE;
                        if (divide != null) {
                            if (left.Type.Rank == 2 && right.Type.Rank == 2) {
                                return ConvertMatrixMatrixMultiplication(state, left, right);
                            } else {
                                return null;
                            }
                        }*/
                        LEFTDIVISION leftDivision = node as LEFTDIVISION;
                        if (leftDivision != null) {
                            if (left.Type.Rank == 2 && right.Type.Rank == 1) {
                                return ConvertMatrixVectorLeftDivision(state, left, right);
                            } else {
                                return null;
                            }
                        }
                        LESS less = node as LESS;
                        if (less != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, "<", left, right);
                            }
                        }
                        LESS_EQUAL lesseq = node as LESS_EQUAL;
                        if (lesseq != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, "<=", left, right);
                            }
                        }
                        GREATER greater = node as GREATER;
                        if (greater != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, ">", left, right);
                            }
                        }
                        GREATER_EQUAL greatereq = node as GREATER_EQUAL;
                        if (greatereq != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, ">=", left, right);
                            }
                        }
                        EQUAL equal = node as EQUAL;
                        if (equal != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, "==", left, right);
                            }
                        }
                        NON_EQUAL nonequal = node as NON_EQUAL;
                        if (nonequal != null) {
                            if ((left.Type.Rank == 0 || right.Type.Rank == 0) && left.Type.Rank != right.Type.Rank) {
                                return ConvertElementWiseBoolOp(state, "!=", left, right);
                            }
                        }
                        PSEUDO_SCALAR_PRODUCT scalarProduct = node as PSEUDO_SCALAR_PRODUCT;
                        if (scalarProduct != null) {
                            return ConvertPseudoScalarProduct(state, left, right);
                        }
                        //DIVIDE divide = node as DIVIDE;
                        //if (divide != null && left.Type.Rank == 0 && right.Type.Rank == 0) {
                        //    return ConvertElementWiseBinary(state, "/", "/", left, right);
                        //}
                    }
                } else if (leftResult != null) {
                    // right side is integer
                    ConversionResult left = leftResult.Value;
                    if (binary.right_operand is INSTANCE) {
                        rightResult = ConvertScalarInstance(state, binary.right_operand as INSTANCE, null);
                    }
                    EXPONENT exponent = node as EXPONENT;
                    if (exponent != null && binary.right_operand is INTEGER_LITERAL) {
                        if (left.Type.Rank == 2) {
                            return ConvertExponent(state, left, (binary.right_operand as INTEGER_LITERAL).integer);
                        } else {
                            return null;
                        }
                    }
                } else {
                    // left side is integer
                }

            }
            return null;
        }

        private static ConversionResult? ConvertElementWiseBoolOp(ConversionState state, String op, ConversionResult left, ConversionResult right) {
            Debug.Assert(left.Type.Rank == 0 || right.Type.Rank == 0);
            ComputeType resultType;
            Identifier resultSizeIdentifier;
            ConversionResult matrix = left.Type.Rank != 0 ? left : right;
            ConversionResult scalar = left.Type.Rank == 0 ? left : right;
            resultType = matrix.Type;
            resultSizeIdentifier = matrix.SizeIdentifier;
            // handle kernels
            if (matrix.KernelDetails != null) {
                matrix = ConversionHelper.GenerateKernelExecution(state, matrix, null);
            }
            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            if (right.KernelDetails != null) {
                rightStrings.Add(right.KernelDetails.Value.Source);
            }
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, false);
            String accessExpression = String.Format("({0} {1} {2})", left.AccessExpression, op, rightStrings[0]);
            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, op, right.CompleteExpression);
            
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 1)
            );
            if (matrix.Type.Rank == 2) {
                state.Constructor.Body.Statements.Add(new AssignmentStatement {
                    Target = NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0),
                    Source = new BinaryExpression {
                        NodeType = NodeType.Mul,
                        Operand1 = NodeHelper.GetConstantIndexerForExpression(matrix.SizeIdentifier, 0),
                        Operand2 = NodeHelper.GetConstantIndexerForExpression(matrix.SizeIdentifier, 1)
                    }
                });
            } else {
                state.Constructor.Body.Statements.Add(NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, matrix.SizeIdentifier, 0));
            }
            String kernelSource = ComputeKernelTemplates.all;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", accessExpression);
            
            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                index += 1;
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
            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                accessExpression,
                resultType,
                resultSizeIdentifier,
                arguments,
                waitHandles,
                completeExpression
            );
        }

        //private static ConversionResult? ConvertScalarArrayMultiplication(ConversionState state, ConversionResult left, ConversionResult right) {
        //    if (left.KernelDetails != null) {
        //        left = ConversionHelper.GenerateKernelExecution(state, left, null);
        //    }
        //    List<String> rightStrings = new List<String>();
        //    rightStrings.Add(right.AccessExpression);
        //    if (right.KernelDetails != null) {
        //        rightStrings.Add(right.KernelDetails.Value.Source);
        //    }
        //    Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, false);
        //    String accessExpression = String.Format("({0} {1} {2})", left.AccessExpression, "*", rightStrings[0]);
        //    String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "*", right.CompleteExpression);
        //    KernelDetails? kernelDetails;
        //    if (rightStrings.Count > 1) {
        //        kernelDetails = new KernelDetails(rightStrings[1], right.KernelDetails.Value.Generate);
        //    } else {
        //        kernelDetails = left.KernelDetails;
        //    }
        //    List<Identifier> waitHandles = left.WaitHandles;
        //    waitHandles.AddRange(right.WaitHandles);
        //    return new ConversionResult(kernelDetails, accessExpression, right.Type, right.SizeIdentifier, arguments, waitHandles, completeExpression);
        //}

        static ConversionResult? ConvertCall(ConversionState state, CALL call, MethodCall mcall) {
            ComputeType mathType;
            if (!ComputeType.FromType(call.type).TryGetValue(out mathType)) {
                mathType = new ComputeType(ComputeScalarType.Single, 0);
            }
            
            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(null, ref mathType);
            if (indices == null) {
                return null;
            }
            String name = call.Name.Substring(0, call.Name.IndexOf('('));
            ExpressionArgument receiverArgument;
            Identifier sizeIdentifier = null;

            Expression[] expr = { mcall };
            
            receiverArgument = new ExpressionArgument(state.Arguments.Count, expr, null, mathType.Rank);
            // useless unique value
            state.Arguments.Add("#"+CONTEXT.last_temp_no, receiverArgument);

            Identifier constructorParameterIdentifier = Identifier.For("data" + receiverArgument.Index.ToString());
            // check if null
            state.Constructor.Body.Statements.Add(
                new If {
                    Condition = new BinaryExpression {
                        NodeType = NodeType.Eq,
                        Operand1 = constructorParameterIdentifier,
                        Operand2 = Literal.Null
                    },
                    TrueBlock = new Block {
                        Statements = new StatementList(
                            NodeHelper.GetThrowFor(SystemTypes.ArgumentNullException)
                        )
                    }
                }
            );
            // get dimensions and store in field
            sizeIdentifier = Identifier.For("size" + receiverArgument.Index.ToString());

            state.Constructor.Body.Statements.Add(
                new VariableDeclaration {
                    Name = sizeIdentifier,
                    Type = SystemTypes.UInt64.GetArrayType(1),
                    Initializer = new MethodCall {
                        Callee = new MemberBinding {
                            TargetObject = constructorParameterIdentifier,
                            BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                        }
                    },
                }
            );
            
            String variableName = "buffer" + receiverArgument.Index.ToString();
            Dictionary<String, KernelArgumentDetails> kernelArguments = new Dictionary<String, KernelArgumentDetails>();
            kernelArguments.Add(variableName, new KernelArgumentDetails(0, mathType, true));
            String completeExpression;
            String accessExpression;

            completeExpression = ConversionHelper.FormatIdentifierExpression(mathType, indices, receiverArgument.Index);
            accessExpression = "Access({0})";

            return new ConversionResult(
                null,
                accessExpression,
                mathType,
                sizeIdentifier,
                kernelArguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertInstance(ConversionState state, INSTANCE instance, INDEXER indexer) {
            return ConvertInstance(state, instance, indexer, false);
        }

        static ConversionResult? ConvertInstance(ConversionState state, INSTANCE instance, INDEXER indexer, bool withScalars) {
            return ConvertInstance(state, instance, indexer, withScalars, false);
        }

        static ConversionResult? ConvertInstance(ConversionState state, INSTANCE instance, INDEXER indexer, bool withScalars, bool withIndexers) {
            ComputeType mathType;
            if (!ComputeType.FromType(instance.type).TryGetValue(out mathType)) {
                if (withScalars) {
                    return ConvertScalarInstance(state, instance, indexer);
                } else {
                    return null;
                }
            }
            ComputeType computeType = mathType;
            bool complexIndexer;
            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(indexer, ref mathType, out complexIndexer);
            if (indices == null && complexIndexer) {
                mathType = computeType;
                indices = new Expression[computeType.Rank];
            } else if (indices == null) {
                return null;
            }
            ExpressionArgument receiverArgument;
            Identifier sizeIdentifier = null;
            if (state.Arguments.TryGetValue(instance.name.ToString(), out receiverArgument)) {
                sizeIdentifier = Identifier.For("size" + receiverArgument.Index.ToString());
                // ckeck and generate check if both indices represent the same
                if (!ConversionHelper.GenerateNonFirstIndexerCheckAndAddItToArgument(state.Constructor, receiverArgument, indices)) {
                    return null;
                }
            } else {
                // receiver is used the first time in this expression
                receiverArgument = new ExpressionArgument(state.Arguments.Count, indices, complexIndexer ? indexer : null, -1);
                state.Arguments.Add(instance.name.ToString(), receiverArgument);
                Identifier constructorParameterIdentifier = Identifier.For("data" + receiverArgument.Index.ToString());
                // check if null
                state.Constructor.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Eq,
                            Operand1 = constructorParameterIdentifier,
                            Operand2 = Literal.Null
                        },
                        TrueBlock = new Block {
                            Statements = new StatementList(
                                NodeHelper.GetThrowFor(SystemTypes.ArgumentNullException)
                            )
                        }
                    }
                );
                // get dimensions and store in field
                sizeIdentifier = Identifier.For("size" + receiverArgument.Index.ToString());
                if (!indices.All(index => index == null)) {
                    // only take relevant dimensions according to indexer
                    Identifier indexerSizeIdentifier = Identifier.For("size" + receiverArgument.Index.ToString() + "notindexed");
                    state.Constructor.Body.Statements.Add(
                        new VariableDeclaration {
                            Name = indexerSizeIdentifier,
                            Type = SystemTypes.UInt64.GetArrayType(1),
                            Initializer = new MethodCall {
                                Callee = new MemberBinding {
                                    TargetObject = constructorParameterIdentifier,
                                    BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                                }
                            },
                        }
                    );
                    state.Constructor.Body.Statements.Add(
                        new VariableDeclaration {
                            Name = sizeIdentifier,
                            Type = SystemTypes.UInt64.GetArrayType(1),
                            Initializer = new ConstructArray {
                                ElementType = SystemTypes.UInt64,
                                Rank = 1,
                                Operands = new ExpressionList(
                                    new Literal(mathType.Rank, SystemTypes.Int32)
                                ),
                            }
                        }
                    );
                    // assign corresponding non-indexed values
                    Int32 sizeIndex = 0;
                    for (Int32 indexerIndex = 0; indexerIndex < indices.Length; indexerIndex += 1) {
                        if (indices[indexerIndex] == null) {
                            state.Constructor.Body.Statements.Add(
                                new AssignmentStatement {
                                    Target = NodeHelper.GetConstantIndexerForExpression(sizeIdentifier, sizeIndex),
                                    Source = NodeHelper.GetConstantIndexerForExpression(indexerSizeIdentifier, indexerIndex),
                                }
                            );
                            sizeIndex += 1;
                        }
                    }
                } else {
                    // no indexer, can assign directly from GetDimensions
                    state.Constructor.Body.Statements.Add(
                        new VariableDeclaration {
                            Name = sizeIdentifier,
                            Type = SystemTypes.UInt64.GetArrayType(1),
                            Initializer = new MethodCall {
                                Callee = new MemberBinding {
                                    TargetObject = constructorParameterIdentifier,
                                    BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                                }
                            },
                        }
                    );
                }
            }
            String variableName = "buffer" + receiverArgument.Index.ToString();
            Dictionary<String, KernelArgumentDetails> kernelArguments = new Dictionary<String, KernelArgumentDetails>();
            kernelArguments.Add(variableName, new KernelArgumentDetails(0, mathType, true));
            String completeExpression;
            String accessExpression;

            completeExpression = ConversionHelper.FormatIdentifierExpression(mathType, indices, receiverArgument.Index, complexIndexer);
            accessExpression = "Access({0})";

            Int32? complexIndex = null;
            if (complexIndexer) complexIndex = receiverArgument.Index;

            return new ConversionResult(
                null,
                accessExpression,
                mathType,
                sizeIdentifier,
                kernelArguments,
                new List<Identifier>(),
                completeExpression,
                complexIndex
            );
        }

        static ConversionResult? ConvertLiteral(ConversionState state, LITERAL literal) {
            ComputeType mathType = new ComputeType(ComputeScalarType.Single, 0);

            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(null, ref mathType);
            if (indices == null) {
                return null;
            }
            ExpressionArgument receiverArgument;
            Identifier sizeIdentifier = null;

            if (literal is REAL_LITERAL) {
                Expression[] expr = { (literal as LITERAL).convert() as Expression };
                receiverArgument = new ExpressionArgument(state.Arguments.Count, expr, null, 0);
            } else {
                return null;
            }
            state.Arguments.Add(String.Format("literal_{0}", CONTEXT.last_temp_no), receiverArgument);

            Identifier constructorParameterIdentifier = Identifier.For("scalar" + receiverArgument.Index.ToString());

            String variableName = "_scalar" + receiverArgument.Index.ToString();
            Dictionary<String, KernelArgumentDetails> kernelArguments = new Dictionary<String, KernelArgumentDetails>();
            kernelArguments.Add(variableName, new KernelArgumentDetails(0, mathType, true));
            String completeExpression;
            String accessExpression;

            completeExpression = ConversionHelper.FormatIdentifierExpression(mathType, indices, receiverArgument.Index, false, true);
            accessExpression = "({0})";

            return new ConversionResult(
                null,
                accessExpression,
                mathType,
                sizeIdentifier,
                kernelArguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertScalarInstance(ConversionState state, INSTANCE instance, INDEXER indexer) {
            ComputeType mathType = new ComputeType(ComputeScalarType.Single, 0);

            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(indexer, ref mathType);
            if (indices == null) {
                return null;
            }
            ExpressionArgument receiverArgument;
            Identifier sizeIdentifier = null;

            if (state.Arguments.TryGetValue(instance.name.ToString(), out receiverArgument)) {

            } else {
                // receiver is used the first time in this expression
                if (instance.entity is CONSTANT_DECL) {
                    Expression[] expr = { (instance.entity as CONSTANT_DECL).initializer.convert() as Expression };
                    receiverArgument = new ExpressionArgument(state.Arguments.Count, expr, null, 0);
                } else {
                    receiverArgument = new ExpressionArgument(state.Arguments.Count, indices, null, -1);
                }
                state.Arguments.Add(instance.name.ToString(), receiverArgument);
            }
            Identifier constructorParameterIdentifier = Identifier.For("scalar" + receiverArgument.Index.ToString());

            String variableName = "_scalar" + receiverArgument.Index.ToString();
            Dictionary<String, KernelArgumentDetails> kernelArguments = new Dictionary<String, KernelArgumentDetails>();
            kernelArguments.Add(variableName, new KernelArgumentDetails(0, mathType, true));
            String completeExpression;
            String accessExpression;

            completeExpression = ConversionHelper.FormatIdentifierExpression(mathType, indices, receiverArgument.Index, false, true);
            accessExpression = "({0})";

            return new ConversionResult(
                null,
                accessExpression,
                mathType,
                sizeIdentifier,
                kernelArguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertMatrixTranspose(ConversionState state, ConversionResult left) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            Debug.Assert(left.KernelDetails == null);
            // store new size in variable
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, left.SizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 1, left.SizeIdentifier, 0)
            );
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            Dictionary<String, KernelArgumentDetails> arguments = left.KernelArguments;
            String kernelSource = ComputeKernelTemplates.MatrixTranspose;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            
            List<Identifier> waitHandles = left.WaitHandles;
            
            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                // get work item size
                //Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                //ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, globalRangeIdentifier, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0), NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 1));
                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                while (index < 2) {
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, index));
                    index += 1;
                }
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

            String completeExpression = String.Format("({1} {0})", left.CompleteExpression, "!");
            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                resultSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertElementWiseUnary(ConversionState state, String op, String zonnonOp, ConversionResult left) {
            ComputeType resultType;
            Identifier resultSizeIdentifier;
            resultType = left.Type;
            resultSizeIdentifier = left.SizeIdentifier;
            // handle kernels
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            Dictionary<String, KernelArgumentDetails> arguments = left.KernelArguments;
            String accessExpression = String.Format("({1} {0})", left.AccessExpression, op);
            String completeExpression = String.Format("({1} {0})", left.CompleteExpression, zonnonOp);
            KernelDetails? kernelDetails = left.KernelDetails;
            List<Identifier> waitHandles = left.WaitHandles;
            return new ConversionResult(
                kernelDetails,
                accessExpression,
                resultType,
                resultSizeIdentifier,
                arguments,
                waitHandles,
                completeExpression
            );
        }

        static ConversionResult? ConvertElementWiseBinary(ConversionState state, String op, String zonnonOp, ConversionResult left, ConversionResult right) {
            ComputeType resultType;
            Identifier resultSizeIdentifier;
            Debug.Assert(left.Type.Rank == 0 || right.Type.Rank == 0 || left.Type.Rank == right.Type.Rank);
            resultType = left.Type.Rank != 0 ? left.Type : right.Type;
            resultSizeIdentifier = left.SizeIdentifier != null ? left.SizeIdentifier : right.SizeIdentifier;
            if (left.Type.Rank == right.Type.Rank) state.Constructor.Body.Statements.Add(NodeHelper.GetThrowIfDimensionsNotEqual(left.SizeIdentifier, right.SizeIdentifier));
            // handle kernels
            if (left.KernelDetails != null && right.KernelDetails != null) {
                // both sides already contain a kernel. one of them has to be generated.
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            if (right.KernelDetails != null) {
                rightStrings.Add(right.KernelDetails.Value.Source);
            }
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, false);
            String accessExpression = String.Format("({0} {1} {2})", left.AccessExpression, op, rightStrings[0]);
            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, zonnonOp, right.CompleteExpression);
            KernelDetails? kernelDetails;
            if (rightStrings.Count > 1) {
                kernelDetails = new KernelDetails(rightStrings[1], right.KernelDetails.Value.Generate);
            } else {
                kernelDetails = left.KernelDetails;
            }
            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);
            return new ConversionResult(
                kernelDetails,
                accessExpression,
                resultType,
                resultSizeIdentifier,
                arguments,
                waitHandles,
                completeExpression
            );
        }

        static ConversionResult? ConvertPseudoScalarProduct(ConversionState state, ConversionResult left, ConversionResult right) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            if (right.KernelDetails != null) {
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            Debug.Assert(left.KernelDetails == null);
            Debug.Assert(right.KernelDetails == null);
            // check sizes
            if (left.Type.Rank != right.Type.Rank) {
                return null;
            }
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetThrowIfDimensionsNotEqual(left.SizeIdentifier, right.SizeIdentifier)
            );
            #region // 1st stage
            // store new size in variable
            Int32 kernelIndex = state.GetNextKernelIndex();
            // get problem global size
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, left.Type.Rank)
            );
            for (Int32 index = 0; index < left.Type.Rank; index++) {
                state.Constructor.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, index, left.SizeIdentifier, index)
                );
            }

            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);

            String kernelSource = ComputeKernelTemplates.pps1;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", String.Format("({0})*({1})", left.AccessExpression, rightStrings[0]));
            kernelSource = kernelSource.Replace("{operation}", "mine + other");
            kernelSource = kernelSource.Replace("{identity}", "0");

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

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
                    Operand2 = new Literal(left.Type.ScalarType.ByteSize, SystemTypes.UInt64)
                };
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetSizeMultiplicationClosure(kernelSizeIdentifier, left.Type.Rank));
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
            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "+*", right.CompleteExpression);

            ConversionResult firstStage = new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                kernelSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
            ConversionResult arg = ConversionHelper.GenerateKernelExecution(state, firstStage, null);
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
            kernelSource = kernelSource.Replace("{operation}", "mine + other");
            kernelSource = kernelSource.Replace("{identity}", "0");

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

        //static ConversionResult? ConvertScalarProduct(ConversionState state, ConversionResult left, ConversionResult right) {
        //    if (left.KernelDetails != null) {
        //        left = ConversionHelper.GenerateKernelExecution(state, left, null);
        //    }
        //    if (right.KernelDetails != null) {
        //        right = ConversionHelper.GenerateKernelExecution(state, right, null);
        //    }
        //    Debug.Assert(left.KernelDetails == null);
        //    Debug.Assert(right.KernelDetails == null);
        //    // check sizes
        //    if (left.Type.Rank != right.Type.Rank) {
        //        return null;
        //    }
        //    state.Constructor.Body.Statements.Add(
        //        NodeHelper.GetThrowIfDimensionsNotEqual(left.SizeIdentifier, right.SizeIdentifier)
        //    );
        //    // store size argument to scalar product multiplication kernel in field
        //    Int32 kernelIndex = state.GetNextKernelIndex();
        //    Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
        //    ConversionHelper.GenerateKernelSizeField(state, kernelSizeIdentifier);
        //    state.Constructor.Body.Statements.Add(
        //        new AssignmentStatement {
        //            Target = kernelSizeIdentifier,
        //            Source = NodeHelper.GetSizeMultiplicationClosure(left.SizeIdentifier, left.Type.Rank),
        //        }
        //    );
        //    List<String> rightStrings = new List<String>();
        //    rightStrings.Add(right.AccessExpression);
        //    Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);
        //    String kernelSource = ComputeKernelTemplates.Reduction;
        //    kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
        //    kernelSource = kernelSource.Replace("{reduction}", "(a + b)");
        //    kernelSource = kernelSource.Replace("{identity}", "0");
        //    kernelSource = kernelSource.Replace("{expression}", "({leftExpression} * {rightExpression})");
        //    kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
        //    kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);

        //    List<Identifier> waitHandles = left.WaitHandles;
        //    waitHandles.AddRange(right.WaitHandles);

        //    Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
        //        Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
        //        // get work item sizes
        //        Identifier kernelReductionSizeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "ReductionSize");
        //        state.CodeletMethod.Body.Statements.Add(
        //            new VariableDeclaration {
        //                Name = kernelReductionSizeIdentifier,
        //                Type = SystemTypes.UInt64,
        //                Initializer = new MethodCall {
        //                    Callee = new MemberBinding {
        //                        TargetObject = Identifier.For("device"),
        //                        BoundMember = STANDARD.OpenCLComputeDevice.GetMethod(Identifier.For("GetReductionSize"), SystemTypes.UInt64),
        //                    },
        //                    Operands = new ExpressionList(
        //                        kernelSizeIdentifier
        //                    )
        //                }
        //            }
        //        );
        //        Identifier kernelRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "Range");
        //        ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, kernelRangeIdentifier, SystemTypes.UInt64, 1);
        //        state.CodeletMethod.Body.Statements.Add(
        //            new AssignmentStatement {
        //                Target = NodeHelper.GetConstantIndexerForExpression(kernelRangeIdentifier, 0),
        //                Source = kernelReductionSizeIdentifier,
        //            }
        //        );
        //        // get kernel
        //        ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
        //        // set kernel arguments
        //        ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, 0, kernelSizeIdentifier);
        //        ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, 1,
        //            new BinaryExpression {
        //                NodeType = NodeType.Mul,
        //                Operand1 = new Literal(left.Type.ScalarType.ByteSize, SystemTypes.Int32),
        //                Operand2 = kernelReductionSizeIdentifier,
        //            }
        //        );
        //        Int32 index = 2;
        //        foreach (var buffer in buffers) {
        //            ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
        //            index += 1;
        //        }
        //        Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
        //        ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
        //        Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
        //        ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, kernelRangeIdentifier, kernelRangeIdentifier, kernelPredecessorsIdentifier);
        //        ConversionHelper.GenerateFlushCommandQueue(state);
        //        return waitHandleIdentifier;
        //    };

        //    String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "+*", right.CompleteExpression);

        //    ComputeType mathType = new ComputeType(left.Type.ScalarType, 0);

        //    return new ConversionResult(
        //        new KernelDetails(kernelSource, generate),
        //        "value",
        //        mathType,
        //        null,
        //        arguments,
        //        new List<Identifier>(),
        //        completeExpression
        //    );
        //}

        static ConversionResult? ConvertExponent(ConversionState state, ConversionResult left, Int64 power) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            Debug.Assert(left.KernelDetails == null);
            // store new size in variable
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            Dictionary<String, KernelArgumentDetails> arguments = left.KernelArguments;
            String kernelSource = ComputeKernelTemplates.MatrixExponent;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{power}", power.ToString());

            List<Identifier> waitHandles = left.WaitHandles;

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());

                // ulong num = 16;
                Identifier localSizeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "LocalSize");
                state.CodeletMethod.Body.Statements.Add(
                    new VariableDeclaration {
                        Name = localSizeIdentifier,
                        Type = SystemTypes.UInt64,
                        Initializer = new Literal(16, SystemTypes.UInt64)
                    }
                );
                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                // ulong[] globalRange = {kernelSize[0], kernelSize[1]};
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 2);
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 0, kernelSizeIdentifier, 0)
                );
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 1, kernelSizeIdentifier, 1)
                );
                // get nearest for x
                state.CodeletMethod.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Ne,
                            Operand1 = new BinaryExpression {
                                NodeType = NodeType.Rem,
                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                Operand2 = localSizeIdentifier
                            },
                            Operand2 = Literal.Int64Zero
                        },
                        TrueBlock = new Block {
                            Statements = new StatementList(
                                new AssignmentStatement {
                                    Target = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                    Source = new BinaryExpression {
                                        NodeType = NodeType.Add,
                                        Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                        Operand2 = new BinaryExpression {
                                            NodeType = NodeType.Sub,
                                            Operand1 = localSizeIdentifier,
                                            Operand2 = new BinaryExpression {
                                                NodeType = NodeType.Rem,
                                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                                Operand2 = localSizeIdentifier
                                            }
                                        }
                                    }
                                }
                            )
                        }
                    }
                );
                // get nearest for y
                state.CodeletMethod.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Ne,
                            Operand1 = new BinaryExpression {
                                NodeType = NodeType.Rem,
                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                Operand2 = localSizeIdentifier
                            },
                            Operand2 = Literal.Int64Zero
                        },
                        TrueBlock = new Block {
                            Statements = new StatementList(
                                new AssignmentStatement {
                                    Target = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                    Source = new BinaryExpression {
                                        NodeType = NodeType.Add,
                                        Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                        Operand2 = new BinaryExpression {
                                            NodeType = NodeType.Sub,
                                            Operand1 = localSizeIdentifier,
                                            Operand2 = new BinaryExpression {
                                                NodeType = NodeType.Rem,
                                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                                Operand2 = localSizeIdentifier
                                            }
                                        }
                                    }
                                }
                            )
                        }
                    }
                );

                // kernelXLocalRange = new UInt64[] { kernelXLocalSize, kernelXLocalSize };
                Identifier localRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "LocalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, localRangeIdentifier, SystemTypes.UInt64, 2);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, localRangeIdentifier, localSizeIdentifier, localSizeIdentifier);

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, index));
                index++;
                while (index <= 2) {
                    Expression localArgumentSizeExpression = new BinaryExpression {
                        NodeType = NodeType.Mul,
                        Operand1 = new Literal(left.Type.ScalarType.ByteSize, SystemTypes.Int32),
                        Operand2 = new BinaryExpression {
                            NodeType = NodeType.Mul,
                            Operand1 = localSizeIdentifier,
                            Operand2 = localSizeIdentifier
                        }
                    };
                    ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index, localArgumentSizeExpression);
                    index++;
                }
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index++;
                }
                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, localRangeIdentifier, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "**", power.ToString());

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                resultSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertVectorMatrixMultiplication(ConversionState state, ConversionResult left, ConversionResult right) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            if (right.KernelDetails != null) {
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            Debug.Assert(left.KernelDetails == null);
            Debug.Assert(right.KernelDetails == null);
            // generate runtime size check
            // if (a[0] != b[0]) throw new ArgumentException();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetThrowIfIndicesNotEqual(left.SizeIdentifier, 0, right.SizeIdentifier, 0)
            );
            // store new size in variable
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, right.SizeIdentifier, 1)
            );
            // store size arguments to matrix multiplication kernel in field
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, right.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, right.SizeIdentifier, 1)
            );
            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);
            String kernelSource = ComputeKernelTemplates.VectorMatrixMultiplication;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                // get work item size
                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetVariableDeclarationForSizeIdentifier(globalRangeIdentifier, 1)
                );
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 0, kernelSizeIdentifier, 1)
                );
                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                while (index < 2) {
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, index));
                    index += 1;
                }
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }
                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "*", right.CompleteExpression);

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                resultSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );

        }

        static ConversionResult? ConvertMatrixVectorLeftDivision(ConversionState state, ConversionResult left, ConversionResult right) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            if (right.KernelDetails != null) {
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            Debug.Assert(left.KernelDetails == null);
            Debug.Assert(right.KernelDetails == null);
            // generate runtime size check
            // if (a[1] != b[0]) throw new ArgumentException();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetThrowIfIndicesNotEqual(left.SizeIdentifier, 1, right.SizeIdentifier, 0)
            );
            // store new size in variable
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            // store size arguments to matrix multiplication kernel in field
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);

            #region // LU decomposition
            String kernelSource = ComputeKernelTemplates.LeftDivision;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Identifier LMatrix;
            ConversionHelper.GenerateTemporaryBuffer(state, left, out LMatrix);
            Identifier UMatrix;
            ConversionHelper.GenerateTemporaryBuffer(state, left, out UMatrix);
            Identifier zVector;
            ConversionHelper.GenerateTemporaryBuffer(state, right, out zVector);
            
            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());

                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 1);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, globalRangeIdentifier, Literal.Int32One);

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, LMatrix);
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, UMatrix);
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, zVector);
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }

                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "\\", right.CompleteExpression);

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                right.Type,
                resultSizeIdentifier,
                arguments,
                waitHandles,
                completeExpression
            );
            #endregion

            /*#region // LU decomposition
            String kernelSource = ComputeKernelTemplates.LU;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", left.AccessExpression);

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Identifier LMatrix;
            ConversionHelper.GenerateTemporaryBuffer(state, left, out LMatrix);
            // UMatrix will be target buffer

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());

                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 1);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, globalRangeIdentifier, Literal.Int32One);

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, LMatrix);
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }

                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "\\", right.CompleteExpression);

            ConversionResult firstStage = new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                left.SizeIdentifier,
                left.KernelArguments,
                left.WaitHandles,
                completeExpression
            );

            left = ConversionHelper.GenerateKernelExecution(state, firstStage, null);
            #endregion

            #region // Solving
            kernelIndex = state.GetNextKernelIndex();
            
            kernelSource = ComputeKernelTemplates.LeftLUDivision;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{expression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);
           
            arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);

            waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Identifier zVector;
            ConversionHelper.GenerateTemporaryBuffer(state, right, out zVector);

            generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());

                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 1);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, globalRangeIdentifier, Literal.Int32One);

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index++, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, 0));
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, zVector);
                ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index++, LMatrix);
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }

                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, Literal.Null, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                right.Type,
                resultSizeIdentifier,
                arguments,
                waitHandles,
                completeExpression
            );
            #endregion
            */
        }


        static ConversionResult? ConvertMatrixVectorMultiplication(ConversionState state, ConversionResult left, ConversionResult right) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            if (right.KernelDetails != null) {
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            Debug.Assert(left.KernelDetails == null);
            Debug.Assert(right.KernelDetails == null);
            // generate runtime size check
            // if (a[1] != b[0]) throw new ArgumentException();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetThrowIfIndicesNotEqual(left.SizeIdentifier, 1, right.SizeIdentifier, 0)
            );
            // store new size in variable
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            // store size arguments to matrix multiplication kernel in field
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);
            String kernelSource = ComputeKernelTemplates.MatrixVectorMultiplication;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                while (index < 2) {
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, index));
                    index += 1;
                }

                // (jb) Local arguments for kernel
                Expression localArgumentSizeExpression = new BinaryExpression {
                    NodeType = NodeType.Mul,
                    Operand1 = new Literal(32, SystemTypes.Int32),
                    Operand2 = new Literal(left.Type.ScalarType.ByteSize, SystemTypes.Int32)
                };
                ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index, localArgumentSizeExpression);
                index += 1;
                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 2);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, globalRangeIdentifier, new Literal(32, SystemTypes.Int32), Literal.Int32Zero);
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 1, kernelSizeIdentifier, 0)
                );
                Identifier localRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "LocalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, localRangeIdentifier, SystemTypes.UInt64, 2);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, localRangeIdentifier, new Literal(32, SystemTypes.Int32), Literal.Int32One);

                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }

                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, localRangeIdentifier, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "*", right.CompleteExpression);

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                right.Type,
                resultSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
        }

        static ConversionResult? ConvertMatrixMatrixMultiplication(ConversionState state, ConversionResult left, ConversionResult right) {
            if (left.KernelDetails != null) {
                left = ConversionHelper.GenerateKernelExecution(state, left, null);
            }
            if (right.KernelDetails != null) {
                right = ConversionHelper.GenerateKernelExecution(state, right, null);
            }
            Debug.Assert(left.KernelDetails == null);
            Debug.Assert(right.KernelDetails == null);
            // generate runtime size check
            // if (a[1] != b[0]) throw new ArgumentException();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetThrowIfIndicesNotEqual(left.SizeIdentifier, 1, right.SizeIdentifier, 0)
            );
            // store new size in variable
            // UInt64[] sizeX = new UInt64[] { a[0], b[1] }
            Identifier resultSizeIdentifier = state.GetNextTempSizeIdentifier();
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetVariableDeclarationForSizeIdentifier(resultSizeIdentifier, 2)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(resultSizeIdentifier, 1, right.SizeIdentifier, 1)
            );
            // store size arguments to matrix multiplication kernel in field
            Int32 kernelIndex = state.GetNextKernelIndex();
            Identifier kernelSizeIdentifier = Identifier.For(String.Format("_kernel{0}Size", kernelIndex));
            ConversionHelper.GenerateKernelSizeArrayField(state, kernelSizeIdentifier);
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForNewSizeArray(kernelSizeIdentifier, 3)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 0, left.SizeIdentifier, 0)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 1, left.SizeIdentifier, 1)
            );
            state.Constructor.Body.Statements.Add(
                NodeHelper.GetAssignmentStatementForIndices(kernelSizeIdentifier, 2, right.SizeIdentifier, 1)
            );

            List<String> rightStrings = new List<String>();
            rightStrings.Add(right.AccessExpression);
            Dictionary<String, KernelArgumentDetails> arguments = CombineKernelArguments(left.KernelArguments, right.KernelArguments, rightStrings, true);
            String kernelSource = ComputeKernelTemplates.MatrixMatrixMultiplication;
            kernelSource = kernelSource.Replace("{type}", left.Type.ScalarType.OpenCLTypeName);
            kernelSource = kernelSource.Replace("{leftExpression}", left.AccessExpression);
            kernelSource = kernelSource.Replace("{rightExpression}", rightStrings[0]);

            List<Identifier> waitHandles = left.WaitHandles;
            waitHandles.AddRange(right.WaitHandles);

            Func<List<Identifier>, Expression, Identifier> generate = (buffers, kernelExpression) => {
                Identifier kernelIdentifier = Identifier.For("_kernel" + kernelIndex.ToString());
                
                Identifier localSizeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "LocalSize");
                state.CodeletMethod.Body.Statements.Add(
                    new VariableDeclaration {
                        Name = localSizeIdentifier,
                        Type = SystemTypes.UInt64,
                        Initializer = new Literal(16, SystemTypes.UInt64)
                    }
                );
                Identifier globalRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "GlobalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, globalRangeIdentifier, SystemTypes.UInt64, 2);
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 0, kernelSizeIdentifier, 0)
                );
                state.CodeletMethod.Body.Statements.Add(
                    NodeHelper.GetAssignmentStatementForIndices(globalRangeIdentifier, 1, kernelSizeIdentifier, 2)
                );
                state.CodeletMethod.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Ne,
                            Operand1 = new BinaryExpression {
                                NodeType = NodeType.Rem,
                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                Operand2 = localSizeIdentifier
                            },
                            Operand2 = Literal.Int64Zero
                        },
                        TrueBlock = new Block {
                            Statements = new StatementList (
                                new AssignmentStatement {
                                    Target = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                    Source = new BinaryExpression {
                                        NodeType = NodeType.Add,
                                        Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                        Operand2 = new BinaryExpression {
                                            NodeType = NodeType.Sub,
                                            Operand1 = localSizeIdentifier,
                                            Operand2 = new BinaryExpression {
                                                NodeType = NodeType.Rem,
                                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 0),
                                                Operand2 = localSizeIdentifier
                                            }
                                        }
                                    }
                                }
                            )
                        }
                    }
                );
                state.CodeletMethod.Body.Statements.Add(
                    new If {
                        Condition = new BinaryExpression {
                            NodeType = NodeType.Ne,
                            Operand1 = new BinaryExpression {
                                NodeType = NodeType.Rem,
                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                Operand2 = localSizeIdentifier
                            },
                            Operand2 = Literal.Int64Zero
                        },
                        TrueBlock = new Block {
                            Statements = new StatementList(
                                new AssignmentStatement {
                                    Target = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                    Source = new BinaryExpression {
                                        NodeType = NodeType.Add,
                                        Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                        Operand2 = new BinaryExpression {
                                            NodeType = NodeType.Sub,
                                            Operand1 = localSizeIdentifier,
                                            Operand2 = new BinaryExpression {
                                                NodeType = NodeType.Rem,
                                                Operand1 = NodeHelper.GetConstantIndexerForExpression(globalRangeIdentifier, 1),
                                                Operand2 = localSizeIdentifier
                                            }
                                        }
                                    }
                                }
                            )
                        }
                    }
                );

                // kernelXLocalRange = new UInt64[] { kernelXLocalSize, kernelXLocalSize };
                Identifier localRangeIdentifier = Identifier.For("kernel" + kernelIndex.ToString() + "LocalRange");
                ConversionHelper.GenerateLocalArrayVariable(state.CodeletMethod, localRangeIdentifier, SystemTypes.UInt64, 2);
                ConversionHelper.GenerateArrayInitialisation(state, state.CodeletMethod, localRangeIdentifier, localSizeIdentifier, localSizeIdentifier);

                // get kernel
                ConversionHelper.GenerateGetKernelForProgram(state, kernelIdentifier, kernelExpression);
                // set kernel arguments
                Int32 index = 0;
                while (index < 3) {
                    ConversionHelper.GenerateKernelSetValueArgument(state, kernelIdentifier, index, NodeHelper.GetConstantIndexerForExpression(kernelSizeIdentifier, index));
                    index += 1;
                }
                while (index < 5) {
                    Expression localArgumentSizeExpression = new BinaryExpression {
                        NodeType = NodeType.Mul,
                        Operand1 = new Literal(left.Type.ScalarType.ByteSize, SystemTypes.Int32),
                        Operand2 = new BinaryExpression {
                            NodeType = NodeType.Mul,
                            Operand1 = localSizeIdentifier,
                            Operand2 = localSizeIdentifier
                        }
                    };
                    ConversionHelper.GenerateKernelSetLocalArgument(state, kernelIdentifier, index, localArgumentSizeExpression);
                    index += 1;
                }
                foreach (var buffer in buffers) {
                    ConversionHelper.GenerateKernelSetGlobalArgument(state, kernelIdentifier, index, buffer);
                    index += 1;
                }
                Identifier kernelPredecessorsIdentifier = Identifier.For(String.Format("kernel{0}predecessors", kernelIndex));
                ConversionHelper.GenerateKernelPredecessors(state, kernelPredecessorsIdentifier, waitHandles);
                Identifier waitHandleIdentifier = Identifier.For("_eventObject" + kernelIndex.ToString());
                ConversionHelper.GenerateEventObjectAndStartKernel(state, waitHandleIdentifier, kernelIdentifier, globalRangeIdentifier, localRangeIdentifier, kernelPredecessorsIdentifier);
                ConversionHelper.GenerateFlushCommandQueue(state);
                return waitHandleIdentifier;
            };

            String completeExpression = String.Format("({0} {1} {2})", left.CompleteExpression, "*", right.CompleteExpression);

            return new ConversionResult(
                new KernelDetails(kernelSource, generate),
                "value",
                left.Type,
                resultSizeIdentifier,
                arguments,
                new List<Identifier>(),
                completeExpression
            );
        }
        
        static Dictionary<String, KernelArgumentDetails> CombineKernelArguments(Dictionary<String, KernelArgumentDetails> leftArguments, Dictionary<String, KernelArgumentDetails> rightArguments, List<String> rightStrings, Boolean bindElementwise) {
            Dictionary<String, KernelArgumentDetails> arguments = new Dictionary<String, KernelArgumentDetails>();
            foreach (var leftArgument in leftArguments) {
                Boolean elementwise = leftArgument.Value.IsElementwise;
                if (bindElementwise) {
                    Debug.Assert(elementwise);
                    elementwise = false;
                }
                arguments.Add(leftArgument.Key, new KernelArgumentDetails(leftArgument.Value.OpenCLArgumentId, leftArgument.Value.MathType, elementwise));
            }
            List<KeyValuePair<String, String>> replacements = new List<KeyValuePair<String, String>>();
            Int32 tempNameCount = 0;
            foreach (var rightArgument in rightArguments) {
                String tempName = "temp" + tempNameCount.ToString();
                tempNameCount += 1;
                for (Int32 index = 0; index < rightStrings.Count; index += 1) {
                    rightStrings[index] = ReplaceTemplatePlaceholder(rightStrings[index], rightArgument.Value.OpenCLArgumentId.ToString(), tempName);
                }
                Boolean elementwise = rightArgument.Value.IsElementwise;
                if (bindElementwise) {
                    Debug.Assert(elementwise);
                    elementwise = false;
                }
                KernelArgumentDetails details;
                if (arguments.TryGetValue(rightArgument.Key, out details)) {
                    details = new KernelArgumentDetails(details.OpenCLArgumentId, details.MathType, details.IsElementwise && elementwise);
                } else {
                    details = new KernelArgumentDetails(arguments.Count, rightArgument.Value.MathType, elementwise);
                }
                arguments[rightArgument.Key] = details;
                replacements.Add(new KeyValuePair<String, String>(tempName, details.OpenCLArgumentId.ToString()));
            }
            foreach (var replacement in replacements) {
                for (Int32 index = 0; index < rightStrings.Count; index += 1) {
                    rightStrings[index] = ReplaceTemplatePlaceholder(rightStrings[index], replacement.Key, replacement.Value);
                }
            }
            return arguments;
        }
    }
}
