namespace ETH.Zonnon.Compute {
    using System;
    using System.Collections.Generic;
    using System.Compiler;
    using System.Diagnostics;

    struct MathTypeCheckResult {
        public TYPE ResultType { get; private set; }

        public MathTypeCheckResult(TYPE resultType)
            : this() {
            ResultType = resultType;
        }
    }

    struct MathTypeCheckErrorInfo {
        public SourceContext SourceContext { get; private set; }
        public String Op { get; private set; }

        public MathTypeCheckErrorInfo(SourceContext sourceContext, String op)
            : this() {
            SourceContext = sourceContext;
            Op = op;
        }
    }

    struct ComputeType {
        public ComputeScalarType ScalarType { get; private set; }
        public Int32 Rank { get; private set; }

        public ComputeType(ComputeScalarType scalarType, Int32 rank)
            : this() {
            Debug.Assert(rank >= 0);
            Debug.Assert(scalarType != null);
            ScalarType = scalarType;
            Rank = rank;
        }

        public static Boolean operator ==(ComputeType type1, ComputeType type2) {
            return type1.ScalarType == type2.ScalarType && type1.Rank == type2.Rank;
        }

        public static Boolean operator !=(ComputeType type1, ComputeType type2) {
            return !(type1 == type2);
        }

        public static ComputeType? FromType(TYPE type) {
            ARRAY_TYPE arrayType;
            ComputeScalarType scalarType;
            if (type.Is(out arrayType) && arrayType.isMath) {
                if (arrayType.isMath && ComputeScalarType.TryGet(arrayType.base_type, out scalarType)) {
                    return new ComputeType(scalarType, arrayType.dimensions.Length);
                }
            //} else {
            //    if (ComputeScalarType.TryGet(type, out scalarType)) {
            //        return new ComputeType(scalarType, 0);
            //    }
            }
            return null;
        }
    }

    struct KernelArgumentDetails {
        public Int32 OpenCLArgumentId { get; private set; }
        public ComputeType MathType { get; private set; }
        public Boolean IsElementwise { get; private set; }

        public KernelArgumentDetails(Int32 openCLArgumentId, ComputeType mathType, Boolean isElementwise)
            : this() {
            OpenCLArgumentId = openCLArgumentId;
            MathType = mathType;
            IsElementwise = isElementwise;
        }
    }

    struct KernelDetails {
        public String Source { get; private set; }
        public Func<List<Identifier>, Expression, Identifier> Generate { get; private set; }

        public KernelDetails(String source, Func<List<Identifier>, Expression, Identifier> generate)
            : this() {
            Source = source;
            Generate = generate;
        }
    }

    struct ConversionResult {
        public KernelDetails? KernelDetails { get; private set; }
        public String AccessExpression { get; private set; }
        public ComputeType Type { get; private set; }
        public Identifier SizeIdentifier { get; private set; }
        public Dictionary<String, KernelArgumentDetails> KernelArguments { get; private set; }
        public List<Identifier> WaitHandles { get; private set; }
        public String CompleteExpression { get; private set; }
        public Int32? ArgumentIndex { get; private set; }
        public Boolean HasComplexIndexer { get { return ArgumentIndex.HasValue; } }

        public ConversionResult(
            KernelDetails? kernelDetails,
            String accessExpression,
            ComputeType mathType,
            Identifier sizeIdentifier,
            Dictionary<String, KernelArgumentDetails> kernelArguments,
            List<Identifier> waitHandles,
            String completeExpression,
            Int32? argumentIndex
        )
            : this() {
            KernelDetails = kernelDetails;
            AccessExpression = accessExpression;
            Type = mathType;
            SizeIdentifier = sizeIdentifier;
            KernelArguments = kernelArguments;
            WaitHandles = waitHandles;
            CompleteExpression = completeExpression;
            ArgumentIndex = argumentIndex;
        }

        public ConversionResult(
            KernelDetails? kernelDetails,
            String accessExpression,
            ComputeType mathType,
            Identifier sizeIdentifier,
            Dictionary<String, KernelArgumentDetails> kernelArguments,
            List<Identifier> waitHandles,
            String completeExpression
        )
            : this(kernelDetails, 
            accessExpression,
            mathType,
            sizeIdentifier, 
            kernelArguments, 
            waitHandles, 
            completeExpression,
            null) {
        }
    }
}
