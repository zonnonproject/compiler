//-----------------------------------------------------------------------------
//
//  Copyright (c) 2000-2013 ETH Zurich (http://www.ethz.ch) and others.
//  All rights reserved. This program and the accompanying materials
//  are made available under the terms of the Microsoft Public License.
//  which accompanies this distribution, and is available at
//  http://opensource.org/licenses/MS-PL
//
//  Contributors:
//    ETH Zurich, Native Systems Group - Initial contribution and API
//    http://zonnon.ethz.ch/contributors.html
//
//-----------------------------------------------------------------------------

using System;
using System.Compiler;

namespace ETH.Zonnon
{

    public enum ErrorKind
    {
        // Messages
        Success = 1000,
        EndOfMessages = 1001,

        // Warnings
        WarningSeverity = 1005,
        ExtraEqual,
        ObsoleteParamName,
        MaxTypeSize,
        SuperfluousSize,
        CompilerCalculatedResult,
        CompilerCalculatedValue,
        DifferentSignatures,
        ImmutableIsPrivate,
        SimpleComparison,        
        DelegateForMethodOfValueObject,
        AlreadyImportedInDefinition,
        SyntaxWarningIn,
        WrongParameterInOperatorDeclaration,
        OnlyExternalTypesInOperator,
        DeprecatedImport,

        // Errors
        //
        ErrorSeverity = 2000,
        UnexpectedEndOfSource,
        OldObjectSyntax,
        LexemExpected,
        IntegerLiteralIsTooBig,
        RealLiteralIsTooBig,
        IllegalCharacter,
        CannotImportDefinition,

        SystemErrorIn,

        TerminatorDoesntMatchTypeName,
        SyntaxErrorIn,
        NoUnitName,
        DuplicateUnit,
        WrongUnitName,
        IncorrectUnitName,
        DuplicateDeclaration,
        IllegalModifier,

        WrongDefinitionName,
        WrongProtocolName,
        WrongImportName,
        WrongRefinesImplementsName,
        WrongTypeName,
        WrongProcedureName,
        WrongActivityName,

        UndeclaredProcedure,
        UndeclaredEntity,

        QualIdInNestedUnit,
        ImportInDeclaration,

        IllegalNesting,
        UndeclaredSimpleEntity,
        WrongQualification,

        UndeclaredUnit,
        UndeclaredLocalUnit,

        IllegalTypeForUnOperator,
        IllegalTypeForBinOperator,

		WrongUnitKeyword,

		UndeclaredLocal,
        IncorrectImport,

        NoType,
        WrongImplementationName,
        WrongThis,
        NonConstant,
        NegativeConst,
        ZeroConstant,
        ZeroStepValue,
        ZeroDivisor,
        IllegalProcedureCall,
        IllegalLeftPart,
        WrongNumberOfArgs,
        WrongPredeclaredCall,
        PredeclaredCallInConstantExpr,
        NotImplemented,
        IllegalTypeOf,
        CannotResolve,

        AssignmentWithReturn,
        OperatorShouldReturn,
        WrongPlaceForOperator,
        IllegalOperator,
        IllegalPartOfOperator,
        OperatorTooLong,
        NoOperator,
        UserDefinedOperatorParam,
        IllegalNumOfParams,
        IllegalBinaryOperator,
        IllegalUnaryOperator,
        FirstParInAssignment,
        SameTypeInAssignment,

        VarParamInObject,
        VarParamInActivity,
        WrongFinalIdentifier,
        UnknownType,
        TooBigSetMember,
        AssignmentCompatibility,
        AssignmentCompatibilityInProcedureCall,
        WrongIndicesNumber,
        WrongIndexerType,
        IllegalForVariable,

        IllegalCall,
        MissingParameters,
        ExtraParameters,
        IllegalArgumentForVar,
        ExtraReturnValue,
        MissedReturnValue,
        RefinesItself,
        MutualRefinement,
        IllegalUseOfExternal,
        ImplementationIsNotPublic,
        IllegalHiding,
        IllegalImplementingProc,
        IllegalImplementingProc2,
        IllegalImplementingProc3,
        IllegalImplementingProc4,

        NonDefinitionInImplements,
        NonObjectTypeInImplements,
        IllegalEntityInImplements,

        DefinitionInIs,
        NonObjectTypeInIs,
        IllegalEntityInIs,

        IllegalTypeInNew,
        IllegalConditionType,
        IllegalSafeguard,

        ImportMissed,

        PrivateDefinition,
        PrivateObject,
        PrivateType,
        PrivateActivity,
        PrivateProtocol,
        PrivateEntity,
        SupposedToBePrivate,        
        PublicMethodInActor,

        IllegalModifierFor,

        AssignToConstant,
        ModifyImmutable,
        SupposedToBeProtected,

        UnresolvedReference,
        RecursiveDefinition,
        NotAnInstance,
        WrongConstructor,
        WrongConstructorParams,
        ValueObjectWithParams,

        IllegalAssignmentSign,
        WrongAssignmentLeft,
        WrongAssignmentRight,
        EmtpyEnumType,
        ValueObjectInDeref,
        ReferenceInOperator,

        MissingObjectParameters,
        ExtraObjectParameters,
        MissingActivityParameters,
        ExtraActivityParameters,

        NoAccessorPrototype,
        NoAccessorModifier,
        IllegalProcForVar,
        IllegalAccessorSignature,
        IllegalAccessorSpec,
        AssignToProc,

