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

//using Microsoft.VisualStudio.Package;
using System;
using System.Collections;
using System.CodeDom.Compiler;
using System.Compiler;
using System.Diagnostics;
using System.IO;
using ETH.Zonnon.Compute;

namespace ETH.Zonnon
{
    public sealed class StructError : Exception
    {
        public Identifier last_id;
        public StructError ( Identifier id ) { last_id = id; }
    }

    public sealed class ParserFactory : IParserFactory
    {
        public IParser CreateParser ( string fileName, int lineNumber, DocumentText text, Module symbolTable,
                                      ErrorNodeList errorNodes, CompilerParameters options)
        {
            Document document = ZonnonCompiler.CreateZonnonDocument(fileName, lineNumber, text);
            return new Parser(document,errorNodes,symbolTable,options as ZonnonCompilerParameters);
        }
    }

    // PARSER
    // ------
    //
    public sealed class Parser : IParser
    {
        public static Scanner       LEXAN;
        private       Recovery      IMPROVER;
        private       ErrorNodeList errors;
		private		  NODE			predecessor;
        private       AWAIT         enclosingAwait = null;

        public ZonnonCompilerParameters options;
                 // Should be set immediately after creating Parser's instance.
        public static bool debug  = false;
        public static bool debugT = false;

        private        AuthoringSink sink;
        private static Guid          dummyGuid = new Guid();

        public Parser ( Module symbolTable, ZonnonCompilerParameters options )
        {
            CONTEXT.options = this.options = options;
            CONTEXT.symbolTable = symbolTable;
            CONTEXT.firstPass = true;
            CONTEXT.init();  // Is it really necessary? What is ctor for, anyway?
        }

        public Parser ( Document document, ErrorNodeList errors, Module symbolTable, ZonnonCompilerParameters options )
        {
            LEXAN = new Scanner(document,errors, (this.options!=null)?this.options.GenerateXML:false);
            ERROR.open(LEXAN,errors);

            this.errors = errors;

            IMPROVER = new Recovery();

            CONTEXT.options = this.options = options;
            CONTEXT.symbolTable = symbolTable;
            CONTEXT.firstPass = true;
            CONTEXT.init();
        }

        public CompilationUnit ParseCompilationUnit ( String source,
                                                      String fname,
                                                      Compilation compilation,
                                                      CompilerParameters parameters,
                                                      ErrorNodeList errors,
                                                      AuthoringSink sink )
        {
#if DEBUG
            Enter("ParseCompilationUnit 1");
#endif
            Guid dummy = Parser.dummyGuid;
            Document document = new Document(fname, 1, source, dummy, dummy, dummy);
            this.errors = errors;
            this.options.SourceFiles.Clear();
            this.options.SourceFiles.Add(fname); //hv: Otherwise this.ParseCompilationUnit will not succeed
            LEXAN = new Scanner(document, errors, options.GenerateXML);
            this.sink = sink;
            try
            {
             // CompilationUnit cu = new CompilationUnit();
                CompilationUnit cu = compilation.CompilationUnits[0];
///             cu.Namespaces = new NamespaceList();
///             cu.TargetModule = (new Compiler()).CreateAssembly(parameters, errors);
                cu.Nodes = new NodeList();
                this.ParseCompilationUnit(cu);
#if DEBUG
                Exit("ParseCompilationUnit 1",true);
#endif
                return cu;
            }
            finally
            {
                this.errors = null;
                LEXAN = null;
                this.sink = null;
#if DEBUG
                Exit("ParseCompilationUnit 1",false);
#endif
            }
        }

        public void ParseCompilationUnit ( CompilationUnit cu )
        {
            ZonnonCompilation zc = cu.Compilation as ZonnonCompilation;
            if ( zc != null ) CONTEXT.globalTree = zc.globalTree;
            CONTEXT.compilation = cu.Compilation;
#if DEBUG
         // EXTERNALS.report();
            Enter("ParseCompilationUnit 2");
#endif
            CONTEXT.clean();
            CONTEXT.enter(CONTEXT.globalTree);
            while ( !LEXAN.isEOF() )
            {
				// check, if we have a comment
				parseComment(null);
                

                if ( parseProgramUnit() )
                {
                    CONTEXT.clean();
                    CONTEXT.enter(CONTEXT.globalTree);
                    if ( !LEXAN.expectDot() )
                    {
                        LEXAN.isSemicolon();
                        // Typical error: semicolon instead of dot;
                        // if this is the error, just eat semicolon.
                    }
                }
                else
                {
                    LEXAN.skipUntil(Recovery.Dot_Module_Definition_Implementation_Object);
                    LEXAN.isDot();
                }
            }
            CONTEXT.exit();

//          ......................................................            
            
#if DEBUG
            if ( debug )
            {
//              STANDARD.report();
                CONTEXT.globalTree.report(0);
            }
            Exit("ParseCompilationUnit 2");
#endif
        }

        public static void ConvertTree ( CompilationUnit cu )
        {
            ZonnonCompilation zc = cu.Compilation as ZonnonCompilation;
            if ( zc == null ) { Debug.Assert(false); return; }

            CONTEXT.firstPass = false;

//          ......................................................
            // Resolve pass TODO: Separate resolve from convert

            // Perform global conversion: Zonnon Tree => CCI Tree            
            Node ns = zc.globalTree.convert();
            CONTEXT.globalMath.AttachToNamespace(ns as Namespace);
            CONTEXT._kernelRegistry.SetNamespace(ns as Namespace);
            CONTEXT._operationRegistry.SetNamespace(ns as Namespace);
            ExpressionConverter.SetNamespace(ns as Namespace);

#if DEBUG
            if (true)
            {
                //Tree output
                //zc.globalTree.report(0); //!uncomment it for zonnon tree output
                //ETH.Zonnon.REPORT.report(ns, 0);
            }
#endif

            // Add small starter, if necessary
            // NODE.prepare(ns); // Obsolete embedded dialogue
            // Put the resulting CCI tree to the compilation unit
            if ( cu.Nodes == null ) cu.Nodes = new NodeList();
            cu.Nodes.Add(ns);

//          ......................................................
            if ( CONTEXT.options.MainModule != null && !CONTEXT.options.EmbeddedDialogue &&
                 !zc.wasMainModule )
            {
                ERROR.NoMainModule(CONTEXT.options.MainModule);
             // return;
            }

            //          ......................................................
           
            if (CONTEXT.options.GenerateXML)
            {
				for (int i = 0; i < CONTEXT.options.SourceFiles.Count; i++)
				{
					//FileStream file = new FileStream(CONTEXT.options.SourceFiles[i].ToString() + ".xml", FileMode.Create, FileAccess.Write);
					//StreamWriter sw = new StreamWriter(file);
					//sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
					//sw.WriteLine("<!DOCTYPE compilation SYSTEM \"http://n.ethz.ch/student/urmuelle/Zonnon/zonnon.dtd\">");
					//sw.WriteLine("<compilation>");
					System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
					System.Xml.XmlNode compilation = doc.CreateNode(System.Xml.XmlNodeType.Element, "compilation", "");
					//compilation.Attributes.Append(doc.CreateAttribute("filename"));
					//compilation.Attributes["filename"].Value = CONTEXT.options.SourceFiles[i].ToString();
                    
                    ASTtoXML visitor = new ASTtoXML(compilation, doc, CONTEXT.options.SourceFiles[i].ToString());
                    visitor.Visit(CONTEXT.globalTree);

                    // CONTEXT.globalTree.generateXML(compilation, doc, );
					//sw.WriteLine("</compilation>");
					//sw.Close();
					System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(CONTEXT.options.SourceFiles[i].ToString() + ".xml", System.Text.Encoding.UTF8);
					writer.Formatting = System.Xml.Formatting.Indented;

					compilation.WriteTo(writer);

					writer.Flush();
					writer.Close();
				}
            }
           
#if DEBUG
            if (CONTEXT.options.Debug)
                ETH.Zonnon.REPORT.report(ns, 0);
//            if ( debug || debugT )
//            {
//                REPORT.report(cu,0);
//            }
//            if ( debug )
//            {
////              STANDARD.report();
//                zc.globalTree.report(0);   // To see how the Zonnon tree has been collected and resolved
//            }
#endif
        }

        Expression IParser.ParseExpression ( )
        {
            return this.ParseExpression(0, null, null);
        }

        public Expression ParseExpression ( int startColumn, string terminator, AuthoringSink sink )
        {
#if DEBUG
            Enter("ParseExpression");
#endif
    //      this.sink = sink;
    //      TokenSet followers = Parser.EndOfFile;
    //      if (terminator != null)
    //          followers |= this.GetTokenFor(terminator);
    //      this.scanner.endPos = startColumn;
    //      return this.ParseExpression(followers);
#if DEBUG
            Exit("ParseExpression");
#endif
            return null;
        }

        public void ParseMethodBody ( Method method )
        {
#if DEBUG
            Enter("ParseMethodBody");
#endif
    //      this.GetNextToken();
    //      method.Body.Statements = new StatementList();
    //      this.ParseStatements(method.Body.Statements, Parser.EndOfFile);
#if DEBUG
            Exit("ParseMethodBody");
#endif
        }

        void IParser.ParseStatements ( StatementList statements )
        {
            this.ParseStatements(statements, 0, null, null);
        }

        public int ParseStatements ( StatementList statements,
                                     int startColumn,
                                     string terminator,
                                     AuthoringSink sink )
        {
#if DEBUG
            Enter("ParseStatements");
#endif
    //      this.sink = sink;
    //      TokenSet followers = Parser.EndOfFile;
    //      if (terminator != null)
    //          followers |= this.GetTokenFor(terminator);
    //      this.scanner.endPos = startColumn;
    //      this.GetNextToken();
    //      this.ParseStatements(statements, followers);
    //      return this.scanner.CurrentSourceContext.StartCol;
#if DEBUG
            Exit("ParseStatements");
#endif
            return 0;
        }

        void IParser.ParseTypeMembers ( TypeNode type )
        {
            this.ParseTypeMembers(type, 0, null, null);
        }

        public int ParseTypeMembers ( TypeNode type,
                                      int startColumn,
                                      string terminator,
                                      AuthoringSink sink )
        {
#if DEBUG
            Enter("ParseTypeMembers");
#endif
    //      this.sink = sink;
    //      TokenSet followers = Parser.EndOfFile;
    //      if (terminator != null)
    //          followers |= this.GetTokenFor(terminator);
    //      this.scanner.endPos = startColumn;
    //      this.GetNextToken();
    //      this.ParseTypeMembers(type, followers);
    //      return this.scanner.CurrentSourceContext.StartCol;
#if DEBUG
            Exit("ParseTypeMembers");
#endif
            return 0;
        }

		// parseComment
		// -------------
		//
		// Looks up, whether symbol is a comment and creates according nodes
		//
		public void parseComment ( NODE owner )
		{
			while ( LEXAN.isComment() )
			{
				string str;
				str = Scanner.comment;
#if DEBUG
				Enter("Processing comment");
#endif
				COMMENT c = new COMMENT(owner);
				c.str = str;
				this.predecessor = c;
#if DEBUG
				Exit("Comment");
#endif
			}
		}

        // parseProgramUnit
        // ----------------
        //
        // ProgramUnit = ( Module | Definition | Implementation | Object )
        //
        public bool parseProgramUnit ( )
        {
            bool result = true;
#if DEBUG
            Enter("Program Unit");
#endif

			// See, whether next symbol is a comment
			//parseComment(null, this.predecessor);

			if      ( LEXAN.isModule() )         {  parseModule();         }
			else if ( LEXAN.isDefinition() )     {  parseDefinition();     }
			else if ( LEXAN.isImplementation() ) {  parseImplementation(); }
			else if ( LEXAN.isObject() )         {  parseObject();         }
			else if ( LEXAN.isRecord() )         {  parseRecord();         }
            else if ( LEXAN.isProtocol())        {  parseProtocol(CONTEXT.current_namespace);   }
			else if ( LEXAN.isEOF() )
			{
				result = false;  // End of compilation
			}
			else
			{
				ERROR.WrongUnitKeyword(Scanner.last_id.Name);
				result = false;
			}
#if DEBUG
            Exit("Program Unit",result);
#endif
            return result;
        }

        // parseModule
        // -----------
        // module = MODULE QualIdent [ ImplementationClause ] ";" [ Import ] ObjectSpec ".".
        //
        // 'MODULE' keyword has been already parsed.
        //
        private void parseModule ( )
        {
#if DEBUG
            Enter("Module");
#endif
            SourceContext start = LEXAN.getSourceContext(); // 'module' keyword
            int line_number = start.StartLine;
            
			// See, whether next symbol is a comment
			parseComment(null);
            MODIFIERS modifiers  = parseModifiers();

            MODULE_DECL module = null;
			// See, whether next symbol is a comment
			parseComment(null);
            IDENT_LIST  ident = parseQualIdent();

            if ( ident == null )
            {
                ERROR.NoUnitName("module");
                LEXAN.skipUntil(Recovery.AfterModuleName);
                ident = new IDENT_LIST();
                ident.Add(ERROR.errUnitName);
            }

            // Creating context for the new module,
            // creating initial tree for the module itself.
            module = MODULE_DECL.create(ident,modifiers);

            // IMPLEMENTS ...
            parseImplementationClause(module,Recovery.ExportInModule);

			// See, whether next symbol is a comment
			parseComment(module);

            if ( !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);  // the better way is to add Semicolon to ExportInModule.
                LEXAN.isSemicolon();  // just eat ';'...
            }
            SourceContext sc_begin = LEXAN.getSourceContext();

            parseImportDeclaration(module,Recovery.ImportInModule);

            // { Declaration } { NestedUnit } [ BEGIN ... ] END [ QualIdent ]
            parseDeclarations(module);

            Identifier id_final = null;

			// See, whether next symbol is a comment
			parseComment(module);
      
            if ( LEXAN.isBegin() )
            {	
                id_final = parseBlockStatement(module);
                sc_begin = LEXAN.getSourceContext();
            }
            else if ( !LEXAN.expectEnd() )
            {
                ERROR.SyntaxErrorIn("module","final END is not found");
                LEXAN.skipUntil(LEXEM.End);  // LEXAN.skipUntil(Recovery.End_Dot);
             // LEXAN.isEnd();
            }
            else
            {
				// See, whether next symbol is a comment
				parseComment(module);
                if ( LEXAN.isIdent() ) id_final = Scanner.last_id;
            }
            if ( id_final == null )
            {
                ERROR.NoFinalIdentifier("module",module.name.Name);
            }
            else // id_final != null
            {
                // Compare final name with the module name
                if (sink != null)
                    sink.MatchPair(sc_begin, LEXAN.getSourceContext());
            }

			// See, whether next symbol is a comment
			parseComment(module);

			predecessor = module;
            module.setContext(start,LEXAN.getSourceContext());
            UNIT_DECL.finalize(module,id_final);
#if DEBUG
            Exit("Module");
#endif
        }

        // parseImplementationClause
        // -------------------------
        // ImplementationClause = IMPLEMENTS QualIdent { "," QualIdent }
        //
        private void parseImplementationClause ( NODE unit, BitArray followers )
        {
#if DEBUG
            Enter("ImplementationClause");
#endif
			// See, whether next symbol is a comment
			parseComment(unit);
            // IMPLEMENTS ...
            if ( !LEXAN.isImplements() ) goto Finish;

            do
            {
                IDENT_LIST qualName = parseQualIdent();

                if (qualName != null)
                {
                    if (unit is UNIT_DECL)
                        ((UNIT_DECL)unit).addImplementedDefinition(qualName);
                    else if (unit is ACTIVITY_DECL)
                        ((ACTIVITY_DECL)unit).addImplementedProtocol(qualName);
                    else if (unit is ROUTINE_DECL) // procedure or operator
                        ((ROUTINE_DECL)unit).addImplementedProcedure(qualName);
                    else
                        ERROR.SystemErrorIn("parseImplementationClause", "wrong unit");

                    // Taking next qual-id from the 'implements' clause.
                    continue;
                }
                else
                {
                    // Possibly is's [] definition
                    if (LEXAN.isLeftBracket() && LEXAN.isRightBracket())
                    { // Indexer
                        if (unit is OBJECT_DECL)
                        {
                            (unit as OBJECT_DECL).indexer = new OBJECT_DECL.INDEXER_DECL();
                        }
                        else if (unit is PROCEDURE_DECL)
                        {
                            PROCEDURE_DECL proc = unit as PROCEDURE_DECL;
                            OBJECT_DECL obj = proc.enclosing as OBJECT_DECL;
                            if ((obj != null) && (obj.indexer != null))
                            {
                                // We're in the object that implements indexer
                                // It can be [].Set or [].Get or just
                                //             ^^^^      ^^^^        
                                if (LEXAN.isDot()&&LEXAN.isIdent())
                                {
                                    string name = Scanner.last_id.Name;
                                    if (name.ToUpper() == "GET")
                                    {
                                        obj.indexer.Get = proc;
                                    }
                                    else if (name.ToUpper() == "SET")
                                    {
                                        obj.indexer.Set = proc;
                                    }else
                                    ERROR.SyntaxErrorIn("indexer","Should be [].Get or [].Set");                                    
                                }
                                else
                                {
                                    ERROR.SyntaxErrorIn("indexer","Should be [].Get or [].Set");
                                }
                            }
                        }
                    }
                    
                }
                // ident==null: An error in qual-id.
                // -- error was reported by parseQualIdent
                followers.Or(Recovery.InsideExport);  // ';' | ','

				// See, whether next symbol is a comment
				parseComment(unit);

                LEXAN.skipUntil(followers);

				// See, whether next symbol is a comment
				parseComment(unit);
            }
            while
                ( LEXAN.isComma() );

         Finish:
#if DEBUG
            Exit("ImplementationClause");
#endif
            return;
        }

