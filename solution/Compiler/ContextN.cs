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

using System.Collections.Generic;

using System.Compiler;
using ETH.Zonnon.Compute;

namespace ETH.Zonnon
{
    public class CONTEXT
    {
        // Cannot create instances of class CONTEXT!
        private CONTEXT() { }
        public static bool AnalysisOnly;

        //-----------------------------------------------------------

        // Compiler's global values
        // This modules seems to be the best place for keeping them...

        // These globals are set by Parser's ctor.
        public  static ZonnonCompilerParameters options;
        public  static bool firstPass;
        public  static Module symbolTable;
        public  static bool partialParsing = false;
        public static bool useComputeMath;

        // These globals is set by ETH Zonnnon Integration ProjectManager CompileProject
        public static bool GenerateGPUCode { get; set; }

        // These globals are copied by ParseCompilationUnit
        // at the beginning of the compilation process
        public static Compilation compilation;
        public static NAMESPACE_DECL globalTree;
        
        public static MathGenerator globalMath;
        public static KernelRegistry _kernelRegistry;
        public static OperationRegistry _operationRegistry;

     // public  static NAMESPACE_DECL  globalTree;
     //
     // public void initZonnonTree ( )
     // {
     //     globalTree = new NAMESPACE_DECL(Identifier.For("Zonnon"));  // null);
     //     globalTree.enclosing = null;  // the only global namespace
     // }

        //-----------------------------------------------------------

        private static DECLARATION current_context;
        public  static DECLARATION current { get { return current_context; } }

        private static UNIT_DECL current_program_unit;
        public  static UNIT_DECL current_unit { get { return current_program_unit; }
                                                set { current_program_unit = value; } }

        private static ROUTINE_DECL current_routine_decl;
        public  static ROUTINE_DECL current_routine { get { return current_routine_decl; }
                                                      set { current_routine_decl = value; } }

        private static NAMESPACE_DECL current_namespace_decl;
        public  static NAMESPACE_DECL current_namespace { get { return current_namespace_decl; }
                                                          set { current_namespace_decl = value; } }

        //-----------------------------------------------------------

        private static int last_temp_no_internal = 0;
        public  static int last_temp_no { get { return last_temp_no_internal++; } }

        //-----------------------------------------------------------

		public static void init ( )
        {
            last_label_no = 0;
            last_temp_no_internal = 0;
            current_context = null;
            current_program_unit = null;
            current_routine_decl = null;

            string suffix = "";
            string output = "";
            if (options.Output != null)
                output = options.Output.Substring(0);
            else if (options.OutputAssembly != null)
                output = options.OutputAssembly.Substring(0);

            int slash_index = output.LastIndexOf('/');
            if (slash_index == -1) slash_index = output.LastIndexOf('\\');
            int dot_index = output.LastIndexOf('.');
            if (dot_index == -1) dot_index = output.Length;
            if (slash_index == -1)
            {
                suffix = output.Substring(slash_index + 1, dot_index);
            }
            else
            {
                suffix = output.Substring(slash_index + 1, dot_index - slash_index + 1);
            }
            suffix.Replace("-", "_minus_");

            globalMath = new MathGenerator(suffix);
            _kernelRegistry = new KernelRegistry();
            _operationRegistry = new OperationRegistry();
            // current_namespace_decl = null;

            globalTree = null; //-- do not do: it's required afterwards for reporting
        }

        public static void clean ( )
        {
            last_label_no = 0;
            last_temp_no_internal = 0;
            current_context = null;
            current_program_unit = null;
            current_routine_decl = null;
            // Keep global tree for reporting!!
            // CONTEXT.enter(TREE.global);
        }

        public static void enter ( DECLARATION block )
        {
            if ( current_context != null && current_context != block.enclosing ) // System error
            {
                ERROR.SystemErrorIn("enter","attempt to enter to an unrelated scope");
                return;
            }
            current_context = block;

            if ( block is UNIT_DECL && !(block is NAMESPACE_DECL) )
            {
                current_program_unit = (UNIT_DECL)block;
                current_routine_decl = null;
            }
            if ( block is ROUTINE_DECL )
                current_routine_decl = (ROUTINE_DECL)block;
            else if ( block is NAMESPACE_DECL )
                current_namespace_decl = (NAMESPACE_DECL)block;
        }

        //-----------------------------------------------------------

        public static void exit ( )
        {
            if ( current_context == null )
            {
                ERROR.SystemErrorIn("exit","attempt to exit from empty context");
                return;
            }
            current_context = (DECLARATION)current_context.enclosing;

            if ( current_context is NAMESPACE_DECL )
            {
                current_namespace_decl = (NAMESPACE_DECL)current_context;
                current_program_unit = null;
            }
            if ( current_context is UNIT_DECL && !(current_context is NAMESPACE_DECL) )
                current_program_unit = (UNIT_DECL)current_context;
            else
                current_program_unit = null;

            // For the case if we exit from a nested routine...
            if ( current_context is ROUTINE_DECL )
                current_routine = (ROUTINE_DECL)current_context;
            else
                current_routine = null;
        }

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

