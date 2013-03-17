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

using System.Compiler;
using ZonnonHalt = Zonnon.RTL.Halt;
using ETH.Zonnon.Compute;
using System.Diagnostics;

namespace ETH.Zonnon
{
    public class STANDARD
    {
        // Module STANDARD encapsulates the standard Zonnon entities.
        //
        // Notice that we do not include Standard to any compiler
        // structure: neither to program tree nor to the import stack,
        // nor to the current context.
        //
        // The only function which deals with Standard is processName()
        // function from SEMANTICS namespace: we check Standard there
        // trying to find the undeclared name.
        public static MODULE_DECL Standard;

        // Standard types
        public static TYPE_DECL Object;
        public static TYPE_DECL Integer;
        public static TYPE_DECL Real;
        public static TYPE_DECL Fixed;
        public static TYPE_DECL Set;
        public static TYPE_DECL Range;
        public static TYPE_DECL Char;
        public static TYPE_DECL String;
        public static TYPE_DECL Boolean;
        public static TYPE_DECL Cardinal;

        // Standard exception types
        public static TYPE_DECL Overflow;
        public static TYPE_DECL ZeroDivision;
        public static TYPE_DECL CastError;
        public static TYPE_DECL NullInstance;
        public static TYPE_DECL RangeError_Std;
        public static TYPE_DECL CaseError;
        public static TYPE_DECL ReadError;
        public static TYPE_DECL ProtocolMismatch;
        public static TYPE_DECL ProtocolServerMismatch;

        // Math exception types
        public static TYPE_DECL IncompatibleSizes;
        public static TYPE_DECL DiagonalElements;
        public static TYPE_DECL NoSLUSolution;

        // Standard constants
        public static CONSTANT_DECL False;
        public static CONSTANT_DECL True;

        // Some staff for activity support
        public static TYPE_DECL protocol;
        public static TYPE_DECL activityType;
        public static TYPE_DECL barrier;
		public static TYPE_DECL objectLock;

        // Standard procedures and functions
        public static PROCEDURE_DECL Abs;
     // public static PROCEDURE_DECL Ash;
        public static PROCEDURE_DECL All;
        public static PROCEDURE_DECL AnyP; //P stays for "Procedure", because "Any" is already declared as ANY_TYPE
        public static PROCEDURE_DECL Assert;
        public static PROCEDURE_DECL Box;
        public static PROCEDURE_DECL Cap;
     // public static PROCEDURE_DECL Chr;
        public static PROCEDURE_DECL Copy;
        public static PROCEDURE_DECL Dec;
     // public static PROCEDURE_DECL Entier;
        public static PROCEDURE_DECL Excl;
     // public static PROCEDURE_DECL Float;
        public static PROCEDURE_DECL Find;
        public static PROCEDURE_DECL Dense;
        public static PROCEDURE_DECL Halt;
        public static PROCEDURE_DECL Inc;
        public static PROCEDURE_DECL Incl;
        public static PROCEDURE_DECL Len;
     // public static PROCEDURE_DECL Long;
        public static PROCEDURE_DECL Low;
        public static PROCEDURE_DECL Max;
        public static PROCEDURE_DECL Min;
        public static PROCEDURE_DECL Odd;
     // public static PROCEDURE_DECL Ord;
        public static PROCEDURE_DECL Pred;
     // public static PROCEDURE_DECL RealProc;
     // public static PROCEDURE_DECL Short;
        public static PROCEDURE_DECL Size;
        public static PROCEDURE_DECL Sparse;
        public static PROCEDURE_DECL Succ;
        public static PROCEDURE_DECL Sum;
        public static PROCEDURE_DECL Unbox;
     // public static PROCEDURE_DECL Val;
        public static PROCEDURE_DECL ColSum;
        public static PROCEDURE_DECL RowSum;

        public static PROCEDURE_DECL Read;
        public static PROCEDURE_DECL ReadLn;
        public static PROCEDURE_DECL Write;
        public static PROCEDURE_DECL WriteLn;
        //public static PROCEDURE_DECL PulseAll;

        private static VOID_TYPE Void;
        private static ANY_TYPE  Any;

        // System assembly
        private static AssemblyNode system;

        // Zonnon runtime
        private static QualifiedIdentifier rtlName;
        
        private static AssemblyNode rtlAssembly;
        public  static TypeNode HaltException;
        public static TypeNode IncompatibleSizesException;
        public static TypeNode DiagonalElementsException;
        public static TypeNode NoSLUSolutionException;
        public static TypeNode ZeroDivisionException;
//      public  static TypeNode RangeError;
        public  static TypeNode Sets;
        public static TypeNode Ranges;
        public  static TypeNode Output;
        public  static TypeNode Input;
        public  static TypeNode Console;
        public static TypeNode Array;
        public static TypeNode systemMath;
        public static TypeNode ObjectLock;
        public static TypeNode Math;
        public static TypeNode SparseMatrix;
        public static TypeNode SparseVector;
        public static TypeNode RowSPA;
        public static TypeNode ColSPA;
        public static TypeNode RowVectorSPA;
        public static TypeNode SPA;
        public static TypeNode Data;
        public static TypeNode DataUse;
        public static TypeNode DataAccess;
        public static TypeNode DataRangeIndex;
        public static TypeNode KernelManager;
        public static TypeNode ComputeManager;
        public static TypeNode DependencyManager;
        public static TypeNode ComputeHelper;
        public static TypeNode Codelet;
        public static TypeNode Kernel;
        public static TypeNode CommandQueue;
        public static TypeNode EventObject;
        public static TypeNode EventObjectCompletionCallback;
        public static TypeNode Device;
        public static TypeNode Buffer;
        public static TypeNode BufferFlags;
        public static TypeNode Context;
        public static TypeNode OpenCLComputeDevice;
        public static TypeNode SystemBuffer;

        //Staff for matadata
        public static TypeNode ZonnonAttribute;
        
        public  static TypeNode common;

