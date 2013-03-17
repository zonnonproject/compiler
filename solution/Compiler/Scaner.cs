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
using System.IO;
using System.Collections;
using System.Compiler;

namespace ETH.Zonnon
{
    // Scanner
    // -------
    //
    // Interface:
    //
    // isLexem(), expect()          exported to PARSER
    //
    public sealed class Scanner : System.Compiler.Scanner //Microsoft.VisualStudio.Package.Scanner
    {
        // document
        // --------
        // The source text is represented in the compiler by the member
        // of Document class.
        // There are several fields in the class specifying the source characteristics
        // (document type, the language and its vendor, (current?) line number,
        // the "name" of the source text, and the text itself (public 'Text' member
        // of type System.Compiler.DocumentText).
        // Also there is an array 'lineOffsets': perhaps, for supporting debug...
        private Document document;

        // The same as 'Text' member from 'document' (see the constructor).
        private DocumentText sourceText;

        // The entire source program is either in 'sourceText' structure ('Text' member)
        // OR in THIS string. Low-level functions (like get_lexem() below) work
        // either with 'sourceText' or with 'sourceString'.
        //
        // The value of this variable is set by (overriden) SetSource() function.
        // Otherwise it is null (low-level functions check it).
        private string sourceString;

        // lexem_start_position
        // lexem_end_position
        // --------------------
        // These two variables specify the position of the current lexem
        // in the source. The values will be passed outside via ScanTokenAndProvideInfoAboutIt()
        // function.
        private int lexem_start_position;  // the first character of the lexem
        public  int lexem_end_position;    // the last character of the lexem PLUS ONE

        // current_position
        // line_number
        // current_symbol
        // current_lexem
        // ----------------
        // Internal variables for organizing scanning process.
        //
        private int   current_position;      // current scanning position
        private int   current_line_number;   // the number of the current line
        private char  current_symbol;        // current character taken from the source
        private LEXEM current_lexem;         // the code of the last recognized lexem
		private bool scanComments;			 // are we going to skip comments or preserve them?

        private int   text_end_position;     // the last character of the source PLUS ONE

        public SourceContext getSourceContext ( )
        {
            return new SourceContext(document,lexem_start_position,lexem_end_position);
        }

        // errors
        // ------
        // The structure for collecting the error messages
        // (it is the same for scanner, parser and other compiler parts,
        // and it is passed via constructor, see below).
        private ErrorNodeList errors;

        // SCANNER
        // -------
        // The default constructor.
        // SHOULD I initialize any values here?
        //
        internal Scanner ( )
        {
        }

        // SCANNER
        // -------
        // The constructor initializes the major class members.
        //
        internal Scanner ( Document document, ErrorNodeList errors, bool scanComments )
        {
            this.document = document;
            this.sourceText = document.Text;
            this.lexem_end_position = 0;
            this.text_end_position = document.Text.Length;
            this.errors = errors;
			this.scanComments = true; //scanComments;

            current_position = -1;
            current_line_number = 1;
            current_lexem = LEXEM.NoLexem;
            current_symbol = '\0';
            move();
        }

        // SetSource
        // ---------
        // This function overrides the function with the same name from
        // System.Compiler.Scanner. It is called from outside the compiler
        // (perhaps, from the environment) to set the alternative source.
        //
        // TODO: set appropriate values to my own locals!
        //
        public override void SetSource ( string source, int offset )
        {
            current_position    = -1;    // as in ctor
            current_line_number =  1;    // as in ctor
            current_symbol      = '\0';  // as in ctor

            this.sourceString = source;
            this.lexem_end_position = offset;
            this.text_end_position = source.Length;

			move();
        }

        //===============================================================================

        // subString
        // move
        // skipBlanks
        // ----------

        private string subString ( int start, int length )
        {
            if ( sourceString != null )
                return sourceString.Substring(start,length);
            else
                return sourceText.Substring(start,length);
        }

        // character
        // ---------
        // Returns the character in the specified source position.
        //
        private char character ( int position )
        {
            try
            {
                if (sourceString != null)
                    return sourceString[position];
                else
                    return sourceText[position];
            }
            catch(System.IndexOutOfRangeException)
            {
                return '\0';
            }
        }

        // move
        // ----
        // Moves the current position and puts the character
        // in this position to current_symbol variable.
        //
        private void move ( )
        {
            current_position++;
            if ( current_position > text_end_position-1 )  // end of source
            {
                current_symbol = (char)0;
                return;
            }
            current_symbol = character(current_position);
        }

        private void move_back ( )
        {
            current_position--;
            current_symbol = character(current_position);
        }

        // skipBlanks
        // ----------
        //
        private bool skipBlanks ( )
        {
            bool new_line = false;

            while ( true )
            {
                switch ( current_symbol )
                {
                    case ' '     :
                    case '\x009' : move();
                                   break;

                    case '\n'    : move(); new_line = true; current_line_number++;
                                   break;

                    case '\r'    : move(); new_line = true; current_line_number++;
                                   if ( current_symbol == '\n' ) move();
                                   break;

                    default      : return new_line;
                }
            }
        }

        //===============================================================================

        // COMMENTS, STRINGS etc.
        //
        // scanComment
        // scanString
        // -----------

        private int comment_nesting_level = 0;
		public  static string     comment; 

        // scanComment
        // -----------
        // We have already scanned '(' before calling the function
        // (see getLexem() function).
        //
        // NOTE: there ARE nested comments in Zonnon (as in Oberon).
        // NOTE: there are NO short comments in Zonnon.
        //
        // TODO: consider the case with unterminated comment!
        // TODO: consider the case with (* and *) inside string literals.
        //
        // stopAtEndOfLine parameter:
        //
        // If the parameter is TRUE then the function is used by ScanTokenAndProvideInfoAboutIt().
        // In this case we should scan comment only until end of line,
        // then stop scanning the comment and return.
        // Otherwise we just continue scanning (i.e., skipping) the comment.
        //

		private int scanComment ( bool stopAtEndOfLine)
        {
            return scanComment ( stopAtEndOfLine, 1);
        }

		private int scanComment ( bool stopAtEndOfLine, int  count)
		{			  
			bool end_of_line = false;

			while ( true )
			{
				while ( true )
				{
					end_of_line = skipBlanks();

					// If scanner is used by the environment AND end of line
					// has been encountered then stop scanning and return
					// the current depth of nesting comment.
					if (  end_of_line && stopAtEndOfLine )
						return count;

					if ( current_symbol == '*' || current_symbol == '(' || current_symbol == (char)0 )
						break;

					move();
				}

				switch ( current_symbol )
				{
					case (char)0 : // End of source: unterminated comment!

						if ( stopAtEndOfLine ) return count; // no error message
						ERROR.UnexpectedEndOfSource("in comment");
						return -1;  // will be checked by getLexem()

					case '(' : // Nested comment?
						if ( current_position+1 < text_end_position && character(current_position+1) == '*' )
						{
							move(); move();  // passing '(*'
							count++;
							continue;
						}
						move();  // passing '(' and continue
						break;

					case '*' : // End of comment?
						if ( current_position+1 < text_end_position && character(current_position+1) == ')' )
						{
							move(); move();  // passing '*)', and exit
							if ( --count == 0 ) return 0;
							else                continue;
						}
						move();  // passing '*' and continue
						break;
				}
			}
		}

