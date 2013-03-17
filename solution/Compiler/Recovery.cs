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
using System.Collections;

namespace ETH.Zonnon
{
    // Things for error recovery

    internal sealed class Recovery
	{
     // private Scanner scanner;

		public Recovery ( )
        {
        }

        private const int size = (int)LEXEM.UserDefined+1;

        public static readonly BitArray Colon_Array_LeftParenth_Id;
        public static readonly BitArray Comma_Colon_RightParenth;
        public static readonly BitArray Comma_Of_Semicolon;
        public static readonly BitArray Comma_RightBrace_Id;
        public static readonly BitArray Comma_RightParenth;
        public static readonly BitArray End_Dot;
        public static readonly BitArray End_Id_Dot;
        public static readonly BitArray Equal_Array_LeftParenth_Id;
        public static readonly BitArray Equal_Semicolon;
        public static readonly BitArray Import_Type_Const_Var_Procedure_End;
        public static readonly BitArray LeftParenth_Id;
        public static readonly BitArray RightBrace_Id;
    //  public static readonly BitArray RightParenth;
    //  public static readonly BitArray Semicolon;
        public static readonly BitArray Semicolon_Colon_RightParenth;
        public static readonly BitArray Semicolon_Comma;
        public static readonly BitArray Semicolon_Implements_Import_Type_Const_Var_Procedure_End;
        public static readonly BitArray Semicolon_Import_Type_Const_Var_Procedure_End;
    //  public static readonly BitArray Semicolon_Comma_Import_Type_Const_Var_Procedure_End;
        public static readonly BitArray Semicolon_Refines_Import_Type_Const_Var_Procedure_End;
    //  public static readonly BitArray Semicolon_Comma_Type_Const_Var_Procedure_Begin_End;
        public static readonly BitArray Semicolon_Type_Const_Var_Procedure_End;
        public static readonly BitArray Semicolon_Type_Const_Var_Procedure_Begin_End;
        public static readonly BitArray Type_Const_Var_Procedure_End;
        public static readonly BitArray Dot_Module_Definition_Implementation_Object;

        public static readonly BitArray Comma_Assignment;
        public static readonly BitArray Comma_Semicolon_End_Else;

        // TYPE  ...
        // CONST ...
        // VAR   ...
        public static readonly BitArray Type_Const_Var;

        // ;
        // PROCEDURE
        // BEGIN
        // END
        public static readonly BitArray Procedure_Begin_End;

        // ;
        // PROCEDURE
        // END
        public static readonly BitArray Procedure_End;

        // OBJECT
        // DEFINITION
        // IMPLEMENTATION
        public static readonly BitArray Object_Definition_Implementation;

        //--------------------------------------------------------------------

        // MODULE m IMPLEMENTS a.b.c;
        //   IMPORT ...
        //   TYPE/VAR/CONST ...
        //   OBJECT/DEFINITION/IMPLEMENTATION
        //   PROCEDURE
        // BEGIN
        // END m
        public static readonly BitArray AfterModuleName;

        // DEFINITION d REFINES a.b.s;
        //   IMPORT ...
        //   TYPE/VAR/CONST ...
        //   PROCEDURE
        // END d
        public static readonly BitArray AfterDefinitionName;

        // IMPLEMENTATION d;
        //   IMPORT ...
        //   TYPE/VAR/CONST ...
        //   PROCEDURE
        // END d
        public static readonly BitArray AfterImplementationName;

        // OBJECT o IMPLEMENTS a.b.c;
        //   IMPORT ...
        //   TYPE/VAR/CONST ...
        //   PROCEDURE
        // BEGIN
        // END o
        public static readonly BitArray AfterObjectName;

        // RECORD r;
        //     Simple Variable Declarations
        // BEGIN
        //     Statements
        // END r
        public static readonly BitArray AfterRecordName;

        //--------------------------------------------------------------------

        // MODULE m IMPLEMENTS a.b.c;           OBJECT o IMPLEMENTS a.b.c;
        //   IMPORT ...                             IMPORT ...
        //   TYPE/VAR/CONST ...                     TYPE/VAR/CONST ...
        //   OBJECT/DEFINITION/IMPLEMENTATION
        //   PROCEDURE                              PROCEDURE
        // BEGIN                                  BEGIN
        // END                                    END
        public static readonly BitArray ExportInModule;  // IMPORT TYPE/CONST/VAR  OBJECT/DEFINITION/IMPLEMENTATION  PROCEDURE  BEGIN  END
        public static readonly BitArray ExportInObject;  // IMPORT TYPE/CONST/VAR                                    PROCEDURE  BEGIN  END

