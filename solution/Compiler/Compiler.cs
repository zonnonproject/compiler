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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;

namespace ETH.Zonnon
{
    [System.ComponentModel.DesignerCategory("code")]
    public class ZonnonCodeProvider: CodeDomProvider
    {
        public ZonnonCodeProvider ( ) { }

#if VS2005 || ORCAS
      [Obsolete]
      public override ICodeGenerator CreateGenerator ( ) { return new ZonnonCompiler(); }

      [Obsolete]
      public override ICodeCompiler CreateCompiler ( ) { return new ZonnonCompiler(); }

      [Obsolete]
      public override string FileExtension { get { return "znn"; } }

      [Obsolete]
      public override ICodeParser CreateParser() { throw new NotImplementedException(); }
                                                 //TODO: figure out what this is used for
#else
        public override ICodeGenerator CreateGenerator() { return new ZonnonCompiler(); }

        public override ICodeCompiler CreateCompiler() { return new ZonnonCompiler(); }

      public override string FileExtension { get { return "znn"; } }

      public override ICodeParser CreateParser() { throw new NotImplementedException(); }
                                                 //TODO: figure out what this is used for

#endif
        public override LanguageOptions LanguageOptions { get { return LanguageOptions.None; } }
    }

    ///////////////////////////////////////////////////////////////////////////////

    public class ZonnonCompilation : System.Compiler.Compilation
    {
        // global
        // ------
        // The tree with the IR image of the entire program.
        // Zonnon program is considered as a series of compilation units
        // which can be enclosed by implicit namespaces. The overall tree
        // is enclosed to the global "Zonnon" namespace.
        //
        // The 'global' value will be passed outside parser after completing
        // the compilation (see ParseCompilationUnit() method, Parser.cs file).
  
        public NAMESPACE_DECL  globalTree;
        public bool            wasMainModule;
  
        public ZonnonCompilation ( System.Compiler.Module targetModule,
                                   System.Compiler.CompilationUnitList compilationUnits,
                                   System.CodeDom.Compiler.CompilerParameters compilerParameters,
                                   System.Compiler.Scope globalScope) 
                      : base(targetModule,compilationUnits,compilerParameters,globalScope)
        { 
            init();
            EXTERNALS.clear();
            wasMainModule = false;
        }

        public void init ( )
        {           
           
            globalTree = new NAMESPACE_DECL(Identifier.For("Zonnon"));  // null);
            globalTree.enclosing = null;  // the only global namespace

            Zonnon.EXTERNALS.clear();

            if ( this.CompilationUnits == null || this.CompilationUnits.Length == 0 ) return;
            for ( int i=0, n=this.CompilationUnits.Length; i<n; i++ )
                this.CompilationUnits[i].Compilation = this;
        }
    }

//  public class ZonnonCompilationUnit : System.Compiler.CompilationUnitSnippet
//  {
//      public ZonnonCompilationUnit() : base() 
//      { 
//      }
//  }

    /////////////////////////////////////////////////////////////////////////////////////////

    public class ZonnonCompiler : System.Compiler.Compiler, System.CodeDom.Compiler.ICodeGenerator
    {
        public ZonnonCompiler ( )
        { 
        }

        //---------------------------------------------------------------------------------

        public override Compilation CreateCompilation(Module targetModule, CompilationUnitList compilationUnits, CompilerParameters compilerParameters, Scope globalScope)
        {
            return new ZonnonCompilation(targetModule, compilationUnits, compilerParameters, globalScope);
         // return base.CreateCompilation (targetModule,compilationUnits,compilerParameters,globalScope);
        }
  
//      public override CompilationUnitSnippet CreateCompilationUnitSnippet(string fileName, int lineNumber, DocumentText text, Compilation compilation)
//      {
//       // return new ZonnonCompilationUnit();
//          return base.CreateCompilationUnitSnippet (fileName, lineNumber, text, compilation);
//      }

        //---------------------------------------------------------------------------------

#region FrameworkOverrides

      public override CompilerResults CompileAssemblyFromIR ( Compilation compilation, ErrorNodeList errorNodes)
      {
          ((ZonnonCompilation)compilation).init();
          return base.CompileAssemblyFromIR(compilation,errorNodes);
      }