		// Scan comment and store its return value so that it can be used within a node
		//
		//
		private int scanComment ( )
		{
			int  count = 0;  // ? is it correct ?
			//bool end_of_line = false;
			comment = "";

			//count = stopAtEndOfLine ? comment_nesting_level : 1;
			count = 1; //comment_nesting_level;

			while ( true )
			{
				while ( true )
				{
					//end_of_line = skipBlanks();

					// If scanner is used by the environment AND end of line
					// has been encountered then stop scanning and return
					// the current depth of nesting comment.
					
					/*
					if (  end_of_line ) //&& stopAtEndOfLine )
					{
						System.Console.WriteLine("COMMENT: " + comment);
						System.Console.WriteLine("Count " + count);
						return count;
					}
					*/
                    
					if ( current_symbol == '\n')
						current_line_number++;
					if ( current_symbol == '*' || current_symbol == '(' || current_symbol == (char)0 )
						break;
                    if (current_symbol == '<')
                    { if (CONTEXT.options.GenerateXML) comment += "&lt;"; }
                    else if (current_symbol == '>')
                    { if (CONTEXT.options.GenerateXML) comment += "&gt;"; }
                    else
                    { if (CONTEXT.options.GenerateXML) comment += current_symbol; }
					move();
				}

				switch ( current_symbol )
				{
					case (char)0 : // End of source: unterminated comment!
						ERROR.UnexpectedEndOfSource("in comment");
						return -1;  // will be checked by getLexem()

					case '(' : // Nested comment?
						if ( current_position+1 < text_end_position && character(current_position+1) == '*' )
						{
							move(); move();  // passing '(*'
							count++;
							continue;
						}
                        if (CONTEXT.options.GenerateXML) comment += current_symbol;
						move();  // passing '(' and continue
						break;

					case '*' : // End of comment?
						if ( current_position+1 < text_end_position && character(current_position+1) == ')' )
						{
							move(); move();  // passing '*)', and exit
							if ( --count == 0 ) return 0;
							else                continue;
						}
                        if (CONTEXT.options.GenerateXML) comment += current_symbol;
						move();  // passing '*' and continue
						break;
				}
			}
		}

        // scanString
        // ----------
        //
        // TODO: process end-of-line & end-of-file cases.
        //
        private void scanString ( char terminator )
        {
            identifier = "";

            while ( true )
            {
                while ( current_symbol != terminator )
                {
                    if ( current_symbol == (char)0 )
                    {
                        ERROR.UnexpectedEndOfSource("in string");
                        return;
                    }
                    // Store symbols in the buffer
                    identifier += current_symbol;
                    move();
                }
                if ( current_position+1 < text_end_position && character(current_position+1) == terminator )
                {
                    // Double quotes: "". Keep ONE " in the buffer
                    identifier += terminator;
                    move(); move();
                    // Continue scanning...
                }
                else
                {
                    // End of string
                    move();
                    return;
                }
            }
        }

        //===============================================================================

        // LETTERS, DIGITS etc.

        // isDigit
        // isHexDigit
        // isAsciiLetter
        // isLetterOrDigit
        // isPartOfId
        // ---------------

        private bool isDigit ( )
        {
            return current_symbol >= '0' && current_symbol <= '9';
        }

        private bool isHexDigit ( )
        {
            return isDigit() || current_symbol >= 'A' && current_symbol <= 'F' ||
                                current_symbol >= 'a' && current_symbol <= 'f';
        }

        private bool isAsciiLetter ( )
        {
            return current_symbol >= 'A' && current_symbol <= 'Z' ||
                   current_symbol >= 'a' && current_symbol <= 'z' ||
                   current_symbol >= 'à' && current_symbol <= 'ÿ' ||
                   current_symbol >= 'À' && current_symbol <= 'ß';
        }

        private bool isLetterOrDigit ( )
        {
            return isAsciiLetter() || isDigit();
        }

        private bool isPartOfId ( )
        {
            return isLetterOrDigit() || current_symbol == '_' || current_symbol == '$';
        }

        //===============================================================================

        // IDENTIFIERS & KEYWORDS
        //
        // scanIdentifier
        // detectKeyword
        // detectModifier
        // --------------

        // Additional information about last 'identifier' lexem
        public  static string     identifier;   // (is used for strings and numeric literals as well)
        private static uint       hash_value;   // last identifier's hash value
        public  static Identifier last_id;

        public static readonly uint hash_module = 577; // 511-- previous module; // hash module (exported)

        private LEXEM scanIdentifier ( )
        {
            int i = 0;
            uint g;     // for calculating hash
            const uint hash_mask = 0xF0000000;

            identifier = "";
            hash_value = 0;

            do  // taking symbol by symbol calculating hash value.
            {
                identifier += current_symbol;
                // Calculating hash: see Dragon Book, Fig. 7.35
                hash_value = (hash_value << 4) + (byte)current_symbol;
                if ( (g = hash_value & hash_mask) != 0 )
                {
                 // hash_value ^= g >> 24 ^ g;
                    hash_value = hash_value ^ (hash_value >> 24);
                    hash_value ^= g;
                }

                // passing the symbol
                move();
                i++;
            }
            while
                ( isPartOfId() );

            lexem_end_position = current_position;
            hash_value %= hash_module;   // final hash value for identifier

            last_id = Identifier.For(identifier);
            last_id.SourceContext = this.getSourceContext();
            // Is this identifier a keyword?
            LEXEM kw = detectKeyword();  // takes 'hash_value' and 'identifier'
            if ( kw != LEXEM.NoLexem )
                return kw;
            else
            {
   //           TABLE.add(subString(lexem_start_position,i),hash_value);  //*////////////
                return LEXEM.Id;
            }
        }