        // parseImportDeclaration
        // ----------------------
        // ImportDeclaration = IMPORT Import { "," Import } ";".
        // Import            = QualIdent [ AS Ident ].
        //
        private bool parseImportDeclaration ( UNIT_DECL unit, BitArray followers )
        {
            bool result = false;
#if DEBUG
            Enter("Import");
#endif
			// See, whether next symbol is a comment
			parseComment(unit);

            // IMPORT ...
            if ( !LEXAN.isImport() ) goto Finish;
            result = true;

            do {                
                IDENT_LIST qualName = parseQualIdent();                
                Identifier nick  = null;                
                if ( qualName == null )
                {
                    ERROR.SyntaxErrorIn("import declaration","incorrect qual-id");
                    followers.Or(Recovery.InsideImport);    // ',' | AS | ';'

					// See, whether next symbol is a comment
					parseComment(unit);

                    LEXAN.skipUntil(followers);
                    continue;
                }

				// See, whether next symbol is a comment
				parseComment(unit);

                if ( LEXAN.isAs() )
                {
					// See, whether next symbol is a comment
					parseComment(unit);

                    LEXAN.expectIdent();
                    nick = Scanner.last_id;
                }

                unit.addImportedUnit(qualName, nick, false, qualName.sourceContext);

				// See, whether next symbol is a comment
				parseComment(unit);
            }
            while
                ( LEXAN.isComma() );

			// See, whether next symbol is a comment
			parseComment(unit);

            if ( !LEXAN.expectSemicolon() )
            {
                ERROR.SyntaxErrorIn("import declaration","no semicolon");
                LEXAN.skipUntil(followers);
				
				// See, whether next symbol is a comment
				parseComment(unit);

                LEXAN.isSemicolon();
            }

          Finish:
#if DEBUG
            Exit("Import",result);
#endif
            return result;
        }

        // parseDefinition
        // ---------------
        // definition = DEFINITION QualIdent [ EXTENDS QualIdent ] ";" [ Import ] { Declaration } END [ QualIdent ]
        //
        // 'DEFINITION' keyword has been already parsed.
        //
        private void parseDefinition ( )
        {
#if DEBUG
            Enter("Definition");
#endif
			// See, whether next symbol is a comment
			parseComment(null);

            SourceContext start = LEXAN.getSourceContext(); // 'definition' keyword
            MODIFIERS modifiers  = parseModifiers();

            DEFINITION_DECL definition = null;
            IDENT_LIST      ident = parseQualIdent();

            if ( ident == null )
            {
                ERROR.NoUnitName("definition");
                LEXAN.skipUntil(Recovery.AfterDefinitionName);
                ident = new IDENT_LIST();
                ident.Add(ERROR.errUnitName);
            }

            // Creating context for the new definition,
            // creating initial tree for the definition itself.
            definition = DEFINITION_DECL.create(ident,modifiers);

			// See, whether next symbol is a comment
			parseComment(definition);

            if ( LEXAN.isRefines() )
            {
                IDENT_LIST base_definition = parseQualIdent();
                if ( base_definition == null ) // an error in qual-id
                {
                    ERROR.SyntaxErrorIn("extends clause","incorrect qual-id");
                    LEXAN.skipUntil(Recovery.AfterDefinitionName);
                }
                else
                {
                    definition.addBaseDefinition(base_definition);
                }
            }

			// See, whether next symbol is a comment
			parseComment(definition);

            if ( !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);  // the better way is to add Semicolon to ImportInDefinition.

				// See, whether next symbol is a comment
				parseComment(definition);

                LEXAN.isSemicolon();  // just eat ';' if we stopped on it
            }

            parseImportDeclaration(definition,Recovery.ImportInDefinition);

            parseDeclarations(definition);

			// See, whether next symbol is a comment
			parseComment(definition);

            if ( !LEXAN.expectEnd() )
            {
                ERROR.SyntaxErrorIn("definition","final END is not found");
                LEXAN.skipUntil(LEXEM.End);  // LEXAN.skipUntil(Recovery.End_Dot);
                                             // LEXAN.isEnd();
				// See, whether next symbol is a comment
				parseComment(definition);

                LEXAN.isEnd();
            }

            Identifier id_final = null;

			// See, whether next symbol is a comment
			parseComment(definition);

            if ( LEXAN.isIdent() )  id_final = Scanner.last_id;
            else                    ERROR.NoFinalIdentifier("definition",definition.name.Name);

			// See, whether next symbol is a comment
			parseComment(definition);

            definition.setContext(start,LEXAN.getSourceContext());
            UNIT_DECL.finalize(definition,id_final);
#if DEBUG
            Exit("Definition");
#endif
        }

        // parseModifiers
        // --------------
        // DeclModifiers = "{" DeclModifier "}"
        // DeclModifier = PUBLIC | PRIVATE | IMMUTABLE
        //
        // ProcModifiers = "{" ProcModifier { "," ProcModifier  } "}"
        // ProcModifier = PRIVATE | PUBLIC | FINAL
        //
        // ObjModifiers = "{" ObjModifier { "," ObjModifier } "}"
        // ObjModifier = VALUE | REF
        //
		private MODIFIERS parseModifiers ( )
		{
			return parseModifiers (null);
		}

		private MODIFIERS parseModifiers ( MODIFIERS prevmodifiers )
		{
#if DEBUG
			Enter("Modifiers");
#endif
			MODIFIERS modifiers;
			if(prevmodifiers == null)
			{
				modifiers = new MODIFIERS();
			}
			else
			{
				modifiers = prevmodifiers;
			}

			// See, whether next symbol is a comment
			parseComment(null);

            if ( !LEXAN.isLeftBrace() ) goto Finish;

            do
            {
             // if ( LEXAN.isIntNumber() )
             // {
             //     // Operator's precedence
             //     modifiers.Precedence = (int)Scanner.integer_value;
             // }
             // else 

				// See, whether next symbol is a comment
				parseComment(null);

                if ( LEXAN.isIdent() )
                {
                    // Add modifier to the list if ids.
                    int modifier_code = Scanner.detectModifier();
                    if ( modifier_code == -1 )
                        ERROR.IllegalModifier(Scanner.last_id.Name);
                    else
                        modifiers.setModifier(modifier_code,true);
                }
                else  // something's wrong...
                {
                    ERROR.SyntaxErrorIn("modifiers","incorrect modifiers' list");
                    LEXAN.skipUntil(Recovery.Comma_RightBrace_Id);
                }

				// See, whether next symbol is a comment
				parseComment(null);
            }
            while
                ( LEXAN.isComma() );

            if ( !LEXAN.expectRightBrace() )
            {
                ERROR.SyntaxErrorIn("modifiers","no right brace");
                LEXAN.skipUntil(Recovery.RightBrace_Id);
                LEXAN.expectRightBrace();
            }

          Finish:
#if DEBUG
            Exit("Modifiers",modifiers!=null);
#endif
            modifiers.validate();
            return modifiers;
        }

        // parseImplementation
        // -------------------
        // implementation = IMPLEMENTATION QualIdent ";" [ ImportList ] { Declaration } END [ Ident ].
        //
        // 'IMPLEMENTATION' keyword has been already parsed.
        //
        private void parseImplementation ( )
        {
#if DEBUG
            Enter("Implementation");
#endif
			// See, whether next symbol is a comment
			parseComment(null);

			// int line_number = LEXAN.getSourceContext().StartLine;

            SourceContext start = LEXAN.getSourceContext(); // 'implementation' keyword
            IMPLEMENTATION_DECL implementation = null;
            IDENT_LIST ident = parseQualIdent();

            if ( ident == null )
            {
                ERROR.NoUnitName("implementation");
                LEXAN.skipUntil(Recovery.AfterImplementationName);
                ident = new IDENT_LIST();
                ident.Add(ERROR.errUnitName);
            }

            // Creating context for the new implementation,
            // creating initial tree for the implementation itself.
            implementation = IMPLEMENTATION_DECL.create(ident);

			// See, whether next symbol is a comment
			parseComment(implementation);

            if ( !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);  // the better way is to add Semicolon to ImportInImplementation.

				// See, whether next symbol is a comment
				parseComment(implementation);

                LEXAN.isSemicolon();  // just eat ';' if we stopped on it
            }

            parseImportDeclaration(implementation,Recovery.ImportInImplementation);
            parseDeclarations(implementation);

            Identifier id_final = null;

			// See, whether next symbol is a comment
			parseComment(implementation);

            if ( LEXAN.isBegin() )
            {
                id_final = parseBlockStatement(implementation);
            }
            else if ( !LEXAN.expectEnd() )
            {
                ERROR.SyntaxErrorIn("implementation","final END is not found");
				
				// See, whether next symbol is a comment
				parseComment(implementation);

                LEXAN.skipUntil(LEXEM.End);  // LEXAN.skipUntil(Recovery.End_Dot);
             // LEXAN.isEnd();
            }
            else
            {
				// See, whether next symbol is a comment
				parseComment(implementation);

                if ( LEXAN.isIdent() ) id_final = Scanner.last_id;
            }
            if ( id_final == null )
            {
                ERROR.NoFinalIdentifier("implementation",implementation.name.Name);
            }
            else // id_final != null
            {
                // Compare final name with the implementation name
            }

			// See, whether next symbol is a comment
			parseComment(implementation);

            implementation.setContext(start,LEXAN.getSourceContext());
            UNIT_DECL.finalize(implementation,id_final);
#if DEBUG
            Exit("Implementation");
#endif
        }

        // parseObject
        // -----------
        //
        // Object = OBJECT [ ObjModifier ] QualIdent [ ImplementationClause ] ";" [ Import ] ObjectSpec.
        //
        // 'OBJECT' keyword has been already parsed.
        //
        private void parseObject ( )
        {
            parseObject(null,null);
        }

        private void parseObject ( IDENT_LIST ident, MODIFIERS modifiers )
        {
#if DEBUG
            Enter("Object");
#endif
			// See, whether next symbol is a comment
			parseComment(null);

            SourceContext start = LEXAN.getSourceContext(); // 'object' keyword

            bool typeNotation = false;
            if ( ident!=null && modifiers!=null )
                typeNotation = true;
            else if ( CONTEXT.current_unit is MODULE_DECL )
                ERROR.OldObjectSyntax();

			/* if ( modifiers == null )*/ modifiers  = parseModifiers(modifiers);
            if ( ident == null ) ident = parseQualIdent();

            OBJECT_DECL obj = null;

            if ( ident == null )
            {
                ERROR.NoUnitName("object");
                LEXAN.skipUntil(Recovery.AfterObjectName);
                ident = new IDENT_LIST();
                ident.Add(ERROR.errUnitName);
            }

            // Creating context for the new object,
            // creating initial tree for the object itself.
            obj = OBJECT_DECL.create(ident,modifiers);

            // Object parameters; the syntax is the same as for
            // procedure parameters.
            parseFormalParameters(obj);

            // IMPLEMENTS ...
            parseImplementationClause(obj,Recovery.ExportInObject);

			// See, whether next symbol is a comment
			parseComment(obj);

            if ( !typeNotation && !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);  // the better way is to add Semicolon to ExportInObject.

				// See, whether next symbol is a comment
				parseComment(obj);

                LEXAN.isSemicolon();  // just eat ';' if we stopped on it
            }
            parseImportDeclaration(obj,Recovery.ImportInObject);
            parseDeclarations(obj);

            Identifier id_final = null;

			// See, whether next symbol is a comment
			parseComment(obj);

            if ( LEXAN.isBegin() )
            {
                id_final = parseBlockStatement(obj);
            }
            else if ( !LEXAN.expectEnd() )
            {
                ERROR.SyntaxErrorIn("object","final END is not found");
                LEXAN.skipUntil(LEXEM.End);  // LEXAN.skipUntil(Recovery.End_Dot);
             // LEXAN.isEnd();
            }
            else
            {
				// See, whether next symbol is a comment
				parseComment(obj);

                if ( LEXAN.isIdent() ) id_final = Scanner.last_id;
            }
            if ( id_final == null )
            {
                ERROR.NoFinalIdentifier("object",obj.name.Name);
            }
            else // id_final != null
            {
                // Compare final name with the object name
            }

			// See, whether next symbol is a comment
			parseComment(obj);

            obj.setContext(start,LEXAN.getSourceContext());
            UNIT_DECL.finalize(obj,id_final);
#if DEBUG
            Exit("Object");
#endif
        }

        // parseRecord
        // -----------
        //
        // Record = RECORD [ public | private ] QualIdent ";" DeclarationSeq [ BEGIN Statements ] END "."
        //
        // 'RECORD' keyword has been already parsed.
        //
        private void parseRecord ( )
        {
            parseRecord(null,null);
        }

        private void parseRecord ( IDENT_LIST ident, MODIFIERS modifiers )
        {
#if DEBUG
            Enter("Record");
#endif
			// See, whether next symbol is a comment
			parseComment(null);

            SourceContext start = LEXAN.getSourceContext(); // 'record' keyword

            bool typeNotation = false;
            if ( ident!=null && modifiers!=null )
                typeNotation = true;
            else if ( CONTEXT.current_unit is MODULE_DECL )
                ERROR.OldObjectSyntax();

            if ( modifiers == null ) modifiers  = parseModifiers();
            if ( ident == null ) ident = parseQualIdent();

            OBJECT_DECL rec = null;

            if ( ident == null )
            {
                ERROR.NoUnitName("record");
                LEXAN.skipUntil(Recovery.AfterRecordName);
                ident = new IDENT_LIST();
                ident.Add(ERROR.errUnitName);
            }

            // Creating context for the new record,
            // creating initial tree for the record itself.
            // Treating record as a simplest variant of val-object.
            rec = OBJECT_DECL.create(ident,modifiers);

            // No object parameters...
            // No 'implements' clause...

			// See, whether next symbol is a comment
			parseComment(rec);

            if ( !typeNotation && !LEXAN.expectSemicolon() )
            {
				// See, whether next symbol is a comment
				parseComment(rec);

                LEXAN.skipUntil(LEXEM.Semicolon);  // the better way is to add Semicolon to ExportInObject.

				// See, whether next symbol is a comment
				parseComment(rec);

                LEXAN.isSemicolon();  // just eat ';' if we stopped on it
            }

            // No import declaration...

            // Only simple variable declarations even without
            // 'var keyword...

            modifiers = new MODIFIERS();
            modifiers.Public = true;
            while ( parseVariableDeclaration(rec,modifiers) ) { }

            Identifier id_final = null;

        //  if ( LEXAN.isBegin() )
        //  {
        //      id_final = parseBlockStatement(rec);
        //  }
        //  else 
			// See, whether next symbol is a comment
			parseComment(rec);

            if ( !LEXAN.expectEnd() )
            {
                ERROR.SyntaxErrorIn("record","final END is not found");
				// See, whether next symbol is a comment
				parseComment(rec);

                LEXAN.skipUntil(LEXEM.End);  // LEXAN.skipUntil(Recovery.End_Dot);
             // LEXAN.isEnd();
            }
            else
            {
				// See, whether next symbol is a comment
				parseComment(rec);

                if ( LEXAN.isIdent() ) id_final = Scanner.last_id;
            }
            if ( id_final == null )
            {
                ERROR.NoFinalIdentifier("record",rec.name.Name);
            }
            else // id_final != null
            {
                // Compare final name with the object name
            }

			// See, whether next symbol is a comment
			parseComment(rec);

            rec.setContext(start,LEXAN.getSourceContext());
            UNIT_DECL.finalize(rec,id_final);
#if DEBUG
            Exit("Record");
#endif
        }

        // parseDeclarations
        // -----------------
        // The function parses the sequence of all kinds of declarations:
        //
        //   CONST [ DeclModifier ] { ConstantDeclaration ";" }  |
        //   TYPE  [ DeclModifier ] { TypeDeclaration     ";" }  |
        //   VAR   [ DeclModifier ] { VariableDeclaration ";" }
        //   NestedUnit
        //   ProcedureDeclaration
        //   OperatorDeclaration
        //   ActivityDeclaration
        //
        // Important moment: The syntax of declarations implemented by this procedure
        // is WIDER (less strict) than native Zonnon syntax. This means that normally
        // Zonnon rules require a specific order of declarations and certain subsets of
        // possible declarations for various kinds of modules/routines.
        // This is done intentionally - to simplify parser and to move some errors
        // from "syntactic" to "semantic" category.
        //
        // Second important moment: A sequence of declarations should be always terminated
        // either by BEGIN or by END. We use this for error recovery: if there is no BEGIN/END
        // after the sequence we assume an error and skip everything until one of those words.
        // This will help in recovering after some hard errors like:
        //
        //      CONST  (* This single word is a valid construct! *)
        //         1;
        //      <Huge sequence of declarations>
        //      END
        //
        // Without checking for BEGIN/END after CONST we will exit from parseDeclarations()
        // staying on '1' lexem.
        //
        private void parseDeclarations ( NODE unit )
        {
            MODIFIERS modifiers;
#if DEBUG
            Enter("Declarations");
#endif
            while ( true )
            {
				// See, whether next symbol is a comment

				parseComment(unit);
                // Simple declarations
                if ( LEXAN.isConst() )
                {
                    modifiers = parseModifiers();
                    while ( parseConstantDeclaration(unit,modifiers) ) { }
                    continue;
                }
				// See, whether next symbol is a comment
				parseComment(unit);

                if ( LEXAN.isType() )
                {
                    modifiers = parseModifiers();
                    while ( parseTypeDeclaration(unit,modifiers) ) { }
                    continue;
                }

				// See, whether next symbol is a comment
				parseComment(unit);

                if ( LEXAN.isVar() )
                {
                    modifiers = parseModifiers();
                    while ( parseVariableDeclaration(unit,modifiers) ) { }
                    continue;
                }

                string where;

                // Nested units
				
				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isDefinition() )     { parseDefinition();     where = "nested definition"; goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isImplementation() ) { parseImplementation(); where = "nested implementation"; goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isObject() )         { parseObject();         where = "nested object"; goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isModule() )         { parseModule();         where = "nested module"; goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isRecord() )         { parseRecord();         where = "nested record"; goto LocalUnitCheck; }

                // Routines
				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isProcedure() ) { parseProcedure(unit);  where = "procedure"; goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isOperator() )  { parseOperator(unit);   where = "operator";  goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isActivity() )  { parseActivity(unit);   where = "activity";  goto LocalUnitCheck; }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isProtocol() )  { parseProtocol(unit);   where = "protocol";  goto LocalUnitCheck; }

                // Otherwise exit the loop
                break;

             LocalUnitCheck:

				// See, whether next symbol is a comment
				parseComment(unit);

                if ( !LEXAN.expectSemicolon() )
                {
                    ERROR.SyntaxErrorIn(where,"no final semicolon");
                    LEXAN.skipUntil(Recovery.AfterLocalUnit);
                }
                continue;

            } // while
