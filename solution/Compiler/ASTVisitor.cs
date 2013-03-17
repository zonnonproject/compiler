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
using System.Text;

namespace ETH.Zonnon
{

    public class ASTVisitor
    {
        public ASTVisitor()
        {
        }

        public virtual void Visit(NODE node)
        {
            if (!Visit_Before(node)) return; 
            switch(node.ASTNodeType){
                case ASTNodeType.STATEMENT_LIST:
                    Visit_STATEMENT_LIST((STATEMENT_LIST)node);
                    break;
                case ASTNodeType.IDENT_LIST:
                    Visit_IDENT_LIST((IDENT_LIST)node);
                    break;
                case ASTNodeType.COMMENT:
                    Visit_COMMENT((COMMENT)node);
                    break;
                case ASTNodeType.COMMENT_LIST:
                    Visit_COMMENT_LIST((COMMENT_LIST)node);
                    break;
                case ASTNodeType.IF_PAIR:
                    Visit_IF_PAIR((IF_PAIR)node);
                    break;
                case ASTNodeType.CASE_ITEM:
                    Visit_CASE_ITEM((CASE_ITEM)node);
                    break;
                case ASTNodeType.RANGE:
                    Visit_RANGE((RANGE)node);
                    break;
                case ASTNodeType.EXCEPTION:
                    Visit_EXCEPTION((EXCEPTION)node);
                    break;
  // Group TYPE:
                case ASTNodeType.ANY_TYPE:
                    Visit_ANY_TYPE((ANY_TYPE)node);
                    break;
                case ASTNodeType.UNKNOWN_TYPE:
                    Visit_UNKNOWN_TYPE((UNKNOWN_TYPE)node);
                    break;
                case ASTNodeType.ARRAY_TYPE:
                    Visit_ARRAY_TYPE((ARRAY_TYPE)node);
                    break;
                case ASTNodeType.SET_TYPE:
                    Visit_SET_TYPE((SET_TYPE)node);
                    break;
                case ASTNodeType.RANGE_TYPE:
                    Visit_RANGE_TYPE((RANGE_TYPE)node);
                    break;
                case ASTNodeType.INTERFACE_TYPE:
                    Visit_INTERFACE_TYPE((INTERFACE_TYPE)node);
                    break;
                case ASTNodeType.OBJECT_TYPE:
                    Visit_OBJECT_TYPE((OBJECT_TYPE)node);
                    break;
                case ASTNodeType.ACTIVITY_TYPE:
                    Visit_ACTIVITY_TYPE((ACTIVITY_TYPE)node);
                    break;
                case ASTNodeType.ABSTRACT_ACTIVITY_TYPE:
                    Visit_ABSTRACT_ACTIVITY_TYPE((ABSTRACT_ACTIVITY_TYPE)node);
                    break;
                case ASTNodeType.INTEGER_TYPE:
                    Visit_INTEGER_TYPE((INTEGER_TYPE)node);
                    break;
                case ASTNodeType.REAL_TYPE:
                    Visit_REAL_TYPE((REAL_TYPE)node);
                    break;
                case ASTNodeType.FIXED_TYPE:
                    Visit_FIXED_TYPE((FIXED_TYPE)node);
                    break;
                case ASTNodeType.CHAR_TYPE:
                    Visit_CHAR_TYPE((CHAR_TYPE)node);
                    break;
                case ASTNodeType.STRING_TYPE:
                    Visit_STRING_TYPE((STRING_TYPE)node);
                    break;
                case ASTNodeType.CARDINAL_TYPE:
                    Visit_CARDINAL_TYPE((CARDINAL_TYPE)node);
                    break;
                case ASTNodeType.BOOLEAN_TYPE:
                    Visit_BOOLEAN_TYPE((BOOLEAN_TYPE)node);
                    break;
                case ASTNodeType.VOID_TYPE:
                    Visit_VOID_TYPE((VOID_TYPE)node);
                    break;
                case ASTNodeType.ENUM_TYPE:
                    Visit_ENUM_TYPE((ENUM_TYPE)node);
                    break;
                case ASTNodeType.PROC_TYPE:
                    Visit_PROC_TYPE((PROC_TYPE)node);
                    break;
                case ASTNodeType.EXTERNAL_TYPE:
                    Visit_EXTERNAL_TYPE((EXTERNAL_TYPE)node);
                    break;
        // Group EXPRESSION:            
            // Sub Group UNARY:
                case ASTNodeType.NEGATION:
                    Visit_NEGATION((NEGATION)node);
                    break;
                case ASTNodeType.UNARY_MINUS:
                    Visit_UNARY_MINUS((UNARY_MINUS)node);
                    break;
                case ASTNodeType.UNARY_PLUS:
                    Visit_UNARY_PLUS((UNARY_PLUS)node);
                    break;
                case ASTNodeType.TYPE_CONV:
                    Visit_TYPE_CONV((TYPE_CONV)node);
                    break;
            // Sub Group BINARY:
                case ASTNodeType.ASSIGNMENT_OPERATOR:
                    Visit_ASSIGNMENT_OPERATOR((ASSIGNMENT_OPERATOR)node);
                    break;
                case ASTNodeType.EXPR_ARRAY_ASSIGNMENT:
                    Visit_EXPR_ARRAY_ASSIGNMENT((EXPR_ARRAY_ASSIGNMENT)node);
                    break;  
                case ASTNodeType.PLUS:
                    Visit_PLUS((PLUS)node);
                    break;
                case ASTNodeType.MINUS:
                    Visit_MINUS((MINUS)node);
                    break;
                case ASTNodeType.MULTIPLY:
                    Visit_MULTIPLY((MULTIPLY)node);
                    break;
                case ASTNodeType.DIVIDE:
                    Visit_DIVIDE((DIVIDE)node);
                    break;                
                case ASTNodeType.TRANSPOSE:
                    Visit_TRANSPOSE((TRANSPOSE)node);
                    break;
                case ASTNodeType.RANGESTEP:
                    Visit_RANGESTEP((RANGESTEP)node);
                    break;
                case ASTNodeType.ARRAY_RANGE:
                    Visit_ARRAY_RANGE((ARRAY_RANGE)node);
                    break;
                case ASTNodeType.LEFTDIVISION:
                    Visit_LEFTDIVISION((LEFTDIVISION)node);
                    break;
                case ASTNodeType.DIV:
                    Visit_DIV((DIV)node);
                    break;
                case ASTNodeType.MOD:
                    Visit_MOD((MOD)node);
                    break;
                case ASTNodeType.EXPONENT:
                    Visit_EXPONENT((EXPONENT)node);
                    break;
                case ASTNodeType.AND:
                    Visit_AND((AND)node);
                    break;
                case ASTNodeType.OR:
                    Visit_OR((OR)node);
                    break;
                // Sub Sub Group RELATION
                case ASTNodeType.EQUAL:
                    Visit_EQUAL((EQUAL)node);
                    break;
                case ASTNodeType.NON_EQUAL:
                    Visit_NON_EQUAL((NON_EQUAL)node);
                    break;
                case ASTNodeType.LESS:
                    Visit_LESS((LESS)node);
                    break;
                case ASTNodeType.LESS_EQUAL:
                    Visit_LESS_EQUAL((LESS_EQUAL)node);
                    break;
                case ASTNodeType.GREATER:
                    Visit_GREATER((GREATER)node);
                    break;
                case ASTNodeType.GREATER_EQUAL:
                    Visit_GREATER_EQUAL((GREATER_EQUAL)node);
                    break;
                case ASTNodeType.EQUAL_ELEMENTWISE:
                    Visit_EQUAL_ELEMENTWISE((EQUAL_ELEMENTWISE)node);
                    break;
                case ASTNodeType.NON_EQUAL_ELEMENTWISE:
                    Visit_NON_EQUAL_ELEMENTWISE((NON_EQUAL_ELEMENTWISE)node);
                    break;
                case ASTNodeType.LESS_ELEMENTWISE:
                    Visit_LESS_ELEMENTWISE((LESS_ELEMENTWISE)node);
                    break;
                case ASTNodeType.LESS_EQUAL_ELEMENTWISE:
                    Visit_LESS_EQUAL_ELEMENTWISE((LESS_EQUAL_ELEMENTWISE)node);
                    break;
                case ASTNodeType.GREATER_ELEMENTWISE:
                    Visit_GREATER_ELEMENTWISE((GREATER_ELEMENTWISE)node);
                    break;
                case ASTNodeType.GREATER_EQUAL_ELEMENTWISE:
                    Visit_GREATER_EQUAL_ELEMENTWISE((GREATER_EQUAL_ELEMENTWISE)node);
                    break;
                case ASTNodeType.IN:
                    Visit_IN((IN)node);
                    break;
                case ASTNodeType.IMPLEMENTS:
                    Visit_IMPLEMENTS((IMPLEMENTS)node);
                    break;
                case ASTNodeType.IS:
                    Visit_IS((IS)node);
                    break;
            // Sub Group DESIGNATOR:
                case ASTNodeType.DEREFERENCE:
                    Visit_DEREFERENCE((DEREFERENCE)node);
                    break;
                case ASTNodeType.INDEXER:
                    Visit_INDEXER((INDEXER)node);
                    break;
                case ASTNodeType.SELECTOR:
                    Visit_SELECTOR((SELECTOR)node);
                    break;
                case ASTNodeType.SAFEGUARD:
                    Visit_SAFEGUARD((SAFEGUARD)node);
                    break;
                case ASTNodeType.CALL:
                    Visit_CALL((CALL)node);
                    break;
                case ASTNodeType.SET_CTOR:
                    Visit_SET_CTOR((SET_CTOR)node);
                    break;
                case ASTNodeType.NEW:
                    Visit_NEW((NEW)node);
                    break;
                case ASTNodeType.SELF:
                    Visit_SELF((SELF)node);
                    break;
                case ASTNodeType.OBJECT:
                    Visit_OBJECT((OBJECT)node);
                    break;
                case ASTNodeType.INSTANCE:
                    Visit_INSTANCE((INSTANCE)node);
                    break;
        // Sub Group LITERAL:
                case ASTNodeType.ENUMERATOR:
                    Visit_ENUMERATOR((ENUMERATOR)node);
                    break;
                case ASTNodeType.STRING_LITERAL:
                    Visit_STRING_LITERAL((STRING_LITERAL)node);
                    break;
                case ASTNodeType.CHAR_LITERAL:
                    Visit_CHAR_LITERAL((CHAR_LITERAL)node);
                    break;
                case ASTNodeType.INTEGER_LITERAL:
                    Visit_INTEGER_LITERAL((INTEGER_LITERAL)node);
                    break;
                case ASTNodeType.REAL_LITERAL:
                    Visit_REAL_LITERAL((REAL_LITERAL)node);
                    break;
                case ASTNodeType.CCI_LITERAL:
                    Visit_CCI_LITERAL((CCI_LITERAL)node);
                    break;
                case ASTNodeType.NULL:
                    Visit_NULL((NULL)node);
                    break;
        // Group STATEMENT:
                case ASTNodeType.ASSIGNMENT:
                    Visit_ASSIGNMENT((ASSIGNMENT)node);
                    break;
                case ASTNodeType.CALL_STMT:
                    Visit_CALL_STMT((CALL_STMT)node);
                    break;
                case ASTNodeType.EXIT:
                    Visit_EXIT((EXIT)node);
                    break;
                case ASTNodeType.RETURN:
                    Visit_RETURN((RETURN)node);
                    break;
                case ASTNodeType.REPLY:
                    Visit_REPLY((REPLY)node);
                    break;
                case ASTNodeType.AWAIT:
                    Visit_AWAIT((AWAIT)node);
                    break;
                case ASTNodeType.SEND_RECEIVE:
                    Visit_SEND_RECEIVE((SEND_RECEIVE)node);
                    break;
                case ASTNodeType.LAUNCH:
                    Visit_LAUNCH((LAUNCH)node);
                    break;
                case ASTNodeType.ACCEPT:
                    Visit_ACCEPT((ACCEPT)node);
                    break;
                case ASTNodeType.IF:
                    Visit_IF((IF)node);
                    break;
                case ASTNodeType.CASE:
                    Visit_CASE((CASE)node);
                    break;
                case ASTNodeType.BLOCK:
                    Visit_BLOCK((BLOCK)node);
                    break;
            // Sub Group CYCLE:
                case ASTNodeType.FOR:
                    Visit_FOR((FOR)node);
                    break;
                case ASTNodeType.WHILE:
                    Visit_WHILE((WHILE)node);
                    break;
                case ASTNodeType.REPEAT:
                    Visit_REPEAT((REPEAT)node);
                    break;
                case ASTNodeType.LOOP:
                    Visit_LOOP((LOOP)node);
                    break;
        // Group DECLARATION:
            // Sub Group UNIT_DECL:
                case ASTNodeType.UNKNOWN_DECL:
                    Visit_UNKNOWN_DECL((UNKNOWN_DECL)node);
                    break;
                case ASTNodeType.EXTERNAL_DECL:
                    Visit_EXTERNAL_DECL((EXTERNAL_DECL)node);
                    break;
                case ASTNodeType.NAMESPACE_DECL:
                    Visit_NAMESPACE_DECL((NAMESPACE_DECL)node);
                    break;
                case ASTNodeType.MODULE_DECL:
                    Visit_MODULE_DECL((MODULE_DECL)node);
                    break;
                case ASTNodeType.OBJECT_DECL:
                    Visit_OBJECT_DECL((OBJECT_DECL)node);
                    break;
                case ASTNodeType.DEFINITION_DECL:
                    Visit_DEFINITION_DECL((DEFINITION_DECL)node);
                    break;
                case ASTNodeType.IMPLEMENTATION_DECL:
                    Visit_IMPLEMENTATION_DECL((IMPLEMENTATION_DECL)node);
                    break;
            // Sub Group ROUTINE_DECL:
                case ASTNodeType.PROCEDURE_DECL:
                    Visit_PROCEDURE_DECL((PROCEDURE_DECL)node);
                    break;
                case ASTNodeType.OPERATOR_DECL:
                    Visit_OPERATOR_DECL((OPERATOR_DECL)node);
                    break;
                case ASTNodeType.ACTIVITY_DECL:
                    Visit_ACTIVITY_DECL((ACTIVITY_DECL)node);
                    break;
            // Sub Group SIMPLE_DECL:
                case ASTNodeType.IMPORT_DECL:
                    Visit_IMPORT_DECL((IMPORT_DECL)node);
                    break;
            // Sub Sub Group    VARIABLE_DECL:
                case ASTNodeType.PARAMETER_DECL:
                    Visit_PARAMETER_DECL((PARAMETER_DECL)node);
                    break;
                case ASTNodeType.LOCAL_DECL:
                    Visit_LOCAL_DECL((LOCAL_DECL)node);
                    break;
                case ASTNodeType.FIELD_DECL:
                    Visit_FIELD_DECL((FIELD_DECL)node);
                    break;
                case ASTNodeType.CONSTANT_DECL:
                    Visit_CONSTANT_DECL((CONSTANT_DECL)node);
                    break;
                case ASTNodeType.TYPE_DECL:
                    Visit_TYPE_DECL((TYPE_DECL)node);
                    break;
                case ASTNodeType.ENUMERATOR_DECL:
                    Visit_ENUMERATOR_DECL((ENUMERATOR_DECL)node);
                    break;
                case ASTNodeType.PROTOCOL_DECL:
                    Visit_PROTOCOL_DECL((PROTOCOL_DECL)node);
                    break;
        // Group EXTENSION
                case ASTNodeType.SYNTAX:
                    Visit_SYNTAX((SYNTAX)node);
                    break;
                case ASTNodeType.PRODUCTION:
                    Visit_PRODUCTION((PRODUCTION)node);
                    break;
            // Sub Group UNIT:
                case ASTNodeType.TERMINAL:
                    Visit_TERMINAL((TERMINAL)node);
                    break;
                case ASTNodeType.TYPE_NAME:
                    Visit_TYPE_NAME((TYPE_NAME)node);
                    break;
                case ASTNodeType.CONSTANT:
                    Visit_CONSTANT((CONSTANT)node);
                    break;
                case ASTNodeType.NONTERMINAL:
                    Visit_NONTERMINAL((NONTERMINAL)node);
                    break;
                case ASTNodeType.UNKNOWN_NONTERMINAL:
                    Visit_UNKNOWN_NONTERMINAL((UNKNOWN_NONTERMINAL)node);
                    break;
                case ASTNodeType.SEQUENCE:
                    Visit_SEQUENCE((SEQUENCE)node);
                    break;
                case ASTNodeType.ALTERNATIVES:
                    Visit_ALTERNATIVES((ALTERNATIVES)node);
                    break;
                default:
                    Visit_UNKNOWN_NODE(node);
                    break;
        }
            Visit_After(node);
        }
        