        // detectKeyword
        // -------------
        // Uses 'identifier' and 'hash_value'.
        //
        private LEXEM detectKeyword ( )
        {
            LEXEM lexem = LEXEM.NoLexem;

            switch ( hash_value )
            {
                case  20:   if ( identifier == "end" )            lexem = LEXEM.End;            break;
                case  28:   if ( identifier == "implements" )     lexem = LEXEM.Implements;     break;
                case  33:   if ( identifier == "array" )          lexem = LEXEM.Array;          break;
                case  45:   if ( identifier == "operator" )       lexem = LEXEM.Operator;       break;
                case  51:   if ( identifier == "if" )             lexem = LEXEM.If;             break;
                case  59:   if ( identifier == "in" )             lexem = LEXEM.In;             break;
                case  64:   if ( identifier == "is" )             lexem = LEXEM.Is;             break;
                case  69:   if ( identifier == "while" )          lexem = LEXEM.While;          break;
                case 104:   if ( identifier == "case" )           lexem = LEXEM.Case;           break;
                case 109:   if ( identifier == "loop" )           lexem = LEXEM.Loop;           break;
                case 116:   if ( identifier == "math")            lexem = LEXEM.Math;           break;
                case 120:   if ( identifier == "procedure" )      lexem = LEXEM.Procedure;      break;
                case 128:   if ( identifier == "await" )          lexem = LEXEM.Await;          break;
                case 139:   if ( identifier == "var" )            lexem = LEXEM.Var;            break;
                case 146:   if ( identifier == "object" )         lexem = LEXEM.Object;         break;
                case 147:   if ( identifier == "of" )             lexem = LEXEM.Of;             break;
                case 149:   if ( identifier == "else" )           lexem = LEXEM.Else;           break;
/* NEW */       case 151:   if ( identifier == "termination" )    lexem = LEXEM.Termination;    break;
                case 155:   if ( identifier == "on" )             lexem = LEXEM.On;             break;
                case 159:   if ( identifier == "or" )             lexem = LEXEM.Or;             break;
                case 163:   if ( identifier == "unused")          lexem = LEXEM.Unused;         break;
                case 191:   if ( identifier == "exit" )           lexem = LEXEM.Exit;           break;
                case 198:   if ( identifier == "self" )           lexem = LEXEM.Self;           break;
                case 201:   if ( identifier == "import" )         lexem = LEXEM.Import;         break;
                case 205:   if ( identifier == "return" )         lexem = LEXEM.Return;         break;
                case 208:   if ( identifier == "until" )          lexem = LEXEM.Until;          break;
                case 224:   if ( identifier == "definition" )     lexem = LEXEM.Definition;     break;
                case 228:   if ( identifier == "send" )           lexem = LEXEM.Send;           break;
                case 231:   if ( identifier == "repeat" )         lexem = LEXEM.Repeat;         break;
                case 236:   if ( identifier == "to" )             lexem = LEXEM.To;             break;
                case 242:   if ( identifier == "elsif" )          lexem = LEXEM.Elsif;          break;
                case 245:   if ( identifier == "type" )           lexem = LEXEM.Type;           break;
                case 260:   if ( identifier == "accept" )         lexem = LEXEM.Accept;         break;
                case 279:   if ( identifier == "div" )            lexem = LEXEM.Div;            break;
                case 306:   if ( identifier == "for" )            lexem = LEXEM.For;            break;
                case 342:   if ( identifier == "then" )           lexem = LEXEM.Then;           break;
                case 353:   if ( identifier == "mod" )            lexem = LEXEM.Mod;            break;
                case 368:   if ( identifier == "const" )          lexem = LEXEM.Const;          break;
             // case 381:   if ( identifier == "dialog" )         lexem = LEXEM.Dialog;         break;
                case 390:   if ( identifier == "begin" )          lexem = LEXEM.Begin;          break;
                case 441:   if ( identifier == "protocol" )       lexem = LEXEM.Protocol;       break;
                case 443:   if ( identifier == "exception" )      lexem = LEXEM.Exception;      break;
                case 464:   if ( identifier == "activity" )       lexem = LEXEM.Activity;       break;
                case 468:   if ( identifier == "new" )            lexem = LEXEM.New;            break;
                case 482:   if ( identifier == "sparse" )         lexem = LEXEM.Sparse;         break;
                case 498:   if ( identifier == "launch" )         lexem = LEXEM.Launch;         break;
                case 506:   if ( identifier == "refines" )        lexem = LEXEM.Refines;        break;
                case 513:   if ( identifier == "as" )             lexem = LEXEM.As;             break;
                case 516:   if ( identifier == "implementation" ) lexem = LEXEM.Implementation; break;
                case 521:   if ( identifier == "nil" )            lexem = LEXEM.Nil;            break;
/* NEW */       case 532:   if ( identifier == "from" )           lexem = LEXEM.From;           break;
                case 535:   if ( identifier == "by" )             lexem = LEXEM.By;             break;
                case 549:   if ( identifier == "module" )         lexem = LEXEM.Module;         break;
                case 557:   if ( identifier == "do" )             lexem = LEXEM.Do;             break;
                case 559:   if ( identifier == "receive" )        lexem = LEXEM.Receive;        break;
/* NEW */       case 575:   if ( identifier == "record" )         lexem = LEXEM.Record;         break;

/************* Old keywords for the module of 511
                case  67:   if ( identifier == "ACTIVITY" )   lexem = LEXEM.Activity;    break;
                case 437:   if ( identifier == "ARRAY" )      lexem = LEXEM.Array;       break;
                case 101:   if ( identifier == "AS" )         lexem = LEXEM.As;          break;
                case 336:   if ( identifier == "AWAIT" )      lexem = LEXEM.Await;       break;
                case 317:   if ( identifier == "BEGIN" )      lexem = LEXEM.Begin;       break;
                case 123:   if ( identifier == "BY" )         lexem = LEXEM.By;          break;
                case 177:   if ( identifier == "CASE" )       lexem = LEXEM.Case;        break;
                case 439:   if ( identifier == "CONST" )      lexem = LEXEM.Const;       break;
                case  45:   if ( identifier == "DEFINITION" ) lexem = LEXEM.Definition;  break;
                case 266:   if ( identifier == "DIV" )        lexem = LEXEM.Div;         break;
                case 145:   if ( identifier == "DO" )         lexem = LEXEM.Do;          break;
                case 454:   if ( identifier == "ELSE" )       lexem = LEXEM.Else;        break;
                case 244:   if ( identifier == "ELSIF" )      lexem = LEXEM.Elsif;       break;
                case  73:   if ( identifier == "END" )        lexem = LEXEM.End;         break;
                case 460:   if ( identifier == "EXCEPTION" )  lexem = LEXEM.Exception;   break;
                case 315:   if ( identifier == "EXIT" )       lexem = LEXEM.Exit;        break;
                case 359:   if ( identifier == "FOR" )        lexem = LEXEM.For;         break;
                case 216:   if ( identifier == "IF" )         lexem = LEXEM.If;          break;
                case 328:   if ( identifier == "IMPLEMENTATION" ) lexem = LEXEM.Implementation; break;
                case 173:   if ( identifier == "IMPLEMENTS" ) lexem = LEXEM.Implements;  break;
                case 215:   if ( identifier == "IMPORT" )     lexem = LEXEM.Import;      break;
                case 224:   if ( identifier == "IN" )         lexem = LEXEM.In;          break;
                case 229:   if ( identifier == "IS" )         lexem = LEXEM.Is;          break;
                case 203:   if ( identifier == "LOOP" )       lexem = LEXEM.Loop;        break;
                case  93:   if ( identifier == "MOD" )        lexem = LEXEM.Mod;         break;
                case 283:   if ( identifier == "MODULE" )     lexem = LEXEM.Module;      break;
                case 208:   if ( identifier == "NEW" )        lexem = LEXEM.New;         break;
                case 261:   if ( identifier == "NIL" )        lexem = LEXEM.Nil;         break;
                case  71:   if ( identifier == "OBJECT" )     lexem = LEXEM.Object;      break;
                case 312:   if ( identifier == "OF" )         lexem = LEXEM.Of;          break;
                case 320:   if ( identifier == "ON" )         lexem = LEXEM.On;          break;
                case  99:   if ( identifier == "OPERATOR" )   lexem = LEXEM.Operator;    break;
                case 324:   if ( identifier == "OR" )         lexem = LEXEM.Or;          break;
                case 219:   if ( identifier == "PROCEDURE" )  lexem = LEXEM.Procedure;   break;
                case  12:   if ( identifier == "REFINES" )    lexem = LEXEM.Refines;     break;
                case 483:   if ( identifier == "REPEAT" )     lexem = LEXEM.Repeat;      break;
                case 278:   if ( identifier == "RETURN" )     lexem = LEXEM.Return;      break;
                case 196:   if ( identifier == "SELF" )       lexem = LEXEM.Self;        break;
                case 357:   if ( identifier == "THEN" )       lexem = LEXEM.Then;        break;
                case 401:   if ( identifier == "TO" )         lexem = LEXEM.To;          break;
                case 277:   if ( identifier == "TYPE" )       lexem = LEXEM.Type;        break;
                case  15:   if ( identifier == "UNTIL" )      lexem = LEXEM.Until;       break;
                case 143:   if ( identifier == "VAR" )        lexem = LEXEM.Var;         break;
                case   3:   if ( identifier == "WHILE" )      lexem = LEXEM.While;       break;
***************/
            }
            return lexem;
        }

        // detectModifier
        // --------------
        // Uses 'last_id' and 'hash_value'.
        //
        public static int detectModifier ( )
        {
            switch ( hash_value )
            {
                case  69 : if ( last_id.Name == "private" )    return MODIFIERS.posPrivate;    break;
                case  15 : if ( last_id.Name == "public" )     return MODIFIERS.posPublic;     break;
                case 229 : if ( last_id.Name == "sealed" )     return MODIFIERS.posSealed;     break;
                case 556 : if ( last_id.Name == "immutable" )  return MODIFIERS.posImmutable;  break;
                case 321 : if ( last_id.Name == "ref" )        return MODIFIERS.posReference;  break;
                case 247 : if ( last_id.Name == "value" )      return MODIFIERS.posValue;      break;
                case 537 : if ( last_id.Name == "locked" )     return MODIFIERS.posLocked;     break;
                case  85 : if ( last_id.Name == "concurrent" ) return MODIFIERS.posConcurrent; break;
                case 189 : if ( last_id.Name == "barrier" )    return MODIFIERS.posBarrier;    break;
                case 404 : if ( last_id.Name == "get" )        return MODIFIERS.posGetter;     break;
                case  14 : if ( last_id.Name == "set" )        return MODIFIERS.posSetter;     break;
                case 462 : if ( last_id.Name == "shared" )     return MODIFIERS.posShared;     break;
                case 181 : if ( last_id.Name == "protected" )  return MODIFIERS.posProtected;  break;
                case 484:  if ( last_id.Name == "actor")       return MODIFIERS.posActor;      break;
                default  : return -1;
            }
            return -1;
        }

        //===============================================================================

        // NUMERIC LITERALS

        public static double real_value;       // value of the last real literal
        public static long   integer_value;    // value of the last integer literal

