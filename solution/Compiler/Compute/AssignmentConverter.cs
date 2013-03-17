namespace ETH.Zonnon.Compute {
    using System;
    using System.Compiler;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;

    static class AssignmentConverter {
        public static Statement Convert(ConversionState state, ConversionResult expressionResult, Identifier receiverIdentifier, ComputeType receiverComputeType, INDEXER indexer) {
            
            bool complexIndexer;
            ComputeType computeType = receiverComputeType;
            Expression[] indices = ConversionHelper.GetExpressionsFromIndexer(indexer, ref receiverComputeType, out complexIndexer);
            // create empty indices if there is a complex indexer
            if (indices == null && complexIndexer) {
                receiverComputeType = computeType;
                indices = new Expression[computeType.Rank];
            }
            if (indices == null || (receiverComputeType != expressionResult.Type && !complexIndexer)) {
                return null;
            }
            
            // if the right expression has a complex indexer, we need it too
            complexIndexer |= expressionResult.HasComplexIndexer;

            List<ConversionHelper.Argument> sortedArguments = ConversionHelper.ExtractArguments(state, false);
            List<ConversionHelper.Argument> scalarArguments = ConversionHelper.ExtractArguments(state, true);

            ExpressionArgument receiverArgument;
            ConversionHelper.ProcessReceiver(state, expressionResult, receiverIdentifier, receiverComputeType, indices, indexer, ref sortedArguments, complexIndexer, out receiverArgument);

            ConversionResult finalConversionResult = ConversionHelper.GenerateKernelExecution(state, expressionResult, Identifier.For("buffer" + receiverArgument.Index.ToString()), complexIndexer);
            
            // generate delegates
            DelegateNode getOpenCLTimeDelegateType;
            DelegateNode startOpenCLDelegateType;
            ConversionHelper.GenerateOpenCLDelegates(state, out getOpenCLTimeDelegateType, out startOpenCLDelegateType);
            
            // generate get time method
            ConversionHelper.GenerateTimeMethod(state);
            
            // generate the codelets
            Identifier codeletsIdentifier;
            ConversionHelper.GenerateCodelets(state, getOpenCLTimeDelegateType, startOpenCLDelegateType, out codeletsIdentifier);

            // process arguments
            Identifier dataUsesIdentifier;
            ExpressionList constructArguments = new ExpressionList();
            ConversionHelper.ProcessArguments(state, sortedArguments, scalarArguments, complexIndexer, startOpenCLDelegateType, out dataUsesIdentifier, ref state.Constructor.Parameters, ref constructArguments);

            ConversionHelper.GenerateSubmitCodelet(state, codeletsIdentifier, dataUsesIdentifier);
            
            ConversionHelper.FinishOperation(state, finalConversionResult);

            String completeExpression = string.Format("{0} = {1}", ConversionHelper.FormatIdentifierExpression(receiverComputeType, indices, receiverArgument.Index, complexIndexer), finalConversionResult.CompleteExpression);
            Class actualOperationClass = CONTEXT._operationRegistry.RegisterOperation(completeExpression, state.Class);
            return new ExpressionStatement {
                Expression = new Construct {
                    Constructor = new MemberBinding {
                        BoundMember = actualOperationClass.GetConstructors()[0],
                    },
                    Operands = constructArguments,
                }
            };

        }
    }
}