        // MODULE m EXPORTS qual-id, qual-id, ... ;
        //                         =        =     =
        public static readonly BitArray InsideExport;    //  ',' | ';'

        //--------------------------------------------------------------------

        // IMPORT a.b.c AS c;  -- internal case
        //
        // MODULE m ...            OBJECT o ...             DEFINITION d ...         IMPLEMENTATION i ...
        //   IMPORT a.b.c AS c;      IMPORT a.b.c. AS c;      IMPORT a.b.c. AS c;      IMPORT a.b.c. AS c;
        //   TYPE  ...               TYPE   ...               TYPE   ...               TYPE   ...
        //   VAR   ...               VAR    ...               VAR    ...               VAR    ...
        //   CONST ...               CONST  ...               CONST  ...               CONST  ...
        //   OBJECT
        //   DEFINITION
        //   IMPLEMENTATION
        //   PROCEDURE               PROCEDURE                PROCEDURE                PROCEDURE
        // BEGIN                   BEGIN
        // END                     END                      END                        END
        //
        // IMPORT a.b.c AS c;  -- global case
        //
        // MODULE
        // OBJECT
        // DEFINITION
        // IMPLEMENTATION
        //
        public static readonly BitArray ImportInModule;          //  TYPE/CONST/VAR  OBJECT/DEFINITION/IMPLEMENTATION  PROCEDURE  BEGIN  END
        public static readonly BitArray ImportInObject;          //  TYPE/CONST/VAR                                    PROCEDURE  BEGIN  END
        public static readonly BitArray ImportInDefinition;      //  TYPE/CONST/VAR                                    PROCEDURE         END
        public static readonly BitArray ImportInImplementation;  //  TYPE/CONST/VAR                                    PROCEDURE         END
        public static readonly BitArray ImportGlobal;            //  MODULE          OBJECT/DEFINITION/IMPLEMENTATION

        // IMPORT qual-id AS id, qual-id AS id, ... ;
        //                ==   =                    =
        public static readonly BitArray InsideImport;            //  ',' | 'AS' | ';'

        // MODULE m;
        //    ...
        //    OBJECT o; ... END o;
        //    OBJECT ...
        //    DEFINITION ...
        //    IMPLEMENTATION ...
        // END m.
        //
        public static readonly BitArray AfterLocalUnit;

        //--------------------------------------------------------------------

        // Which lexems can follow statements or parts of compound statements?

        public static readonly BitArray EndOfStatement;       // ';', END, ON, ELSE, ELSIF, UNTIL

        public static readonly BitArray EndOfBlockStatement;  // ';', ON, END
        public static readonly BitArray EndOfException;       // ';', ON, END
        public static readonly BitArray EndOfIfCondition;     //      THEN, ELSIF, ELSE, END
        public static readonly BitArray EndOfThenCase;        // ';', ELSE, ELSIF, END
        public static readonly BitArray EndOfElsifCase;       // ';', ELSE, ELSIF, END
        public static readonly BitArray EndOfElseCase;        // ';', END
        public static readonly BitArray EndOfCaseBranch;      // ';', ELSE, END, '|'
        public static readonly BitArray EndOfCase;            // ';', END
        public static readonly BitArray EndOfWhile;           // ';', END
        public static readonly BitArray EndOfRepeat;          // ';', UNTIL
        public static readonly BitArray EndOfLoop;            // ';', END
        public static readonly BitArray EndOfFor;             // ';', END

        //----------------------------------------------------------------------------------

        private static void initialize ( ref BitArray bits, params LEXEM[] lexems )
        {
            for ( int i=0, n=lexems.GetLength(0); i<n; i++ )
                bits[(int)lexems[i]] = true;
        }

        private static void initialize ( ref BitArray bits, ref BitArray adds )
        {
            for ( int i=0, n=adds.Length; i<n; i++ )
            {
                if ( adds[i] ) bits[i] = true;
            }
        }

        //-----------------------------------------------------------------------------------