        // scanNumber
        // ----------
        //
        // TODO: process end-of-source case.
        //
        private LEXEM scanNumber ( )
        {
            identifier = "";

            while ( isDigit() )
            {
                // Store digits in the buffer
                identifier += current_symbol;
                move();
            }
            if ( current_symbol == '.' )
            {
                move();
                if ( current_symbol == '.' )
                {
                    move_back();
                    goto Out;
                }
                // Real number.
                identifier += '.';
                while ( isDigit() )
                {
                    // Store mantisse digits in the buffer
                    identifier += current_symbol;
                    move();
                }
                if ( current_symbol == 'E' || current_symbol == 'D' )
                {
                    // ScaleFactor
                    identifier += 'E';
                    move();
                    if ( current_symbol == '+' || current_symbol == '-' )
                    {
                        identifier += current_symbol;
                        move();
                    }
                    while ( isDigit() )
                    {
                        identifier += current_symbol;
                        move();
                    }
                }
                try
                {
                    // Culture-independent floating point format.
                    System.Globalization.NumberFormatInfo nfi = new System.Globalization.NumberFormatInfo();
                    // Parsing the actual value using the format created.
                    real_value = Double.Parse(identifier, nfi);
                 // real_value = Single.Parse(identifier);
                }
                catch (Exception e)
                {
                    if (e is OverflowException) ERROR.RealLiteralIsTooBig(identifier);
                    else ERROR.SyntaxErrorIn("real constant", "format should be {D}.D{D}[(+|-)][E{D}]");
                    real_value = 0.0F;
                }
                return LEXEM.Real;
            }
            if ( isHexDigit() )
            {
                // integer in hexadecimal form
                do
                {
                    identifier += current_symbol;
                    move();
                }
                while
                    ( isHexDigit() );

                if ( current_symbol == 'H' )
                {
                    move();
                    calculateHexNumber(false);
                    return LEXEM.Int;
                }
                else if ( current_symbol == 'X' )
                {
                    // character in hexadecimal form
                    // {digit}{hexDigit}X
                    move();
                    calculateHexNumber(true);
                    return LEXEM.String;
                }
                else
                {
                    // lexical error
                    return LEXEM.Int;
                }
            }
            if ( current_symbol == 'X' )
            {
                // character in form {digit}X
                move();
                calculateHexNumber(true);
                return LEXEM.String;
            }
            if (current_symbol == 'H')
            {
                // character in form {digit}X
                move();
                calculateHexNumber(false);
                return LEXEM.Int;
            }
         // else

         Out:
            // just an integer literal
            try
            {
                integer_value = Int32.Parse(identifier);
            }                
            catch ( OverflowException )
            {
                // Constant too big
                ERROR.IntegerLiteralIsTooBig(identifier);
                integer_value = 0;
            }
            catch( Exception )
            {
                ERROR.SyntaxErrorIn(identifier, "integer expected");
            }
            return LEXEM.Int;
        }

        private void calculateHexNumber ( bool forChar )
        {
         // if ( identifier.Length > 2 ) return;
            long w = 0;
            string hex = "0123456789ABCDEF";
            for ( int i=0, n=identifier.Length; i<n; i++ )
            {
                for ( int j=0, k=16; j<k; j++ )
                    if ( hex[j] == identifier[i] ) { w += j; break; }

                if ( i<n-1 ) w = w*16;
            }
            if ( forChar )
            {
                identifier = "";
                identifier += (char)w;
            }
            else
            {
                integer_value = w;
            }
        }

        //===============================================================================

        // getLexem
        // --------
        //
        // suppress_comments:
        //
        //   If the parameter is true then the function ...
        //   This case is for "normal" scanning.
        //
        //   If the parameter is false then the function ...
        //   This case is for using outside compiler
        //   (in ScanTokenAndProvideInfoAboutIt() function).
        //
        // The function is not used directly by the parser; the parser calls
        // wrappers is<Lexem>() and expect<Lexem>() instead (see LEXAN class).
        //
        private LEXEM getLexem ( bool suppress_comments )
        {
          Again:
            skipBlanks();
            lexem_start_position = current_position;

       //     if ( detectUserDefinedOperator() )

            switch ( current_symbol )
            {
                case '\0' :  // END OF FILE
                           current_lexem = LEXEM.EOF; break;

                case '+' : move(); if (current_symbol == '*') { move(); current_lexem = LEXEM.PlusStar; }
                           else { current_lexem = LEXEM.Plus; }
                           break;

                case '-' : move(); current_lexem = LEXEM.Minus; break;

                case '(' : move();
                           if ( current_symbol == '*' )  // comment (* ... *)
                           {
                               move();

							   int res;
							   if (this.scanComments)
                                   res = scanComment ( );
							   else
								   res = scanComment(!suppress_comments);

                               if ( res == -1 )
                               {   // end of file encountered;
                                   // the message has been already issued by scanComment()

                                   if (suppress_comments)  // just go for the next lexem.
                                       current_lexem = LEXEM.EOF;
                                   else
                                       current_lexem = LEXEM.Comment;
                                   comment_nesting_level = 0;
                               }
                               else if ( res == 0 )  // comment has been completely scanned
                               {
                                   if ( suppress_comments )  // just go for the next lexem.
                                       goto Again;

                                   // Stop scanner: we have taken the 'lexem' we wanted.
                                   current_lexem = LEXEM.Comment;
                                   comment_nesting_level = 0;
                               }
                               else // res>0: res represents the comment's nesting level
                               {
                                   current_lexem = LEXEM.Comment;
                                   comment_nesting_level = res;
                               }
                           }
                           else
                               current_lexem = LEXEM.LeftParenth;
                           break;

                case ')' : move(); current_lexem = LEXEM.RightParenth; break;
                case '[' : move(); current_lexem = LEXEM.LeftBracket;  break;
                case ']' : move(); current_lexem = LEXEM.RightBracket; break;
                case '{' : move(); current_lexem = LEXEM.LeftBrace;    break;
                case '}' : move(); current_lexem = LEXEM.RightBrace;   break;
                case ';' : move(); current_lexem = LEXEM.Semicolon;    break;
                case ',' : move(); current_lexem = LEXEM.Comma;        break;

                case '*' : move(); if ( current_symbol=='*' ) { move(); current_lexem = LEXEM.Exponent; }
                                   else                       {         current_lexem = LEXEM.Star;     }
                           break;

                case '=' : move(); current_lexem = LEXEM.Equal;
                           if ( current_symbol == '=' ) // C-like equality...
                           {
                               move();
                               ERROR.ExtraEqual();                          
                           }
                        // else if ( current_symbol == '>' ) // Arrow =>
                        // {
                        //     move(); current_lexem = LEXEM.Arrow;
                        // }
                           break;

                case '^' : move(); current_lexem = LEXEM.Caret;        break;
                case '&' : move(); current_lexem = LEXEM.Ampersand;    break;
                case '#' : move(); current_lexem = LEXEM.NonEqual;     break;
                case '/' : move(); current_lexem = LEXEM.Slash;        break;
            //  case '\'': move(); current_lexem = LEXEM.Apostrophe; break;
                case '\\': move(); current_lexem = LEXEM.BackSlash; break;
                case '~' : move(); current_lexem = LEXEM.Tilde;        break;
                case '|' : move(); current_lexem = LEXEM.Vert;         break;
                case '!' : move(); current_lexem = LEXEM.Exclamation;  break;
                case '?' : move(); current_lexem = LEXEM.Question;     break;  // only for activity protocols

                case ':' : move(); if ( current_symbol=='=' ) { move(); current_lexem = LEXEM.Assign; }
                                   else                       {         current_lexem = LEXEM.Colon;  }
                           break;

               case '.': move(); if (current_symbol == '.') { move(); current_lexem = LEXEM.DotDot; }
                           else if (current_symbol == '*') { move(); current_lexem = LEXEM.DotStar; }
                           else if (current_symbol == '/') { move(); current_lexem = LEXEM.DotSlash; }
                           else if (current_symbol == '=') { move(); current_lexem = LEXEM.DotEqual; }
                           else if (current_symbol == '#') { move(); current_lexem = LEXEM.DotNonEqual; }
                           else if (current_symbol == '<') 
                                { 
                                    move();
                                    if (current_symbol == '=')
                                    { move(); current_lexem = LEXEM.DotLessEqual; }
                                    else current_lexem = LEXEM.DotLess; 
                                }
                           else if (current_symbol == '>')
                           {
                               move();
                               if (current_symbol == '=')
                               { move(); current_lexem = LEXEM.DotGreaterEqual; }
                               else current_lexem = LEXEM.DotGreater;
                           }
                           else { current_lexem = LEXEM.Dot; }
                           break;

                case '<' : move(); if ( current_symbol=='=' ) { move(); current_lexem = LEXEM.LessEqual; }
                                   else                       {         current_lexem = LEXEM.Less;      }
                           break;

                case '>' : move(); if ( current_symbol=='=' ) { move(); current_lexem = LEXEM.GreaterEqual; }
                                   else                       {         current_lexem = LEXEM.Greater;      }
                           break;

                case '"' : move(); scanString('"');  current_lexem = LEXEM.String; break;
                case '\'': move(); scanString('\''); current_lexem = LEXEM.String; break;

                default :

                    if ( isAsciiLetter() || current_symbol == '_' )    // identifier OR keyword
                    {
                        current_lexem = scanIdentifier();
                    }
                    else if ( isDigit() )     // number
                    {
                        current_lexem = scanNumber();
                    }
                    else                      // illegal character
                    {
                        if ( suppress_comments )
                            ERROR.IllegalCharacter(current_symbol,(uint)current_symbol);
                     // else
                     //     -- Don't issue diagnostics if getLexem() is called from outside compiler.

                        current_lexem = LEXEM.NoLexem;
                        move();
                    }
                    break;
            }

            lexem_end_position = current_position /* +1 */;
            return current_lexem;
        }