        // A similar mechanism for ordinary blocks (for creating Barriers)

//        private static int last_barrier_no_internal = 0;
//        public  static int last_barrier_no { get { return ++last_barrier_no_internal; } }

        //private static BLOCK current_block_decl;
        //public  static BLOCK current_block { get { return current_block_decl; }
        //                                     set { current_block_decl = value; } }

        //public static void enterBlock ( BLOCK block ) { current_block = block; }
        //public static void exitBlock  ( )             { current_block = current_block.getEnclosingBlock(); }

//        public static int current_barrier ( )
//        {
//            if ( current_block != null )  return current_block.barrierNo;
//            if ( current_routine != null) return current_routine.barrierNo;
//            return current_unit.barrierNo;
//        }

//        public static void zeroBarrier ( UNIT_DECL unit ) { unit.barrierNo = 0; }
        //-------------- labels -------------

        static int last_label_no = 0;
        public static Identifier GetNextLabel(){ return new Identifier("label"+(last_label_no++));}

        //-----------------------------------------------------------
#if DEBUG
        public static void report ( )
        {
            System.Console.WriteLine("CURRENT CONTEXT");

            NODE node = current_context;
            while ( node != null )
            {
                node.report(0);
                node = node.enclosing;
            }

            System.Console.WriteLine("END CURRENT CONTEXT");
        }
#endif

    } // class CONTEXT

    ///////////////////////////////////////////////////////////////////////////

    public class EXTERNALS
    {
        // Cannot create instances of class EXTERNALS!
        private EXTERNALS() { }

        // Common list of namespaces taken from all referenced assemblies.
        private static NamespaceList allNamespaces = null;


		private static Dictionary<string, TypeNodeList> nsCached = null;


      //  static EXTERNALS ( )
        public static void clear()
        {
            allNamespaces = null;
        }
        public static void init ( )
        {
         // if ( allNamespaces != null ) return;
            allNamespaces = new NamespaceList();

			nsCached = new Dictionary<string, TypeNodeList>();

            // Scan all assemblies referenced in parameters
            // and take all namespaces from them.
            // Put all these namespaces into the common list.
            System.Collections.Specialized.StringCollection assemblies = CONTEXT.options.ReferencedAssemblies;
            foreach ( string assName in assemblies )
            {
                AssemblyNode assembly = AssemblyNode.GetAssembly(assName,true,false,true);
                if ( assembly == null && !CONTEXT.AnalysisOnly)
                {
                    ERROR.UnresolvedReference(assName);
                    continue;
                }
                NamespaceList nss = assembly.GetNamespaceList();
                for ( int i=0, n=nss.Length; i<n; i++ )
                    allNamespaces.Add(nss[i]);
            }
        }

        //----------------------------------------------------------------------

        private static TypeNodeList findAll ( string name )
        {            
            if (allNamespaces == null) init();
            TypeNodeList nsl = new TypeNodeList();
            nsCached.Add(name, nsl);                        
            for ( int i=0, n=allNamespaces.Length; i<n; i++ )
                if (allNamespaces[i].Name.Name == name) 
                {
                    Namespace ns = allNamespaces[i];  
                    TypeNodeList tnls = ns.Types;
                    for (int k = 0; k < tnls.Length; k++)
                        nsl.Add(tnls[k]);
                }
            return nsl;
        }


        public static bool testNamespace(string name)
        {            
            if (allNamespaces == null) init();

            for (int i = 0, n = allNamespaces.Length; i < n; i++)
                if (
                    (allNamespaces[i].Name.Name.Length > name.Length && allNamespaces[i].Name.Name.Substring(0, name.Length+1) == name + ".")
                    ||
                    (allNamespaces[i].Name.Name.Length == name.Length && allNamespaces[i].Name.Name.Substring(0, name.Length) == name)
                    )
                {
                    return true;
                }
            return false;
        }

        public static NamespaceList getGlobalNamespaces()
        {
            if (allNamespaces == null) init();
            return allNamespaces;
        }

        public static TypeNode findType(string nsName, Identifier typeName)
        {
            if (typeName == null) return null;
            TypeNodeList ns;
            if (allNamespaces == null || nsCached== null) init();
            if (!nsCached.TryGetValue(nsName, out ns)) { ns = findAll(nsName); }

            TypeNodeList types = nsCached[nsName];

            for (int i = 0, n = types.Length; i < n; i++)
                if (types[i].Name.Name == typeName.Name)
                    return types[i];
            return null;
        }

        public static void report ( )
        {
            if (allNamespaces == null) init();
            for ( int i=0, n=allNamespaces.Length; i<n; i++ )
            {
                Namespace ns = allNamespaces[i];
                System.Console.WriteLine("{0}",ns.Name.Name);
            }
        }
    }

}  // ETH.Zonnon.Compiler