#if DEBUG
            Exit("Declarations");
#endif
            return;
        }

        // parseConstantDeclaration
        // ------------------------
        // Ident "=" ConstExpression ";"
        //
        private bool parseConstantDeclaration ( NODE unit, MODIFIERS modifiers )
        {
            bool result = true;
#if DEBUG
            Enter("Constant Declaration");
#endif
            EXPRESSION initializer = null;

			// See, whether next symbol is a comment
			parseComment(unit);

            if ( LEXAN.isIdent() )
            {
                Identifier name = Scanner.last_id;

				// See, whether next symbol is a comment
				parseComment(unit);

                SourceContext start = LEXAN.getSourceContext();

                if ( !LEXAN.expectEqual() )
                {
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.skipUntil(Recovery.Equal_Semicolon);

					// See, whether next symbol is a comment
					parseComment(unit);
                    if ( !LEXAN.isEqual() )
                        // this means we got ';' i.e. we have skipped the entire expression.
                        goto Terminator;
                }

                initializer = parseExpression(null);

                if ( initializer == null )
                {
                    ERROR.SyntaxErrorIn("constant declaration","no initializer");
                    goto Terminator;
                }

                CONSTANT_DECL constant = CONSTANT_DECL.create(unit,modifiers,name,initializer);
				
				// See, whether next symbol is a comment
				parseComment(unit);
                if ( constant != null ) constant.setContext(start,LEXAN.getSourceContext());

            Terminator:

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( !LEXAN.expectSemicolon() )
                {
                    ERROR.SyntaxErrorIn("constant declaration","no semicolon");
                    LEXAN.skipUntil(Recovery.Semicolon_Type_Const_Var_Procedure_End);

					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isSemicolon();  // just eat ';' if we stopped on it
                    result = false;
                }
            }
            else
            {
                // no constant declaration(s) (more)
                result = false;
            }
#if DEBUG
            Exit("Constant Declaration",result);
#endif
            return result;
        }

        // parseTypeDeclaration
        // --------------------
        //
        // Ident "=" Type
        //
        private bool parseTypeDeclaration ( NODE unit, MODIFIERS modifiers )
        {
            bool result = true;
#if DEBUG
            Enter("Type Declaration");
#endif
			// int line_number = LEXAN.getSourceContext().StartLine;
			// See, whether next symbol is a comment
			parseComment(unit);
            if ( LEXAN.isIdent() )
            {
                Identifier name = Scanner.last_id;
				// See, whether next symbol is a comment
				parseComment(unit);
                SourceContext start = LEXAN.getSourceContext();

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( !LEXAN.expectEqual() )
                {
                    // Trying to find either '=' or the first lexem of a type.
                    LEXAN.skipUntil(Recovery.Equal_Array_LeftParenth_Id);
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isEqual();  // just eat '=' if we have stopped on it.
                }

                IDENT_LIST ident = new IDENT_LIST();
                ident.Add(name);

                TYPE type = parseType(ident,modifiers,"type declaration");

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( !LEXAN.expectSemicolon() )
                {
                    ERROR.SyntaxErrorIn("type declaration","no semicolon");
                    LEXAN.skipUntil(Recovery.Semicolon_Type_Const_Var_Procedure_End);
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isSemicolon();  // just eat ';' if we stopped on it
                    result = false;
                }
                if ( type != null )
                {
                    TYPE_DECL type_decl = TYPE_DECL.create(unit,modifiers,name,type);
					// See, whether next symbol is a comment
					parseComment(unit);
					if ( type_decl != null )
					{
						type_decl.setContext(start,LEXAN.getSourceContext());
					}
                }
             // else -- object or record type was parsed
            }
            else
            {
                // no type declaration(s) (more)
                result = false;
            }
#if DEBUG
            Exit("Type Declaration",result);
#endif
            return result;
        }

        // parseVariableDeclaration
        // ------------------------
        //
        // Ident { "," Ident } ":" Type
        //
        private bool parseVariableDeclaration ( NODE unit, MODIFIERS modifiers )
        {
            bool result = true;
#if DEBUG
            Enter("Variable Declaration");
#endif
			//int line_number = LEXAN.getSourceContext().StartLine;
			// See, whether next symbol is a comment
			parseComment(unit);
            if ( LEXAN.isIdent() )
            {
                IDENT_LIST variables = new IDENT_LIST();
                Identifier name = Scanner.last_id;

                variables.Add(name);

				// See, whether next symbol is a comment
				parseComment(unit);
                while ( LEXAN.isComma() )
                {
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isIdent();
                    variables.Add(Scanner.last_id);
                }

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( !LEXAN.expectColon() )
                {
                    // Trying to find either ':' or the first lexem of a type.
                    LEXAN.skipUntil(Recovery.Colon_Array_LeftParenth_Id);
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isColon();  // just eat ':' if we have stopped on it.
                    result = false;
                }

                TYPE type = parseType(null,null,"variable declaration");

				// See, whether next symbol is a comment
				parseComment(unit);
                if ( !LEXAN.expectSemicolon() )
                {
                    ERROR.SyntaxErrorIn("variable declaration","no semicolon");
                    LEXAN.skipUntil(Recovery.Semicolon_Type_Const_Var_Procedure_End);
					// See, whether next symbol is a comment
					parseComment(unit);
                    LEXAN.isSemicolon();  // just eat ';' if we stopped on it
                }
                if ( (type is ARRAY_TYPE || type is PROC_TYPE || type is ENUM_TYPE) && type.enclosing == null )
                {
                    // anonymous type; inserting type declaration for that type...
                    MODIFIERS mods = new MODIFIERS();
                    mods.Private = true;
                    Identifier type_id = Identifier.For("_type_"+variables[0].Name);
                    type_id.SourceContext = variables[0].SourceContext;

                    TYPE_DECL.create(unit,mods,type_id,type);
                }
                VARIABLE_DECL v = VARIABLE_DECL.create(unit,variables,type,false,modifiers);
								
				// See, whether next symbol is a comment
				parseComment(unit);
            }
            else
            {
                // no variable declaration(s) (more)
                result = false;
            }
#if DEBUG
            Exit("Variable Declaration",result);
#endif
            return result;
        }

        // parseProcedure
        // --------------
        //
        // ProcedureDeclaration = ProcedureHeading ";" [ ProcedureBody [ Ident ] ";" ].
        // ProcedureHeading = PROCEDURE [ ProcModifiers ] IdentPub [ FormalParameters ] [ ImplementationClause ].
        //
        // "PROCEDURE" keyword has been already parsed.
        //
        private void parseProcedure ( NODE unit )
        {
#if DEBUG
            Enter("Procedure");
#endif
            if (unit is ROUTINE_DECL && !unit.ErrorReported)
            {
                ERROR.NotImplemented("nested procedures are");
                unit.ErrorReported = true;
            }

            SourceContext  start = LEXAN.getSourceContext(); // 'procedure' keyword

            PROCEDURE_DECL procedure;
            MODIFIERS      modifiers = parseModifiers();
            Identifier     ident = null;

			// See, whether next symbol is a comment
			parseComment(null);
			//int line_number = LEXAN.getSourceContext().StartLine;
            if ( LEXAN.isIdent() )
            {
                ident = Scanner.last_id;
            }
            else
            {
                ERROR.NoUnitName("procedure");
                LEXAN.skipUntil(Recovery.LeftParenth_Id);  // Recovery.AfterProcedureName
                ident = ERROR.errUnitName;
            }

            // Creating context for the new procedure,
            // creating initial tree for the procedure itself.
            procedure = PROCEDURE_DECL.create(ident,modifiers);

            parseFormalParameters(procedure);

			// See, whether next symbol is a comment
			parseComment(procedure);
            if ( LEXAN.isColon() ) procedure.return_type = parseFormalType(false);
            else                   procedure.return_type = new VOID_TYPE();

            procedure.inParams = false;

            // IMPLEMENTS ...
            parseImplementationClause(procedure,Recovery.Semicolon_Type_Const_Var_Procedure_Begin_End);
         // procedure.check(); -- will be called later (during the Second Pass)

            if ( procedure.enclosing is DEFINITION_DECL ) goto Finish;
                // Procedures in definitions do not have bodies
            
			// See, whether next symbol is a comment
			parseComment(procedure);
            if ( !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);
				// See, whether next symbol is a comment
				parseComment(procedure);
                LEXAN.isSemicolon();
            }

            // Procedure body
            parseDeclarations(procedure);

			// See, whether next symbol is a comment
			parseComment(procedure);
            LEXAN.expectBegin();
            procedure.inBody = true;

            Identifier id_final = parseBlockStatement(procedure);

            if (!LEXAN.isEOF() && CONTEXT.current_routine != null && // TODO Strange case when EOF
                (id_final == null || id_final.Name != CONTEXT.current_routine.name.Name))
            {   
                ERROR.NoFinalIdentifier("procedure", procedure.name.Name);
            }
        Finish:
			// See, whether next symbol is a comment
			parseComment(procedure);
         // LEXAN.expectSemicolon(); -- will be done after exiting
            procedure.setContext(start,LEXAN.getSourceContext());

            // Perhaps, something like 'evaluateProcedure' is necessary here...
            CONTEXT.exit();
#if DEBUG
            Exit("Procedure");
#endif
        }

        // parseType
        // ---------
        //
        private TYPE parseType ( IDENT_LIST id, MODIFIERS modifiers, string where )
        {
            // Non-null id & modifiers parameters mean that
            // it's possible to find an object/record type
            // like
            //             type O = object ... end O;
            //
            // These parameters are used only for 'object' and 'record' cases below.
 
            SourceContext start;
            TYPE type = null;
#if DEBUG
            Enter("Type");
#endif
            // ArrayType = "ARRAY" Length { "," Length } "OF" Type
            // Length    = ConstExpression | "*"

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isArray() )
            {
#if DEBUG
                Enter("Array Type");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();  // 'array' keyword

                // TODO:
                // Array dimensions can be specified either as EXPRESSIONs
                // or as ENUMERATION TYPEs! the latter is not implemented here.

                bool isMath = false;
                bool isSparse = false;

                parseComment(null);
                if (LEXAN.isLeftBrace())
                {
                    if (LEXAN.isMath())
                    {
                        // See, whether next symbol is a comment
                        parseComment(null);
                        LEXAN.expectRightBrace();
                        isMath = true;
                    }

                    else if (LEXAN.isSparse())
                    {
                        // See, whether next symbol is a comment
                        parseComment(null);
                        LEXAN.expectRightBrace();
                        isSparse = true;
                    }

                    else
                    {
                        ERROR.LexemExpected("math");
                    }
                }

                if (isSparse)
                {
                    SPARSE_TYPE sparseType = SPARSE_TYPE.create();

                    bool needOf = true;

                    parseComment(null);
                    if (LEXAN.isStar())
                    {
                        sparseType.dimensions.Add(null);
                        sparseType.isVector = true;
                    }
                    else
                    {
                        ERROR.SyntaxErrorIn("sparse array size specification", "it can be dynamic only");
                        LEXAN.skipUntil(Recovery.Comma_Of_Semicolon);
                    }

                    parseComment(null);
                    if (LEXAN.isComma())
                    {
                        parseComment(null);
                        if (LEXAN.expectStar())
                        {
                            sparseType.dimensions.Add(null);
                            sparseType.isVector = false;
                        }
                    }

                    if (needOf)
                    {
                        if (!LEXAN.expectOf())
                            ERROR.SyntaxErrorIn("sparse array size specification", "sparse arrays can be either one or two dimensional");
                    }

                    TYPE elemType = parseType(null, null, "array element type");

                    sparseType.finalize(elemType);
                    sparseType.setContext(start, LEXAN.getSourceContext());

                    type = sparseType;
                }
                else
                {
                    ARRAY_TYPE arrayType = ARRAY_TYPE.create();

                    arrayType.isMath = isMath;

                    bool needOf = true;
                    bool isStatic = false; //to define whether the declared array is static (=> expressions are used to declare dimensions)
                    bool isDynamic = false; //to define whether the declared array is dynamic (=> stars ['*'] are used to declare dimensions)

                    do
                    {
                        // See, whether next symbol is a comment
                        parseComment(null);
                        if (LEXAN.isStar())
                        {
                            arrayType.dimensions.Add(null);
                            arrayType.isOpen = true;
                            isDynamic = true;
                            if (isStatic) //error: array is not fully dynamic
                            {
                                ERROR.SyntaxErrorIn("array size specification", "an array must be declared as either fully static or fully dynamic");
                            }
                        }
                        else
                        {
                            EXPRESSION expr = parseExpression(null);

                            if (expr == null)
                            {
                                //NG: we suppose that it is not an error and the user just forgot to write one '*';
                                //so, the user has written smth like that: "... array of integer;" and it should be equal to "... array * of integer;"
                                //if it is wrong (so, arrayType.dimensions.Length > 0 or the next lexem is not "Of" then generate a error
                                if (!LEXAN.isOf() || (arrayType.dimensions.Length > 0))
                                {
                                    ERROR.SyntaxErrorIn("array size specification", "incorrect expression");
                                    LEXAN.skipUntil(Recovery.Comma_Of_Semicolon);
                                }
                                else
                                {
                                    needOf = false;
                                }
                            }
                            else
                            {
                                arrayType.dimensions.Add(expr);
                                isStatic = true;
                                if (isDynamic) //error: array is not fully static
                                {
                                    ERROR.SyntaxErrorIn("array size specification", "an array must be declared as either fully static or fully dynamic");
                                }
                            }
                        }
                        // See, whether next symbol is a comment
                        parseComment(null);
                    }
                    while
                        (LEXAN.isComma());

                    // See, whether next symbol is a comment
                    parseComment(null);

                    if (needOf)
                        LEXAN.expectOf();

                    TYPE elemType = parseType(null, null, "array element type");

                    arrayType.finalize(elemType);
                    arrayType.setContext(start, LEXAN.getSourceContext());

                    if (arrayType.dimensions.Length == 0)
                    {
                        arrayType.dimensions.Add(null);
                        arrayType.isOpen = true;
                        ERROR.SyntaxWarningIn("array size specification", "probably you forgot to write '*'");
                        type = null;
                    }

                    type = arrayType;
                }
#if DEBUG
                Exit("Array Type",type!=null);
#endif
            }

            // ObjectType = OBJECT [ PostulatedInterface ]
            else if ( LEXAN.isObject() )
            {
#if DEBUG
                Enter("Object/Interface Type");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isLeftBrace() )
                {
                    type = parsePostulatedInterface();
                }
                else if ( LEXAN.isSemicolon() ) // just OBJECT?
                {
                    LEXAN.backSemicolon();
                    type = STANDARD.Object.type;
                }
                else  // type T = object ... end T;
                {
                    parseObject(id,modifiers);
                    type = null;
                }
				// See, whether next symbol is a comment
				parseComment(null);
                if ( type != null )
                    type.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Object/Interface Type");
#endif
            }

            else if ( LEXAN.isRecord() )
            {
                parseRecord(id,modifiers);
                type = null;
            }

            // An activity type

            else if ( LEXAN.isActivity() )
            {
#if DEBUG
                Enter("Activity Type");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();
                type = parseAbstractActivityType();
                type.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Activity Type");
#endif
            }

            // EnumType = "(" IdentList ")"

            else if ( LEXAN.isLeftParenth() )
            {
#if DEBUG
                Enter("Enumeration Type");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext(); // '('

                IDENT_LIST ids = new IDENT_LIST();

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isRightParenth() )  // Empty enumeration type
                {
                    ERROR.EmtpyEnumType();
                    goto Create;
                }

                do
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    if ( LEXAN.isIdent() )
                    {
                        ids.Add(Scanner.last_id);
                    }
                    else
                    {
                        ERROR.SyntaxErrorIn("enumeration type definition","");
                        LEXAN.skipUntil(Recovery.Comma_RightParenth);
                    }
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while
                    ( LEXAN.isComma() );

				// See, whether next symbol is a comment
				parseComment(null);
                if ( !LEXAN.expectRightParenth() )
                {
                    ERROR.SyntaxErrorIn("enumeration type definition","no right parenthesis");
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.skipUntil(LEXEM.RightParenth);
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.isRightParenth();
                }
            Create:
                type = ENUM_TYPE.create(ids);
                type.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Enumeration Type",type!=null);
#endif
            }

            // ProcedureType = PROCEDURE [ FormalParameters ]

            else if ( LEXAN.isProcedure() )
            {
#if DEBUG
                Enter("Procedure Type");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                PROC_TYPE procedureType = PROC_TYPE.create();

                parseFormalTypes(procedureType);

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isColon() ) procedureType.return_type = parseFormalType(false);
                else                   procedureType.return_type = new VOID_TYPE();

                type = procedureType;
				// See, whether next symbol is a comment
				parseComment(null);
                type.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Procedure Type",type!=null);