        static Recovery()
        {
            Type_Const_Var = new BitArray(size);
            initialize(ref Type_Const_Var,LEXEM.Type,LEXEM.Const,LEXEM.Var);

            Procedure_Begin_End = new BitArray(size);
            initialize(ref Procedure_Begin_End,LEXEM.Procedure,LEXEM.Begin,LEXEM.End);

            Procedure_End = new BitArray(size);
            initialize(ref Procedure_End,LEXEM.Procedure,LEXEM.End);

            Object_Definition_Implementation = new BitArray(size);
            initialize(ref Object_Definition_Implementation,LEXEM.Object,LEXEM.Definition,LEXEM.Implementation);

            //-------------------------------------------------------------------------------

            AfterModuleName = new BitArray(size);
            initialize(ref AfterModuleName,LEXEM.Semicolon,LEXEM.Refines,LEXEM.Import);
            initialize(ref AfterModuleName,ref Type_Const_Var);
            initialize(ref AfterModuleName,ref Procedure_Begin_End);
            initialize(ref AfterModuleName,ref Object_Definition_Implementation);

            AfterDefinitionName = new BitArray(size);
            initialize(ref AfterDefinitionName,LEXEM.Semicolon,LEXEM.Refines,LEXEM.Import);
            initialize(ref AfterDefinitionName,ref Type_Const_Var);
            initialize(ref AfterDefinitionName,ref Procedure_End);

            AfterImplementationName = new BitArray(size);
            initialize(ref AfterImplementationName,    LEXEM.Semicolon,LEXEM.Import);
            initialize(ref AfterImplementationName,ref Type_Const_Var);
            initialize(ref AfterImplementationName,ref Procedure_End);

            AfterObjectName = AfterModuleName;

            AfterRecordName = new BitArray(size);
            initialize(ref AfterRecordName,LEXEM.End,LEXEM.Begin);

            //-------------------------------------------------------------------------------
            ExportInObject = new BitArray(size);
            initialize(ref ExportInObject, LEXEM.Import);
            initialize(ref ExportInObject, ref Type_Const_Var);
            initialize(ref ExportInObject, ref Procedure_Begin_End);

            ExportInModule = new BitArray(size);
            initialize(ref ExportInModule,    LEXEM.Import);
            initialize(ref ExportInModule,ref Type_Const_Var);
            initialize(ref ExportInModule,ref Object_Definition_Implementation);
            initialize(ref ExportInModule,ref Procedure_Begin_End);

            InsideExport = new BitArray(size);
            initialize(ref InsideExport,LEXEM.Comma,LEXEM.Semicolon); //  ',' | ';'

            //-----------------------------------------------------------------------------------

            ImportInModule         = new BitArray(size);
            ImportInObject         = new BitArray(size);
            ImportInDefinition     = new BitArray(size);
            ImportInImplementation = new BitArray(size);
            ImportGlobal           = new BitArray(size);

            initialize(ref ImportInModule,        ref Procedure_Begin_End);
            initialize(ref ImportInModule,        ref Type_Const_Var);
            initialize(ref ImportInModule,        ref Object_Definition_Implementation);

            initialize(ref ImportInObject,        ref Procedure_Begin_End);
            initialize(ref ImportInObject,        ref Type_Const_Var);

            initialize(ref ImportInDefinition,    ref Procedure_End);
            initialize(ref ImportInDefinition,    ref Type_Const_Var);

            initialize(ref ImportInImplementation,ref Procedure_End);
            initialize(ref ImportInImplementation,ref Type_Const_Var);

            initialize(ref ImportGlobal,              LEXEM.Module);
            initialize(ref ImportGlobal,          ref Object_Definition_Implementation);

            //---------------------------------------------------------------------------------

            InsideImport = new BitArray(size);

            initialize(ref InsideImport,          LEXEM.As,LEXEM.Comma,LEXEM.Semicolon);

            //---------------------------------------------------------------------------------

            AfterLocalUnit = new BitArray(size);

            initialize(ref AfterLocalUnit, ref Object_Definition_Implementation);
            initialize(ref AfterLocalUnit,     LEXEM.Procedure);
            initialize(ref AfterLocalUnit,     LEXEM.Activity);
            initialize(ref AfterLocalUnit,     LEXEM.Operator);
            initialize(ref AfterLocalUnit,     LEXEM.End);

            //---------------------------------------------------------------------------------

            EndOfStatement = new BitArray(size);                       // ';', END, ON, ELSE, ELSIF, UNTIL
            initialize(ref EndOfStatement,LEXEM.Semicolon,LEXEM.End,LEXEM.Else,LEXEM.Elsif,LEXEM.On,LEXEM.Until);

            EndOfBlockStatement = new BitArray(size);                  // ';', ON, END
            EndOfException = EndOfBlockStatement;
            initialize(ref EndOfBlockStatement,LEXEM.Semicolon,LEXEM.On,LEXEM.End);

            EndOfElseCase  = new BitArray(size);                       // ';' or END
            EndOfCase      = EndOfElseCase;
            EndOfWhile     = EndOfElseCase;
            EndOfLoop      = EndOfElseCase;
            EndOfFor       = EndOfElseCase;
            initialize(ref EndOfElseCase,LEXEM.Semicolon,LEXEM.End);

            EndOfIfCondition = new BitArray(size);                     //      THEN, ELSIF, ELSE, END
            initialize(ref EndOfIfCondition,LEXEM.Then,LEXEM.Elsif,LEXEM.Else,LEXEM.End);

            EndOfThenCase = new BitArray(size);                        // ';', ELSE, ELSIF, END
            EndOfElsifCase = EndOfThenCase;
            initialize(ref EndOfThenCase,LEXEM.Semicolon,LEXEM.Else,LEXEM.Elsif,LEXEM.End);

            EndOfCaseBranch = new BitArray(size);                     // ';', ELSE or END, '|'
            initialize(ref EndOfCaseBranch,LEXEM.Semicolon,LEXEM.Else,LEXEM.End,LEXEM.Vert);

            EndOfRepeat = new BitArray(size);                         // ';', UNTIL
            initialize(ref EndOfRepeat,LEXEM.Semicolon,LEXEM.Until);

            //---------------------------------------------------------------------------------

            Colon_Array_LeftParenth_Id = new BitArray(size);

            Colon_Array_LeftParenth_Id[(int)LEXEM.Colon]       = true;
            Colon_Array_LeftParenth_Id[(int)LEXEM.Array]       = true;
            Colon_Array_LeftParenth_Id[(int)LEXEM.LeftParenth] = true;
            Colon_Array_LeftParenth_Id[(int)LEXEM.Id]          = true;

            Comma_Colon_RightParenth = new BitArray(size);

            Comma_Colon_RightParenth[(int)LEXEM.Comma]        = true;
            Comma_Colon_RightParenth[(int)LEXEM.Colon]        = true;
            Comma_Colon_RightParenth[(int)LEXEM.RightParenth] = true;

            Comma_Of_Semicolon = new BitArray(size);

            Comma_Of_Semicolon[(int)LEXEM.Comma]     = true;
            Comma_Of_Semicolon[(int)LEXEM.Of]        = true;
            Comma_Of_Semicolon[(int)LEXEM.Semicolon] = true;

            Comma_RightBrace_Id = new BitArray(size);

            Comma_RightBrace_Id[(int)LEXEM.Comma]      = true;
            Comma_RightBrace_Id[(int)LEXEM.RightBrace] = true;
            Comma_RightBrace_Id[(int)LEXEM.Id]         = true;

            Comma_RightParenth = new BitArray(size);

            Comma_RightParenth[(int)LEXEM.Comma]        = true;
            Comma_RightParenth[(int)LEXEM.RightParenth] = true;

            End_Dot = new BitArray(size);

            End_Dot[(int)LEXEM.End] = true;
            End_Dot[(int)LEXEM.Dot] = true;

            End_Id_Dot = new BitArray(size);

            End_Id_Dot[(int)LEXEM.End] = true;
            End_Id_Dot[(int)LEXEM.Id]  = true;
            End_Id_Dot[(int)LEXEM.Dot] = true;

            Equal_Array_LeftParenth_Id = new BitArray(size);

            Equal_Array_LeftParenth_Id[(int)LEXEM.Equal]       = true;
            Equal_Array_LeftParenth_Id[(int)LEXEM.Array]       = true;
            Equal_Array_LeftParenth_Id[(int)LEXEM.LeftParenth] = true;
            Equal_Array_LeftParenth_Id[(int)LEXEM.Id]          = true;

            Equal_Semicolon = new BitArray(size);

            Equal_Semicolon[(int)LEXEM.Semicolon] = true;
            Equal_Semicolon[(int)LEXEM.Equal]     = true;

            Import_Type_Const_Var_Procedure_End = new BitArray(size);

            Import_Type_Const_Var_Procedure_End[(int)LEXEM.Import   ] = true;
            Import_Type_Const_Var_Procedure_End[(int)LEXEM.Type     ] = true;
            Import_Type_Const_Var_Procedure_End[(int)LEXEM.Const    ] = true;
            Import_Type_Const_Var_Procedure_End[(int)LEXEM.Var      ] = true;
            Import_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure] = true;
            Import_Type_Const_Var_Procedure_End[(int)LEXEM.End      ] = true;

