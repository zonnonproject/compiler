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

namespace ETH.Zonnon
{
    // LEXEM
    // -----
    // Aeon Lexem Codes
    //
    public enum LEXEM : int
    {
        NoLexem,       //  no/error lexem
        EOF,           //  end-of-file "lexem"
        Comment,       //  comment

        Id,            //  identifier

        Int,           //  integer literal
        Real,          //  real literal
        String,        //  string & character literal

        LeftParenth,   //  (
        RightParenth,  //  )
        LeftBracket,   //  [
        RightBracket,  //  ]
        LeftBrace,     //  {
        RightBrace,    //  }
        Semicolon,     //  ;
        Comma,         //  ,
        Colon,         //  :
        Star,          //  *
        Equal,         //  =
        DotEqual,      //  .=
        Caret,         //  ^
        Ampersand,     //  &
        Exponent,      //  **
        Assign,        //  :=
        Dot,           //  .
        DotDot,        //  ..
        NonEqual,      //  #
        Less,          //  <
        LessEqual,     //  <=
        Greater,       //  >
        GreaterEqual,  //  >=
        DotNonEqual,   //  .#
        DotLess,       //  .<
        DotLessEqual,  //  .<=
        DotGreater,    //  .>
        DotGreaterEqual,  //  .>=
        Plus,          //  +
        Minus,         //  -
        Slash,         //  /
        PlusStar,      //  +*  
        DotStar,       //  .*
        DotSlash,      //  ./ 
        Apostrophe,    //  '
        StepBy,        //  BY
        BackSlash,     //  \
        Tilde,         //  ~
        Vert,          //  |
        Exclamation,   //  !
        Question,      //  ?
//      Arrow,         //  =>

        Module,        //  MODULE
        Import,        //  IMPORT
        Definition,    //  DEFINITION
        Refines,       //  REFINES
        End,           //  END
        Const,         //  CONST
        Type,          //  TYPE
        Var,           //  VAR
        Array,         //  ARRAY
    //  Record,        //  RECORD
        Object,        //  OBJECT
        Record,        //  RECORD  ----------
        Implements,    //  IMPLEMENTS
        Procedure,     //  PROCEDURE
        Of,            //  OF
        Exit,          //  EXIT
        Return,        //  RETURN
        If,            //  IF
        Is,            //  IS
        Then,          //  THEN
        Else,          //  ELSE
        Elsif,         //  ELSIF
        Case,          //  CASE
        While,         //  WHILE
        Do,            //  DO
        Repeat,        //  REPEAT
        Until,         //  UNTIL
        Loop,          //  LOOP
        For,           //  FOR
        To,            //  TO
        By,            //  BY
        In,            //  IN
        Or,            //  OR
        Div,           //  DIV
        Mod,           //  MOD
        Nil,           //  NIL
        On,            //  ON
        Exception,     //  EXCEPTION
        Termination,   //  TERMINATION
        Begin,         //  BEGIN
    //  Initializer,   //  INITIALIZER
        Self,          //  SELF
        Implementation,//  IMPLEMENTATION
        As,            //  AS
    //  Exports,       //  EXPORTS
    //  Extends        //  EXTENDS
        Activity,      //  ACTIVITY
        New,           //  NEW
        Operator,      //  OPERATOR
        Await,         //  AWAIT
        Send,          //  SEND
        Accept,        //  ACCEPT
        Launch,        //  LAUNCH
        Receive,       //  RECEIVE
        From,          //  FROM
     // Dialog,        //  dialog
        Protocol,      //  protocol
        Unused,        //  unused
        Math,          //  IsMath
        Sparse,        //  sparse

        UserDefined,   //  A user-defined operator
    };

}  // namespace ETH.Zonnon.Compiler