#endif
            }

            // Type name...

            else
            {
#if DEBUG
                Enter("Type Name");
#endif
				// See, whether next symbol is a comment
				parseComment(null);

                start = LEXAN.getSourceContext();

                bool isMath = false;

                IDENT_LIST ident = parseQualIdent();
                if ( ident == null )
                {
                    ERROR.NoType(where);
                    LEXAN.skipUntil(Recovery.Semicolon_Colon_RightParenth);  // 'Colon' is unnecessary here
                    type = null;
                }
                else
                {
                    long w = 0;

					// See, whether next symbol is a comment
					parseComment(null);
                    if ( LEXAN.isLeftBrace() )
                    {
                        if (LEXAN.isMath()) {
                            isMath = true;
                            if (!LEXAN.isComma())
                                goto NoWidth;
                        }
                        // Parsing 'width'
                        EXPRESSION width = parseExpression(null);
                        if ( width == null ) // Error
                        {
                            w = 0;
                        }
                        else
                        {
                            object res = width.calculate();
                            if ( res == null )
                            {
                                ERROR.NonConstant(LEXAN.getSourceContext(),"width");
                                w = 0;
                            }
                            else
                                w = (res is long) ? (long)res : (long)(ulong)res;
                        }
                        if (!isMath && LEXAN.isComma())
                            isMath = LEXAN.expectMath();
           NoWidth:
                    //  LEXAN.isIntNumber();
                    //  w = Scanner.integer_value;
						// See, whether next symbol is a comment
						parseComment(null);
                        LEXAN.expectRightBrace();
                    }
                    else
                    {
                        w = 0;
                    }
                    type = TYPE.evaluateTypeName(ident, w);

                    if (type != null) {
                        type.setContext(start, LEXAN.getSourceContext());
                        //if (isMath) {
                        //    TYPE baseType = type;
                        //    type = new MATH_TYPE(type);
                        //    baseType.enclosing = type;
                        //}
                    }
                }
#if DEBUG
                Exit("Type Name",type!=null);
#endif
            }
#if DEBUG
            Exit("Type");
#endif
            return type;
        }

//NG: the followubg function has to be rewritten
//        // parseFormalType
//        // ---------------
//        // FormalType = { ARRAY OF } ( QualIdent | OBJECT [ PostulatedInterface ] )
//        //
//        private TYPE parseFormalType ( bool noName )
//        {
//#if DEBUG
//            Enter("Formal Type");
//#endif
//            // See, whether next symbol is a comment
//            parseComment(null);
//            SourceContext start = LEXAN.getSourceContext();

//            TYPE result    = null;
//            TYPE elem_type = null;
//            int  count = 0;


//            System.Collections.Generic.List<int> dimensions = new System.Collections.Generic.List<int>();

//            while ( true )
//            {
//                // See, whether next symbol is a comment
//                parseComment(null);
//                if ( LEXAN.isArray() )
//                {
//                    if (LEXAN.isStar())
//                    {
//                        while (LEXAN.isComma()) { count++; LEXAN.expectStar(); }
//                    }
//                    count++;
//                    dimensions.Add(count); count = 0;
//                    LEXAN.expectOf();
//                }
//                else
//                    break;
//            }

//            // See, whether next symbol is a comment
//            parseComment(null);
//            if ( LEXAN.isObject() )
//            {
//                // See, whether next symbol is a comment
//                parseComment(null);
//                if ( LEXAN.isLeftBrace() )
//                    elem_type = parsePostulatedInterface();
//                else
//                    elem_type = STANDARD.Object.type;
//            }
//            else
//            {
//                IDENT_LIST ident = parseQualIdent();

//                // Possible error: the form 'p:type' is used instead of 'type'
//                if (noName && (ident!=null) && (ident.Length == 1 && LEXAN.isColon()))
//                {
//                    // This is not a type name but a parameter name
//                    ERROR.ObsoleteParamName(ident[0].Name);
//                    // Parsing the type after colon
//                    ident = parseQualIdent();
//                }

//                long w;

//                // See, whether next symbol is a comment
//                parseComment(null);
//                if (LEXAN.isLeftBrace())
//                {
//                    // Parsing 'width'
//                    EXPRESSION width = parseExpression(null);
//                    if (width == null) // Error
//                    {
//                        w = 0;
//                    }
//                    else
//                    {
//                        object res = width.calculate();
//                        if (res == null)
//                        {
//                            ERROR.NonConstant(LEXAN.getSourceContext(), "width");
//                            w = 0;
//                        }
//                        else
//                            w = (res is long) ? (long)res : (long)(ulong)res;
//                    }
//                    //  LEXAN.isIntNumber();
//                    //  w = Scanner.integer_value;
//                    // See, whether next symbol is a comment
//                    parseComment(null);
//                    LEXAN.expectRightBrace();
//                }
//                else
//                {
//                    w = 0;
//                }

//                elem_type = TYPE.evaluateTypeName(ident,w);
//            }

//            int[] dim = dimensions.ToArray();

//            result = TYPE.evaluateFormalType(dim, elem_type);
//            if ( result != null )
//                result.setContext(start,LEXAN.getSourceContext());
//#if DEBUG
//            Exit("Formal Type",result!=null);
//#endif
//            return result;
//        }

        // parseFormalType
        // ---------------
        // FormalType = { ARRAY OF } ( QualIdent | OBJECT [ PostulatedInterface ] )
        //
        private TYPE parseFormalType(bool noName)
        {
#if DEBUG
            Enter("Formal Type");
#endif
            // See, whether next symbol is a comment
            parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

            TYPE result = null;

            #region parse array
            if (LEXAN.isArray())
            {
                // See, whether next symbol is a comment
                parseComment(null);
                start = LEXAN.getSourceContext();  // 'array' keyword

                bool isMath = false;
                parseComment(null);
                if (LEXAN.isLeftBrace())
                {
                    if (LEXAN.expectMath())
                    {
                        // See, whether next symbol is a comment
                        parseComment(null);
                        LEXAN.expectRightBrace();
                        isMath = true;
                    }
                }

                ARRAY_TYPE arrayType = ARRAY_TYPE.create();
                arrayType.isMath = isMath;

                bool needOf = true;
                bool isStatic = false; //to define whether the declared array is static (=> expressions are used to declare dimensions)
                bool isDynamic = false; //to define whether the declared array is dynamic (=> stars ['*'] are used to declare dimensions)

                do
                {
                    // See, whether next symbol is a comment
                    parseComment(null);
                    if (LEXAN.isStar())
                    {
                        arrayType.dimensions.Add(null);
                        arrayType.isOpen = true;
                        isDynamic = true;
                        if (isStatic) //error: array is not fully dynamic
                        {
                            ERROR.SyntaxErrorIn("array size specification", "an array must be declared as either fully static or fully dynamic");
                        }
                    }
                    else
                    {
                        EXPRESSION expr = parseExpression(null);

                        if (expr == null)
                        {
                            //NG: we suppose that it is not an error and the user just forgot to write one '*';
                            //so, the user has written smth like that: "... array of integer;" and it should be equal to "... array * of integer;"
                            //if it is wrong (so, arrayType.dimensions.Length > 0 or the next lexem is not "Of" then generate a error
                            if (!LEXAN.isOf() || (arrayType.dimensions.Length > 0))
                            {
                                ERROR.SyntaxErrorIn("array size specification", "incorrect expression");
                                LEXAN.skipUntil(Recovery.Comma_Of_Semicolon);
                            }
                            else
                            {
                                needOf = false;
                            }
                        }
                        else
                        {
                            arrayType.dimensions.Add(expr);
                            isStatic = true;
                            if (isDynamic) //error: array is not fully static
                            {
                                ERROR.SyntaxErrorIn("array size specification", "an array must be declared as either fully static or fully dynamic");
                            }
                        }
                    }
                    // See, whether next symbol is a comment
                    parseComment(null);
                }
                while
                    (LEXAN.isComma());

                // See, whether next symbol is a comment
                parseComment(null);

                if (needOf)
                    LEXAN.expectOf();

                TYPE elemType = parseType(null, null, "array element type");

                arrayType.finalize(elemType);
                arrayType.setContext(start, LEXAN.getSourceContext());

                if (arrayType.dimensions.Length == 0)
                {
                    arrayType.dimensions.Add(null);
                    arrayType.isOpen = true;
                    ERROR.SyntaxWarningIn("array size specification", "probably you forgot to write '*'");
                }
                result = arrayType;
            }
            #endregion

            else
            {
                parseComment(null);
                if (LEXAN.isObject())
                {
                    // See, whether next symbol is a comment
                    parseComment(null);
                    if (LEXAN.isLeftBrace())
                        result = parsePostulatedInterface();
                    else
                        result = STANDARD.Object.type;
                }
                else
                {
                    IDENT_LIST ident = parseQualIdent();

                    // Possible error: the form 'p:type' is used instead of 'type'
                    if (noName && (ident != null) && (ident.Length == 1 && LEXAN.isColon()))
                    {
                        // This is not a type name but a parameter name
                        ERROR.ObsoleteParamName(ident[0].Name);
                        // Parsing the type after colon
                        ident = parseQualIdent();
                    }

                    long w;

                    // See, whether next symbol is a comment
                    parseComment(null);
                    if (LEXAN.isLeftBrace())
                    {
                        // Parsing 'width'
                        EXPRESSION width = parseExpression(null);
                        if (width == null) // Error
                        {
                            w = 0;
                        }
                        else
                        {
                            object res = width.calculate();
                            if (res == null)
                            {
                                ERROR.NonConstant(LEXAN.getSourceContext(), "width");
                                w = 0;
                            }
                            else
                                w = (res is long) ? (long)res : (long)(ulong)res;
                        }
                        //  LEXAN.isIntNumber();
                        //  w = Scanner.integer_value;
                        // See, whether next symbol is a comment
                        parseComment(null);
                        LEXAN.expectRightBrace();
                    }
                    else
                    {
                        w = 0;
                    }

                    result = TYPE.evaluateTypeName(ident, w);
                }

                if (result == null)
                    result = parseType(null, null, "");
            }

            if (result != null)
                result.setContext(start, LEXAN.getSourceContext());
#if DEBUG
            Exit("Formal Type", result != null);
#endif
            return result;
        }

        // parsePostulatedInterface
        // ------------------------
        //
        // PostulatedInterface = "{" QualIdent { "," QualIdent } "}"
        //
        // The first "{" has been already parsed.
        //
        public INTERFACE_TYPE parsePostulatedInterface ( )
        {
#if DEBUG
            Enter("Postulated interface");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

            INTERFACE_TYPE interfaceType = INTERFACE_TYPE.create();

            do
            {
                IDENT_LIST qualName = parseQualIdent();
                if ( qualName == null )
                {
                    // Something's wrong with qual-id
                    qualName = new IDENT_LIST();
                    qualName.Add(ERROR.errUnitName);
                }
                interfaceType.addPostulatedDefinition(qualName);
				// See, whether next symbol is a comment
				parseComment(null);
            }
            while
                ( LEXAN.isComma() );

			// See, whether next symbol is a comment
			parseComment(null);
            if ( !LEXAN.expectRightBrace() )
            {
                ERROR.SyntaxErrorIn("postulated interface specification","no right brace");
                LEXAN.skipUntil(Recovery.Comma_RightBrace_Id);
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.isRightBrace();
            }
#if DEBUG
            Exit("Postulated interface");
#endif
            interfaceType.setContext(start,LEXAN.getSourceContext());
            return interfaceType;
        }

        // parseAbstractActivityType
        // -------------------------
        //
        // var a : activity { P };  (* where P is a protocol *)
        //
        private TYPE parseAbstractActivityType ( )
        {
            IDENT_LIST qualName = null;

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isLeftBrace() )
            {
                qualName = parseQualIdent();
                if ( qualName == null )
                {
                    // Something's wrong with qual-id
                    qualName = new IDENT_LIST();
                    qualName.Add(ERROR.errUnitName);
                }
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightBrace();
            }
            return ABSTRACT_ACTIVITY_TYPE.create(qualName);
        }

        // parseFormalParameters
        // ---------------------
        //
        // FormalParameters = "(" [ FPSection { ";" FPSection } ] ")" [ ":" FormalType ].
        //
        // FPSection = [ VAR ] Ident { "," Ident } ":" FormalType.
        //
        // NOTE: The part [ ":" FormalType ] is parsed separately!
        //                ------------------
        //
        // Now this procedure also parses _object_ parameters, so 'unit'
        // can be either of type ROUTINE_DECL or OBJECT_DECL.
        //
        private void parseFormalParameters ( DECLARATION unit )
        {
#if DEBUG
            Enter("Formal Parameters");
#endif
            bool reference_sign;
            int  count = 0;

			// See, whether next symbol is a comment
			parseComment(unit);
            if ( LEXAN.isLeftParenth() )
            {
				// See, whether next symbol is a comment
				parseComment(unit);
                if ( LEXAN.isRightParenth() )
                {
                    // no formal parameters (empty list)
                }
                else
                {
                    do
                    {
                        parseComment(unit);
                        reference_sign = LEXAN.isVar();
                        if ( unit is OBJECT_DECL && ((OBJECT_DECL)unit).modifiers.Value )
                        {
                            ERROR.ValueObjectWithParams();
                        }
                        if ( reference_sign && unit is OBJECT_DECL )
                        {
                            ERROR.VarParamInObject();
                            reference_sign = false;
                        }
                        if (reference_sign && unit is ACTIVITY_DECL)
                        {
                            ERROR.VarParamInActivity();
                            reference_sign = false;
                        }
                        MODIFIERS modifiers = parseModifiers();

                        IDENT_LIST names = new IDENT_LIST();
                        do
                        {
							// See, whether next symbol is a comment
							parseComment(unit);
                            if ( !LEXAN.expectIdent() )
                            {
                                ERROR.SyntaxErrorIn("formal parameters specification","no parameter name after comma");
								// See, whether next symbol is a comment
								parseComment(unit);
                                LEXAN.skipUntil(Recovery.Semicolon_Colon_RightParenth);
                            }
                            names.Add(Scanner.last_id);
                            count++;
							// See, whether next symbol is a comment
							parseComment(unit);
                        }
                        while
                            ( LEXAN.isComma() );

						// See, whether next symbol is a comment
						parseComment(unit);
                        if ( !LEXAN.expectColon() )
                        {
                            ERROR.SyntaxErrorIn("formal parameters specification","no semicolon");
                            LEXAN.skipUntil(Recovery.Semicolon_Colon_RightParenth);
                        }

                        TYPE type = parseFormalType(false);

                        VARIABLE_DECL v = VARIABLE_DECL.create(unit,names,type,reference_sign,modifiers);
						//v.line_number = LEXAN.getSourceContext().StartLine;

						// See, whether next symbol is a comment
						parseComment(unit);
                    }
                    while
                        ( LEXAN.isSemicolon() );

					// See, whether next symbol is a comment
					parseComment(unit);
                    if ( !LEXAN.expectRightParenth() )
                    {
                        ERROR.SyntaxErrorIn("formal parameters specification","no right parenthesis");
                        LEXAN.skipUntil(LEXEM.RightParenth);
                    }
                }
            }
         // else  -- no formal parameters (no list)

            if    ( unit is ROUTINE_DECL )  ((ROUTINE_DECL)unit).paramCount = count;
            else /* unit is OBJECT_DECL */  ((OBJECT_DECL)unit).paramCount = count;
#if DEBUG
            Exit("Formal Parameters");
#endif
            return;
        }

        // parseFormalTypes
        // ----------------
        //
        // FormalTypes = "(" [ FTPSection { ";" FTPSection } ] ")" [ ":" FormalType ].
        //
        // FTPSection = [ VAR ] FormalType { "," FormalType }.
        //
        // NOTE: The part [ ":" FormalType ] is parsed separately!
        //                ------------------
        //
        private void parseFormalTypes ( PROC_TYPE procedure_type )
        {
#if DEBUG
            Enter("Formal Types");
#endif
            bool reference_sign;

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isLeftParenth() )
            {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isRightParenth() )
                {
                    // no formal parameters (empty list)
                }
                else
                {
                    do
                    {
						// See, whether next symbol is a comment
						parseComment(null);
                        reference_sign = LEXAN.isVar();
                        do
                        {
                            TYPE formal_type = parseFormalType(true);
                            TYPE.addFormalTypeOfProcType(procedure_type,formal_type,reference_sign);
							// See, whether next symbol is a comment
							parseComment(null);
                        }
                        while
                            ( LEXAN.isComma() );
						// See, whether next symbol is a comment
						parseComment(null);
                    }
                    while
                        ( LEXAN.isSemicolon() );

					// See, whether next symbol is a comment
					parseComment(null);
                    if ( !LEXAN.expectRightParenth() )
                    {
                        ERROR.SyntaxErrorIn("formal types specification","no right parenthesis");
						// See, whether next symbol is a comment
						parseComment(null);
                        LEXAN.skipUntil(LEXEM.RightParenth);
                    }
                }
            }
         // else  -- no formal parameters (no list)

#if DEBUG
            Exit("Formal Types");