        //===============================================================================

        // ScanTokenAndProvideInfoAboutIt
        // ------------------------------
        // This is the second overriding function. It returns (via its parameters)
        // the information about current lexem: its coordinates, kind ("class"),
        // and "colouring class".
        //
        // I guess this function will be called by the environment for "colorizing"
        // the source text displaying by the integrated editor.
        //
        // The function uses the basic function of this class: getLexem().
        //
        // Notice that this function should correctly process comments (i.e.,
        // it should issue adequate information about comment fragments in the source).
        // This means that basic getLexem() function should not silently skip comments...
        //
        public override bool ScanTokenAndProvideInfoAboutIt ( TokenInfo tokenInfo, ref int state )
        {
            tokenInfo.trigger = TokenTrigger.None;
            if ( state > 0 )
            {
                // We are inside a comment.
                tokenInfo.startIndex = current_position;
                state = scanComment(true, state);
                tokenInfo.endIndex = current_position - 1;                
                tokenInfo.color = TokenColor.Comment;
                tokenInfo.type = TokenType.Comment;
                return tokenInfo.startIndex <= tokenInfo.endIndex;
            }

            comment_nesting_level = state;
            LEXEM lexem = getLexem(false);
            state = comment_nesting_level;

            switch ( lexem )
            {
                // End of source
                case LEXEM.EOF        :  //  end-of-file "lexem"
                                         return false;

                // "Simple" delimiters
                case LEXEM.Colon      :   //  :
                case LEXEM.Semicolon  :   //  ;
             // case LEXEM.Exclamation:   //  !
             // case LEXEM.Question   :   //  ?
             // case LEXEM.Arrow      :   //  =>
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Delimiter;
                                         break;

                // Parameter separator
                case LEXEM.Comma      :   //  ,
                                         tokenInfo.trigger = TokenTrigger.ParamNext;
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Delimiter;
                                         break;

                // Coupled delimiters: left
                case LEXEM.LeftParenth :  //  (
                case LEXEM.LeftBracket :  //  [
                case LEXEM.LeftBrace   :  //  {
                                         tokenInfo.trigger = TokenTrigger.ParamStart|TokenTrigger.MatchBraces;
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Delimiter;
                                         break;

                // Coupled delimiters: right
                case LEXEM.RightParenth : //  )
                case LEXEM.RightBracket : //  ]
                case LEXEM.RightBrace   : //  }
                                         tokenInfo.trigger = TokenTrigger.ParamEnd|TokenTrigger.MatchBraces;
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Delimiter;
                                         break;

//              case LEXEM.NoLexem :      //  no/error lexem

                // Comment
                case LEXEM.Comment :      //  comment
                                         tokenInfo.color = TokenColor.Comment;
                                         tokenInfo.type = TokenType.Comment;
                                         break;

                // Numeric literals
                case LEXEM.Int    :       //  integer literal
                case LEXEM.Real   :       //  real literal
                                         tokenInfo.color = TokenColor.Number;
                                         tokenInfo.type = TokenType.Literal;
                                         break;

                // String literal
                case LEXEM.String :       //  string & character literal
                                         tokenInfo.color = TokenColor.String;
                                         tokenInfo.type = TokenType.String;
                                         break;

                // Identifiers
                case LEXEM.Id :           //  identifier
                                         tokenInfo.color = TokenColor.Identifier;
                                         tokenInfo.type = TokenType.Identifier;
                                         break;

                // Operators
                case LEXEM.Star         : //  *
                case LEXEM.Equal        : //  =
                case LEXEM.DotEqual     : //  .=
                case LEXEM.Ampersand    : //  &
                case LEXEM.Assign       : //  :=
                case LEXEM.Dot          : //  .
                case LEXEM.DotDot       : //  ..
                case LEXEM.NonEqual     : //  #
                case LEXEM.Less         : //  <
                case LEXEM.LessEqual    : //  <=
                case LEXEM.Greater      : //  >
                case LEXEM.GreaterEqual : //  >=
                case LEXEM.DotNonEqual  : //  .#
                case LEXEM.DotLess      : //  .<
                case LEXEM.DotLessEqual : //  .<=
                case LEXEM.DotGreater   : //  .>
                case LEXEM.DotGreaterEqual : //  .>=
                case LEXEM.Plus         : //  +
                case LEXEM.Minus        : //  -
                case LEXEM.Slash        : //  /
                case LEXEM.PlusStar     : //  +*
                case LEXEM.DotStar      : //  .*
                case LEXEM.DotSlash     : //  ./
                case LEXEM.Apostrophe   : //  '
                case LEXEM.Exclamation  : //  !
                //case LEXEM.StepBy       : //  BY
                case LEXEM.BackSlash    : //  \
                case LEXEM.Tilde        : //  ~
                case LEXEM.Caret        : //  ^
                case LEXEM.Exponent     : //  **
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Operator;
                                         break;

                // "Simple" (decoupled) keywords
                case LEXEM.Import     :   //  IMPORT
                case LEXEM.Refines    :   //  REFINES
                case LEXEM.Const      :   //  CONST
                case LEXEM.Type       :   //  TYPE
                case LEXEM.Var        :   //  VAR
                case LEXEM.Array      :   //  ARRAY
                case LEXEM.Implements :   //  IMPLEMENTS
                case LEXEM.Of         :   //  OF
                case LEXEM.Return     :   //  RETURN   -- Should it be moved to the next group??
                case LEXEM.Exit       :   //  EXIT     -- Should it be moved to the next group??
                case LEXEM.To         :   //  TO
                case LEXEM.By         :   //  BY
                case LEXEM.On         :   //  ON
                case LEXEM.Exception  :   //  EXCEPTION
                case LEXEM.Termination:   //  TERMINATION
                case LEXEM.Self       :   //  SELF
                case LEXEM.Div        :   //  DIV
                case LEXEM.Mod        :   //  MOD
                case LEXEM.Nil        :   //  NIL
                case LEXEM.As         :   //  AS
                case LEXEM.New        :   //  NEW
                case LEXEM.Is         :   //  IS
                case LEXEM.Send       :   //  SEND
                case LEXEM.Receive    :   //  RECEIVE
                case LEXEM.Accept     :   //  ACCEPT
                case LEXEM.From       :   //  FROM
                case LEXEM.Launch     :   //  LAUNCH
                case LEXEM.Await      :   //  AWAIT
                case LEXEM.Math       :   //  MATH
                                         tokenInfo.color = TokenColor.Keyword;
                                         tokenInfo.type = TokenType.Keyword;
                                         break;
                case LEXEM.Sparse     :   //  SPARSE
                                         tokenInfo.color = TokenColor.Keyword;
                                         tokenInfo.type = TokenType.Keyword;
                                         break;

                // Keywords of structured constructs
                case LEXEM.Module     :   //  MODULE
                case LEXEM.Definition :   //  DEFINITION
                case LEXEM.Implementation://  IMPLEMENTATION
                case LEXEM.End        :   //  END
                case LEXEM.Object     :   //  OBJECT
                case LEXEM.Record     :   //  RECORD
                case LEXEM.Procedure  :   //  PROCEDURE
                case LEXEM.Operator   :   //  OPERATOR
                case LEXEM.Activity   :   //  activity
                case LEXEM.Unused     :   //  activity
                case LEXEM.If         :   //  IF
                case LEXEM.Then       :   //  THEN
                case LEXEM.Else       :   //  ELSE
                case LEXEM.Elsif      :   //  ELSIF
                case LEXEM.Case       :   //  CASE
                case LEXEM.While      :   //  WHILE
                case LEXEM.Do         :   //  DO
                case LEXEM.Repeat     :   //  REPEAT
                case LEXEM.Until      :   //  UNTIL
                case LEXEM.Loop       :   //  LOOP
                case LEXEM.For        :   //  FOR
                case LEXEM.In         :   //  IN
                case LEXEM.Or         :   //  OR
                case LEXEM.Begin      :   //  BEGIN
                case LEXEM.Protocol   :   //  protocol
                                         tokenInfo.trigger = TokenTrigger.MatchBraces;
                                         tokenInfo.color = TokenColor.Keyword;
                                         tokenInfo.type = TokenType.Keyword;
                                         break;

                case LEXEM.Vert         : //  |  -- this is NOT operator or delimiter BUT a part of CASE operator
                                         tokenInfo.trigger = TokenTrigger.MatchBraces;
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Operator;
                                         break;

                default :
                                         tokenInfo.color = TokenColor.Text;
                                         tokenInfo.type = TokenType.Delimiter;
                                         break;
            }

            tokenInfo.startIndex = lexem_start_position;
            tokenInfo.endIndex = lexem_end_position-1;
            return true;
        }