        /// <summary>
        /// Executed before visiting for every node
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Whether the node should be visited or not</returns>
        protected virtual bool Visit_Before(NODE node)
        {
            return true;
        }

        protected virtual void Visit_After(NODE node)
        {
        }

        protected virtual void Visit_UNKNOWN_NODE(NODE node){
            ERROR.SystemErrorIn("Visit_UNKNOWN_NODE","should never been called");
        }

        protected virtual void Visit_STATEMENT_LIST(STATEMENT_LIST node) {}
        protected virtual void Visit_IDENT_LIST(IDENT_LIST node) {}
        protected virtual void Visit_COMMENT(COMMENT node) {}
        protected virtual void Visit_COMMENT_LIST(COMMENT_LIST node) {}
        protected virtual void Visit_IF_PAIR(IF_PAIR node) {}
        protected virtual void Visit_CASE_ITEM(CASE_ITEM node) {}
        protected virtual void Visit_RANGE(RANGE node) {}
        protected virtual void Visit_EXCEPTION(EXCEPTION node) {}

#region TYPE
        protected virtual void Visit_ANY_TYPE(ANY_TYPE node) {}
        protected virtual void Visit_UNKNOWN_TYPE(UNKNOWN_TYPE node) {}
        protected virtual void Visit_ARRAY_TYPE(ARRAY_TYPE node) {}
        protected virtual void Visit_SET_TYPE(SET_TYPE node) {}
        protected virtual void Visit_RANGE_TYPE(RANGE_TYPE node) {}
        protected virtual void Visit_INTERFACE_TYPE(INTERFACE_TYPE node) {}
        protected virtual void Visit_OBJECT_TYPE(OBJECT_TYPE node) {}
        protected virtual void Visit_ACTIVITY_TYPE(ACTIVITY_TYPE node) {}
        protected virtual void Visit_ABSTRACT_ACTIVITY_TYPE(ABSTRACT_ACTIVITY_TYPE node) {}
        protected virtual void Visit_INTEGER_TYPE(INTEGER_TYPE node) {}
        protected virtual void Visit_REAL_TYPE(REAL_TYPE node) {}
        protected virtual void Visit_FIXED_TYPE(FIXED_TYPE node) {}
        protected virtual void Visit_CHAR_TYPE(CHAR_TYPE node) {}
        protected virtual void Visit_STRING_TYPE(STRING_TYPE node) {}
        protected virtual void Visit_CARDINAL_TYPE(CARDINAL_TYPE node) {}
        protected virtual void Visit_BOOLEAN_TYPE(BOOLEAN_TYPE node) {}
        protected virtual void Visit_VOID_TYPE(VOID_TYPE node) {}
        protected virtual void Visit_ENUM_TYPE(ENUM_TYPE node) {}
        protected virtual void Visit_PROC_TYPE(PROC_TYPE node) {}
        protected virtual void Visit_EXTERNAL_TYPE(EXTERNAL_TYPE node) {}
#endregion
#region EXPRESSION
    #region UNARY        
        protected virtual void Visit_NEGATION(NEGATION node) {}
        protected virtual void Visit_UNARY_MINUS(UNARY_MINUS node) {}
        protected virtual void Visit_UNARY_PLUS(UNARY_PLUS node) {}
        protected virtual void Visit_TYPE_CONV(TYPE_CONV node) {}
    #endregion
    #region BINARY
        protected virtual void Visit_ASSIGNMENT_OPERATOR(ASSIGNMENT_OPERATOR node) {}
        protected virtual void Visit_PLUS(PLUS node) {}
        protected virtual void Visit_MINUS(MINUS node) {}
        protected virtual void Visit_MULTIPLY(MULTIPLY node) {}
        protected virtual void Visit_DIVIDE(DIVIDE node) {}
        protected virtual void Visit_DIVIDE_ELEMENTWISE(DIVIDE_ELEMENTWISE node) {}
        protected virtual void Visit_MULTIPLY_ELEMENTWISE(MULTIPLY_ELEMENTWISE node) {}
        protected virtual void Visit_PSEUDO_SCALAR_PRODUCT(PSEUDO_SCALAR_PRODUCT node) {}
        protected virtual void Visit_TRANSPOSE(TRANSPOSE node) {}
        protected virtual void Visit_EXPR_ARRAY_ASSIGNMENT(EXPR_ARRAY_ASSIGNMENT node) {}
        protected virtual void Visit_RANGESTEP(RANGESTEP node) {}
        protected virtual void Visit_ARRAY_RANGE(ARRAY_RANGE node) {}
        protected virtual void Visit_LEFTDIVISION(LEFTDIVISION node) {}
        protected virtual void Visit_DIV(DIV node) {}
        protected virtual void Visit_MOD(MOD node) {}
        protected virtual void Visit_EXPONENT(EXPONENT node) {}
        protected virtual void Visit_AND(AND node) {}
        protected virtual void Visit_OR(OR node) {}
        #region RELATION
        protected virtual void Visit_EQUAL(EQUAL node) {}
        protected virtual void Visit_NON_EQUAL(NON_EQUAL node) {}
        protected virtual void Visit_LESS(LESS node) {}
        protected virtual void Visit_LESS_EQUAL(LESS_EQUAL node) {}
        protected virtual void Visit_GREATER(GREATER node) {}
        protected virtual void Visit_GREATER_EQUAL(GREATER_EQUAL node) {}
        protected virtual void Visit_EQUAL_ELEMENTWISE(EQUAL_ELEMENTWISE node) {}
        protected virtual void Visit_NON_EQUAL_ELEMENTWISE(NON_EQUAL_ELEMENTWISE node) {}
        protected virtual void Visit_LESS_ELEMENTWISE(LESS_ELEMENTWISE node) {}
        protected virtual void Visit_LESS_EQUAL_ELEMENTWISE(LESS_EQUAL_ELEMENTWISE node) {}
        protected virtual void Visit_GREATER_ELEMENTWISE(GREATER_ELEMENTWISE node) {}
        protected virtual void Visit_GREATER_EQUAL_ELEMENTWISE(GREATER_EQUAL_ELEMENTWISE node) {}
        protected virtual void Visit_IN(IN node) {}
        protected virtual void Visit_IMPLEMENTS(IMPLEMENTS node) {}
        protected virtual void Visit_IS(IS node) {}
        #endregion
    #endregion
    #region DESIGNATOR
        protected virtual void Visit_DEREFERENCE(DEREFERENCE node) {}
        protected virtual void Visit_INDEXER(INDEXER node) {}
        protected virtual void Visit_SELECTOR(SELECTOR node) {}
        protected virtual void Visit_SAFEGUARD(SAFEGUARD node) {}
        protected virtual void Visit_CALL(CALL node) {}
        protected virtual void Visit_SET_CTOR(SET_CTOR node) {}
        protected virtual void Visit_NEW(NEW node) {}
        protected virtual void Visit_SELF(SELF node) {}
        protected virtual void Visit_OBJECT(OBJECT node) {}
        protected virtual void Visit_INSTANCE(INSTANCE node) {}        
        #region LITERAL
        protected virtual void Visit_ENUMERATOR(ENUMERATOR node) {}
        protected virtual void Visit_STRING_LITERAL(STRING_LITERAL node) {}
        protected virtual void Visit_CHAR_LITERAL(CHAR_LITERAL node) {}
        protected virtual void Visit_INTEGER_LITERAL(INTEGER_LITERAL node) {}
        protected virtual void Visit_REAL_LITERAL(REAL_LITERAL node) {}
        protected virtual void Visit_CCI_LITERAL(CCI_LITERAL node) {}
        protected virtual void Visit_NULL(NULL node) {}
        #endregion
    #endregion
#endregion