        WrongWidthInWrite,
        ExtraWidthInWrite,

        NoDefinition,
        UndeclaredDefinition,

        IllegalTargetType,
        IllegalArgsInTypeConversion,
        IllegalWidthInTypeConversion,

        WrongBeginEndBalance,
        NoMainModule,

        NoEntityImplementation,
        CannotDeriveFromSystem,

        ExceptionAgain,
        TerminationAgain,
        ExcTypeAgain,

        IllegalProcAccess,
        NoProcInDefinition,
        DefinitionAmbiguity,
        NoMemberInInterface,

        DelegateWithProcType,
        DelegateWithWrongArgs,
        DelegateWrongArg,

        NonOpenArray,

        WrongPlaceForProtocol,
        UndeclaredProduction,
        IllegalTypeInProduction,
        IllegalEntityInProduction,
        NoProtocolKeywords,
        UndeclaredEBNFProduction,
        ProtocolAmbiguity,

        AwaitOutsideUnit,

        MissingCondition,
        AwaitIsDeclaredInNonProtectedMethod,
        NotImplementedIndexer,
        NotGetOrSetInIndexer,
        ObjectDoesNotImplementIndexer,
        IndexerIsReadOnly,
        IndexerIsWriteOnly,
        NoNeedForCallingNewForValueType,
        ExitStatementDoesntBelongAnyLoopStatement,
        EmptyProtocol,
        RepetitionMustContainBothDirections,
        ExplicitTypeConversionRequired,
        PropertyIsReadOnly,
        ConstantComputationOverflow,
        NotAllowedName,        
        IllegalUseOfModifier,
        InternalCompilerError,
        NamespaceAndTypeHaveSameName,
        
        //Math errors
        IncompatibleSizes,
        WrongIndexType,
        WrongIndexTypeMathMaybe,
        IncorrectConstantRange,
        WrongRangeVarType,
        RangeRightBorderNotSpecified,
        ArrayNotMath,
        RanksNotEqual,
        IncorrectRetTypeForGenComparison,
        IncorrectRankForTransp,
        IncorrectRanksForBinMatrixOp,
        MatrixIsNotSquare,
        MathArrayInOverloadedOperator,
        IncompatibleMathTypes,
        AssignMathExpressionToNonMath,
        DefinedInBaseDefinition,

        //Protocol validation
        AliasActivityReference,
        ActivityVariableIsNotLocal,
        AcceptCalledForInvalidDialog,
        ProtocolViolation,
        ProtocolNotCompleted,
        ActivityVariableMustBeLocal
    };

    public class ZonnonErrorNode : System.Compiler.ErrorNode
    {
        private static System.Resources.ResourceManager resourceManager;

        public ZonnonErrorNode ( ErrorKind code, params string[] messageParameters )
                                                                   : base((int)code, messageParameters)
        {
#if DEBUG
            if ( CONTEXT.options.Debug || CONTEXT.options.DebugT )
            {
             // System.Console.Write("ERROR! ");
             // for ( int i=0, n=messageParameters.Length; i<n; i++ )
             //     System.Console.Write("{0} ",messageParameters[i]);
             // System.Console.WriteLine();

                string format = GetMessage(null);
                System.Console.WriteLine(format);  //,messageParameters);
            }
#endif
        }

        public override string GetMessage ( System.Globalization.CultureInfo culture )
        {
            if ( resourceManager == null )
                resourceManager = new System.Resources.ResourceManager("Compiler.ErrorMessages",
                                                                       typeof(ZonnonErrorNode).Module.Assembly);
            return this.GetMessage(((ErrorKind)this.Code).ToString(), ZonnonErrorNode.resourceManager, culture);
        }

        public override int Severity
        {
            get { return this.Code < (int)ErrorKind.ErrorSeverity ? 1 : 0; } 
            //TODO: switch on code and return > 0 for warnings
        }
    }

    public class ERROR
    {
        internal static Scanner       scanner;
        internal static ErrorNodeList errors;

        internal static int errCount = 0;

        private static string errorneous_unit_name = "errorneous_unit_name_";

        internal static void open ( Scanner s, ErrorNodeList e )
        {
            scanner = s;
            errors = e;
            errCount = e.Length;
        }

        internal static bool artificialName ( Identifier id )
        {
            if ( id.Name.Length < 21 ) return false;
            return id.Name.Substring(0,21) == errorneous_unit_name;
        }

        internal static Identifier errUnitName
        {
            get { string errName = errorneous_unit_name + errCount.ToString();
                  return Identifier.For(errName); }
        }

        const int LIMIT = 100;
        private static void message ( ErrorKind code, SourceContext context, params string[] extra )
        {
          //  System.Diagnostics.Debugger.Launch();
            if ( (int)code >= (int)ErrorKind.ErrorSeverity ) errCount++;
            ErrorNode error = new ZonnonErrorNode(code,extra);            
            error.SourceContext = context;
            if(errors.Length < LIMIT || code == ErrorKind.InternalCompilerError) errors.Add(error);
        }

        private static void message ( ErrorKind code, params string[] extra )
        {
            SourceContext ctx = scanner.getSourceContext();
            try
            {
                if (ctx.StartLine < 0 || ctx.EndLine < ctx.StartLine || ctx.StartColumn < 0 ||
                (ctx.EndLine == ctx.StartLine && ctx.EndColumn < ctx.StartColumn))
                {
                    // System.Diagnostics.Debug.Assert(false);
                    return;
                }
            }
            catch (IndexOutOfRangeException)
            {
                ctx = new SourceContext();
            }
            message(code,ctx,extra);
        }