        //===============================================================================

        // BASIS for INTERFACE
        // -------------------
        //
        // isLexem
        // expect
        // skipUntil (2 versions)
        // back
        // ---------

        // isLexem
        // -------
        //
        //
        private bool isLexem ( LEXEM lexem )
        {
            if ( current_lexem == LEXEM.NoLexem )
				if (scanComments) 
					current_lexem = getLexem(false);
				else
					current_lexem = getLexem(true);

            if ( current_lexem == lexem ) {
                current_lexem = LEXEM.NoLexem;
#if DEBUG
                if ( CONTEXT.options.Debug )
                {
                    //for (int i=0; i<50; i++ ) System.Console.Write(" ");
                    //System.Console.WriteLine("Lexem passed: {0}\t\t\t{1}",lexem.ToString(),current_line_number);
                }
#endif
                return true;
            }
            else
                return false;
        }

    //  public OPERATOR_DECL isUserDefined ( int priority )
    //  {
    //      return null;
    //  }

        // expect
        // ------
        //
        //
        private bool expect ( LEXEM lexem, string Symbol )
        {
            if ( isLexem(lexem) ) return true;
            // Lexem 'Symbol' expected
            ERROR.LexemExpected(Symbol);  // ERROR.LexemExpected(lexem.ToString());  -- Why not???
            return false;
        }

        // skipBalancingUntil
        // ------------------
        // Is used by RECOVERY.
        //
        public void skipBalancingUntil ( LEXEM lexem )
        {
            int balance = 0;

            if ( current_lexem == LEXEM.NoLexem )
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);

            while ( current_lexem != LEXEM.EOF )
            {
                if ( lexem == LEXEM.RightBrace )
                {
                    if ( current_lexem == LEXEM.LeftBrace )       { balance++; }
                    else if ( current_lexem == LEXEM.RightBrace ) { balance--; }
                }

                if ( current_lexem == lexem && balance == -1 ) return;

				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);
#if DEBUG
                if ( CONTEXT.options.Debug )
                {
                    for (int i=0; i<50; i++ ) System.Console.Write(" ");
                    System.Console.WriteLine("Lexem skipped: {0}",current_lexem.ToString());
                }
#endif
            }
        }

        // skipUntil
        // ---------
        // Is used by RECOVERY.
        //
        public void skipUntil ( LEXEM lexem )
        {
            if ( current_lexem == LEXEM.NoLexem )
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);

            while ( current_lexem != lexem && current_lexem != LEXEM.EOF )
            {
				//current_lexem = getLexem(true);
				current_lexem = getLexem(false);
#if DEBUG
                if ( CONTEXT.options.Debug )
                {
                    for (int i=0; i<50; i++ ) System.Console.Write(" ");
                    System.Console.WriteLine("Lexem skipped: {0}",current_lexem.ToString());
                }
#endif
            }
        }

        // skipUntil
        // ---------
        // Is used by RECOVERY.
        //
        public void skipUntil ( BitArray stopLexems )
        {
            if ( current_lexem == LEXEM.NoLexem )
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);

            bool pass_this_lexem = !stopLexems[(int)current_lexem];

            while ( pass_this_lexem && current_lexem != LEXEM.EOF )
            {
#if DEBUG
                if ( CONTEXT.options.Debug )
                {
                    for (int i=0; i<50; i++ ) System.Console.Write(" ");
                    System.Console.WriteLine("Lexem skipped: {0}",current_lexem.ToString());
                }
#endif
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);
                pass_this_lexem = !stopLexems[(int)current_lexem];
            }
        }

        public void checkFinalizer ( BitArray finalizers, string where, string reason )
        {
            if ( current_lexem == LEXEM.NoLexem )
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);

            if ( finalizers[(int)current_lexem] )
                return; // OK: current lexem is a legal finalizer.

            // Else skipping lexems until one of finalizers.
            ERROR.SyntaxErrorIn(where,reason);
            skipUntil(finalizers);
        }

//      public void addStopSymbol ( ref BitArray followers, LEXEM lexem )
//      {
//          followers[(int)lexem] = true;
//      }