            LeftParenth_Id = new BitArray(size);

            LeftParenth_Id[(int)LEXEM.LeftBrace]   = true;
            LeftParenth_Id[(int)LEXEM.Id]          = true;

            RightBrace_Id = new BitArray(size);

            RightBrace_Id[(int)LEXEM.RightBrace] = true;
            RightBrace_Id[(int)LEXEM.Id]         = true;

            Semicolon_Colon_RightParenth = new BitArray(size);

            Semicolon_Colon_RightParenth[(int)LEXEM.Semicolon]    = true;
            Semicolon_Colon_RightParenth[(int)LEXEM.Colon]        = true;
            Semicolon_Colon_RightParenth[(int)LEXEM.RightParenth] = true;

            Semicolon_Comma = new BitArray(size);

            Semicolon_Comma[(int)LEXEM.Semicolon] = true;
            Semicolon_Comma[(int)LEXEM.Comma]     = true;

            Semicolon_Implements_Import_Type_Const_Var_Procedure_End = new BitArray(size);

            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Semicolon]  = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Implements] = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Import]     = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Type]       = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Const]      = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Var]        = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure]  = true;
            Semicolon_Implements_Import_Type_Const_Var_Procedure_End[(int)LEXEM.End]        = true;

            Semicolon_Import_Type_Const_Var_Procedure_End = new BitArray(size);

            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Semicolon]  = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Import]     = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Type]       = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Const]      = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Var]        = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure]  = true;
            Semicolon_Import_Type_Const_Var_Procedure_End[(int)LEXEM.End]        = true;

            Semicolon_Refines_Import_Type_Const_Var_Procedure_End = new BitArray(size);

            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Semicolon] = true;