        public static void Success               ( )     { message(ErrorKind.Success); }
        public static void EndOfMessages         ( )     { message(ErrorKind.EndOfMessages); }

        public static void ExtraEqual            ( )     { message(ErrorKind.ExtraEqual); }
        public static void ObsoleteParamName     ( string name ) { message(ErrorKind.ObsoleteParamName,name); }
        public static void MaxTypeSize           ( string type, string size, SourceContext context )
                                                         { message(ErrorKind.MaxTypeSize,context,type,size); }
        public static void SuperfluousSize       ( string type ) { message(ErrorKind.SuperfluousSize,type); }
        public static void CompilerCalculatedResult ( string expr, string res, SourceContext context )
                                                         { message(ErrorKind.CompilerCalculatedResult,context,expr,res); }
        public static void CompilerCalculatedValue ( string expr, string res, SourceContext context )
                                                         { message(ErrorKind.CompilerCalculatedValue,context,expr,res); }
        public static void DifferentSignatures    ( string name, SourceContext context )
                                                         { message(ErrorKind.DifferentSignatures,context,name); }
        public static void ImmutableIsPrivate     ( SourceContext context )
                                                         { message(ErrorKind.ImmutableIsPrivate,context); }

        public static void SimpleComparison       ( SourceContext context )
                                                         { message(ErrorKind.SimpleComparison,context); }

        public static void SyntaxWarningIn(string where, string reason) { message(ErrorKind.SyntaxWarningIn, where, reason); }

        //Errors

        public static void OldObjectSyntax        ( )    { message(ErrorKind.OldObjectSyntax); }

        public static void UnexpectedEndOfSource  ( string where )   { message(ErrorKind.UnexpectedEndOfSource,where); }
        public static void LexemExpected          ( string lexem )   { message(ErrorKind.LexemExpected,lexem); }
        public static void IntegerLiteralIsTooBig ( string literal ) { message(ErrorKind.IntegerLiteralIsTooBig,literal); }
        public static void RealLiteralIsTooBig    ( string literal ) { message(ErrorKind.RealLiteralIsTooBig,literal); }
        public static void IllegalCharacter       ( char sym, uint code ) { message(ErrorKind.IllegalCharacter,sym.ToString(),code.ToString()); }

        public static void SystemErrorIn          ( string fun, string reason ) { message(ErrorKind.SystemErrorIn,fun,reason); }

        public static void TerminatorDoesntMatchTypeName ( string typeId, string endId ) { message(ErrorKind.TerminatorDoesntMatchTypeName,typeId,endId); }
        public static void SyntaxErrorIn          ( string where, string reason )   { message(ErrorKind.SyntaxErrorIn,where,reason); }
        public static void NoUnitName             ( string unit )                   { message(ErrorKind.NoUnitName,unit); }
        public static void DuplicateUnit          ( string u_kind, string name )    { message(ErrorKind.DuplicateUnit,u_kind,name); }
        public static void WrongUnitName          ( string name, string qual_name ) { message(ErrorKind.WrongUnitName,name,qual_name); }
        public static void IncorrectUnitName      ( string u_kind, string name, string exist_u_kind ) { message(ErrorKind.IncorrectUnitName,u_kind,name,exist_u_kind); }
        public static void DuplicateDeclaration   ( string name ) { message(ErrorKind.DuplicateDeclaration,name); }
        public static void DuplicateDeclaration   ( SourceContext context, string name )
                                                                  { message(ErrorKind.DuplicateDeclaration,context,name); }
        public static void IllegalModifier        ( string modifier ) { message(ErrorKind.IllegalModifier,modifier); }

        public static void WrongDefinitionName    ( string qual_id ) { message(ErrorKind.WrongDefinitionName,qual_id); }
        public static void WrongDefinitionName    ( SourceContext context, string qual_id ) { message(ErrorKind.WrongDefinitionName,context,qual_id); }
        public static void WrongProtocolName      ( string qual_id ) { message(ErrorKind.WrongProtocolName,qual_id); }
        public static void WrongProtocolName      ( SourceContext context, string qual_id ) { message(ErrorKind.WrongProtocolName,context,qual_id); }
        public static void WrongImportName        ( string qual_id ) { message(ErrorKind.WrongImportName,qual_id); }
        public static void WrongRefinesImplementsName        ( SourceContext context, string qual_id )
                                                                     { message(ErrorKind.WrongRefinesImplementsName,context,qual_id); }
        public static void WrongImportName(SourceContext context, string qual_id)
                                                { message(ErrorKind.WrongImportName, context, qual_id); }

        public static void WrongTypeName          ( string qual_id ) { message(ErrorKind.WrongTypeName,qual_id); }
        public static void WrongTypeName          ( SourceContext context, string qual_id ) { message(ErrorKind.WrongTypeName,context,qual_id); }

        public static void WrongProcedureName     ( string qual_id ) { message(ErrorKind.WrongProcedureName,qual_id); }
        public static void WrongActivityName      ( SourceContext context ) { message(ErrorKind.WrongActivityName,context); }

        public static void UndeclaredProcedure    ( string qual_id, string proc ) { message(ErrorKind.UndeclaredProcedure,qual_id,proc); }

        public static void UndeclaredEntity       (                        string entity, string qual_id ) { message(ErrorKind.UndeclaredEntity,entity,qual_id); }
        