        #region STATEMENT
        protected virtual void Visit_ASSIGNMENT(ASSIGNMENT node) {}
        protected virtual void Visit_CALL_STMT(CALL_STMT node) {}
        protected virtual void Visit_EXIT(EXIT node) {}
        protected virtual void Visit_RETURN(RETURN node) {}
        protected virtual void Visit_REPLY(REPLY node) {}
        protected virtual void Visit_AWAIT(AWAIT node) {}
        protected virtual void Visit_SEND_RECEIVE(SEND_RECEIVE node) {}
        protected virtual void Visit_LAUNCH(LAUNCH node) {}
        protected virtual void Visit_ACCEPT(ACCEPT node) {}
        protected virtual void Visit_IF(IF node) {}
        protected virtual void Visit_CASE(CASE node) {}
        protected virtual void Visit_BLOCK(BLOCK node) {}
    #region CYCLE
        protected virtual void Visit_FOR(FOR node) {}
        protected virtual void Visit_WHILE(WHILE node) {}
        protected virtual void Visit_REPEAT(REPEAT node) {}
        protected virtual void Visit_LOOP(LOOP node) {}
    #endregion
#endregion
#region DECLARATION
    #region UNIT_DECL
        protected virtual void Visit_UNKNOWN_DECL(UNKNOWN_DECL node) {}
        protected virtual void Visit_EXTERNAL_DECL(EXTERNAL_DECL node) {}
        protected virtual void Visit_NAMESPACE_DECL(NAMESPACE_DECL node) {}
        protected virtual void Visit_MODULE_DECL(MODULE_DECL node) {}
        protected virtual void Visit_OBJECT_DECL(OBJECT_DECL node) {}
        protected virtual void Visit_DEFINITION_DECL(DEFINITION_DECL node) {}
        protected virtual void Visit_IMPLEMENTATION_DECL(IMPLEMENTATION_DECL node) {}
    #endregion
    #region ROUTINE_DECL
        protected virtual void Visit_PROCEDURE_DECL(PROCEDURE_DECL node) {}
        protected virtual void Visit_OPERATOR_DECL(OPERATOR_DECL node) {}
        protected virtual void Visit_ACTIVITY_DECL(ACTIVITY_DECL node) {}
    #endregion
    #region SIMPLE_DECL
        protected virtual void Visit_IMPORT_DECL(IMPORT_DECL node) {}
        #region VARIABLE_DECL 
        protected virtual void Visit_PARAMETER_DECL(PARAMETER_DECL node) {}
        protected virtual void Visit_LOCAL_DECL(LOCAL_DECL node) {}
        protected virtual void Visit_FIELD_DECL(FIELD_DECL node) {}
        #endregion
        protected virtual void Visit_CONSTANT_DECL(CONSTANT_DECL node) {}
        protected virtual void Visit_TYPE_DECL(TYPE_DECL node) {}
        protected virtual void Visit_ENUMERATOR_DECL(ENUMERATOR_DECL node) {}
        protected virtual void Visit_PROTOCOL_DECL(PROTOCOL_DECL node) {}
    #endregion
#endregion
        
#region EXTENSION
        protected virtual void Visit_SYNTAX(SYNTAX node) {}
        protected virtual void Visit_PRODUCTION(PRODUCTION node) {}
    #region UNIT
        protected virtual void Visit_TERMINAL(TERMINAL node) {}
        protected virtual void Visit_TYPE_NAME(TYPE_NAME node) {}
        protected virtual void Visit_CONSTANT(CONSTANT node) {}
        protected virtual void Visit_NONTERMINAL(NONTERMINAL node) {}
        protected virtual void Visit_UNKNOWN_NONTERMINAL(UNKNOWN_NONTERMINAL node) {}
        protected virtual void Visit_SEQUENCE(SEQUENCE node) {}
        protected virtual void Visit_ALTERNATIVES(ALTERNATIVES node) {}
    #endregion
#endregion

    }
}
