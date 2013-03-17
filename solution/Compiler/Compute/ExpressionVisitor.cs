namespace ETH.Zonnon.Compute {
    using System;
    using System.Collections.Generic;
    using System.Compiler;

    class ExpressionConverter {
        static Int32 _generatedClassesCount;
        Class _generatedClass;
        Dictionary<String, Int32> _arguments;

        static Class Convert(ASSIGNMENT assignment) {
            ExpressionConverter converter = new ExpressionConverter(assignment);
            return converter._generatedClass;
        }

        ExpressionConverter(ASSIGNMENT assignment) {
            _generatedClass = new Class();
            _arguments = new Dictionary<String, Int32>();
        }

        Int32 RegisterArgument(INSTANCE instance) {
            Int32 value;
            String name = instance.name.ToString();
            if (!_arguments.TryGetValue(name, out value)) {
                value = _arguments.Count;
                _arguments.Add(name, value);
                _generatedClass.Members.Add(
                    new Field {
                        Name = Identifier.For("_size" + value.ToString()),
                        Type =  SystemTypes.Int64.GetArrayType((instance.type as ARRAY_TYPE).dimensions.Length)
                    }
                );
            }
            return value;
        }

        ConversionResult? Visit(NODE node) {
            INSTANCE instance = node as INSTANCE;
            if (instance != null) {
                return VisitInstance(instance);
            }
            BINARY binary = node as BINARY;
            if (binary != null) {
                ConversionResult? leftResult = Visit(binary.left_operand);
                ConversionResult? rightResult = Visit(binary.right_operand);
                if (leftResult != null && rightResult != null) {
                    ConversionResult left = leftResult.Value;
                    ConversionResult right = rightResult.Value;
                    if (TYPE.sameType(left.ElementType, right.ElementType)) {
                        PLUS plus = node as PLUS;
                        if (plus != null) {
                            return VisitElementWiseBinary(plus, "+", left, right);
                        }
                        MINUS minus = node as MINUS;
                        if (minus != null) {
                            return VisitElementWiseBinary(minus, "-", left, right);
                        }
                        MULTIPLY_ELEMENTWISE multiplyElementwise = node as MULTIPLY_ELEMENTWISE;
                        if (multiplyElementwise != null) {
                            return VisitElementWiseBinary(multiplyElementwise, "*", left, right);
                        }
                        DIVIDE_ELEMENTWISE divideElementwise = node as DIVIDE_ELEMENTWISE;
                        if (divideElementwise != null) {
                            return VisitElementWiseBinary(divideElementwise, "/", left, right);
                        }
                        MULTIPLY multiply = node as MULTIPLY;
                        if (multiply != null) {
                            return VisitMultiplication(multiply, left, right);
                        }
                    }
                }
            }
            return null;
        }

        ConversionResult? VisitInstance(INSTANCE instance) {
            ARRAY_TYPE arrayType = instance.type as ARRAY_TYPE;
            if (arrayType == null || !arrayType.isMath) {
                return null;
            }
            Int32 index = RegisterArgument(instance);
            String access = String.Format("Access(argument{0})", index);
            String complete = index.ToString();
            HashSet<Int32> arguments = new HashSet<Int32>();
            arguments.Add(index);
            return new ConversionResult(null, access, complete, arrayType.dimensions.Length, arrayType.base_type, null, arguments);
        }

        ConversionResult? VisitElementWiseBinary(BINARY binary, String op, ConversionResult left, ConversionResult right) {
            throw new NotImplementedException();
        }

        ConversionResult? VisitMultiplication(MULTIPLY multiply, ConversionResult left, ConversionResult right) {

        }
    }
}