        // ctor
        // ----
        // Creates module STANDARD and construct nodes for all
        // predefined Zonnon entities as specified in the language.
        //
        static STANDARD ( )
        {
            Void = new VOID_TYPE();
            Any = new ANY_TYPE();

            rtlName = new QualifiedIdentifier(Identifier.For("Zonnon"),Identifier.For("RTL"));

            system      = AssemblyNode.GetAssembly(typeof(System.Console).Assembly);
            rtlAssembly = AssemblyNode.GetAssembly(typeof(ZonnonHalt).Assembly);
            Identifier system_id = Identifier.For("System");

//            RangeError = rtlAssembly.GetType(system_id, Identifier.For("ArgumentOutOfRangeException"));

            Identifier rtl_id = Identifier.For("Zonnon.RTL");
            Identifier rtlcompute_id = Identifier.For("Zonnon.RTL.Compute");
            Identifier rtlopenclwrapper_id = Identifier.For("Zonnon.RTL.OpenCL.Wrapper");
            HaltException = rtlAssembly.GetType(rtl_id, Identifier.For("Halt"));
            IncompatibleSizesException = rtlAssembly.GetType(rtl_id, Identifier.For("IncompatibleSizes"));
            DiagonalElementsException = rtlAssembly.GetType(rtl_id, Identifier.For("DiagonalElements"));
            NoSLUSolutionException = rtlAssembly.GetType(rtl_id, Identifier.For("NoSLUSolution"));
            ZeroDivisionException = system.GetType(Identifier.For("System"), Identifier.For("DivideByZeroException"));

            common        = rtlAssembly.GetType(rtl_id,Identifier.For("Common"));
            Sets          = rtlAssembly.GetType(rtl_id,Identifier.For("Set"));
            Ranges        = rtlAssembly.GetType(rtl_id, Identifier.For("Range")); 
            Output        = rtlAssembly.GetType(rtl_id,Identifier.For("Output"));
            Input         = rtlAssembly.GetType(rtl_id,Identifier.For("Input"));
            Console       =      system.GetType(Identifier.For("System"),Identifier.For("Console"));
            Array         =      system.GetType(Identifier.For("System"), Identifier.For("Array"));
            systemMath    =      system.GetType(Identifier.For("System"), Identifier.For("Math"));
            ObjectLock    = rtlAssembly.GetType(rtl_id, Identifier.For("ObjectLock"));
            Math          = rtlAssembly.GetType(rtl_id, Identifier.For("Math"));
            Data = rtlAssembly.GetType(rtlcompute_id, Identifier.For("Data"));
            DataUse = rtlAssembly.GetType(rtlcompute_id, Identifier.For("DataUse"));
            DataAccess = rtlAssembly.GetType(rtlcompute_id, Identifier.For("DataAccess"));
            DataRangeIndex = rtlAssembly.GetType(rtlcompute_id, Identifier.For("DataRangeIndex"));
            KernelManager = rtlAssembly.GetType(rtlcompute_id, Identifier.For("KernelManager"));
            ComputeManager = rtlAssembly.GetType(rtlcompute_id, Identifier.For("ComputeManager"));
            DependencyManager = rtlAssembly.GetType(rtlcompute_id, Identifier.For("DependencyManager"));
            ComputeHelper = rtlAssembly.GetType(rtlcompute_id, Identifier.For("ComputeHelper"));
            Codelet = rtlAssembly.GetType(rtlcompute_id, Identifier.For("Codelet"));
            Kernel = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("Kernel"));
            CommandQueue = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("CommandQueue"));
            EventObject = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("EventObject"));
            EventObjectCompletionCallback = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("EventObjectCompletionCallback"));
            Device = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("Device"));
            Buffer = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("Buffer"));
            BufferFlags = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("BufferFlags"));
            Context = rtlAssembly.GetType(rtlopenclwrapper_id, Identifier.For("Context"));
            OpenCLComputeDevice = rtlAssembly.GetType(rtlcompute_id, Identifier.For("OpenCLComputeDevice"));
            SystemBuffer = SystemTypes.SystemAssembly.GetType(Identifier.For("System"), Identifier.For("Buffer"));
            SparseMatrix = rtlAssembly.GetType(rtl_id, Identifier.For("SparseMatrix`1"));
            SparseVector  = rtlAssembly.GetType(rtl_id, Identifier.For("SparseVector`1"));
            RowSPA        = rtlAssembly.GetType(rtl_id, Identifier.For("RowSPA`1"));
            ColSPA        = rtlAssembly.GetType(rtl_id, Identifier.For("ColSPA`1"));
            RowVectorSPA  = rtlAssembly.GetType(rtl_id, Identifier.For("RowVectorSPA`1"));
            SPA           = rtlAssembly.GetType(rtl_id, Identifier.For("SPA`1"));

            ZonnonAttribute = rtlAssembly.GetType(rtl_id, Identifier.For("ZonnonAttribute"));

            Standard = new MODULE_DECL(Identifier.For("STANDARD"));
            Standard.locals = new DECLARATION_LIST();
            Standard.enclosing = null; // it's global
            Standard.body = null;
         // Standard.definitions = null;

            //---- Predefined types

            Object = new TYPE_DECL(Identifier.For("object"));  // really not needed 'cause this is keyword!
            Object.enclosing = Standard;
            Object.type = new INTERFACE_TYPE();
                Object.type.enclosing = Object;
            Standard.locals.Add(Object);

            Integer = new TYPE_DECL(Identifier.For("integer"));
            Integer.enclosing = Standard;
            Integer.type = new INTEGER_TYPE(32);         // Width 32 by default
            Integer.type.enclosing = Integer;
            Standard.locals.Add(Integer);

            Real = new TYPE_DECL(Identifier.For("real"));
            Real.enclosing = Standard;
            Real.type = new REAL_TYPE(64);               // Width 64 by default
            Real.type.enclosing = Real;
            Standard.locals.Add(Real);

            Fixed = new TYPE_DECL(Identifier.For("fixed"));
            Fixed.enclosing = Standard;
            Fixed.type = new FIXED_TYPE(96);               // Width 96 by default
            Fixed.type.enclosing = Fixed;
            Standard.locals.Add(Fixed);

            Set = new TYPE_DECL(Identifier.For("set"));
            Set.enclosing = Standard;
            Set.type = new SET_TYPE(32);                 // Width 32 by default
            Set.type.enclosing = Set;
            Standard.locals.Add(Set);

            Range = new TYPE_DECL(Identifier.For("range"));
            Range.enclosing = Standard;
            Range.type = new RANGE_TYPE();                 // Width 32 by default
            Range.type.enclosing = Range;
            Standard.locals.Add(Range);

            Char = new TYPE_DECL(Identifier.For("char"));
            Char.enclosing = Standard;
            Char.type = new CHAR_TYPE(16);               // Width 16 by default
            Char.type.enclosing = Char;
            Standard.locals.Add(Char);

            String = new TYPE_DECL(Identifier.For("string"));
            String.enclosing = Standard;
            String.type = new STRING_TYPE();
            String.type.enclosing = String;
            Standard.locals.Add(String);

            Cardinal = new TYPE_DECL(Identifier.For("cardinal"));
            Cardinal.enclosing = Standard;
            Cardinal.type = new CARDINAL_TYPE(32);       // Width 32 by default
            Cardinal.type.enclosing = Cardinal;
            Standard.locals.Add(Cardinal);

            Boolean = new TYPE_DECL(Identifier.For("boolean"));
            Boolean.enclosing = Standard;
            Boolean.type = new BOOLEAN_TYPE();
            Boolean.type.enclosing = Boolean;
            Standard.locals.Add(Boolean);

            // Predefined exceptions

            Overflow = new TYPE_DECL(Identifier.For("Overflow"));
            Overflow.enclosing = Standard;
            Overflow.type = new EXTERNAL_TYPE(system.GetType(Identifier.For("System"),Identifier.For("OverflowException")));
                //new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"),Identifier.For("OverflowError")));
            Overflow.type.enclosing = Overflow;
            Standard.locals.Add(Overflow);

            ZeroDivision = new TYPE_DECL(Identifier.For("ZeroDivision"));
            ZeroDivision.enclosing = Standard;
            ZeroDivision.type = new EXTERNAL_TYPE(system.GetType(Identifier.For("System"), Identifier.For("ArithmeticException")));
            ZeroDivision.type.enclosing = ZeroDivision;
            Standard.locals.Add(ZeroDivision);

            CastError = new TYPE_DECL(Identifier.For("Cast"));
            CastError.enclosing = Standard;
            CastError.type = new EXTERNAL_TYPE(system.GetType(Identifier.For("System"),Identifier.For("InvalidCastException")));
            CastError.type.enclosing = CastError;
            Standard.locals.Add(CastError);

            NullInstance = new TYPE_DECL(Identifier.For("NilReference"));
            NullInstance.enclosing = Standard;
            NullInstance.type = new EXTERNAL_TYPE(system.GetType(Identifier.For("System"),Identifier.For("NullReferenceException")));
            NullInstance.type.enclosing = NullInstance;
            Standard.locals.Add(NullInstance);

            RangeError_Std = new TYPE_DECL(Identifier.For("IndexOutOfRangeException"));
            RangeError_Std.enclosing = Standard;
            RangeError_Std.type = new EXTERNAL_TYPE(system.GetType(Identifier.For("System"), Identifier.For("IndexOutOfRangeException")));
            RangeError_Std.type.enclosing = RangeError_Std;
            Standard.locals.Add(RangeError_Std);

            CaseError = new TYPE_DECL(Identifier.For("UnmatchedCase"));
            CaseError.enclosing = Standard;
            CaseError.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"),Identifier.For("CaseError")));
            CaseError.type.enclosing = CaseError;
            Standard.locals.Add(CaseError);

            ReadError = new TYPE_DECL(Identifier.For("Read"));
            ReadError.enclosing = Standard;
            ReadError.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"),Identifier.For("InputError")));

            ProtocolMismatch = new TYPE_DECL(Identifier.For("ProtocolMismatch"));
            ProtocolMismatch.enclosing = Standard;
            ProtocolMismatch.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"),Identifier.For("ProtocolMismatch")));
            ProtocolMismatch.type.enclosing = ProtocolMismatch;
            Standard.locals.Add(ProtocolMismatch);

            ProtocolServerMismatch = new TYPE_DECL(Identifier.For("ProtocolServerMismatch"));
            ProtocolServerMismatch.enclosing = Standard;
            ProtocolServerMismatch.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"), Identifier.For("ProtocolServerMismatch")));
            ProtocolServerMismatch.type.enclosing = ProtocolServerMismatch;
            Standard.locals.Add(ProtocolServerMismatch);

            True = new CONSTANT_DECL(Identifier.For("true"));
            True.enclosing = Standard;
            True.type = Boolean.type;
            True.initializer = CCI_LITERAL.create(new BOOLEAN_TYPE(),new Literal(true,SystemTypes.Boolean));
            Standard.locals.Add(True);

            False = new CONSTANT_DECL(Identifier.For("false"));
            False.enclosing = Standard;
            False.type = Boolean.type;
            False.initializer = CCI_LITERAL.create(new BOOLEAN_TYPE(),new Literal(false,SystemTypes.Boolean));
            Standard.locals.Add(False);

            // Predefined  IsMath exceptions
            IncompatibleSizes = new TYPE_DECL(Identifier.For("IncompatibleSizes"));
            IncompatibleSizes.enclosing = Standard;
            IncompatibleSizes.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"), Identifier.For("IncompatibleSizes")));
            IncompatibleSizes.type.enclosing = IncompatibleSizes;
            Standard.locals.Add(IncompatibleSizes);

            DiagonalElements = new TYPE_DECL(Identifier.For("DiagonalElements"));
            DiagonalElements.enclosing = Standard;
            DiagonalElements.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"), Identifier.For("DiagonalElements")));
            DiagonalElements.type.enclosing = DiagonalElements;
            Standard.locals.Add(DiagonalElements);

            NoSLUSolution = new TYPE_DECL(Identifier.For("NoSLUSolution"));
            NoSLUSolution.enclosing = Standard;
            NoSLUSolution.type = new EXTERNAL_TYPE(rtlAssembly.GetType(Identifier.For("Zonnon.RTL"), Identifier.For("NoSLUSolution")));
            NoSLUSolution.type.enclosing = NoSLUSolution;
            Standard.locals.Add(NoSLUSolution);

            // Some activity staff

            protocol = new TYPE_DECL(Identifier.For("Protocol"));
            protocol.enclosing = Standard;
                    TypeNode p = rtlAssembly.GetType(rtl_id,Identifier.For("Protocol"));
            protocol.type = new EXTERNAL_TYPE(p);
            protocol.type.enclosing = protocol;
            Standard.locals.Add(protocol);

            activityType = new TYPE_DECL(Identifier.For("activityType"));
            activityType.enclosing = Standard;
            activityType.type = new EXTERNAL_TYPE(p.NestedTypes[0]);  // activityType delegate is within Protocol
            activityType.type.enclosing = activityType;
            Standard.locals.Add(activityType);

            barrier = new TYPE_DECL(Identifier.For("Barrier"));
            barrier.enclosing = Standard;
            barrier.type = new EXTERNAL_TYPE(rtlAssembly.GetType(rtl_id,Identifier.For("Barrier")));
            barrier.type.enclosing = barrier;
            Standard.locals.Add(barrier);

			objectLock = new TYPE_DECL(Identifier.For("ObjectLock"));
			objectLock.enclosing = Standard;
			objectLock.type = new EXTERNAL_TYPE(rtlAssembly.GetType(rtl_id,Identifier.For("ObjectLock")));
			objectLock.type.enclosing = objectLock;
			Standard.locals.Add(objectLock);

            // Predefined procedures

            Abs = new PROCEDURE_DECL(Identifier.For("abs"));
            Abs.enclosing = Standard;
            Abs.type = Any;  // depends on the argument!!!
            Abs.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateABS);
            Abs.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertABS);
            Abs.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateABS);
            Standard.locals.Add(Abs);

         // Doesn't exist anymore
         //
         // Ash = new PROCEDURE_DECL(Identifier.For("ASH"));
         // Ash.enclosing = Standard;
         // Ash.type = Void;
         // Ash.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertASH);
         // Ash.calculateStandardCall = null;
         // Standard.locals.Add(Ash);

            All = new PROCEDURE_DECL(Identifier.For("all"));
            All.enclosing = Standard;
            All.type = Boolean.type; //Any;  // depends on the argument!!!
            //All.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateALL);
            All.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertALL);
            All.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateALL);
            Standard.locals.Add(All);

            AnyP = new PROCEDURE_DECL(Identifier.For("any"));
            AnyP.enclosing = Standard;
            AnyP.type = Boolean.type; //Any;  // depends on the argument!!!
            //AnyP.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateANY);
            AnyP.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertANY);
            AnyP.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateANY);
            Standard.locals.Add(AnyP);

            Assert = new PROCEDURE_DECL(Identifier.For("assert"));
            Assert.enclosing = Standard;
            Assert.type = Void;
            Assert.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertASSERT);
            Assert.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateASSERT);
            Standard.locals.Add(Assert);

            Box = new PROCEDURE_DECL(Identifier.For("box"));
            Box.enclosing = Standard;
            Box.type = Any;
            Box.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateBOX);
            Box.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertBOX);
            Box.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateBOX);
            Standard.locals.Add(Box);

            Cap = new PROCEDURE_DECL(Identifier.For("cap"));
            Cap.enclosing = Standard;
            Cap.type = Char.type;
            Cap.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertCAP);
            Cap.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateCAP);
            Standard.locals.Add(Cap);

         // Chr = new PROCEDURE_DECL(Identifier.For("chr"));
         // Chr.enclosing = Standard;
         // Chr.type = Char.type;
         // Chr.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertCHR);
         // Chr.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateCHR);
         // Standard.locals.Add(Chr);

            ColSum = new PROCEDURE_DECL(Identifier.For("colsum"));
            ColSum.enclosing = Standard;
            ColSum.type = Any;  // depends on the argument!!!
            ColSum.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateCOLSUM);
            ColSum.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertCOLSUM);
            ColSum.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateCOLSUM);
            Standard.locals.Add(ColSum);

            RowSum = new PROCEDURE_DECL(Identifier.For("rowsum"));
            RowSum.enclosing = Standard;
            RowSum.type = Any;  // depends on the argument!!!
            RowSum.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateROWSUM);
            RowSum.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertROWSUM);
            RowSum.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateROWSUM);
            Standard.locals.Add(RowSum);

            Copy = new PROCEDURE_DECL(Identifier.For("copy"));
            Copy.enclosing = Standard;
            Copy.type = Void;
            Copy.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertCOPY);
            Copy.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateCOPY);
            Standard.locals.Add(Copy);

            Dec = new PROCEDURE_DECL(Identifier.For("dec"));
            Dec.enclosing = Standard;
            Dec.type = Void;
            Dec.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertDEC);
            Dec.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateDEC);
            Standard.locals.Add(Dec);

         // Entier = new PROCEDURE_DECL(Identifier.For("entier"));
         // Entier.enclosing = Standard;
         // Entier.type = Integer.type;
         // Entier.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertENTIER);
         // Entier.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateENTIER);
         // Standard.locals.Add(Entier);

            Excl = new PROCEDURE_DECL(Identifier.For("excl"));
            Excl.enclosing = Standard;
            Excl.type = Void;
            Excl.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertEXCL);
            Excl.calculateStandardCall = null;
            Standard.locals.Add(Excl);

         // Float = new PROCEDURE_DECL(Identifier.For("float"));
         // Float.enclosing = Standard;
         // Float.type = Any;  // either FLOAT or FLOAT{w}
         // Float.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateFLOAT);
         // Float.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertFLOAT);
         // Float.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateFLOAT);
         // Standard.locals.Add(Float);

            Find = new PROCEDURE_DECL(Identifier.For("find"));
            Find.enclosing = Standard;
            ARRAY_TYPE at = new ARRAY_TYPE();
            at.isMath = true;
            at.dimensions = new EXPRESSION_LIST(1);
            at.dimensions.Length = 1;
            at.const_dimensions = new int[1];
            at.base_type = Integer.type;
            Find.type = at;
            //Find.type = Integer.type;
            Find.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertFIND);
            Find.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateFIND);
            Standard.locals.Add(Find);

            Dense = new PROCEDURE_DECL(Identifier.For("todense"));
            Dense.enclosing = Standard;
            Dense.type = Any;
            Dense.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateDENSE);
            Dense.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertDENSE);
            Dense.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateDENSE);
            Standard.locals.Add(Dense);

            Halt = new PROCEDURE_DECL(Identifier.For("halt"));
            Halt.enclosing = Standard;
            Halt.type = Void;
            Halt.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertHALT);
            Halt.calculateStandardCall = null;
            Standard.locals.Add(Halt);

            Inc = new PROCEDURE_DECL(Identifier.For("inc"));
            Inc.enclosing = Standard;
            Inc.type = Void;
            Inc.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertINC);
            Inc.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateINC);
            Standard.locals.Add(Inc);

            Incl = new PROCEDURE_DECL(Identifier.For("incl"));
            Incl.enclosing = Standard;
            Incl.type = Void;
            Incl.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertINCL);
            Incl.calculateStandardCall = null;
            Standard.locals.Add(Incl);

            Len = new PROCEDURE_DECL(Identifier.For("len"));
            Len.enclosing = Standard;
            Len.type = Integer.type;
            Len.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertLEN);
            Len.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateLEN);
            Standard.locals.Add(Len);

         // Long = new PROCEDURE_DECL(Identifier.For("long"));
         // Long.enclosing = Standard;
         // Long.type = Any;  // either INTEGER{64} or REAL{64}
         // Long.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateLONG);
         // Long.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertLONG);
         // Long.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateLONG);
         // Standard.locals.Add(Long);

            Low = new PROCEDURE_DECL(Identifier.For("low"));
            Low.enclosing = Standard;
            Low.type = Char.type;
            Low.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertLOW);
            Low.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateLOW);
            Standard.locals.Add(Low);

            Max = new PROCEDURE_DECL(Identifier.For("max"));
            Max.enclosing = Standard;
            Max.type = Any;  // depends on the argument!!!
            Max.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateMAX);
            Max.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertMAX);
            Max.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateMAX);
            Standard.locals.Add(Max);

            Min = new PROCEDURE_DECL(Identifier.For("min"));
            Min.enclosing = Standard;
            Min.type = Any; // depends on the argument!!!
            Min.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateMIN);
            Min.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertMIN);
            Min.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateMIN);
            Standard.locals.Add(Min);

            Odd = new PROCEDURE_DECL(Identifier.For("odd"));
            Odd.enclosing = Standard;
            Odd.type = Boolean.type;
            Odd.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertODD);
            Odd.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateODD);
            Standard.locals.Add(Odd);

         // Ord = new PROCEDURE_DECL(Identifier.For("ord"));
         // Ord.enclosing = Standard;
         // Ord.type = Integer.type;
         // Ord.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertORD);
         // Ord.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateORD);
         // Standard.locals.Add(Ord);

            Pred = new PROCEDURE_DECL(Identifier.For("pred"));
            Pred.enclosing = Standard;
            Pred.type = Any;  // Integer of enum
            Pred.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluatePRED);
            Pred.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertPRED);
            Pred.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculatePRED);
            Standard.locals.Add(Pred);

         // RealProc = new PROCEDURE_DECL(Identifier.For("real"));
         // RealProc.enclosing = Standard;
         // RealProc.type = new REAL_TYPE(32);
         // RealProc.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertREAL);
         // RealProc.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateREAL);
         // Standard.locals.Add(RealProc);

         // Short = new PROCEDURE_DECL(Identifier.For("short"));
         // Short.enclosing = Standard;
         // Short.type = new INTEGER_TYPE(16);
         // Short.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertSHORT);
         // Short.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateSHORT);
         // Standard.locals.Add(Short);

            Size = new PROCEDURE_DECL(Identifier.For("size"));
            Size.enclosing = Standard;
            Size.type = Integer.type;
            Size.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertSIZE);
            Size.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateSIZE);
            Standard.locals.Add(Size);

            Sparse = new PROCEDURE_DECL(Identifier.For("tosparse"));
            Sparse.enclosing = Standard;
            Sparse.type = Any;
            Sparse.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateSPARSE);
            Sparse.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertSPARSE);
            Sparse.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateSPARSE);
            Standard.locals.Add(Sparse);

            Succ = new PROCEDURE_DECL(Identifier.For("succ"));
            Succ.enclosing = Standard;
            Succ.type = Any;
            Succ.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateSUCC);
            Succ.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertSUCC);
            Succ.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateSUCC);
            Standard.locals.Add(Succ);

            Sum = new PROCEDURE_DECL(Identifier.For("sum"));
            Sum.enclosing = Standard;
            Sum.type = Any;  // depends on the argument!!!
            Sum.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateSUM);
            Sum.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertSUM);
            Sum.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateSUM);
            Standard.locals.Add(Sum);

            Unbox = new PROCEDURE_DECL(Identifier.For("unbox"));
            Unbox.enclosing = Standard;
            Unbox.type = Any;
            Unbox.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateUNBOX);
            Unbox.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertUNBOX);
            Unbox.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateUNBOX);
            Standard.locals.Add(Unbox);

         // Val = new PROCEDURE_DECL(Identifier.For("val"));
         // Val.enclosing = Standard;
         // Val.type = Any;
         // Val.evaluateType = new PROCEDURE_DECL.EvaluateType(evaluateVAL);
         // Val.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertVAL);
         // Val.calculateStandardCall = new PROCEDURE_DECL.CalculateStandardCall(calculateVAL);
         // Standard.locals.Add(Val);

            Read = new PROCEDURE_DECL(Identifier.For("read"));
            Read.enclosing = Standard;
            Read.type = Void;
            Read.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertREAD);
            Read.calculateStandardCall = null;
            Standard.locals.Add(Read);

            ReadLn = new PROCEDURE_DECL(Identifier.For("readln"));
            ReadLn.enclosing = Standard;
            ReadLn.type = Void;
            ReadLn.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertREADLN);
            ReadLn.calculateStandardCall = null;
            Standard.locals.Add(ReadLn);

            Write = new PROCEDURE_DECL(Identifier.For("write"));
            Write.enclosing = Standard;
            Write.type = Void;
            Write.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertWRITE);
            Write.calculateStandardCall = null;
            Standard.locals.Add(Write);

            WriteLn = new PROCEDURE_DECL(Identifier.For("writeln"));
            WriteLn.enclosing = Standard;
            WriteLn.type = Void;
            WriteLn.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall(convertWRITELN);
            WriteLn.calculateStandardCall = null;
            Standard.locals.Add(WriteLn);

            //PulseAll = new PROCEDURE_DECL(Identifier.For("PulseAll"));
            //PulseAll.enclosing = Standard;
            //PulseAll.type = Void;
            //PulseAll.convertStandardCall = new PROCEDURE_DECL.ConvertStandardCall();
            //PulseAll.calculateStandardCall = null;
            //Standard.locals.Add(PulseAll);

         // Standard.report(0);
        }

        #region ABS
        // ABS ---------------------------------------------------------

        public static TYPE evaluateABS ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            return call.arguments[0].type;
        }

        public static Node convertABS ( CALL call )
        {
            // a>0 ? a : -a

            if (!validateABS(call)) return null;

            EXPRESSION arg = call.arguments[0];

            call.type = arg.type;  // because initially call.type was ANY_TYPE

            if ((arg.type is INTEGER_TYPE) ||
                // (arg.type is CARDINAL_TYPE) ||
                 (arg.type is REAL_TYPE))
            {
                // (T v=expr; expr<0 ? -expr : expr)
                // =================================
                BlockExpression block = new BlockExpression();
                block.Block = new Block();
                block.Block.Statements = new StatementList();

                VariableDeclaration local = new VariableDeclaration();
                local.Initializer = (Expression)arg.convert();
                local.Name = Identifier.For("Absolute Value");
                local.Type = (TypeNode)arg.type.convert();
                block.Block.Statements.Add(local);

                BinaryExpression comp = new BinaryExpression();
                comp.Operand1 = local.Name;
                comp.Operand2 = new Literal(0, SystemTypes.Int32);
                comp.NodeType = NodeType.Lt;

                TernaryExpression cond = new TernaryExpression();
                cond.Operand1 = comp;
                cond.Operand2 = new UnaryExpression(local.Name, NodeType.Neg);
                cond.Operand3 = local.Name;
                cond.NodeType = NodeType.Conditional;

                ExpressionStatement abs = new ExpressionStatement();
                abs.Expression = cond;
                block.Block.Statements.Add(abs);

                return block;
            }
            
            else if (arg.type is ARRAY_TYPE)
            {
                #region compute math
                if (CONTEXT.useComputeMath) {
                    ConversionState conversionState = new ConversionState();
                    ConversionResult conversionResult;
                    // if argument is data
                    if (ExpressionConverter.Convert(conversionState, arg, true).TryGetValue(out conversionResult)) {
                        // convert method
                        Node node = MethodConverter.Convert(
                            conversionState,
                            conversionResult,
                            new MethodConverter.MethodStruct {
                                Name = "abs",
                                KernelSource = ComputeKernelTemplates.apply,
                                Type = call.type,
                                Func = MethodConverter.ConvertApply,
                                Operation = "fabs(value)"
                            }
                        );
                        if (node != null) {
                            // return openCL call
                            return node;
                        }
                    }
                }
                #endregion

                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                if (arg.type is ARRAY_TYPE)
                {
                    if (!(arg is INDEXER))
                    {
                        mathCall.Callee = new MemberBinding(null,
                            CONTEXT.globalMath.GetElementWiseArrayOp(
                            ((ARRAY_TYPE)arg.type).dimensions.Length,
                            ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                            null,
                            ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                            0,
                            new TernaryExpression(),
                            call.sourceContext));

                        mathCall.Operands.Add(arg.convert() as Expression);
                    }
                    else
                    {
                        mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                        EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                        for (int i = 0; i < indices.Length; i++)
                        {
                            if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                            {
                                mathCall.Operands.Add(indices[i].convert() as Expression);
                            }
                            else if (indices[i].type is RANGE_TYPE)
                            {
                                if (indices[i] is ARRAY_RANGE)
                                {
                                    ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                    mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                    mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                    mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                    mathCall.Operands.Add(cur_range.by.convert() as Expression);
                                }
                                else //it's range_type variable
                                {
                                    mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                        STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                    mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                    mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                        STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                    mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                        STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                                }
                            }
                            else if (indices[i].type is ARRAY_TYPE)
                            {
                                mathCall.Operands.Add(indices[i].convert() as Expression);
                            }
                        }

                        mathCall.Callee = new MemberBinding(null,
                            CONTEXT.globalMath.GetElementWiseArrayOp(
                            ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                            ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                            ((INDEXER)arg).indices,
                            ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                            0,
                            new TernaryExpression(),
                            call.sourceContext));
                    }
                }

                mathCall.Type = arg.type.convert() as TypeNode;
                return mathCall;
            }

            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'abs'", arg.sourceContext);
                return null;
            }
        }

        private static bool validateABS(CALL call)
        {
            EXPRESSION arg = call.arguments[0];

            if ((arg.type is INTEGER_TYPE) || (arg.type is REAL_TYPE)) return true;
            if ((arg.type is ARRAY_TYPE) && (
                (((ARRAY_TYPE)arg.type).base_type is INTEGER_TYPE) ||
                (((ARRAY_TYPE)arg.type).base_type is REAL_TYPE))
                && (((ARRAY_TYPE)arg.type).isMath)) return true;
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'abs'", arg.sourceContext);
                return false;
            }
        }

        private static object calculateABS ( CALL call )
        {
            SourceContext context = call.arguments[0].sourceContext;

            if ( call.arguments.Length > 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"abs");
                return null;
            }
            object arg1 = call.arguments[0].calculate();
            call.type = call.arguments[0].type;  // because initially call.type was ANY_TYPE

            if ( arg1 == null )
            {
             // ERROR.NonConstant(context,"abs"); -- this is the obvious error!!?
                return null;
            }
            if ( arg1 is long )
            {
                if ( (long)arg1<0 ) return -(long)arg1; else return arg1;
            }
            else if ( arg1 is ulong )
            {
                return arg1;
            }
            else if ( arg1 is double )
            {
                double v = (double)arg1;
                if ( v < 0 ) v = -v;
                return v;
            }
            else
            {
                ERROR.WrongPredeclaredCall(context,"abs");
                return null;
            }
        }

        #endregion

        #region ALL
        // ALL ---------------------------------------------------------

        public static TYPE evaluateALL(CALL call)
        {
            if (call.arguments.Length != 1) return null;
            //if (call.arguments[0] is INDEXER)
            //{
            //    if ((((INDEXER)call.arguments[0]).type is ARRAY_TYPE) &&
            //        ((((INDEXER)call.arguments[0]).type as ARRAY_TYPE).isMath))
            //        return (((INDEXER)call.arguments[0]).type as ARRAY_TYPE).base_type;
            //}
            else if ((call.arguments[0].type is ARRAY_TYPE))
                return ((ARRAY_TYPE)call.arguments[0].type).base_type;
            return null;
        }

        public static Node convertALL(CALL call)
        {
            // a>0 ? a : -a

            if (call.arguments.Length != 1)
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "all");
                return null;
            }

            EXPRESSION arg = call.arguments[0];

            //call.type = evaluateALL(call);  // because initially call.type was ANY_TYPE
            call.type = Boolean.type;

            if ((arg.type is ARRAY_TYPE) && (((ARRAY_TYPE)arg.type).isMath))
            {
                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                bool leftIsIndexer = arg is INDEXER;

                if (!leftIsIndexer)
                    mathCall.Operands.Add(arg.convert() as Expression);
                else
                    mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                if (leftIsIndexer)
                {
                    EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                        else if (indices[i].type is RANGE_TYPE)
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                mathCall.Operands.Add(cur_range.by.convert() as Expression);
                            }
                            else //it's range_type variable
                            {
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                            }
                        }
                        else if (indices[i].type is ARRAY_TYPE)
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                    }
                }

                if (!leftIsIndexer)
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        null,
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "All",
                        null,
                        call.sourceContext));
                }
                else
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        ((INDEXER)arg).indices,
                        ((ARRAY_TYPE)((INDEXER)arg).type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "All",
                        null,
                        call.sourceContext));
                }

                //mathCall.Type = evaluateALL(call).convert() as TypeNode;
                mathCall.Type = SystemTypes.Boolean;

                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'all'", arg.sourceContext);
                return null;
            }
        }

        private static object calculateALL(CALL call)
        {
            return null;
        }
        #endregion

        #region ANY
        // ANY ---------------------------------------------------------

        public static TYPE evaluateANY(CALL call)
        {
            if (call.arguments.Length != 1) return null;
            //if (call.arguments[0] is INDEXER)
            //{
            //    if ((((INDEXER)call.arguments[0]).type is ARRAY_TYPE) &&
            //        ((((INDEXER)call.arguments[0]).type as ARRAY_TYPE).isMath))
            //        return (((INDEXER)call.arguments[0]).type as ARRAY_TYPE).base_type;
            //}
            else if ((call.arguments[0].type is ARRAY_TYPE))
                return ((ARRAY_TYPE)call.arguments[0].type).base_type;
            return null;
        }

        public static Node convertANY(CALL call)
        {
            // a>0 ? a : -a

            if (call.arguments.Length != 1)
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "any");
                return null;
            }

            EXPRESSION arg = call.arguments[0];

            //call.type = evaluateANY(call);  // because initially call.type was ANY_TYPE
            call.type = Boolean.type;

            if ((arg.type is ARRAY_TYPE) && (((ARRAY_TYPE)arg.type).isMath))
            {
                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                bool leftIsIndexer = arg is INDEXER;

                if (!leftIsIndexer)
                    mathCall.Operands.Add(arg.convert() as Expression);
                else
                    mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                if (leftIsIndexer)
                {
                    EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                        else if (indices[i].type is RANGE_TYPE)
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                mathCall.Operands.Add(cur_range.by.convert() as Expression);
                            }
                            else //it's range_type variable
                            {
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                            }
                        }
                        else if (indices[i].type is ARRAY_TYPE)
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                    }
                }

                if (!leftIsIndexer)
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        null,
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Any",
                        null,
                        call.sourceContext));
                }
                else
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        ((INDEXER)arg).indices,
                        ((ARRAY_TYPE)((INDEXER)arg).type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Any",
                        null,
                        call.sourceContext));
                }

                //mathCall.Type = evaluateANY(call).convert() as TypeNode;
                mathCall.Type = SystemTypes.Boolean;
                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'any'", arg.sourceContext);
                return null;
            }
        }

        private static object calculateANY(CALL call)
        {
            return null;
        }
        #endregion

        #region ASSERT
        // ASSERT --------------------------------------------------

        public static Node convertASSERT ( CALL call )
        {
            int l = call.arguments.Length;
            if ( l<1 || l>2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"assert");
                return null;
            }

            // assert(arg1,arg2)  =>  if ( !arg1 ) halt(arg2);
            // assert(arg1)       =>  if ( !arg1 ) halt(0);

         // Expression condition = (Expression)call.arguments[0].convert();
            EXPRESSION condition = call.arguments[0];
            Expression cond;
            object c = condition.calculate();
            if ( c != null )
            {
                ERROR.CompilerCalculatedValue("Parameter in 'assert'",c.ToString(),condition.sourceContext);
                // If condition is always true, so we can generate nothing...
                if ( (bool)c ) return null;
                cond = new Literal(false,SystemTypes.Boolean);
            }
            else
                cond = (Expression)condition.convert();

            Expression returnCode;
            if ( l==1 ) returnCode = new Literal(0,SystemTypes.Int32);
            else        returnCode = (Expression)call.arguments[1].convert();

            If if_stmt = new If();

            UnaryExpression if_cond = new UnaryExpression();
            if_cond.Operand = cond;
            if_cond.NodeType = NodeType.LogicalNot;
            if_cond.Type = SystemTypes.Boolean;

            if_stmt.Condition = if_cond;

            if_stmt.TrueBlock = new Block(new StatementList());

            Throw halt = new Throw();

            Construct newHalt = new Construct();
            newHalt.Constructor = new MemberBinding(null,HaltException.GetConstructors()[0]);
                                 // new QualifiedIdentifier(rtlName,Identifier.For("Halt"));
            newHalt.Type = HaltException;
            newHalt.Operands = new ExpressionList();
            newHalt.Operands.Add(new Literal((long)call.sourceContext.StartLine,SystemTypes.Int64));
            newHalt.Operands.Add(new Literal(call.sourceContext.StartColumn,SystemTypes.Int32));
            newHalt.Operands.Add(returnCode);

            halt.Expression = newHalt;
            halt.SourceContext = call.sourceContext;

            if_stmt.TrueBlock.Statements.Add(halt);
            return if_stmt;
        }

        public static object calculateASSERT ( CALL call )
        {
            return null;
        }
        #endregion

        #region BOX
        // BOX ------------------------------------------------------

        public static TYPE evaluateBOX ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            return call.arguments[0].type;
        }

        public static Node convertBOX ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"box");
                return null;
            }
            return null;
        }

        public static object calculateBOX ( CALL call )
        {
         // call.type = ...
            return null;
        }
        #endregion

        #region CAP
        // CAP -------------------------------------------------------
        
        public static Node convertCAP ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"cap");
                return null;
            }
            EXPRESSION arg = call.arguments[0];
            convertStringLiteralToChar(ref arg);

            if ( !(arg.type is CHAR_TYPE) )
            {
                ERROR.IllegalTypeOf(arg.type.ToString(),"the argument for 'cap'",arg.sourceContext);
                return null;
            }

            // System.Char.ToUpper(c);
            TypeNode CharClass = system.GetType(Identifier.For("System"),Identifier.For("Char"));
            Member ToUpper = CharClass.GetMethod(Identifier.For("ToUpper"), SystemTypes.Char);
            
            MethodCall cap = new MethodCall();
            cap.Callee = new MemberBinding(null, ToUpper);
            cap.Operands = new ExpressionList();
            cap.Operands.Add((Expression)arg.convert());
            cap.SourceContext = call.sourceContext;

            return cap;
        }

        private static object calculateCAP ( CALL call )
        {
            object v = call.arguments[0].calculate();
            if ( v == null ) return null;
            if ( !(v is char) ) return null;

            return char.ToUpper((char)v);
        }
        #endregion

        #region COLSUM
        // COLSUM ---------------------------------------------------------

        public static TYPE evaluateCOLSUM(CALL call)
        {
            if (call.arguments.Length != 1) return null;
            else if ((call.arguments[0].type is ARRAY_TYPE))
            {
                ARRAY_TYPE at = new ARRAY_TYPE();
                at.base_type = ((ARRAY_TYPE)call.arguments[0].type).base_type;
                at.isMath = true;
                at.dimensions = new EXPRESSION_LIST(1);
                at.dimensions.Length = 1;
                at.const_dimensions = new int[1];
                return at;
            }
            return null;
        }

        public static Node convertCOLSUM(CALL call)
        {
            if (call.arguments.Length != 1)
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "colsum");
                return null;
            }

            EXPRESSION arg = call.arguments[0];

            call.type = evaluateCOLSUM(call);  // because initially call.type was ANY_TYPE

            if ((arg.type is ARRAY_TYPE) && (((ARRAY_TYPE)arg.type).isMath))
            {
                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();
                mathCall.Operands.Add(arg.convert() as Expression);

                mathCall.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetOptimizedColSumFunction(
                    ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                    call.sourceContext));
                
                mathCall.Type = evaluateCOLSUM(call).convert() as TypeNode;

                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'colsum'", arg.sourceContext);
                return null;
            }
        }

        private static object calculateCOLSUM(CALL call)
        {
            return null;
        }
        #endregion

        #region ROWSUM
        // ROWSUM ---------------------------------------------------------

        public static TYPE evaluateROWSUM(CALL call)
        {
            if (call.arguments.Length != 1) return null;
            else if ((call.arguments[0].type is ARRAY_TYPE))
            {
                ARRAY_TYPE at = new ARRAY_TYPE();
                at.base_type = ((ARRAY_TYPE)call.arguments[0].type).base_type;
                at.isMath = true;
                at.dimensions = new EXPRESSION_LIST(1);
                at.dimensions.Length = 1;
                at.const_dimensions = new int[1];
                return at;
            }
            return null;
        }

        public static Node convertROWSUM(CALL call)
        {
            if (call.arguments.Length != 1)
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "rowsum");
                return null;
            }

            EXPRESSION arg = call.arguments[0];

            call.type = evaluateROWSUM(call);  // because initially call.type was ANY_TYPE

            if ((arg.type is ARRAY_TYPE) && (((ARRAY_TYPE)arg.type).isMath))
            {
                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();
                mathCall.Operands.Add(arg.convert() as Expression);

                mathCall.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetOptimizedRowSumFunction(
                    ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                    call.sourceContext));

                mathCall.Type = evaluateROWSUM(call).convert() as TypeNode;

                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'rowsum'", arg.sourceContext);
                return null;
            }
        }

        private static object calculateROWSUM(CALL call)
        {
            return null;
        }
        #endregion

        #region COPY
        // COPY -------------------------------------------------------        
        public static Node convertCOPY ( CALL call )
        {
            if ( call.arguments.Length != 2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"copy");
                return null;
            }
            EXPRESSION arg1 = call.arguments[0];
            EXPRESSION arg2 = call.arguments[1];

            Expression StringFrom = null;
            Expression ArrayTo = null;

            int err = 0;
            bool vice_versa = false;

            if ( arg1.type is STRING_TYPE )
            {
                StringFrom = (Expression)arg1.convert();
            }
            else if ( arg1.type is EXTERNAL_TYPE )
            {
                Node n = ((EXTERNAL_TYPE)arg1.type).entity;
                if ( (Class)n == SystemTypes.String ) StringFrom = (Expression)arg1.convert();
                else                                  err = 1;
            }
            else if ( arg1.type is ARRAY_TYPE )
            {
                vice_versa = true;
                ARRAY_TYPE a = (ARRAY_TYPE)arg1.type;
                if ( a.dimensions.Length == 1 && a.base_type is CHAR_TYPE )
                    StringFrom = (Expression)arg1.convert();
                else err = 1;
            }
            else err = 1;

            if ( err > 0 )                
                ERROR.IllegalTypeOf(
                    (arg1.type != null)?
                    arg1.type.ToString(): "type of "+arg1.Name,"the 1st argument for 'copy'",arg1.sourceContext);

            if ( vice_versa )
            {
                if ( arg2.type is STRING_TYPE )
                {
                    ArrayTo = (Expression)arg2.convert();
                }
                else if ( arg2.type is EXTERNAL_TYPE )
                {
                    Node n = ((EXTERNAL_TYPE)arg2.type).entity;
                    if ( (Class)n == SystemTypes.String ) ArrayTo = (Expression)arg2.convert();
                    else                                  err = 2;
                }
                else err = 2;
            }
            else
            {
                if ( arg2.type is ARRAY_TYPE )
                {
                    ARRAY_TYPE a = (ARRAY_TYPE)arg2.type;
                    if ( a.base_type is CHAR_TYPE && a.dimensions.Length == 1 )
                        ArrayTo = (Expression)arg2.convert();
                    else err = 2;
                }
                else err = 2;
            }
            if ( err>1 )
            {
                ERROR.IllegalTypeOf(arg2.type.ToString(),"the 2nd argument for 'copy'",arg2.sourceContext);
                return null;
            }

            // int Lx = x.Length;
            // int Lv = v.Length;
            // int i;
            // if ( Lx > Lv ) throw new RangeError(l,c);
            // for ( i=0; (i<Lx)&&(x[i]!=0); i++ )  //(x[i]!=0) only when from array to string
            //     v[i] = x[i];
            Block block = new Block(new StatementList(),call.sourceContext);

            if (arg2.type is STRING_TYPE)
            {
                // Add  var := "";
                AssignmentStatement initDest = new AssignmentStatement();
                initDest.Target = ArrayTo;
                initDest.Source = new Literal("", SystemTypes.String, call.sourceContext);
                block.Statements.Add(initDest);
            }

            VariableDeclaration localx = new VariableDeclaration(Identifier.For("Lx"),SystemTypes.Int32,
                                                                 new QualifiedIdentifier(StringFrom,Identifier.For("Length")));
            VariableDeclaration localv = null;
            VariableDeclaration locali = new VariableDeclaration(Identifier.For("i"),SystemTypes.Int32,null);

        if ( !vice_versa )
            localv = new VariableDeclaration(Identifier.For("Lv"),SystemTypes.Int32,
                                             new QualifiedIdentifier(ArrayTo,Identifier.For("Length")));
            block.Statements.Add(localx);
        if ( !vice_versa ) 
            block.Statements.Add(localv);
            block.Statements.Add(locali);

        if ( !vice_versa )
        {
            If check = new If(null,new Block(new StatementList()),null);
            // x.Length > v.Length
            check.Condition = new BinaryExpression(localx.Name,localv.Name,NodeType.Gt);
          
            // throw new RangeError(l,c);
            TypeNode ArgumentOutOfRangeError = system.GetType(Identifier.For("System"), Identifier.For("ArgumentOutOfRangeException"));
            Throw thro = new Throw();
            Construct ctor = new Construct();
            ctor.Constructor = new MemberBinding(null, ArgumentOutOfRangeError);
            ctor.Type = ArgumentOutOfRangeError;
            ctor.Operands = new ExpressionList();
            thro.Expression = ctor;
            thro.SourceContext = call.sourceContext;
          
            check.TrueBlock.Statements.Add(thro);
          
            block.Statements.Add(check);
        }
            // for ( i=0; (i<Lx)&&(x[i]!=0); i++ )
            //     v[i] = x[i];
            For loop = new For();
            
            loop.Initializer = new StatementList();
                AssignmentStatement init = new AssignmentStatement();
                init.Target = locali.Name;
                init.Source = new Literal(0,SystemTypes.Int32);
            loop.Initializer.Add(init);
            loop.Condition =
                (vice_versa) ? // If copy array to string stop with the first 0
            new BinaryExpression(

                new BinaryExpression(locali.Name, localx.Name, NodeType.Lt), //i<Lx)            
                //x[i]!=0
                new BinaryExpression(new Indexer(StringFrom, new ExpressionList(
                    new Expression[] { (locali.Name) }), SystemTypes.Char),
                    new Literal((char)0, SystemTypes.Char, call.sourceContext), NodeType.Ne),
                    NodeType.And)
             : // Do not check for null when from string to array
            new BinaryExpression(locali.Name, localx.Name, NodeType.Lt); //i<Lx);

            loop.Incrementer = new StatementList();
                AssignmentStatement incr = new AssignmentStatement();
                incr.Operator = NodeType.Add;  // Hope this means += :-)
                incr.Target = locali.Name;
                incr.Source = new Literal(1,SystemTypes.Int32);
            loop.Incrementer.Add(incr);

            loop.Body = new Block(new StatementList());
                if ( vice_versa )
                {
                    AssignmentStatement ass_elem = new AssignmentStatement();
                        ass_elem.Operator = NodeType.Add;  // hope it means +=
                        ass_elem.Target = ArrayTo;

                        Indexer src = new Indexer();
                        src.ElementType = SystemTypes.Char;
                        src.Object = StringFrom;
                        src.Operands = new ExpressionList();
                        src.Operands.Add(locali.Name);
                    ass_elem.Source = src;
                    loop.Body.Statements.Add(ass_elem);
                }
                else
                {
                    AssignmentStatement ass_elem = new AssignmentStatement();
                        Indexer trg = new Indexer();
                        trg.ElementType = SystemTypes.Char;
                        trg.Object = ArrayTo;
                        trg.Operands = new ExpressionList();
                        trg.Operands.Add(locali.Name);
                    ass_elem.Target = trg;

                        Indexer src = new Indexer();
                        src.ElementType = SystemTypes.Char;
                        src.Object = StringFrom;
                        src.Operands = new ExpressionList();
                        src.Operands.Add(locali.Name);
                    ass_elem.Source = src;
                    loop.Body.Statements.Add(ass_elem);
                }

            block.Statements.Add(loop);

            return block;
        }

        private static object calculateCOPY ( CALL call )
        {
            return null;
        }
        #endregion

        #region DEC
        // DEC --------------------------------------------------------
        
        public static Node convertDEC ( CALL call )
        {
            int l = call.arguments.Length;
            if ( l<1 || l>2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"dec");
                return null;
            }

            AssignmentStatement assignment = new AssignmentStatement();
            assignment.Operator = NodeType.Sub;  // Hope this means -= :-)

            assignment.SourceContext = call.sourceContext;

            EXPRESSION arg1 = call.arguments[0];

            if ( arg1 == null ) return null;

            if ( arg1 is INSTANCE )
            {
                INSTANCE instance = (INSTANCE)arg1;
                if ( !(instance.entity is VARIABLE_DECL) ) return null;
                assignment.Target = ((VARIABLE_DECL)(instance.entity)).name;
            }
            else if ( arg1 is SELECTOR )
            {
                SELECTOR arg = arg1 as SELECTOR;
                assignment.Target = (Expression)arg.convert();
                if ( assignment.Target == null ) return null;
            }
            else if ( arg1 is INDEXER )
            {
                INDEXER arg = arg1 as INDEXER;
                assignment.Target = (Expression)arg.convert();
                if ( assignment.Target == null ) return null;
            }
            else // Error: illegal argument in the predeclared procedure
            {
                ERROR.WrongPredeclaredCall(arg1.sourceContext,"dec");
                return null;
            }

            if ( call.arguments.Length == 1 )
            {
                assignment.Source = new Literal(1,SystemTypes.Int32);
            }
            else if ( call.arguments.Length == 2 )
            {
                assignment.Source = (Expression)(call.arguments[1].convert());
            }
            else
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"dec");
                return null;
            }
            return assignment;
        }

        private static object calculateDEC ( CALL call )
        {
            ERROR.PredeclaredCallInConstantExpr(call.sourceContext,"dec");
            return null;
        }
        #endregion

        #region EXCL
        // EXCL ---------------------------------------------------------
        
        public static Node convertEXCL ( CALL call )
        {
            if ( call.arguments.Length != 2 )
            {
                ERROR.WrongPredeclaredCall(call.sourceContext,"excl");
                return null;
            }

            EXPRESSION arg1 = call.arguments[0];
            if ( !(arg1.type is SET_TYPE) )
            {
                ERROR.IllegalTypeOf(arg1.type.ToString(),"the 1st argument for 'excl'",arg1.sourceContext);
                return null;
            }
            if ( !(arg1 is DESIGNATOR) )
            {
                ERROR.IllegalArgumentForVar("1",arg1.sourceContext);
                return null;
            }
            EXPRESSION arg2 = call.arguments[1];
            if ( !(arg2.type is INTEGER_TYPE) && !(arg2.type is CARDINAL_TYPE) )
            {
                ERROR.IllegalTypeOf(arg2.type.ToString(),"the 2nd argument for 'excl'",arg2.sourceContext);
                return null;
            }

            int shift = ((SET_TYPE)arg1.type).width <= 32 ? 0 : 1;
            MethodCall excl = new MethodCall();
            excl.Callee = new MemberBinding(null,STANDARD.Sets.GetMembersNamed(Identifier.For("excl"))[shift]);
            excl.Callee.SourceContext = call.sourceContext;
            excl.Operands = new ExpressionList();
            excl.Operands.Add(new Literal((long)call.sourceContext.StartLine,SystemTypes.Int64));
            excl.Operands.Add(new Literal(call.sourceContext.StartColumn,SystemTypes.Int32));
                    
                UnaryExpression by_ref = new UnaryExpression((Expression)arg1.convert(),NodeType.RefAddress);
                by_ref.SourceContext = arg1.sourceContext;
                by_ref.Type = (TypeNode)arg1.type.convert();  // uint/ulong

            excl.Operands.Add(by_ref);
            excl.Operands.Add((Expression)arg2.convert());

            return excl;
        }

        public static object calculateEXCL ( CALL call )
        {
            return null;
        }
        #endregion

        #region DENSE
        // DENSE ---------------------------------------------------------
        public static TYPE evaluateDENSE(CALL call)
        {
            if (call.arguments.Length != 1) return null;

            if (!validateDENSE(call)) return null;
            SPARSE_TYPE curSparseType = call.arguments[0].type as SPARSE_TYPE;
            ARRAY_TYPE resType = new ARRAY_TYPE();

            resType.isMath = true;
            resType.isOpen = true;
            resType.base_type = curSparseType.base_type;
            resType.dimensions.Add(null);
            if (!curSparseType.isVector) resType.dimensions.Add(null);

            return resType;
        }

        public static Node convertDENSE(CALL call)
        {
            if (!validateDENSE(call)) return null;

            EXPRESSION arg = call.arguments[0];

            call.type = evaluateDENSE(call);  // because initially call.type was ANY_TYPE

            if (arg.type is SPARSE_TYPE)
            {
                TypeNode curType;
                SPARSE_TYPE curSparseType = call.arguments[0].type as SPARSE_TYPE;
                if (curSparseType.isVector)
                    curType = SparseVector.GetTemplateInstance(CONTEXT.symbolTable, 
                        curSparseType.base_type.convert() as TypeNode);
                else
                    curType = SparseMatrix.GetTemplateInstance(CONTEXT.symbolTable,
                        curSparseType.base_type.convert() as TypeNode);
                MethodCall toDense = new MethodCall();
                if (curSparseType.isVector)
                    toDense.Callee = new MemberBinding(call.arguments[0].convert() as Expression, 
                        curType.GetMembersNamed(Identifier.For("ToDenseVector"))[0]);
                else
                    toDense.Callee = new MemberBinding(call.arguments[0].convert() as Expression,
                        curType.GetMembersNamed(Identifier.For("ToDenseMatrix"))[0]);
                toDense.Operands = new ExpressionList();
                
                return toDense;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'todense'", arg.sourceContext);
                return null;
            }
        }

        private static bool validateDENSE(CALL call)
        {
            EXPRESSION arg = call.arguments[0];

            if (arg.type is SPARSE_TYPE) return true;
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'todense'", arg.sourceContext);
                return false;
            }
        }

        private static object calculateDENSE(CALL call)
        {
            return null;
        }
        #endregion

        #region HALT
        // HALT ---------------------------------------------------------

        public static Node convertHALT ( CALL call )
        {
            // throw new Halt(line,pos,code);

            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"halt");
                return null;
            }