	  public override void CompileParseTree ( Compilation compilation, ErrorNodeList errorNodes )
      {
          if ( compilation == null ) { Debug.Assert(false, "wrong compilation"); return; }
          CONTEXT.useComputeMath = ((ZonnonCompilerParameters)compilation.CompilerParameters).UseComputeMath;
		  // Before the actual back-end compilation takes place,
		  // we perform the Zonnon-tree to CCI-tree conversion.
		  // Notice that we do that for the overall tree (i.e., for all sources)
		  // and store the resulting CCI tree to CompilationUnits[0],
          // but do not spread subtrees to corresponding CompilationUnits.
          ERROR.errCount = 0;
		  Parser.ConvertTree(compilation.CompilationUnits[0]);
          bool mainCompilationFailed = ERROR.errCount > 0;

      //    ERROR.EndOfMessages();

      //  Module symbolTable = compilation.TargetModule;
      //  compilation.GlobalScope = this.GetGlobalScope(symbolTable);

          ZonnonCompilerParameters options = compilation.CompilerParameters as ZonnonCompilerParameters;
          if ( options == null ) options = new ZonnonCompilerParameters();
          // Walk IR looking up names
#if DEBUG
          if ( options.Debug )
              System.Console.Write("Scoper is working...");
#endif
          ErrorHandler eh = new ErrorHandler(errorNodes);
          TrivialHashtable ambiguousTypes = new TrivialHashtable();
          TrivialHashtable referencedLabels = new TrivialHashtable();
          TrivialHashtable scopeFor = new TrivialHashtable();
          TypeSystem typeSystem = new TypeSystem(eh);

          Scoper scoper = new Scoper(scopeFor);
          scoper.VisitCompilation(compilation);
#if DEBUG
          if ( options.Debug )
          {
              System.Console.WriteLine("Done.");
              System.Console.Write("Looker is working... ");
          }
#endif
          Looker looker = new Looker(compilation.GlobalScope,eh,scopeFor,typeSystem,ambiguousTypes,referencedLabels);
          looker.VisitCompilation(compilation);

          // Walk IR inferring types and resolving overloads
          if (!mainCompilationFailed) // Resolve only if no errors
          {
              Resolver resolver = new Resolver(eh, typeSystem);
              resolver.VisitCompilation(compilation);

          // Walk IR checking for semantic errors and repairing it so that it the next walk will work
              Checker checker = new Checker(eh, typeSystem, ambiguousTypes, referencedLabels);
              checker.VisitCompilation(compilation);

          // Walk IR reducing it to nodes that have predefined mappings to MD+IL
              Normalizer normalizer = new Normalizer(typeSystem);
              normalizer.VisitCompilation(compilation);

              Analyzer analyzer = new Analyzer(typeSystem, compilation);
              analyzer.VisitCompilation(compilation);
          }
#if DEBUG
          if ( options.Debug )
          {
              System.Console.WriteLine("Done.");
          }
#endif
      }

      // The function overrides the one from System.Compiler.Copiler class.
      // As written in the comment to the function, it
      //
      // "parses all of the CompilationUnitSnippets in the given compilation, ignoring method bodies. 
      // Then resolves all type expressions.                                  ----------------------
      // The resulting types can be retrieved from the module in compilation.TargetModule. 
      // The base types, interfaces and member signatures will all be resolved and on an equal footing 
      // with imported, already compiled modules and assemblies."

      public override void ConstructSymbolTable ( Compilation compilation, ErrorNodeList errors )
      {
          if ( compilation == null ) { Debug.Assert(false); return; }

          ((ZonnonCompilation)compilation).init(); //////////////////////////////
          
          // RM: We're using out own tree for all the helpers
          /*RM
          Module symbolTable = compilation.TargetModule = 
                                 this.CreateModule(compilation.CompilerParameters, errors, compilation);

          TrivialHashtable scopeFor = new TrivialHashtable();
          ErrorHandler eh = new ErrorHandler(errors);
          TypeSystem typeSystem = new TypeSystem(eh);

          Scoper scoper = new Scoper(scopeFor);
          scoper.currentModule = symbolTable;

          Looker looker = new Looker(this.GetGlobalScope(symbolTable), eh, scopeFor, typeSystem, null, null);
          looker.currentAssembly = (looker.currentModule = symbolTable) as AssemblyNode;
          looker.ignoreMethodBodies = true;

          Scope globalScope = compilation.GlobalScope = this.GetGlobalScope(symbolTable);

          CompilationUnitList sources = compilation.CompilationUnits;
          if ( sources == null ) { Debug.Assert(false); return; }

          int n = sources.Length;
          for (int i = 0; i < n; i++)
          {
              CompilationUnitSnippet compilationUnitSnippet = sources[i] as CompilationUnitSnippet;
              if ( compilationUnitSnippet == null ) { Debug.Assert(false); continue; }

              compilationUnitSnippet.ChangedMethod = null;
              Document doc = compilationUnitSnippet.SourceContext.Document;
              if ( doc == null || doc.Text == null ) { Debug.Assert(false); continue; }

              IParserFactory factory = compilationUnitSnippet.ParserFactory;
              if ( factory == null ) { Debug.Assert(false); return; }

              IParser p = factory.CreateParser(doc.Name,doc.LineNumber,doc.Text,
                                               symbolTable,errors,compilation.CompilerParameters);

              if ( p is ResgenCompilerStub ) continue;
              if ( p == null ) { Debug.Assert(false); continue; }

              p.ParseCompilationUnit(compilationUnitSnippet);

              StringSourceText stringSourceText = doc.Text.TextProvider as StringSourceText;
              if ( stringSourceText != null && stringSourceText.IsSameAsFileContents )
                  doc.Text.TextProvider = new CollectibleSourceText(doc.Name, doc.Text.Length);
              else if ( doc.Text.TextProvider != null )
                  doc.Text.TextProvider.MakeCollectible();
          }

          try
          {

              Parser.ConvertTree(compilation.CompilationUnits[0]);
          }
          catch (Exception ex)
          {
              ERROR.InternalCompilerError("Compiler has experienced an exception processing the AST\n" +
                  ex.ToString() +"\n" + ex.StackTrace);
          }
          ((ZonnonCompilation)compilation).init(); //////////////////////////////

          // TODO: need work here. E.g. the loops are not needed.
          CompilationUnitList compilationUnits = new CompilationUnitList();
          for (int i = 0; i < n; i++)
          {
              CompilationUnit cUnit = sources[i];
              compilationUnits.Add(scoper.VisitCompilationUnit(cUnit));
          }
          for (int i = 0; i < n; i++)
          {
              CompilationUnit cUnit = compilationUnits[i];
              if ( cUnit == null ) continue;
              looker.VisitCompilationUnit(cUnit);
          }
           * */
      }

//      private static Guid ZonnonDebugGuid = new Guid("4161633B-8849-4736-B10D-50E1A0F7E534");
      private static Guid ZonnonDebugGuid = new Guid("8FFA4FA4-F168-43e2-99BE-E50F6D6D3389"); // NEW R