#endif
            return;
        }

        // parseBlockStatement
        // -------------------
        //
        // BlockStatement = BEGIN StatementSequence [ Exception { Exception } ] END.
        //
        // Exception = ON [ QualIdent { "," QualIdent } ] DO StatementSequence
        //           | ON EXCEPTION DO StatementSequence.
        //
        // "BEGIN" keyword has been already parsed.
        //

        private Identifier parseBlockStatement ( NODE enclosing )
        {
#if DEBUG
            Enter("Block Statement");
#endif
            SourceContext start = LEXAN.getSourceContext();

            Identifier final_id = null;
            BLOCK block = BLOCK.create(enclosing);

			// See, whether next symbol is a comment
			parseComment(block);

            MODIFIERS m = parseModifiers();
            block.modifiers = m;

            try
            {
                parseStatementSequence(block,Recovery.EndOfBlockStatement,"no ';', ON, or END");

                int completed = 0;

				// See, whether next symbol is a comment
				parseComment(block);
                while ( LEXAN.isOn() )
                {
					// See, whether next symbol is a comment
					parseComment(block);
                    EXCEPTION exception = EXCEPTION.create(block);

					// See, whether next symbol is a comment
					parseComment(block);

                    if ( LEXAN.isException() )
                    {
                        // This must be the last branch in the exception list...
                        if ( completed > 0 ) ERROR.ExceptionAgain();
                            // Error: 'on exception' comes again or it comes after 'on termination'
                        completed = 1;
                    }
                    else if ( LEXAN.isTermination() )
                    {
                        if ( completed > 1 ) ERROR.TerminationAgain();
                            // Error: 'on termination' comes again
                        completed = 2;
                    }
                    else
                    {
                        // Type name
                        if ( completed > 0 ) { ERROR.ExcTypeAgain(); completed = 0; }

                        IDENT_LIST qualName = parseQualIdent();
                        TYPE exceptionType = TYPE.evaluateTypeName(qualName,0);

                        exception.Add(exceptionType);

                        if ( exception != null )
                        {
                            // Then we have the list of exceptions...
							// See, whether next symbol is a comment
							parseComment(block);
                            while ( LEXAN.isComma() )
                            {
								// See, whether next symbol is a comment
								parseComment(block);
                                qualName = parseQualIdent();
                                exceptionType = TYPE.evaluateTypeName(qualName,0);
                                exception.Add(exceptionType);
                            }
                        }
                    }

					// See, whether next symbol is a comment
					parseComment(block);
                    LEXAN.expectDo();
                    
                    if ( completed == 2 )
                    {
                        parseStatementSequence(block.termination,Recovery.EndOfException,"no ';' or END" );
                    }
                    else
                    {
                        parseStatementSequence(exception,Recovery.EndOfException,"no ';' or END" );
                        block.exceptions.Add(exception);
                    }
                }
            }
            catch ( StructError se )
            {
                final_id = se.last_id;
                goto Finish;
            }

			// See, whether next symbol is a comment
			parseComment(block);
            if ( LEXAN.expectEnd() )
            {
				// See, whether next symbol is a comment
				parseComment(block);
                if ( LEXAN.isIdent() ) final_id = Scanner.last_id;
                else if ( enclosing is OPERATOR_DECL )
                {
                    // Expect 'op-sign'
					// See, whether next symbol is a comment
					parseComment(block);
                    LEXAN.expectString();
                    //final_id = Scanner.last_id;
                    //NG: the previous line didn't worked correctly
                    final_id = new Identifier(Scanner.identifier);
                }
            }
            else
            {
                ERROR.SyntaxErrorIn("block statement","no END");
				// See, whether next symbol is a comment
				parseComment(block);
                LEXAN.skipUntil(LEXEM.End);
            }

        Finish:
            block.setContext(start, LEXAN.getSourceContext());
            block.finalize(enclosing);          
			// See, whether next symbol is a comment
			parseComment(block);
#if DEBUG
            Exit("Block Statement");
#endif
            return final_id;
        }

        private SourceContext checkFinalEnd ( string stmt_name )
        {
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.expectEnd();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext res = LEXAN.getSourceContext();

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isIdent() )
            {
                // This is "end ID" construct; this means we reached
                // the ane of a _compilation_unit_ but not a statement!
                ERROR.WrongBeginEndBalance(stmt_name);
                throw new StructError(Scanner.last_id);
            }
            return res;
        }

        private bool parseStatement ( NODE enclosing )
        {
            SourceContext start;

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isIf() )
            {
#if DEBUG
                Enter("If Statement");
#endif
                IF  if_stmt = IF.create();
                int no;
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                IF_PAIR if_pair = new IF_PAIR(if_stmt);

                // Processing IF-THEN part
                EXPRESSION condition = parseExpression(if_pair);
                if_pair.Add(condition);
                if (condition == null) ERROR.MissingCondition(start);
                if ( !LEXAN.expectThen() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.skipUntil(Recovery.EndOfIfCondition);  // THEN, ELSIF, ELSE, END
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.isThen();
                }

                parseStatementSequence(if_pair,Recovery.EndOfThenCase,"no ';', ELSE, ELSIF, or END");
                no = if_pair.statements.Length;
                if ( no > 0 )
                    if_pair.setContext(if_pair.statements[0].sourceContext,
                                       if_pair.statements[no-1].sourceContext);
                if_stmt.Add(if_pair);

                // Processing ELSIF-THEN parts
				// See, whether next symbol is a comment
				parseComment(null);
                while ( LEXAN.isElsif() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    if_pair = new IF_PAIR(if_stmt);
                    if_pair.Add(parseExpression(if_pair));
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectThen();
                    parseStatementSequence(if_pair,Recovery.EndOfThenCase,"no ';', ELSE, ELSIF, or END");
                    no = if_pair.statements.Length;
                    if ( no > 0 )
                        if_pair.setContext(if_pair.statements[0].sourceContext,
                                           if_pair.statements[no-1].sourceContext);
                    if_stmt.Add(if_pair);
                }

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isElse() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    SourceContext start_else = LEXAN.getSourceContext();

                    if_pair = new IF_PAIR(if_stmt);
                 // if_pair.Add((EXPRESSION)null);  -- not necessary
                    parseStatementSequence(if_pair,Recovery.EndOfElseCase,"no ';' or END");
                    no = if_pair.statements.Length;
                    if ( no > 0 )
                       if_pair.setContext(if_pair.statements[0].sourceContext,
                                          if_pair.statements[no-1].sourceContext);
                    if_stmt.Add(if_pair);
                }
                if_stmt.EndIfContext = checkFinalEnd("'if'"); // LEXAN.expectEnd();
                if_stmt.finalize(enclosing);
                if_stmt.setContext(start,if_stmt.EndIfContext);
                