/*
            Throw halt = new Throw();

            Construct newHalt = new Construct();
            newHalt.Constructor = new MemberBinding(null,HaltException.GetConstructors()[0]);
                                 // new QualifiedIdentifier(rtlName,Identifier.For("Halt"));
            newHalt.Type = HaltException;
            newHalt.Operands = new ExpressionList();
            newHalt.Operands.Add(new Literal((long)call.sourceContext.StartLine,SystemTypes.Int64));
            newHalt.Operands.Add(new Literal(call.sourceContext.StartColumn,SystemTypes.Int32));
            
            Expression returnCode = (Expression)(call.arguments[0].convert());
            if ( returnCode == null ) // because of compilation errors
                returnCode = new Literal(1,SystemTypes.Int32);
            newHalt.Operands.Add(returnCode);

            halt.Expression = newHalt;
            halt.SourceContext = call.sourceContext;
*/
            MethodCall halt = new MethodCall();
            halt.Callee = new MemberBinding(null, STANDARD.HaltException.GetMembersNamed(Identifier.For("stopTheProgram"))[0]);
            halt.Callee.SourceContext = call.sourceContext;
            halt.Operands = new ExpressionList();
            halt.Operands.Add(new Literal((long)call.sourceContext.StartLine, SystemTypes.Int64));
            halt.Operands.Add(new Literal(call.sourceContext.StartColumn, SystemTypes.Int32));
            Expression returnCode = (Expression)(call.arguments[0].convert());
            if (returnCode == null) // because of compilation errors
                returnCode = new Literal(1, SystemTypes.Int32);
            halt.Operands.Add(returnCode);

            return halt;
        }
        #endregion

        #region FIND
        // FIND ----------------------------------------------------------

        public static Node convertFIND(CALL call)
        {
            if ((call.arguments.Length > 2) || (call.arguments.Length < 1))
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "find");
                return null;
            }

            EXPRESSION arg1 = call.arguments[0];
            EXPRESSION arg2 = null;

            if (call.arguments.Length == 2)
            {
                if (!((arg1.type is INTEGER_TYPE) || (arg1.type is REAL_TYPE) || (arg1.type is CARDINAL_TYPE)
                    || (arg1.type is BOOLEAN_TYPE)))
                {
                    ERROR.IllegalTypeOf(arg1.type.ToString(), "the 1st argument for 'find'", arg1.sourceContext);
                    return null;
                }
                arg2 = call.arguments[1];
                TYPE t = arg2.type;
                if (!((t is ARRAY_TYPE) && (((ARRAY_TYPE)t).isMath) && (((ARRAY_TYPE)t).dimensions.Length == 1)))
                {
                    ERROR.IllegalTypeOf(arg2.type.ToString(), "the 2nd argument for 'find'", arg2.sourceContext);
                    return null;
                }
            }
            else
            {
                TYPE t = arg1.type;
                if (!((t is ARRAY_TYPE) && (((ARRAY_TYPE)t).isMath) && (((ARRAY_TYPE)t).base_type is BOOLEAN_TYPE) && (((ARRAY_TYPE)t).dimensions.Length == 1)))
                {
                    ERROR.IllegalTypeOf(t.ToString(), "the argument for 'find'", arg1.sourceContext);
                    return null;
                }
            }

            MethodCall find = new MethodCall();
            find.Callee = new MemberBinding(null, STANDARD.Math.GetMembersNamed(Identifier.For("find"))[0]);
            find.Callee.SourceContext = call.sourceContext;
            find.Operands = new ExpressionList();
            if (call.arguments.Length == 2)
            {
                find.Operands.Add(arg1.convert() as Expression);
                find.Operands.Add(arg2.convert() as Expression);
            }
            else
            {
                find.Operands.Add(new Literal(true, SystemTypes.Boolean));
                find.Operands.Add(arg1.convert() as Expression);
            }
            //find.Type = new ArrayTypeExpression(SystemTypes.Int32, 1);

            return find;
        }

        public static object calculateFIND(CALL call)
        {
            return null;
        }
        #endregion

        #region INC
        // INC -------------------------------------------------------------

        public static Node convertINC ( CALL call )
        {
            int l = call.arguments.Length;
            if ( l<1 || l>2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"inc");
                return null;
            }

            AssignmentStatement assignment = new AssignmentStatement();
            assignment.Operator = NodeType.Add;  // Hope this means += :-)

            assignment.SourceContext = call.sourceContext;

            EXPRESSION arg1 = call.arguments[0];

            if ( arg1 == null ) return null;

            if ( arg1 is INSTANCE )
            {
                INSTANCE instance = (INSTANCE)arg1;
                if ( !(instance.entity is VARIABLE_DECL) ) return null;
                assignment.Target = ((VARIABLE_DECL)(instance.entity)).name;
            }
            else if ( arg1 is SELECTOR )
            {
                SELECTOR arg = arg1 as SELECTOR;
                assignment.Target = (Expression)arg.convert();
                if ( assignment.Target == null ) return null;
            }
            else if ( arg1 is INDEXER )
            {
                INDEXER arg = arg1 as INDEXER;
                assignment.Target = (Expression)arg.convert();
                if ( assignment.Target == null ) return null;
            }
            else // Error: illegal argument in the predeclared procedure
            {
                ERROR.WrongPredeclaredCall(arg1.sourceContext,"inc");
                return null;
            }

            if ( call.arguments.Length == 1 )
            {
                assignment.Source = new Literal(1,SystemTypes.Int32);
            }
            else if ( call.arguments.Length == 2 )
            {
                assignment.Source = (Expression)(call.arguments[1].convert());
            }
            else
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"inc");
                return null;
            }
            return assignment;
        }

        private static object calculateINC ( CALL call )
        {
            ERROR.PredeclaredCallInConstantExpr(call.sourceContext,"inc");
            return null;
        }
        #endregion

        #region INCL
        // INCL ----------------------------------------------------------

        public static Node convertINCL ( CALL call )
        {
            if ( call.arguments.Length != 2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"incl");
                return null;
            }

            EXPRESSION arg1 = call.arguments[0];
            if ( !(arg1.type is SET_TYPE) )
            {
                ERROR.IllegalTypeOf(arg1.type.ToString(),"the 1st argument for 'incl'",arg1.sourceContext);
                return null;
            }
            if ( !(arg1 is DESIGNATOR) )
            {
                ERROR.IllegalArgumentForVar("1",arg1.sourceContext);
                return null;
            }
            EXPRESSION arg2 = call.arguments[1];
            if ( !(arg2.type is INTEGER_TYPE) && !(arg2.type is CARDINAL_TYPE) )
            {
                ERROR.IllegalTypeOf(arg2.type.ToString(),"the 2nd argument for 'incl'",arg2.sourceContext);
                return null;
            }

            int shift = ((SET_TYPE)arg1.type).width <= 32 ? 0 : 1;
            MethodCall incl = new MethodCall();
            incl.Callee = new MemberBinding(null,STANDARD.Sets.GetMembersNamed(Identifier.For("incl"))[shift]);
            incl.Callee.SourceContext = call.sourceContext;
            incl.Operands = new ExpressionList();
            incl.Operands.Add(new Literal((long)call.sourceContext.StartLine,SystemTypes.Int64));
            incl.Operands.Add(new Literal(call.sourceContext.StartColumn,SystemTypes.Int32));
                    
                UnaryExpression by_ref = new UnaryExpression((Expression)arg1.convert(),NodeType.RefAddress);
                by_ref.SourceContext = arg1.sourceContext;
                by_ref.Type = (TypeNode)arg1.type.convert();  // uint/ulong

            incl.Operands.Add(by_ref);
            incl.Operands.Add((Expression)arg2.convert());

            return incl;
        }

        public static object calculateINCL ( CALL call )
        {
            return null;
        }
        #endregion

        #region LEN
        // LEN ------------------------------------------------------------

        public static Node convertLEN ( CALL call )
        {
            int l = call.arguments.Length;
            if ( l<1 || l>2 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"len");
                return null;
            }

         // int[,] arr = new int[10,10];
         // int i = arr.GetLength(0);
            // Semantic checks
            EXPRESSION arr = call.arguments[0];
            if ( arr.type == null )
            {
                // An error before
                return null;
            }
            if ( !(arr.type is ARRAY_TYPE) && !(arr.type is STRING_TYPE) && !(arr.type is SPARSE_TYPE) )
            {
                ERROR.IllegalTypeOf(arr.type.ToString(),"the argument for 'len'",arr.sourceContext);
                return null;
            }
            if ( call.arguments.Length > 1 )
            {
                object d = call.arguments[1].calculate();
                if ( d != null )
                {
                    if (arr.type is ARRAY_TYPE)
                    {
                        if ((long)d > ((ARRAY_TYPE)arr.type).dimensions.Length - 1)
                        {
                            ERROR.WrongPredeclaredCall(call.arguments[1].sourceContext, "len");
                            return null;
                        }
                    }
                    else if (arr.type is SPARSE_TYPE)
                    {
                        if ((long)d > ((SPARSE_TYPE)arr.type).dimensions.Length - 1)
                        {
                            ERROR.WrongPredeclaredCall(call.arguments[1].sourceContext, "len");
                            return null;
                        }
                    }
                }
            }

            QualifiedIdentifier qual = new QualifiedIdentifier();
            qual.Qualifier = (Expression)call.arguments[0].convert();  // Should yield Identifier
            if (arr.type is ARRAY_TYPE) {
                qual.Identifier = Identifier.For("GetLength");
            } else if (arr.type is STRING_TYPE) {
                qual.Identifier = Identifier.For("Length");
                // Here we generate property (Length) but not methos (GetLength)
                return qual;
            } else if (arr.type is SPARSE_TYPE) {
                qual.Identifier = Identifier.For("GetLength");
            } else {
                return null;
            }

            MethodCall lcall = new MethodCall();
            lcall.Callee = qual;
            lcall.IsTailCall = false;
            lcall.Operands = new ExpressionList();
            lcall.SourceContext = call.sourceContext;

            if ( call.arguments.Length > 1 )
            {
                for ( int i=1,n=call.arguments.Length; i<n; i++ )
                {
                    lcall.Operands.Add((Expression)call.arguments[i].convert());
                }
            }
            else
                lcall.Operands.Add(new Literal(0,SystemTypes.Int32));

         // int[] a = new int[6];
         // int x = a.GetLength(0);


            /*
             * Data a = ...
             * int n = (int)a.GetDimensions()[0];
             */

            if (CONTEXT.useComputeMath && arr.type is ARRAY_TYPE && qual.Qualifier is BinaryExpression && (qual.Qualifier as BinaryExpression).Operand1 is MethodCall) {
                return new BinaryExpression {
                    NodeType = NodeType.Castclass,
                    Operand2 = new MemberBinding {
                        BoundMember = SystemTypes.Int32,
                    },
                    Operand1 = new Indexer {
                        Object = new MethodCall {
                            Callee = new MemberBinding {
                                TargetObject = (((qual.Qualifier as BinaryExpression).Operand1 as MethodCall).Callee as MemberBinding).TargetObject,
                                BoundMember = STANDARD.Data.GetMethod(Identifier.For("GetDimensions")),
                            },
                        },
                        Operands = new ExpressionList(lcall.Operands[0])
                    }
                };
            }

            return lcall;
        }

        private static object calculateLEN ( CALL call )
        {
            return null;
        }
        #endregion

        #region LOW
        // LOW -----------------------------------------------------------
        public static Node convertLOW ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"low");
                return null;
            }

            EXPRESSION arg = call.arguments[0];
            convertStringLiteralToChar(ref arg);

            if ( !(arg.type is CHAR_TYPE) )
            {
                ERROR.IllegalTypeOf(arg.type.ToString(),"the argument for 'low'",arg.sourceContext);
                return null;
            }

            // System.Char.ToLower(c);
            TypeNode CharClass = system.GetType(Identifier.For("System"),Identifier.For("Char"));
            Member ToLower = CharClass.GetMethod(Identifier.For("ToLower"), SystemTypes.Char);
            
            MethodCall low = new MethodCall();
            low.Callee = new MemberBinding(null,ToLower);
            low.Operands = new ExpressionList();
            low.Operands.Add((Expression)arg.convert());
            low.SourceContext = call.sourceContext;

            return low;
        }

        public static object calculateLOW ( CALL call )
        {
            object v = call.arguments[0].calculate();
            if ( v == null ) return null;
            if ( !(v is char) ) return null;

            return char.ToLower((char)v);
        }
        #endregion

        #region MAX
        // MAX -----------------------------------------------------------

        public static TYPE evaluateMAX ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            TYPE t = call.arguments[0].type;

            if ( t is SET_TYPE      ) return STANDARD.Integer.type;
            if ( t is CHAR_TYPE     ) return STANDARD.Integer.type;
            if ( t is INTEGER_TYPE  ) return t;
            if ( t is CARDINAL_TYPE ) return t;
            if ( t is REAL_TYPE     ) return t;
            if ( t is ENUM_TYPE     ) return t;

            if (t is ARRAY_TYPE)
                return (((ARRAY_TYPE)t).base_type);
            return null;
        }

        public static Node convertMAX ( CALL call )
        {
         // if ( call.arguments.Length != 1 ) -- not needed because of special compilation

            // The function tries to calculate the result (it _should_ return a result)
            // and also modifies the _type_ of the whole call because the original type
            // for MAX was ANY_TYPE.

            // The type is taken from the parameter
            // (we have stored it there during parse time).
            EXPRESSION arg = call.arguments[0];
            TYPE t = call.arguments[0].type;

            if (t is SET_TYPE)
            {
                call.type = STANDARD.Integer.type;
                return new Literal((long)(((SET_TYPE)t).width - 1), SystemTypes.Int64);
            }
            else if (t is CHAR_TYPE)
            {
                call.type = STANDARD.Integer.type;
                return new Literal((char)255, SystemTypes.Char);
            }
            else if (t is INTEGER_TYPE)
            {
                call.type = t;
                long w = ((INTEGER_TYPE)t).width;
                if (w <= 8) return new Literal((byte)System.SByte.MaxValue, SystemTypes.Int8);
                if (w <= 16) return new Literal((short)System.Int16.MaxValue, SystemTypes.Int16);
                if (w <= 32) return new Literal((int)System.Int32.MaxValue, SystemTypes.Int32);
                if (w <= 64) return new Literal((long)System.Int64.MaxValue, SystemTypes.Int64);
                else return new Literal((long)System.Int64.MaxValue, SystemTypes.Int64);
            }
            else if (t is CARDINAL_TYPE)
            {
                call.type = t;
                long w = ((CARDINAL_TYPE)t).width;
                if (w <= 8) return new Literal((char)System.Byte.MaxValue, SystemTypes.UInt8);
                if (w <= 16) return new Literal((ushort)System.UInt16.MaxValue, SystemTypes.UInt16);
                if (w <= 32) return new Literal((uint)System.UInt32.MaxValue, SystemTypes.UInt32);
                if (w <= 64) return new Literal((ulong)System.UInt64.MaxValue, SystemTypes.UInt64);
                else return new Literal((ulong)System.UInt64.MaxValue, SystemTypes.UInt64);
            }
            else if (t is REAL_TYPE)
            {
                call.type = t;
                long w = ((REAL_TYPE)t).width;
                if (w <= 32) return new Literal((float)System.Single.MaxValue, SystemTypes.Single);
                if (w <= 64) return new Literal((double)System.Double.MaxValue, SystemTypes.Double);
                //  if ( w <= 96 ) return (decimal)System.Decimal.MaxValue;
                //  else           return (decimal)System.Decimal.MaxValue;
                else return new Literal((double)System.Double.MaxValue, SystemTypes.Double);
            }
            else if (t is ENUM_TYPE)
            {
                call.type = t;
                ENUMERATOR_DECL_LIST el = ((ENUM_TYPE)t).enumerators;
                return new Literal((int)el[el.Length - 1].val, SystemTypes.Int32);
            }
            if ((t is ARRAY_TYPE) && (((ARRAY_TYPE)t).isMath))
            {
                #region compute math
                if (CONTEXT.useComputeMath) {
                    ConversionState conversionState = new ConversionState();
                    ConversionResult conversionResult;
                    // if argument is data
                    if (ExpressionConverter.Convert(conversionState, arg, true).TryGetValue(out conversionResult)) {
                        // convert method
                        Node node = MethodConverter.Convert(
                            conversionState, 
                            conversionResult, 
                            new MethodConverter.MethodStruct {
                                Name = "max",
                                Type = call.type,
                                Func = MethodConverter.ConvertPPS,
                                Identity = "-INFINITY",
                                Operation = "(mine > other)? mine : other"
                            }
                        );
                        if (node != null) {
                            // return openCL call
                            return node;
                        }
                    }
                }
                #endregion

                if (!((((ARRAY_TYPE)t).base_type is INTEGER_TYPE) || (((ARRAY_TYPE)t).base_type is CARDINAL_TYPE) || (((ARRAY_TYPE)t).base_type is REAL_TYPE)))
                {
                    ERROR.IllegalTypeOf(t.ToString(), "the argument for 'max'", t.sourceContext);
                    return null;
                }

                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                bool leftIsIndexer = arg is INDEXER;

                if (!leftIsIndexer)
                    mathCall.Operands.Add(arg.convert() as Expression);
                else
                    mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                if (leftIsIndexer)
                {
                    EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                        else if (indices[i].type is RANGE_TYPE)
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                mathCall.Operands.Add(cur_range.by.convert() as Expression);
                            }
                            else //it's range_type variable
                            {
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                            }
                        }
                        else if (indices[i].type is ARRAY_TYPE)
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                    }
                }

                if (!leftIsIndexer)
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        null,
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Max",
                        calculateMIN(((ARRAY_TYPE)arg.type).base_type, null),
                        call.sourceContext));
                }
                else
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        ((INDEXER)arg).indices,
                        ((ARRAY_TYPE)((INDEXER)arg).type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Max",
                        calculateMIN(((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type, null),
                        call.sourceContext));
                }

                mathCall.Type = evaluateMAX(call).convert() as TypeNode;
                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(t.ToString(), "the argument for 'max'", t.sourceContext);
                return null;
            }
        }

        private static object calculateMAX(TYPE t, CALL call)
        {
            if (t is ARRAY_TYPE) return null;
            if (t is SET_TYPE)
            {
                if (call != null) call.type = STANDARD.Integer.type;
                return (long)(((SET_TYPE)t).width - 1);
            }
            else if (t is CHAR_TYPE)
            {
                if (call != null) call.type = STANDARD.Integer.type;
                return (long)255;
            }
            else if (t is INTEGER_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((INTEGER_TYPE)t).width;
                if (w <= 8) return (byte)System.SByte.MaxValue;
                if (w <= 16) return (short)System.Int16.MaxValue;
                if (w <= 32) return (int)System.Int32.MaxValue;
                if (w <= 64) return (long)System.Int64.MaxValue;
                else return (long)System.Int64.MaxValue;
            }
            else if (t is CARDINAL_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((CARDINAL_TYPE)t).width;
                if (w <= 8) return (char)System.Byte.MaxValue;
                if (w <= 16) return (ushort)System.UInt16.MaxValue;
                if (w <= 32) return (uint)System.UInt32.MaxValue;
                if (w <= 64) return (ulong)System.UInt64.MaxValue;
                else return (ulong)System.UInt64.MaxValue;
            }
            else if (t is REAL_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((REAL_TYPE)t).width;
                if (w <= 32) return (double)System.Single.MaxValue;
                if (w <= 64) return (double)System.Double.MaxValue;
                //  if ( w <= 96 ) return (decimal)System.Decimal.MaxValue;
                //  else           return (decimal)System.Decimal.MaxValue;
                else return (double)System.Double.MaxValue;
            }
            else if (t is ENUM_TYPE)
            {
                if (call != null) call.type = t;
                ENUMERATOR_DECL_LIST el = ((ENUM_TYPE)t).enumerators;
                return (int)el[el.Length - 1].val;
            }
            else
            {
                ERROR.IllegalTypeOf(t.ToString(), "the argument for 'max'", t.sourceContext);
                return null;
            }
        }

        private static object calculateMAX ( CALL call )
        {
            // The function tries to calculate the result (it _should_ return a result)
            // and also modifies the _type_ of the whole call because the original type
            // for MAX was ANY_TYPE.

            // The type is taken from the parameter
            // (we have stored it there during parse time).
            return calculateMAX(call.arguments[0].type, call);
        }
        #endregion

        #region MIN
        // MIN --------------------------------------------------------
        public static TYPE evaluateMIN ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            TYPE t = call.arguments[0].type;

            if ( t is SET_TYPE      ) return STANDARD.Integer.type;
            if ( t is CHAR_TYPE     ) return STANDARD.Integer.type;
            if ( t is INTEGER_TYPE  ) return t;
            if ( t is CARDINAL_TYPE ) return t;
            if ( t is REAL_TYPE     ) return t;
            if ( t is ENUM_TYPE     ) return t;

            if (t is ARRAY_TYPE)
                return (((ARRAY_TYPE)t).base_type);

            return null;
        }

        public static Node convertMIN ( CALL call )
        {
            // The function tries to calculate the result (it _should_ return a result)
            // and also modifies the _type_ of the whole call because the original type
            // for MIN was ANY_TYPE.

            // The type is taken from the parameter
            // (we have stored it there during parse time).
            EXPRESSION arg = call.arguments[0];
            TYPE t = call.arguments[0].type;

            if (t is SET_TYPE || t is CHAR_TYPE)
            {
                call.type = STANDARD.Integer.type;
                return new Literal((int)0,SystemTypes.Int32);
            }
            else if (t is INTEGER_TYPE)
            {
                call.type = t;
                long w = ((INTEGER_TYPE)t).width;
                if (w <= 8) return new Literal(System.SByte.MinValue, SystemTypes.Int8);
                if (w <= 16) return new Literal((short)System.Int16.MinValue, SystemTypes.Int16);
                if (w <= 32) return new Literal((int)System.Int32.MinValue, SystemTypes.Int32);
                if (w <= 64) return new Literal((long)System.Int64.MinValue, SystemTypes.Int64);
                else return new Literal((long)System.Int64.MinValue, SystemTypes.Int64);
            }
            else if (t is CARDINAL_TYPE)
            {
                call.type = t;
                long w = ((CARDINAL_TYPE)t).width;
                if (w <= 8) return new Literal((char)System.Byte.MinValue, SystemTypes.UInt8);
                if (w <= 16) return new Literal((ushort)System.UInt16.MinValue, SystemTypes.UInt16);
                if (w <= 32) return new Literal((uint)System.UInt32.MinValue, SystemTypes.UInt32);
                if (w <= 64) return new Literal((long)System.UInt64.MinValue, SystemTypes.UInt64);
                else return new Literal((long)System.UInt64.MinValue, SystemTypes.UInt64);
            }
            else if (t is REAL_TYPE)
            {
                call.type = t;
                long w = ((REAL_TYPE)t).width;
                if (w <= 32) return new Literal((float)System.Single.MinValue, SystemTypes.Single);
                if (w <= 64) return new Literal((double)System.Double.MinValue, SystemTypes.Double);
                //  if ( w <= 96 ) return (decimal)System.Decimal.MinValue;
                //  else           return (decimal)System.Decimal.MinValue;
                else return new Literal((double)System.Double.MinValue, SystemTypes.Double); ;
            }
            else if (t is ENUM_TYPE)
            {
                call.type = t;
                return new Literal((int)((ENUM_TYPE)t).enumerators[0].val, SystemTypes.Int32); 
            }
            if ((t is ARRAY_TYPE) && (((ARRAY_TYPE)t).isMath))
            {
                #region compute math
                if (CONTEXT.useComputeMath) {
                    ConversionState conversionState = new ConversionState();
                    ConversionResult conversionResult;
                    // if argument is data
                    if (ExpressionConverter.Convert(conversionState, arg, true).TryGetValue(out conversionResult)) {
                        // convert method
                        Node node = MethodConverter.Convert(
                            conversionState,
                            conversionResult,
                            new MethodConverter.MethodStruct {
                                Name = "min",
                                Type = call.type,
                                Func = MethodConverter.ConvertPPS,
                                Identity = "INFINITY",
                                Operation = "(mine < other)? mine : other"
                            }
                        );
                        if (node != null) {
                            // return openCL call
                            return node;
                        }
                    }
                }
                #endregion

                if (!((((ARRAY_TYPE)t).base_type is INTEGER_TYPE) || (((ARRAY_TYPE)t).base_type is CARDINAL_TYPE) || (((ARRAY_TYPE)t).base_type is REAL_TYPE)))
                {
                    ERROR.IllegalTypeOf(t.ToString(), "the argument for 'min'", t.sourceContext);
                    return null;
                }

                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                bool leftIsIndexer = arg is INDEXER;

                if (!leftIsIndexer)
                    mathCall.Operands.Add(arg.convert() as Expression);
                else
                    mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                if (leftIsIndexer)
                {
                    EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                        else if (indices[i].type is RANGE_TYPE)
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                mathCall.Operands.Add(cur_range.by.convert() as Expression);
                            }
                            else //it's range_type variable
                            {
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                            }
                        }
                        else if (indices[i].type is ARRAY_TYPE)
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                    }
                }

                if (!leftIsIndexer)
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        null,
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Min",
                        calculateMAX(((ARRAY_TYPE)arg.type).base_type, null),
                        call.sourceContext));
                }
                else
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        ((INDEXER)arg).indices,
                        ((ARRAY_TYPE)((INDEXER)arg).type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Min",
                        calculateMAX(((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type, null),
                        call.sourceContext));
                }

                mathCall.Type = evaluateMIN(call).convert() as TypeNode;
                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(t.ToString(), "the argument for 'min'", t.sourceContext);
                return null;
            }                        
        }

        private static object calculateMIN(TYPE t, CALL call)
        {
            if (t is ARRAY_TYPE) return null;
            if (t is SET_TYPE || t is CHAR_TYPE)
            {
                if (call != null) call.type = STANDARD.Integer.type;
                return 0L; // new Literal(0,SystemTypes.Int32);
            }
            else if (t is CHAR_TYPE)
            {
                if (call != null) call.type = STANDARD.Integer.type;
                return (long)255;
            }
            else if (t is INTEGER_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((INTEGER_TYPE)t).width;
                if (w <= 8) return (sbyte)System.SByte.MinValue;
                if (w <= 16) return (short)System.Int16.MinValue;
                if (w <= 32) return (int)System.Int32.MinValue;
                if (w <= 64) return (long)System.Int64.MinValue;
                else return (long)System.Int64.MinValue;
            }
            else if (t is CARDINAL_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((CARDINAL_TYPE)t).width;
                if (w <= 8) return (char)System.Byte.MinValue;
                if (w <= 16) return (ushort)System.UInt16.MinValue;
                if (w <= 32) return (uint)System.UInt32.MinValue;
                if (w <= 64) return (ulong)System.UInt64.MinValue;
                else return (ulong)System.UInt64.MinValue;
            }
            else if (t is REAL_TYPE)
            {
                if (call != null) call.type = t;
                long w = ((REAL_TYPE)t).width;
                if (w <= 32) return (double)System.Single.MinValue;
                if (w <= 64) return (double)System.Double.MinValue;
                //  if ( w <= 96 ) return (decimal)System.Decimal.MinValue;
                //  else           return (decimal)System.Decimal.MinValue;
                else return (double)System.Double.MinValue;
            }
            else if (t is ENUM_TYPE)
            {
                if (call != null) call.type = t;
                ENUMERATOR_DECL_LIST el = ((ENUM_TYPE)t).enumerators;
                return (int)el[el.Length - 1].val;
            }
            else
            {
                ERROR.IllegalTypeOf(t.ToString(), "the argument for 'min'", t.sourceContext);
                return null;
            }
        }

        private static object calculateMIN ( CALL call )
        {
            // The function tries to calculate the result (it _should_ return a result)
            // and also modifies the _type_ of the whole call because the original type
            // for MIN was ANY_TYPE.

            // The type is taken from the parameter
            // (we have stored it there during parse time).
            return calculateMIN(call.arguments[0].type, call);
        }
        #endregion

        #region ODD
        // ODD ---------------------------------------------------
        public static Node convertODD ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"odd");
                return null;
            }

            // arg is integer|cardinal

            EXPRESSION arg = call.arguments[0];
            if ( !(arg.type is INTEGER_TYPE) && !(arg.type is CARDINAL_TYPE) )
            {
                ERROR.IllegalTypeOf(arg.type.ToString(),"the argument for 'odd'",arg.sourceContext);
                return null;
            }

            // arg mod 2 = 1
            BinaryExpression mod = new BinaryExpression();
            mod.Operand1 = (Expression)arg.convert();
            mod.Operand2 = new Literal(2,SystemTypes.Int32);
            mod.NodeType = NodeType.Rem;

            BinaryExpression comp = new BinaryExpression();
            comp.Operand1 = mod;
            comp.Operand2 = new Literal(1,SystemTypes.Int32);
            comp.NodeType = NodeType.Eq;

            return comp;
        }

        private static object calculateODD ( CALL call )
        {
            return null;
        }
        #endregion

        #region PRED
        // PRED ----------------------------------------------------
        public static TYPE evaluatePRED ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            TYPE t = call.arguments[0].type;
            if ( t is INTEGER_TYPE ) return t;
            if ( t is CARDINAL_TYPE) return t;
            if ( t is ENUM_TYPE    ) return t;
            if ( t is CHAR_TYPE    ) return t;
            return null;
        }

        public static Node convertPRED ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"pred");
                return null;
            }

            EXPRESSION arg = call.arguments[0];
            convertStringLiteralToChar(ref arg);

            BinaryExpression bin = new BinaryExpression();
            
            if ( arg.type is INTEGER_TYPE )
            {
                bin.Operand1 = (Expression)arg.convert();
                bin.Operand2 = new Literal(1,SystemTypes.Int32);
                bin.NodeType = NodeType.Sub;
                bin.SourceContext = arg.sourceContext;
            }
            else if (arg.type is CARDINAL_TYPE)
            {
                bin.Operand1 = (Expression)arg.convert();
                bin.Operand2 = new Literal(1, SystemTypes.UInt16);
                bin.NodeType = NodeType.Sub;
                bin.SourceContext = arg.sourceContext;
            }
            else if ( arg.type is ENUM_TYPE )
            {
                // v==E.first ? throw new RangeError(l,c),0 : (E)((int)v-1)

                call.type = arg.type;

                ENUM_TYPE et = (ENUM_TYPE)arg.type;


				// v==E.first
                    BinaryExpression comp = new BinaryExpression();
                    comp.Operand1 = (Expression)arg.convert();
                    comp.Operand1.SourceContext = arg.sourceContext;
                    comp.Operand2 = new Literal((int)et.enumerators[0].val,SystemTypes.Int32);
                    comp.NodeType = NodeType.Eq;

                    MethodCall exp = new MethodCall();
                    exp.Callee = new MemberBinding(null, common.GetMembersNamed(Identifier.For("throwRangeError"))[0]);
                    exp.Callee.SourceContext = call.sourceContext;
                    exp.Operands = new ExpressionList();

                    BinaryExpression comma_conv = new BinaryExpression();
                    comma_conv.Operand1 = exp;
                    comma_conv.Operand2 = new MemberBinding(null, (TypeNode)arg.type.convert());
                    comma_conv.NodeType = NodeType.Castclass;

                    // (E)((int)v-1)
                    BinaryExpression minus = new BinaryExpression();
                    minus.Operand1 = (Expression)arg.convert();
                    minus.Operand1.SourceContext = arg.sourceContext;
                    minus.Operand2 = new Literal(1,SystemTypes.Int32);
                    minus.NodeType = NodeType.Sub;
                    minus.SourceContext = call.sourceContext;

                    BinaryExpression minus_conv = new BinaryExpression();
                    minus_conv.Operand1 = minus;
                    minus_conv.Operand2 = new MemberBinding(null,(TypeNode)arg.type.convert());
                    minus_conv.NodeType = NodeType.Castclass;

                TernaryExpression pred = new TernaryExpression();
                pred.Operand1 = comp;
                pred.Operand2 = comma_conv; // (E)(throw ...,0)
                pred.Operand3 = minus_conv;
                pred.NodeType = NodeType.Conditional;

                return pred;
            }
            else if ( arg.type is CHAR_TYPE )
            {
                BinaryExpression conv = new BinaryExpression();
                conv.NodeType = NodeType.Castclass;
                conv.Operand1 = (Expression)arg.convert();
                conv.Operand2 = new MemberBinding(null,SystemTypes.Int32);
                conv.SourceContext = arg.sourceContext;

                bin.Operand1 = conv;
                bin.Operand2 = new Literal(1,SystemTypes.Int32);
                bin.NodeType = NodeType.Sub;
                bin.SourceContext = arg.sourceContext;

                BinaryExpression minus_conv = new BinaryExpression();
                minus_conv.Operand1 = bin;
                minus_conv.Operand2 = new MemberBinding(null,(TypeNode)SystemTypes.Char);
                minus_conv.NodeType = NodeType.Castclass;
                bin = minus_conv;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(),"the argument for 'pred'",arg.sourceContext);
                return null;
            }
            call.type = arg.type;
            return bin;
        }

        private static object calculatePRED ( CALL call )
        {
            call.type = call.arguments[0].type;

            object v = call.arguments[0].calculate();
            if ( v == null ) return null;

            if ( v is long ) return (long)v-1;
            else /* ulong */ return (ulong)v-1;
        }
        #endregion

        #region SIZE
        // SIZE ------------------------------------------------------
        public static Node convertSIZE ( CALL call )
        {
         // if ( call.arguments.Length != 1 ) -- not needed because of special compilation

            return new Literal((int)calculateSIZE(call),SystemTypes.Int32);
        }

        private static object calculateSIZE ( CALL call )
        {
            // The type is taken from the parameter
            // (we have stored it there during parse time).
            TYPE t = call.arguments[0].type;
            int  w = -1;
            if      ( t is INTEGER_TYPE ) w = (int)(((INTEGER_TYPE)t).width/8);
            else if ( t is REAL_TYPE    ) w = (int)(((REAL_TYPE)t).width/8);
            else if ( t is CHAR_TYPE    ) w = (int)(((CHAR_TYPE)t).width/8);
            else if ( t is SET_TYPE     ) w = (int)(((SET_TYPE)t).width/8);
            else if ( t is BOOLEAN_TYPE ) w = 1;
            if (w >= 0) return w;

            TypeNode tt = (TypeNode)t.convert();
            return (int)tt.ClassSize;  // Always 0 (as well as PackingSize)...
        }
        #endregion

        #region SUCC
        // SUCC -------------------------------------------------------
        public static TYPE evaluateSUCC ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            TYPE t = call.arguments[0].type;
            if ( t is INTEGER_TYPE ) return t;
            if ( t is CARDINAL_TYPE) return t;
            if ( t is ENUM_TYPE    ) return t;
            if ( t is CHAR_TYPE    ) return t;
            return null;
        }

        public static Node convertSUCC ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"succ");
                return null;
            }

            EXPRESSION arg = call.arguments[0];
            convertStringLiteralToChar(ref arg);

            BinaryExpression bin = new BinaryExpression();
            
            if ( arg.type is INTEGER_TYPE )
            {
                bin.Operand1 = (Expression)arg.convert();
                bin.Operand2 = new Literal(1,SystemTypes.Int32);
                bin.NodeType = NodeType.Add;
                bin.SourceContext = arg.sourceContext;
            }
            else if (arg.type is CARDINAL_TYPE)
            {
                bin.Operand1 = (Expression)arg.convert();
                bin.Operand2 = new Literal(1, SystemTypes.UInt16);
                bin.NodeType = NodeType.Add;
                bin.SourceContext = arg.sourceContext;
            }
            else if ( arg.type is ENUM_TYPE )
            {
                // v==E.last ? (throw new RangeError(l,c),0) : (E)((int)v+1)

                call.type = arg.type;
                ENUM_TYPE et = (ENUM_TYPE)arg.type;
                
                    // v==E.last
                    BinaryExpression comp = new BinaryExpression();
                    comp.Operand1 = (Expression)arg.convert();
                    comp.Operand1.SourceContext = arg.sourceContext;
                    comp.Operand2 = new Literal((int)et.enumerators[et.enumerators.Length-1].val,SystemTypes.Int32);
                    comp.NodeType = NodeType.Eq;

                    MethodCall exp = new MethodCall();
                    exp.Callee = new MemberBinding(null, common.GetMembersNamed(Identifier.For("throwRangeError"))[0]);
                    exp.Callee.SourceContext = call.sourceContext;
                    exp.Operands = new ExpressionList();

                    BinaryExpression comma_conv = new BinaryExpression();
                    comma_conv.Operand1 =  exp;
                    comma_conv.Operand2 = new MemberBinding(null,(TypeNode)arg.type.convert());
                    comma_conv.NodeType = NodeType.Castclass;

                    // (E)((int)v+1)
                    BinaryExpression plus = new BinaryExpression();
                    plus.Operand1 = (Expression)arg.convert();
                    plus.Operand1.SourceContext = arg.sourceContext;
                    plus.Operand2 = new Literal(1,SystemTypes.Int32);
                    plus.NodeType = NodeType.Add;
                    plus.SourceContext = call.sourceContext;

                    BinaryExpression plus_conv = new BinaryExpression();
                    plus_conv.Operand1 = plus;
                    plus_conv.Operand2 = new MemberBinding(null,(TypeNode)arg.type.convert());
                    plus_conv.NodeType = NodeType.Castclass;
                
                TernaryExpression succ = new TernaryExpression();
                succ.Operand1 = comp;
                succ.Operand2 = comma_conv; // (E)(throw ...,0)
                succ.Operand3 = plus_conv;
                succ.NodeType = NodeType.Conditional;
                succ.SourceContext = call.sourceContext;
                return succ;
            }
            else if ( arg.type is CHAR_TYPE )
            {
                BinaryExpression conv = new BinaryExpression();
                conv.NodeType = NodeType.Castclass;
                conv.Operand1 = (Expression)arg.convert();
                conv.Operand2 = new MemberBinding(null,SystemTypes.Int32);
                conv.SourceContext = arg.sourceContext;

                bin.Operand1 = conv;
                bin.Operand2 = new Literal(1,SystemTypes.Int32);
                bin.NodeType = NodeType.Add;
                bin.SourceContext = arg.sourceContext;

                BinaryExpression plus_conv = new BinaryExpression();
                plus_conv.Operand1 = bin;
                plus_conv.Operand2 = new MemberBinding(null,(TypeNode)SystemTypes.Char);
                plus_conv.NodeType = NodeType.Castclass;
                bin = plus_conv;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(),"the argument for 'succ'",arg.sourceContext);
                return null;
            }
            call.type = arg.type;
            return bin;
        }

        private static object calculateSUCC ( CALL call )
        {
            call.type = call.arguments[0].type;

            object v = call.arguments[0].calculate();
            if ( v == null ) return null;

            if ( v is long ) return (long)v+1;
            else /* ulong */ return (ulong)+1;
        }
        #endregion

        #region SPARSE
        // SPARSE -------------------------------------------------------
        public static TYPE evaluateSPARSE(CALL call)
        {
            if (call.arguments.Length != 1) return null;

            if (!validateSPARSE(call)) return null;
            ARRAY_TYPE curArrayType = call.arguments[0].type as ARRAY_TYPE;
            SPARSE_TYPE resType = new SPARSE_TYPE();

            resType.base_type = curArrayType.base_type;
            for (int i = 0; i < curArrayType.dimensions.Length; i++)
                resType.dimensions.Add(null);
            if (resType.dimensions.Length == 1) resType.isVector = true;
            else resType.isVector = false;

            return resType;
        }

        public static Node convertSPARSE(CALL call)
        {
            if (!validateSPARSE(call)) return null;

            EXPRESSION arg = call.arguments[0];

            call.type = evaluateSPARSE(call);  // because initially call.type was ANY_TYPE

            if (arg.type is ARRAY_TYPE)
            {
                TypeNode curType;
                SPARSE_TYPE curSparseType = call.type as SPARSE_TYPE;
                if (curSparseType.isVector)
                    curType = SparseVector.GetTemplateInstance(CONTEXT.symbolTable,
                        curSparseType.base_type.convert() as TypeNode);
                else
                    curType = SparseMatrix.GetTemplateInstance(CONTEXT.symbolTable,
                        curSparseType.base_type.convert() as TypeNode);
                
                Construct toSparse = new Construct();
                toSparse.Type = curType;
                if (curSparseType.isVector)
                    toSparse.Constructor = new MemberBinding(null,
                        curType.GetConstructors()[5]);
                else
                    toSparse.Constructor = new MemberBinding(null,
                        curType.GetConstructors()[5]);

                toSparse.Operands = new ExpressionList();
                toSparse.Operands.Add(new Literal(
                    call.sourceContext.StartLine, SystemTypes.Int64));
                toSparse.Operands.Add(new Literal(
                    call.sourceContext.StartColumn, SystemTypes.Int32));
                toSparse.Operands.Add(call.arguments[0].convert() as Expression);

                Literal zeroLiteral = new Literal((System.Int32)0, SystemTypes.Int32);
                long width = 0;
                if (curSparseType.base_type is INTEGER_TYPE)
                {
                    width = (curSparseType.base_type as INTEGER_TYPE).width;
                    {
                        if (width <= 16) zeroLiteral = new Literal((System.Int16)0, SystemTypes.Int16);
                        else if (width <= 32) zeroLiteral = new Literal((System.Int32)0, SystemTypes.Int32);
                        else zeroLiteral = new Literal((System.Int64)0, SystemTypes.Int64);
                    }
                }
                else if (curSparseType.base_type is CARDINAL_TYPE)
                {
                    width = (curSparseType.base_type as CARDINAL_TYPE).width;
                    {
                        if (width <= 16) zeroLiteral = new Literal((System.UInt16)0, SystemTypes.UInt16);
                        else if (width <= 32) zeroLiteral = new Literal((System.UInt32)0, SystemTypes.UInt32);
                        else zeroLiteral = new Literal((System.UInt64)0, SystemTypes.UInt64);
                    }
                }
                else if (curSparseType.base_type is REAL_TYPE)
                {
                    width = (curSparseType.base_type as REAL_TYPE).width;
                    {
                        if (width <= 32) zeroLiteral = new Literal((System.Single)0, SystemTypes.Single);
                        else if (width <= 64) zeroLiteral = new Literal((System.Double)0, SystemTypes.Double);
                        else zeroLiteral = new Literal((System.Decimal)0, SystemTypes.Decimal);
                    }
                }

                toSparse.Operands.Add(zeroLiteral);

                return toSparse;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'tosparse'", arg.sourceContext);
                return null;
            }
        }

        private static bool validateSPARSE(CALL call)
        {
            EXPRESSION arg = call.arguments[0];

            if ((arg.type is ARRAY_TYPE) &&
                ((arg.type as ARRAY_TYPE).dimensions.Length == 1) || ((arg.type as ARRAY_TYPE).dimensions.Length == 2)) 
                return true;
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'tosparse'", arg.sourceContext);
                return false;
            }
        }

        private static object calculateSPARSE(CALL call)
        {
            return null;
        }
        #endregion

        #region SUM
        // SUM ---------------------------------------------------------

        public static TYPE evaluateSUM(CALL call)
        {
            if (call.arguments.Length != 1) return null;
            //if (call.arguments[0] is INDEXER)
            //{
            //    if ((((INDEXER)call.arguments[0]).type is ARRAY_TYPE) &&
            //        ((((INDEXER)call.arguments[0]).type as ARRAY_TYPE).isMath))
            //        return (((INDEXER)call.arguments[0]).type as ARRAY_TYPE).base_type;
            //}
            else if ((call.arguments[0].type is ARRAY_TYPE))
                return ((ARRAY_TYPE)call.arguments[0].type).base_type;
            return null;
        }

        public static Node convertSUM(CALL call)
        {
            // a>0 ? a : -a

            if (call.arguments.Length != 1)
            {
                ERROR.WrongNumberOfArgs(call.sourceContext, "sum");
                return null;
            }

            EXPRESSION arg = call.arguments[0];

            call.type = evaluateSUM(call);  // because initially call.type was ANY_TYPE

            if ((arg.type is ARRAY_TYPE) && (((ARRAY_TYPE)arg.type).isMath)) {
                #region compute math
                if (CONTEXT.useComputeMath) {
                    ConversionState conversionState = new ConversionState();
                    ConversionResult conversionResult;
                    // if argument is data
                    if (ExpressionConverter.Convert(conversionState, arg, true).TryGetValue(out conversionResult)) {
                        // convert method
                        Node node = MethodConverter.Convert(
                            conversionState,
                            conversionResult,
                            new MethodConverter.MethodStruct {
                                Name = "sum",
                                Type = call.type,
                                Func = MethodConverter.ConvertPPS,
                                Identity = "0",
                                Operation = "mine + other"
                            }
                        );
                        if (node != null) {
                            // return openCL call
                            return node;
                        }
                    }
                }
                #endregion

                ARRAY_TYPE art = arg.type as ARRAY_TYPE;
                if (!((art.base_type is INTEGER_TYPE) || (art.base_type is CARDINAL_TYPE) || (art.base_type is REAL_TYPE)))
                {
                    ERROR.IllegalTypeOf(art.ToString(), "the argument for 'sum'", art.sourceContext);
                    return null;
                }

                MethodCall mathCall = new MethodCall();
                mathCall.Operands = new ExpressionList();

                bool leftIsIndexer = arg is INDEXER;

                if (!leftIsIndexer)
                    mathCall.Operands.Add(arg.convert() as Expression);
                else
                    mathCall.Operands.Add(((INDEXER)arg).left_part.convert() as Expression);

                if (leftIsIndexer)
                {
                    EXPRESSION_LIST indices = ((INDEXER)arg).indices;
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                        else if (indices[i].type is RANGE_TYPE)
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                mathCall.Operands.Add(cur_range.from.convert() as Expression);
                                mathCall.Operands.Add(new Literal(cur_range.wasToWritten, SystemTypes.Boolean));
                                mathCall.Operands.Add(cur_range.to.convert() as Expression);
                                mathCall.Operands.Add(cur_range.by.convert() as Expression);
                            }
                            else //it's range_type variable
                            {
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("from"))[0]));
                                mathCall.Operands.Add(new Literal(true, SystemTypes.Boolean));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("to"))[0]));
                                mathCall.Operands.Add(new MemberBinding(indices[i].convert() as Expression,
                                    STANDARD.Ranges.GetMembersNamed(Identifier.For("by"))[0]));
                            } 
                        }
                        else if (indices[i].type is ARRAY_TYPE)
                        {
                            mathCall.Operands.Add(indices[i].convert() as Expression);
                        }
                    }
                }

                if (!leftIsIndexer)
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        null,
                        ((ARRAY_TYPE)arg.type).dimensions.Length,
                        ((ARRAY_TYPE)arg.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Sum",
                        null,
                        call.sourceContext));
                }
                else
                {
                    mathCall.Callee = new MemberBinding(null,
                        CONTEXT.globalMath.GetArrayFunction(
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        ((INDEXER)arg).indices,
                        ((ARRAY_TYPE)((INDEXER)arg).type).dimensions.Length,
                        ((ARRAY_TYPE)((INDEXER)arg).left_part.type).base_type.convert() as TypeNode,
                        NodeType.Add,
                        null,
                        "Sum",
                        null,
                        call.sourceContext));
                }

                mathCall.Type = evaluateSUM(call).convert() as TypeNode;
                return mathCall;
            }
            else
            {
                ERROR.IllegalTypeOf(arg.type.ToString(), "the argument for 'sum'", arg.sourceContext);
                return null;
            }
        }

        private static object calculateSUM(CALL call)
        {
            return null;
        }

        // public static Node convertASH ( CALL call )
        // {
        //     return null;
        // }
        #endregion

        #region UNBOX
        // UNBOX ------------------------------------------------------

        public static TYPE evaluateUNBOX ( CALL call )
        {
            if ( call.arguments.Length != 1 ) return null;
            return call.arguments[0].type;
        }

        public static Node convertUNBOX ( CALL call )
        {
            if ( call.arguments.Length != 1 )
            {
                ERROR.WrongNumberOfArgs(call.sourceContext,"unbox");
                return null;
            }

            return null;
        }

        public static object calculateUNBOX ( CALL call )
        {
         // call.type = ...
            return null;
        }
        #endregion

        #region READ READLN
        // READ READLN --------------------------------------------------

        public static Node convertREAD ( CALL call )
        {
            return convertREADinternal(call,false);
        }

        public static Node convertREADLN ( CALL call )
        {
            return convertREADinternal(call,true);
        }

        // READ(v1,v2);               READLN(v1,v2);
        // -----------                -------------
        //
        // try {                      try {
        //     newline();                 newline();
        //     v1 = (T1)read(v1);         v1 = (T1)read(v1);
        //     v2 = (T2)read(v2);         v2 = (T2)read(v2);
        //                                flush();
        // }                          }
        //      catch ( System.FormatException ) {
        //             throw new InputException(l,c);
        //      }

        private static Node convertREADinternal ( CALL call, bool withFlush )
        {
            // try { ... } catch (...) {...}
            //
            Try try_statement = new Try();
            try_statement.TryBlock = new Block();
            try_statement.TryBlock.Statements = new StatementList();
            try_statement.SourceContext = call.sourceContext;

            // try { newline(); ... } catch (...) {...}
            //       ==========
            Member     newline      = Input.GetMembersNamed(Identifier.For("newline"))[0];
            MethodCall newline_call = new MethodCall(new MemberBinding(null,newline),new ExpressionList());

            newline_call.SourceContext = call.sourceContext;
            ExpressionStatement newline_call_statement = new ExpressionStatement(newline_call);
            try_statement.TryBlock.Statements.Add(newline_call_statement);

            // try { newline(); read(ref v); ... } catch (...) {...}
            //                  ============
            for ( int i=0, n=call.arguments.Length; i<n; i++ )
            {
                EXPRESSION arg = call.arguments[i];
                if ( arg == null ) return null;  // because of an error earlier

                Member     callee;
                MethodCall read_call;

                if ( arg.type is UNKNOWN_TYPE )
                    arg.type = (TYPE)arg.type.resolve();

                TYPE t = arg.type;

                // read(v)
                // -------
                if ( t is STRING_TYPE || t is EXTERNAL_TYPE )
                {
                    callee = Input.GetMembersNamed(Identifier.For("read_string"))[0];
                    read_call = new MethodCall(new MemberBinding(null,callee),new ExpressionList());
                }
                else if ( t is OBJECT_TYPE || t is INTERFACE_TYPE )
                {
                    ERROR.NotImplemented("read/readln for user-defined types are");
                    return null;
                }
                else
                {
                    callee = Input.GetMembersNamed(Identifier.For("read"))[0];
                    read_call = new MethodCall(new MemberBinding(null,callee),new ExpressionList());
                }
                Expression argument = (Expression)arg.convert();
                if ( argument == null ) /* Error */ return null;
                argument.SourceContext = arg.sourceContext;

                read_call.SourceContext = call.sourceContext;
                read_call.Operands.Add(argument);
                
                // v = (T)read(v);
                // --------------
                AssignmentStatement read = new AssignmentStatement();
                read.Target = argument;
                    BinaryExpression source = new BinaryExpression();
                    source.NodeType = NodeType.Castclass;
                    source.Operand1 = read_call;
                    source.Operand2 = new MemberBinding(null,(TypeNode)t.convert());
                    source.SourceContext = call.sourceContext;
                read.Source = source;
                read.SourceContext = call.sourceContext;
                    
             // ExpressionStatement read_call_statement = new ExpressionStatement(read_call);
                try_statement.TryBlock.Statements.Add(read); //read_call_statement);
            }

            // try { newline(); read(ref v); ...; flush(); } catch (...) {...}
            //                                    ========
            if ( withFlush )
            {
                Member callee = Input.GetMembersNamed(Identifier.For("flush"))[0];
                MethodCall flush_call = new MethodCall(new MemberBinding(null,callee),new ExpressionList());
                ExpressionStatement flush_call_statement = new ExpressionStatement(flush_call);
                try_statement.TryBlock.Statements.Add(flush_call_statement);
            }

            // try { newline(); read(ref v); ...; flush(); } catch (...) {...}
            //                                               =================
            try_statement.Catchers = new CatchList();
            Catch catch_handler = new Catch();

            // try { ... } catch (System.FormatException) {...}
            //                    ======================
            TypeNode fe = system.GetType(Identifier.For("System"),Identifier.For("FormatException"));
            
            catch_handler.TypeExpression = null; //////////////////////////////???;
            catch_handler.Type = fe;
         // catch_handler.Variable = Identifier.For("h");

            // try { ... } catch (System.FormatException) { throw new InputException(l,c); }
            //                                              ==============================
            catch_handler.Block = new Block(new StatementList());

            Throw throw_statement = new Throw();

            Construct ctor = new Construct();
            ctor.Constructor = new QualifiedIdentifier(rtlName,Identifier.For("InputError"));
            ctor.Operands = new ExpressionList();
            ctor.Operands.Add(new Literal((long)call.sourceContext.StartLine,SystemTypes.Int64));
            ctor.Operands.Add(new Literal(call.sourceContext.StartColumn,SystemTypes.Int32));

            throw_statement.Expression = ctor;
            catch_handler.Block.Statements.Add(throw_statement);

            try_statement.Catchers.Add(catch_handler);

            return try_statement;

         // AssignmentStatement assignment = new AssignmentStatement();
         // assignment.Operator = NodeType.Nop;  // Hope this means "normal" assignment :-)
         //
         //     MethodCall read_call = new MethodCall();
         //     read_call.Callee = new QualifiedIdentifier(new QualifiedIdentifier(Identifier.For("System"),
         //                                                                        Identifier.For("Console")),
         //                                                Identifier.For(name));
         //     read_call.Operands = new ExpressionList();  // empty!
         //
         // assignment.Source = read_call;
         // assignment.SourceContext = call.sourceContext;
         // assignment.Target = (Expression)(call.arguments[0].convert());
         //
         // return assignment;
        }
        #endregion

        #region WRITE WRITELN
        // WRITE WRITELN -------------------------------------------

        public static Node convertWRITE ( CALL call )
        {
            // WRITE(expr); ==> System.Console.Write(expr);
            return convertWRITEinternal(call,false); //"Write");
        }

        public static Node convertWRITELN ( CALL call )
        {
            // WRITELN(expr); ==> System.Console.WriteLine(expr);
            return convertWRITEinternal(call,true); //"WriteLine");
        }

     // private static Node convertWRITEinternal ( CALL call, string name )
     // {
     //     MethodCall write_call = new MethodCall();
     //
     //     write_call.Callee = new QualifiedIdentifier(new QualifiedIdentifier(Identifier.For("System"),
     //                                                                         Identifier.For("Console")),
     //                                                 Identifier.For(name));
     //     write_call.Operands = new ExpressionList();
     //
     //     for ( int i=0, n=call.arguments.Length; i<n; i++ )
     //     {
     //         EXPRESSION argument = call.arguments[i];
     //
     //         if ( argument.type is UNKNOWN_TYPE )
     //             argument.type = (TYPE)argument.type.resolve();
     //
     //         TYPE t = argument.type;
     //         if ( t is OBJECT_TYPE || t is INTERFACE_TYPE )
     //         {
     //             ERROR.NotImplemented("write/writeln for user-defined types are");
     //             return null;
     //         }
     //         write_call.Operands.Add((Expression)argument.convert());
     //     }
     //     return write_call;
     // }
    
        private static Node convertWRITEinternal ( CALL call, bool nl )
        {
            if ( call.arguments.Length == 0 )
            {
                if ( !nl ) return null; // nothing is generated

                MethodCall new_line = new MethodCall();
                Method new_liner = Console.GetMethod(Identifier.For("WriteLine"),null);
                new_line.Callee = new MemberBinding(null,new_liner);
             // new_line.Callee = new QualifiedIdentifier
             //                             (new QualifiedIdentifier(Identifier.For("System"),
             //                                                      Identifier.For("Console")),
             //                              Identifier.For("WriteLine"));
                new_line.Operands = new ExpressionList();
                new_line.SourceContext = call.sourceContext;
                return new_line;
            }

            int i = 0;
            string name = nl ? "writeln" : "write";

            Block block = new Block(new StatementList());

            AssemblyNode system = AssemblyNode.GetAssembly(typeof(System.Threading.Monitor).Assembly);
            TypeNode monitor = system.GetType(Identifier.For("System.Threading"), Identifier.For("Monitor"));

            TypeNode typenode = system.GetType(Identifier.For("System"), Identifier.For("Console"));
            UnaryExpression ldtoken = new UnaryExpression(new Literal(typenode, SystemTypes.RuntimeTypeHandle), NodeType.Ldtoken, SystemTypes.IntPtr);
            MethodCall type_of = new MethodCall(new MemberBinding(null, Runtime.GetTypeFromHandle), new ExpressionList(ldtoken), NodeType.Call, SystemTypes.Type);
            

            MethodCall lcall = new MethodCall(); // do not reuse the previous call node!!!
            lcall.Operands = new ExpressionList();
            lcall.Callee = new QualifiedIdentifier(new MemberBinding(null, monitor), Identifier.For("Enter"));
            lcall.Operands.Add(type_of);

            block.Statements.Add(new ExpressionStatement(lcall));

            while ( i < call.arguments.Length )
            {
                long width = -1;
                if ( call.arguments[i+1] != null )
                {
                    object gen_width = call.arguments[i+1].calculate();
                    if ( gen_width == null )
                    {
                        ERROR.NonConstant(call.arguments[i+1].sourceContext,name+"'s width");
                        return null;
                    }
                    else
                    {
                        width = (long)gen_width;
                    }
                }

                long width2 = -1;
                if ( call.arguments[i+2] != null )
                {
                    object gen_width2 = call.arguments[i+2].calculate();
                    if ( gen_width2 == null )
                    {
                        ERROR.NonConstant(call.arguments[i+2].sourceContext,name+"'s mantissa width");
                        return null;
                    }
                    else
                    {
                        width2 = (long)gen_width2;
                    }
                }

                if ( width < width2 )
                {
                    ERROR.WrongWidthInWrite(call.arguments[i].sourceContext);
                    return null;
                }

                EXPRESSION argument = null;
                if (call.arguments[i] != null)
                    argument = call.arguments[i].extendProcType();
                else
                    ERROR.UnknownType(call.sourceContext, "argument " + (i / 3 + 1).ToString());

                if ( argument == null ) return null; // an error before

                if ( argument.type is UNKNOWN_TYPE )
                    argument.type = (TYPE)argument.type.resolve();

                TYPE t = argument.type;
                if ( t is OBJECT_TYPE || t is INTERFACE_TYPE || t is ENUM_TYPE || t is PROC_TYPE )
                {
                    ERROR.IllegalTypeOf(t.ToString(),"the argument for 'write(ln)'",argument.sourceContext);
                    return null;
                }

                if ( !(t is REAL_TYPE) && width2 != -1 )
                {
                    ERROR.ExtraWidthInWrite(call.arguments[i+2].sourceContext);
                    return null;
                }

                i+=3;

                MethodCall write = new MethodCall();
                write.Operands = new ExpressionList();
                write.SourceContext = call.sourceContext;

                Member writer = null;
                Expression arg = (Expression)argument.convert();
                Expression spec1 = null;
                Expression spec2 = null;
                Expression nl_expr = null;

            Again:
                if ( t is CHAR_TYPE )
                {
                    writer = Output.GetMembersNamed(Identifier.For("writeChar"))[0];
                    if ( width == -1 ) width = 1;
                }
                else if ( t is INTEGER_TYPE || t is CARDINAL_TYPE )
                {
                    writer = Output.GetMembersNamed(Identifier.For("writeInt"))[0];
                    if ( width == -1 ) width = 20;
                }
                else if ( t is STRING_TYPE )
                {
                    writer = Output.GetMembersNamed(Identifier.For("writeString"))[0];
                    if ( width == -1 ) width = 4;
                }
                else if ( t is BOOLEAN_TYPE )
                {
                    writer = Output.GetMembersNamed(Identifier.For("writeBool"))[0];
                    if ( width == -1 ) width = 6;
                }
                else if ( t is REAL_TYPE )
                {
                    int shift = width2==-1 ? 0 : 1;
                    writer = Output.GetMembersNamed(Identifier.For("writeReal"))[shift];
                    if ( width == -1 ) width = 20;
                    if ( width2 != -1 )
                        spec2 = new Literal(width2,SystemTypes.Int64);
                }
                else if ( t is EXTERNAL_TYPE )
                {
                    t = TYPE.convertToZonnon((EXTERNAL_TYPE)t);
                    if (t is EXTERNAL_TYPE)
                    {
                        ERROR.IllegalTypeOf(t.ToString(), "the argument for 'write/writeln'", argument.sourceContext);
                        return null;
                    }
                    goto Again;
                }
                else if ( t is UNKNOWN_TYPE )
                {
                    // ... Because of some previous errors
                    return null;
                    // Diagnostics should be already issued
                }
                else
                {
                    if ( t == null ) return null;
                    ERROR.IllegalTypeOf(t.ToString(),"the argument for 'write/writeln'",argument.sourceContext);
                    return null;
                }
                spec1 = new Literal(width,SystemTypes.Int64);

                write.Callee = new MemberBinding(null,writer);

                if ( arg != null ) write.Operands.Add(arg);
                else               write.Operands.Add(new Literal(null,SystemTypes.Object));
                write.Operands.Add(spec1);
                if ( spec2 != null ) write.Operands.Add(spec2);

                if ( nl && i>=call.arguments.Length ) nl_expr = new Literal(true,SystemTypes.Boolean);
                else                                  nl_expr = new Literal(false,SystemTypes.Boolean);
                write.Operands.Add(nl_expr);

                ExpressionStatement expr = new ExpressionStatement();
                expr.Expression = write;
                expr.SourceContext = call.sourceContext;
                block.Statements.Add(expr);
            }

            lcall = new MethodCall(); // do not reuse the previous call node!!!
            lcall.Operands = new ExpressionList();
            lcall.Callee = new QualifiedIdentifier(new MemberBinding(null, monitor), Identifier.For("Exit"));
            lcall.Operands.Add(type_of);

            block.Statements.Add(new ExpressionStatement(lcall));
            return block;
        }

        // Service -------------------------------------------------------------------------

        private static void convertStringLiteralToChar ( ref EXPRESSION arg )
        {
            if ( arg is STRING_LITERAL )
            {
                string str = ((STRING_LITERAL)arg).str;
                if ( str.Length == 1 )
                {
                    // String literal of length 1 is treated here as character literal.
                    // Replace STRING_TYPE for CHAR_TYPE for more adequate diagnostics
                    // Create CHAR_LITERAL node and replace original STRING_LITERAL node in the tree.
                    arg = CHAR_LITERAL.create(str[0],arg.sourceContext);
                }
            }
        }
        #endregion

        // ----------------------------------------------------------------------------------
        

        // ----------------------------------------------------------------------------------
#if DEBUG
        public static void report ( )
        {
            Standard.report(0);
        }
#endif
    }
}