//          Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Refines]   = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Import]    = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Type]      = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Const]     = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Var]       = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure] = true;
            Semicolon_Refines_Import_Type_Const_Var_Procedure_End[(int)LEXEM.End]       = true;

            Semicolon_Type_Const_Var_Procedure_End = new BitArray(size);

            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Semicolon]  = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Type]       = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Const]      = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Var]        = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure]  = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.End]        = true;

            Semicolon_Type_Const_Var_Procedure_Begin_End = new BitArray(size);

            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Semicolon]  = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Type]       = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Const]      = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Var]        = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Procedure]  = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Begin]      = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.End]        = true;

            Semicolon_Type_Const_Var_Procedure_Begin_End = new BitArray(size);

            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Semicolon] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Type     ] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Const    ] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Var      ] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Procedure] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.Begin    ] = true;
            Semicolon_Type_Const_Var_Procedure_Begin_End[(int)LEXEM.End      ] = true;

            Semicolon_Type_Const_Var_Procedure_End = new BitArray(size);

            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Semicolon] = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Type     ] = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Const    ] = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Var      ] = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.Procedure] = true;
            Semicolon_Type_Const_Var_Procedure_End[(int)LEXEM.End      ] = true;

            Type_Const_Var_Procedure_End = new BitArray(size);

            Type_Const_Var_Procedure_End[(int)LEXEM.Type     ] = true;
            Type_Const_Var_Procedure_End[(int)LEXEM.Const    ] = true;
            Type_Const_Var_Procedure_End[(int)LEXEM.Var      ] = true;
            Type_Const_Var_Procedure_End[(int)LEXEM.Procedure] = true;
            Type_Const_Var_Procedure_End[(int)LEXEM.End      ] = true;

            Dot_Module_Definition_Implementation_Object = new BitArray(size);

            Dot_Module_Definition_Implementation_Object[(int)LEXEM.Dot           ] = true;
            Dot_Module_Definition_Implementation_Object[(int)LEXEM.Module        ] = true;
            Dot_Module_Definition_Implementation_Object[(int)LEXEM.Definition    ] = true;
            Dot_Module_Definition_Implementation_Object[(int)LEXEM.Implementation] = true;
            Dot_Module_Definition_Implementation_Object[(int)LEXEM.Object        ] = true;

            Comma_Assignment = new BitArray(size);

            Comma_Assignment[(int)LEXEM.Comma ] = true;
            Comma_Assignment[(int)LEXEM.Assign] = true;

            Comma_Semicolon_End_Else = new BitArray(size);

            Comma_Semicolon_End_Else[(int)LEXEM.Comma    ] = true;
            Comma_Semicolon_End_Else[(int)LEXEM.Semicolon] = true;
            Comma_Semicolon_End_Else[(int)LEXEM.Else     ] = true;
            Comma_Semicolon_End_Else[(int)LEXEM.End      ] = true;
        }