#if DEBUG
                Exit("If Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isCase() )
            {
#if DEBUG
                Enter("Case Statement");
#endif
                CASE case_stmt = CASE.create();
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                case_stmt.condition = parseExpression(case_stmt);
                if (case_stmt.condition == null) ERROR.MissingCondition(start);
                // See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectOf();

                do
                {
                    CASE_ITEM case_item = new CASE_ITEM(case_stmt);

					// See, whether next symbol is a comment
					parseComment(null);
                    while ( LEXAN.isVert() ) 
					{ 
						// See, whether next symbol is a comment
						parseComment(null);
					}

                    do
                    {
                        RANGE range = new RANGE(case_item);

                        range.left_border = parseExpression(range);
                        if ( range.left_border == null )
                        {
                            ERROR.SyntaxErrorIn("CASE statement","no case label");
                            LEXAN.skipUntil(Recovery.EndOfCaseBranch);
                            break;
                        }
						// See, whether next symbol is a comment
						parseComment(null);
                        if ( LEXAN.isDotDot() )
                            range.right_border = parseExpression(range);

                        case_item.Add(range);
						// See, whether next symbol is a comment
						parseComment(null);
                    }
                    while
                        ( LEXAN.isComma() );

					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectColon();
                    parseStatementSequence(case_item,Recovery.EndOfCaseBranch,"no ';', ELSE or END");

                //  while ( LEXAN.isVert() ) { }

                    case_stmt.Add(case_item);
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while
                    ( LEXAN.isVert() );

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isElse() )
                {
                    CASE_ITEM case_item = new CASE_ITEM(case_stmt);
                    parseStatementSequence(case_item,Recovery.EndOfCase,"no ';' or END");
                    case_stmt.Add(case_item);
                }
                checkFinalEnd("'case'");  // LEXAN.expectEnd();

                case_stmt.finalize(enclosing);
                case_stmt.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Case Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isWhile() )
            {
#if DEBUG
                Enter("While Statement");
#endif
                WHILE while_stmt = WHILE.create();
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                while_stmt.condition = parseExpression(while_stmt);
                if (while_stmt.condition == null) ERROR.MissingCondition(start);
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectDo();
                parseStatementSequence(while_stmt,Recovery.EndOfWhile,"no ';' or END");
                SourceContext final = checkFinalEnd("'while'"); // LEXAN.expectEnd();

                int n = while_stmt.statements.Length;
                if ( n > 0 )
                    while_stmt.statements.setContext(while_stmt.statements[0],while_stmt.statements[n-1]);

                while_stmt.finalize(enclosing);
             // while_stmt.setContext(while_stmt.condition.sourceContext /*start*/,
             //                       n==0 ? final : while_stmt.statements[n-1].sourceContext);
                while_stmt.setContext(start,final);
#if DEBUG
                Exit("While Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isRepeat() )
            {
#if DEBUG
                Enter("Repeat Statement");
#endif
                REPEAT repeat = REPEAT.create();
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                parseStatementSequence(repeat,Recovery.EndOfRepeat,"no ';' or UNTIL");
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectUntil();
                repeat.condition = parseExpression(repeat);
                if (repeat.condition == null) ERROR.MissingCondition(LEXAN.getSourceContext());
                repeat.finalize(enclosing);
                repeat.setContext(start,repeat.condition.sourceContext);
#if DEBUG
                Exit("Repeat Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isLoop() )
            {
#if DEBUG
                Enter("Loop Statement");
#endif
                LOOP loop = LOOP.create();
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                parseStatementSequence(loop,Recovery.EndOfLoop,"no ';' or END");
                checkFinalEnd("'loop'"); // LEXAN.expectEnd();

                loop.finalize(enclosing);
                loop.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Loop Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isFor() )
            {
#if DEBUG
                Enter("For Statement");
#endif
                FOR for_stmt = FOR.create();

				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();
                for_stmt.enclosing = CONTEXT.current;

                for_stmt.forVar = parseDesignator();  
                
				// See, whether next symbol is a comment
				parseComment(null);
                SourceContext var = LEXAN.getSourceContext();

                if (for_stmt.forVar != null)
                {
                    for_stmt.forVar.enclosing = for_stmt;
                    // for_stmt.forVar.modifiers -- TO SET
                    // for_stmt.forVar.name -- already set
                }
                else
                {
                    ERROR.SyntaxErrorIn("for statement", "interation variable missing");
                }

             // for_stmt.setContext(var); -- WHY??
             // for_stmt.forVar.type -- will be set in evaluateFor()

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.expectAssign() )
                {
                    for_stmt.from = parseExpression(for_stmt);
					// See, whether next symbol is a comment
                    if (for_stmt.from == null) ERROR.SyntaxErrorIn("for cycle", "initial value for iteration variable is missing");
                    parseComment(null);
                    LEXAN.expectTo();
                    for_stmt.to = parseExpression(for_stmt);
                    if (for_stmt.to == null) ERROR.SyntaxErrorIn("for cycle", "final value for iteration variable is missing");
					// See, whether next symbol is a comment
					parseComment(null);
                    if (LEXAN.isBy())
                    {
                        for_stmt.by = parseExpression(for_stmt);
                        if (for_stmt.by == null) ERROR.SyntaxErrorIn("for cycle", "interation step is missing");
                    }
                }
                else  // Diagnostics is already issued
                    LEXAN.skipUntil(LEXEM.Do);

				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectDo();

             // CONTEXT.enter(for_stmt);
                parseStatementSequence(for_stmt,Recovery.EndOfFor,"no ';' or END");
             // CONTEXT.exit();

                checkFinalEnd("'for'"); // LEXAN.expectEnd();

                for_stmt.finalize(enclosing);
                for_stmt.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("For Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isExit() )
            {
#if DEBUG
                Enter("Exit Statement");
#endif
                EXIT exit = EXIT.create(enclosing);
				// See, whether next symbol is a comment
				parseComment(null);
                exit.setContext(LEXAN.getSourceContext());
#if DEBUG
                Exit("Exit Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isReturn() )
            {
#if DEBUG
                Enter("Return Statement");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();

                if ( CONTEXT.current_routine is ACTIVITY_DECL )
                {
                    REPLY reply_stmt = REPLY.create();
                    SourceContext finish = start;

                    do
                    {
                        
                        EXPRESSION expr = parseExpression(reply_stmt);
                        if (expr != null)
                        {
                            reply_stmt.values_to_reply.Add(expr);
                            finish = expr.sourceContext;
                        }
						// See, whether next symbol is a comment
						parseComment(null);
                    }
                    while ( LEXAN.isComma() );

                    reply_stmt.finalize(enclosing);
                    reply_stmt.setContext(start,finish);
                }
                else // conventional return
                {
                    RETURN return_stmt = RETURN.create();
                    return_stmt.return_value = parseExpression(return_stmt);
                    return_stmt.finalize(enclosing);
                    return_stmt.setContext(start,return_stmt.return_value==null 
                                                               ? LEXAN.getSourceContext()
                                                               : return_stmt.return_value.sourceContext);
                }
#if DEBUG
                Exit("Return Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isAwait() )
            {
#if DEBUG
                Enter("Await Statement");
#endif
                AWAIT await = AWAIT.create();
				// See, whether next symbol is a comment
				parseComment(null);
                enclosingAwait = await; // We entered await statement;
                start = LEXAN.getSourceContext();

                await.val = parseExpression(await);
                if (await.val == null) ERROR.SyntaxErrorIn("await", "condition is missing");
                await.finalize(enclosing);
                await.setContext(start,LEXAN.getSourceContext());
                enclosingAwait = null; // We exited await statement;
#if DEBUG
                Exit("Await Statement");
#endif
                goto Skip_Semicolons;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isNew() )
            {
#if DEBUG
                Enter("Activity Launch statement");
#endif
				// See, whether next symbol is a comment
				parseComment(null);
                start = LEXAN.getSourceContext();
                
				// int line_number = LEXAN.getSourceContext().StartLine;
                NEW n = parseNew();

                LAUNCH launch = LAUNCH.create(n);
                
                launch.finalize(enclosing);
                launch.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Activity Launch statement");
#endif
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isActivity() )
            {
#if DEBUG
                Enter("Anonymous Activity statement");
#endif
                ERROR.NotImplemented("anonymous activities are");
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.skipUntil(LEXEM.End);
                LEXAN.isEnd();

             // send.finalize(enclosing);
             // send.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Anonymous Activity statement");
#endif
            }
/****
            if ( LEXAN.isSend() )
            {
#if DEBUG
                Enter("Send statement");
#endif
                start = LEXAN.getSourceContext();

                EXPRESSION toSend = parseExpression(null);
                DESIGNATOR act = null;
                if ( LEXAN.isTo() ) act = parseDesignator();

                SEND send = SEND.create(act,toSend);

                send.finalize(enclosing);
                send.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Send statement");
#endif
            }

            if ( LEXAN.isReceive() )
            {
#if DEBUG
                Enter("Receive statement");
#endif
                DESIGNATOR act = null;
                DESIGNATOR var = null;

                start = LEXAN.getSourceContext();

             // DESIGNATOR firstElem = parseDesignator();
             // if ( firstElem is INSTANCE && ((INSTANCE)firstElem).entity is ACTIVITY_DECL )
             // {
             //     act = (DESIGNATOR)firstElem;
             //     LEXAN.expectArrow();
             //     var = parseDesignator();
             // }
             // else
             //     var = firstElem;

                var = parseDesignator();
                if ( LEXAN.isFrom() ) act = parseDesignator();

                RECEIVE receive = RECEIVE.create(false,act,var);
                receive.finalize(enclosing);
                receive.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Receive statement");
#endif
            }
***/
			// See, whether next symbol is a comment
			parseComment(null);
            start = LEXAN.getSourceContext();
            if ( LEXAN.isAccept() )
            {
#if DEBUG
                Enter("Accept statement");
#endif
                ACCEPT accept = ACCEPT.create();
				// See, whether next symbol is a comment
				parseComment(null);                
                do 
                {
                    DESIGNATOR dsg = parseDesignator();
                    if ( dsg != null ) accept.designators.Add(dsg);
                    else ERROR.SyntaxErrorIn("accept", "variable is missing");
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while ( LEXAN.isComma() );

                accept.finalize(enclosing);
                accept.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Accept statement");
#endif
            }
/***
            if ( LEXAN.isLaunch() )
            {
#if DEBUG
                Enter("Launch statement");
#endif
                LAUNCH launch = LAUNCH.create();
                start = LEXAN.getSourceContext();

                if ( !LEXAN.isNil())
                    parseStatement(launch);

                launch.finalize(enclosing);
                launch.setContext(start,LEXAN.getSourceContext());
#if DEBUG
                Exit("Launch statement");
#endif
            }
***/
			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isDo() ) //  isBegin() )
            {
#if DEBUG
                Enter("Block Statement");
#endif
                Identifier id = parseBlockStatement(enclosing);
                if ( id != null )
                {
                    ERROR.WrongBeginEndBalance("block");
                    throw new StructError(id);
                }
#if DEBUG
                Exit("Block Statement");
#endif
                goto Skip_Semicolons;
            }

         // else  -- assignment OR call OR nothing
#if DEBUG
            Enter("Assignment/Call Statement");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            start = LEXAN.getSourceContext();

            DESIGNATOR designator = parseDesignator();

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isAssign() )
            {
                if (LEXAN.isLeftBracket())
                {
                    EXPR_ARRAY_ASSIGNMENT matrixAssg = EXPR_ARRAY_ASSIGNMENT.create();

                    //TODO : PARSE
                    indexList = new System.Collections.Generic.List<int>();

                    parseMatrixRow(matrixAssg, 0);
                    matrixAssg.finalize(enclosing, designator);
                    matrixAssg.setContext(start, LEXAN.getSourceContext());
                }
                else
                {
                    ASSIGNMENT assignment = ASSIGNMENT.create();
                    EXPRESSION right = parseExpression(assignment);

                    if (right == null)
                        ERROR.SyntaxErrorIn("assignment statement", "errorneous right part");
                    else
                    {
                        //we have to check, maybe it's array_range
                        if (LEXAN.isDotDot())
                        {
                            EXPRESSION from = right;
                            right = new ARRAY_RANGE();
                            ((ARRAY_RANGE)right).from = from;

                            EXPRESSION to = parseExpression(enclosing);
                            if (to != null)
                            {
                                ((ARRAY_RANGE)right).to = to;
                                ((ARRAY_RANGE)right).wasToWritten = true;
                            }
                            else
                            {
                                //ERROR because it's assignment, right border has to be specified
                                ERROR.SyntaxErrorIn("assignment statement", "range right border is not specified");
                            }
                            parseComment(null);
                            if (LEXAN.isBy())
                            {
                                ((ARRAY_RANGE)right).by = parseExpression(enclosing);
                            }

                            ((ARRAY_RANGE)right).finalize();
                        }

                        assignment.finalize(enclosing, designator, right);
                        assignment.setContext(start, right.sourceContext);
                    }
                }
            }
            else if ( LEXAN.isEqual() )
            {
                ERROR.IllegalAssignmentSign();
                LEXAN.skipUntil(Recovery.EndOfStatement);
            }
            else if ( LEXAN.isComma() )
            {
                // Multiple assignment:   x, y, z := expr1, expr2, expr3;
                // or receive:            x, y := a();
                // or send/receive:       x, y := a(expr1, expr2, expr3);
                //
                //          where 'a' is an activity instance.

                SourceContext finish = LEXAN.getSourceContext();

                EXPRESSION_LIST leftParts = new EXPRESSION_LIST();
                leftParts.Add(designator);

                do
                {
                    designator = parseDesignator();
                    if ( designator == null )
                    {
                        ERROR.SyntaxErrorIn("multiple assignment","errorneous left part");
                        LEXAN.skipUntil(Recovery.Comma_Assignment);
                        continue;
                    }
                    leftParts.Add(designator);
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while ( LEXAN.isComma() );

				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectAssign();

                EXPRESSION_LIST rightParts = new EXPRESSION_LIST();
                do
                {
                    EXPRESSION right = parseExpression(null);  // argument is actually not used!..
                    if ( right == null )
                    {
                        ERROR.SyntaxErrorIn("multiple assignment","errorneous right part");
                        LEXAN.skipUntil(Recovery.Comma_Semicolon_End_Else);
                        continue;
                    }
                    rightParts.Add(right);
                    finish = right.sourceContext;
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while ( LEXAN.isComma() );

                // Create final node(s)

                if ( leftParts.Length == rightParts.Length )
                {
                    // This is a usual multiple assignment;
                    // just split it into a number of single assignments.

                    NODE routine = CONTEXT.current_routine;
                    if ( routine == null ) routine = CONTEXT.current_unit;

                    ArrayList list = new ArrayList();

                    for ( int j=0, m=leftParts.Length; j<m; j++ )
                    {
                        TYPE localType = leftParts[j].type;
                        IDENT_LIST ident = new IDENT_LIST();
                        ident.Add(Identifier.For("_temp_" + CONTEXT.last_temp_no.ToString()));

                        VARIABLE_DECL local = VARIABLE_DECL.create(routine,ident,localType,false,null);
                        list.Add(local);
                    }

                    for ( int k=0, l=leftParts.Length; k<l; k++ )
                    {
                        ASSIGNMENT assignment = ASSIGNMENT.create();
                        INSTANCE temp = INSTANCE.create((VARIABLE_DECL)list[k]);
                        assignment.finalize(enclosing,temp,rightParts[k]);
                        assignment.setContext(start,finish);
                    }

                    for ( int i=0, n=leftParts.Length; i<n; i++ )
                    {
                        ASSIGNMENT assignment = ASSIGNMENT.create();
                        INSTANCE temp = INSTANCE.create((VARIABLE_DECL)list[i]);
                        assignment.finalize(enclosing,(DESIGNATOR)leftParts[i],temp);
                        assignment.setContext(start,finish);
                    }
                }
                else if ( rightParts.Length == 1 )
                {
                    // This is a receive or send/receive statement

                    SEND_RECEIVE sr = SEND_RECEIVE.create(leftParts,rightParts[0]);
                    sr.finalize(enclosing);
                    sr.setContext(start,finish);
                }
                else
                {
                    ERROR.SyntaxErrorIn("multiple assignment","non-equal numbers of left and right parts");
                }
            }
            else if ( designator == null )
            {
                goto Skip_Semicolons;
            }
            else
            {
                // 'designator' already contains a call
                CALL_STMT call = CALL_STMT.create(enclosing,designator);
                call.setContext(start,LEXAN.getSourceContext());
            }
#if DEBUG
            Exit("Assignment/Call Statement");
#endif
        Skip_Semicolons:

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.checkEndOfStatement() || LEXAN.isEOF() )
            {
                // END, ELSE, ELSIF, UNTIL, or ON keyword;
                // Statements are really over.
                return false;
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isSemicolon() )
            {
                while ( LEXAN.isSemicolon() ) { };
                return true;
            }

            // No ANY statement finalizer!
            ERROR.SyntaxErrorIn("statement","no statement finalizer");
            LEXAN.skipUntil(Recovery.EndOfStatement);

            while ( LEXAN.isSemicolon() ) 
			{ 
				// See, whether next symbol is a comment
				parseComment(null);
			}  // just eats semicolons if any
            return true;  // In order for 'parseStatementSequence' to repeat the attempt
        }

        // parseStatementSequence
        // ----------------------
        //
        private void parseStatementSequence ( NODE enclosing, BitArray finalizers, string reason )
        {
#if DEBUG
            Enter("Statement Sequence");
#endif
            while ( parseStatement(enclosing) )
            {
                // Empty body
            }
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.checkFinalizer(finalizers,"statement sequence",reason);
#if DEBUG
            Exit("Statement Sequence");
#endif
        }

        // parseActivity
        // -------------
        //
        //
        private void parseActivity ( NODE enclosing )
        {
#if DEBUG
            Enter("Activity");
#endif
            SourceContext start = LEXAN.getSourceContext();
			//int line_number = LEXAN.getSourceContext().StartLine;

            // This is an activity declaration:
            //
            //     activity Name;
            //         Declarations
            //     begin
            //         Statements
            //     end Name;

            MODIFIERS modifiers  = parseModifiers();
            Identifier ident = null;

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.expectIdent() )
            {
                ident = Scanner.last_id;
            }
            else
            {
                ERROR.NoUnitName("activity");
                LEXAN.skipUntil(Recovery.LeftParenth_Id);  // Recovery.AfterProcedureName
                ident = ERROR.errUnitName;
            }

            // Creating context for the new activity,
            // creating initial tree for the activity itself.
            ACTIVITY_DECL activity = ACTIVITY_DECL.create(ident,modifiers);

            parseFormalParameters(activity);

			// See, whether next symbol is a comment
			parseComment(activity);
            if ( LEXAN.isColon() ) 
            {
                ERROR.SyntaxErrorIn("activity","cannot specify return type");
                // Just to keep the parsing process...
                parseFormalType(false);
            }
            activity.inParams = false;

            parseImplementationClause(activity,Recovery.Semicolon_Type_Const_Var_Procedure_Begin_End);

            if ( activity.enclosing is DEFINITION_DECL ) goto Finish;
            // Activities in definitions do not have bodies

			// See, whether next symbol is a comment
			parseComment(activity);
            if ( !LEXAN.expectSemicolon() )
            {
                LEXAN.skipUntil(LEXEM.Semicolon);
				// See, whether next symbol is a comment
				parseComment(activity);
                LEXAN.isSemicolon();
            }
                
            // Activity: local declarations and body

            parseDeclarations(activity);

			// See, whether next symbol is a comment
			parseComment(activity);
            LEXAN.expectBegin();
            activity.inBody = true;

            Identifier id_final = parseBlockStatement(activity);

            if ( id_final == null )
                ERROR.NoFinalIdentifier("activity",activity.name.Name);
            else  // id_final != null
            {
                // Compare final name with the procedure name
            }
        Finish:
         // LEXAN.expectSemicolon(); -- will be done after exiting
            activity.setContext(start,LEXAN.getSourceContext());
            CONTEXT.exit();
#if DEBUG
            Exit("Activity");
#endif
        }

        // parseOperator
        // -------------
        //
        private void parseOperator ( NODE unit )
        {
#if DEBUG
            Enter("Operator");
#endif
            if ( unit is ROUTINE_DECL )
                ERROR.NotImplemented("nested operators are");
			
			//int line_number = LEXAN.getSourceContext().StartLine;

			// See, whether next symbol is a comment
			parseComment(null);

            SourceContext start = LEXAN.getSourceContext();

            MODIFIERS  modifiers = parseModifiers();
            string     code = null;

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isString() )
            {
                code = Scanner.identifier;
            }
            else
            {
                ERROR.NoUnitName("operator");
                LEXAN.skipUntil(Recovery.LeftParenth_Id);  // Recovery.AfterProcedureName
                code = "+++";
            }

            // Creating context for the new operator,
            // creating initial tree for the operator itself.
            OPERATOR_DECL operatorr = OPERATOR_DECL.create(code,modifiers);

            parseFormalParameters(operatorr);

			// See, whether next symbol is a comment
			parseComment(operatorr);
            if ( LEXAN.isColon() ) 
            {
                operatorr.return_type = parseFormalType(false);
                if ( code == ":=" )
                {
                    ERROR.AssignmentWithReturn();
                    operatorr.return_type = new VOID_TYPE();
                }
            }
            else
            {
                if ( code != ":=" ) ERROR.OperatorShouldReturn();
                operatorr.return_type = new VOID_TYPE();
            }
            operatorr.inParams = false;

            if ( !(operatorr.enclosing is MODULE_DECL) )
                // Operators can appear only in modules
                ERROR.WrongPlaceForOperator();

            if ( operatorr.enclosing is DEFINITION_DECL )
                // Assume this is only operator header...
                goto Finish;

			// See, whether next symbol is a comment
			parseComment(operatorr);
            LEXAN.expectSemicolon();

            // Procedure body
            parseDeclarations(operatorr);

			// See, whether next symbol is a comment
			parseComment(operatorr);
            LEXAN.expectBegin();
            operatorr.inBody = true;
            Identifier code_final = parseBlockStatement(operatorr);

            if ( code_final == null )
            {
                ERROR.NoFinalIdentifier("operator",operatorr.code);
            }
            else // code_final != null
            {
                // Compare final name with the operator name
                if (operatorr.code != code_final.Name)
                {
                    ERROR.NoFinalIdentifier("operator", operatorr.code);
                }
            }

          Finish:
         // LEXAN.expectSemicolon(); -- will be done after exiting
            // Perhaps, something like 'evaluateOperator' is necessary here...
            operatorr.setContext(start,LEXAN.getSourceContext());
            CONTEXT.exit();
#if DEBUG
            Exit("Operator");
#endif
        }

        // parseQualIdent
        // --------------
        // QualIdent = ident { "." ident }
        //
        private IDENT_LIST parseQualIdent ( )
        {
            bool result = false;
            bool first = true;

			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.isEOF();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext startContext = LEXAN.getSourceContext();
            SourceContext dotContext   = LEXAN.getSourceContext();
            IDENT_LIST ids = new IDENT_LIST();
            if (LEXAN.isUnused()) //Special type of variable
            {
                ids.Add(new Identifier("#unused", LEXAN.getSourceContext()));
                return ids;
            }
#if DEBUG
            Enter("QualIdent");
#endif
            do {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isIdent() )
                {
                    if (Scanner.last_id == null || Scanner.last_id.Name == "")
                    {
                        // To prevent crash of CCI part
                        sink = null;
                    }
                    result = true;
                    if ( sink != null )
                    {
                        if (Scanner.last_id.SourceContext.StartLine > 1 &&
                               Scanner.last_id.SourceContext.StartColumn > 1)
                        {
                            Identifier idf = Identifier.For(Scanner.last_id.Name);
                            idf.SourceContext = Scanner.last_id.SourceContext;
                            if (first)
                            {
                                sink.StartName(idf);
                                first = false;

                            }
                            else
                            {
                                // This means we have got '.' before this Id                                
                                sink.QualifyName(dotContext, idf);
                            }
                        }
                    }
                    ids.Add(Scanner.last_id);
                }
                else if ( LEXAN.isString() )
                {
                    // Perhaps, full form of operator call:
                    // module."+"
                    ids.Add(Identifier.For(Scanner.identifier));
                }
                else
                {
                    if ( result )  // ERROR: "." without 'ident' after it
                        ERROR.SyntaxErrorIn("qualified identifier","");
                    ids = null;
                    goto Finish;
                }
                // Assume we will have '.' after Id...
                dotContext = LEXAN.getSourceContext();
				// See, whether next symbol is a comment
				parseComment(null);
            }
            while
                ( LEXAN.isDot() );

         Finish:
            if ( ids != null )
                ids.setContext(startContext,dotContext);
#if DEBUG
            Exit("QualIdent",result);
#endif
            
            return ids;
        }

        static System.Collections.Generic.List<int> indexList;

        private void parseMatrixRow(EXPR_ARRAY_ASSIGNMENT matrixAssig, int depth)
        {

            if (matrixAssig.dimensions.Count <= depth)
            {
                matrixAssig.dimensions.Insert(depth, 0);
            }

            int counter = 0;
            if (indexList.Count <= depth) indexList.Add(0); else indexList[depth] = 0;
            do
            {
                if (LEXAN.isLeftBracket())
                {
                    parseMatrixRow(matrixAssig, depth + 1);
                }
                else
                {
                    EXPRESSION expr = parseExpression(matrixAssig);
                    if (expr != null)
                    {
                        EXPR_ARRAY_ASSIGNMENT.EXPR_ARRAY_ITEM mati = new EXPR_ARRAY_ASSIGNMENT.EXPR_ARRAY_ITEM();
                        mati.expr = expr;
                        mati.indexList = new System.Collections.Generic.List<int>();
                        foreach (int i in indexList) mati.indexList.Add(i);
                        matrixAssig.right_part.Add(mati);
                    }
                    else
                    {
                        ERROR.SyntaxErrorIn("Expression Array Assignment", "Expression expected");
                    }
                }
                indexList[depth]++;
                counter++;

            } while (LEXAN.isComma());

            if (matrixAssig.dimensions[depth] == 0)
            {
                matrixAssig.dimensions[depth] = counter;
            }
            else if (counter != matrixAssig.dimensions[depth])
            {
                ERROR.SyntaxErrorIn("Expression Array Assignment", "Different number of elements in one dimension");
            }

            if (!LEXAN.isRightBracket())
                ERROR.SyntaxErrorIn("] is expected!", "");

        }

        // parseExpression
        // ---------------
        //
        private EXPRESSION parseExpression ( NODE enclosing )
        {
#if DEBUG
            Enter("Expression");
#endif
            EXPRESSION expr = parseSimpleExpression();            
            BINARY rel = parseRelation();            
            if ( rel != null )
            {
                rel.left_operand = expr;
                rel.right_operand = parseSimpleExpression();
                if (rel.right_operand == null) ERROR.SyntaxErrorIn("relation", "missing right operand for " + rel.code);
                rel.setContext(expr,rel.right_operand);
                expr = rel;
            }

            if( expr != null ) expr.enclosing = enclosing;
#if DEBUG
            Exit("Expression", expr!=null);
#endif
            return expr;
        }

        // parseSimpleExpression
        // ---------------------
        //
        private EXPRESSION parseSimpleExpression ( )
        {
#if DEBUG
            Enter("SimpleExpression");
#endif
            UNARY unary;
            EXPRESSION term;

			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.step();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

         // OPERATOR_DECL op = LEXAN.isUserDefined(1);
         // if      ( op != null )       unary = new USER_UNARY(op);
         // else 
			// See, whether next symbol is a comment
			parseComment(null);
            if      ( LEXAN.isPlus() )   unary = new UNARY_PLUS();
            else if ( LEXAN.isMinus() )  unary = new UNARY_MINUS();
            else                         unary = null;

            term = parseTerm(unary);
         // if ( unary != null )
         // {
         //     unary.operand = term;
         //     term = unary;
         // }

            while ( true )
            {
                BINARY bin = parseAddOperator();
                if ( bin == null ) break;

                bin.left_operand = term;
                bin.right_operand = parseTerm(null);
                if (bin.right_operand == null) ERROR.SyntaxErrorIn("expression", "missing operand for " + bin.code);
                bin.setContext(term,bin.right_operand);
                term = bin;
            }

         // if ( term != null )
         //     term.setContext(start,LEXAN.getSourceContext());
#if DEBUG
            Exit("SimpleExpression",term!=null);
#endif
            return term;
        }

        // parseTerm
        // ---------
        //
        private EXPRESSION parseTerm ( UNARY unary )
        {
#if DEBUG
            Enter("Term");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.step();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = unary!=null ? unary.sourceContext : LEXAN.getSourceContext();

            EXPRESSION factor = parseExpoFactor();

            if ( unary != null )
            {
                unary.operand = factor;
                if (factor == null) ERROR.SyntaxErrorIn("expression", "missing operand for unary " + unary.code);
                unary.setContext(unary.sourceContext,factor.sourceContext);

                factor = unary;
            }

            while ( true )
            {
                BINARY bin = parseMulOperator();
                if ( bin == null ) break;

                bin.left_operand = factor;
                bin.right_operand = parseExpoFactor();
                if (bin.right_operand == null) ERROR.SyntaxErrorIn("expression", "missing right operand for " + bin.code);
                bin.setContext(factor,bin.right_operand);

                factor = bin;
            }
#if DEBUG
            Exit("Term",factor!=null);
#endif
            return factor;
        }

        private EXPRESSION parseExpoFactor ( )
        {
            EXPRESSION bas = parseFactor();

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isExponent() )
            {
                EXPRESSION expo = parseFactor();

                if ( expo == null )
                {
                    ERROR.SyntaxErrorIn("exponential operator","wrong exponent");
                    return bas;
                }

                EXPONENT res = new EXPONENT();
                res.left_operand = bas;
                res.right_operand = expo;
                res.setContext(bas,expo);
                return res;
            }            
            else if (LEXAN.isApostrophe())
            {

                TRANSPOSE res = new TRANSPOSE();
                res.operand = bas;
                res.setContext(LEXAN.getSourceContext());
                return res;
            }
            return bas;
        }

        // parseFactor
        // -----------
        //
        private EXPRESSION parseFactor ( )
        {
            EXPRESSION expr;
#if DEBUG
            Enter("Factor");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.step();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

         // OPERATOR_DECL op = LEXAN.isUserDefined(0);
         // if ( op != null )
         // {
         //     USER_UNARY un = new USER_UNARY(op);
         //     un.operand = parseFactor();
         //     expr = un;
         // }
         // else
			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isTilde() )
            {
                NEGATION negation = new NEGATION();
                negation.operand = parseFactor();  // recursive call...
                if (negation.operand == null) ERROR.SyntaxErrorIn("expression", "missing operand for negation");
                negation.setContext(start,negation.operand.sourceContext);

                expr = negation;
            }
            else if (LEXAN.isMinus())
            {
                UNARY_MINUS unaryminus = new UNARY_MINUS();
                unaryminus.operand = parseFactor();  // recursive call...
                unaryminus.setContext(start, unaryminus.operand.sourceContext);

                expr = unaryminus;
            }
            else if (LEXAN.isExclamation())
            {
                TRANSPOSE transp = new TRANSPOSE();
                transp.operand = parseFactor();  // recursive call...
                transp.setContext(start, transp.operand.sourceContext);

                expr = transp;
            }
            else if ( LEXAN.isIntNumber() )
            {
                long val   = Scanner.integer_value;
                long width = -1;

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isLeftBrace() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectInt();
                    width = Scanner.integer_value;
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectRightBrace();
                }
                expr = INTEGER_LITERAL.create(val,(int)width);
                expr.setContext(start);
            }
            else if ( LEXAN.isRealNumber() )
            {
                double val   = Scanner.real_value;
                long   width = -1;

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isLeftBrace() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectInt();
                    width = Scanner.integer_value;
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectRightBrace();
                }
                expr = REAL_LITERAL.create(val,(int)width);
                expr.setContext(start);
            }
            else if ( LEXAN.isString() )
            {
                string str = Scanner.identifier;
                long width = 16;

                expr = null;

				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isLeftBrace() )
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectInt();
                    width = Scanner.integer_value;
					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectRightBrace();

                    if ( str.Length > 1 )
                        ERROR.SuperfluousSize("STRING");
                    else
                        expr = CHAR_LITERAL.create(str[0],width);
                }
				if ( expr == null )
				{ 
					expr = STRING_LITERAL.create(str);
				}
                expr.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isNil() )        
            {
                expr = NULL.create();
                expr.setContext(start);
            }
            else if ( LEXAN.isNew() )        
            {
                expr = parseNew();
                expr.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isLeftBrace() )
            {
                // Set constructor
                SET_CTOR ctor = new SET_CTOR();
                do
                {
                    RANGE range = new RANGE(null);
                    range.left_border = parseExpression(null);
					// See, whether next symbol is a comment
					parseComment(null);
                    if (LEXAN.isDotDot())
                    {
                        range.right_border = parseExpression(null);
                        if (range.right_border == null) ERROR.SyntaxErrorIn("expression", "right border for set is missing");
                    }
                    ctor.elements.Add(range);
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while
                    ( LEXAN.isComma() );

				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightBrace();

                expr = ctor;
                expr.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isLeftParenth() )
            {
                // Subexpression
                expr = parseExpression(null);
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightParenth();
            }
            else
            {
                DESIGNATOR designator = parseDesignator();
                // we can have conversion here with explicit width
                if(LEXAN.isLeftBrace())
                {
                    long w;
                    // Parsing 'width'
                    EXPRESSION width = parseExpression(null);
                    if (width == null) // Error
                    {
                        w = 0;
                        ERROR.SyntaxErrorIn("width cast", "value is missing");
                    }
                    else
                    {
                        object res = width.calculate();
                        if (res == null)
                        {
                            ERROR.NonConstant(LEXAN.getSourceContext(), "width");
                            w = 0;
                        }
                        else
                            w = (res is long) ? (long)res : (long)(ulong)res;
                    }
                    //  LEXAN.isIntNumber();
                    //  w = Scanner.integer_value;

                    LEXAN.expectRightBrace();
                    if (LEXAN.isLeftParenth())
                    {
                        // This is the case like
                        //  char{3} (ch)
                        //   |   |    |
                        // type width argument
                        TYPE targetType = TYPE.evaluateTypeName(designator.full_name, w);
                        EXPRESSION operand = parseExpression(designator);
                        if (operand != null)
                            expr = new TYPE_CONV(operand, targetType);
                        else
                            expr = designator; // For nice error processing
                        LEXAN.expectRightParenth();
                    }
                    else
                    {
                        // TODO: This is a really stange case. 
                        // ---------------
                        // This is the case like
                        //      ch {3}
                        //      |    \    
                        // argument width 
                        // type remains the same as of argument
                        TYPE targetType = designator.type;

                        if (designator.type != null)
                        {
                            if (designator.type.name != null)
                                targetType = TYPE.evaluateTypeName(designator.type.name, w);
                            else
                                targetType = TYPE.evaluateTypeName(designator.type.enclosing.name, w);
                        }
                        else
                            ERROR.SyntaxErrorIn("expression", "wrong use of type size changer"); //TODO: change this to something
                        // more accurate. The reason is that we can't apply {} to the type
                        // of the argument
                        expr = new TYPE_CONV(designator, targetType);
                    }
                }else{
                    expr = TYPE_CONV.checkTypeConversion(designator);
                }
            }


         // if ( LEXAN.isLeftBrace() )
         // {
         //     TYPE targetType = parseType("type cast");
         //     expr = new TYPE_CONV(expr,targetType);
         //     LEXAN.expectRightBrace();
         // }
#if DEBUG
            Exit("Factor",expr!=null);
#endif
            return expr;
        }

        // parseDesignator
        // ---------------
        // Designator together with SIGNATURE & ACTUALS.
        //
        private DESIGNATOR parseDesignator ( )
        {
#if DEBUG
            Enter("Designator");
#endif
            LEXAN.isEOF();
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

            IDENT_LIST qualName   = parseQualIdent();
            DESIGNATOR designator = null;

            if ( qualName == null )
            {
                // Perhaps, SELF?
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isSelf() )
                {
                    designator = SELF.create();
                    designator.setContext(start);
                }
                else if (LEXAN.isObject())
                {
                    designator = OBJECT.create();
                    designator.setContext(start);
                }
             // else
             //     -- designator remains null
            }
            else
            {
                designator = SELECTOR.processQualName(null,qualName);
				if ( designator != null ) 
				{
                    if (designator is INSTANCE)
                    {
                        DECLARATION decl = (designator as INSTANCE).entity;
                        if (decl is VARIABLE_DECL)
                        {
                            ((decl as VARIABLE_DECL).usedInAwait) |=  enclosingAwait != null;
                        }                
                    }
                    designator.setContext(qualName);
				}
            }

            while ( true )
            {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isCaret() )
                {
#if DEBUG
                    Enter("Dereference");
#endif
                    DEREFERENCE deref = new DEREFERENCE();
                    deref.pointer = designator;
                    deref.setContext(start,LEXAN.getSourceContext());

                    designator = deref;
#if DEBUG
                    Exit("Dereference",designator!=null);
#endif
                }
                else if ( LEXAN.isLeftBracket() )
                {
#if DEBUG
                    Enter("Indexer");
#endif
                    INDEXER indexer = new INDEXER();
                    indexer.left_part = designator;
                    do
                    {   
                        if (LEXAN.isDotDot())
                        {
                            ARRAY_RANGE range = new ARRAY_RANGE();
                            EXPRESSION to = parseExpression(designator);
                            if (to != null) { range.to = to; range.wasToWritten = true; }
                            //if (!(LEXAN.isRightBracket() || LEXAN.isComma()))
                            //{
                            //    range.to = parseExpression(designator);
                            //}
                            parseComment(null);
                            if (LEXAN.isBy())
                            {
                                range.by = parseExpression(designator);
                            }

                            parseComment(null);
                            //Create array range and send from 0, len-1
                            range.finalize();
                            indexer.indices.Add(range);
                        }
                        else
                        {
                            EXPRESSION index = parseExpression(designator);
                            if (index == null) ERROR.SyntaxErrorIn("indexer", "argument is missing");
                            if (LEXAN.isDotDot())
                            {
                                ARRAY_RANGE range = new ARRAY_RANGE();
                                range.from = index;

                                EXPRESSION to = parseExpression(designator);
                                if (to != null) { range.to = to; range.wasToWritten = true; }
                                //if (!(LEXAN.isRightBracket() || LEXAN.isComma()))
                                //{
                                //    range.to = parseExpression(designator);
                                //}
                                parseComment(null);
                                if (LEXAN.isBy())
                                {
                                    range.by = parseExpression(designator);
                                }

                                //Create array range and send from 0, len-1
                                range.finalize();
                                indexer.indices.Add(range);
                            }
                            else
                            {
                                indexer.indices.Add(index);
                            }
                        }

						// See, whether next symbol is a comment
						parseComment(null);
                    }
                    while
                        ( LEXAN.isComma() );

                    indexer.setContext(start,LEXAN.getSourceContext());
                    designator = indexer;

					// See, whether next symbol is a comment
					parseComment(null);
                    LEXAN.expectRightBracket();
#if DEBUG
                    Exit("Indexer",designator!=null);
#endif
                }
                else if (LEXAN.isLeftBrace()) // {
                { 
                    // It is a coversion like char{12}()
                    // It will be processed by a caller. Return identifier and
                    // put the brace back to lex
                    LEXAN.back(LEXEM.LeftBrace);
                    // designator will be returned
                    break;
                }
                else if (LEXAN.isLeftParenth())
                {
                    // ACHTUNG!!                             ///////////////
                    // Add SIGNATURES to actuals!!!          ///////////////
#if DEBUG
                    Enter("Call");
#endif
                    CALL call = new CALL();
                    call.callee = designator;
                    call.enclosing = CONTEXT.current;
                    // WARNING! Actuals may be omitted!
                    // See, whether next symbol is a comment
                    parseComment(null);
                    if (sink != null)
                        sink.StartParameters(LEXAN.getSourceContext());

                    if (LEXAN.isRightParenth())
                    {
                        // no actuals
                    }
                    else
                    {
                        EXPRESSION first_actual = null; //first actual parameter; can be != null for max and min

                        if (designator != null && !(designator is SAFEGUARD) && !(designator is OBJECT))
                        {
                            DECLARATION left = (DECLARATION)designator.resolve();
                            if (left == STANDARD.Max || left == STANDARD.Min || left == STANDARD.Size)
                            {
                                // Parameter for MAX, MIN, and SIZE should be a TYPE.
                                // Create pseudo-argument ("literal") and
                                // store the type in its field.
                                first_actual = parseExpression(call);
                                if ((first_actual is TYPE_CONV) || !(first_actual.type is ARRAY_TYPE)) //it means that argument was not array but type
                                {
                                    EXPRESSION pseudo_actual = new INTEGER_LITERAL();
                                    //TYPE t = parseType(null, null, "predefined MIN/MAX/SIZE");
                                    TYPE t = first_actual.type;
                                    pseudo_actual.type = t;
                                    call.arguments.Add(pseudo_actual);

                                    if (left == STANDARD.Size)
                                        call.type = STANDARD.Integer.type;
                                    else // MIN/MAX
                                    {
                                        if (t is CHAR_TYPE || t is INTEGER_TYPE || t is SET_TYPE)
                                            call.type = STANDARD.Integer.type;
                                        else if (t is ENUM_TYPE || t is REAL_TYPE || t is CARDINAL_TYPE)
                                            call.type = t;
                                    }
                                    goto Continue;
                                }

                                /****/
                                //EXPRESSION pseudo_actual = new INTEGER_LITERAL();
                                //TYPE t = parseType(null, null, "predefined MIN/MAX/SIZE");
                                //pseudo_actual.type = t;
                                //call.arguments.Add(pseudo_actual);

                                //if (left == STANDARD.Size)
                                //    call.type = STANDARD.Integer.type;
                                //else // MIN/MAX
                                //{
                                //    if (t is CHAR_TYPE || t is INTEGER_TYPE || t is SET_TYPE)
                                //        call.type = STANDARD.Integer.type;
                                //    else if (t is ENUM_TYPE || t is REAL_TYPE || t is CARDINAL_TYPE)
                                //        call.type = t;
                                //}
                                //goto Continue;
                                /****/
                            }
                            //  else if ( left == STANDARD.Val )
                            //  {
                            //      // First parameter should be a type
                            //      EXPRESSION pseudo_actual = new INTEGER_LITERAL();
                            //      TYPE t = parseType("predefined VAL");
                            //      pseudo_actual.type = t;
                            //      call.arguments.Add(pseudo_actual);
                            //      call.type = t;
                            //
                            //      LEXAN.expectComma();
                            //      EXPRESSION second = parseExpression(designator);
                            //      call.arguments.Add(second);
                            //
                            //      goto Continue;
                            //  }
                            else if (left == STANDARD.Write || left == STANDARD.WriteLn)
                            {
                                // Special argument syntax
                                do
                                {
                                    // Taking argument itself
                                    call.arguments.Add(parseExpression(designator));

                                    // See, whether next symbol is a comment
                                    parseComment(null);
                                    if (LEXAN.isColon())
                                    {
                                        // Taking width
                                        call.arguments.Add(parseExpression(designator));

                                        // See, whether next symbol is a comment
                                        parseComment(null);
                                        if (LEXAN.isColon())
                                        {
                                            // Taking mantissa's width
                                            call.arguments.Add(parseExpression(designator));
                                        }
                                        else
                                            call.arguments.Add(null);
                                    }
                                    else
                                    {
                                        call.arguments.Add(null);
                                        call.arguments.Add(null);
                                    }
                                    // See, whether next symbol is a comment
                                    parseComment(null);
                                }
                                while
                                    (LEXAN.isComma());
                                goto Continue;
                            }
                        }

                        if (first_actual != null)
                        {
                            call.arguments.Add(first_actual);
                            // See, whether next symbol is a comment
                            parseComment(null);
                            if (!LEXAN.isComma()) goto Continue;
                        }

                        do
                        {
                            if (sink != null)
                                sink.NextParameter(LEXAN.getSourceContext());
                            EXPRESSION actual = parseExpression(call);  // !!!!!!!!!!!!!
                            if (actual == null) ERROR.SyntaxErrorIn("procedure call", "actual argument is missing");
                            call.arguments.Add(actual);
                            // See, whether next symbol is a comment
                            parseComment(null);
                        }
                        while
                            (LEXAN.isComma());

                    Continue:
                        // See, whether next symbol is a comment
                        parseComment(null);
                        LEXAN.expectRightParenth();
                        if (sink != null)
                            sink.EndParameters(LEXAN.getSourceContext());
                    }
                    call.setContext(start, LEXAN.getSourceContext());
                    //TODO: Replace this hack with better solution
                    // Now this is hack for the case:
                    // (expression).proc(args);
                    // E.g. (a+b).proc;
                    if ((call != null) && (call.callee == null) && (call.arguments.Length == 1))
                    {
                        EXPRESSION exqualifier = call.arguments[0];
                        DESIGNATOR des = parseDesignator();
                        if ((des is CALL) && (exqualifier is DESIGNATOR))
                        {
                            DESIGNATOR qual = exqualifier as DESIGNATOR;
                            CALL cl = des as CALL;
                            SELECTOR member = cl.callee as SELECTOR;
                            cl.callee = new SELECTOR();
                            ((SELECTOR)cl.callee).left_part = qual;
                            //  ((SELECTOR)cl.callee).member = member;
                        }
                        else
                        {
                            ERROR.NotImplemented("Calls like (a+b).method");
                        }

                    }
                    designator = call;
#if DEBUG
                    Exit("Call", designator != null);
#endif
                }
                else if (LEXAN.isDot())
                {
#if DEBUG
                    Enter("Member");
#endif
                    // See, whether next symbol is a comment
                    parseComment(null);
                    LEXAN.expectIdent();
                    designator = SELECTOR.process(designator, Scanner.last_id);
                    if (designator != null) designator.setContext(start, LEXAN.getSourceContext());
#if DEBUG
                    Exit("Member", designator != null);
#endif
                }
                else
                    break;  // exit while-loop

            }  // while
