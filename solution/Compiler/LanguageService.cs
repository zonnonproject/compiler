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
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ETH.Zonnon
{
#if ROTOR
    //Define dummy class which is not dependent on Visual Studio
    public class ZonnonGlyphProvider
    {
        public virtual int GetGlyph(System.Compiler.Member member)
        {
            return 0;
        }

        public virtual int GetGlyph(NODE member)
        {
            return 0;
        }
    }
#endif

    public sealed class LanguageService : System.Compiler.LanguageService
    {
        private Scanner scanner;
        private TrivialHashtable scopeFor;
        public ZonnonGlyphProvider glyphProvider;        

        public LanguageService(ZonnonGlyphProvider glyphProvider)
            : base(new ErrorHandler(new ErrorNodeList()))
        {
            this.scanner = new Scanner();
            this.glyphProvider = glyphProvider;
        }

        public override void GetMethodFormat(out string typeStart, out string typeEnd, out bool typePrefixed)
        {
            typeStart = "";
            typeEnd = "";
            typePrefixed = false;
        }

        public override Compilation GetDummyCompilationFor(string fileName)
        {
            string fContents = null;
            if (File.Exists(fileName))
            {
                StreamReader sr = new StreamReader(fileName);
                fContents = sr.ReadToEnd(); sr.Close();
            }
            Compilation compilation = new Compilation();
            // compilation.IsDummy = true;
            compilation.CompilerParameters = this.GetDummyCompilerParameters();
            compilation.TargetModule = new Module();
            DocumentText docText = new DocumentText(new StringSourceText(fContents, true));
            SourceContext sctx = new SourceContext(ZonnonCompiler.CreateZonnonDocument(fileName, 0, docText));
            compilation.CompilationUnits = new CompilationUnitList(new CompilationUnitSnippet(new Identifier(fileName), new ParserFactory(), sctx));
            compilation.CompilationUnits[0].Compilation = compilation;
            return compilation;
        }

        public override System.CodeDom.Compiler.CompilerParameters GetDummyCompilerParameters()
        {
            return new ZonnonCompilerParameters();
        }
        public override System.Compiler.Scanner GetScanner()
        {
            return this.scanner;
        }
        public override MemberList GetNestedNamespacesAndTypes(Identifier name, Scope scope, AssemblyReferenceList assembliesToSearch)
        {
            MemberList result = new MemberList();
            ErrorHandler errorHandler = new ErrorHandler(new ErrorNodeList(0));
            Looker looker = new Looker(null, errorHandler, null, null, null, null);
            looker.currentModule = this.currentSymbolTable;
            return looker.GetNestedNamespacesAndTypes(name, scope, assembliesToSearch);
        }
        public override MemberList GetTypesNamespacesAndPrefixes(Scope scope, bool constructorMustBeVisible)
        {
            MemberList result = new MemberList();
            while (scope != null && !(scope is TypeScope)) scope = scope.OuterScope;
            if (scope == null) return result;
            TypeNode currentType = ((TypeScope)scope).Type;
            if (currentType == null || currentType.DeclaringModule == null) return result;
            ErrorHandler errorHandler = new ErrorHandler(new ErrorNodeList(0));
            Looker looker = new Looker(null, errorHandler, null, null, null, null);
            looker.currentType = currentType;
            looker.currentModule = currentType.DeclaringModule;
            return looker.GetVisibleTypesNamespacesAndPrefixes(scope, constructorMustBeVisible);
        }

        // ParseAndAnalyzeCompilationUnit
        // ------------------------------
        // The function overrides the abstract one from System.Compiler.LanguageService class.
        // As written in the comment to this class, it 
        // "tracks the symbol table (Module) associated with the current editor window".
        // Another brief explanation is from a companion guide:
        // "Called from the background thread to provide syntax checking".
        //
        // In the base class, this function is called from ParseSource() function like as follows:
        //
        // switch (reason) {
        //      case ParseReason.CollapsibleRegions:
        //      case ParseReason.MatchBraces:
        //      case ParseReason.HighlightBraces:
        //      case ParseReason.MemberSelect:
        //      case ParseReason.MethodTip: 
        //      case ParseReason.QuickInfo:
        //      case ParseReason.Autos:
        //              return this.ParsePartialCompilationUnit(fname, text, line, col, asink, reason);
        //!!!   case ParseReason.Check: {
        //              ErrorNodeList errors = new ErrorNodeList();
        //!!!           this.ParseAndAnalyzeCompilationUnit(fname, text, errors, compilation, asink);
        //              ... }
        //
        // So, as I understand, this function is launched every time when the
        // file in the current editor window has changed.
        //
        public override void ParseAndAnalyzeCompilationUnit(string fname, string text, int line, int col, ErrorNodeList errors,
                                                              Compilation compilation, AuthoringSink sink)
        {
            lock (typeof(ETH.Zonnon.ZonnonCompiler))
            {
                if (fname == null || text == null || errors == null || compilation == null)
                { Debug.Assert(false); return; }

                ((ZonnonCompilation)compilation).init();  //////////////////////////

                CompilationUnitList compilationUnitSnippets = compilation.CompilationUnits;
                if (compilationUnitSnippets == null) { Debug.Assert(false); return; }

                // Fix up the CompilationUnitSnippet corresponding to fname with the new source text
                CompilationUnitSnippet cuSnippet = this.GetCompilationUnitSnippet(compilation, fname);
                if (cuSnippet == null) { Debug.Assert(false); return; }

                ZonnonCompiler compiler = new ZonnonCompiler();
                cuSnippet.SourceContext.Document = compiler.CreateDocument(fname, 1, new DocumentText(text));
                cuSnippet.SourceContext.EndPos = text.Length;

                // Parse all of the compilation unit snippets
                Module symbolTable = compiler.CreateModule(compilation.CompilerParameters, errors);
                int n = compilationUnitSnippets.Length;
                for (int i = 0; i < n; i++)
                {
                    CompilationUnitSnippet compilationUnitSnippet = compilationUnitSnippets[i] as CompilationUnitSnippet;
                    if (compilationUnitSnippet == null) { Debug.Assert(false); continue; }

                    Document doc = compilationUnitSnippet.SourceContext.Document;
                    if (doc == null || doc.Text == null) { Debug.Assert(false); continue; }

                    IParserFactory factory = compilationUnitSnippet.ParserFactory;
                    if (factory == null) continue;
                    compilationUnitSnippet.Nodes = null;
                    compilationUnitSnippet.PreprocessorDefinedSymbols = null;

                    // We create a new parser for every compilation unit.
                    // For all units except the current one (i.e., the one in the current
                    // editor window) we provide the new error list which will be discarded afterwards.
                    // For the current unit the common error list is used, and it will be kept using
                    // after (see the rest of the function).
                    IParser p = factory.CreateParser(doc.Name, doc.LineNumber, doc.Text,
                                                     symbolTable,
                                                     compilationUnitSnippet == cuSnippet
                                                            ? errors
                                                            : new ErrorNodeList(),
                                                     compilation.CompilerParameters);

                    if (p == null) { Debug.Assert(false); continue; }
                    if (p is ResgenCompilerStub) continue;

                    p.ParseCompilationUnit(compilationUnitSnippet);

                    StringSourceText stringSourceText = doc.Text.TextProvider as StringSourceText;
                    if (stringSourceText != null && stringSourceText.IsSameAsFileContents)
                        doc.Text.TextProvider = new CollectibleSourceText(doc.Name, doc.Text.Length);
                }
                // Construct symbol table for entire project
                Scope globalScope = compiler.GetGlobalScope(symbolTable);
                ErrorHandler errorHandler = new ErrorHandler(errors);
                TrivialHashtable ambiguousTypes = new TrivialHashtable();
                TrivialHashtable referencedLabels = new TrivialHashtable();
                TrivialHashtable scopeFor = this.scopeFor = new TrivialHashtable();
                TypeSystem typeSystem = new TypeSystem(errorHandler);

                Scoper scoper = new Scoper(scopeFor);
                scoper.currentModule = symbolTable;

                Looker symLooker = new Looker(globalScope, new ErrorHandler(new ErrorNodeList(0)), scopeFor, typeSystem, ambiguousTypes, referencedLabels);
                symLooker.currentAssembly = (symLooker.currentModule = symbolTable) as AssemblyNode;

                Looker looker = new Looker(globalScope, errorHandler, scopeFor, typeSystem, ambiguousTypes, referencedLabels);
                looker.currentAssembly = (looker.currentModule = symbolTable) as AssemblyNode;
                looker.identifierInfos = this.identifierInfos = new NodeList();
                looker.identifierPositions = this.identifierPositions = new Int32List();
                looker.identifierLengths = this.identifierLengths = new Int32List();
                looker.identifierScopes = this.identifierScopes = new ScopeList();

                for (int i = 0; i < n; i++)
                {
                    CompilationUnit cUnit = compilationUnitSnippets[i];
                    scoper.VisitCompilationUnit(cUnit);
                }
                for (int i = 0; i < n; i++)
                {
                    CompilationUnit cUnit = compilationUnitSnippets[i];
                    if (cUnit == cuSnippet)
                        // Uses real error message list and populate the identifier info lists
                        looker.VisitCompilationUnit(cUnit);
                    else
                        // Errors are discarded
                        symLooker.VisitCompilationUnit(cUnit);
                }

                // Now analyze the given file for errors
                Resolver resolver = new Resolver(errorHandler, typeSystem);
                resolver.currentAssembly = (resolver.currentModule = symbolTable) as AssemblyNode;
                resolver.VisitCompilationUnit(cuSnippet);

                Partitioner partitioner = new Partitioner();
                partitioner.VisitCompilationUnit(cuSnippet);

                Checker checker = new Checker(errorHandler, typeSystem, ambiguousTypes, referencedLabels);
                checker.currentAssembly = (checker.currentModule = symbolTable) as AssemblyNode;
                checker.VisitCompilationUnit(cuSnippet);

                compilation.TargetModule = this.currentSymbolTable = symbolTable;
            }
        }

        // Where and when this function is actually used??
        //
        public override CompilationUnit ParseCompilationUnit(string fname,
                                                               string source,
                                                               ErrorNodeList errors,
                                                               Compilation compilation,
                                                               AuthoringSink sink)
        {
            lock (typeof(ETH.Zonnon.ZonnonCompiler))
            {
                // I hope that 'compilation' passed to this function was created
                // using CreateCompilation method (see ETH.Compiler.Compiler in Compiler.cs),
                // i.e., actually, 'compilation' is of type ZonnonCompilation...
                ZonnonCompilation zc = compilation as ZonnonCompilation;
                if (zc == null)
                {
                    // Debug.Fail(compilation.ToString());
                    // Debug.Assert(false); 
                    // It is Ok. Just opened file is not in the project 
                    return null;
                }

                // This is reallocated by the caller for every call
                Parser p = new Parser(compilation.TargetModule,
                                      compilation.CompilerParameters as ZonnonCompilerParameters);

                CompilationUnit cu = p.ParseCompilationUnit(source, fname, compilation,
                                                            compilation.CompilerParameters,
                                                            errors, sink);
                if (cu != null) cu.Compilation = compilation;
                return cu;
            }
        }

        public override System.Compiler.AuthoringScope ParsePartialCompilationUnit(string fname, string text, int line, int col, AuthoringSink asink, ParseReason reason)
        {
            lock (typeof(ETH.Zonnon.ZonnonCompiler))
            {
                Compilation compilation = this.GetCompilationFor(fname);
                if (line >= 0 && (reason == ParseReason.MemberSelect || reason == ParseReason.CompleteWord))
                    text = this.Truncate(text, line, col);
                Module savedSymbolTable = this.currentSymbolTable;
                compilation.TargetModule = this.currentSymbolTable = new Module();
                if (savedSymbolTable != null) //TODO: Find out why this can be null
                    this.currentSymbolTable.AssemblyReferences = savedSymbolTable.AssemblyReferences;
                CONTEXT.partialParsing = true;
                CompilationUnit partialCompilationUnit = null;
                try
                {
                    partialCompilationUnit = this.ParseCompilationUnit(fname, text, new ErrorNodeList(), compilation, asink);
                }
                finally
                {
                    CONTEXT.partialParsing = false;
                }
                compilation.TargetModule = this.currentSymbolTable = savedSymbolTable;
                if (reason != ParseReason.HighlightBraces && reason != ParseReason.MatchBraces)
                {
                    MemberFinder memberFinder = this.GetMemberFinder(line + 1, col + 1);
                    memberFinder.Visit(partialCompilationUnit);
                    Member unresolvedMember = memberFinder.Member;
                    memberFinder.Member = null;
                    CompilationUnit cu = this.GetCompilationUnitSnippet(compilation, fname);
                    if (cu != null)
                    {
                        if (unresolvedMember == null)
                        {
                            //Dealing with a construct that is not part of a type definition, such as a using statement
                            this.Resolve(partialCompilationUnit);
                        }
                        else
                        {
                            memberFinder.Visit(cu);
                            if (memberFinder.Member != null)
                                this.Resolve(unresolvedMember, memberFinder.Member);
                            else
                                this.Resolve(partialCompilationUnit); //Symbol table is out of date
                        }
                    }
                }
                return null;
            }
        }
        
        public override void Resolve(CompilationUnit partialCompilationUnit)
        {
            if (partialCompilationUnit == null)
            {
                // Just the file not in a project
                /* Debug.Assert(false); */
                return;
            }
            TrivialHashtable scopeFor = new TrivialHashtable();
            Scoper scoper = new Scoper(scopeFor);
            scoper.currentModule = this.currentSymbolTable;
            scoper.VisitCompilationUnit(partialCompilationUnit);

            ErrorHandler errorHandler = new ErrorHandler(new ErrorNodeList(0));
            TrivialHashtable ambiguousTypes = new TrivialHashtable();
            TrivialHashtable referencedLabels = new TrivialHashtable();
            TypeSystem typeSystem = new TypeSystem(errorHandler);
            Looker looker = new Looker(null, errorHandler, scopeFor, typeSystem, ambiguousTypes, referencedLabels);
            looker.currentAssembly = (looker.currentModule = this.currentSymbolTable) as AssemblyNode;
            looker.identifierInfos = this.identifierInfos = new NodeList();
            looker.identifierPositions = this.identifierPositions = new Int32List();
            looker.identifierLengths = this.identifierLengths = new Int32List();
            looker.identifierScopes = this.identifierScopes = new ScopeList();
            looker.VisitCompilationUnit(partialCompilationUnit);
            //Walk IR inferring types and resolving overloads
            Resolver resolver = new Resolver(errorHandler, new TypeSystem(errorHandler));
            resolver.currentAssembly = (resolver.currentModule = this.currentSymbolTable) as AssemblyNode;
            resolver.Visit(partialCompilationUnit);
        }
        public override void Resolve(Member unresolvedMember, Member resolvedMember)
        {
            if (unresolvedMember == null || resolvedMember == null) return;
            ErrorHandler errorHandler = new ErrorHandler(new ErrorNodeList(0));
            TrivialHashtable ambiguousTypes = new TrivialHashtable();
            TrivialHashtable referencedLabels = new TrivialHashtable();
            TypeSystem typeSystem = new TypeSystem(errorHandler);
            Looker looker = new Looker(null, errorHandler, this.scopeFor, typeSystem, ambiguousTypes, referencedLabels);
            looker.currentAssembly = (looker.currentModule = this.currentSymbolTable) as AssemblyNode;
            TypeNode currentType = resolvedMember.DeclaringType;
            if (resolvedMember is TypeNode && unresolvedMember.DeclaringType != null &&
              ((TypeNode)resolvedMember).FullName == unresolvedMember.DeclaringType.FullName)
            {
                unresolvedMember.DeclaringType = (TypeNode)resolvedMember;
                currentType = (TypeNode)resolvedMember;
                looker.scope = this.scopeFor[resolvedMember.UniqueKey] as Scope;
            }
            else if (resolvedMember.DeclaringType != null)
            {
                unresolvedMember.DeclaringType = resolvedMember.DeclaringType;
                looker.scope = this.scopeFor[resolvedMember.DeclaringType.UniqueKey] as Scope;
            }
            else if (resolvedMember.DeclaringNamespace != null)
            {
                unresolvedMember.DeclaringNamespace = resolvedMember.DeclaringNamespace;
                looker.scope = this.scopeFor[resolvedMember.DeclaringNamespace.UniqueKey] as Scope;
            }
            if (looker.scope == null) return;
            looker.currentType = currentType;
            looker.identifierInfos = this.identifierInfos = new NodeList();
            looker.identifierPositions = this.identifierPositions = new Int32List();
            looker.identifierLengths = this.identifierLengths = new Int32List();
            looker.identifierScopes = this.identifierScopes = new ScopeList();
            looker.Visit(unresolvedMember);
            //Walk IR inferring types and resolving overloads
            Resolver resolver = new Resolver(errorHandler, new TypeSystem(errorHandler));
            resolver.currentAssembly = (resolver.currentModule = this.currentSymbolTable) as AssemblyNode;
            resolver.currentType = currentType;
            resolver.Visit(unresolvedMember);
        }

        public NODE SearchZonnonAstForZonnonNodeAtPosition(string file_name, int line, int col)
        {
            NODE node = null;
     
            // We go only through modules and units, sot it should be fast without hash for now
            if (CONTEXT.globalTree != null)
            {
                node = CONTEXT.globalTree.findScopeAtContext(line+1, col+1, file_name);
            }
            return node;
        }

    }
#if !ROTOR
    public class AuthoringScope : Microsoft.VisualStudio.Package.AuthoringScope
    {
        public ZonnonGlyphProvider glyphProvider;
        public LanguageService languageService;

        public AuthoringScope(LanguageService languageService, ZonnonGlyphProvider glyphProvider) 
        {
            this.glyphProvider = glyphProvider;
            this.languageService = languageService;
        }

        public override string GetDataTipText(int line, int col, out Microsoft.VisualStudio.TextManager.Interop.TextSpan span)
        {
            //SourceContext ctx;
            //string text = "";
            // Remove the code below when uncomment
                span = new Microsoft.VisualStudio.TextManager.Interop.TextSpan();
                span.iStartLine = line;
                span.iStartIndex = col > 0 ? col - 1 : col;
                span.iEndIndex = col + 2;
                span.iEndLine = line;
                return "";
            //---------
            //TODO: Modify search to make it useful
            // We need to find corrsponding node for particular position in a particular document
            // It would be easier/better if we changed compiler to support a forest of AST trees
            /*
            NODE node = languageService.SearchZonnonAstForZonnonNodeAtPosition(line, col);
            if (node is EXPRESSION)
            {
                if (((EXPRESSION)node).type != null && !(((EXPRESSION)node).type is UNKNOWN_TYPE))
                {
                    string type = "";
                    string value = "";
                    string signature = "";
                    if (node is CALL) type = "(call) ";                                            
                    else if (node is INSTANCE)
                    {
                        NODE entity = ((INSTANCE)node).entity;
                        if (entity is PARAMETER_DECL) type = "(param) ";
                        else if (entity is LOCAL_DECL) type = "(local) ";
                        else if (entity is FIELD_DECL) type = "(field) ";
                        else if (entity is PROCEDURE_DECL)
                        {
                            type = "(procedure) ";
                            signature = "(";
                            VARIABLE_DECL_LIST par = ((PROCEDURE_DECL)entity).parameters;
                            for (int j = 0; j < par.Length; j++)
                            {
                                if (j != 0) signature += ", ";
                                signature += par[j].type.Name;                                
                            }
                            signature += ") ";
                        }
                        else if (entity is DEFINITION_DECL) type = "(definition) ";
                        else if (entity is OBJECT_DECL) type = "(object) ";
                        else if (entity is CONSTANT_DECL) { type = "(const) "; value = " = " + ((CONSTANT_DECL)entity).initializer.calculate().ToString(); }
                    }

                    text = type + node.sourceContext.SourceText+signature+ value + " : " + ((EXPRESSION)node).type.ToString();
                    ctx = node.sourceContext;
                }
                else
                {
                    ctx = new SourceContext();
                }
            }
            else
            {
                ctx = new SourceContext();
            }
            span = new Microsoft.VisualStudio.TextManager.Interop.TextSpan();
            if (ctx.Document != null && ctx.StartLine == line + 1)
            {
                // This gives the span of the symbol we are providing information about
                // so that the data tip text remains open until the mouse exits the bounds
                // of this span.
                span.iStartIndex = ctx.StartColumn - 1;
                span.iStartLine = ctx.StartLine - 1;
                span.iEndIndex = ctx.EndColumn - 1;
                span.iEndLine = ctx.EndLine - 1;
            }
            else
            {
                // The authoring scope failed to provide us with a valid source context that spans the current cursor position. 
                // Make up a span.
                span.iStartLine = line;
                span.iStartIndex = col > 0 ? col - 1 : col;
                span.iEndIndex = col + 2;
                span.iEndLine = line;
            }
            return text;
             */
        }

        public string GetTextFor(DECLARATION node)
        {
            if (node is PROCEDURE_DECL || node is OPERATOR_DECL || node is ACTIVITY_DECL)
            {
                return node.ToString();
            }
            return node.Name;                
        }

        private int uniq = 0;
        // As sorted list must have unique keys 
        // we add tail. We could check presense every time too
        public string Unique()
        {
            if(++uniq == Int32.MaxValue) uniq = 0;
            return uniq.ToString();
        }


        public string GetFileName(Microsoft.VisualStudio.TextManager.Interop.IVsTextView view)
        {
            if (view == null) return null;
            string fname = null;
            try
            {
                uint formatIndex;
                Microsoft.VisualStudio.TextManager.Interop.IVsTextLines pBuffer;
                view.GetBuffer(out pBuffer);
                Microsoft.VisualStudio.Shell.Interop.IPersistFileFormat pff = (Microsoft.VisualStudio.Shell.Interop.IPersistFileFormat)pBuffer;
                pff.GetCurFile(out fname, out formatIndex);
                pff = null;
                pBuffer = null;
            }
            catch { }
            return fname;
        }

        enum MyScope { None, Local, Enumeration, Interfaces, ExternalType, ExternalNamespace };
        /// <summary>
        /// Called for completions.
        /// </summary>
        public override Microsoft.VisualStudio.Package.Declarations GetDeclarations(Microsoft.VisualStudio.TextManager.Interop.IVsTextView view, int line, int col, Microsoft.VisualStudio.Package.TokenInfo info)
        {
            string file_name = GetFileName(view);

            System.Collections.Generic.SortedList<string, DECLARATION> decls = null;
            Declarations decl = null;
            try
            {
                NODE node = languageService.SearchZonnonAstForZonnonNodeAtPosition(file_name, line, col - 1);

                string s;
                view.GetTextStream(line, 0, line, col, out s);
                int leftsep = s.LastIndexOfAny(",\t\n\r (){}*+\\/&^%!=-;".ToCharArray());
                int rightsep = s.LastIndexOf('.');
                string identifierText;
                string partialText;
                if (rightsep >= 0)
                {
                    if (rightsep - leftsep - 1 < 0) identifierText = "";
                    else identifierText = s.Substring(leftsep + 1, rightsep - leftsep - 1);
                    partialText = s.Substring(rightsep + 1);
                }
                else
                {
                    identifierText = "";
                    partialText = s.Substring(leftsep + 1);
                }
                if (node != null)
                {
                    DECLARATION context = node.getEnclosingDeclaration();
                    UNIT_DECL_LIST interfaces = null;
                    ENUMERATOR_DECL_LIST enumerators = null;
                    TypeNode exnd = null;
                    NamespaceList exns = null;

                    MyScope scope = MyScope.Local;
                    if (identifierText != "") // Find the right scope
                    {
                        //Process qualified name
                        int dotPos = identifierText.IndexOf('.');
                        do
                        {
                            if (dotPos < 0) dotPos = identifierText.Length;
                            string name = identifierText.Substring(0, dotPos);
                            if (identifierText.Length == dotPos) identifierText = "";
                            else identifierText = identifierText.Substring(dotPos + 1);
                            if (scope == MyScope.Local)
                            {
                                NODE res = context.find(Identifier.For(name));
                                if (res != null)
                                {
                                    NODE nd = res.resolve();
                                    if (nd is UNKNOWN_DECL)
                                    {
                                        // Try external
                                        exnd = EXTERNALS.findType(nd.enclosing.Name, nd.name);
                                        if (exnd == null)
                                        {
                                            bool ok = false;
                                            //Namespace?
                                            NamespaceList list = EXTERNALS.getGlobalNamespaces();
                                            for (int i = 0; i < list.Length; i++)
                                            {
                                                Namespace ns = list[i];
                                                if (ns.Name.Name == nd.Name)
                                                {
                                                    if (exns == null) exns = new NamespaceList();
                                                    exns.Add(ns);
                                                    ok = true;
                                                }
                                            }
                                            // Nothing found
                                            if (ok)
                                            {
                                                scope = MyScope.ExternalNamespace;
                                            }
                                            else
                                            {
                                                scope = MyScope.None;
                                                break;
                                            }
                                        }
                                        else
                                        { //Type found
                                            scope = MyScope.ExternalType;
                                        }

                                    }
                                    else
                                        if (nd is IMPORT_DECL)
                                        {
                                            context = ((IMPORT_DECL)nd).imported_unit;
                                        }
                                        else
                                            if (nd is FIELD_DECL || nd is LOCAL_DECL || nd is PARAMETER_DECL)
                                            {
                                                if (nd.type is OBJECT_TYPE)
                                                {
                                                    context = ((OBJECT_TYPE)nd.type).objectUnit;
                                                    if (context == null)
                                                    {
                                                        scope = MyScope.None;
                                                        break;
                                                    }
                                                }
                                                else if (nd.type is INTERFACE_TYPE)
                                                {
                                                    interfaces = ((INTERFACE_TYPE)nd.type).interfaces;
                                                    scope = MyScope.Interfaces;
                                                }
                                                else if (nd.type is ENUM_TYPE)
                                                {
                                                    scope = MyScope.Enumeration;
                                                    enumerators = ((ENUM_TYPE)nd.type).enumerators;
                                                }
                                                else
                                                {
                                                    scope = MyScope.None;
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                // We can't do anything
                                                scope = MyScope.None;
                                                break;
                                            }

                                }
                                else
                                {
                                    scope = MyScope.None;
                                }
                            }
                            else if (scope == MyScope.ExternalType)
                            {
                                // It can be nested type
                                scope = MyScope.None;
                                exnd = exnd.GetNestedType(Identifier.For(name));
                                if (exnd != null) scope = MyScope.ExternalType;
                                //TODO: Add for memebers etc.                            
                            }
                            else if (scope == MyScope.ExternalNamespace)
                            {
                                bool ok = false;
                                NamespaceList newexns = null;
                                for (int k = 0; k < exns.Length; k++)
                                    for (int i = 0; i < exns[k].NestedNamespaces.Length; i++)
                                    {
                                        if (exns[k].NestedNamespaces[i].Name.Name == name)
                                        {
                                            if (newexns == null) newexns = new NamespaceList();
                                            newexns.Add(exns[k].NestedNamespaces[i]);
                                            ok = true;
                                        }
                                    }
                                if (ok)
                                {
                                    exns = newexns;
                                }
                                else
                                {// Probably it's a nested type     
                                    for (int k = 0; k < exns.Length; k++)
                                    {
                                        for (int i = 0; i < exns[k].Types.Length; i++)
                                        {
                                            if (exns[k].Types[i].Name.Name == name)
                                            {
                                                exnd = exns[k].Types[i];
                                                ok = true;
                                                scope = MyScope.ExternalType;
                                                break;
                                            }
                                        }
                                        if (ok) break;
                                    }
                                }
                                if (!ok)
                                {
                                    scope = MyScope.None;
                                    break;
                                }
                            }
                            dotPos = identifierText.IndexOf('.');
                        } while (dotPos > 0);

                        // OUTPUT
                        if (scope == MyScope.ExternalNamespace && exns != null)
                        {
                            int N = 0;
                            for (int i = 0; i < exns.Length; i++)
                            {
                                N += ((exns[i].NestedNamespaces != null) ? exns[i].NestedNamespaces.Length : 0) +
                                    ((exns[i].Types != null) ? exns[i].Types.Length : 0);
                            }

                            string[] options = new string[N];
                            int[] glyphs = new int[N];
                            int j = 0;
                            for (int k = 0; k < exns.Length; k++)
                            {
                                if (exns[k].NestedNamespaces != null)
                                    for (int i = 0; i < exns[k].NestedNamespaces.Length; i++)
                                    {
                                        options[j] = exns[k].NestedNamespaces[i].Name.Name;
                                        glyphs[j] = glyphProvider.GetGlyph(exns[k].NestedNamespaces[i]);
                                        j++;
                                    }
                                if (exns[k].Types != null)
                                    for (int i = 0; i < exns[k].Types.Length; i++)
                                    {
                                        options[j] = exns[k].Types[i].Name.Name;
                                        glyphs[j] = glyphProvider.GetGlyph(exns[k].Types[i]);
                                        j++;
                                    }
                            }
                            Array.Sort(options, glyphs);
                            decl = new Declarations(options, options, glyphs, partialText, view);
                        }
                        else if (scope == MyScope.ExternalType && exnd != null)
                        {
                            int N = exnd.Members.Length;
                            string[] options = new string[N];
                            int[] glyphs = new int[N];
                            for (int i = 0; i < N; i++)
                            {
                                options[i] = exnd.Members[i].Name.Name;
                                glyphs[i] = glyphProvider.GetGlyph(exnd.Members[i]);
                            }
                            Array.Sort(options, glyphs);
                            decl = new Declarations(options, options, glyphs, partialText, view);
                        }
                        else if (scope == MyScope.Local && context != null)
                        {
                            if (context is OBJECT_DECL)
                            {
                                OBJECT_DECL odecl = (OBJECT_DECL)context;
                                int countUnkonwn = 0; // To be skipped
                                decls = new System.Collections.Generic.SortedList<string, DECLARATION>();

                                for (int i = 0; i < odecl.locals.Length; i++)
                                {
                                    if (odecl.locals[i] is IMPORT_DECL && ((IMPORT_DECL)odecl.locals[i]).IsSatellite())
                                    {
                                        UNIT_DECL imp = ((IMPORT_DECL)odecl.locals[i]).imported_unit;
                                        for (int j = 0; j < imp.locals.Length; j++)
                                        {
                                            if (!(imp.locals[j] is UNKNOWN_DECL))
                                            {
                                                decls.Add(imp.locals[j].Name + Unique(), imp.locals[j]);
                                            }
                                        }
                                    }
                                    if (!(odecl.locals[i] is UNKNOWN_DECL))
                                    {
                                        decls.Add(odecl.locals[i].Name + Unique(), odecl.locals[i]);
                                    }
                                }
                            }
                        }
                    }
                    else // Give the local scope: EnclosingRoutine + Enclosing Unit
                    {
                        //Give local context
                        ROUTINE_DECL routine = null;
                        OBJECT_DECL obj = null;
                        MODULE_DECL mod = null;
                        IMPLEMENTATION_DECL imp = null;
                        DECLARATION ctx = context;
                        while (true)
                        {
                            if (ctx is ROUTINE_DECL) routine = (ROUTINE_DECL)ctx;
                            else if (ctx is OBJECT_DECL) obj = (OBJECT_DECL)ctx;
                            else if (ctx is MODULE_DECL) mod = (MODULE_DECL)ctx;
                            else if (ctx is IMPLEMENTATION_DECL) imp = (IMPLEMENTATION_DECL)ctx;

                            if (ctx == null || ctx.IsTopLevelUnit()) break;
                            ctx = ctx.getEnclosingDeclaration();
                        }

                        int countUnkonwn = 0; // To be skipped

                        decls = new System.Collections.Generic.SortedList<string, DECLARATION>();

                        if (routine != null)
                        {
                            for (int i = 0; i < routine.parameters.Length; i++)
                            {
                                decls.Add(routine.parameters[i].Name + Unique(), routine.parameters[i]);
                            }
                            for (int i = 0; i < routine.locals.Length; i++)
                            {
                                decls.Add(routine.locals[i].Name + Unique(), routine.locals[i]);
                            }
                        }
                        if (obj != null)
                        {
                            for (int i = 0; i < obj.locals.Length; i++)
                            {
                                if (!(obj.locals[i] is UNKNOWN_DECL))
                                    decls.Add(obj.locals[i].Name + Unique(), obj.locals[i]);
                            }
                            if (obj.definitions != null)
                            {
                                for (int i = 0; i < obj.definitions.Length; i++)
                                {
                                    if (obj.definitions[i] != null)
                                    {
                                        for (int j = 0; j < obj.definitions[i].locals.Length; j++)
                                            if (!(obj.definitions[i].locals[j] is UNKNOWN_DECL))
                                                decls.Add(obj.definitions[i].locals[j].Name + Unique(), obj.definitions[i].locals[j]);
                                    }
                                }
                            }
                        }
                        if (imp != null)
                        {
                            for (int i = 0; i < imp.locals.Length; i++)
                            {
                                if (!(imp.locals[i] is UNKNOWN_DECL))
                                    decls.Add(imp.locals[i].Name + Unique(), imp.locals[i]);
                            }
                            if (imp.implemented_definition != null)
                            {
                                for (int j = 0; j < imp.implemented_definition.locals.Length; j++)
                                    if (!(imp.implemented_definition.locals[j] is UNKNOWN_DECL))
                                        decls.Add(imp.implemented_definition.locals[j].Name + Unique(),
                                            imp.implemented_definition.locals[j]);
                            }
                        }
                        if (mod != null)
                        {
                            for (int i = 0; i < mod.locals.Length; i++)
                            {
                                if (!(mod.locals[i] is UNKNOWN_DECL))
                                    decls.Add(mod.locals[i].Name + Unique(), mod.locals[i]);
                            }
                            if (mod.definitions != null)
                            {
                                for (int i = 0; i < mod.definitions.Length; i++)
                                {
                                    if (mod.definitions[i] != null)
                                    {
                                        for (int j = 0; j < mod.definitions[i].locals.Length; j++)
                                            if (!(mod.definitions[i].locals[j] is UNKNOWN_DECL))
                                                decls.Add(mod.definitions[i].locals[j].Name + Unique(),
                                                    mod.definitions[i].locals[j]);
                                    }
                                }
                            }
                        }


                    }
                }

                if (decl == null)
                {
                    if (decls == null) decl = new Declarations(new string[0], new string[0], new int[0], "", view);
                    else
                    {
                        int N = decls.Count;
                        string[] options = new string[N];
                        string[] texts = new string[N];
                        int[] glyphs = new int[N];

                        for (int i = 0; i < N; i++)
                        {
                            options[i] = GetTextFor(decls.Values[i]);
                            texts[i] = decls.Values[i].Name;
                            glyphs[i] = glyphProvider.GetGlyph(decls.Values[i]);
                        }
                        decl = new Declarations(options, texts, glyphs, partialText, view);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Exception in Intellisense");
            }

            return decl;
        }

        public override Microsoft.VisualStudio.Package.Overloads GetMethods(int line, int col, string name)
        {
            int count = 0;
            string[] descriptions = new string[count];
            string[] names = new string[count];
            string[] parameterCloses = new string[count];
            string[] parameterOpens = new string[count];
            string[] parameterSeparators = new string[count];
            int[] parameterCounts = new int[count];
            return new Overloads(descriptions, names, parameterCloses, parameterOpens, parameterSeparators, parameterCounts);
        }
        public override Microsoft.VisualStudio.Package.Overloads GetTypes(int line, int col, string name)
        {
            int count = 0;
            string[] types = new string[count];
            return new Overloads(types);
        }

        public override string Goto(Microsoft.VisualStudio.Package.VsCommands cmd, Microsoft.VisualStudio.TextManager.Interop.IVsTextView textView, int line, int col, out Microsoft.VisualStudio.TextManager.Interop.TextSpan span)
        {
            span = new Microsoft.VisualStudio.TextManager.Interop.TextSpan();
            SourceContext targetPosition = new SourceContext();
            switch (cmd)
            {
                case Microsoft.VisualStudio.Package.VsCommands.GotoDecl:
                case Microsoft.VisualStudio.Package.VsCommands.GotoDefn:
                    try
                    {
                        NODE node = languageService.SearchZonnonAstForZonnonNodeAtPosition(GetFileName(textView), line, col);
                        if (node is INSTANCE)
                        {
                            NODE entity = ((INSTANCE)node).entity;
                            if (entity != null) targetPosition = entity.sourceContext;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.Assert(false, "Exception in goto definition");
                    }
                    break;
                case Microsoft.VisualStudio.Package.VsCommands.GotoRef:
                        // not implemented
                    break;
            }
            if (targetPosition.Document != null)
            {
                span.iEndIndex = targetPosition.EndColumn - 1;
                span.iEndLine = targetPosition.EndLine - 1;
                span.iStartIndex = targetPosition.StartColumn - 1;
                span.iStartLine = targetPosition.StartLine - 1;
                return targetPosition.Document.Name;
            }
            else
            {
                //TODO: return URL to object browser for imported type information.
            }
            return null;
        }

    }
#endif

#if !ROTOR
    public class Declarations : Microsoft.VisualStudio.Package.Declarations
    {
        Microsoft.VisualStudio.TextManager.Interop.IVsTextView view;
        string[] options, texts;
        int[] glyphs;
        string filter;
        int leftRange, rightRange;

        private void applyFilter()
        {            
            leftRange = 0;
            rightRange = options.Length - 1;
            if(options.Length == 0) return;
            while (!(options[leftRange].StartsWith(filter)) && (leftRange<options.Length - 1)) leftRange++;
            while (!(options[rightRange].StartsWith(filter)) && (rightRange > 0)) rightRange--;            
        }

        public Declarations(string[] options, string [] texts, int[] glyphs, string partialInput, Microsoft.VisualStudio.TextManager.Interop.IVsTextView view)
        {            
            this.options = options;
            this.glyphs = glyphs;
            this.view = view;
            this.filter = partialInput;
            this.texts = texts;
            leftRange = 0;
            rightRange = options.Length - 1;
            applyFilter();
        }

        public override int GetCount()
        {            
            return options.Length;
        }
        public override string GetDisplayText(int index)
        {            
            return options[index];
        }
        public override string GetInsertionText(int index)
        {
            return texts[index];
        }
        public override int GetGlyph(int index)
        {
            return glyphs[index];
        }
        public override string GetDescription(int index)
        {
            return "";
        }

        public override void GetBestMatch(string text, out int index, out bool uniqueMatch)
        {
            filter = text;
            applyFilter();
            int n = rightRange - leftRange+1;
            uniqueMatch = n==1;
            if (leftRange <= rightRange) index = leftRange;
            else index = 0;
        }
        public override bool IsCommitChar(string textSoFar, char commitChar)
        {
            filter = textSoFar;
            applyFilter();
            int n = rightRange - leftRange + 1;
            if (n > 1) return false;
            if (n == 1) return true;
            return !(Char.IsLetterOrDigit(commitChar) || commitChar == '_');
        }
    }
#endif

#if !ROTOR
    public class Overloads : Microsoft.VisualStudio.Package.Overloads
    {
        private string[] descriptions;
        private string[] names;
        private string[] parameterCloses;
        private string[] parameterOpens;
        private string[] parameterSeparators;
        private int[] parameterCounts;
        //private int[] positionOfSelectedMembers;
        private string[] types;

        public Overloads(string[] descriptions, string[] names, string[] parameterCloses,
            string[] parameterOpens, string[] parameterSeparators, int[] parameterCounts)
        {
            this.descriptions = descriptions;
            this.names = names;
            this.parameterCloses = parameterCloses;
            this.parameterOpens = parameterOpens;
            this.parameterSeparators = parameterSeparators;
            this.parameterCounts = parameterCounts;
        }

        public Overloads(string[] types)
        {
            this.types = types;
        }

        public override int GetCount()
        {
            return names.Length;
        }
        public override string GetDescription(int index)
        {
            return descriptions[index];
        }
        public override string GetName(int index)
        {
            return names[index];
        }
        public override string GetParameterClose(int index)
        {
            return parameterCloses[index];
        }
        public override int GetParameterCount(int index)
        {
            return parameterCounts[index];
        }
        public override void GetParameterInfo(int index, int parameter, out string name, out string display, out string description)
        {
            name = names[index];
            display = "";
            description = descriptions[index];
        }
        public override string GetParameterOpen(int index)
        {
            return parameterOpens[index];
        }
        public override string GetParameterSeparator(int index)
        {
            return parameterSeparators[index];
        }
        public override string GetType(int index)
        {
            return types[index];
        }
        public override int GetPositionOfSelectedMember()
        {
            return 0;
        }
    }
#endif
    public class AuthoringHelper : System.Compiler.AuthoringHelper
    {

        public AuthoringHelper(ErrorHandler errorHandler, CultureInfo culture) :
            base(errorHandler, culture)
        {
        }

     
    }
}