//      public void addStopSymbol ( ref BitArray followers, LEXEM lexem1, LEXEM lexem2 )
//      {
//          followers[(int)lexem1] = true;
//          followers[(int)lexem2] = true;
//      }

        // step
        // ----
        //
        public void step ( )
        {
            this.isLexem(LEXEM.EOF);
        }

        // back
        // ----
        //
        public void back ( LEXEM lexem )
        {
            if ( current_lexem != LEXEM.NoLexem )
                ERROR.SystemErrorIn("back","cannot recover lexem");
            else
            {
                current_lexem = lexem;
#if DEBUG
                if ( CONTEXT.options.Debug )
                {
                    for (int i=0; i<50; i++ ) System.Console.Write(" ");
                    System.Console.WriteLine("Lexem recovered: {0}",current_lexem.ToString());
                }
#endif
            }
        }

        //===============================================================================

        // INTERFACE
        // ---------
        //
        // is<LexemName>
        // expect<LexemName>
        // back<LexemName>
        // -----------------

        public bool isEOF         ( ) { return isLexem(LEXEM.EOF); }

		public bool isComment	  ( ) { return isLexem(LEXEM.Comment); }
        public bool isIdent       ( ) { return isLexem(LEXEM.Id); }
        public bool isString      ( ) { return isLexem(LEXEM.String); }
        public bool isIntNumber   ( ) { return isLexem(LEXEM.Int); }
        public bool isRealNumber  ( ) { return isLexem(LEXEM.Real); }

        public bool isLeftParenth ( ) { return isLexem(LEXEM.LeftParenth); }
        public bool isRightParenth( ) { return isLexem(LEXEM.RightParenth); }
        public bool isLeftBracket ( ) { return isLexem(LEXEM.LeftBracket); }
        public bool isRightBracket( ) { return isLexem(LEXEM.RightBracket); }
        public bool isLeftBrace   ( ) { return isLexem(LEXEM.LeftBrace); }
        public bool isRightBrace  ( ) { return isLexem(LEXEM.RightBrace); }
        public bool isSemicolon   ( ) { return isLexem(LEXEM.Semicolon); }
        public bool isComma       ( ) { return isLexem(LEXEM.Comma); }
        public bool isColon       ( ) { return isLexem(LEXEM.Colon); }
        public bool isStar        ( ) { return isLexem(LEXEM.Star); }
        public bool isEqual       ( ) { return isLexem(LEXEM.Equal); }
        public bool isDotEqual    ( ) { return isLexem(LEXEM.DotEqual); }
        public bool isCaret       ( ) { return isLexem(LEXEM.Caret); }
        public bool isAmpersand   ( ) { return isLexem(LEXEM.Ampersand); }
        public bool isAssign      ( ) { return isLexem(LEXEM.Assign); }
        public bool isDot         ( ) { return isLexem(LEXEM.Dot); }
        public bool isDotDot      ( ) { return isLexem(LEXEM.DotDot); }
        public bool isNonEqual    ( ) { return isLexem(LEXEM.NonEqual); }
        public bool isLess        ( ) { return isLexem(LEXEM.Less); }
        public bool isLessEqual   ( ) { return isLexem(LEXEM.LessEqual); }
        public bool isGreater     ( ) { return isLexem(LEXEM.Greater); }
        public bool isGreaterEqual( ) { return isLexem(LEXEM.GreaterEqual); }
        public bool isDotNonEqual ( ) { return isLexem(LEXEM.DotNonEqual); }
        public bool isDotLess     ( ) { return isLexem(LEXEM.DotLess); }
        public bool isDotLessEqual( ) { return isLexem(LEXEM.DotLessEqual); }
        public bool isDotGreater  ( ) { return isLexem(LEXEM.DotGreater); }
        public bool isDotGreaterEqual( ) { return isLexem(LEXEM.DotGreaterEqual); }
        public bool isPlus        ( ) { return isLexem(LEXEM.Plus); }
        public bool isMinus       ( ) { return isLexem(LEXEM.Minus); }
        public bool isSlash       ( ) { return isLexem(LEXEM.Slash); }
        public bool isPlusStar    ( ) { return isLexem(LEXEM.PlusStar); }
        public bool isDotStar     ( ) { return isLexem(LEXEM.DotStar); }
        public bool isDotSlash    ( ) { return isLexem(LEXEM.DotSlash); }
        public bool isApostrophe  ( ) { return isLexem(LEXEM.Apostrophe); }
     // public bool isStepBy      ( ) { return isLexem(LEXEM.StepBy); }
        public bool isBackSlash   ( ) { return isLexem(LEXEM.BackSlash); }
        public bool isTilde       ( ) { return isLexem(LEXEM.Tilde); }
        public bool isVert        ( ) { return isLexem(LEXEM.Vert); }
     // public bool isArrow       ( ) { return isLexem(LEXEM.Arrow); }
        public bool isExponent    ( ) { return isLexem(LEXEM.Exponent); }

        public bool isModule      ( ) { return isLexem(LEXEM.Module); }
        public bool isImport      ( ) { return isLexem(LEXEM.Import); }
        public bool isDefinition  ( ) { return isLexem(LEXEM.Definition); }
        public bool isRefines     ( ) { return isLexem(LEXEM.Refines); }
        public bool isEnd         ( ) { return isLexem(LEXEM.End); }
        public bool isConst       ( ) { return isLexem(LEXEM.Const); }
        public bool isType        ( ) { return isLexem(LEXEM.Type); }
        public bool isVar         ( ) { return isLexem(LEXEM.Var); }
        public bool isArray       ( ) { return isLexem(LEXEM.Array); }
        public bool isObject      ( ) { return isLexem(LEXEM.Object); }
        public bool isRecord      ( ) { return isLexem(LEXEM.Record); }
        public bool isImplements  ( ) { return isLexem(LEXEM.Implements); }
        public bool isProcedure   ( ) { return isLexem(LEXEM.Procedure); }
        public bool isOf          ( ) { return isLexem(LEXEM.Of); }
        public bool isExit        ( ) { return isLexem(LEXEM.Exit); }
        public bool isReturn      ( ) { return isLexem(LEXEM.Return); }
        public bool isIf          ( ) { return isLexem(LEXEM.If); }
        public bool isThen        ( ) { return isLexem(LEXEM.Then); }
        public bool isElse        ( ) { return isLexem(LEXEM.Else); }
        public bool isElsif       ( ) { return isLexem(LEXEM.Elsif); }
        public bool isCase        ( ) { return isLexem(LEXEM.Case); }
        public bool isWhile       ( ) { return isLexem(LEXEM.While); }
        public bool isDo          ( ) { return isLexem(LEXEM.Do); }
        public bool isRepeat      ( ) { return isLexem(LEXEM.Repeat); }
        public bool isUntil       ( ) { return isLexem(LEXEM.Until); }
        public bool isLoop        ( ) { return isLexem(LEXEM.Loop); }
        public bool isMath        ( ) { return isLexem(LEXEM.Math); }
        public bool isSparse      ( ) { return isLexem(LEXEM.Sparse); }
        public bool isFor         ( ) { return isLexem(LEXEM.For); }
        public bool isTo          ( ) { return isLexem(LEXEM.To); }
        public bool isBy          ( ) { return isLexem(LEXEM.By); }
        public bool isIn          ( ) { return isLexem(LEXEM.In); }
        public bool isOr          ( ) { return isLexem(LEXEM.Or); }
        public bool isDiv         ( ) { return isLexem(LEXEM.Div); }
        public bool isMod         ( ) { return isLexem(LEXEM.Mod); }
        public bool isNil         ( ) { return isLexem(LEXEM.Nil); }
        public bool isOn          ( ) { return isLexem(LEXEM.On); }
        public bool isException   ( ) { return isLexem(LEXEM.Exception); }
        public bool isTermination ( ) { return isLexem(LEXEM.Termination); }
        public bool isBegin       ( ) { return isLexem(LEXEM.Begin); }
        public bool isSelf        ( ) { return isLexem(LEXEM.Self); }
        public bool isImplementation(){ return isLexem(LEXEM.Implementation); }
        public bool isAs          ( ) { return isLexem(LEXEM.As); }
        public bool isActivity    ( ) { return isLexem(LEXEM.Activity); }
        public bool isNew         ( ) { return isLexem(LEXEM.New); }
        public bool isOperator    ( ) { return isLexem(LEXEM.Operator); }
        public bool isAwait       ( ) { return isLexem(LEXEM.Await); }
        public bool isIs          ( ) { return isLexem(LEXEM.Is); }
        public bool isQuestion    ( ) { return isLexem(LEXEM.Question); }
        public bool isExclamation ( ) { return isLexem(LEXEM.Exclamation); }
        public bool isSend        ( ) { return isLexem(LEXEM.Send); }
        public bool isReceive     ( ) { return isLexem(LEXEM.Receive); }
        public bool isAccept      ( ) { return isLexem(LEXEM.Accept); }
        public bool isFrom        ( ) { return isLexem(LEXEM.From); }
        public bool isLaunch      ( ) { return isLexem(LEXEM.Launch); }
        public bool isProtocol    ( ) { return isLexem(LEXEM.Protocol); }
        public bool isUnused      ( ) { return isLexem(LEXEM.Unused); }

        public bool expectIdent        ( ) { return expect(LEXEM.Id,     "identifier"); }
        public bool expectString       ( ) { return expect(LEXEM.String, "string"); }
        public bool expectInt          ( ) { return expect(LEXEM.Int,    "int number"); }
        public bool expectReal         ( ) { return expect(LEXEM.Real,   "real number"); }

        public bool expectLeftParenth  ( ) { return expect(LEXEM.LeftParenth, "("); }
        public bool expectRightParenth ( ) { return expect(LEXEM.RightParenth,")"); }
        public bool expectLeftBracket  ( ) { return expect(LEXEM.LeftBracket, "["); }
        public bool expectRightBracket ( ) { return expect(LEXEM.RightBracket,"]"); }
        public bool expectLeftBrace    ( ) { return expect(LEXEM.LeftBrace,   "{"); }
        public bool expectRightBrace   ( ) { return expect(LEXEM.RightBrace,  "}"); }
        public bool expectSemicolon    ( ) { return expect(LEXEM.Semicolon,   ";"); }
        public bool expectComma        ( ) { return expect(LEXEM.Comma,       ","); }
        public bool expectColon        ( ) { return expect(LEXEM.Colon,       ":"); }
        public bool expectStar         ( ) { return expect(LEXEM.Star,        "*"); }
        public bool expectEqual        ( ) { return expect(LEXEM.Equal,       "="); }
        public bool expectDotEqual     ( ) { return expect(LEXEM.DotEqual,    ".="); }
    //  public bool expectCaret        ( ) { return expect(LEXEM.Caret,       "^"); }
        public bool expectAmpersand    ( ) { return expect(LEXEM.Ampersand,   "&"); }
        public bool expectAssign       ( ) { return expect(LEXEM.Assign,      ":="); }
        public bool expectDot          ( ) { return expect(LEXEM.Dot,         "."); }
        public bool expectDotDot       ( ) { return expect(LEXEM.DotDot,      ".."); }
        public bool expectNonEqual     ( ) { return expect(LEXEM.NonEqual,    "#"); }
        public bool expectLess         ( ) { return expect(LEXEM.Less,        "<"); }
        public bool expectLessEqual    ( ) { return expect(LEXEM.LessEqual,   "<="); }
        public bool expectGreater      ( ) { return expect(LEXEM.Greater,     ">"); }
        public bool expectGreaterEqual ( ) { return expect(LEXEM.GreaterEqual,">="); }
        public bool expectDotNonEqual  ( ) { return expect(LEXEM.DotNonEqual, ".#"); }
        public bool expectDotLess      ( ) { return expect(LEXEM.DotLess,     ".<"); }
        public bool expectDotLessEqual ( ) { return expect(LEXEM.DotLessEqual,".<="); }
        public bool expectDotGreater   ( ) { return expect(LEXEM.DotGreater,  ".>"); }
        public bool expectDotGreaterEqual(){ return expect(LEXEM.DotGreaterEqual,".>="); }
        public bool expectPlus         ( ) { return expect(LEXEM.Plus,        "+"); }
        public bool expectMinus        ( ) { return expect(LEXEM.Minus,       "-"); }
        public bool expectSlash        ( ) { return expect(LEXEM.Slash,       "/"); }
        public bool expectPlusStar     ( ) { return expect(LEXEM.PlusStar,    "+*"); }
        public bool expectDotStar      ( ) { return expect(LEXEM.DotStar,     ".*"); }
        public bool expectDotSlash     ( ) { return expect(LEXEM.DotSlash,    "./"); }
        public bool expectApostrophe   ( ) { return expect(LEXEM.Apostrophe,  "'"); }
        public bool expectExclamation  ( ) { return expect(LEXEM.Exclamation, "!"); }
     // public bool expectStepBy       ( ) { return expect(LEXEM.StepBy,      "by"); }
        public bool expectBackSlash    ( ) { return expect(LEXEM.BackSlash,   "\\"); }
        public bool expectTilde        ( ) { return expect(LEXEM.Tilde,       "~"); }
        public bool expectVert         ( ) { return expect(LEXEM.Vert,        "|"); }
     // public bool expectArrow        ( ) { return expect(LEXEM.Arrow,       "=>"); }

        public bool expectModule       ( ) { return expect(LEXEM.Module,     "module"); }
        public bool expectImport       ( ) { return expect(LEXEM.Import,     "import"); }
        public bool expectDefinition   ( ) { return expect(LEXEM.Definition, "definition"); }
        public bool expectRefines      ( ) { return expect(LEXEM.Refines,    "refines"); }
        public bool expectEnd          ( ) { return expect(LEXEM.End,        "end"); }
        public bool expectConst        ( ) { return expect(LEXEM.Const,      "const"); }
        public bool expectType         ( ) { return expect(LEXEM.Type,       "type"); }
        public bool expectVar          ( ) { return expect(LEXEM.Var,        "var"); }
        public bool expectArray        ( ) { return expect(LEXEM.Array,      "array"); }
        public bool expectObject       ( ) { return expect(LEXEM.Object,     "object"); }
        public bool expectRecord       ( ) { return expect(LEXEM.Record,     "record"); }
        public bool expectImplements   ( ) { return expect(LEXEM.Implements, "implements"); }
        public bool expectProcedure    ( ) { return expect(LEXEM.Procedure,  "procedure"); }
        public bool expectOf           ( ) { return expect(LEXEM.Of,         "of"); }
        public bool expectExit         ( ) { return expect(LEXEM.Exit,       "exit"); }
        public bool expectReturn       ( ) { return expect(LEXEM.Return,     "return"); }
        public bool expectIf           ( ) { return expect(LEXEM.If,         "if"); }
        public bool expectThen         ( ) { return expect(LEXEM.Then,       "then"); }
        public bool expectElse         ( ) { return expect(LEXEM.Else,       "else"); }
        public bool expectElsif        ( ) { return expect(LEXEM.Elsif,      "elsif"); }
        public bool expectCase         ( ) { return expect(LEXEM.Case,       "case"); }
        public bool expectWhile        ( ) { return expect(LEXEM.While,      "while"); }
        public bool expectDo           ( ) { return expect(LEXEM.Do,         "do"); }
        public bool expectRepeat       ( ) { return expect(LEXEM.Repeat,     "repeat"); }
        public bool expectUntil        ( ) { return expect(LEXEM.Until,      "until"); }
        public bool expectLoop         ( ) { return expect(LEXEM.Loop,       "loop"); }
        public bool expectMath         ( ) { return expect(LEXEM.Math,       "math"); }
        public bool expectSparse       ( ) { return expect(LEXEM.Sparse,     "sparse"); }
        public bool expectFor          ( ) { return expect(LEXEM.For,        "for"); }
        public bool expectTo           ( ) { return expect(LEXEM.To,         "to"); }
        public bool expectBy           ( ) { return expect(LEXEM.By,         "by"); }
        public bool expectIn           ( ) { return expect(LEXEM.In,         "in"); }
        public bool expectOr           ( ) { return expect(LEXEM.Or,         "or"); }
        public bool expectDiv          ( ) { return expect(LEXEM.Div,        "div"); }
        public bool expectMod          ( ) { return expect(LEXEM.Mod,        "mod"); }
        public bool expectNil          ( ) { return expect(LEXEM.Nil,        "nil"); }
        public bool expectOn           ( ) { return expect(LEXEM.On,         "on"); }
        public bool expectException    ( ) { return expect(LEXEM.Exception,  "exception"); }
        public bool expectTermination  ( ) { return expect(LEXEM.Termination,"termination"); }
        public bool expectBegin        ( ) { return expect(LEXEM.Begin,      "begin"); }
        public bool expectSelf         ( ) { return expect(LEXEM.Self,       "self"); }
        public bool expectImplementation() { return expect(LEXEM.Implementation,"implementation"); }
        public bool expectAs           ( ) { return expect(LEXEM.As,         "as"); }
        public bool expectActivity     ( ) { return expect(LEXEM.Activity,   "activity"); }

        public void backBegin ( )     { back(LEXEM.Begin); }
        public void backEnd   ( )     { back(LEXEM.End); }
        public void backSemicolon ( ) { back(LEXEM.Semicolon); }
        public void backOn ( )        { back(LEXEM.On); }

        public bool checkEndOfStatement ( )
        {
            if ( current_lexem == LEXEM.NoLexem )
				current_lexem = getLexem(true);
				//current_lexem = getLexem(false);

            switch ( current_lexem )
            {
                case LEXEM.End  :
                case LEXEM.Else :
                case LEXEM.Elsif:
                case LEXEM.On   :
                case LEXEM.Until:
                case LEXEM.Vert :
                      //  back(current_lexem); -- it's not necessary because we didn't forget the lexem!
                          return true;

                default : return false;
            }
        }


    }  // class SCANNER

}  // namespace ETH.Zonnon.Compiler