#if DEBUG
            Exit("Designator");
#endif
            return designator;
        }

        // parseNew
        // --------
        //
        //
        private NEW parseNew ( )
        {
#if DEBUG
            Enter("New");
#endif            
            NEW New = new NEW();
            DESIGNATOR designator = parseDesignator();            

            if ( designator != null )
            {                
                New.new_type = designator;
            }
            else
            {
                ERROR.SyntaxErrorIn("new","no type name");
                LEXAN.skipUntil(LEXEM.Semicolon);
                goto ExitPars;
            }

            if ( designator is CALL )
            {
				// See, whether next symbol is a comment
                New.arguments = ((CALL)designator).arguments;
                New.new_type = ((CALL)designator).callee;
            }
        ExitPars:
#if DEBUG
            Exit("New");
#endif
            return New;
        }

        private BINARY parseMulOperator ( )
        {
#if DEBUG
            Enter("MulOperator");
#endif
            BINARY bin;

			// See, whether next symbol is a comment
			parseComment(null);
            if      ( LEXAN.isStar() )      bin = new MULTIPLY();
            else if ( LEXAN.isSlash() )     bin = new DIVIDE();
            else if ( LEXAN.isDiv() )       bin = new DIV();
            else if ( LEXAN.isMod() )       bin = new MOD();
            else if ( LEXAN.isAmpersand() ) bin = new AND();
            else if ( LEXAN.isPlusStar()  ) bin = new PSEUDO_SCALAR_PRODUCT();
            else if ( LEXAN.isDotStar()   ) bin = new MULTIPLY_ELEMENTWISE();
            else if ( LEXAN.isDotSlash()  ) bin = new DIVIDE_ELEMENTWISE();
            else if ( LEXAN.isBackSlash() ) bin = new LEFTDIVISION();
            else
            {
             // OPERATOR_DECL op = LEXAN.isUserDefined(2);
             // if ( op != null ) bin = new USER_BINARY(op);
             // else              
                    bin = null;
            }

#if DEBUG
            Exit("MulOperator",bin!=null);
#endif
            return bin;
        }

        // parseAddOperator
        // ----------------
        //
        private BINARY parseAddOperator ( )
        {
#if DEBUG
            Enter("AddOperator");
#endif
            BINARY bin;

			// See, whether next symbol is a comment
			parseComment(null);
            if (LEXAN.isPlus()) bin = new PLUS();
            else if (LEXAN.isMinus()) bin = new MINUS();
            else if (LEXAN.isOr()) bin = new OR();
            //else if (LEXAN.isBy()) bin = new RANGESTEP();
            //else if (LEXAN.isDotDot()) bin = new ARRAY_RANGE();
            else
            {
                // OPERATOR_DECL op = LEXAN.isUserDefined(3);
                // if ( op != null ) bin = new USER_BINARY(op);
                // else              
                bin = null;
            }

#if DEBUG
            Exit("AddOperator",bin!=null);
#endif
            return bin;
        }

        // parseRelation
        // -------------
        //
        private BINARY parseRelation ( )
        {
#if DEBUG
            Enter("Relation");
#endif
            BINARY rel;

			// See, whether next symbol is a comment
			parseComment(null);
            if      ( LEXAN.isEqual() )        rel = new EQUAL();
            else if ( LEXAN.isNonEqual() )     rel = new NON_EQUAL();
            else if ( LEXAN.isLess() )         rel = new LESS();
            else if ( LEXAN.isLessEqual() )    rel = new LESS_EQUAL();
            else if ( LEXAN.isGreater() )      rel = new GREATER();
            else if ( LEXAN.isGreaterEqual() ) rel = new GREATER_EQUAL();
            else if ( LEXAN.isDotEqual())      rel = new EQUAL_ELEMENTWISE();
            else if ( LEXAN.isDotNonEqual())   rel = new NON_EQUAL_ELEMENTWISE();
            else if ( LEXAN.isDotLess())       rel = new LESS_ELEMENTWISE();
            else if ( LEXAN.isDotLessEqual())  rel = new LESS_EQUAL_ELEMENTWISE();
            else if ( LEXAN.isDotGreater())    rel = new GREATER_ELEMENTWISE();
            else if ( LEXAN.isDotGreaterEqual())   rel = new GREATER_EQUAL_ELEMENTWISE();
            else if ( LEXAN.isIn() )           rel = new IN();
            else if ( LEXAN.isImplements() )   rel = new IMPLEMENTS();
            else if ( LEXAN.isIs() )           rel = new IS();
            else
            {
             // OPERATOR_DECL op = LEXAN.isUserDefined(4);
             // if ( op != null ) rel = new USER_BINARY(op);
             // else              
                    rel = null;
            }

#if DEBUG
            Exit("Relation",rel!=null);
#endif
            return rel;
        }

        private void parseSignature ( )
        {
#if DEBUG
            Enter("Signature");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.expectLeftParenth();

            do
            {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isVar() )
                {
                }
                do
                {
					// See, whether next symbol is a comment
					parseComment(null);
                    // FormalType
                    if ( LEXAN.isArray() )
                    {
						// See, whether next symbol is a comment
						parseComment(null);
                        LEXAN.expectOf();
                        parseQualIdent();
                    }
                    else if ( LEXAN.isObject() )
                    {
                    }
                    else
                    {
                        parseQualIdent();
                    }
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while
                    ( LEXAN.isComma() );
            }
            while
                ( LEXAN.isSemicolon() );

			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.expectRightParenth();

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isColon() )
            {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isObject() )
                {
                }
                else
                {
                    parseQualIdent();
                }
            }
