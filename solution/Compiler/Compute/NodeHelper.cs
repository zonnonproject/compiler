namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;

    sealed class NodeHelper {
        public static Indexer GetConstantIndexerForExpression(Expression identifier, Int32 value) {
            return new Indexer {
                Object = identifier,
                Operands = new ExpressionList(
                    new Literal(value, SystemTypes.Int32)
                )
            };
        }

        public static Throw GetThrowFor(Member type) {
            return new Throw {
                Expression = new Construct {
                    Constructor = new MemberBinding(null, type)
                }
            };
        }

        public static If GetThrowIfDimensionsNotEqual(Expression thisSize, Expression otherSize) {
            return new If {
                Condition = new UnaryExpression {
                    NodeType = NodeType.LogicalNot,
                    Operand = new MethodCall {
                        Callee = new MemberBinding {
                            BoundMember = STANDARD.ComputeHelper.GetMethod(Identifier.For("AreDimensionsEqual"), SystemTypes.UInt64.GetArrayType(1), SystemTypes.UInt64.GetArrayType(1))
                        },
                        Operands = new ExpressionList(
                            thisSize,
                            otherSize
                        ),
                    },
                },
                TrueBlock = new Block {
                    Statements = new StatementList(
                        NodeHelper.GetThrowFor(SystemTypes.ArgumentException)
                    ),
                },
            };
        }

        public static If GetThrowIfIndicesNotEqual(Expression left, Int32 leftIndex, Expression right, Int32 rightIndex) {
            return new If {
                Condition = new BinaryExpression {
                    NodeType = NodeType.Ne,
                    Operand1 = NodeHelper.GetConstantIndexerForExpression(left, leftIndex),
                    Operand2 = NodeHelper.GetConstantIndexerForExpression(right, rightIndex)
                },
                TrueBlock = new Block {
                    Statements = new StatementList(
                        NodeHelper.GetThrowFor(SystemTypes.ArgumentException)
                    )
                }
            };
        }

        public static VariableDeclaration GetVariableDeclarationForSizeIdentifier(Identifier identifier, Int32 rank) {
            return new VariableDeclaration {
                Name = identifier,
                Type = SystemTypes.UInt64.GetArrayType(1),
                Initializer = new ConstructArray {
                    ElementType = SystemTypes.UInt64,
                    Operands = new ExpressionList(
                        new Literal(rank, SystemTypes.Int32)
                    ),
                }
            };
        }

        public static AssignmentStatement GetAssignmentStatementForIndices(Expression target, Int32 targetIndex, Expression source, Int32 sourceIndex) {
            return new AssignmentStatement {
                Target = NodeHelper.GetConstantIndexerForExpression(target, targetIndex),
                Source = NodeHelper.GetConstantIndexerForExpression(source, sourceIndex),
            };
        }

        public static AssignmentStatement GetAssignmentStatementForNewSizeArray(Identifier identifier, Int32 size) {
            return new AssignmentStatement {
                Target = identifier,
                Source = new ConstructArray {
                    ElementType = SystemTypes.UInt64,
                    Rank = 1,
                    Operands = new ExpressionList(
                        new Literal(size, SystemTypes.Int32)
                    )
                }
            };
        }

        public static Expression GetSizeMultiplicationClosure(Identifier sizeIdentifier, Int32 rank) {
            Expression multiplies = NodeHelper.GetConstantIndexerForExpression(sizeIdentifier, 0);
            for (Int32 index = 1; index < rank; index += 1) {
                Expression old = multiplies;
                multiplies = new BinaryExpression {
                    NodeType = NodeType.Mul,
                    Operand1 = old,
                    Operand2 = NodeHelper.GetConstantIndexerForExpression(sizeIdentifier, 1)
                };
            }
            return multiplies;
        }

        public static Expression GetIndexedSize(String target, Int32 dimension) {
            Expression expX = new BinaryExpression {
                NodeType = NodeType.Sub,
                Operand1 = Identifier.For(String.Format("{0}to{1}", target, dimension)),
                Operand2 = Identifier.For(String.Format("{0}from{1}", target, dimension))
            };
            return new BinaryExpression {
                NodeType = NodeType.Add,
                Operand1 = new BinaryExpression {
                    NodeType = NodeType.Div,
                    Operand1 = expX,
                    Operand2 = Identifier.For(String.Format("{0}by{1}", target, dimension))
                },
                Operand2 = new UnaryExpression {
                    NodeType = NodeType.Not,
                    Operand = new UnaryExpression {
                        NodeType = NodeType.Not,
                        Operand = new BinaryExpression {
                            NodeType = NodeType.Rem,
                            Operand1 = expX,
                            Operand2 = Identifier.For(String.Format("{0}by{1}", target, dimension))
                        }
                    }
                }
            };
        }

        public static Expression CastToUInt64(Expression int64Exp) {
            return new BinaryExpression {
                NodeType = NodeType.Castclass,
                Operand1 = int64Exp,
                Operand2 = new MemberBinding {
                    BoundMember = SystemTypes.UInt64
                }
            };
        }

        public static Expression CastToInt64(Expression uint64Exp) {
            return new BinaryExpression {
                NodeType = NodeType.Castclass,
                Operand1 = uint64Exp,
                Operand2 = new MemberBinding {
                    BoundMember = SystemTypes.Int64
                }
            };
        }


        public static Statement GetIndexerPart(Identifier data, Identifier source, Identifier target, Int32 dimension, bool withDimensionCall) {
            if (withDimensionCall) {
                return new If {
                    Condition = new BinaryExpression {
                        NodeType = NodeType.Eq,
                        Operand1 = source,
                        Operand2 = Literal.Int32MinusOne,
                    },
                    TrueBlock = new Block {
                        Statements = new StatementList(new AssignmentStatement {
                            Source = NodeHelper.GetConstantIndexerForExpression(new MethodCall {
                                Callee = new MemberBinding {
                                    TargetObject = data,
                                    BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                                }
                            }, dimension),
                            Target = target
                        })
                    },
                    FalseBlock = new Block {
                        Statements = new StatementList(new AssignmentStatement {
                            Source = new BinaryExpression {
                                NodeType = NodeType.Castclass,
                                Operand1 = new BinaryExpression {
                                    NodeType = NodeType.Add,
                                    Operand1 = source,
                                    Operand2 = Literal.Int64One
                                },
                                Operand2 = new MemberBinding {
                                    BoundMember = SystemTypes.UInt64
                                }
                            },
                            Target = target,
                        })
                    }
                };
            } else {
                return new AssignmentStatement {
                    Source = new BinaryExpression {
                        NodeType = NodeType.Castclass,
                        Operand1 = source,
                        Operand2 = new MemberBinding {
                            BoundMember = SystemTypes.UInt64
                        }
                    },
                    Target = target,
                };
            }
        }
    }
}