        public static void UndeclaredEBNFProduction(string entity, string qual_id) { message(ErrorKind.UndeclaredEBNFProduction, entity, qual_id); }
        public static void UndeclaredEntity       ( SourceContext context, string entity, string qual_id ) { message(ErrorKind.UndeclaredEntity,context,entity,qual_id); }

        public static void QualIdInNestedUnit     ( string qual_id ) { message(ErrorKind.QualIdInNestedUnit,qual_id); }
        public static void ImportInDeclaration    ( string import ) { message(ErrorKind.ImportInDeclaration,import); }
        public static void IllegalNesting         ( string new_unit ) { message(ErrorKind.IllegalNesting,new_unit); }
        public static void UndeclaredSimpleEntity ( string entity ) { message(ErrorKind.UndeclaredSimpleEntity,entity); }
        public static void WrongQualification     ( string qual ) { message(ErrorKind.WrongQualification,qual); }
        public static void WrongQualification     ( SourceContext context, string qual ) { message(ErrorKind.WrongQualification,context,qual); }

        public static void UndeclaredUnit         ( SourceContext context, string unit ) 
                                                         { message(ErrorKind.UndeclaredUnit,context,unit); }

        public static void UndeclaredLocalUnit    ( string unit ) { message(ErrorKind.UndeclaredLocalUnit,unit); }

        public static void IllegalTypeForBinOperator ( SourceContext context, string op, string type1, string type2 )
                                                         { message(ErrorKind.IllegalTypeForBinOperator,context,op,type1,type2); }
        public static void IllegalTypeForUnOperator ( SourceContext context, string op, string type )
                                                         { message(ErrorKind.IllegalTypeForUnOperator,context,op,type); }
		public static void WrongUnitKeyword      ( string keyword )
			                                             { message(ErrorKind.WrongUnitKeyword,keyword); }
		public static void UndeclaredLocal       ( string entity, SourceContext context )
		                                                 { message(ErrorKind.UndeclaredLocal,context,entity); }
        public static void IncorrectImport       ( string qual )
		                                                 { message(ErrorKind.IncorrectImport,qual); }
        public static void NoType                ( string where )     { message(ErrorKind.NoType,where); }
        public static void NoType                ( SourceContext context, string where )
                                                         { message(ErrorKind.NoType,context,where); }
        public static void WrongThis             ( )     { message(ErrorKind.WrongThis); }
        public static void NonConstant           ( SourceContext context, string where )
                                                         { message(ErrorKind.NonConstant,context,where); }
        public static void NegativeConst         ( string where, SourceContext context )
                                                         { message(ErrorKind.NegativeConst,context,where); }
        public static void ZeroConstant          ( SourceContext context )
                                                         { message(ErrorKind.ZeroConstant,context,null); }
        public static void ZeroStepValue         ( SourceContext context )
                                                         { message(ErrorKind.ZeroStepValue,context,null); }
        public static void ZeroDivisor           ( SourceContext context )
                                                         { message(ErrorKind.ZeroDivisor,context,null); }
        public static void IllegalProcedureCall  ( SourceContext context, string name )
                                                         { message(ErrorKind.IllegalProcedureCall,context,name); }
        public static void IllegalLeftPart       ( SourceContext context, string name )
                                                         { message(ErrorKind.IllegalLeftPart,context,name); }
        public static void WrongNumberOfArgs     ( SourceContext context, string name )
                                                         { message(ErrorKind.WrongNumberOfArgs,context,name); }
        public static void WrongPredeclaredCall  ( SourceContext context, string name )
                                                         { message(ErrorKind.WrongPredeclaredCall,context,name); }
        public static void PredeclaredCallInConstantExpr ( SourceContext context, string proc )
                                                         { message(ErrorKind.PredeclaredCallInConstantExpr,context,proc); }
        public static void NotImplemented        ( string what ) { message(ErrorKind.NotImplemented,what); }
        public static void NotImplemented        ( SourceContext context, string what )
                                                         { message(ErrorKind.NotImplemented,context,what); }
        public static void IllegalTypeOf         ( string type, string what, SourceContext context ) 
                                                         { message(ErrorKind.IllegalTypeOf,context,type,what); }
        public static void AssignmentWithReturn  ( )     { message(ErrorKind.AssignmentWithReturn); }
        public static void OperatorShouldReturn  ( )     { message(ErrorKind.OperatorShouldReturn); }
        public static void WrongPlaceForOperator ( )     { message(ErrorKind.WrongPlaceForOperator); }
        public static void IllegalOperator       ( string code ) 
                                                         { message(ErrorKind.IllegalOperator,code); }
        public static void IllegalPartOfOperator ( string code )
                                                         { message(ErrorKind.IllegalPartOfOperator,code); }
        public static void OperatorTooLong       ( string code )
                                                         { message(ErrorKind.OperatorTooLong,code); }
        public static void NoOperator            ( )     { message(ErrorKind.NoOperator); }
        public static void UserDefinedOperatorParam ( SourceContext context )
                                                         { message(ErrorKind.UserDefinedOperatorParam,context); }
        public static void IllegalNumOfParams    ( SourceContext context )
                                                         { message(ErrorKind.IllegalNumOfParams,context); }
        public static void IllegalBinaryOperator ( string code, SourceContext context )
                                                         { message(ErrorKind.IllegalBinaryOperator,context,code); }
        public static void IllegalUnaryOperator  ( string code, SourceContext context )
                                                         { message(ErrorKind.IllegalUnaryOperator,context,code); }
        public static void FirstParInAssignment  ( SourceContext context )
                                                         { message(ErrorKind.FirstParInAssignment,context); }
        public static void SameTypeInAssignment  ( string type, SourceContext context )
                                                         { message(ErrorKind.SameTypeInAssignment,context,type); }