      public override Document CreateDocument ( string fileName, int lineNumber, DocumentText text )
      {
///       return new Document(fileName,lineNumber,text,SymDocumentType.Text,
///                           Compiler.ZonnonDebugGuid,SymLanguageVendor.Microsoft);
          return ZonnonCompiler.CreateZonnonDocument(fileName,lineNumber,text);
      }

      public static Document CreateZonnonDocument ( string fileName, int lineNumber, DocumentText text )
      {
          //TODO: allocate a GUID for ETH and supply this in the place of SymLanguageVendor.Microsoft
          return new Document(fileName,lineNumber,text,SymDocumentType.Text,ZonnonCompiler.ZonnonDebugGuid, SymLanguageVendor.Microsoft);
      }

      public override Document CreateDocument ( string fileName, int lineNumber, string text )
      {
          return new Document(fileName,lineNumber,text,SymDocumentType.Text,
                              ZonnonCompiler.ZonnonDebugGuid,SymLanguageVendor.Microsoft);
      }

      public override IParser CreateParser(string fileName, int lineNumber, DocumentText text, Module symbolTable, ErrorNodeList errors, CompilerParameters options)
      {
          //       Document document = this.CreateDocument(fileName, lineNumber, text);
          //       return new Parser(symbolTable, document, errorNodes);
          //
          Document document = this.CreateDocument(fileName, lineNumber, text);
          Parser parser = new Parser(document,errors,symbolTable,options as ZonnonCompilerParameters);
          //Parser.debug  = options is ZonnonCompilerParameters ? ((ZonnonCompilerParameters)options).Debug  : false;
          //Parser.debugT = options is ZonnonCompilerParameters ? ((ZonnonCompilerParameters)options).DebugT : false;
          return parser;
      }

      #endregion
      #region CodeDomCodeGenerator

///   private static string RuntimeDirectory = Path.GetDirectoryName(typeof(Object).Module.FullyQualifiedName) +
///                                            Path.DirectorySeparatorChar;

      bool ICodeGenerator.Supports ( GeneratorSupport support ) { return true; }
      void ICodeGenerator.GenerateCodeFromType ( CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o )
      { }
      void ICodeGenerator.GenerateCodeFromExpression ( CodeExpression e, TextWriter w, CodeGeneratorOptions o )
      { }
      void ICodeGenerator.GenerateCodeFromCompileUnit ( CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o )
      { }
      void ICodeGenerator.GenerateCodeFromNamespace ( CodeNamespace e, TextWriter w, CodeGeneratorOptions o )
      { }
      void ICodeGenerator.GenerateCodeFromStatement ( CodeStatement e, TextWriter w, CodeGeneratorOptions o )
      { }
      bool ICodeGenerator.IsValidIdentifier ( string value ) { return true; }
      void ICodeGenerator.ValidateIdentifier ( string value ) {  }
      string ICodeGenerator.CreateEscapedIdentifier ( string value ) { return value; }
      string ICodeGenerator.CreateValidIdentifier ( string value ) { return value; }
      string ICodeGenerator.GetTypeOutput ( CodeTypeReference type ) { return type.ToString(); }
      #endregion
  }
}
