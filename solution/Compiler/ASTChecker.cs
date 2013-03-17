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
    public sealed class ASTChecker: ASTVisitor
    {
        public ASTChecker()
        {
        }

        protected override void Visit_STATEMENT_LIST(STATEMENT_LIST node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IDENT_LIST(IDENT_LIST node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_COMMENT(COMMENT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_COMMENT_LIST(COMMENT_LIST node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IF_PAIR(IF_PAIR node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CASE_ITEM(CASE_ITEM node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_RANGE(RANGE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXCEPTION(EXCEPTION node)
        {
            /* MOVE CODE HERE */

        }

        #region TYPE
        protected override void Visit_ANY_TYPE(ANY_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_UNKNOWN_TYPE(UNKNOWN_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ARRAY_TYPE(ARRAY_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SET_TYPE(SET_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_RANGE_TYPE(RANGE_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_INTERFACE_TYPE(INTERFACE_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_OBJECT_TYPE(OBJECT_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ACTIVITY_TYPE(ACTIVITY_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ABSTRACT_ACTIVITY_TYPE(ABSTRACT_ACTIVITY_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_INTEGER_TYPE(INTEGER_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_REAL_TYPE(REAL_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_FIXED_TYPE(FIXED_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CHAR_TYPE(CHAR_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_STRING_TYPE(STRING_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CARDINAL_TYPE(CARDINAL_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_BOOLEAN_TYPE(BOOLEAN_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_VOID_TYPE(VOID_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ENUM_TYPE(ENUM_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_PROC_TYPE(PROC_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXTERNAL_TYPE(EXTERNAL_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #region EXPRESSION
        #region UNARY
        protected override void Visit_NEGATION(NEGATION node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_UNARY_MINUS(UNARY_MINUS node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_UNARY_PLUS(UNARY_PLUS node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_TYPE_CONV(TYPE_CONV node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #region BINARY
        protected override void Visit_ASSIGNMENT_OPERATOR(ASSIGNMENT_OPERATOR node)
        {
            /* MOVE CODE HERE */
        }
        protected override void Visit_PLUS(PLUS node)
        {
            /* MOVE CODE HERE */
        }
        protected override void Visit_MINUS(MINUS node)
        {
            /* MOVE CODE HERE */
        }
        protected override void Visit_MULTIPLY(MULTIPLY node)
        {
            /* MOVE CODE HERE */
        }
        protected override void Visit_DIVIDE(DIVIDE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_DIVIDE_ELEMENTWISE(DIVIDE_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_MULTIPLY_ELEMENTWISE(MULTIPLY_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_PSEUDO_SCALAR_PRODUCT(PSEUDO_SCALAR_PRODUCT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_TRANSPOSE(TRANSPOSE node)
        {
            /* MOVE CODE HERE */
        }
        protected override void Visit_EXPR_ARRAY_ASSIGNMENT(EXPR_ARRAY_ASSIGNMENT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_RANGESTEP(RANGESTEP node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ARRAY_RANGE(ARRAY_RANGE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LEFTDIVISION(LEFTDIVISION node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_DIV(DIV node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_MOD(MOD node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXPONENT(EXPONENT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_AND(AND node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_OR(OR node)
        {
            /* MOVE CODE HERE */

        }
        #region RELATION
        protected override void Visit_EQUAL(EQUAL node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_NON_EQUAL(NON_EQUAL node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_LESS(LESS node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_LESS_EQUAL(LESS_EQUAL node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_GREATER(GREATER node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_GREATER_EQUAL(GREATER_EQUAL node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_EQUAL_ELEMENTWISE(EQUAL_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */
             
        }
        protected override void Visit_NON_EQUAL_ELEMENTWISE(NON_EQUAL_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LESS_ELEMENTWISE(LESS_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LESS_EQUAL_ELEMENTWISE(LESS_EQUAL_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_GREATER_ELEMENTWISE(GREATER_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_GREATER_EQUAL_ELEMENTWISE(GREATER_EQUAL_ELEMENTWISE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IN(IN node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IMPLEMENTS(IMPLEMENTS node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IS(IS node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #endregion
        #region DESIGNATOR
        protected override void Visit_DEREFERENCE(DEREFERENCE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_INDEXER(INDEXER node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SELECTOR(SELECTOR node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SAFEGUARD(SAFEGUARD node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CALL(CALL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SET_CTOR(SET_CTOR node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_NEW(NEW node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SELF(SELF node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_OBJECT(OBJECT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_INSTANCE(INSTANCE node)
        {
            /* MOVE CODE HERE */

        }
        #region LITERAL
        protected override void Visit_ENUMERATOR(ENUMERATOR node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_STRING_LITERAL(STRING_LITERAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CHAR_LITERAL(CHAR_LITERAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_INTEGER_LITERAL(INTEGER_LITERAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_REAL_LITERAL(REAL_LITERAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CCI_LITERAL(CCI_LITERAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_NULL(NULL node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #endregion
        #endregion

        #region STATEMENT
        protected override void Visit_ASSIGNMENT(ASSIGNMENT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CALL_STMT(CALL_STMT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXIT(EXIT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_RETURN(RETURN node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_REPLY(REPLY node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_AWAIT(AWAIT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SEND_RECEIVE(SEND_RECEIVE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LAUNCH(LAUNCH node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ACCEPT(ACCEPT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IF(IF node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CASE(CASE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_BLOCK(BLOCK node)
        {
            /* MOVE CODE HERE */

        }
        #region CYCLE
        protected override void Visit_FOR(FOR node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_WHILE(WHILE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_REPEAT(REPEAT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LOOP(LOOP node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #endregion
        #region DECLARATION
        #region UNIT_DECL
        protected override void Visit_UNKNOWN_DECL(UNKNOWN_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXTERNAL_DECL(EXTERNAL_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_NAMESPACE_DECL(NAMESPACE_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_MODULE_DECL(MODULE_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_OBJECT_DECL(OBJECT_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_DEFINITION_DECL(DEFINITION_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_IMPLEMENTATION_DECL(IMPLEMENTATION_DECL node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #region ROUTINE_DECL
        protected override void Visit_PROCEDURE_DECL(PROCEDURE_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_OPERATOR_DECL(OPERATOR_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ACTIVITY_DECL(ACTIVITY_DECL node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #region SIMPLE_DECL
        protected override void Visit_IMPORT_DECL(IMPORT_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_PARAMETER_DECL(PARAMETER_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_LOCAL_DECL(LOCAL_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_FIELD_DECL(FIELD_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CONSTANT_DECL(CONSTANT_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_TYPE_DECL(TYPE_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ENUMERATOR_DECL(ENUMERATOR_DECL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_PROTOCOL_DECL(PROTOCOL_DECL node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #endregion

        #region EXTENSION
        protected override void Visit_SYNTAX(SYNTAX node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_PRODUCTION(PRODUCTION node)
        {
            /* MOVE CODE HERE */

        }
        #region UNIT
        protected override void Visit_TERMINAL(TERMINAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_TYPE_NAME(TYPE_NAME node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CONSTANT(CONSTANT node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_NONTERMINAL(NONTERMINAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_UNKNOWN_NONTERMINAL(UNKNOWN_NONTERMINAL node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_SEQUENCE(SEQUENCE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_ALTERNATIVES(ALTERNATIVES node)
        {
            /* MOVE CODE HERE */

        }
        #endregion
        #endregion
    }
}