#if DEBUG
            Exit("Signature");
#endif
        }

        ////////////////////////////////////////////////////////////////////////////

        // protocol P = (* Syntax of the protocol *) 
        //     (
        //         (* Keywords *)
        //         RUNAWAY, CHASE, KO, ATTACK, DEFENSE, LEG, NECK, HEAD,
        //
        //         (* Productions *)
        //         attack = ATTACK strike,
        //         defense = DEFENSE strike,
        //         strike = bodypart [ strength ],
        //         bodypart = LEG | NECK | HEAD,
        //         strength = integer,
        //         fight = { attack ( { defense  attack } | RUNAWAY [ ?CHASE] | KO | fight ) }
        //     );

        // parseProtocol
        // -------------
        //
        // protocol name = ( Enum-type-definition-("keywords") , Syntax-productions );
        //
        private void parseProtocol ( NODE enclosing )
        {
#if DEBUG
            Enter("Protocol");
#endif
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

            Identifier protocol_ident = null;
            ENUM_TYPE keywords = null;
            SYNTAX syntax = new SYNTAX();

            MODIFIERS modifiers  = parseModifiers();

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.expectIdent() )
            {
                protocol_ident = Scanner.last_id;
            }
            else
            {
                ERROR.NoUnitName("protocol");
                LEXAN.skipUntil(Recovery.LeftParenth_Id);
                protocol_ident = ERROR.errUnitName;
            }
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.expectEqual();
			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.expectLeftParenth();

            // Parsing keywords (if any)

			// See, whether next symbol is a comment
			parseComment(null);
            start = LEXAN.getSourceContext(); // '('

            IDENT_LIST ids = new IDENT_LIST();
            Identifier last = null;
            string protocolSpecification = "";

            do
            {
				// See, whether next symbol is a comment
				parseComment(null);
                if ( LEXAN.isIdent() )
                {
                    last = new Identifier(Scanner.last_id.Name,Scanner.last_id.SourceContext);
                    if ( LEXAN.isEqual() ) // this is a production, not a keyword
                        break;

                    ids.Add(last);
                    last = null;
                }
                else
                {
                    ERROR.SyntaxErrorIn("protocol","no keywords and/or productions");
                    LEXAN.skipUntil(Recovery.Comma_RightParenth);
                }
				// See, whether next symbol is a comment
				parseComment(null);
            }
            while
                ( LEXAN.isComma() );

            if ( ids.Length > 0 )
            {
                keywords = ENUM_TYPE.create(ids);
             // keywords.enclosing -- later (see below)
                keywords.setContext(start,LEXAN.getSourceContext());
            }

            // Now parsing the syntax productions
            if ( last != null )
            {
                syntax.keywords = keywords; // we need it while parsing the syntax
                parseSyntax(last, syntax, ref protocolSpecification);
            }

            // Processing final lexems
			// See, whether next symbol is a comment
			parseComment(null);
            if ( !LEXAN.expectRightParenth() )
            {
                ERROR.SyntaxErrorIn("protocol declaration","no right parenthesis");
                LEXAN.skipUntil(LEXEM.RightParenth);
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.isRightParenth();
            }
            //  LEXAN.expectSemicolon(); -- will be done outside

            // Creating the protocol itself
            PROTOCOL_DECL protocol = PROTOCOL_DECL.create(modifiers,protocol_ident,keywords,syntax, protocolSpecification);
            if ( keywords != null ) keywords.enclosing = protocol;
            if ( syntax != null ) syntax.enclosing = protocol;
            protocol.setContext(start,LEXAN.getSourceContext());
#if DEBUG
            Exit("Protocol");
#endif
            return;
        }

        private void parseSyntax(Identifier first, SYNTAX syntax, ref string protocolSpecification)
        {
            // See Protocol.cs file for syntax classes.

            while ( true )
            {
                parseProduction(first, syntax, ref protocolSpecification);
             // syntax.productions.Add(production); -- is done in parseProduction
                first = null;

				// See, whether next symbol is a comment
				parseComment(null);

                if (LEXAN.isComma()) 
                {
                    protocolSpecification += ", ";
                    continue; 
                }
                break;
            }
        }

        private void parseProduction(Identifier first, SYNTAX syntax, ref string protocolSpecification)
        {
            Identifier prod_name = null;
            SourceContext start;

            if (first != null)
            {
                prod_name = first;
                start = first.SourceContext;
                protocolSpecification += first.Name + " = ";
            }
            else
            {
                // See, whether next symbol is a comment
                parseComment(null);
                if (!LEXAN.isIdent()) // an error
                {
                    ERROR.SyntaxErrorIn("protocol syntax", "no production");
                    return;
                }
                prod_name = Scanner.last_id;
                start = LEXAN.getSourceContext();
                protocolSpecification += prod_name.Name;

                // See, whether next symbol is a comment
                parseComment(null);
                LEXAN.expectEqual();
                protocolSpecification += " = ";
            }

            // Create the new production, add it to the list and resolve it.
            PRODUCTION production = new PRODUCTION(prod_name,syntax);
            production.right_part = parseSequence(syntax, ref protocolSpecification); 
            production.setContext(start,LEXAN.getSourceContext());
        }

        private UNIT parseUnit(SYNTAX syntax, ref string protocolSpecification)
        {
            // For terminals, non-terminals, parenthesized sequences,
            // alternatives, optional groups or repetition groups.
            UNIT unit = null;
            bool receive = false;

			// See, whether next symbol is a comment
			parseComment(null);
            LEXAN.isEOF();

         Again:
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();

            // fight = { attack ( { defense  attack } | RUNAWAY [ ?CHASE] | KO | fight ) }.
            // attack = ATTACK strike. 
            // defense = DEFENSE strike.	
            // strike = bodypart [ strength ].
            // bodypart = LEG | NECK | HEAD.
            // strength = integer. 

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isLeftBrace() )           // repetition group
            {
                protocolSpecification += "{ ";
                unit = parseSequence(syntax, ref protocolSpecification);
                unit.kind = UNIT.Kind.ZeroOrMore;
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightBrace();
                protocolSpecification += "}";
                unit.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isLeftParenth() )    // just a group
            {
                protocolSpecification += "( ";
                unit = parseSequence(syntax, ref protocolSpecification);
                unit.kind = UNIT.Kind.Always;
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightParenth();
                protocolSpecification += ")";
                unit.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isLeftBracket() )    // optional group
            {
                protocolSpecification += "[ ";
                unit = parseSequence(syntax, ref protocolSpecification);
                unit.kind = UNIT.Kind.Optional;
				// See, whether next symbol is a comment
				parseComment(null);
                LEXAN.expectRightBracket();
                protocolSpecification += "]";
                unit.setContext(start,LEXAN.getSourceContext());
            }
            else if ( LEXAN.isIntNumber() )
            {
                long val = Scanner.integer_value;
                unit = new CONSTANT(INTEGER_LITERAL.create(val),receive);
                unit.setContext(start);
                protocolSpecification += val.ToString() + " ";
            }
            else if ( LEXAN.isRealNumber() )
            {
                double val   = Scanner.real_value;
                unit = new CONSTANT(REAL_LITERAL.create(val),receive);
                unit.setContext(start);
                protocolSpecification += val.ToString() + " ";
            }
            else if ( LEXAN.isString() )
            {
                string  str = Scanner.identifier;
                LITERAL literal = null;
                if ( str.Length == 1 ) literal = CHAR_LITERAL.create(str[0]);
                else                   literal = STRING_LITERAL.create(str);
                unit = new CONSTANT(literal,receive);
                unit.setContext(start);
                protocolSpecification += str + " ";
            }
            else if ( LEXAN.isQuestion() )     // receive sign
            {
                receive = true;
                protocolSpecification += "?";
                goto Again;
            }
            else if ( LEXAN.isObject() )
            {
                unit = new TYPE_NAME(INTERFACE_TYPE.create(),receive);
                unit.setContext(start);
                protocolSpecification += Scanner.identifier + " ";
            }
            else  // Identifier (perhaps qualified)
            {
                IDENT_LIST qualName = parseQualIdent();

                if ( qualName == null )
                {
                 // ERROR.SyntaxErrorIn("production","illegal terminal or non-terminal");
                 // This might be just an end of a sequence/alternative etc.
                    return null;
                }
                else if ( qualName.Length == 1 )
                {
                    protocolSpecification += qualName.ToString() + " ";

                    // Perhaps this is a local (non)terminal?
                    Identifier id = qualName[0];
                    PRODUCTION production = syntax.productions.find(id);
                    if (production != null)
                    {
                        // This is a known non-terminal
                        unit = new NONTERMINAL(production);
                    }
                    else
                    {
                        ENUM_TYPE keywords = syntax.keywords;
                        ENUMERATOR_DECL e = null;

                        if (keywords != null && (e = (ENUMERATOR_DECL)syntax.keywords.find(id)) != null)
                            // This is a "true" terminal: a keyword
                            unit = new TERMINAL(e, receive);
                        else
                        {
                            // This is either an unresolved nonterminal,
                            // or... a standard type?
                            TYPE type = TYPE.evaluateTypeName(qualName[0], 0);
                            if (type != null) // this is a predefined type!
                            {
                                if (type is INTEGER_TYPE || type is CARDINAL_TYPE || type is CHAR_TYPE ||
                                    type is REAL_TYPE || type is STRING_TYPE)
                                {
                                    unit = new TYPE_NAME(type, receive);
                                }
                                else
                                {
                                    ERROR.IllegalTypeInProduction(type.ToString(), start);
                                    unit = new TYPE_NAME(STANDARD.Integer.type, receive);
                                }
                            }
                            else
                                goto UnknownName;  // see below
                        }
                    }
                    unit.setContext(start);

                    if ( receive && unit is NONTERMINAL )
                        ERROR.SyntaxErrorIn("'" + ((unit == null || unit.name == null) ? "protocol" : unit.name.Name) + "'", "cannot mark production by receive sign");
                }
             // else -- qualIdent.Length > 1

            UnknownName:

                if ( unit != null ) return unit;
             // else -- this is a simple or qualified name which hasn't been recognized above.

                // An external referenece?
                DESIGNATOR designator = SELECTOR.processQualName(null,qualName);
                if ( designator != null ) 
                {
                    NODE d = designator.resolve();
                    if ( d is TYPE_DECL )
                    {
                        TYPE t = (d as TYPE_DECL).type;
                        if ( t is ENUM_TYPE || t is INTEGER_TYPE || t is CARDINAL_TYPE ||
                            t is REAL_TYPE || t is CHAR_TYPE || t is STRING_TYPE )
                        {
                            unit = new TYPE_NAME(t,receive);
                        }
                        else if ( t is INTERFACE_TYPE && ((INTERFACE_TYPE)t).interfaces.Length==0 )
                        {
                            unit = new TYPE_NAME(INTERFACE_TYPE.create(),receive);
                        }
                        else
                        {
                            ERROR.IllegalTypeInProduction(t.ToString(),start);
                            unit = new TYPE_NAME(STANDARD.Integer.type,receive);
                        }
                    }
                    else if ( d is ENUMERATOR_DECL )
                    {
                        unit = new TERMINAL((ENUMERATOR_DECL)d,receive);
                    }
                    else if ( d is CONSTANT_DECL )
                    {
                        NODE r = (d as CONSTANT_DECL).resolve();
                        EXPRESSION e = (r as CONSTANT_DECL).initializer;
                        unit = new CONSTANT(e as LITERAL,receive);
                    }
                    else if ( d is UNKNOWN_DECL )
                    {
                        // It seems this is an unresolved nonterminal
                        // Add it to the list of unresolved.
                        // unit = new UNKNOWN_NONTERMINAL(syntax,qualName[0],receive);
                        ERROR.UndeclaredEBNFProduction(d.Name, CONTEXT.current.Name);                        
                    }
                    else
                    {
                        ERROR.IllegalEntityInProduction(qualName.ToString(),qualName.sourceContext);
                        unit = new CONSTANT(INTEGER_LITERAL.create(1),receive);
                    }
                    if(unit != null) unit.setContext(start);
                }
                else
                {
                    ERROR.IllegalEntityInProduction(qualName.ToString(),qualName.sourceContext);
                    unit = new CONSTANT(INTEGER_LITERAL.create(1),receive);
                }
            }
            return unit;
        }

        private UNIT parseSequence(SYNTAX syntax, ref string protocolSpecification)
        {
			// See, whether next symbol is a comment
			parseComment(null);
            SourceContext start = LEXAN.getSourceContext();
            UNIT result;

            SEQUENCE sequence = new SEQUENCE();
            while ( true )
            {
                UNIT unit = parseUnit(syntax, ref protocolSpecification);
                if ( unit == null ) break;
                sequence.sequence.Add(unit);
            }

			// See, whether next symbol is a comment
			parseComment(null);
            if ( LEXAN.isVert() )
            {
                // This is not a simple sequence but a group of alternatives
                sequence.setContext(start,LEXAN.getSourceContext());
                ALTERNATIVES alternatives = new ALTERNATIVES();
                alternatives.alternatives.Add(sequence);

                do
                {
                    protocolSpecification += "| ";
					// See, whether next symbol is a comment
					parseComment(null);
                    start = LEXAN.getSourceContext();
                    sequence = new SEQUENCE();

                    while ( true )
                    {
                        UNIT unit = parseUnit(syntax, ref protocolSpecification);
                        if ( unit != null )
                        {
                            sequence.sequence.Add(unit);
                        }
                        else
                        {
                            sequence.setContext(start,LEXAN.getSourceContext());
                            alternatives.alternatives.Add(sequence);
                            break;
                        }
                    }
					// See, whether next symbol is a comment
					parseComment(null);
                }
                while ( LEXAN.isVert() );

                result = alternatives;
            }
            else
                result = sequence;

            result.setContext(start,LEXAN.getSourceContext());
            return result;
        }

        ////////////////////////////////////////////////////////////////////////////

#if DEBUG
        // Tracing
        // -------

        private static int currShift = 6;

        private static void Enter ( string Msg )
        {
            if ( !debug ) return;
            for ( int i=0; i<currShift; i++ )
                System.Console.Write(" ");
            System.Console.WriteLine("Entering {0}",Msg);
            currShift += 3;
        }

        private static void Exit ( string Msg )
        {
            if ( !debug ) return;
            currShift -= 3;
            for ( int i=0; i<currShift; i++ )
                System.Console.Write(" ");
            System.Console.WriteLine("Exiting {0}",Msg);
        }

        private static void Exit ( string Msg, bool Res )
        {
            if ( !debug ) return;
            if ( Res ) Msg += ": SUCCESS";
            else       Msg += ": FAIL";
            Exit(Msg);
        }
#endif
    }  // class PARSER

}   // namespace ETH.Zonnon.Compiler