        public static void VarParamInObject      ( )     { message(ErrorKind.VarParamInObject); }
        public static void VarParamInActivity    ( ) { message(ErrorKind.VarParamInActivity); }
        public static void NoFinalIdentifier     ( string unit, string name )
                                                         { message(ErrorKind.WrongFinalIdentifier,unit,name); }
        public static void UnknownType           ( SourceContext context, string entity )
                                                         { message(ErrorKind.UnknownType,context,entity); }
        public static void TooBigSetMember       ( long val, SourceContext context )
                                                         { message(ErrorKind.TooBigSetMember,context,val.ToString()); }
        public static void AssignmentCompatibility ( string t1, string t2, SourceContext context )
                                                         { message(ErrorKind.AssignmentCompatibility,context,t1,t2); }
        public static void AssignmentCompatibilityInProcedureCall(string t1, string t2, string arg, string procName, string argName, SourceContext context)
                                                         { message(ErrorKind.AssignmentCompatibilityInProcedureCall, context, t1, t2, arg, procName, argName); }
        public static void WrongIndicesNumber(SourceContext context)
                                                         { message(ErrorKind.WrongIndicesNumber,context,null); }
        public static void WrongIndexerType      ( string type, SourceContext context )
                                                         { message(ErrorKind.WrongIndexerType,context,type); }
        public static void IllegalForVariable    ( SourceContext context )
                                                         { message(ErrorKind.IllegalForVariable,context,null); }
        public static void IllegalCall           ( SourceContext context )
                                                         { message(ErrorKind.IllegalCall,context,null); }
        public static void MissingParameters     ( int n, SourceContext context )
                                                         { message(ErrorKind.MissingParameters,context,n.ToString()); }
        public static void ExtraParameters       ( int n, SourceContext context )
                                                         { message(ErrorKind.ExtraParameters,context,n.ToString()); }
        public static void IllegalArgumentForVar ( string name, SourceContext context )
                                                         { message(ErrorKind.IllegalArgumentForVar,context,name); }
        public static void ExtraReturnValue      ( string name, SourceContext context )
                                                         { message(ErrorKind.ExtraReturnValue,context,name); }
        public static void MissedReturnValue     ( string name, SourceContext context )
                                                         { message(ErrorKind.MissedReturnValue,context,name); }
        public static void RefinesItself         ( string name )
                                                         { message(ErrorKind.RefinesItself,name); }
        public static void MutualRefinement      ( string name1, string name2 )
                                                         { message(ErrorKind.MutualRefinement,name1,name2); }
        public static void IllegalUseOfExternal  ( string name, SourceContext context )
                                                         { message(ErrorKind.IllegalUseOfExternal,context,name); }
        public static void ImplementationIsNotPublic ( string proc_name, string member_name, SourceContext context )
                                                         { message(ErrorKind.ImplementationIsNotPublic,context,proc_name,member_name); }
        public static void IllegalHiding         ( string name )
                                                         { message(ErrorKind.IllegalHiding,name); }
        public static void IllegalImplementingProc ( string name_imp, string name_def, SourceContext context )
                                                         { message(ErrorKind.IllegalImplementingProc,context,name_imp,name_def); }
        public static void IllegalImplementingProc2 ( string def_proc, string unit_kind, string unit_name, SourceContext context )
                                                         { message(ErrorKind.IllegalImplementingProc2,context,def_proc,unit_kind,unit_name); }
        public static void IllegalImplementingProc3 ( string proc_name, string def_name, SourceContext context )
                                                         { message(ErrorKind.IllegalImplementingProc3,context,proc_name,def_name); }
        public static void NonDefinitionInImplements ( string name, SourceContext context )
                                                         { message(ErrorKind.NonDefinitionInImplements,context,name); }
        public static void NonObjectTypeInImplements ( SourceContext context )
                                                         { message(ErrorKind.NonObjectTypeInImplements,context); }
        public static void IllegalEntityInImplements ( SourceContext context )
                                                         { message(ErrorKind.IllegalEntityInImplements,context); }
        public static void DefinitionInIs            ( SourceContext context, string name )
                                                         { message(ErrorKind.DefinitionInIs,context,name); }
        public static void NonObjectTypeInIs         ( SourceContext context )
                                                         { message(ErrorKind.NonObjectTypeInIs,context); }
        public static void IllegalEntityInIs         ( SourceContext context )
                                                         { message(ErrorKind.IllegalEntityInIs,context); }
        public static void IllegalTypeInNew          ( SourceContext context )
                                                         { message(ErrorKind.IllegalTypeInNew,context); }
        public static void IllegalConditionType      ( SourceContext context )
                                                         { message(ErrorKind.IllegalConditionType,context); }
        public static void IllegalSafeguard          ( string name, SourceContext context )
                                                         { message(ErrorKind.IllegalSafeguard,context,name); }
        public static void ImportMissed              ( string name )
                                                         { message(ErrorKind.ImportMissed,name); }
        public static void PrivateDefinition         ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateDefinition,context,name); }
        public static void PrivateObject             ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateObject,context,name); }
        public static void PrivateType               ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateType,context,name); }
        public static void PrivateActivity           ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateActivity,context,name); }
        public static void PrivateProtocol           ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateProtocol,context,name); }
        public static void PrivateEntity             ( string name, SourceContext context )
                                                         { message(ErrorKind.PrivateEntity,context,name); }
        public static void IllegalModifierFor        ( string modifier, string declaration, SourceContext context )
                                                         { message(ErrorKind.IllegalModifierFor,context,modifier,declaration); }
        public static void IllegalModifierFor        ( string modifier, string declaration )
                                                         { message(ErrorKind.IllegalModifierFor,modifier,declaration); }
        public static void AssignToConstant          ( string name, SourceContext context )
                                                         { message(ErrorKind.AssignToConstant,context,name); }
        public static void ModifyImmutable           ( string name, SourceContext context )
                                                         { message(ErrorKind.ModifyImmutable,context,name); }
        public static void UnresolvedReference       ( string name )
                                                         { message(ErrorKind.UnresolvedReference,name); }
        public static void RecursiveDefinition       ( string name, SourceContext context )
                                                         { message(ErrorKind.RecursiveDefinition,context,name); }
        public static void NotAnInstance             ( string name, SourceContext context )
                                                         { message(ErrorKind.NotAnInstance,context,name); }
        public static void WrongConstructor          ( string name, int args, SourceContext context )
                                                         { message(ErrorKind.WrongConstructor,context,name,args.ToString()); }
        public static void WrongConstructorParams    ( string name, string expected, SourceContext context )
                                                         { message(ErrorKind.WrongConstructorParams,context,name, expected); }
        public static void ValueObjectWithParams     ( ) { message(ErrorKind.ValueObjectWithParams); }
        public static void IllegalAssignmentSign     ( ) { message(ErrorKind.IllegalAssignmentSign); }
        public static void WrongAssignmentLeft           ( SourceContext context )
                                                         { message(ErrorKind.WrongAssignmentLeft,context); }
        public static void WrongAssignmentRight(SourceContext context)
                                                        { message(ErrorKind.WrongAssignmentRight, context); }
        public static void EmtpyEnumType             ( ) { message(ErrorKind.EmtpyEnumType); }
        public static void ValueObjectInDeref        ( string name, SourceContext context )
                                                         { message(ErrorKind.ValueObjectInDeref,context,name); }
        public static void MissingObjectParameters   ( int n, SourceContext context )
                                                         { message(ErrorKind.MissingObjectParameters,context,n.ToString()); }
        public static void ExtraObjectParameters     ( int n, SourceContext context )
                                                         { message(ErrorKind.ExtraObjectParameters,context,n.ToString()); }
        public static void MissingActivityParameters ( int n, SourceContext context )
                                                         { message(ErrorKind.MissingActivityParameters,context,n.ToString()); }
        public static void ExtraActivityParameters   ( int n, SourceContext context )
                                                         { message(ErrorKind.ExtraActivityParameters,context,n.ToString()); }
        public static void NoAccessorPrototype       ( string proc_name, SourceContext context )
                                                         { message(ErrorKind.NoAccessorPrototype,context,proc_name); }
        public static void NoAccessorModifier        ( string var_name, SourceContext context )
                                                         { message(ErrorKind.NoAccessorModifier,context,var_name); }
        public static void IllegalProcForVar         ( string var_name, SourceContext context )
                                                         { message(ErrorKind.IllegalProcForVar,context,var_name); }
        public static void IllegalAccessorSignature  ( string var_name, SourceContext context )
                                                         { message(ErrorKind.IllegalAccessorSignature,context,var_name); }
        public static void IllegalAccessorSpec       ( string SetGet, string var_name, SourceContext context )
                                                         { message(ErrorKind.IllegalAccessorSpec,context,SetGet,var_name); }
        public static void AssignToProc              ( string proc_name, SourceContext context )
                                                         { message(ErrorKind.AssignToProc,context,proc_name); }
        public static void WrongWidthInWrite         ( SourceContext context )
                                                         { message(ErrorKind.WrongWidthInWrite,context); }
        public static void ExtraWidthInWrite         ( SourceContext context )
                                                         { message(ErrorKind.ExtraWidthInWrite,context); }
        public static void NoDefinition              ( SourceContext context, string name )
                                                         { message(ErrorKind.NoDefinition,context); }
        public static void UndeclaredDefinition      ( SourceContext context, string name )
                                                         { message(ErrorKind.UndeclaredDefinition,context,name); }
        public static void IllegalTargetType         ( string type )
                                                         { message(ErrorKind.IllegalTargetType,type); }
        public static void IllegalArgsInTypeConversion ( ) { message(ErrorKind.IllegalArgsInTypeConversion); }
        public static void IllegalWidthInTypeConversion ( ) { message(ErrorKind.IllegalWidthInTypeConversion); }
        public static void WrongBeginEndBalance      ( string stmt )
                                                         { message(ErrorKind.WrongBeginEndBalance,stmt); }
        public static void NoMainModule              ( string module )
                                                         { message(ErrorKind.NoMainModule,module); }
        public static void NoEntityImplementation    ( SourceContext context, string unitkind, string unitname, string entityname )
                                                         { message(ErrorKind.NoEntityImplementation,context,unitkind,unitname,entityname); }
        public static void CannotDeriveFromSystem    ( SourceContext context, string name )
                                                         { message(ErrorKind.CannotDeriveFromSystem,context,name); }
        public static void ExceptionAgain            ( ) { message(ErrorKind.ExceptionAgain); }
        public static void TerminationAgain          ( ) { message(ErrorKind.TerminationAgain); }
        public static void ExcTypeAgain              ( ) { message(ErrorKind.ExcTypeAgain); }
        public static void IllegalProcAccess         ( string ProcName, string DefName, SourceContext context )
                                                         { message(ErrorKind.IllegalProcAccess,context,ProcName,DefName); }
        public static void NoProcInDefinition        ( string ProcName, string DefName, SourceContext context )
                                                         { message(ErrorKind.NoProcInDefinition,context,ProcName,DefName); }
        public static void DefinitionAmbiguity       ( string memb, string def1, string def2, SourceContext context )
                                                         { message(ErrorKind.DefinitionAmbiguity,context,memb,def1,def2); }
        public static void NoMemberInInterface       ( string memb, string int_type, SourceContext context )
                                                         { message(ErrorKind.NoMemberInInterface,context,memb,int_type); }
        public static void DelegateWithProcType      ( string arg, SourceContext context )
                                                         { message(ErrorKind.DelegateWithProcType,context,arg); }
        public static void DelegateWrongArg          ( string arg, SourceContext context )
                                                         { message(ErrorKind.DelegateWrongArg,context,arg); }
        public static void DelegateWithWrongArgs(string delname, string desiredArgs, SourceContext context)
                                                       { message(ErrorKind.DelegateWithWrongArgs, context, delname, desiredArgs); }
        public static void NonOpenArray(string arr, SourceContext context) 
                                                        { message(ErrorKind.NonOpenArray,context,arr); }
        public static void WrongPlaceForProtocol     ( SourceContext context ) 
                                                        { message(ErrorKind.WrongPlaceForProtocol,context); }
        public static void UndeclaredProduction      ( string name, SourceContext context )
                                                        { message(ErrorKind.UndeclaredProduction,context,name); }
        public static void IllegalTypeInProduction   ( string t, SourceContext context )
                                                        { message(ErrorKind.IllegalTypeInProduction,context,t); }
        public static void IllegalEntityInProduction ( string e, SourceContext context )
                                                        { message(ErrorKind.IllegalEntityInProduction,context,e); }
        public static void NoProtocolKeywords        ( string p, SourceContext context )
                                                        { message(ErrorKind.NoProtocolKeywords,context,p); }
        public static void AwaitOutsideUnit          ( SourceContext context )
                                                        { message(ErrorKind.AwaitOutsideUnit,context); }
        public static void MissingCondition          (SourceContext context)
                                                        { message(ErrorKind.MissingCondition, context); }
        public static void AwaitIsDeclaredInNonProtectedMethod (SourceContext context)
                                                        { message(ErrorKind.AwaitIsDeclaredInNonProtectedMethod, context); }
        public static void NotImplementedIndexer(string t, SourceContext context)
                                                        { message(ErrorKind.NotImplementedIndexer, context, t); }
        public static void NotGetOrSetInIndexer(string t, SourceContext context)
                                                        { message(ErrorKind.NotGetOrSetInIndexer, context, t); }
        public static void ObjectDoesNotImplementIndexer(string t, SourceContext context)
                                                        { message(ErrorKind.ObjectDoesNotImplementIndexer, context, t); }
        public static void IndexerIsReadOnly(string t, SourceContext context)
                                                        { message(ErrorKind.IndexerIsReadOnly, context, t); }
        public static void IndexerIsWriteOnly(string t, SourceContext context)
                                                        { message(ErrorKind.IndexerIsWriteOnly, context, t); }
        public static void NoNeedForCallingNewForValueType(SourceContext context)
                                                        { message(ErrorKind.NoNeedForCallingNewForValueType, context); }
        public static void ExitStatementDoesntBelongAnyLoopStatement(SourceContext context)
                                                        { message(ErrorKind.ExitStatementDoesntBelongAnyLoopStatement, context); }
        public static void EmptyProtocol(string t, SourceContext context)
                                                        { message(ErrorKind.EmptyProtocol, context, t); }
        public static void RepetitionMustContainBothDirections(SourceContext context)
                                                        { message(ErrorKind.RepetitionMustContainBothDirections, context); }
        public static void ExplicitTypeConversionRequired(SourceContext context)
                                                        { message(ErrorKind.ExplicitTypeConversionRequired, context); }
        public static void PropertyIsReadOnly(string t, SourceContext context)
                                                        { message(ErrorKind.PropertyIsReadOnly, context, t); }
        public static void DelegateForMethodOfValueObject(string t, SourceContext context)
                                                        { message(ErrorKind.DelegateForMethodOfValueObject, context, t); }
        public static void ConstantComputationOverflow(SourceContext context)
                                                        { message(ErrorKind.ConstantComputationOverflow, context); }
        public static void NotAllowedName(string name, string unit, SourceContext context)
                                                        { message(ErrorKind.NotAllowedName, context, name, unit); }
        public static void SupposedToBePrivate(string name, SourceContext context)
                                                        { message(ErrorKind.SupposedToBePrivate, context, name); }
        public static void InternalCompilerError(string details)
                                                        { message(ErrorKind.InternalCompilerError, details); }
        public static void AlreadyImportedInDefinition(string name, string unit, SourceContext context)
                                                        { message(ErrorKind.AlreadyImportedInDefinition, context, name, unit); }
        public static void CannotImportDefinition(string name, SourceContext context)
                                                        { message(ErrorKind.CannotImportDefinition, context, name); }
        public static void ReferenceInOperator(string variableName, SourceContext context)
                                                        { message(ErrorKind.ReferenceInOperator, context, variableName); }
        public static void IllegalUseOfModifier(string modifier, string used_for, SourceContext context)
                                                        { message(ErrorKind.IllegalUseOfModifier, context, modifier, used_for); }

        //Math errors
        public static void IncompatibleSizes(SourceContext context, string op, string type1, string type2)
                                                        { message(ErrorKind.IncompatibleSizes, context, op, type1, type2); }
        public static void WrongIndexType(SourceContext context, string type_index, string type_indexer)
                                                        { message(ErrorKind.WrongIndexType, context, type_index, type_indexer); }
        public static void WrongIndexTypeMathMaybe(SourceContext context, string type_index, string type_indexer)
                                                        { message(ErrorKind.WrongIndexTypeMathMaybe, context, type_index, type_indexer); }
        public static void IncorrectConstantRange(SourceContext context, string border1, string border2)
                                                        { message(ErrorKind.IncorrectConstantRange, context, border1, border2); }
        public static void WrongRangeVarType(SourceContext context, string type)
                                                        { message(ErrorKind.WrongRangeVarType, context, type); }
        public static void RangeRightBorderNotSpecified(SourceContext context)
                                                        { message(ErrorKind.RangeRightBorderNotSpecified, context); }
        public static void ArrayNotMath (SourceContext context, string op, string type1)
                                                        { message(ErrorKind.ArrayNotMath, context, op, type1); }
        public static void RanksNotEqual(SourceContext context, string op, string type1, string type2)
                                                        { message(ErrorKind.RanksNotEqual, context, op, type1, type2); }
        public static void IncorrectRetTypeForGenComparison(SourceContext context, string op, string type1, string type2)
                                                        { message(ErrorKind.IncorrectRetTypeForGenComparison, context, op, type1, type2); }
        public static void IncorrectRankForTransp(SourceContext context, string type1)
                                                        { message(ErrorKind.IncorrectRankForTransp, context, type1); }
        public static void IncorrectRanksForBinMatrixOp(SourceContext context, string op, string type1, string type2)
                                                        { message(ErrorKind.IncorrectRanksForBinMatrixOp, context, op, type1, type2); }
        public static void MatrixIsNotSquare(SourceContext context, string op, string type1)
                                                        { message(ErrorKind.MatrixIsNotSquare, context, type1); }
        public static void MathArrayInOverloadedOperator(SourceContext context, string type1)
                                                        { message(ErrorKind.MathArrayInOverloadedOperator, context, type1); }
        public static void IncompatibleMathTypes(SourceContext context, string type1, string type2, string op)
                                                        { message(ErrorKind.IncompatibleMathTypes, context, type1, type2, op); }
        public static void AssignMathExpressionToNonMath(SourceContext context)
                                                        { message(ErrorKind.IncompatibleMathTypes, context); }
        public static void WrongParameterInOperatorDeclaration(SourceContext context)
                                                        { message(ErrorKind.WrongParameterInOperatorDeclaration, context); }
        public static void OnlyExternalTypesInOperator(SourceContext context)
                                                        { message(ErrorKind.OnlyExternalTypesInOperator, context); }
        public static void SupposedToBeProtected(SourceContext context)
                                                        { message(ErrorKind.SupposedToBeProtected, context); }
        public static void CannotResolve(SourceContext context, string name)
                                                        { message(ErrorKind.CannotResolve, context, name); }
        public static void AliasActivityReference(SourceContext context)
                                                        { message(ErrorKind.AliasActivityReference, context); }
        public static void ActivityVariableIsNotLocal(SourceContext context, string name, string scope)
                                                        { message(ErrorKind.ActivityVariableIsNotLocal, context, name, scope); }
        public static void AcceptCalledForInvalidDialog(SourceContext context)
                                                        { message(ErrorKind.AcceptCalledForInvalidDialog, context); }
        public static void ProtocolViolation(SourceContext context, string expected)
                                                        { message(ErrorKind.ProtocolViolation, context, expected); }
        public static void ProtocolNotCompleted(SourceContext context, string name, string expectations)
                                                        { message(ErrorKind.ProtocolNotCompleted, context, name, expectations); }
        public static void ProtocolAmbiguity(SourceContext context, string name)
                                                        { message(ErrorKind.ProtocolAmbiguity, context, name); }
        public static void ActivityVariableMustBeLocal(SourceContext context)
                                                        { message(ErrorKind.ActivityVariableMustBeLocal, context); }
        public static void NamespaceAndTypeHaveSameName(SourceContext context, string name)
                                                        { message(ErrorKind.NamespaceAndTypeHaveSameName, context, name); }
        public static void DeprecatedImport(SourceContext context, string name)
                                                        { message(ErrorKind.DeprecatedImport, context, name); }
        public static void DefinedInBaseDefinition(SourceContext context, string name, string basedef)
                                                        { message(ErrorKind.DefinedInBaseDefinition, context, name, basedef); }
        public static void IllegalImplementingProc4(SourceContext context, string name)
                                                        { message(ErrorKind.IllegalImplementingProc4, context, name); }
        public static void PublicMethodInActor(SourceContext context)
                                                        { message(ErrorKind.PublicMethodInActor, context); }                 
        
    }

}  // namespace ETH.Zonnon.Compiler
 