/*
        public void skipUntil_Implements_Semicolon ( )  { scanner.skipUntil(Implements_Semicolon);  }
        public void skipUntil_Comma_Semicolon_Type_Const_Var_Procedure_Begin_End()
                                                               { scanner.skipUntil(Comma_Semicolon_Type_Const_Var_Procedure_Begin_End);  }
        public void skipUntil_Semicolon_Import_Type_Const_Var_Procedure_Begin_End ( )
                                                               { scanner.skipUntil(Semicolon_Import_Type_Const_Var_Procedure_Begin_End);  }
        public void skipUntil_Semicolon_Type_Const_Var_Procedure_Begin_End ( )
                                                               { scanner.skipUntil(Semicolon_Type_Const_Var_Procedure_Begin_End);  }
        public void skipUntil_Semicolon_Type_Const_Var_Procedure ( )
                                                               { scanner.skipUntil(Semicolon_Type_Const_Var_Procedure); }
        public void skipUntil_Semicolon_Type_Const_Var_Procedure_End ( )
                                                               { scanner.skipUntil(Semicolon_Type_Const_Var_Procedure_End); }
        public void skipUntil_Semicolon_Comma ( )       { scanner.skipUntil(Semicolon_Comma); }
        public void skipUntil_Refines_Semicolon ( )     { scanner.skipUntil(Refines_Semicolon); }
        public void skipUntil_RightBrace_Id ( )         { scanner.skipUntil(RightBrace_Id); }
        public void skipUntil_Equal_Semicolon ( )       { scanner.skipUntil(Equal_Semicolon); }
        public void skipUntil_Colon_Object_Record_Array_LeftParenth_Id ( )
                                                               { scanner.skipUntil(Colon_Object_Record_Array_LeftParenth_Id); }
        public void skipUntil_Comma_RightBrace_Id ( )   { scanner.skipUntil(Comma_RightBrace_Id); }
        public void skipUntil_Comma_RightParenth ( )    { scanner.skipUntil(Comma_RightParenth); }
        public void skipUntil_Dot ( )                   { scanner.skipUntil(LEXEM.Dot); }
        public void skipUntil_End_Dot ( )               { scanner.skipUntil(End_Dot); }
        public void skipUntil_Equal_Object_Record_Array_LeftParenth_Id ( )
                                                               { scanner.skipUntil(Equal_Object_Record_Array_LeftParenth_Id); }
//      public void skipUntil_LeftBrace_LeftParenth_Id(){ scanner.skipUntil(LeftBrace_LeftParenth_Id); }
        public void skipUntil_LeftParenth_Id()          { scanner.skipUntil(LeftParenth_Id); }
        public void skipUntil_Semicolon ( )             { scanner.skipUntil(LEXEM.Semicolon); }
        public void skipUntil_Comma_Colon_Id()          { scanner.skipUntil(Comma_Colon_Id); }

        public void skipUntil_Comma_Of_Semicolon()      { scanner.skipUntil(Comma_Of_Semicolon); }
        public void skipUntil_Semicolon_Colon_RightParenth()
                                                               { scanner.skipUntil(Semicolon_Colon_RightParenth); }
        public void skipUntil_Comma_Colon_RightParenth(){ scanner.skipUntil(Comma_Colon_RightParenth); }
        public void skipUntil_RightParenth()            { scanner.skipUntil(LEXEM.RightParenth); }
        */
	}
}
