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
using System.Linq;
using System.Text;
using System.Compiler;
using System.Diagnostics;

namespace ETH.Zonnon
{
    public class MathGenerator
    {
        private Class mathRoutines;
        private Identifier mathRoutinesIdentifier;

        /// <summary>
        /// Use this to attach generated IsMath functions to the root namespace
        /// </summary>
        /// <param name="ns">root namespace</param>
        public void AttachToNamespace(Namespace ns)
        {
            mathRoutines.DeclaringModule = CONTEXT.symbolTable;
            Debug.Assert(ns.Types != null);                        
            mathRoutines.Namespace = ns.Name;
            ns.Types.Add(mathRoutines);
            CONTEXT.symbolTable.Types.Add(mathRoutines);
        }

        #region AuxilaryRoutinesForGenerating

        QualifiedIdentifier CreateQualIdentifier(Identifier id)
        {
            return new QualifiedIdentifier(Identifier.For("Main"), id);
        }

        /// <summary>
        /// Function for adding parameters into the block
        /// </summary>
        /// <param name="method">the method to add parameters</param>
        /// <param name="indicesStr">indices in string format (like SRBN...) (this info starts from indexInfo)</param>
        /// <param name="indexInfo">starting index for indices information</param>
        /// <param name="indices">indices to be added as parameters</param>
        /// <param name="prefix">prefix for parameters names (like "left_" or "right_")</param>
        /// <param name="arrayName">array identifier to define ranges length (for range indices)</param>
        private void AddingParameters(Method method,
            string indicesStr, int index_info, EXPRESSION_LIST indices, string prefix, string arrayName, SourceContext sourceContext)
        {
            if (indices == null) return;

            //checking for null reference
            TypeNode mathException = SystemTypes.NullReferenceException;
            Expression operand1 = Identifier.For(arrayName);
            Expression operand2 = new Literal(null, SystemTypes.Object, sourceContext);
            NodeType boolOp = NodeType.Eq;
            Int64 startLine = -1;
            Int32 startColumn = -1;

            If ifstm1 = new If();
            BinaryExpression ifcond = new BinaryExpression(operand1, operand2, boolOp, SystemTypes.Boolean, sourceContext);
            ifstm1.Condition = ifcond;

            Throw throwstm = new Throw();
            Construct throwstm_constructor = new Construct();
            throwstm_constructor.Constructor = new MemberBinding(null, mathException.GetConstructor(new TypeNode[] { SystemTypes.String }));
            // new QualifiedIdentifier(rtlName,Identifier.For("Halt"));
            throwstm_constructor.Type = mathException;
            throwstm_constructor.Operands = new ExpressionList();
            string exceptionMessage = " at line " + startLine.ToString() + ", pos " + startColumn.ToString();
            throwstm_constructor.Operands.Add(new Literal(exceptionMessage, SystemTypes.String));
            throwstm.Expression = throwstm_constructor;

            ifstm1.TrueBlock = new Block();
            ifstm1.TrueBlock.Statements = new StatementList(new Statement[] { throwstm });
            ifstm1.TrueBlock.SourceContext = sourceContext;
            ifstm1.SourceContext = sourceContext;

            method.Body.Statements.Add(ifstm1);
            //if indices == null then we will make this checking in AddingNi function

            //Adding indices as parameters
            int n_s = 0; //for simple indices
            int n_r = 0; //for ranges and all the vector indices
            
            for (int i = 0; i < indices.Length; i++)
            {
                switch (indicesStr[index_info + i])
                {
                    case 'S':
                        {
                            method.Parameters.Add(
                                new Parameter(Identifier.For(prefix + "n_s" + n_s.ToString()), indices[i].type.convert() as TypeNode));
                            n_s++;
                            break;
                        }
                    case 'R':
                        {
                            if (indices[i] is ARRAY_RANGE)
                            {
                                ARRAY_RANGE cur_range = indices[i] as ARRAY_RANGE;
                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_from"), cur_range.from.type.convert() as TypeNode));
                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_wasToWritten"), SystemTypes.Boolean));
                                method.Parameters.Add(
                                        new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_to"), cur_range.to.type.convert() as TypeNode));

                                If ifstm = new If();
                                ifstm.Condition = new UnaryExpression(Identifier.For(prefix + "n_r" + n_r.ToString() + "_wasToWritten"), NodeType.LogicalNot, sourceContext);
                                ifstm.TrueBlock = new Block();
                                ifstm.TrueBlock.SourceContext = sourceContext;
                                ifstm.TrueBlock.Statements = new StatementList();
                                AssignmentStatement asgn = new AssignmentStatement(
                                        Identifier.For(prefix + "n_r" + n_r.ToString() + "_to"),
                                        new BinaryExpression(
                                            new BinaryExpression(
                                                new MethodCall(
                                                    new MemberBinding(Identifier.For(arrayName),
                                                        SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                                        new ExpressionList(new Expression[] { new Literal(i, SystemTypes.Int32) }),
                                                        NodeType.Call, SystemTypes.Int32),
                                                new Literal(1, SystemTypes.Int32),
                                                NodeType.Sub_Ovf,
                                                sourceContext),
                                            new MemberBinding(null, cur_range.to.type.convert() as TypeNode),
                                            NodeType.Castclass, 
                                            sourceContext),
                                        NodeType.Nop,
                                        sourceContext);
                                ifstm.TrueBlock.Statements.Add(asgn);
                                method.Body.Statements.Add(ifstm);

                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_by"), cur_range.by.type.convert() as TypeNode));
                                n_r++;
                                break;
                            }
                            else //it's range variable
                            {
                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_from"), SystemTypes.Int32));
                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_wasToWritten"), SystemTypes.Boolean));
                                method.Parameters.Add(
                                        new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_to"), SystemTypes.Int32));

                                If ifstm = new If();
                                ifstm.Condition = new UnaryExpression(Identifier.For(prefix + "n_r" + n_r.ToString() + "_wasToWritten"), NodeType.LogicalNot, sourceContext);
                                ifstm.TrueBlock = new Block();
                                ifstm.TrueBlock.SourceContext = sourceContext;
                                ifstm.TrueBlock.Statements = new StatementList();
                                AssignmentStatement asgn = new AssignmentStatement(
                                        Identifier.For(prefix + "n_r" + n_r.ToString() + "_to"),
                                        new BinaryExpression(
                                            new BinaryExpression(
                                                new MethodCall(
                                                    new MemberBinding(Identifier.For(arrayName),
                                                        SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                                        new ExpressionList(new Expression[] { new Literal(i, SystemTypes.Int32) }),
                                                        NodeType.Call, SystemTypes.Int32),
                                                new Literal(1, SystemTypes.Int32),
                                                NodeType.Sub_Ovf),
                                            new MemberBinding(null, SystemTypes.Int32),
                                            NodeType.Castclass),
                                        NodeType.Nop, sourceContext);
                                ifstm.TrueBlock.Statements.Add(asgn);
                                method.Body.Statements.Add(ifstm);

                                method.Parameters.Add(
                                    new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString() + "_by"), SystemTypes.Int32));
                                n_r++;
                                break;
                            }
                        }
                    default: //case 'I' || 'C' || 'B':
                        {
                            method.Parameters.Add(
                                new Parameter(Identifier.For(prefix + "n_r" + n_r.ToString()),
                                    indices[i].type.convert() as TypeNode));
                            n_r++;
                            break;
                        }
                }
            }

            //Debug.Assert(returnDim == n_r);
        }

        /// <summary>
        /// This function generates necesary local variables (prefix_n_i = arrayName.GetLength(i))
        /// </summary>
        /// <param name="block"></param>
        /// <param name="indicesStr"></param>
        /// <param name="index_info"></param>
        /// <param name="indices"></param>
        /// <param name="prefix"></param>
        /// <param name="arrayName"></param>
        /// <param name="returnDim"></param>
        /// <param name="bvIndices"></param>
        /// <param name="sourceContext"></param>
        private void AddingNi(Block block, 
            TypeNode intType,
            string indicesStr, int index_info, EXPRESSION_LIST indices, string prefix, string arrayName, int returnDim, int[] bvIndices, SourceContext sourceContext)
        {      
            int k = 0;
            if (indices == null)
            {
                //checking for null reference
                TypeNode mathException = SystemTypes.NullReferenceException;
                Expression operand1 = Identifier.For(arrayName);
                Expression operand2 = new Literal(null, SystemTypes.Object, sourceContext);
                NodeType boolOp = NodeType.Eq;
                Int64 startLine = sourceContext.StartLine;
                Int32 startColumn = sourceContext.StartColumn;

                If ifstm1 = new If();
                BinaryExpression ifcond = new BinaryExpression(operand1, operand2, boolOp, SystemTypes.Boolean, sourceContext);
                ifstm1.Condition = ifcond;

                Throw throwstm = new Throw();
                Construct throwstm_constructor = new Construct();
                throwstm_constructor.Constructor = new MemberBinding(null, mathException.GetConstructor(new TypeNode[] { SystemTypes.String }));
                // new QualifiedIdentifier(rtlName,Identifier.For("Halt"));
                throwstm_constructor.Type = mathException;
                throwstm_constructor.Operands = new ExpressionList();
                string exceptionMessage = " at line " + startLine.ToString() + ", pos " + startColumn.ToString();
                throwstm_constructor.Operands.Add(new Literal(exceptionMessage, SystemTypes.String));
                throwstm.Expression = throwstm_constructor;

                ifstm1.TrueBlock = new Block();
                ifstm1.TrueBlock.Statements = new StatementList(new Statement[] { throwstm });
                ifstm1.TrueBlock.SourceContext = sourceContext;
                ifstm1.SourceContext = sourceContext;

                block.Statements.Add(ifstm1);
                //if indices != null then we made this checking in AddingParameters function

                Member getLength = null;
                if (intType is ArrayType)
                    getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                else
                    getLength = intType.GetMembersNamed(Identifier.For("GetLength"))[0];

                // ~    ni = left.GetLength(i);
                for (int i = 0; i < returnDim; i++)
                {
                    AssignmentStatement asgn = new AssignmentStatement();
                    asgn.Source = new MethodCall(
                        new MemberBinding(Identifier.For(arrayName),
                            getLength),
                            new ExpressionList(new Expression[] { new Literal(i, SystemTypes.Int32) }),
                            NodeType.Call, SystemTypes.Int32);
                    asgn.Target = Identifier.For(prefix + "n" + i.ToString());
                    block.Statements.Add(asgn);
                }
            }
            else
            {
                int i_r = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    switch (indicesStr[index_info + i])
                    {
                        case 'S':
                            { break; }
                        case 'R':
                            {
                                // ~    ni = (n_ri_to - n_ri_from)/n_ri_by + 1;
                                AssignmentStatement asgn = new AssignmentStatement();
                                asgn.Source = new BinaryExpression(
                                    new BinaryExpression(
                                        new BinaryExpression(
                                            Identifier.For(prefix + "n_r" + i_r.ToString() + "_to"),
                                            Identifier.For(prefix + "n_r" + i_r.ToString() + "_from"),
                                            NodeType.Sub_Ovf,
                                            sourceContext),
                                        Identifier.For(prefix + "n_r" + i_r.ToString() + "_by"),
                                        NodeType.Div,
                                        sourceContext),
                                    new Literal(1, SystemTypes.Int16),
                                    NodeType.Add_Ovf,
                                    sourceContext);
                                asgn.Target = Identifier.For(prefix + "n" + i_r.ToString());
                                asgn.SourceContext = sourceContext;
                                block.Statements.Add(asgn);
                                i_r++;
                                break;
                            }
                        case 'I':
                            {
                                //~ ni = length(n_ri);
                                AssignmentStatement asgn = new AssignmentStatement();
                                asgn.Source = new MethodCall(
                                    new MemberBinding(Identifier.For(prefix + "n_r" + i_r.ToString()),
                                        SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                        new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                                        NodeType.Call, SystemTypes.Int32);
                                asgn.Target = Identifier.For(prefix + "n" + i_r.ToString());
                                block.Statements.Add(asgn);
                                i_r++;
                                break;
                            }
                        case 'C':
                            {
                                //~ ni = length(n_ri);
                                AssignmentStatement asgn = new AssignmentStatement();
                                asgn.Source = new MethodCall(
                                    new MemberBinding(Identifier.For(prefix + "n_r" + i_r.ToString()),
                                        SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                        new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                                        NodeType.Call, SystemTypes.Int32);
                                asgn.Target = Identifier.For(prefix + "n" + i_r.ToString());
                                block.Statements.Add(asgn);
                                i_r++;
                                break;
                            }
                        case ('B'):
                            {
                                GenerateCodeForBooleanVector(block, i_r, prefix, i, arrayName, sourceContext);
                                bvIndices[k++] = i_r;
                                i_r++;
                                break;
                            }
                    }
                }
            }
        }

        /// <summary>
        /// Generates code with while statements and j declarations statements and adds this into forstatements
        /// (it is used for boolean vector indices)
        /// </summary>
        /// <param name="block"></param>
        /// <param name="forStatements"></param>
        /// <param name="bvLeftIndices"></param>
        /// <param name="bvRightIndices"></param>
        private void AddingWhileStmIntoFor(Block block, StatementList forStatements, int[] bvLeftIndices, string leftPrefix, int[] bvRightIndices, string rightPrefix, SourceContext sourceContext)
        {
            int left_k = 0;
            int right_k = 0;
            int bvLeftIndicesLength = 0; 
            if (bvLeftIndices != null) bvLeftIndicesLength = bvLeftIndices.Length;
            int bvRightIndicesLength = 0; 
            if (bvRightIndices != null) bvRightIndicesLength = bvRightIndices.Length;
            int returnDim = forStatements.Length; //it can be not equal to returnDim, 
                    //but it is for element-wise array-array operations

            if (bvLeftIndicesLength + bvRightIndicesLength > 0) //there are boolean vectors
            {
                if (bvLeftIndicesLength > 0)
                {
                    if (bvLeftIndices[0] == 0)
                    {
                        Construct construct = new Construct();
                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                        construct.Constructor.Type = SystemTypes.Int32;
                        construct.Type = SystemTypes.Int32;
                        construct.Operands = new ExpressionList(); // no params
                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                            new LocalDeclaration(Identifier.For(leftPrefix + "j0"), construct), SystemTypes.Int32);
                        block.Statements.Add(j_decl);
                    }
                }
                if (bvRightIndicesLength > 0)
                {
                    if (bvRightIndices[0] == 0)
                    {
                        Construct construct = new Construct();
                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                        construct.Constructor.Type = SystemTypes.Int32;
                        construct.Type = SystemTypes.Int32;
                        construct.Operands = new ExpressionList(); // no params
                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                            new LocalDeclaration(Identifier.For(rightPrefix + "j0"), construct), SystemTypes.Int32);
                        block.Statements.Add(j_decl);
                    }
                }

                for (int i = 0; i < returnDim; i++)
                {
                    bool flag0 = false, flag1 = false, flag2 = false;
                    if (bvLeftIndicesLength > 0)
                    {
                        if (i != bvLeftIndices[left_k])
                        {
                            if (i + 1 == bvLeftIndices[left_k])
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For(leftPrefix + "j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                ((For)forStatements[i]).Body.Statements.Add(j_decl);
                            }
                            if (i < returnDim - 1)
                            {
                                flag0 = true;
                            }
                        }
                        else
                        {
                            While whilestm = new While();
                            whilestm.Condition = new UnaryExpression(
                                new Indexer(Identifier.For(leftPrefix + "n_r" + i.ToString()),
                                    new ExpressionList(new Expression[] { Identifier.For(leftPrefix + "j" + i.ToString()) })),
                                NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                            whilestm.SourceContext = sourceContext;
                            whilestm.Body = new Block();
                            whilestm.Body.SourceContext = sourceContext;
                            whilestm.Body.Statements = new StatementList();
                            whilestm.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For(leftPrefix + "j" + i.ToString()),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));

                            ((For)forStatements[i]).Body.Statements.Add(whilestm);

                            if ((left_k < bvLeftIndicesLength - 1) && (i + 1 == bvLeftIndices[left_k + 1]))
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For(leftPrefix + "j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                ((For)forStatements[i]).Body.Statements.Add(j_decl);
                            }

                            if (i < returnDim - 1)
                            {
                                flag1 = true;
                            }

                            left_k++;
                            if (left_k == bvLeftIndicesLength) left_k--;
                        }
                    }
                    if (bvRightIndicesLength > 0)
                    {
                        if (i != bvRightIndices[right_k])
                        {
                            if (i + 1 == bvRightIndices[right_k])
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For(rightPrefix + "j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                ((For)forStatements[i]).Body.Statements.Add(j_decl);
                            }
                            flag0 = true;
                        }
                        else
                        {
                            While whilestm = new While();
                            whilestm.Condition = new UnaryExpression(
                                new Indexer(Identifier.For(rightPrefix + "n_r" + i.ToString()),
                                    new ExpressionList(new Expression[] { Identifier.For(rightPrefix + "j" + i.ToString()) })),
                                NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                            whilestm.SourceContext = sourceContext;
                            whilestm.Body = new Block();
                            whilestm.Body.SourceContext = sourceContext;
                            whilestm.Body.Statements = new StatementList();
                            whilestm.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For(rightPrefix + "j" + i.ToString()),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));

                            ((For)forStatements[i]).Body.Statements.Add(whilestm);

                            if ((right_k < bvRightIndicesLength - 1) && (i + 1 == bvRightIndices[right_k + 1]))
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For(rightPrefix + "j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                ((For)forStatements[i]).Body.Statements.Add(j_decl);
                            }

                            if (i < returnDim - 1)
                            {
                                flag2 = true;
                            }

                            right_k++;
                            if (right_k == bvRightIndicesLength) right_k--;
                        }
                    }

                    if (flag0 || flag1 || flag2)
                    {
                        if (i + 1 < returnDim)
                            ((For)forStatements[i]).Body.Statements.Add(forStatements[i + 1]);
                    }

                    if (flag1)
                        ((For)forStatements[i]).Body.Statements.Add(new AssignmentStatement(
                                Identifier.For(leftPrefix + "j" + i.ToString()),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                    if (flag2)
                        ((For)forStatements[i]).Body.Statements.Add(new AssignmentStatement(
                                Identifier.For(rightPrefix + "j" + i.ToString()),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                }
            }
            else //no boolean vector indices
            {
                for (int i = 0; i < returnDim - 1; i++)
                    ((For)forStatements[i]).Body.Statements.Add(forStatements[i + 1]);
            }
        }
        
        /// <summary>
        /// Generates a piece of code in block for developing boolean vector indices, calculetes ni and so on
        /// </summary>
        /// <param name="block">Block to add the generated code</param>
        /// <param name="i">The index number of this boolean vector</param>
        /// <param name="namePrefix">Prefix of the "n_r" variable; can be (usually) left_, right_ or nothing</param>
        /// <param name="arrayIndex">index in array (of the name arrayIdent) to check the length</param>
        /// <param name="arrayIdent">Array identifier to check the length (length of boolean vector has to be equal to arrayIdent.GetLength(i))</param>
        /// <param name="sourceContext"></param>
        private void GenerateCodeForBooleanVector(Block block, int i, string namePrefix, int arrayIndex, string arrayIdent, SourceContext sourceContext)
        {
            block.Statements.Add(GenerateIfStatementWithThrow(new MethodCall(
                new MemberBinding(Identifier.For(namePrefix + "n_r" + i.ToString()),
                    SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                    new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                    NodeType.Call, SystemTypes.Int32),
                new MethodCall(
                    new MemberBinding(Identifier.For(arrayIdent),
                        SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                        new ExpressionList(new Expression[] { new Literal(arrayIndex, SystemTypes.Int32) }),
                        NodeType.Call, SystemTypes.Int32), 
                NodeType.Ne,
                STANDARD.IncompatibleSizesException,
                sourceContext.StartLine, sourceContext.StartColumn,
                sourceContext));
            
            //~ ni = (number of true elements in n_ri);
            For forStm = new For();

            //Making initialization
            AssignmentStatement init_st = new AssignmentStatement(
                Identifier.For("i0"), new Literal(0, SystemTypes.Int32), NodeType.Nop, sourceContext);
            forStm.Initializer = new StatementList(new Statement[] { init_st });

            // Making condition
            BinaryExpression condition = new BinaryExpression();
            condition.NodeType = NodeType.Lt;
            condition.Operand1 = Identifier.For("i0");
            condition.Operand2 = new MethodCall(
                new MemberBinding(Identifier.For(namePrefix + "n_r" + i.ToString()),
                    SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                    new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                    NodeType.Call, SystemTypes.Int32);
            condition.SourceContext = sourceContext;
            forStm.Condition = condition;

            // Making incrementer
            AssignmentStatement incr_st = new AssignmentStatement(
                Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add);
            forStm.Incrementer = new StatementList(new Statement[] { incr_st });

            //Making body
            forStm.Body = new Block();
            forStm.Body.Statements = new StatementList();
            forStm.Body.SourceContext = sourceContext;

            Block true_bl = new Block();
            true_bl.Statements = new StatementList();
            AssignmentStatement intern_st = new AssignmentStatement(
                Identifier.For(namePrefix + "n" + i.ToString()), new Literal(1, SystemTypes.Int32), NodeType.Add);
            true_bl.Statements.Add(intern_st);

            If if_st = new If(new Indexer(Identifier.For(namePrefix + "n_r" + i.ToString()),
                        new ExpressionList(new Expression[] { Identifier.For("i0") }),
                        SystemTypes.Boolean, sourceContext),
                true_bl, new Block());
            forStm.Body.Statements.Add(if_st);

            block.Statements.Add(forStm);
        }

        /// <summary>
        /// Generates code for the indexer
        /// </summary>
        /// <param name="indices"></param>
        /// <param name="strIndices"></param>
        /// <param name="indexer"></param>
        /// <param name="dim"></param>
        /// <param name="prefix"></param>
        private void AddingComplexIndexing(EXPRESSION_LIST indices, ExpressionList indexer, string strIndices, int index_info, int dim, string prefix, SourceContext sourceContext)
        {
            if (indices == null)
            {
                for (int i = 0; i < dim; i++)
                {
                    indexer.Add(Identifier.For("i" + i.ToString()));
                }
            }
            else
            {
                int i_s = 0;
                int i_r = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    switch (strIndices[index_info + i])
                    {
                        case 'S':
                            {
                                indexer.Add(Identifier.For(prefix + "n_s" + i_s.ToString()));
                                i_s++;
                                break;
                            }
                        case 'R':
                            {
                                indexer.Add(new BinaryExpression(
                                    Identifier.For(prefix + "n_r" + i_r.ToString() + "_from"),
                                    new BinaryExpression(
                                        Identifier.For("i" + i_r.ToString()),
                                        Identifier.For(prefix + "n_r" + i_r.ToString() + "_by"),
                                        NodeType.Mul_Ovf, sourceContext),
                                    NodeType.Add_Ovf,
                                    sourceContext));
                                i_r++;
                                break;
                            }
                        case 'B':
                            {
                                indexer.Add(Identifier.For(prefix + "j" + i_r.ToString()));
                                i_r++;
                                break;
                            }
                        default: // 'C' or 'I'
                            {
                                indexer.Add(new Indexer(Identifier.For(prefix + "n_r" + i_r.ToString()),
                                    new ExpressionList(new Expression[] { Identifier.For("i" + i_r.ToString()) })));
                                i_r++;
                                break;
                            }
                    }
                }
            }
        }
        
        /// <summary>
        /// Function which inserts checking for n_i variables to check whether arrays are equal in every dimension
        /// </summary>
        /// <param name="block"></param>
        /// <param name="indicesStr"></param>
        /// <param name="index_info"></param>
        /// <param name="indices"></param>
        /// <param name="prefix1"></param>
        /// <param name="prefix2"></param>
        /// <param name="returnDim"></param>
        /// <param name="bvIndices"></param>
        /// <param name="sourceContext"></param>
        private void AddingNiChecking(Block block, string indicesStr, int index_info, EXPRESSION_LIST indices, string prefix1, string prefix2, int returnDim, int[] bvIndices, SourceContext sourceContext)
        {
                // ~    prefix1ni == prefix2ni
            for (int i = 0; i < returnDim; i++)
            {
                block.Statements.Add(GenerateIfStatementWithThrow(
                    Identifier.For(prefix1 + "n" + i.ToString()),
                    Identifier.For(prefix2 + "n" + i.ToString()),
                    NodeType.Ne,
                    STANDARD.IncompatibleSizesException,
                    sourceContext.StartLine,
                    sourceContext.StartColumn,
                    sourceContext));
            }
        }
        
        /// <summary>
        /// Generates set of index variables i0, i1,..., i index-1 for iterators and
        /// variables n0, n1, ..., n lengths-1 to store lengths of arrays
        /// </summary>
        /// <param name="index">number of local integer variables for iterators</param>
        /// <param name="prefix1">prefix for the first set of variables</param>
        /// <param name="prefix2">prefix for the second set of variables</param>
        /// <param name="lengths1">number of local integer variables to store lengths of arrays of the name "prefix1 + ni"</param>
        /// <param name="lengths2">number of local integer variables to store lengths of arrays of the name "prefix2 + ni"</param>
        /// <returns></returns>
        private LocalDeclarationsStatement GenerateLocalIntVariables(int index, string prefix1, int lengths1, string prefix2, int lengths2, SourceContext sourceContext)
        {
            TypeNode rtype = (TypeNode)STANDARD.Integer.type.convert();
            LocalDeclarationsStatement rangeVar = new LocalDeclarationsStatement();
            rangeVar.Constant = false;
            rangeVar.Declarations = new LocalDeclarationList(index + lengths1 + lengths2);
            rangeVar.InitOnly = false;
            rangeVar.Type = rtype;

            Construct construct = new Construct();
            construct.Constructor = new MemberBinding(null, rtype);
            construct.Constructor.Type = SystemTypes.Type;
            // construct.SourceContext = this.sourceContext;
            construct.Type = rtype;
            construct.Operands = new ExpressionList(); // no params

            for (int i = 0; i < index; i++)
            {
                LocalDeclaration r = new LocalDeclaration();
                r.Name = Identifier.For("i" + i.ToString());
                // Generate initializer for the local declaration (see NEW.convert)
                r.InitialValue = construct;
                rangeVar.Declarations.Add(r);
            }

            for (int i = 0; i < lengths1; i++)
            {
                LocalDeclaration r = new LocalDeclaration();
                r.Name = Identifier.For(prefix1 + "n" + i.ToString());
                // Generate initializer for the local declaration (see NEW.convert)
                r.InitialValue = construct;
                rangeVar.Declarations.Add(r);
            }

            for (int i = 0; i < lengths2; i++)
            {
                LocalDeclaration r = new LocalDeclaration();
                r.Name = Identifier.For(prefix2 + "n" + i.ToString());
                // Generate initializer for the local declaration (see NEW.convert)
                r.InitialValue = construct;
                rangeVar.Declarations.Add(r);
            }

            return rangeVar;
        }

        /// <summary>
        /// Generates a returned variable named res of type type
        /// </summary>
        /// <param name="type">The type of the returned variable</param>
        /// <returns></returns>
        private LocalDeclarationsStatement GenerateLocalResVariable(TypeNode type, SourceContext sourceContext)
        {
            LocalDeclarationsStatement rangeVar = new LocalDeclarationsStatement();
            rangeVar.Constant = false;
            rangeVar.Declarations = new LocalDeclarationList(1);
            rangeVar.InitOnly = false;
            rangeVar.Type = type;

            Construct construct = new Construct();
            construct.Constructor = new MemberBinding(null, type);
            construct.Constructor.Type = SystemTypes.Type;
            // construct.SourceContext = this.sourceContext;
            construct.Type = type;
            construct.Operands = new ExpressionList();

            LocalDeclaration r = new LocalDeclaration();
            r.Name = Identifier.For("res");
            // Generate initializer for the local declaration (see NEW.convert)
            r.InitialValue = construct;
            rangeVar.Declarations.Add(r);

            return rangeVar;
        }

        /// <summary>
        /// Generates a returned array variable named res of type type with lengths from sizes
        /// </summary>
        /// <param name="type">The type of the array</param>
        /// <param name="sizes">The lengths of the array in every dimension</param>
        /// <returns></returns>
        private LocalDeclarationsStatement GenerateLocalResArray(ArrayTypeExpression type, ExpressionList sizes, SourceContext sourceContext)
        {
            LocalDeclarationsStatement rangeVar = new LocalDeclarationsStatement();
            rangeVar.Constant = false;
            rangeVar.Declarations = new LocalDeclarationList(1);
            rangeVar.InitOnly = false;
            rangeVar.Type = type;

            ConstructArray new_array = new ConstructArray();
            new_array.ElementType = type.ElementType;
            new_array.Operands = new ExpressionList();
            new_array.Rank = sizes.Length;

            foreach (Expression expr in sizes)
                new_array.Operands.Add(expr);

            LocalDeclaration r = new LocalDeclaration();
            r.Name = Identifier.For("res");
            // Generate initializer for the local declaration (see NEW.convert)
            r.InitialValue = new_array;
            rangeVar.Declarations.Add(r);

            return rangeVar;
        }

        private LocalDeclarationsStatement GenerateLocalIntVector(Expression size, int number, SourceContext sourceContext)
        {
            ArrayTypeExpression type = new ArrayTypeExpression(SystemTypes.Int32, 1);
            ExpressionList sizes = new ExpressionList(new Expression[] { size });

            LocalDeclarationsStatement rangeVar = new LocalDeclarationsStatement();
            rangeVar.Constant = false;
            rangeVar.Declarations = new LocalDeclarationList(1);
            rangeVar.InitOnly = false;
            rangeVar.Type = type;

            ConstructArray new_array = new ConstructArray();
            new_array.ElementType = SystemTypes.Int32;
            new_array.Operands = new ExpressionList();
            new_array.Rank = 1;
            new_array.Operands.Add(size);

            LocalDeclaration r = new LocalDeclaration();
            r.Name = Identifier.For("bvIndices" + number.ToString());
            // Generate initializer for the local declaration (see NEW.convert)
            r.InitialValue = new_array;
            rangeVar.Declarations.Add(r);

            return rangeVar;
        }

        /// <summary>
        /// Generates statement for (int i index = 0; i less then forTo; i += by)
        /// </summary>
        /// <param name="index">number of i variable</param>
        /// <param name="forTo">the higher bound</param>
        /// <param name="by">step; if by == null then by == 1</param>
        /// <returns></returns>
        private For GenerateFor(int index, Expression forTo, Expression by, SourceContext sourceContext)
        {

            For forStatement = new For();
            Expression forFrom = new Literal(0, SystemTypes.Int32);
            Expression forBy = (by != null) ? (Expression)by : new Literal(1, SystemTypes.Int32);

            //forStatement.SourceContext = base.sourceContext;


            // Making condition
            BinaryExpression condition = new BinaryExpression();

            condition.NodeType = NodeType.Lt;

            condition.Operand1 = Identifier.For("i" + index.ToString());
            condition.Operand2 = forTo;
            condition.SourceContext = sourceContext;
            // condition.SourceContext = range.to.sourceContext;
            forStatement.Condition = condition;


            // Making incrementer
            forStatement.Incrementer = new StatementList();
            AssignmentStatement assignment = new AssignmentStatement();
            assignment.NodeType = NodeType.AssignmentStatement;
            assignment.Operator = NodeType.Add;
            assignment.Source = forBy;
            //assignment.SourceContext = base.sourceContext;
            assignment.Target = Identifier.For("i" + index.ToString());

            forStatement.Incrementer.Add(assignment);


            // Making initializer
            forStatement.Initializer = new StatementList();
            AssignmentStatement initializer = new AssignmentStatement();
            initializer.NodeType = NodeType.AssignmentStatement;
            initializer.Operator = NodeType.Nop;
            initializer.Source = (Expression)new Literal(0, SystemTypes.Int32);
            initializer.Target = Identifier.For("i" + index.ToString());
            // initializer.SourceContext = asexpr.sourceContext;

            forStatement.Initializer.Add(initializer);
            forStatement.Body = new Block();
            forStatement.Body.Statements = new StatementList();

            return forStatement;
        }

        /// <summary>
        /// Generates statement for (int i index = forFrom; i less then forTo; i += by)
        /// </summary>
        /// <param name="index">number of i variable</param>
        /// <param name="forFrom">the lower bound</param>
        /// <param name="forTo">the higher bound</param>
        /// <param name="by">step; if by == null then by == 1</param>
        /// <returns></returns>
        private For GenerateFor(int index, Expression forFrom, Expression forTo, Expression by, SourceContext sourceContext)
        {
            For forStatement = new For();
            forStatement.SourceContext = sourceContext;
            Expression forBy = (by != null) ? (Expression)by : new Literal(1, SystemTypes.Int32);

            //forStatement.SourceContext = base.sourceContext;

            // Making condition
            BinaryExpression condition = new BinaryExpression();

            condition.NodeType = NodeType.Lt;

            condition.Operand1 = Identifier.For("i" + index.ToString());
            condition.Operand2 = forTo;
            condition.SourceContext = sourceContext;
            // condition.SourceContext = range.to.sourceContext;
            forStatement.Condition = condition;

            // Making incrementer
            forStatement.Incrementer = new StatementList();
            AssignmentStatement assignment = new AssignmentStatement();
            assignment.NodeType = NodeType.AssignmentStatement;
            assignment.Operator = NodeType.Add;
            assignment.Source = forBy;
            assignment.SourceContext = sourceContext;
            //assignment.SourceContext = base.sourceContext;
            assignment.Target = Identifier.For("i" + index.ToString());

            forStatement.Incrementer.Add(assignment);

            // Making initializer
            forStatement.Initializer = new StatementList();
            AssignmentStatement initializer = new AssignmentStatement();
            initializer.NodeType = NodeType.AssignmentStatement;
            initializer.Operator = NodeType.Nop;
            initializer.Source = forFrom;
            initializer.Target = Identifier.For("i" + index.ToString());
            // initializer.SourceContext = asexpr.sourceContext;

            forStatement.Initializer.Add(initializer);
            forStatement.Body = new Block();
            forStatement.Body.Statements = new StatementList();
            forStatement.SourceContext = sourceContext;
            forStatement.Body.SourceContext = sourceContext;

            return forStatement;
        }

        /// <summary>
        /// Generates if statement:
        /// if (operand1 boolOp operand2) { throw new throwMathException(startLine, startColumn); }
        /// </summary>
        /// <param name="operand1">Left operand in if condition</param>
        /// <param name="operand2">Right operand in if condition</param>
        /// <param name="boolOp">Operation in if condition (like NodeType.Ne or .Lt or .Le ...)</param>
        /// <param name="mathException">The exception to be generated if the condition is true;
        /// this exception should have the first constructor with 2 parameters: startLine and startColumn;
        /// all the IsMath exceptions have such a constructor</param>
        /// <param name="startLine"></param>
        /// <param name="startColumn"></param>
        /// <returns></returns>
        public If GenerateIfStatementWithThrow(Expression operand1, Expression operand2, 
            NodeType boolOp, TypeNode mathException, long startLine, int startColumn, SourceContext sourceContext)
        {
            If ifstm = new If();
            BinaryExpression ifcond = new BinaryExpression(operand1, operand2, boolOp, SystemTypes.Boolean, sourceContext);
            ifstm.Condition = ifcond;

            Throw throwstm = new Throw();
            Construct throwstm_constructor = new Construct();
            throwstm_constructor.Constructor = new MemberBinding(null, mathException.GetConstructors()[0]);
            // new QualifiedIdentifier(rtlName,Identifier.For("Halt"));
            throwstm_constructor.Type = mathException;
            throwstm_constructor.Operands = new ExpressionList();
            if (mathException != STANDARD.ZeroDivisionException)
            {
                throwstm_constructor.Operands.Add(new Literal(startLine, SystemTypes.Int64));
                throwstm_constructor.Operands.Add(new Literal(startColumn, SystemTypes.Int32));
            }
            throwstm.Expression = throwstm_constructor;

            ifstm.TrueBlock = new Block();
            ifstm.TrueBlock.Statements = new StatementList(new Statement[] { throwstm });
            ifstm.TrueBlock.SourceContext = sourceContext;
            ifstm.SourceContext = sourceContext;
            
            return ifstm;
        }

        #endregion

        #region GetMatrixMultiplication
        /// <summary>
        /// Returns appropriate function for matrix multiplication;
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <param name="leftDim">Left array rank (dimensions; real rank)</param>
        /// <param name="rightDim">Right array rank (dimensions; real rank)</param>
        /// <param name="leftRealDim">Real dimension of the left array (maybe there are ranges and simple indices)</param>
        /// <param name="rightRealDim">Real dimension of the right array (maybe there are ranges and simple indices)</param>
        /// <param name="leftType">Left array base type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="leftIndices">If left array is an indexer, it's indices</param>
        /// <param name="rightIndices">If right array is an indexer, it's indices</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opPlus">Type of plus operator</param>
        /// <param name="ovlPlus">If + was overloaded, the appropriate plus operator</param>
        /// <param name="opMult">Type of mult operator</param>
        /// <param name="ovlMult">If * was overloaded, the appropriate mult operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetMatrixMultiplication(int leftDim, int rightDim,
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            TypeNode leftType, TypeNode rightType,
            TypeNode returnType,
            bool isRetTypeSystem,
            NodeType opPlus, Expression ovlPlus, NodeType opMult, Expression ovlMult, SourceContext sourceContext)
        {
            Debug.Assert(leftDim < 3 && leftDim > 0);
            Debug.Assert(rightDim < 3 && rightDim > 0);
            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;

            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            Identifier name = Identifier.For(
                "MatrixMult" + strLeftIndicesTemp + leftDim.ToString() + "d" +
                strRightIndicesTemp + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                if (leftIndices == null)
                    leftArrayType.Rank = leftDim;
                else
                    leftArrayType.Rank = strLeftIndices.Length;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                if (rightIndices == null)
                    rightArrayType.Rank = rightDim;
                else
                    rightArrayType.Rank = strRightIndices.Length;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                int returnDim = leftDim + rightDim - 2;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim + 1, "left_", leftDim, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, strLeftIndices, 0, leftIndices, "left_", "left", leftDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, rightArrayType, strRightIndices, 0, rightIndices, "right_", "right", rightDim, bvRightIndices, sourceContext);



                #region 1d * 1d
                if (returnDim == 0)
                {
                    // Scalar

                    //Type MatrixMult11Type1Type2(Type1[] a, Type2[] b)
                    //{
                    //    int n = a.GetLength(0);
                    //    if (n != b.GetLength(0))
                    //        throw new Exception(...);
                    //    Type Res = 0;
                    //    for (int j = 0; j < n; i++)
                    //        Res += a[j] * b[j];
                    //    return Res;
                    //}

                    // ~    if (n != b.GetLength(0)) throw new Exception(...);
                    method.Body.Statements.Add(
                        GenerateIfStatementWithThrow(Identifier.For("left_" + "n0"),
                        Identifier.For("right_" + "n0"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));

                    // ~    Type Res = 0;
                    if (isRetTypeSystem)
                    {
                        //Res = new Type;
                        method.Body.Statements.Add(GenerateLocalResVariable(returnType, sourceContext));
                    }
                    else //we need to generate a flag variable and res = null
                    {
                        LocalDeclarationsStatement lds = new LocalDeclarationsStatement();
                        lds.Constant = false;
                        lds.Declarations = new LocalDeclarationList(1);
                        lds.InitOnly = false;
                        lds.Type = returnType;
                        LocalDeclaration ld = new LocalDeclaration();
                        ld.Name = Identifier.For("res");
                        ld.InitialValue = new Literal(null);
                        lds.Declarations.Add(ld);
                        method.Body.Statements.Add(lds);

                        LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                        lds1.Constant = false;
                        lds1.Declarations = new LocalDeclarationList(1);
                        lds1.InitOnly = false;
                        lds1.Type = SystemTypes.Boolean;
                        LocalDeclaration ld1 = new LocalDeclaration();
                        ld1.Name = Identifier.For("flag");
                        ld1.InitialValue = new Literal(false, SystemTypes.Boolean, sourceContext);
                        lds1.Declarations.Add(ld1);
                        method.Body.Statements.Add(lds1);
                    }

                    For forstm = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);
                    if (bvLeftIndicesLength + bvRightIndicesLength > 0) //there are boolean vectors
                    {
                        if (bvLeftIndicesLength > 0)
                        {
                            Construct construct = new Construct();
                            construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                            construct.Constructor.Type = SystemTypes.Int32;
                            construct.Type = SystemTypes.Int32;
                            construct.Operands = new ExpressionList(); // no params
                            LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                new LocalDeclaration(Identifier.For("left_j0"), construct), SystemTypes.Int32);
                            method.Body.Statements.Add(j_decl);

                            While whilestm = new While();
                            whilestm.Condition = new UnaryExpression(
                                new Indexer(Identifier.For("left_" + "n_r0"),
                                    new ExpressionList(new Expression[] { Identifier.For("left_j0") })),
                                NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                            whilestm.Body = new Block();
                            whilestm.Body.Statements = new StatementList();
                            whilestm.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("left_j0"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));

                            ((For)forstm).Body.Statements.Add(whilestm);
                        }
                        if (bvRightIndicesLength > 0)
                        {
                            Construct construct = new Construct();
                            construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                            construct.Constructor.Type = SystemTypes.Int32;
                            construct.Type = SystemTypes.Int32;
                            construct.Operands = new ExpressionList(); // no params
                            LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                new LocalDeclaration(Identifier.For("right_j0"), construct), SystemTypes.Int32);
                            method.Body.Statements.Add(j_decl);

                            While whilestm = new While();
                            whilestm.Condition = new UnaryExpression(
                                new Indexer(Identifier.For("right_" + "n_r0"),
                                    new ExpressionList(new Expression[] { Identifier.For("right_j0") })),
                                NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                            whilestm.Body = new Block();
                            whilestm.Body.Statements = new StatementList();
                            whilestm.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("right_j0"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));

                            ((For)forstm).Body.Statements.Add(whilestm);
                        }
                    }

                    AssignmentStatement asgnFor = new AssignmentStatement();
                    // TODO: case when operator is overloaded
                    asgnFor.Operator = NodeType.Nop;

                    BinaryExpression plus = new BinaryExpression();
                    plus.SourceContext = sourceContext;
                    BinaryExpression mult = new BinaryExpression();
                    mult.SourceContext = sourceContext;

                    mult.NodeType = opMult;
                    if (leftIndices == null)
                        mult.Operand1 = new Indexer(Identifier.For("left"), new ExpressionList(new Expression[] { Identifier.For("i0") }), leftType);
                    else
                    {
                        ExpressionList index = new ExpressionList();
                        int i_s = 0;
                        int i_r = 0;
                        for (int i = 0; i < strLeftIndices.Length; i++)
                        {
                            switch (strLeftIndices[i])
                            {
                                case 'S':
                                    {
                                        index.Add(Identifier.For("left_" + "n_s" + i_s.ToString()));
                                        i_s++;
                                        break;
                                    }
                                case 'R':
                                    {
                                        index.Add(new BinaryExpression(
                                            Identifier.For("left_" + "n_r" + i_r.ToString() + "_from"),
                                            new BinaryExpression(
                                                Identifier.For("i" + i_r.ToString()),
                                                Identifier.For("left_" + "n_r" + i_r.ToString() + "_by"),
                                                NodeType.Mul_Ovf,
                                                sourceContext),
                                            NodeType.Add_Ovf,
                                            sourceContext));
                                        i_r++;
                                        break;
                                    }
                                case 'B':
                                    {
                                        index.Add(Identifier.For("left_j" + i_r.ToString()));
                                        i_r++;
                                        break;
                                    }
                                default: // 'C' or 'I'
                                    {
                                        index.Add(new Indexer(Identifier.For("left_n_r" + i_r.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("i" + i_r.ToString()) })));
                                        i_r++;
                                        break;
                                    }
                            }
                        }
                        mult.Operand1 = new Indexer(Identifier.For("left"), index, leftType);
                    }
                    if (rightIndices == null)
                        mult.Operand2 = new Indexer(Identifier.For("right"), new ExpressionList(new Expression[] { Identifier.For("i0") }), rightType);
                    else
                    {
                        ExpressionList index = new ExpressionList();
                        int i_s = 0;
                        int i_r = 0;
                        for (int i = 0; i < strRightIndices.Length; i++)
                        {
                            switch (strRightIndices[i])
                            {
                                case 'S':
                                    {
                                        index.Add(Identifier.For("right_" + "n_s" + i_s.ToString()));
                                        i_s++;
                                        break;
                                    }
                                case 'R':
                                    {
                                        index.Add(new BinaryExpression(
                                            Identifier.For("right_" + "n_r" + i_r.ToString() + "_from"),
                                            new BinaryExpression(
                                                Identifier.For("i" + i_r.ToString()),
                                                Identifier.For("right_" + "n_r" + i_r.ToString() + "_by"),
                                                NodeType.Mul_Ovf,
                                                sourceContext),
                                            NodeType.Add_Ovf,
                                            sourceContext));
                                        i_r++;
                                        break;
                                    }
                                case 'B':
                                    {
                                        index.Add(Identifier.For("right_j" + i_r.ToString()));
                                        i_r++;
                                        break;
                                    }
                                default: // 'C' or 'I'
                                    {
                                        index.Add(new Indexer(Identifier.For("right_n_r" + i_r.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("i" + i_r.ToString()) })));
                                        i_r++;
                                        break;
                                    }
                            }
                        }
                        mult.Operand2 = new Indexer(Identifier.For("right"), index, rightType);
                    }

                    plus.NodeType = opPlus;
                    plus.Operand1 = Identifier.For("res");
                    plus.Operand2 = mult;

                    if (opMult == NodeType.MethodCall)
                    {
                        plus.Operand2 = new MethodCall(
                            ovlMult,
                            new ExpressionList(new Expression[] { 
                            mult.Operand1,
                            mult.Operand2}),
                            NodeType.Call);
                    }

                    asgnFor.Source = plus;

                    if (opPlus == NodeType.MethodCall)
                    {
                        asgnFor.Source = new MethodCall(
                            ovlPlus,
                            new ExpressionList(new Expression[] { 
                            plus.Operand1,
                            plus.Operand2}),
                            NodeType.Call);
                    }

                    asgnFor.Target = Identifier.For("res");

                    if (isRetTypeSystem)
                    {
                        forstm.Body.Statements.Add(asgnFor);
                    }
                    else
                    {
                        Block trueBlock = new Block(new StatementList(new Statement[] { asgnFor }), sourceContext);
                        Block falseBlock = new Block(new StatementList(new Statement[] {  
                            new AssignmentStatement(Identifier.For("res"), plus.Operand2.Clone() as Expression, NodeType.Nop, sourceContext), 
                            new AssignmentStatement(Identifier.For("flag"), new Literal(true, SystemTypes.Boolean, sourceContext), NodeType.Nop, sourceContext)
                            }),
                        sourceContext);

                        If ifstm = new If(
                            Identifier.For("flag"),
                            trueBlock,
                            falseBlock);

                        forstm.Body.Statements.Add(ifstm);
                    }

                    if (bvLeftIndicesLength > 0)
                    {
                        forstm.Body.Statements.Add(new AssignmentStatement(
                            Identifier.For("left_j0"),
                            new Literal(1, SystemTypes.Int32),
                            NodeType.Add));
                    }

                    if (bvRightIndicesLength > 0)
                    {
                        forstm.Body.Statements.Add(new AssignmentStatement(
                            Identifier.For("right_j0"),
                            new Literal(1, SystemTypes.Int32),
                            NodeType.Add));
                    }
                    method.Body.Statements.Add(forstm);

                    method.ReturnType = returnType;
                }
                #endregion

                else if (returnDim == 1)
                {
                    // Vector (2 cases)
                    #region 2d * 1d

                    if (leftDim == 2)
                    {
                        //Type[] MatrixMult21Type1Type2(Type1[,] A, Type2[] b)
                        //{
                        //    int m = A.GetLength(0);
                        //    int n = A.GetLength(1);
                        //    if (n != b.GetLength(0))
                        //        throw new Exception(...);
                        //    Type[] Res = new Type[m];
                        //    for (int i = 0; i < m; i++)
                        //        for (int j = 0; j < n; j++)
                        //            Res[i] += A[i, j] * b[j];
                        //    return Res;
                        //}

                        // n0 = left.GetLength(0);   ~   m = A.GetLength(0);
                        // n1 = left.GetLength(1);  ~   n = A.GetLength(1);

                        // ~    if (n != b.GetLength(0)) throw new Exception(...);
                        method.Body.Statements.Add(GenerateIfStatementWithThrow(
                            Identifier.For("left_" + "n1"),
                            Identifier.For("right_" + "n0"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine, sourceContext.StartColumn,
                            sourceContext));

                        // returnArrayType[] res = new returnArrayType[n0]  ~   Type[] Res = new Type[m];
                        ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                        returnArrayType.ElementType = returnType;
                        returnArrayType.Rank = returnDim;
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayType,
                            new ExpressionList(new Expression[] { Identifier.For("left_" + "n0") }),
                            sourceContext));

                        if (!isRetTypeSystem)
                        {
                            LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                            lds1.Constant = false;
                            lds1.Declarations = new LocalDeclarationList(1);
                            lds1.InitOnly = false;
                            lds1.Type = SystemTypes.Boolean;
                            LocalDeclaration ld1 = new LocalDeclaration();
                            ld1.Name = Identifier.For("flag");
                            ld1.InitialValue = new Literal(false, SystemTypes.Boolean, sourceContext);
                            lds1.Declarations.Add(ld1);
                            method.Body.Statements.Add(lds1);
                        }

                        For forstm0 = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);
                        For forstm1 = GenerateFor(1, Identifier.For("left_" + "n1"), null, sourceContext);

                        if (!isRetTypeSystem)
                        {
                            forstm0.Body.Statements.Add(new AssignmentStatement(Identifier.For("flag"), new Literal(false, SystemTypes.Boolean, sourceContext)));
                        }

                        ///////
                        //AddingWhileStmIntoFor(method.Body, new StatementList(new Statement[] { forstm0, forstm1 }),
                        //    bvLeftIndices, bvRightIndices);
                        int left_k = 0;

                        if (bvLeftIndicesLength + bvRightIndicesLength > 0) //there are boolean vectors
                        {
                            if (bvLeftIndicesLength > 0)
                            {
                                if (bvLeftIndices[0] == 0)
                                {
                                    Construct construct = new Construct();
                                    construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                    construct.Constructor.Type = SystemTypes.Int32;
                                    construct.Type = SystemTypes.Int32;
                                    construct.Operands = new ExpressionList(); // no params
                                    LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                        new LocalDeclaration(Identifier.For("left_j0"), construct), SystemTypes.Int32);
                                    method.Body.Statements.Add(j_decl);
                                }
                            }
                            if (bvRightIndicesLength > 0)
                            {
                                if (bvRightIndices[0] == 0)
                                {
                                    Construct construct = new Construct();
                                    construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                    construct.Constructor.Type = SystemTypes.Int32;
                                    construct.Type = SystemTypes.Int32;
                                    construct.Operands = new ExpressionList(); // no params
                                    LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                        new LocalDeclaration(Identifier.For("right_j0"), construct), SystemTypes.Int32);
                                    method.Body.Statements.Add(j_decl);
                                }
                            }

                            for (int i = 0; i < 2; i++)
                            {
                                if (bvLeftIndicesLength > 0)
                                {
                                    if (i != bvLeftIndices[left_k])
                                    {
                                        if (i + 1 == bvLeftIndices[left_k])
                                        {
                                            Construct construct = new Construct();
                                            construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                            construct.Constructor.Type = SystemTypes.Int32;
                                            construct.Type = SystemTypes.Int32;
                                            construct.Operands = new ExpressionList(); // no params
                                            LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                                new LocalDeclaration(Identifier.For("left_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                            if (i == 0) forstm0.Body.Statements.Add(j_decl);
                                            else if (i == 1) forstm1.Body.Statements.Add(j_decl);
                                        }

                                        if (i == 0) forstm0.Body.Statements.Add(forstm1);

                                    }
                                    else
                                    {
                                        While whilestm = new While();
                                        whilestm.Condition = new UnaryExpression(
                                            new Indexer(Identifier.For("left_" + "n_r" + i.ToString()),
                                                new ExpressionList(new Expression[] { Identifier.For("left_j" + i.ToString()) })),
                                            NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                        whilestm.Body = new Block();
                                        whilestm.Body.Statements = new StatementList();
                                        whilestm.Body.Statements.Add(new AssignmentStatement(
                                            Identifier.For("left_j" + i.ToString()),
                                            new Literal(1, SystemTypes.Int32),
                                            NodeType.Add));

                                        if (i == 0) forstm0.Body.Statements.Add(whilestm);
                                        else if (i == 1) forstm1.Body.Statements.Add(whilestm);

                                        if ((left_k < bvLeftIndicesLength - 1) && (i + 1 == bvLeftIndices[left_k + 1]))
                                        {
                                            Construct construct = new Construct();
                                            construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                            construct.Constructor.Type = SystemTypes.Int32;
                                            construct.Type = SystemTypes.Int32;
                                            construct.Operands = new ExpressionList(); // no params
                                            LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                                new LocalDeclaration(Identifier.For("left_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                            if (i == 0) forstm0.Body.Statements.Add(j_decl);
                                            if (i == 1) forstm1.Body.Statements.Add(j_decl);
                                        }

                                        if (i == 0)
                                        {
                                            forstm0.Body.Statements.Add(forstm1);
                                            forstm0.Body.Statements.Add(new AssignmentStatement(
                                                Identifier.For("left_j" + i.ToString()),
                                                new Literal(1, SystemTypes.Int32), NodeType.Add));
                                        }

                                        left_k++;
                                        if (left_k == bvLeftIndicesLength) left_k--;
                                    }
                                }

                            }

                            if (bvRightIndicesLength > 0)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("right_j0"), construct), SystemTypes.Int32);
                                forstm0.Body.Statements.Add(j_decl);

                                int i = 0;
                                While whilestm = new While();
                                whilestm.Condition = new UnaryExpression(
                                    new Indexer(Identifier.For("right_" + "n_r" + i.ToString()),
                                        new ExpressionList(new Expression[] { Identifier.For("right_j" + i.ToString()) })),
                                    NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                whilestm.Body = new Block();
                                whilestm.Body.Statements = new StatementList();
                                whilestm.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("right_j" + i.ToString()),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));

                                forstm1.Body.Statements.Add(whilestm);
                                if (bvLeftIndicesLength == 0) forstm0.Body.Statements.Add(forstm1);
                            }
                        }
                        else //no boolean vector indices
                        {
                            forstm0.Body.Statements.Add(forstm1);
                        }

                        ///////

                        // res[i0] += left[i0, i1] * right [i1]   ~   Res[i] += A[i, j] * b[j];
                        AssignmentStatement asgnFor = new AssignmentStatement();
                        // TODO: case when operator is overloaded
                        asgnFor.Operator = NodeType.Nop;

                        BinaryExpression mult = new BinaryExpression();
                        mult.SourceContext = sourceContext;
                        BinaryExpression plus = new BinaryExpression();
                        plus.SourceContext = sourceContext;

                        mult.NodeType = opMult;
                        if (leftIndices == null)
                            mult.Operand1 = new Indexer(Identifier.For("left"),
                                new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i1") }), leftType);
                        else
                        {
                            ExpressionList index = new ExpressionList();
                            int i_s = 0;
                            int i_r = 0;
                            for (int i = 0; i < strLeftIndices.Length; i++)
                            {
                                switch (strLeftIndices[i])
                                {
                                    case 'S':
                                        {
                                            index.Add(Identifier.For("left_" + "n_s" + i_s.ToString()));
                                            i_s++;
                                            break;
                                        }
                                    case 'R':
                                        {
                                            index.Add(new BinaryExpression(
                                                Identifier.For("left_" + "n_r" + i_r.ToString() + "_from"),
                                                new BinaryExpression(
                                                    Identifier.For("i" + i_r.ToString()),
                                                    Identifier.For("left_" + "n_r" + i_r.ToString() + "_by"),
                                                    NodeType.Mul_Ovf,
                                                    sourceContext),
                                                NodeType.Add_Ovf,
                                                sourceContext));
                                            i_r++;
                                            break;
                                        }
                                    case 'B':
                                        {
                                            index.Add(Identifier.For("left_j" + i_r.ToString()));
                                            i_r++;
                                            break;
                                        }
                                    default: // 'C' or 'I'
                                        {
                                            index.Add(new Indexer(Identifier.For("left_n_r" + i_r.ToString()),
                                                new ExpressionList(new Expression[] { Identifier.For("i" + i_r.ToString()) })));
                                            i_r++;
                                            break;
                                        }
                                }
                            }
                            mult.Operand1 = new Indexer(Identifier.For("left"), index, leftType);
                        }
                        if (rightIndices == null)
                            mult.Operand2 = new Indexer(Identifier.For("right"), new ExpressionList(new Expression[] { Identifier.For("i1") }), rightType);
                        else
                        {
                            ExpressionList index = new ExpressionList();
                            int i_s = 0;
                            int i_r = 0;
                            for (int i = 0; i < strRightIndices.Length; i++)
                            {
                                switch (strRightIndices[i])
                                {
                                    case 'S':
                                        {
                                            index.Add(Identifier.For("right_" + "n_s" + i_s.ToString()));
                                            i_s++;
                                            break;
                                        }
                                    case 'R':
                                        {
                                            index.Add(new BinaryExpression(
                                                Identifier.For("right_" + "n_r" + i_r.ToString() + "_from"),
                                                new BinaryExpression(
                                                    Identifier.For("i1"),
                                                    Identifier.For("right_" + "n_r" + i_r.ToString() + "_by"),
                                                    NodeType.Mul_Ovf,
                                                    sourceContext),
                                                NodeType.Add_Ovf,
                                                sourceContext));
                                            i_r++;
                                            break;
                                        }
                                    case 'B':
                                        {
                                            index.Add(Identifier.For("right_j" + i_r.ToString()));
                                            i_r++;
                                            break;
                                        }
                                    default: // 'C' or 'I'
                                        {
                                            index.Add(new Indexer(Identifier.For("right_n_r" + i_r.ToString()),
                                                new ExpressionList(new Expression[] { Identifier.For("i1") })));
                                            i_r++;
                                            break;
                                        }
                                }
                            }
                            mult.Operand2 = new Indexer(Identifier.For("right"), index, rightType);
                        }

                        plus.NodeType = opPlus;
                        plus.Operand1 = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);
                        plus.Operand2 = mult;

                        if (opMult == NodeType.MethodCall)
                        {
                            plus.Operand2 = new MethodCall(
                                ovlMult,
                                new ExpressionList(new Expression[] { 
                            mult.Operand1,
                            mult.Operand2}),
                                NodeType.Call);
                        }

                        asgnFor.Source = plus;

                        if (opPlus == NodeType.MethodCall)
                        {
                            asgnFor.Source = new MethodCall(
                                ovlPlus,
                                new ExpressionList(new Expression[] { 
                            plus.Operand1,
                            plus.Operand2}),
                                NodeType.Call);
                        }

                        asgnFor.Target = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);

                        if (isRetTypeSystem)
                        {
                            forstm1.Body.Statements.Add(asgnFor);
                        }
                        else
                        {
                            Block trueBlock = new Block(new StatementList(new Statement[] { asgnFor }), sourceContext);
                            Block falseBlock = new Block(new StatementList(new Statement[] {  
                                new AssignmentStatement(plus.Operand1.Clone() as Expression, plus.Operand2.Clone() as Expression, NodeType.Nop, sourceContext), 
                                new AssignmentStatement(Identifier.For("flag"), new Literal(true, SystemTypes.Boolean, sourceContext), NodeType.Nop, sourceContext)
                            }),
                            sourceContext);

                            If ifstm = new If(
                                Identifier.For("flag"),
                                trueBlock,
                                falseBlock);

                            forstm1.Body.Statements.Add(ifstm);
                        }

                        if ((bvLeftIndicesLength > 0) &&
                            (bvLeftIndices[bvLeftIndicesLength - 1] == 1))
                        {
                            forstm1.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("left_j1"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                        }

                        if (bvRightIndicesLength > 0)
                        {
                            forstm1.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("right_j0"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                        }

                        method.Body.Statements.Add(forstm0);

                        method.ReturnType = returnArrayType;
                    }
                    #endregion

                    #region 1d * 2d
                    else
                    {
                        //Type[] MatrixMult12Type1Type2(Type1[] a, Type2[,] B)
                        //{
                        //    int n = a.GetLength(0);
                        //    if (n != B.GetLength(0))
                        //        throw new Exception(...);
                        //    int l = B.GetLength(1);
                        //    Type[] Res = new Type[l];
                        //    for (int k = 0; k < l; k++)
                        //        for (int j = 0; j < n; j++)
                        //            Res[k] += a[j] * B[j, k];
                        //    return Res;
                        //}

                        // n0 = right.GetLength(1);  ~   l = B.GetLength(1);
                        // n1 = left.GetLength(0);   ~   n = a.GetLength(0);


                        // ~    if (n != B.GetLength(0)) throw new Exception(...);                        
                        method.Body.Statements.Add(GenerateIfStatementWithThrow(
                            Identifier.For("right_" + "n0"),
                            Identifier.For("left_" + "n0"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine, sourceContext.StartColumn,
                            sourceContext));

                        // returnArrayType[] res = new returnArrayType[n0]  ~   Type[] Res = new Type[l];
                        ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                        returnArrayType.ElementType = returnType;
                        returnArrayType.Rank = returnDim;
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayType,
                            new ExpressionList(new Expression[] { Identifier.For("right_" + "n1") }),
                            sourceContext));

                        if (!isRetTypeSystem)
                        {
                            LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                            lds1.Constant = false;
                            lds1.Declarations = new LocalDeclarationList(1);
                            lds1.InitOnly = false;
                            lds1.Type = SystemTypes.Boolean;
                            LocalDeclaration ld1 = new LocalDeclaration();
                            ld1.Name = Identifier.For("flag");
                            ld1.InitialValue = new Literal(false, SystemTypes.Boolean, sourceContext);
                            lds1.Declarations.Add(ld1);
                            method.Body.Statements.Add(lds1);
                        }

                        For forstm0 = GenerateFor(0, Identifier.For("right_" + "n1"), null, sourceContext);
                        For forstm1 = GenerateFor(1, Identifier.For("left_" + "n0"), null, sourceContext);

                        if (!isRetTypeSystem)
                        {
                            forstm0.Body.Statements.Add(new AssignmentStatement(Identifier.For("flag"), new Literal(false, SystemTypes.Boolean, sourceContext)));
                        }

                        ///////
                        //AddingWhileStmIntoFor(method.Body, new StatementList(new Statement[] { forstm0, forstm1 }),
                        //    bvLeftIndices, bvRightIndices);

                        if (bvRightIndicesLength + bvLeftIndicesLength > 0) //there are boolean vectors
                        {
                            if (bvRightIndicesLength == 1)
                            {
                                if (bvRightIndices[0] == 1)
                                {
                                    Construct construct = new Construct();
                                    construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                    construct.Constructor.Type = SystemTypes.Int32;
                                    construct.Type = SystemTypes.Int32;
                                    construct.Operands = new ExpressionList(); // no params
                                    LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                        new LocalDeclaration(Identifier.For("right_j1"), construct), SystemTypes.Int32);
                                    method.Body.Statements.Add(j_decl);

                                    While whilestm = new While();
                                    whilestm.Condition = new UnaryExpression(
                                        new Indexer(Identifier.For("right_" + "n_r1"),
                                            new ExpressionList(new Expression[] { Identifier.For("right_j1") })),
                                        NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                    whilestm.Body = new Block();
                                    whilestm.Body.Statements = new StatementList();
                                    whilestm.Body.Statements.Add(new AssignmentStatement(
                                        Identifier.For("right_j1"),
                                        new Literal(1, SystemTypes.Int32),
                                        NodeType.Add));

                                    forstm0.Body.Statements.Add(whilestm);
                                    forstm0.Body.Statements.Add(forstm1);
                                    forstm0.Body.Statements.Add(new AssignmentStatement(
                                            Identifier.For("right_j1"),
                                            new Literal(1, SystemTypes.Int32), NodeType.Add));
                                }

                                else //bvRightIndices[0] == 0
                                {
                                    Construct construct = new Construct();
                                    construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                    construct.Constructor.Type = SystemTypes.Int32;
                                    construct.Type = SystemTypes.Int32;
                                    construct.Operands = new ExpressionList(); // no params
                                    LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                        new LocalDeclaration(Identifier.For("right_j0"), construct), SystemTypes.Int32);
                                    forstm0.Body.Statements.Add(j_decl);

                                    While whilestm = new While();
                                    whilestm.Condition = new UnaryExpression(
                                        new Indexer(Identifier.For("right_" + "n_r0"),
                                            new ExpressionList(new Expression[] { Identifier.For("right_j0") })),
                                        NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                    whilestm.Body = new Block();
                                    whilestm.Body.Statements = new StatementList();
                                    whilestm.Body.Statements.Add(new AssignmentStatement(
                                        Identifier.For("right_j0"),
                                        new Literal(1, SystemTypes.Int32),
                                        NodeType.Add));
                                    forstm1.Body.Statements.Add(whilestm);
                                    forstm0.Body.Statements.Add(forstm1);
                                }
                            }
                            else if (bvRightIndicesLength == 2)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("right_j1"), construct), SystemTypes.Int32);
                                method.Body.Statements.Add(j_decl);

                                LocalDeclarationsStatement j_decl0 = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("right_j0"), (Expression)construct.Clone()), SystemTypes.Int32);
                                forstm0.Body.Statements.Add(j_decl0);

                                While whilestm1 = new While();
                                whilestm1.Condition = new UnaryExpression(
                                    new Indexer(Identifier.For("right_" + "n_r1"),
                                        new ExpressionList(new Expression[] { Identifier.For("right_j1") })),
                                    NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                whilestm1.Body = new Block();
                                whilestm1.Body.Statements = new StatementList();
                                whilestm1.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("right_j1"),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));

                                forstm0.Body.Statements.Add(whilestm1);
                                forstm0.Body.Statements.Add(forstm1);

                                While whilestm0 = new While();
                                whilestm0.Condition = new UnaryExpression(
                                    new Indexer(Identifier.For("right_" + "n_r0"),
                                        new ExpressionList(new Expression[] { Identifier.For("right_j0") })),
                                    NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                whilestm0.Body = new Block();
                                whilestm0.Body.Statements = new StatementList();
                                whilestm0.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("right_j0"),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));
                                forstm1.Body.Statements.Add(whilestm0);

                                forstm0.Body.Statements.Add(new AssignmentStatement(
                                        Identifier.For("right_j1"),
                                        new Literal(1, SystemTypes.Int32), NodeType.Add));
                            }

                            //Left Indices
                            if (bvLeftIndicesLength > 0)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("left_j0"), construct), SystemTypes.Int32);
                                method.Body.Statements.Add(j_decl);
                            }

                            if (bvLeftIndicesLength > 0)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("left_j0"), construct), SystemTypes.Int32);
                                forstm0.Body.Statements.Add(j_decl);

                                int i = 0;
                                While whilestm = new While();
                                whilestm.Condition = new UnaryExpression(
                                    new Indexer(Identifier.For("left_" + "n_r" + i.ToString()),
                                        new ExpressionList(new Expression[] { Identifier.For("left_j" + i.ToString()) })),
                                    NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                whilestm.Body = new Block();
                                whilestm.Body.Statements = new StatementList();
                                whilestm.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("left_j" + i.ToString()),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));

                                forstm1.Body.Statements.Add(whilestm);
                                if (bvRightIndicesLength == 0) forstm0.Body.Statements.Add(forstm1);
                            }
                        }
                        else //no boolean vector indices
                        {
                            forstm0.Body.Statements.Add(forstm1);
                        }

                        // res[i0] += left[i1] * right [i1, i0]   ~   Res[k] += a[j] * B[j, k];
                        AssignmentStatement asgnFor = new AssignmentStatement();
                        // TODO: case when operator is overloaded
                        asgnFor.Operator = NodeType.Nop;

                        BinaryExpression mult = new BinaryExpression();
                        mult.SourceContext = sourceContext;
                        BinaryExpression plus = new BinaryExpression();
                        plus.SourceContext = sourceContext;

                        mult.NodeType = opMult;
                        if (leftIndices == null)
                            mult.Operand1 = new Indexer(Identifier.For("left"),
                                new ExpressionList(new Expression[] { Identifier.For("i1") }), leftType);
                        else
                        {
                            ExpressionList index = new ExpressionList();
                            int i_s = 0;
                            int i_r = 0;
                            for (int i = 0; i < strLeftIndices.Length; i++)
                            {
                                switch (strLeftIndices[i])
                                {
                                    case 'S':
                                        {
                                            index.Add(Identifier.For("left_" + "n_s" + i_s.ToString()));
                                            i_s++;
                                            break;
                                        }
                                    case 'R':
                                        {
                                            index.Add(new BinaryExpression(
                                                Identifier.For("left_" + "n_r" + i_r.ToString() + "_from"),
                                                new BinaryExpression(
                                                    Identifier.For("i1"),
                                                    Identifier.For("left_" + "n_r" + i_r.ToString() + "_by"),
                                                    NodeType.Mul_Ovf,
                                                    sourceContext),
                                                NodeType.Add_Ovf,
                                                sourceContext));
                                            i_r++;
                                            break;
                                        }
                                    case 'B':
                                        {
                                            index.Add(Identifier.For("left_j" + i_r.ToString()));
                                            i_r++;
                                            break;
                                        }
                                    default: // 'C' or 'I'
                                        {
                                            index.Add(new Indexer(Identifier.For("left_n_r" + i_r.ToString()),
                                                new ExpressionList(new Expression[] { Identifier.For("i1") })));
                                            i_r++;
                                            break;
                                        }
                                }
                            }
                            mult.Operand1 = new Indexer(Identifier.For("left"), index, leftType);
                        }
                        if (rightIndices == null)
                            mult.Operand2 = new Indexer(Identifier.For("right"),
                                new ExpressionList(new Expression[] { Identifier.For("i1"), Identifier.For("i0") }), rightType);
                        else
                        {
                            ExpressionList index = new ExpressionList();
                            bool flag = false;
                            int i_s = 0;
                            int i_r = 0;
                            for (int i = 0; i < strRightIndices.Length; i++)
                            {
                                switch (strRightIndices[i])
                                {
                                    case 'S':
                                        {
                                            index.Add(Identifier.For("right_" + "n_s" + i_s.ToString()));
                                            i_s++;
                                            break;
                                        }
                                    case 'R':
                                        {
                                            if (!flag)
                                            {
                                                index.Add(new BinaryExpression(
                                                    Identifier.For("right_" + "n_r" + i_r.ToString() + "_from"),
                                                    new BinaryExpression(
                                                        Identifier.For("i1"),
                                                        Identifier.For("right_" + "n_r" + i_r.ToString() + "_by"),
                                                        NodeType.Mul_Ovf,
                                                        sourceContext),
                                                    NodeType.Add_Ovf,
                                                    sourceContext));
                                                i_r++;
                                                flag = true;
                                                break;
                                            }
                                            else
                                            {
                                                index.Add(new BinaryExpression(
                                                    Identifier.For("right_" + "n_r" + i_r.ToString() + "_from"),
                                                    new BinaryExpression(
                                                        Identifier.For("i0"),
                                                        Identifier.For("right_" + "n_r" + i_r.ToString() + "_by"),
                                                        NodeType.Mul_Ovf,
                                                        sourceContext),
                                                    NodeType.Add_Ovf,
                                                    sourceContext));
                                                i_r++;
                                                break;
                                            }
                                        }
                                    case 'B':
                                        {
                                            index.Add(Identifier.For("right_j" + i_r.ToString()));
                                            i_r++;
                                            flag = true;
                                            break;
                                        }
                                    default: // 'C' or 'I'
                                        {
                                            if (!flag)
                                            {
                                                index.Add(new Indexer(Identifier.For("right_n_r" + i_r.ToString()),
                                                    new ExpressionList(new Expression[] { Identifier.For("i1") })));
                                                i_r++;
                                                flag = true;
                                                break;
                                            }
                                            else
                                            {
                                                {
                                                    index.Add(new Indexer(Identifier.For("right_n_r" + i_r.ToString()),
                                                        new ExpressionList(new Expression[] { Identifier.For("i0") })));
                                                    i_r++;
                                                    break;
                                                }
                                            }
                                        }
                                }
                            }
                            mult.Operand2 = new Indexer(Identifier.For("right"), index, rightType);
                        }

                        plus.NodeType = opPlus;
                        plus.Operand1 = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);
                        plus.Operand2 = mult;

                        if (opMult == NodeType.MethodCall)
                        {
                            plus.Operand2 = new MethodCall(
                                ovlMult,
                                new ExpressionList(new Expression[] { 
                            mult.Operand1,
                            mult.Operand2}),
                                NodeType.Call);
                        }

                        asgnFor.Source = plus;

                        if (opPlus == NodeType.MethodCall)
                        {
                            asgnFor.Source = new MethodCall(
                                ovlPlus,
                                new ExpressionList(new Expression[] { 
                            plus.Operand1,
                            plus.Operand2}),
                                NodeType.Call);
                        }

                        asgnFor.Target = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);

                        if (isRetTypeSystem)
                        {
                            forstm1.Body.Statements.Add(asgnFor);
                        }
                        else
                        {
                            Block trueBlock = new Block(new StatementList(new Statement[] { asgnFor }), sourceContext);
                            Block falseBlock = new Block(new StatementList(new Statement[] {  
                                new AssignmentStatement(plus.Operand1.Clone() as Expression, plus.Operand2.Clone() as Expression, NodeType.Nop, sourceContext), 
                                new AssignmentStatement(Identifier.For("flag"), new Literal(true, SystemTypes.Boolean, sourceContext), NodeType.Nop, sourceContext)
                            }),
                            sourceContext);

                            If ifstm = new If(
                                Identifier.For("flag"),
                                trueBlock,
                                falseBlock);

                            forstm1.Body.Statements.Add(ifstm);
                        }

                        if ((bvRightIndicesLength > 0) &&
                            (bvRightIndices[0] == 0))
                        {
                            forstm1.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("right_j0"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                        }

                        if (bvLeftIndicesLength > 0)
                        {
                            forstm1.Body.Statements.Add(new AssignmentStatement(
                                Identifier.For("left_j0"),
                                new Literal(1, SystemTypes.Int32),
                                NodeType.Add));
                        }

                        method.Body.Statements.Add(forstm0);

                        method.ReturnType = returnArrayType;
                    }
                    #endregion
                }

                #region 2d * 2d
                else
                {
                    // Matrix

                    //Type[,] MatrixMult22Type1Type2(Type1[,] A, Type2[,] B)
                    //{
                    //    int m = A.GetLength(0);
                    //    int n = A.GetLength(1);
                    //    if (n != B.GetLength(0))
                    //        throw new Exception(...);
                    //    int l = B.GetLength(1);
                    //    Type[,] Res = new Type[m, l];
                    //    for (int i = 0; i < m; i++)
                    //        for (int k = 0; k < l; k++)
                    //            for (int j = 0; j < n; j++)
                    //                Res[i, k] += A[i, j] * B[j, k];
                    //    return Res;
                    //}

                    // n0 = left.GetLength(0);   ~   m = A.GetLength(0);
                    // n1 = left.GetLength(1);  ~   n = A.GetLength(1);
                    // n2 = right.GetLength(1);  ~   l = B.GetLength(1);

                    // ~    if (n1 != B.GetLength(0)) throw new Exception(...);
                    method.Body.Statements.Add(GenerateIfStatementWithThrow(Identifier.For("left_" + "n1"),
                        Identifier.For("right_" + "n0"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine, sourceContext.StartColumn,
                        sourceContext));

                    // returnArrayType[] res = new returnArrayType[n0, n2]  ~   Type[,] Res = new Type[m, l];
                    ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                    returnArrayType.ElementType = returnType;
                    returnArrayType.Rank = returnDim;
                    method.Body.Statements.Add(GenerateLocalResArray(returnArrayType,
                        new ExpressionList(new Expression[] { Identifier.For("left_" + "n0"), Identifier.For("right_" + "n1") }),
                        sourceContext));

                    if (!isRetTypeSystem)
                    {
                        LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                        lds1.Constant = false;
                        lds1.Declarations = new LocalDeclarationList(1);
                        lds1.InitOnly = false;
                        lds1.Type = SystemTypes.Boolean;
                        LocalDeclaration ld1 = new LocalDeclaration();
                        ld1.Name = Identifier.For("flag");
                        ld1.InitialValue = new Literal(false, SystemTypes.Boolean, sourceContext);
                        lds1.Declarations.Add(ld1);
                        method.Body.Statements.Add(lds1);
                    }

                    For forstm0 = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);
                    For forstm2 = GenerateFor(2, Identifier.For("right_" + "n1"), null, sourceContext);
                    For forstm1 = GenerateFor(1, Identifier.For("left_" + "n1"), null, sourceContext);

                    if (!isRetTypeSystem)
                    {
                        forstm2.Body.Statements.Add(new AssignmentStatement(Identifier.For("flag"), new Literal(false, SystemTypes.Boolean, sourceContext)));
                    }

                    //AddingWhileStmIntoFor
                    //
                    int left_k = 0;
                    int right_k = 0;

                    if (bvLeftIndicesLength + bvRightIndicesLength > 0) //there are boolean vectors
                    {
                        if (bvLeftIndicesLength > 0)
                        {
                            if (bvLeftIndices[0] == 0)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("left_j0"), construct), SystemTypes.Int32);
                                method.Body.Statements.Add(j_decl);
                            }
                        }
                        if (bvRightIndicesLength > 0)
                        {
                            if (bvRightIndices[0] == 0)
                            {
                                Construct construct = new Construct();
                                construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                construct.Constructor.Type = SystemTypes.Int32;
                                construct.Type = SystemTypes.Int32;
                                construct.Operands = new ExpressionList(); // no params
                                LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                    new LocalDeclaration(Identifier.For("right_j0"), construct), SystemTypes.Int32);
                                forstm2.Body.Statements.Add(j_decl);
                            }
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            bool flag0 = false, flag1 = false, flag2 = false;
                            if (bvLeftIndicesLength > 0)
                            {
                                if (i != bvLeftIndices[left_k])
                                {
                                    if (i + 1 == bvLeftIndices[left_k])
                                    {
                                        Construct construct = new Construct();
                                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                        construct.Constructor.Type = SystemTypes.Int32;
                                        construct.Type = SystemTypes.Int32;
                                        construct.Operands = new ExpressionList(); // no params
                                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                            new LocalDeclaration(Identifier.For("left_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                        if (i == 0) forstm2.Body.Statements.Add(j_decl);
                                    }
                                    if (i < 2)
                                    {
                                        flag0 = true;
                                    }
                                }
                                else
                                {
                                    While whilestm = new While();
                                    whilestm.Condition = new UnaryExpression(
                                        new Indexer(Identifier.For("left_" + "n_r" + i.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("left_j" + i.ToString()) })),
                                        NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                    whilestm.Body = new Block();
                                    whilestm.Body.Statements = new StatementList();
                                    whilestm.Body.Statements.Add(new AssignmentStatement(
                                        Identifier.For("left_j" + i.ToString()),
                                        new Literal(1, SystemTypes.Int32),
                                        NodeType.Add));

                                    if (i == 0) forstm0.Body.Statements.Add(whilestm);
                                    else if (i == 1) forstm1.Body.Statements.Add(whilestm);

                                    if ((left_k < bvLeftIndicesLength - 1) && (i + 1 == bvLeftIndices[left_k + 1]))
                                    {
                                        Construct construct = new Construct();
                                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                        construct.Constructor.Type = SystemTypes.Int32;
                                        construct.Type = SystemTypes.Int32;
                                        construct.Operands = new ExpressionList(); // no params
                                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                            new LocalDeclaration(Identifier.For("left_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                        if (i == 0) forstm2.Body.Statements.Add(j_decl);
                                    }

                                    if (i < 2)
                                    {
                                        flag1 = true;
                                    }

                                    left_k++;
                                    if (left_k == bvLeftIndicesLength) left_k--;
                                }
                            }
                            if (bvRightIndicesLength > 0)
                            {
                                if (i != bvRightIndices[right_k])
                                {
                                    if (i + 1 == bvRightIndices[right_k])
                                    {
                                        Construct construct = new Construct();
                                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                        construct.Constructor.Type = SystemTypes.Int32;
                                        construct.Type = SystemTypes.Int32;
                                        construct.Operands = new ExpressionList(); // no params
                                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                            new LocalDeclaration(Identifier.For("right_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                        if (i == 0) forstm0.Body.Statements.Add(j_decl);
                                    }
                                    flag0 = true;
                                }
                                else
                                {
                                    While whilestm = new While();
                                    whilestm.Condition = new UnaryExpression(
                                        new Indexer(Identifier.For("right_" + "n_r" + i.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("right_j" + i.ToString()) })),
                                        NodeType.LogicalNot, SystemTypes.Boolean, sourceContext);
                                    whilestm.Body = new Block();
                                    whilestm.Body.Statements = new StatementList();
                                    whilestm.Body.Statements.Add(new AssignmentStatement(
                                        Identifier.For("right_j" + i.ToString()),
                                        new Literal(1, SystemTypes.Int32),
                                        NodeType.Add));

                                    if (i == 0) forstm1.Body.Statements.Add(whilestm);
                                    else if (i == 1) forstm2.Body.Statements.Add(whilestm);

                                    if ((right_k < bvRightIndicesLength - 1) && (i + 1 == bvRightIndices[right_k + 1]))
                                    {
                                        Construct construct = new Construct();
                                        construct.Constructor = new MemberBinding(null, SystemTypes.Int32);
                                        construct.Constructor.Type = SystemTypes.Int32;
                                        construct.Type = SystemTypes.Int32;
                                        construct.Operands = new ExpressionList(); // no params
                                        LocalDeclarationsStatement j_decl = new LocalDeclarationsStatement(
                                            new LocalDeclaration(Identifier.For("right_j" + (i + 1).ToString()), construct), SystemTypes.Int32);
                                        if (i == 0) forstm0.Body.Statements.Add(j_decl);
                                    }

                                    if (i < 2)
                                    {
                                        flag2 = true;
                                    }

                                    right_k++;
                                    if (right_k == bvRightIndicesLength) right_k--;
                                }
                            }

                            if (flag0 || flag1 || flag2)
                            {
                                if (i == 0) forstm0.Body.Statements.Add(forstm2);
                                if (i == 1) forstm2.Body.Statements.Add(forstm1);
                            }

                            if (flag1)
                            {
                                if (i == 0) forstm0.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("left_j" + i.ToString()),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));
                            }
                            if (flag2)
                            {
                                if (i == 1) forstm2.Body.Statements.Add(new AssignmentStatement(
                                    Identifier.For("right_j" + i.ToString()),
                                    new Literal(1, SystemTypes.Int32),
                                    NodeType.Add));
                            }
                        }
                    }
                    else //no boolean vector indices
                    {
                        forstm2.Body.Statements.Add(forstm1);
                        forstm0.Body.Statements.Add(forstm2);
                    }
                    //end of AddingWhileStmIntoFor

                    // res[i0, i2] += left[i0, i1] * right [i1, i2]   ~   Res[i, k] += A[i, j] * B[j, k];
                    AssignmentStatement asgnFor = new AssignmentStatement();
                    // TODO: case when operator is overloaded
                    asgnFor.Operator = NodeType.Nop;

                    BinaryExpression mult = new BinaryExpression();
                    mult.SourceContext = sourceContext;
                    BinaryExpression plus = new BinaryExpression();
                    plus.SourceContext = sourceContext;

                    mult.NodeType = opMult;
                    if (leftIndices == null)
                    {
                        mult.Operand1 = new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i1") }), leftType);
                    }
                    else
                    {
                        ExpressionList index = new ExpressionList();
                        int i_s = 0;
                        int i_r = 0;
                        for (int i = 0; i < strLeftIndices.Length; i++)
                        {
                            switch (strLeftIndices[i])
                            {
                                case 'S':
                                    {
                                        index.Add(Identifier.For("left_" + "n_s" + i_s.ToString()));
                                        i_s++;
                                        break;
                                    }
                                case 'R':
                                    {
                                        index.Add(new BinaryExpression(
                                            Identifier.For("left_" + "n_r" + i_r.ToString() + "_from"),
                                            new BinaryExpression(
                                                Identifier.For("i" + i_r.ToString()),
                                                Identifier.For("left_" + "n_r" + i_r.ToString() + "_by"),
                                                NodeType.Mul_Ovf,
                                                sourceContext),
                                            NodeType.Add_Ovf,
                                            sourceContext));
                                        i_r++;
                                        break;
                                    }
                                case 'B':
                                    {
                                        index.Add(Identifier.For("left_j" + i_r.ToString()));
                                        i_r++;
                                        break;
                                    }
                                default: // 'C' or 'I'
                                    {
                                        index.Add(new Indexer(Identifier.For("left_n_r" + i_r.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("i" + i_r.ToString()) })));
                                        i_r++;
                                        break;
                                    }
                            }
                        }
                        mult.Operand1 = new Indexer(Identifier.For("left"), index, leftType);
                    }
                    if (rightIndices == null)
                    {
                        mult.Operand2 = new Indexer(Identifier.For("right"),
                            new ExpressionList(new Expression[] { Identifier.For("i1"), Identifier.For("i2") }), rightType);
                    }
                    else
                    {
                        ExpressionList index = new ExpressionList();
                        int i_s = 0;
                        int i_r = 0;
                        for (int i = 0; i < strRightIndices.Length; i++)
                        {
                            switch (strRightIndices[i])
                            {
                                case 'S':
                                    {
                                        index.Add(Identifier.For("right_" + "n_s" + i_s.ToString()));
                                        i_s++;
                                        break;
                                    }
                                case 'R':
                                    {
                                        index.Add(new BinaryExpression(
                                            Identifier.For("right_" + "n_r" + i_r.ToString() + "_from"),
                                            new BinaryExpression(
                                                Identifier.For("i" + (i_r + 1).ToString()),
                                                Identifier.For("right_" + "n_r" + i_r.ToString() + "_by"),
                                                NodeType.Mul_Ovf,
                                                sourceContext),
                                            NodeType.Add_Ovf,
                                            sourceContext));
                                        i_r++;
                                        break;
                                    }
                                case 'B':
                                    {
                                        index.Add(Identifier.For("right_j" + i_r.ToString()));
                                        i_r++;
                                        break;
                                    }
                                default: // 'C' or 'I'
                                    {
                                        index.Add(new Indexer(Identifier.For("right_n_r" + i_r.ToString()),
                                            new ExpressionList(new Expression[] { Identifier.For("i" + (i_r + 1).ToString()) })));
                                        i_r++;
                                        break;
                                    }
                            }
                        }
                        mult.Operand2 = new Indexer(Identifier.For("right"), index, rightType);
                    }

                    plus.NodeType = opPlus;
                    plus.Operand1 = new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i2") }), returnType);
                    plus.Operand2 = mult;

                    if (opMult == NodeType.MethodCall)
                    {
                        plus.Operand2 = new MethodCall(
                            ovlMult,
                            new ExpressionList(new Expression[] { 
                            mult.Operand1,
                            mult.Operand2}),
                            NodeType.Call);
                    }

                    asgnFor.Source = plus;

                    if (opPlus == NodeType.MethodCall)
                    {
                        asgnFor.Source = new MethodCall(
                            ovlPlus,
                            new ExpressionList(new Expression[] { 
                            plus.Operand1,
                            plus.Operand2}),
                            NodeType.Call);
                    }

                    asgnFor.Target = new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i2") }), returnType);

                    if (isRetTypeSystem)
                    {
                        forstm1.Body.Statements.Add(asgnFor);
                    }
                    else
                    {
                        Block trueBlock = new Block(new StatementList(new Statement[] { asgnFor }), sourceContext);
                        Block falseBlock = new Block(new StatementList(new Statement[] {  
                                new AssignmentStatement(plus.Operand1.Clone() as Expression, plus.Operand2.Clone() as Expression, NodeType.Nop, sourceContext), 
                                new AssignmentStatement(Identifier.For("flag"), new Literal(true, SystemTypes.Boolean, sourceContext), NodeType.Nop, sourceContext)
                            }),
                        sourceContext);

                        If ifstm = new If(
                            Identifier.For("flag"),
                            trueBlock,
                            falseBlock);

                        forstm1.Body.Statements.Add(ifstm);
                    }

                    if ((bvLeftIndicesLength > 0) &&
                    (bvLeftIndices[bvLeftIndicesLength - 1] == 1))
                    {
                        forstm1.Body.Statements.Add(new AssignmentStatement(
                            Identifier.For("left_j1"),
                            new Literal(1, SystemTypes.Int32),
                            NodeType.Add));
                    }

                    if ((bvRightIndicesLength > 0) &&
                        (bvRightIndices[0] == 0))
                    {
                        forstm1.Body.Statements.Add(new AssignmentStatement(
                            Identifier.For("right_j0"),
                            new Literal(1, SystemTypes.Int32),
                            NodeType.Add));
                    }

                    method.Body.Statements.Add(forstm0);

                    method.ReturnType = returnArrayType;
                }
                #endregion

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetSparseMatrixMultiplication
        /// <summary>
        /// Returns appropriate function for matrix multiplication;
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <param name="leftDim">Left array rank (dimensions; real rank)</param>
        /// <param name="rightDim">Right array rank (dimensions; real rank)</param>
        /// <param name="leftRealDim">Real dimension of the left array (maybe there are ranges and simple indices)</param>
        /// <param name="rightRealDim">Real dimension of the right array (maybe there are ranges and simple indices)</param>
        /// <param name="leftType">Left array base type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="leftIndices">If left array is an indexer, it's indices</param>
        /// <param name="rightIndices">If right array is an indexer, it's indices</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opPlus">Type of plus operator</param>
        /// <param name="ovlPlus">If + was overloaded, the appropriate plus operator</param>
        /// <param name="opMult">Type of mult operator</param>
        /// <param name="ovlMult">If * was overloaded, the appropriate mult operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetSparseMatrixMultiplication(int leftDim, int rightDim,
            TypeNode leftType, TypeNode rightType, 
            TypeNode returnType,
            NodeType opPlus, NodeType opMult, SourceContext sourceContext)
        {
            Debug.Assert(leftDim < 3 && leftDim > 0);
            Debug.Assert(rightDim < 3 && rightDim > 0);

            Identifier name = Identifier.For(
                "SparseMatrixMult" + leftDim.ToString() + "d" + rightDim.ToString() + "d" + 
                leftType.Name + rightType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                Module module = CONTEXT.symbolTable;

                TypeNode leftArrayType;
                if (leftDim == 2)
                    leftArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, leftType);
                else
                    leftArrayType = STANDARD.SparseVector.GetTemplateInstance(module, leftType);

                TypeNode rightArrayType;
                if (rightDim == 2)
                    rightArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, rightType);
                else
                    rightArrayType = STANDARD.SparseVector.GetTemplateInstance(module, rightType);

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                int returnDim = leftDim + rightDim - 2;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim + 1, "left_", leftDim, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, "", 0, null, "left_", "left", leftDim, null, sourceContext);
                AddingNi(method.Body, rightArrayType, "", 0, null, "right_", "right", rightDim, null, sourceContext);                

                #region 1d * 1d
                if (returnDim == 0)
                {
                    // Scalar

                    //Type MatrixMult11Type1Type2(Type1[] a, Type2[] b)
                    //{
                    //    int n = a.GetLength(0);
                    //    if (n != b.GetLength(0))
                    //        throw new Exception(...);
                    //    Type Res = 0;
                    //    for (int j = 0; j < n; i++)
                    //        Res += a[j] * b[j];
                    //    return Res;
                    //}

                    // ~    if (n != b.GetLength(0)) throw new Exception(...);
                    method.Body.Statements.Add(
                        GenerateIfStatementWithThrow(Identifier.For("left_" + "n0"),
                        Identifier.For("right_" + "n0"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));

                    // ~    Type Res = 0;
                    method.Body.Statements.Add(GenerateLocalResVariable(returnType, sourceContext));

                    For forstm = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);

                    forstm.Body.Statements.Add(new LocalDeclarationsStatement(
                            new LocalDeclaration(
                                Identifier.For("leftIndex"),
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("left"), Identifier.For("JA"), sourceContext),
                                        Identifier.For("IndexOf"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("i0") }),
                                    NodeType.Call),
                                NodeType.Nop),
                        SystemTypes.Int32));

                    forstm.Body.Statements.Add(new LocalDeclarationsStatement(
                            new LocalDeclaration(
                                Identifier.For("rightIndex"),
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("right"), Identifier.For("JA"), sourceContext),
                                        Identifier.For("IndexOf"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("i0") }),
                                    NodeType.Call),
                                NodeType.Nop),
                        SystemTypes.Int32));

                    If if1 = new If(
                    new BinaryExpression(
                        new BinaryExpression(
                            Identifier.For("leftIndex"), new Literal(-1, SystemTypes.Int32),
                            NodeType.Ne, sourceContext),
                        new BinaryExpression(
                            Identifier.For("rightIndex"), new Literal(-1, SystemTypes.Int32),
                            NodeType.Ne, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList(), sourceContext),
                    new Block(new StatementList(), sourceContext));

                    AssignmentStatement asgnFor = new AssignmentStatement();
                    asgnFor.Operator = NodeType.Nop;
                    BinaryExpression plus = new BinaryExpression();
                    plus.SourceContext = sourceContext;
                    BinaryExpression mult = new BinaryExpression();
                    mult.SourceContext = sourceContext;
                    mult.NodeType = opMult;
                    mult.Operand1 = new Indexer( 
                        new QualifiedIdentifier(Identifier.For("left"), Identifier.For("AN"), sourceContext),
                        new ExpressionList(new Expression[] { Identifier.For("leftIndex") }),
                        sourceContext);
                    mult.Operand2 = new Indexer(
                        new QualifiedIdentifier(Identifier.For("right"), Identifier.For("AN"), sourceContext),
                        new ExpressionList(new Expression[] { Identifier.For("rightIndex") }),
                        sourceContext);

                    plus.NodeType = opPlus;
                    plus.Operand1 = Identifier.For("res");
                    plus.Operand2 = mult;

                    asgnFor.Source = plus;
                    asgnFor.Target = Identifier.For("res");

                    if1.TrueBlock.Statements.Add(asgnFor);
                    forstm.Body.Statements.Add(if1);

                    method.Body.Statements.Add(forstm);
                    method.ReturnType = returnType;
                }
                #endregion

                else if (returnDim == 1)
                {
                    // Vector (2 cases)
                    #region 2d * 1d

                    if (leftDim == 2)
                    {
                        //Type[] MatrixMult21Type1Type2(Type1[,] A, Type2[] b)
                        //{
                        //    int m = A.GetLength(0);
                        //    int n = A.GetLength(1);
                        //    if (n != b.GetLength(0))
                        //        throw new Exception(...);
                        //    Type[] Res = new Type[m];
                        //    for (int i = 0; i < m; i++)
                        //        for (int j = 0; j < n; j++)
                        //            Res[i] += A[i, j] * b[j];
                        //    return Res;
                        //}

                        // n0 = left.GetLength(0);   ~   m = A.GetLength(0);
                        // n1 = left.GetLength(1);  ~   n = A.GetLength(1);

                        // ~    if (n != b.GetLength(0)) throw new Exception(...);
                        method.Body.Statements.Add(GenerateIfStatementWithThrow(
                            Identifier.For("left_" + "n1"),
                            Identifier.For("right_" + "n0"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine, sourceContext.StartColumn,
                            sourceContext));

                        // returnArrayType[] res = new returnArrayType[n0]  ~   Type[] Res = new Type[m];
                        ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                        returnArrayType.ElementType = returnType;
                        returnArrayType.Rank = returnDim;
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayType,
                            new ExpressionList(new Expression[] { Identifier.For("left_" + "n0") }), 
                            sourceContext));

                        For forstm0 = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);
                        For forstm1 = GenerateFor(1, Identifier.For("left_" + "n1"), null, sourceContext);

                        forstm0.Body.Statements.Add(forstm1);

                        ///////

                        // res[i0] += left[i0, i1] * right [i1]   ~   Res[i] += A[i, j] * b[j];
                        AssignmentStatement asgnFor = new AssignmentStatement();
                        asgnFor.Operator = NodeType.Nop;
                        
                        BinaryExpression mult = new BinaryExpression();
                        mult.SourceContext = sourceContext;
                        BinaryExpression plus = new BinaryExpression();
                        plus.SourceContext = sourceContext;

                        mult.NodeType = opMult;
                        mult.Operand1 = new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i1") }), leftType);

                        mult.Operand2 = new Indexer(Identifier.For("right"), 
                            new ExpressionList(new Expression[] { Identifier.For("i1") }), rightType);
    
                        plus.NodeType = opPlus;
                        plus.Operand1 = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);
                        plus.Operand2 = mult;

                        asgnFor.Source = plus;

                        asgnFor.Target = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);

                        forstm1.Body.Statements.Add(asgnFor);

                        method.Body.Statements.Add(forstm0);

                        method.ReturnType = returnArrayType;
                    }
                    #endregion

                    #region 1d * 2d
                    else
                    {
                        //Type[] MatrixMult12Type1Type2(Type1[] a, Type2[,] B)
                        //{
                        //    int n = a.GetLength(0);
                        //    if (n != B.GetLength(0))
                        //        throw new Exception(...);
                        //    int l = B.GetLength(1);
                        //    Type[] Res = new Type[l];
                        //    for (int k = 0; k < l; k++)
                        //        for (int j = 0; j < n; j++)
                        //            Res[k] += a[j] * B[j, k];
                        //    return Res;
                        //}

                        // n0 = right.GetLength(1);  ~   l = B.GetLength(1);
                        // n1 = left.GetLength(0);   ~   n = a.GetLength(0);


                        // ~    if (n != B.GetLength(0)) throw new Exception(...);                        
                        method.Body.Statements.Add(GenerateIfStatementWithThrow(
                            Identifier.For("right_" + "n0"),
                            Identifier.For("left_" + "n0"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine, sourceContext.StartColumn,
                            sourceContext));

                        // returnArrayType[] res = new returnArrayType[n0]  ~   Type[] Res = new Type[l];
                        ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                        returnArrayType.ElementType = returnType;
                        returnArrayType.Rank = returnDim;
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayType,
                            new ExpressionList(new Expression[] { Identifier.For("right_" + "n1") }),
                            sourceContext));

                        For forstm0 = GenerateFor(0, Identifier.For("right_" + "n1"), null, sourceContext);
                        For forstm1 = GenerateFor(1, Identifier.For("left_" + "n0"), null, sourceContext);

                        forstm0.Body.Statements.Add(forstm1);

                        // res[i0] += left[i1] * right [i1, i0]   ~   Res[k] += a[j] * B[j, k];
                        AssignmentStatement asgnFor = new AssignmentStatement();
                        // TODO: case when operator is overloaded
                        asgnFor.Operator = NodeType.Nop;
                        
                        BinaryExpression mult = new BinaryExpression();
                        mult.SourceContext = sourceContext;
                        BinaryExpression plus = new BinaryExpression();
                        plus.SourceContext = sourceContext;
                        
                        mult.NodeType = opMult;
                        mult.Operand1 = new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[] { Identifier.For("i1") }), leftType);
                        mult.Operand2 = new Indexer(Identifier.For("right"),
                                new ExpressionList(new Expression[] { Identifier.For("i1"), Identifier.For("i0") }), rightType);
            
                        plus.NodeType = opPlus;
                        plus.Operand1 = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);
                        plus.Operand2 = mult;

                        asgnFor.Source = plus;

                        asgnFor.Target = new Indexer(Identifier.For("res"),
                            new ExpressionList(new Expression[] { Identifier.For("i0") }), returnType);

                        forstm1.Body.Statements.Add(asgnFor);
                        
                        method.Body.Statements.Add(forstm0);

                        method.ReturnType = returnArrayType;
                    }
                    #endregion
                }

                #region 2d * 2d
                else
                {
                    // Matrix

                    //Type[,] MatrixMult22Type1Type2(Type1[,] A, Type2[,] B)
                    //{
                    //    int m = A.GetLength(0);
                    //    int n = A.GetLength(1);
                    //    if (n != B.GetLength(0))
                    //        throw new Exception(...);
                    //    int l = B.GetLength(1);
                    //    Type[,] Res = new Type[m, l];
                    //    for (int i = 0; i < m; i++)
                    //        for (int k = 0; k < l; k++)
                    //            for (int j = 0; j < n; j++)
                    //                Res[i, k] += A[i, j] * B[j, k];
                    //    return Res;
                    //}

                    // n0 = left.GetLength(0);   ~   m = A.GetLength(0);
                    // n1 = left.GetLength(1);  ~   n = A.GetLength(1);
                    // n2 = right.GetLength(1);  ~   l = B.GetLength(1);

                    // ~    if (n1 != B.GetLength(0)) throw new Exception(...);
                    method.Body.Statements.Add(GenerateIfStatementWithThrow(Identifier.For("left_" + "n1"),
                        Identifier.For("right_" + "n0"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine, sourceContext.StartColumn,
                        sourceContext));

                    Construct sparseConstructor = new Construct();
                    sparseConstructor.SourceContext = sourceContext;
                    sparseConstructor.Operands = new ExpressionList();

                    // returnArrayType[] res = new returnArrayType[n0, n2]  ~   Type[,] Res = new Type[m, l];
                    TypeNode returnArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, returnType);
                    sparseConstructor.Constructor = new MemberBinding(null,
                        returnArrayType.GetConstructor(
                        SystemTypes.Int64, SystemTypes.Int32, SystemTypes.Int32, SystemTypes.Int32));
                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartLine, SystemTypes.Int64));
                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartColumn, SystemTypes.Int32));
                    sparseConstructor.Operands.Add(Identifier.For("left_" + "n0"));
                    sparseConstructor.Operands.Add(Identifier.For("right_" + "n1"));

                    LocalDeclarationsStatement ldsRes = new LocalDeclarationsStatement();
                    ldsRes.Constant = false;
                    ldsRes.InitOnly = false;
                    ldsRes.Type = returnArrayType;
                    ldsRes.Declarations = new LocalDeclarationList(1);
                    ldsRes.Declarations.Add(new LocalDeclaration(Identifier.For("res"), sparseConstructor));
                    method.Body.Statements.Add(ldsRes);

                    TypeNode leftSPAType = STANDARD.RowSPA.GetTemplateInstance(module, leftType);
                    TypeNode rightSPAType = STANDARD.ColSPA.GetTemplateInstance(module, rightType);
                    method.Body.Statements.Add(
                    new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("leftSPA"), new Literal(null)),
                        leftSPAType));
                    method.Body.Statements.Add(
                        new LocalDeclarationsStatement(
                            new LocalDeclaration(Identifier.For("rightSPA"), new Literal(null)),
                            rightSPAType));

                    Construct intConstruct = new Construct(
                        new MemberBinding(null, SystemTypes.Int32),
                        new ExpressionList(), SystemTypes.Int32, sourceContext);

                    For forstm0 = GenerateFor(0, Identifier.For("left_" + "n0"), null, sourceContext);
                    For forstm2 = GenerateFor(2, Identifier.For("right_" + "n1"), null, sourceContext);
                    For forstm1 = GenerateFor(1, Identifier.For("left_" + "n1"), null, sourceContext);

                    forstm0.Body.Statements.Add(
                    new AssignmentStatement(
                        Identifier.For("leftSPA"),
                        new Construct(
                            new MemberBinding(null, leftSPAType.GetConstructors()[0]),
                            new ExpressionList(new Expression[] {
                                Identifier.For("left"), Identifier.For("i0")}),
                            sourceContext),
                        NodeType.Nop,
                        sourceContext));
                    forstm2.Body.Statements.Add(
                        new AssignmentStatement(
                            Identifier.For("rightSPA"),
                            new Construct(
                                new MemberBinding(null, rightSPAType.GetConstructors()[0]),
                                new ExpressionList(new Expression[] {
                                Identifier.For("right"), Identifier.For("i2")}),
                                sourceContext),
                            NodeType.Nop,
                            sourceContext));

                    forstm2.Body.Statements.Add(new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("tempValue"), 
                            new Construct(
                                new MemberBinding(null, returnType), 
                                new ExpressionList(), sourceContext),
                            NodeType.Nop),
                        returnType));

                    forstm0.Body.Statements.Add(new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("count"), intConstruct, NodeType.Nop),
                        SystemTypes.Int32));

                    forstm2.Body.Statements.Add(forstm1);
                    forstm0.Body.Statements.Add(forstm2);

                    forstm1.Body.Statements.Add(new LocalDeclarationsStatement(
                            new LocalDeclaration(
                                Identifier.For("leftIndex"),
                                new MethodCall(
                                    new MemberBinding(Identifier.For("leftSPA"),
                                        STANDARD.SPA.GetTemplateInstance(module, leftType).GetMembersNamed(Identifier.For("IndexOfElemInIndices"))[0]),
                                    new ExpressionList(new Expression[] {
                                        Identifier.For("i1")}),
                                    NodeType.Call),
                                NodeType.Nop),
                        SystemTypes.Int32));

                    forstm1.Body.Statements.Add(new LocalDeclarationsStatement(
                                new LocalDeclaration(
                                    Identifier.For("rightIndex"),
                                    new MethodCall(
                                        new MemberBinding(Identifier.For("rightSPA"),
                                            STANDARD.SPA.GetTemplateInstance(module, rightType).GetMembersNamed(Identifier.For("IndexOfElemInIndices"))[0]),
                                        new ExpressionList(new Expression[] {
                                        Identifier.For("i1")}),
                                        NodeType.Call),
                                    NodeType.Nop),
                            SystemTypes.Int32));


                    //int count = 0;
                    //RowSPA<int> leftSPA = new RowSPA<int>(left, i0);
                    //for (i2 = 0; i2 < right_n1; i2++)
                    //{
                    //    Int32 tempValue = new Int32();
                    //    ColSPA<int> rightSPA = new ColSPA<int>(right, i2);
                    //    for (i1 = 0; i1 < left_n1; i1++)
                    //    {
                    //        int leftIndex = leftSPA.IndexOfElemInIndices(i1);
                    //        int rightIndex = rightSPA.IndexOfElemInIndices(i1);
                    //        if ((leftIndex != -1) && (rightIndex != -1))
                    //        {
                    //            tempValue += leftSPA.Data[leftIndex] * rightSPA.Data[rightIndex];
                    //        }
                    //    }
                    //    if (tempValue != 0)
                    //    {
                    //        res.JA.Add(i2);
                    //        res.AN.Add(tempValue);
                    //        count++;
                    //    }
                    //}
                    //res.IA[i0 + 1] = res.IA[i0] + count;

                    If if1 = new If(
                        new BinaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Flags"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("i1") }),
                                sourceContext),
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Flags"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("i1") }),
                                sourceContext),
                            NodeType.LogicalAnd, sourceContext),
                        new Block(new StatementList(), sourceContext),
                        new Block(new StatementList(), sourceContext));

                    forstm1.Body.Statements.Add(if1);

                    // tempValue += left[i0, i1] * right [i1, i2]   ~   Res[i, k] += A[i, j] * B[j, k];
                    AssignmentStatement asgnFor = new AssignmentStatement();
                    asgnFor.Operator = NodeType.Add;
                    BinaryExpression mult = new BinaryExpression();
                    mult.SourceContext = sourceContext;
                    mult.NodeType = opMult;
                    mult.Operand1 = new Indexer(new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Data"), sourceContext),
                            new ExpressionList(new Expression[] { Identifier.For("leftIndex") }), leftType);
                    mult.Operand2 = new Indexer(new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Data"), sourceContext),
                            new ExpressionList(new Expression[] { Identifier.For("rightIndex") }), leftType);
                    asgnFor.Source = mult;
                    asgnFor.Target = Identifier.For("tempValue");
                    if1.TrueBlock.Statements.Add(asgnFor);

                    If ifZeroComp = new If(
                        new BinaryExpression(
                            Identifier.For("tempValue"), new Literal(0, returnType), NodeType.Ne),
                        new Block(new StatementList(), sourceContext),
                        new Block(new StatementList(), sourceContext));
                    ifZeroComp.TrueBlock.Statements.Add(new ExpressionStatement(
                            new MethodCall(new QualifiedIdentifier(
                                    new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AN"), sourceContext),
                                    Identifier.For("Add"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("tempValue") }),
                                NodeType.Call), sourceContext));
                    ifZeroComp.TrueBlock.Statements.Add(new ExpressionStatement(
                            new MethodCall(
                                new QualifiedIdentifier(
                                    new QualifiedIdentifier(Identifier.For("res"), Identifier.For("JA"), sourceContext),
                                    Identifier.For("Add"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("i2") }),
                                NodeType.Call), sourceContext));
                    ifZeroComp.TrueBlock.Statements.Add(new AssignmentStatement(
                        Identifier.For("count"), new Literal(1, SystemTypes.Int32), NodeType.Add, sourceContext));

                    forstm2.Body.Statements.Add(ifZeroComp);

                    //Res.IA[i0 + 1] = Res.IA[i0] + count;
                    forstm0.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("res"), Identifier.For("IA"), sourceContext),
                            new ExpressionList(new Expression[] {
                            new BinaryExpression(Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf)}),
                            sourceContext),
                        new BinaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("res"), Identifier.For("IA"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("i0") }),
                                sourceContext),
                            Identifier.For("count"),
                            NodeType.Add_Ovf),
                        NodeType.Nop,
                        sourceContext));

                    method.Body.Statements.Add(forstm0);

                    method.ReturnType = returnArrayType;
                }
                #endregion

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseArrayScalarOp
        /// <summary>
        /// Returns an appropriate function for an array-scalar standard operation (+, -, *, /, div, mod, less, greater...);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="leftType">Left array base type (elements type)</param>
        /// <param name="indices">Indices in the left part</param>
        /// <param name="rightType">>Right type (scalar type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Mul)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseArrayScalarOp(int leftDim, TypeNode leftType,
            EXPRESSION_LIST indices,
            TypeNode rightType, int returnDim, TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(leftDim > 0);
            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;

            string cur_name = "ElementWise", cur_nameTemp = "ElementWise";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            cur_name += "Array";
            cur_nameTemp += "Array";
            int index_info = 11; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            if (opType == NodeType.Add || opType == NodeType.Add_Ovf || opType == NodeType.Add_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "ScalarPlus" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Sub || opType == NodeType.Sub_Ovf || opType == NodeType.Sub_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "ScalarMin" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Mul || opType == NodeType.Mul_Ovf || opType == NodeType.Mul_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "ScalarMult" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Rem || opType == NodeType.Rem_Un)
            {
                name = Identifier.For(cur_nameTemp + "ScalarMod" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Div || opType == NodeType.Div_Un)
            {
                name = Identifier.For(cur_nameTemp + "ScalarDiv" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Eq)
            {
                name = Identifier.For(cur_nameTemp + "ScalarEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For(cur_nameTemp + "ScalarNonEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For(cur_nameTemp + "ScalarLess" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For(cur_nameTemp + "ScalarLessEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For(cur_nameTemp + "ScalarGreater" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For(cur_nameTemp + "ScalarGreaterEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Castclass)
            {
                name = Identifier.For(cur_nameTemp + "Cast" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_nameTemp + "Scalar" + "_" + ovlOp.ToString() + "_" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                
                //Type[Ndim] ElementWiseArrayScalarMultNType1Type2(Type1[Ndim] A, Type2 b)
                //{
                //Type[Ndim] Res;
                //Res = new Type[A.GetLength(0), ..., A.GetLength(n - 1)];
                //for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //  for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //      ...
                //          for(int in = 0; in < A.GetLength(n - 1); in++)
                //              Res[i1, i2, ..., in] = A[i1, i2, ..., in] * b;
                //return Res;
                //}
                
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                if (opType != NodeType.Castclass)
                    method.Parameters.Add(new Parameter(Identifier.For("right"), rightType));

                if (indices == null)
                    Debug.Assert(returnDim == leftDim);
                else
                {
                    AddingParameters(method, cur_name, index_info, indices, "", "left", sourceContext);
                    //Debug.Assert(returnDim == n_r);
                }

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "", returnDim, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, cur_name, index_info, indices, "", "left", returnDim, bvIndices, sourceContext);

                //Res = new Type[n0, ..., n returnDim - 1];
                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = returnDim;
                
                ExpressionList sizes = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    sizes.Add(Identifier.For("n" + i.ToString()));
                }

                method.Body.Statements.Add(GenerateLocalResArray(returnArrayType, sizes, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvIndices, "", null, "", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_target = new ExpressionList();
                ExpressionList index_source = new ExpressionList();

                for (int i = 0; i < returnDim; i++)
                {
                    index_target.Add(Identifier.For("i" + i.ToString()));
                }
                AddingComplexIndexing(indices, index_source, cur_name, index_info, returnDim, "", sourceContext);
                                
                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = opType;
                    internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source, leftType);
                    if (opType != NodeType.Castclass)
                        internalOp.Operand2 = Identifier.For("right");
                    else
                        internalOp.Operand2 = new MemberBinding(null, rightType);

                    asgnFor.Source = internalOp;
                }
                else //MethodCall
                {
                    asgnFor.Source = new MethodCall(
                        //CreateQualIdentifier((Identifier)ovlOp),
                        ovlOp,
                        new ExpressionList(new Expression[] { 
                            new Indexer(Identifier.For("left"), index_source, leftType),
                            Identifier.For("right") }),
                        NodeType.Call, returnType);
                }

                //type conversion (int16 + int16 = int32 in .net, but int16 in Zonnon)
                //it doesn't work implicitly on Mono, but does on Windows
                if (((returnType == SystemTypes.Int8) || (returnType == SystemTypes.Int16)
                        || (returnType == SystemTypes.UInt8 || (returnType == SystemTypes.UInt16))) &&
                    (opType != NodeType.MethodCall) && (opType != NodeType.Castclass))
                {
                    //We have to convert the result
                    BinaryExpression castop = new BinaryExpression(
                        internalOp,
                        new MemberBinding(null, returnType),
                        NodeType.Castclass,
                        sourceContext);

                    asgnFor.Source = castop;
                }

                if (opType == NodeType.Div || opType == NodeType.Div_Un)
                {
                    if ((returnType == SystemTypes.Single) || (returnType == SystemTypes.Double)
                        || (returnType == SystemTypes.Decimal))
                    {
                        //We have to convert the second operand for division to real
                        BinaryExpression castop = new BinaryExpression(
                            Identifier.For("right"),
                            new MemberBinding(null, SystemTypes.Double as TypeNode),
                            NodeType.Castclass,
                            sourceContext);

                        internalOp.Operand2 = castop;

                        // Here we have to wrap the division expression with a UnaryExpression
                        // with NodeType.Ckfinite. Unfortunately Resolver would not deal 
                        // correctly with this, so you either have to wait for the updated drop,
                        // or you have to override Resolver.InferTypeOfUnaryExpression.
                        UnaryExpression checkInfinite = new UnaryExpression();
                        checkInfinite.NodeType = NodeType.Ckfinite;
                        checkInfinite.Operand = internalOp;
                        checkInfinite.Type = internalOp.Type;

                        asgnFor.Source = checkInfinite;
                    }
                }

                //asgnFor.Source = internalOp;
                asgnFor.Target = new Indexer(Identifier.For("res"), index_target, returnType);
                ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);

                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseScalarArrayOp
        /// <summary>
        /// Returns an appropriate function for a scalar-array standard operation (+, -, *, /, div, mod, less, greater);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (scalar type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="indices">Indices in the left part</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Mul)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseScalarArrayOp(int rightDim, TypeNode leftType, 
            TypeNode rightType,
            EXPRESSION_LIST indices,
            int returnDim, TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string cur_name = "ElementWiseScalar", cur_nameTemp = "ElementWiseScalar";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            cur_name += "Array";
            cur_nameTemp += "Array";
            int index_info = 17; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            if (opType == NodeType.Add || opType == NodeType.Add_Ovf || opType == NodeType.Add_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "Plus" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Sub || opType == NodeType.Sub_Ovf || opType == NodeType.Sub_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "Min" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Mul || opType == NodeType.Mul_Ovf || opType == NodeType.Mul_Ovf_Un)
            {
                name = Identifier.For(cur_nameTemp + "Mult" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Rem || opType == NodeType.Rem_Un)
            {
                name = Identifier.For(cur_nameTemp + "Mod" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Div || opType == NodeType.Div_Un)
            {
                name = Identifier.For(cur_nameTemp + "Div" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Eq)
            {
                name = Identifier.For(cur_nameTemp + "Equal" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For(cur_nameTemp + "NonEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For(cur_nameTemp + "Less" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For(cur_nameTemp + "LessEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For(cur_nameTemp + "Greater" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For(cur_nameTemp + "GreaterEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_nameTemp + "_" + ovlOp.ToString() + "_" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type[Ndim] ElementWiseArrayScalarMultNType1Type2(Type1[Ndim] A, Type2 b)
                //{
                //Type[Ndim] Res;
                //Res = new Type[A.GetLength(0), ..., A.GetLength(n - 1)];
                //for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //  for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //      ...
                //          for(int in = 0; in < A.GetLength(n - 1); in++)
                //              Res[i1, i2, ..., in] = b * A[i1, i2, ..., in];
                //return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                if (indices == null)
                    Debug.Assert(returnDim == rightDim);
                else
                {
                    AddingParameters(method, cur_name, index_info, indices, "", "right", sourceContext);
                }

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "", returnDim, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, rightArrayType, cur_name, index_info, indices, "", "right", returnDim, bvIndices, sourceContext);

                //Res = new Type[n0, ..., n returnDim - 1];
                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = returnDim;

                ExpressionList sizes = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    sizes.Add(Identifier.For("n" + i.ToString()));
                }

                method.Body.Statements.Add(GenerateLocalResArray(returnArrayType, sizes, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, null, "", bvIndices, "", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_target = new ExpressionList();
                ExpressionList index_source = new ExpressionList();

                for (int i = 0; i < returnDim; i++)
                {
                    index_target.Add(Identifier.For("i" + i.ToString()));
                }
                AddingComplexIndexing(indices, index_source, cur_name, index_info, returnDim, "", sourceContext);

                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;                

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = opType;
                    internalOp.Operand1 = Identifier.For("left");
                    internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source, rightType);
                    asgnFor.Source = internalOp;
                }
                else //MethodCall
                {
                    asgnFor.Source = new MethodCall(
                        //CreateQualIdentifier((Identifier)ovlOp),
                        ovlOp,
                        new ExpressionList(new Expression[] { 
                            Identifier.For("left"),
                            new Indexer(Identifier.For("right"), index_source, rightType) }),
                        NodeType.Call, returnType);
                }

                //type conversion (int16 + int16 = int32 in .net, but int16 in Zonnon)
                //it doesn't work implicitly on Mono, but does on Windows
                if (((returnType == SystemTypes.Int8) || (returnType == SystemTypes.Int16)
                        || (returnType == SystemTypes.UInt8 || (returnType == SystemTypes.UInt16))) &&
                    (opType != NodeType.MethodCall))
                {
                    //We have to convert the result
                    BinaryExpression castop = new BinaryExpression(
                        internalOp,
                        new MemberBinding(null, returnType),
                        NodeType.Castclass,
                        sourceContext);

                    asgnFor.Source = castop;
                }

                if (opType == NodeType.Div || opType == NodeType.Div_Un)
                {
                    if ((returnType == SystemTypes.Single) || (returnType == SystemTypes.Double)
                        || (returnType == SystemTypes.Decimal))
                    {
                        //We have to convert the second operand for division to real
                        BinaryExpression castop = new BinaryExpression(
                            new Indexer(Identifier.For("right"), index_source, rightType),
                            new MemberBinding(null, SystemTypes.Double as TypeNode),
                            NodeType.Castclass,
                            sourceContext);

                        internalOp.Operand2 = castop;

                        // Here we have to wrap the division expression with a UnaryExpression
                        // with NodeType.Ckfinite. Unfortunately Resolver would not deal 
                        // correctly with this, so you either have to wait for the updated drop,
                        // or you have to override Resolver.InferTypeOfUnaryExpression.
                        UnaryExpression checkInfinite = new UnaryExpression();
                        checkInfinite.NodeType = NodeType.Ckfinite;
                        checkInfinite.Operand = internalOp;
                        checkInfinite.Type = internalOp.Type;

                        asgnFor.Source = checkInfinite;
                    }
                }

                //asgnFor.Source = internalOp;
                asgnFor.Target = new Indexer(Identifier.For("res"), index_target, returnType);
                ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);

                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseArrayArrayOp
        /// <summary>
        /// Returns an appropriate function for a array-array standard operation (+, -, .*, ./, div, mod, less, greater);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Mul)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseArrayArrayOp(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType, 
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            int returnDim, TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            //Debug.Assert(leftDim == rightDim);
            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;
            
            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            if (opType == NodeType.Add || opType == NodeType.Add_Ovf || opType == NodeType.Add_Ovf_Un)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayPlus" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Sub || opType == NodeType.Sub_Ovf || opType == NodeType.Sub_Ovf_Un)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayMin" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Mul || opType == NodeType.Mul_Ovf || opType == NodeType.Mul_Ovf_Un)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayMult" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Rem || opType == NodeType.Rem_Un)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayMod" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Div || opType == NodeType.Div_Un)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayDiv" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Eq)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayNonEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayLess" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayLessEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayGreater" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayGreaterEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "Array" + "_" + ovlOp.ToString() + "_" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type[Ndim] ElementWisePlusNType1Type2(Type1[Ndim] A, Type2[Ndim] B)
                //{
                //    int k1 = A.GetLength(0);
                //    ...
                //    int kn = A.GetLength(n - 1);
                //    if ((k1 != B.GetLength(0)) || ... || (kn != B.GetLength(n - 1)))
                //        throw new LengthsNotEqualException();
                //    Type[Ndim] Res;
                //    Res = new Type[k1, ..., kn];
                //    for (int i1 = 0; i1 < k1; i1++)
                //        for (int i2 = 0; i2 < k2; i2++)
                //            ...
                //                for(int in = 0; in < kn; in++)
                //                    Res[i1, i2, ..., in] = A[i1, i2, ..., in] + B[i1, i2, ..., in];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "left_", returnDim, "right_", returnDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, strLeftIndices, 0, leftIndices, "left_", "left", returnDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, rightArrayType, strRightIndices, 0, rightIndices, "right_", "right", returnDim, bvRightIndices, sourceContext);                
                AddingNiChecking(method.Body, strRightIndices, 0, rightIndices, "left_", "right_", returnDim, bvRightIndices, sourceContext);

                //Res = new Type[n0, ..., n returnDim - 1];
                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = returnDim;

                ExpressionList sizes = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    sizes.Add(Identifier.For("left_n" + i.ToString()));
                }

                method.Body.Statements.Add(GenerateLocalResArray(returnArrayType, sizes, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("left_n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvLeftIndices, "left_", bvRightIndices, "right_", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_target = new ExpressionList();
                ExpressionList index_source_left = new ExpressionList();
                ExpressionList index_source_right = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    index_target.Add(Identifier.For("i" + i.ToString()));
                }
                AddingComplexIndexing(leftIndices, index_source_left, strLeftIndices, 0, leftDim, "left_", sourceContext);
                AddingComplexIndexing(rightIndices, index_source_right, strRightIndices, 0, rightDim, "right_", sourceContext);
                
                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = opType;
                    internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source_right, rightType);
                    asgnFor.Source = internalOp;
                }
                else //MethodCall
                {
                    asgnFor.Source = new MethodCall(
                        //CreateQualIdentifier((Identifier)ovlOp),
                        ovlOp,
                        new ExpressionList(new Expression[] { 
                            new Indexer(Identifier.For("left"), index_source_left, leftType),
                            new Indexer(Identifier.For("right"), index_source_right, rightType) }),
                        NodeType.Call, returnType);
                }

                //type conversion (int16 + int16 = int32 in .net, but int16 in Zonnon)
                //it doesn't work implicitly on Mono, but does on Windows
                if (((returnType == SystemTypes.Int8) || (returnType == SystemTypes.Int16)
                        || (returnType == SystemTypes.UInt8 || (returnType == SystemTypes.UInt16))) &&
                    (opType != NodeType.MethodCall))
                {
                    //We have to convert the result
                    BinaryExpression castop = new BinaryExpression(
                        internalOp,
                        new MemberBinding(null, returnType),
                        NodeType.Castclass,
                        sourceContext);

                    asgnFor.Source = castop;
                }

                if (opType == NodeType.Div || opType == NodeType.Div_Un)
                {
                    if ((returnType == SystemTypes.Single) || (returnType == SystemTypes.Double)
                        || (returnType == SystemTypes.Decimal))
                    {
                        //We have to convert the second operand for division to real
                        BinaryExpression castop = new BinaryExpression(
                            new Indexer(Identifier.For("right"), index_source_right, rightType),
                            new MemberBinding(null, SystemTypes.Double as TypeNode),
                            NodeType.Castclass,
                            sourceContext);
                        
                        internalOp.Operand2 = castop;

                        // Here we have to wrap the division expression with a UnaryExpression
                        // with NodeType.Ckfinite. Unfortunately Resolver would not deal 
                        // correctly with this, so you either have to wait for the updated drop,
                        // or you have to override Resolver.InferTypeOfUnaryExpression.
                        UnaryExpression checkInfinite = new UnaryExpression();
                        checkInfinite.NodeType = NodeType.Ckfinite;
                        checkInfinite.Operand = internalOp;
                        checkInfinite.Type = internalOp.Type;

                        asgnFor.Source = checkInfinite;
                    }
                }
                
                //asgnFor.Source = internalOp;
                asgnFor.Target = new Indexer(Identifier.For("res"), index_target, returnType);
                ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);

                if ((bvLeftIndicesLength > 0) && 
                    (bvLeftIndices[bvLeftIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("left_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                if ((bvRightIndicesLength > 0) &&
                    (bvRightIndices[bvRightIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("right_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseArrayOp
        /// <summary>
        /// Returns appropriate function for an unary array standard operation (+, -, ~);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Array rank (dimensions length)</param>
        /// <param name="leftType">Array base type</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Neg)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseArrayOp(int leftDim, TypeNode leftType,
            EXPRESSION_LIST indices,
            TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(leftDim > 0);
            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;

            string cur_name = "ElementWise", cur_nameTemp = "ElementWise";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            cur_name += "Array";
            cur_nameTemp += "Array";
            int index_info = 11; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            if (opType == NodeType.Neg )
            {
                name = Identifier.For(cur_nameTemp + "Negative" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if (opType == NodeType.UnaryPlus)
            {
                name = Identifier.For(cur_nameTemp + "UnPlus" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if (opType == NodeType.LogicalNot)
            {
                name = Identifier.For(cur_nameTemp + "Inversion" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if ((opType == 0) && (ovlOp is TernaryExpression)) //ABS
            {
                name = Identifier.For(cur_nameTemp + "ABS" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_nameTemp + "_" + ovlOp.ToString() + "_" + leftDim.ToString() + "d" + leftType.Name);
            }
            
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type1[Ndim] ElementWiseNegativeNType(Type[Ndim] A)
                //{
                //    Type1[Ndim] Res;
                //    Res = new Type1[A.GetLength(0), ..., A.GetLength(n - 1)];
                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //            ...
                //                for(int in = 0; in < A.GetLength(n - 1); in++)
                //                   Res[i1, i2, ..., in] = -A[i1, i2, ..., in];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                if (indices != null)
                {
                    AddingParameters(method, cur_name, index_info, indices, "", "left", sourceContext);
                }

                int returnDim = leftDim;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "", returnDim, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, cur_name, index_info, indices, "", "left", returnDim, bvIndices, sourceContext);

                //Res = new Type[n0, ..., n returnDim - 1];
                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = returnDim;

                ExpressionList sizes = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    sizes.Add(Identifier.For("n" + i.ToString()));
                }

                method.Body.Statements.Add(GenerateLocalResArray(returnArrayType, sizes, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvIndices, "", null, "", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_target = new ExpressionList();
                ExpressionList index_source = new ExpressionList();

                for (int i = 0; i < returnDim; i++)
                {
                    index_target.Add(Identifier.For("i" + i.ToString()));
                }
                AddingComplexIndexing(indices, index_source, cur_name, index_info, returnDim, "", sourceContext);
                                
                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;

                if (opType != 0)
                {
                    if (opType != NodeType.MethodCall)
                    {
                        UnaryExpression internalOp = new UnaryExpression();
                        internalOp.NodeType = opType;
                        internalOp.Operand = new Indexer(Identifier.For("left"), index_source, leftType);
                        asgnFor.Source = internalOp;

                        //type conversion (int16 + int16 = int32 in .net, but int16 in Zonnon)
                        //it doesn't work implicitly on Mono, but does on Windows
                        if ((returnType == SystemTypes.Int8) || (returnType == SystemTypes.Int16)
                                || (returnType == SystemTypes.UInt8 || (returnType == SystemTypes.UInt16)))
                        {
                            //We have to convert the result
                            BinaryExpression castop = new BinaryExpression(
                                internalOp,
                                new MemberBinding(null, returnType),
                                NodeType.Castclass,
                                sourceContext);

                            asgnFor.Source = castop;
                        }
                    }
                    else //MethodCall
                    {
                        asgnFor.Source = new MethodCall(
                        //CreateQualIdentifier((Identifier)ovlOp),
                        ovlOp,
                        new ExpressionList(new Expression[] { 
                            new Indexer(Identifier.For("left"), index_source, leftType) }),
                        NodeType.Call, returnType);
                    }
                }
                else if (ovlOp is TernaryExpression)
                {
                    VariableDeclaration local = new VariableDeclaration();
                    local.Initializer = new Indexer(Identifier.For("left"), index_source, leftType);
                    local.Name = Identifier.For("Absolute Value");
                    local.Type = leftType;
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(local);

                    BinaryExpression comp = new BinaryExpression();
                    comp.Operand1 = local.Name;
                    comp.Operand2 = new Literal(0, SystemTypes.Int32);
                    comp.NodeType = NodeType.Lt;
                    comp.SourceContext = sourceContext;

                    TernaryExpression cond = new TernaryExpression();
                    cond.Operand1 = comp;
                    cond.Operand2 = new UnaryExpression(local.Name, NodeType.Neg);
                    cond.Operand3 = local.Name;
                    cond.NodeType = NodeType.Conditional;
                    cond.SourceContext = sourceContext;

                    asgnFor.Source = cond;
                }

                asgnFor.Target = new Indexer(Identifier.For("res"), index_target, returnType);
                ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);

                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetPseudoScalarProduct
        /// <summary>
        /// Returns appropriate function for array-array pseudo-scalar product;
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (scalar type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="isRetTypeSystem">Whether the return type is a system type (like int or double)</param>
        /// <param name="opPlus">Type of plus operator</param>
        /// <param name="ovlPlus">If + was overloaded, the appropriate plus operator</param>
        /// <param name="opMult">Type of mult operator</param>
        /// <param name="ovlMult">If * was overloaded, the appropriate mult operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetPseudoScalarProduct(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            TypeNode returnType,
            bool isRetTypeSystem,
            NodeType opPlus, Expression ovlPlus, NodeType opMult, Expression ovlMult, SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            //Debug.Assert(leftDim == rightDim);

            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            name = Identifier.For("PseudoScalar" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "Array" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type PseudoScalarNType1Type2(Type1[Ndim] A, Type2[Ndim] B)
                //{
                //    if ((A.GetLength(0) != B.GetLength(0)) || ... || A.GetLength(n - 1) != B.GetLength(n - 1)))
                //        throw new LengthsNotEqualException();
                //    Type Res = 0;
                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //            ...
                //                for(int in = 0; in < A.GetLength(n - 1); in++)
                //                    Res += A[i1, i2, ..., in] * B[i1, i2, ..., in];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;
                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                int returnDim = (leftDim < rightDim) ? leftDim : rightDim;

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "left_", returnDim, "right_", returnDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, strLeftIndices, 0, leftIndices, "left_", "left", returnDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, rightArrayType, strRightIndices, 0, rightIndices, "right_", "right", returnDim, bvRightIndices, sourceContext);
                AddingNiChecking(method.Body, strRightIndices, 0, rightIndices, "left_", "right_", returnDim, bvRightIndices, sourceContext);

                if (isRetTypeSystem)
                {
                    //Res = new Type;
                    method.Body.Statements.Add(GenerateLocalResVariable(returnType, sourceContext));
                }
                else //we need to generate a flag variable and res = null
                {
                    LocalDeclarationsStatement lds = new LocalDeclarationsStatement();
                    lds.Constant = false;
                    lds.Declarations = new LocalDeclarationList(1);
                    lds.InitOnly = false;
                    lds.Type = returnType;
                    LocalDeclaration ld = new LocalDeclaration();
                    ld.Name = Identifier.For("res");
                    ld.InitialValue = new Literal(null);
                    lds.Declarations.Add(ld);
                    method.Body.Statements.Add(lds);

                    LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                    lds1.Constant = false;
                    lds1.Declarations = new LocalDeclarationList(1);
                    lds1.InitOnly = false;
                    lds1.Type = SystemTypes.Boolean;
                    LocalDeclaration ld1 = new LocalDeclaration();
                    ld1.Name = Identifier.For("flag");
                    ld1.InitialValue = new Literal(false, SystemTypes.Boolean, sourceContext);
                    lds1.Declarations.Add(ld1);
                    method.Body.Statements.Add(lds1);
                }

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("left_n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvLeftIndices, "left_", bvRightIndices, "right_", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source_left = new ExpressionList();
                AddingComplexIndexing(leftIndices, index_source_left, strLeftIndices, 0, leftDim, "left_", sourceContext);

                ExpressionList index_source_right = new ExpressionList();
                AddingComplexIndexing(rightIndices, index_source_right, strRightIndices, 0, rightDim, "right_", sourceContext);

                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;
                asgnFor.Target = Identifier.For("res");

                if (opPlus != NodeType.MethodCall)
                {
                    BinaryExpression internalPlusOp = new BinaryExpression();
                    internalPlusOp.SourceContext = sourceContext;
                    internalPlusOp.NodeType = opPlus;
                    internalPlusOp.Operand1 = Identifier.For("res");
                    if (rightDim != 0)
                    {
                        if (opMult != NodeType.MethodCall)
                        {
                            BinaryExpression internalOp = new BinaryExpression();
                            internalOp.SourceContext = sourceContext;
                            internalOp.NodeType = opMult;
                            internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType, sourceContext);
                            internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source_right, rightType, sourceContext);
                            internalPlusOp.Operand2 = internalOp;
                        }
                        else //MethodCall
                        {
                            internalPlusOp.Operand2 = new MethodCall(
                                //CreateQualIdentifier((Identifier)ovlMult),
                                ovlMult,
                                new ExpressionList(new Expression[] { 
                                new Indexer(Identifier.For("left"), index_source_left, leftType),
                                new Indexer(Identifier.For("right"), index_source_right, rightType) }),
                                NodeType.Call);
                        }
                    }
                    else
                    {
                        internalPlusOp.Operand2 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    }

                    asgnFor.Source = internalPlusOp;
                }
                else //opPlus == MethodCall
                {
                    ExpressionList exprl = new ExpressionList();
                    exprl.Add(Identifier.For("res"));
                    if (rightDim != 0)
                    {
                        if (opMult != NodeType.MethodCall)
                        {
                            BinaryExpression internalOp = new BinaryExpression();
                            internalOp.SourceContext = sourceContext;
                            internalOp.NodeType = opMult;
                            internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                            internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source_right, rightType);
                            exprl.Add(internalOp);
                        }
                        else //MethodCall
                        {
                            exprl.Add(new MethodCall(
                                //CreateQualIdentifier((Identifier)ovlMult),
                                ovlMult,
                                new ExpressionList(new Expression[] { 
                                new Indexer(Identifier.For("left"), index_source_left, leftType),
                                new Indexer(Identifier.For("right"), index_source_right, rightType) }),
                                NodeType.Call, returnType));
                        }
                    }
                    else
                    {
                        exprl.Add(new Indexer(Identifier.For("left"), index_source_left, leftType));
                    }

                    asgnFor.Source = new MethodCall(
                        //CreateQualIdentifier((Identifier)ovlPlus),
                        ovlPlus,
                        exprl, NodeType.Call, returnType);
                }

                //check whether res == null (it will happen if res.type != a standard type,
                //and it will happen if operator for "*" was overloaded)
                if (!isRetTypeSystem)
                {
                    AssignmentStatement ast = new AssignmentStatement();
                    ast.Target = Identifier.For("res");
                    if (opMult == NodeType.MethodCall)
                    {
                        ast.Source = new MethodCall(
                            ovlMult,
                            new ExpressionList(new Expression[] { 
                        new Indexer(Identifier.For("left"), index_source_left, leftType),
                        new Indexer(Identifier.For("right"), index_source_right, rightType) }),
                            NodeType.Call);
                    }
                    else
                    {
                        ast.Source = new BinaryExpression(
                            new Indexer(Identifier.For("left"), index_source_left, leftType),
                            new Indexer(Identifier.For("right"), index_source_right, rightType),
                            opMult,
                            sourceContext);
                    }
                    ast.Operator = NodeType.Nop;

                    Block trueBlock = new Block(new StatementList(new Statement[] { asgnFor }), sourceContext);
                    Block falseBlock = new Block(new StatementList(new Statement[] {  
                            ast, 
                            new AssignmentStatement(Identifier.For("flag"), new Literal(true, SystemTypes.Boolean, sourceContext), NodeType.Nop, sourceContext)
                            }), 
                        sourceContext);
                    If ifstm = new If(
                        Identifier.For("flag"),
                        //new BinaryExpression(Identifier.For("res"), new Literal(null), NodeType.Ne, sourceContext),
                        trueBlock,
                        falseBlock);

                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(ifstm);
                    //method.Body.Statements.Add(ifstm);
                }
                else //isRetTypeSystem => we deal with standard types
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);
                }

                if ((bvLeftIndicesLength > 0) &&
                    (bvLeftIndices[bvLeftIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("left_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                if ((bvRightIndicesLength > 0) &&
                    (bvRightIndices[bvRightIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("right_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetArrayFunction
        /// <summary>
        /// Returns appropriate function for array funtions sum, min, max, all, any without specifying a dimension;
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="leftType">>Left type (scalar type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opID">Operation id like "Sum" or "Any"</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetArrayFunction(int leftDim, TypeNode leftType, 
            EXPRESSION_LIST leftIndices, 
            int realDim,
            TypeNode returnType,
            NodeType opNode, Expression ovlOp, string opID, object initValue, SourceContext sourceContext)
        {
            //Debug.Assert(rightDim > 0);
            //Debug.Assert(leftDim == rightDim);

            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strLeftIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            name = Identifier.For("Scalar" + opID + strLeftIndicesTemp + leftDim.ToString() + "d" + leftType.Name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;
                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "", "left", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(realDim, "", realDim, "right_", realDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, strLeftIndices, 0, leftIndices, "", "left", realDim, bvLeftIndices, sourceContext);
                
                //Res = new Type;
                method.Body.Statements.Add(GenerateLocalResVariable(returnType, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(realDim);
                for (int i = 0; i < realDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvLeftIndices, "", null, "", sourceContext);

                //generating index [i0, ... i realDim - 1]
                ExpressionList index_source_left = new ExpressionList();
                AddingComplexIndexing(leftIndices, index_source_left, strLeftIndices, 0, leftDim, "", sourceContext);

                if (opID == "Sum")
                {
                    AssignmentStatement asgnFor = new AssignmentStatement();
                    asgnFor.Operator = opNode;
                    // TODO: case when operator is overloaded
                    asgnFor.Source = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    asgnFor.Target = Identifier.For("res");
                    ((For)forStatements[realDim - 1]).Body.Statements.Add(asgnFor);
                }

                if ((opID == "Max") || (opID == "Min"))
                {
                    AssignmentStatement asgn_init = new AssignmentStatement();
                    asgn_init.Operator = NodeType.Nop;
                    asgn_init.Target = Identifier.For("res");
                    asgn_init.Source = new Literal(initValue, leftType);                    
                    
                    method.Body.Statements.Add(asgn_init);

                    If ifstm = new If();
                    BinaryExpression ifCond = new BinaryExpression();
                    ifCond.SourceContext = sourceContext;
                    ifCond.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    ifCond.Operand2 = Identifier.For("res");
                    ifCond.Type = SystemTypes.Boolean;
                    if (opID == "Min") 
                        ifCond.NodeType = NodeType.Lt;
                    else 
                        ifCond.NodeType = NodeType.Gt;
                    ifCond.SourceContext = sourceContext;
                    ifstm.Condition = ifCond;

                    AssignmentStatement asgn = new AssignmentStatement();
                    asgn.Operator = NodeType.Nop;
                    asgn.Source = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    asgn.Target = Identifier.For("res");

                    ifstm.TrueBlock = new Block(new StatementList(new Statement[] { asgn }));

                    ((For)forStatements[realDim - 1]).Body.Statements.Add(ifstm);
                }

                if ((opID == "All") || (opID == "Any"))
                {
                    If ifstm = new If();
                    BinaryExpression ifCond = new BinaryExpression();
                    ifCond.SourceContext = sourceContext;
                    ifCond.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    if (opID == "All") 
                        ifCond.Operand2 = new Literal(false, SystemTypes.Boolean, sourceContext);
                    else
                        ifCond.Operand2 = new Literal(true, SystemTypes.Boolean, sourceContext);
                    ifCond.Type = SystemTypes.Boolean;
                    ifCond.NodeType = NodeType.Eq;
                    ifCond.SourceContext = sourceContext;
                    ifstm.Condition = ifCond;

                    AssignmentStatement asgn = new AssignmentStatement();
                    asgn.Operator = NodeType.Nop;
                    asgn.Source = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    asgn.Target = Identifier.For("res");
                    Return retStm = new Return();
                    if (opID == "All")
                        retStm.Expression = new Literal(false, SystemTypes.Boolean, sourceContext);
                    else
                        retStm.Expression = new Literal(true, SystemTypes.Boolean, sourceContext);

                    ifstm.TrueBlock = new Block(new StatementList(new Statement[] { retStm }));

                    ((For)forStatements[realDim - 1]).Body.Statements.Add(ifstm);
                }

                if ((bvLeftIndicesLength > 0) &&
                    (bvLeftIndices[bvLeftIndicesLength - 1] == realDim - 1))
                {
                    ((For)forStatements[realDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (realDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }
                                
                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnType;
                Return ret = new Return();

                if (opID == "All")
                    ret.Expression = new Literal(true, SystemTypes.Boolean, sourceContext);
                else if (opID == "Any")
                    ret.Expression = new Literal(false, SystemTypes.Boolean, sourceContext);
                else
                {
                    ret.Expression = Identifier.For("res");
                }
                
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetMatrixTranspose
        /// <summary>
        /// Returns appropriate function for a matrix transpose operation;
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftType">>The array type</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetMatrixTranspose(TypeNode leftType, EXPRESSION_LIST indices, SourceContext sourceContext)
        {
            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;

            string cur_name = "MatrixTranspose", cur_nameTemp = "MatrixTranspose";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            int index_info = 15; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            name = Identifier.For(cur_nameTemp + leftType.Name);
            
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type[,] MatrixTranspositionType(Type[,] A)
                //{
                //    Type[,] Res;
                //    Res = new Type[A.GetLength(0), A.GetLength(1)];
                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = i1 + 1; i2 < A.GetLength(1); i2++)
                //    Res[i1, i2] = A[i2, i1];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                if (indices != null)
                {
                    AddingParameters(method, cur_name, index_info, indices, "", "left", sourceContext);
                }
                
                int returnDim = 2;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(2, "", 2, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, cur_name, index_info, indices, "", "left", returnDim, bvIndices, sourceContext);

                //Res = new Type[n1, n0];
                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = leftType;
                returnArrayType.Rank = returnDim;

                method.Body.Statements.Add(GenerateLocalResArray(returnArrayType, new ExpressionList
                    (new Expression[] { Identifier.For("n1"), Identifier.For("n0") }), sourceContext));

                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = i1 + 1; i2 < A.GetLength(1); i2++)
                //              Res[i1, i2] = A[i2, i1];
                //StatementList forStatements = new StatementList(returnDim);
                //forStatements.Add(GenerateFor(0, Identifier.For("n0"), null));
                //forStatements.Add(GenerateFor(1, 
                //    new BinaryExpression(Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext), 
                //    Identifier.For("n1"), null));

                //((For)forStatements[0]).Body.Statements.Add(forStatements[1]);
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvIndices, "", null, "", sourceContext);

                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;
                //asgnFor.Source = new Indexer(Identifier.For("left"),
                //    new ExpressionList(new Expression[] { Identifier.For("i0"), Identifier.For("i1")}),
                //    sourceContext);
                ExpressionList index_source = new ExpressionList();
                AddingComplexIndexing(indices, index_source, cur_name, index_info, returnDim, "", sourceContext);
                asgnFor.Source = new Indexer(Identifier.For("left"), index_source, leftType);

                asgnFor.Target = new Indexer(Identifier.For("res"), 
                    new ExpressionList(new Expression[] { Identifier.For("i1"), Identifier.For("i0")}), 
                    sourceContext);

                ((For)forStatements[1]).Body.Statements.Add(asgnFor);
                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetLUDecomposition
        public Member GetLUDecomposition(TypeNode leftType, SourceContext sourceContext)
        {
            Identifier name = Identifier.For("LUDecomposition" + leftType.Name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                //LU decomposition by columns
                //matrix should be: 1.square 2.all the elements on the diagonal != 0

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();
                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                int returnDim = 2;
                
                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(3, "", 2, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);
                AddingNi(method.Body, leftArrayType, "", 0, null, "", "left", returnDim, null, sourceContext);

                //we don't need this code anymore because this check is done in GetMain<Left^Right>LUDivisionOp
                ////if (n0 != n1) throw new IncompatibleSizesException(...);
                //method.Body.Statements.Add(
                //    GenerateIfStatementWithThrow(Identifier.For("n0"),
                //        Identifier.For("n1"),
                //        NodeType.Ne,
                //        STANDARD.IncompatibleSizesException,
                //        sourceContext.StartLine,
                //        sourceContext.StartColumn,
                //        sourceContext));

                //for (i0 = 0; i0 < n0 - 1; i0++) //for0
                //{
                //    if (left[i0, i0] == 0)
                //        for (i1 = i0 + 1; i1 < n0; i1 += 1) //for00
                //            left[i1, i0] = 0;
                //    else
                //        for (i1 = i0 + 1; i1 < n0; i1 += 1) //for01
                //            left[i1, i0] = left[i1, i0] / left[i0, i0]; //asgn1
                //
                //    for (i1 = i0 + 1; i1 < n0; i1++) //for1
                //        for (i2 = i0 + 1; i2 < n0; i2++) //for2
                //            left[i1, i2] = left[i1, i2] - left[i1, i0] * left[i0, i2]; //asgn2
                //}

                For for0 = GenerateFor(0, 
                    new BinaryExpression(
                        Identifier.For("n0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext), 
                    null,
                    sourceContext);

                For for00 = GenerateFor(1,
                    new BinaryExpression(
                        Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        Identifier.For("n0"), null, sourceContext);

                For for01 = GenerateFor(1,
                    new BinaryExpression(
                        Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        Identifier.For("n0"), null, sourceContext);

                AssignmentStatement asgn1 = new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                        leftType, sourceContext),
                    new BinaryExpression(
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        NodeType.Div,
                        sourceContext),
                    NodeType.Nop,
                    sourceContext);

                Literal l1;
                if (leftType == SystemTypes.Double)
                    l1 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l1 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l1 = new Literal((Int64)0, leftType);
                else l1 = new Literal(0, leftType);

                for00.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("left"),
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                        leftType, sourceContext), 
                    l1, NodeType.Nop, sourceContext));
                for01.Body.Statements.Add(asgn1);

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("my_temp"),
                        new Construct(new MemberBinding(null, leftType), new ExpressionList(), leftType), NodeType.Nop),
                        leftType));

                Literal l2;
                if (leftType == SystemTypes.Double)
                    l2 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l2 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l2 = new Literal((Int64)0, leftType);
                else l2 = new Literal(0, leftType);

                for0.Body.Statements.Add(new If(
                        new BinaryExpression(
                            new Indexer(Identifier.For("left"),
                                new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }),
                                leftType, sourceContext),
                            l2,
                            NodeType.Eq, sourceContext),
                    new Block(new StatementList(new Statement[1] { for00 }), sourceContext),
                    new Block(new StatementList(new Statement[1] { for01 }), sourceContext)));

                For for1 = GenerateFor(1,
                    new BinaryExpression(
                        Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        Identifier.For("n0"), null, sourceContext);

                For for2 = GenerateFor(2,
                    new BinaryExpression(
                        Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        Identifier.For("n0"), null, sourceContext);

                //left[i1, i2] = left[i1, i2] - left[i1, i0] * left[i0, i2]; //asgn2
                AssignmentStatement asgn2 = new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    new BinaryExpression(
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                            leftType, sourceContext),
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("left"),
                                new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                                leftType, sourceContext),
                            new Indexer(
                                Identifier.For("left"),
                                new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                                leftType, sourceContext),
                            NodeType.Mul,
                            sourceContext),
                        NodeType.Sub,
                        sourceContext),
                    NodeType.Nop,
                    sourceContext);
                
                for2.Body.Statements.Add(asgn2);
                for1.Body.Statements.Add(for2);
                for0.Body.Statements.Add(for1);
                method.Body.Statements.Add(for0);

                method.ReturnType = leftArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("left");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetLeftLUDivision
        public Member GetLeftLUDivision(TypeNode leftType, TypeNode rightType, TypeNode resType, int resDim, SourceContext sourceContext)
        {
            Identifier name = Identifier.For("LeftLUDivision" + resDim.ToString() + "d" + leftType.Name + rightType.Name + resType.Name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                //LU division: A*x = b ~ (L*U)*x = b ~ L*(U*x) = b;

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();
                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;
                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = resDim;
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                ArrayTypeExpression resArrayType = new ArrayTypeExpression();
                resArrayType.ElementType = resType;
                resArrayType.Rank = 1;

                ArrayTypeExpression xArrayType = new ArrayTypeExpression();
                xArrayType.ElementType = resType;
                xArrayType.Rank = resDim;
                method.Parameters.Add(new Parameter(Identifier.For("x"), xArrayType));

                int returnDim = resDim;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(3, "left_", 2, "right_", resDim, sourceContext);
                method.Body.Statements.Add(forVariables);
                AddingNi(method.Body, leftArrayType, "", 0, null, "left_", "left", 2, null, sourceContext);
                AddingNi(method.Body, rightArrayType, "", 0, null, "right_", "right", resDim, null, sourceContext);

                //we don't need this code anymore because this check is done in GetMain<Left^Right>LUDivisionOp
                ////if (left_n0 != left_n1) throw new IncompatibleSizesException(...);
                //method.Body.Statements.Add(
                //    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //        Identifier.For("left_n1"),
                //        NodeType.Ne,
                //        STANDARD.IncompatibleSizesException,
                //        sourceContext.StartLine,
                //        sourceContext.StartColumn,
                //        sourceContext));
                ////if (left_n0 != right_n0) throw new IncompatibleSizesException(...);
                //method.Body.Statements.Add(
                //    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //        Identifier.For("right_n0"),
                //        NodeType.Ne,
                //        STANDARD.IncompatibleSizesException,
                //        sourceContext.StartLine,
                //        sourceContext.StartColumn,
                //        sourceContext));

                method.Body.Statements.Add(GenerateLocalResArray(resArrayType,
                        new ExpressionList(new Expression[1] { Identifier.For("right_n0") }),
                        sourceContext));

                //L*res = right :
                //for (i0 = 0; i0 < left_n0; i0++) //for0
                //{
                //    ResType temp = 0;
                //    for (i1 = 0; i1 < i0; i1++) //for1
                //        temp += res[i1] * left[i0, i1]; //asgn1
                //    res[i0] = right[i0] - temp; //asgn2
                //}
                For forMain = new For();
                if (resDim == 2)
                {
                    forMain = GenerateFor(2, Identifier.For("right_n1"), null, sourceContext);
                    method.Body.Statements.Add(forMain);
                }

                For for0 = GenerateFor(0, Identifier.For("left_n0"), null, sourceContext);
                For for1 = GenerateFor(1, Identifier.For("i0"), null, sourceContext);

                for0.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, resType), new ExpressionList(), resType), NodeType.Nop),
                        resType));
                
                AssignmentStatement asgn1 = new AssignmentStatement(
                    Identifier.For("temp"),
                    new BinaryExpression(
                        new Indexer(
                            Identifier.For("res"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i1") }),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext),
                    NodeType.Add,
                    sourceContext);

                for1.Body.Statements.Add(asgn1);
                for0.Body.Statements.Add(for1);

                AssignmentStatement asgn2 = new AssignmentStatement();
                asgn2.Target = new Indexer( Identifier.For("res"), 
                    new ExpressionList(new Expression[1] { Identifier.For("i0") }), resType, sourceContext);
                if (resDim == 1)
                {
                    asgn2.Source = new BinaryExpression(
                        new Indexer(Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }),
                            rightType, sourceContext),
                        Identifier.For("temp"),
                        NodeType.Sub,
                        sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn2.Source = new BinaryExpression(
                        new Indexer(Identifier.For("right"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                            rightType, sourceContext),
                        Identifier.For("temp"),
                        NodeType.Sub,
                        sourceContext);
                }
                asgn2.Operator = NodeType.Nop;
                asgn2.SourceContext = sourceContext;

                for0.Body.Statements.Add(asgn2);

                if (resDim == 1)
                    method.Body.Statements.Add(for0);
                else if (resDim == 2)
                    forMain.Body.Statements.Add(for0);

                //U*x = res :
                //for (i0 = 0; i0 < left_n0 ; i0++) //for2
                //{
                //    ResType temp = 0;
                //    int temp_n0 = left_n0 - 1;
                //    int temp_n1 = temp_n0 - i0;
                //    
                //    for (i1 = 0; i1 < i0; i1++) //for3
                //          temp += x[temp_n0 - i1] * left[temp_n1, temp_n0 - i1]; //asgn3
                //
                //    if (left[temp_n1, temp_n1] == 0)
                //    {    //trueBlock
                //       x[temp_n1] = 0;
                //       if ((res[temp_n1] - temp) != 0)
                //           throw new Exception();
                //    }
                //    else
                //    {
                //       x[temp_n1] = (res[temp_n1] - temp) / left[temp_n1, temp_n1]; //asgn4
                //    }
                //}

                For for2 = GenerateFor(0, Identifier.For("left_n0"), null, sourceContext);
                For for3 = GenerateFor(1, Identifier.For("i0"), null, sourceContext);

                for2.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, resType), new ExpressionList(), resType), NodeType.Nop),
                        resType));

                for2.Body.Statements.Add(GenerateLocalIntVariables(0, "temp_", 2, "", 0, sourceContext));
                for2.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_n0"),
                    new BinaryExpression(Identifier.For("left_n0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop,
                    sourceContext));
                for2.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_n1"),
                    new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i0"), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop,
                    sourceContext));

                AssignmentStatement asgn3 = new AssignmentStatement();
                asgn3.Target = Identifier.For("temp");
                if (resDim == 1)
                {
                    asgn3.Source = new BinaryExpression(
                        new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[1] { 
                            new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext) }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), 
                            new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext)}),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn3.Source = new BinaryExpression(
                        new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[2] { 
                                new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext),
                                Identifier.For("i2") }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), 
                            new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext)}),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext);
                }
                asgn3.Operator = NodeType.Add;
                asgn3.SourceContext = sourceContext;

                for3.Body.Statements.Add(asgn3);
                for2.Body.Statements.Add(for3);

                Block trueBlock = new Block(new StatementList());

                Literal l1;
                if (resType == SystemTypes.Double)
                    l1 = new Literal((Double)0, resType);
                else if (resType == SystemTypes.Single)
                    l1 = new Literal((Single)0, resType);
                else if (resType == SystemTypes.Int64)
                    l1 = new Literal((Int64)0, resType);
                else l1 = new Literal(0, resType);

                if (resDim == 1)
                {
                    trueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("x"),
                                new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                                resType, sourceContext),
                            l1, NodeType.Nop, sourceContext));
                }
                else if (resDim == 2)
                {
                    trueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("x"),
                                new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), Identifier.For("i2") }),
                                resType, sourceContext),
                            l1, NodeType.Nop, sourceContext));
                }

                Literal l2;
                if (resType == SystemTypes.Double)
                    l2 = new Literal((Double)0, resType);
                else if (resType == SystemTypes.Single)
                    l2 = new Literal((Single)0, resType);
                else if (resType == SystemTypes.Int64)
                    l2 = new Literal((Int64)0, resType);
                else l2 = new Literal(0, resType);

                trueBlock.Statements.Add(GenerateIfStatementWithThrow(
                    new BinaryExpression(
                        new Indexer(
                            Identifier.For("res"),
                            new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                            resType, sourceContext),
                        Identifier.For("temp"),
                        NodeType.Sub,
                        sourceContext),
                    l2,
                    NodeType.Ne,
                    STANDARD.NoSLUSolutionException,
                    sourceContext.StartLine,
                    sourceContext.StartColumn,
                    sourceContext));

                AssignmentStatement asgn4 = new AssignmentStatement();
                if (resDim == 1)
                {
                    asgn4.Target = new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                            resType, sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn4.Target = new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), Identifier.For("i2") }),
                            resType, sourceContext);
                }
                asgn4.Source = new BinaryExpression(
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("res"),
                                new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), Identifier.For("temp_n1") }),
                            leftType, sourceContext),
                        NodeType.Div,
                        sourceContext);
                asgn4.Operator = NodeType.Nop;
                asgn4.SourceContext = sourceContext;

                Literal l3;
                if (leftType == SystemTypes.Double)
                    l3 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l3 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l3 = new Literal((Int64)0, leftType);
                else l3 = new Literal(0, leftType);

                for2.Body.Statements.Add(new If(
                    new BinaryExpression(
                        new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("temp_n1"), Identifier.For("temp_n1") }),
                            leftType, sourceContext),
                        l3, NodeType.Eq, sourceContext),
                    trueBlock,
                    new Block(new StatementList(new Statement[1] { asgn4 }), sourceContext)));

                if (resDim == 1)
                    method.Body.Statements.Add(for2);
                else
                    forMain.Body.Statements.Add(for2);

                method.ReturnType = xArrayType;
                Return ret = new Return();
                ret.Expression = Identifier.For("x");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetRightLUDivision
        public Member GetRightLUDivision(TypeNode leftType, TypeNode rightType, TypeNode resType, int resDim, SourceContext sourceContext)
        {
            Identifier name = Identifier.For("RightLUDivision" + resDim.ToString() + "d" + leftType.Name + rightType.Name + resType.Name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                //LU division: x*A = b ~ x*(L*U) = b ~ (x*L)*U = b;

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();
                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;
                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = resDim;
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                ArrayTypeExpression resArrayType = new ArrayTypeExpression();
                resArrayType.ElementType = resType;
                resArrayType.Rank = 1;

                ArrayTypeExpression xArrayType = new ArrayTypeExpression();
                xArrayType.ElementType = resType;
                xArrayType.Rank = resDim;
                method.Parameters.Add(new Parameter(Identifier.For("x"), xArrayType));

                int returnDim = resDim;

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(3, "left_", 2, "right_", resDim, sourceContext);
                method.Body.Statements.Add(forVariables);
                AddingNi(method.Body, leftArrayType, "", 0, null, "left_", "left", 2, null, sourceContext);
                AddingNi(method.Body, rightArrayType, "", 0, null, "right_", "right", resDim, null, sourceContext);

                //we don't need this code anymore because this check is done in GetMain<Left^Right>LUDivisionOp
                ////if (left_n0 != left_n1) throw new IncompatibleSizesException(...);
                //method.Body.Statements.Add(
                //    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //        Identifier.For("left_n1"),
                //        NodeType.Ne,
                //        STANDARD.IncompatibleSizesException,
                //        sourceContext.StartLine,
                //        sourceContext.StartColumn,
                //        sourceContext));
                //if (resDim == 1)
                //{
                //    //if (left_n0 != right_n0) throw new IncompatibleSizesException(...);
                //    method.Body.Statements.Add(
                //        GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //            Identifier.For("right_n0"),
                //            NodeType.Ne,
                //            STANDARD.IncompatibleSizesException,
                //            sourceContext.StartLine,
                //            sourceContext.StartColumn,
                //            sourceContext));
                //}
                //else if (resDim == 2)
                //{
                //    //if (left_n0 != right_n1) throw new IncompatibleSizesException(...);
                //    method.Body.Statements.Add(
                //        GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //            Identifier.For("right_n1"),
                //            NodeType.Ne,
                //            STANDARD.IncompatibleSizesException,
                //            sourceContext.StartLine,
                //            sourceContext.StartColumn,
                //            sourceContext));
                //}

                method.Body.Statements.Add(GenerateLocalResArray(resArrayType,
                            new ExpressionList(new Expression[1] { Identifier.For("left_n0") }), 
                            sourceContext));

                //res*U = right :
                //for (i0 = 0; i0 < left_n0; i0++) //for0
                //{
                //    ResType temp = 0;
                //    for (i1 = 0; i1 < i0; i1++) //for1
                //        temp += res[i1] * left[i0, i1]; //asgn1
                //    res[i0] = right[i0] - temp; //asgn2
                //}
                For forMain = new For();
                if (resDim == 2)
                {
                    forMain = GenerateFor(2, Identifier.For("right_n0"), null, sourceContext);
                    method.Body.Statements.Add(forMain);
                }

                For for0 = GenerateFor(0, Identifier.For("left_n0"), null, sourceContext);
                For for1 = GenerateFor(1, Identifier.For("i0"), null, sourceContext);

                for0.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, resType), new ExpressionList(), resType), NodeType.Nop),
                        resType));

                AssignmentStatement asgn1 = new AssignmentStatement(
                    Identifier.For("temp"),
                    new BinaryExpression(
                        new Indexer(
                            Identifier.For("res"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext),
                    NodeType.Add,
                    sourceContext);

                for1.Body.Statements.Add(asgn1);
                for0.Body.Statements.Add(for1);

                /////
                Block trueBlock = new Block(new StatementList());

                Literal l1;
                if (resType == SystemTypes.Double)
                    l1 = new Literal((Double)0, resType);
                else if (resType == SystemTypes.Single)
                    l1 = new Literal((Single)0, resType);
                else if (resType == SystemTypes.Int64)
                    l1 = new Literal((Int64)0, resType);
                else l1 = new Literal(0, resType);

                trueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("res"),
                                new ExpressionList(new Expression[1] { Identifier.For("i0") }),
                                resType, sourceContext),
                            l1, NodeType.Nop, sourceContext));

                Literal l2;
                if (resType == SystemTypes.Double)
                    l2 = new Literal((Double)0, resType);
                else if (resType == SystemTypes.Single)
                    l2 = new Literal((Single)0, resType);
                else if (resType == SystemTypes.Int64)
                    l2 = new Literal((Int64)0, resType);
                else l2 = new Literal(0, resType);

                if (resDim == 1)
                {
                    trueBlock.Statements.Add(GenerateIfStatementWithThrow(
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("right"),
                                new ExpressionList(new Expression[1] { Identifier.For("i0") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext),
                        l2,
                        NodeType.Ne,
                        STANDARD.NoSLUSolutionException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));
                }
                else if (resDim == 2)
                {
                    trueBlock.Statements.Add(GenerateIfStatementWithThrow(
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("right"),
                                new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext),
                        l2,
                        NodeType.Ne,
                        STANDARD.NoSLUSolutionException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));
                }

                AssignmentStatement asgn2 = new AssignmentStatement();
                asgn2.Target = new Indexer(
                            Identifier.For("res"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }),
                            resType, sourceContext);
                if (resDim == 1)
                {
                    asgn2.Source = new BinaryExpression(
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("right"),
                                new ExpressionList(new Expression[1] { Identifier.For("i0") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        NodeType.Div,
                        sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn2.Source = new BinaryExpression(
                        new BinaryExpression(
                            new Indexer(
                                Identifier.For("right"),
                                new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        NodeType.Div,
                        sourceContext);
                }

                asgn2.Operator = NodeType.Nop;
                asgn2.SourceContext = sourceContext;

                Literal l3;
                if (leftType == SystemTypes.Double)
                    l3 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l3 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l3 = new Literal((Int64)0, leftType);
                else l3 = new Literal(0, leftType);

                for0.Body.Statements.Add(new If(
                    new BinaryExpression(
                        new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }),
                            leftType, sourceContext),
                        l3, NodeType.Eq, sourceContext),
                    trueBlock,
                    new Block(new StatementList(new Statement[1] { asgn2 }), sourceContext)));
                ////

                if (resDim == 1)
                    method.Body.Statements.Add(for0);
                else if (resDim == 2)
                    forMain.Body.Statements.Add(for0);

                //x*L = res :
                //for (i0 = 0; i0 < left_n0 ; i0++) //for2
                //{
                //    ResType temp = 0;
                //    int temp_n0 = left_n0 - 1;
                //    int temp_n1 = temp_n0 - i0;
                //    
                //    for (i1 = 0; i1 < i0; i1++) //for3
                //          temp += x[temp_n0 - i1] * left[temp_n1, temp_n0 - i1]; //asgn3
                //
                //    if (left[temp_n1, temp_n1] == 0)
                //    {    //trueBlock
                //       x[temp_n1] = 0;
                //       if ((res[temp_n1] - temp) != 0)
                //           throw new Exception();
                //    }
                //    else
                //    {
                //       x[temp_n1] = (res[temp_n1] - temp) / left[temp_n1, temp_n1]; //asgn4
                //    }
                //}

                For for2 = GenerateFor(0, Identifier.For("left_n0"), null, sourceContext);
                For for3 = GenerateFor(1, Identifier.For("i0"), null, sourceContext);

                for2.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, resType), new ExpressionList(), resType), NodeType.Nop),
                        resType));

                for2.Body.Statements.Add(GenerateLocalIntVariables(0, "temp_", 2, "", 0, sourceContext));
                for2.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_n0"),
                    new BinaryExpression(Identifier.For("left_n0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop, sourceContext));
                for2.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_n1"),
                    new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i0"), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop, sourceContext));

                AssignmentStatement asgn3 = new AssignmentStatement();
                asgn3.Target = Identifier.For("temp");
                if (resDim == 1)
                {
                    asgn3.Source = new BinaryExpression(
                        new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[1] { 
                            new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext) }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { 
                                new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext),
                                Identifier.For("temp_n1")}),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn3.Source = new BinaryExpression(
                        new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[2] { 
                                Identifier.For("i2"),
                                new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext) }),
                            resType, sourceContext),
                        new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { 
                                new BinaryExpression(Identifier.For("temp_n0"), Identifier.For("i1"), NodeType.Sub, sourceContext), 
                                Identifier.For("temp_n1"), 
                            }),
                            leftType, sourceContext),
                        NodeType.Mul,
                        sourceContext);
                }
                asgn3.Operator = NodeType.Add;
                asgn3.SourceContext = sourceContext;

                for3.Body.Statements.Add(asgn3);
                for2.Body.Statements.Add(for3);

                AssignmentStatement asgn4 = new AssignmentStatement();
                if (resDim == 1)
                {
                    asgn4.Target = new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                            resType, sourceContext);
                }
                else if (resDim == 2)
                {
                    asgn4.Target = new Indexer(
                            Identifier.For("x"),
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("temp_n1") }),
                            resType, sourceContext);
                }
                asgn4.Source = new BinaryExpression(
                            new Indexer(
                                Identifier.For("res"),
                                new ExpressionList(new Expression[1] { Identifier.For("temp_n1") }),
                                resType, sourceContext),
                            Identifier.For("temp"),
                            NodeType.Sub,
                            sourceContext);
                asgn4.Operator = NodeType.Nop;
                asgn4.SourceContext = sourceContext;

                for2.Body.Statements.Add(asgn4);

                if (resDim == 1)
                    method.Body.Statements.Add(for2);
                else
                    forMain.Body.Statements.Add(for2);

                method.ReturnType = xArrayType;
                Return ret = new Return();
                ret.Expression = Identifier.For("x");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetChangeLinePosition
        /// <summary>
        /// Returns function which change the order of lines to make diagonal elements of a matrix non-zero
        /// </summary>
        /// <param name="leftType"></param>
        /// <param name="rightType"></param>
        /// <param name="sourceContext"></param>
        /// <returns></returns>
        public Member GetChangeLinePos(TypeNode leftType, TypeNode rightType, int rightDim, bool isRight, SourceContext sourceContext)
        {
            string s = "ChangeLinePosition" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            if (isRight) s += "Right";
            else s += "Left";
            Identifier name = Identifier.For(s);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                //We need to generate a specific function (this is the case when isRight == false)
                //public static void ChangeLinePositionleftTyperightType(leftType[,] left, rightType[] right, int startPos, int i0Prev, int i1Prev)
                //{
                //    Int32 i0 = new Int32(), i1 = new Int32(), i2 = new Int32(), i3 = new Int32(), i4 = new Int32(), n0 = new Int32(), n1 = new Int32();
                //    leftType temp_left = new leftType();
                //    rightType temp_right = new rightType();
                //
                //    n0 = left.GetLength(0);
                //    if (startPos == 0)
                //    { //trueBlock1
                //        n1 = left.GetLength(1);
                //        if (n0 != n1)
                //        {
                //            throw (new Exception());
                //        }
                //
                //        //set every line with only one non-zero element to its position
                //        for (i0 = 0; i0 < n0; i0++) //for0
                //        {
                //            i1 = 0;
                //            while ((i1 < n0) && (left[i0, i1] == 0))
                //                i1++;
                //            i2 = n0 - 1;
                //            while ((i2 > -1) && (left[i0, i2] == 0))
                //                i2--;
                //            if ((i1 == i2) && (i0 != i1)) //there is one non-zero element on i1-th position, and it's place is wrong
                //            { //trueBlock2
                //                i3 = 0;
                //                while ((i3 < n0) && (left[i1, i3] == 0))
                //                    i3++;
                //                i4 = n0 - 1;
                //                while ((i4 > -1) && (left[i1, i4] == 0))
                //                    i4--;
                //
                //                if ((i3 != i4) || (i3 != i1)) //line i1 has another structure
                //                {
                //                    //change lines i1 and i0
                //                    for (i2 = 0; i2 < n0; i2++) //forChange
                //                    {
                //                        temp_left = left[i0, i2];
                //                        left[i0, i2] = left[i1, i2];
                //                        left[i1, i2] = temp_left;
                //                        temp_right = right[i0];
                //                        right[i0] = right[i1];
                //                        right[i1] = temp_right;
                //                    }
                //                    i0--;
                //                }
                //            }
                //        }
                //    }
                //
                //    for (i0 = startPos; i0 < n0; i0++) //for1
                //    {
                //        if (left[i0, i0] == 0) 
                //        { //trueBlock3
                //            i1 = i0 + 1;
                //            while ((i1 < n0) && ((left[i1, i0] == 0) || (i0 == i1Prev) || (i1 == i0Prev)))
                //                i1++;
                //
                //            if (i1 < n0)
                //            { //trueblock4
                //                //change lines i1 and i0
                //                for (i2 = 0; i2 < n0; i2++)
                //                {
                //                    temp_left = left[i0, i2];
                //                    left[i0, i2] = left[i1, i2];
                //                    left[i1, i2] = temp_left;
                //                    temp_right = right[i0];
                //                    right[i0] = right[i1];
                //                    right[i1] = temp_right;
                //                }
                //                i0Prev = i0;
                //                i1Prev = i1;
                //            }
                //            else
                //            { //falseBlock1
                //                i1 = 0;
                //                while ((i1 < i0) && ((left[i1, i0] == 0) || (i0 == i1Prev) || (i1 == i0Prev)))
                //                    i1++;
                //                if (i1 < i0)
                //                { //trueBlock5
                //                    //change lines i1 and i0
                //                    for (i2 = 0; i2 < n0; i2++)
                //                    {
                //                        temp_left = left[i0, i2];
                //                        left[i0, i2] = left[i1, i2];
                //                        left[i1, i2] = temp_left;
                //                        temp_right = right[i0];
                //                        right[i0] = right[i1];
                //                        right[i1] = temp_right;
                //                    }
                //                    if (left[i1, i1] == 0) 
                //                        ChangePos(left, right, i1, i0, i1);
                //                }
                //            }
                //        }
                //    }
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();
                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;
                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                method.Parameters.Add(new Parameter(Identifier.For("startPos"), SystemTypes.Int32));
                method.Parameters.Add(new Parameter(Identifier.For("i0Prev"), SystemTypes.Int32));
                method.Parameters.Add(new Parameter(Identifier.For("i1Prev"), SystemTypes.Int32));

                mathRoutines.Members.Add(method);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(5, "left_", 2, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp_left"),
                        new Construct(new MemberBinding(null, leftType), new ExpressionList(), leftType), NodeType.Nop),
                        leftType));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp_right"),
                        new Construct(new MemberBinding(null, rightType), new ExpressionList(), rightType), NodeType.Nop),
                        rightType));

                AddingNi(method.Body, leftArrayType, "", 0, null, "left_", "left", 2, null, sourceContext);

                Block trueBlock1 = new Block(new StatementList());
                AddingNi(trueBlock1, rightArrayType, "", 0, null, "right_", "right", rightDim, null, sourceContext);

                //we don't need this code anymore because this check is done in GetMain<Left^Right>LUDivisionOp
                ////if (left_n0 != left_n1) throw new IncompatibleSizesException(...);
                //trueBlock1.Statements.Add(
                //    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //        Identifier.For("left_n1"),
                //        NodeType.Ne,
                //        STANDARD.IncompatibleSizesException,
                //        sourceContext.StartLine,
                //        sourceContext.StartColumn,
                //        sourceContext));
                ////if (left_n0 != right_n0) throw new IncompatibleSizesException(...);
                //if ((!isRight) || (rightDim == 1)) 
                //{
                //    trueBlock1.Statements.Add(
                //        GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //            Identifier.For("right_n0"),
                //            NodeType.Ne,
                //            STANDARD.IncompatibleSizesException,
                //            sourceContext.StartLine,
                //            sourceContext.StartColumn,
                //            sourceContext));
                //}
                //else if (rightDim == 2)
                //{
                //    trueBlock1.Statements.Add(
                //        GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                //            Identifier.For("right_n1"),
                //            NodeType.Ne,
                //            STANDARD.IncompatibleSizesException,
                //            sourceContext.StartLine,
                //            sourceContext.StartColumn,
                //            sourceContext));
                //}

                //set every line with only one non-zero element to its position
                For for0 = GenerateFor(0, Identifier.For("left_n0"), null, sourceContext);
                for0.Body.Statements.Add(new AssignmentStatement(Identifier.For("i1"), new Literal(0, SystemTypes.Int32), NodeType.Nop, sourceContext));

                Literal l1;
                if (leftType == SystemTypes.Double)
                    l1 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l1 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l1 = new Literal((Int64)0, leftType);
                else l1 = new Literal(0, leftType);

                //while ((i1 < n0) && (left[i0, i1] == 0)) i1++;
                for0.Body.Statements.Add(new While(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i1"), Identifier.For("left_n0"), NodeType.Lt, sourceContext),
                        new BinaryExpression(new Indexer(Identifier.For("left"),
                                (isRight) ? 
                                new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }) :
                                new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i1") }),
                                leftType, sourceContext),
                            l1, NodeType.Eq, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { 
                            //new ExpressionStatement(new UnaryExpression(Identifier.For("i1"), NodeType.Increment, sourceContext), sourceContext)}                        
                            new AssignmentStatement(Identifier.For("i1"), 
                            new BinaryExpression(Identifier.For("i1"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                            NodeType.Nop, sourceContext) }
            ))));

                for0.Body.Statements.Add(new AssignmentStatement(Identifier.For("i2"), 
                    new BinaryExpression(Identifier.For("left_n0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext), 
                    NodeType.Nop, sourceContext));

                Literal l2;
                if (leftType == SystemTypes.Double)
                    l2 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l2 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l2 = new Literal((Int64)0, leftType);
                else l2 = new Literal(0, leftType);

                //while ((i2 > -1) && (left[i0, i2] == 0)) i2--;
                for0.Body.Statements.Add(new While(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i2"), new Literal(-1, SystemTypes.Int32), NodeType.Gt, sourceContext),
                        new BinaryExpression(new Indexer(Identifier.For("left"),
                                (isRight) ? 
                                new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                                new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                                leftType, sourceContext),
                            l2, NodeType.Eq, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { new AssignmentStatement(Identifier.For("i2"),
                        new BinaryExpression(Identifier.For("i2"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                        NodeType.Nop, sourceContext) }), sourceContext)));

                Block trueBlock2 = new Block(new StatementList());
                //i3 = 0;
                trueBlock2.Statements.Add(new AssignmentStatement(Identifier.For("i3"), new Literal(0, SystemTypes.Int32), NodeType.Nop, sourceContext));

                Literal l3;
                if (leftType == SystemTypes.Double)
                    l3 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l3 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l3 = new Literal((Int64)0, leftType);
                else l3 = new Literal(0, leftType);

                //while ((i3 < n0) && (left[i1, i3] == 0)) i3++;
                trueBlock2.Statements.Add(new While(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i3"), Identifier.For("left_n0"), NodeType.Lt, sourceContext),
                        new BinaryExpression(new Indexer(Identifier.For("left"),
                                (isRight) ?
                                new ExpressionList(new Expression[2] { Identifier.For("i3"), Identifier.For("i1") }) :
                                new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i3") }),
                                leftType, sourceContext),
                            l3, NodeType.Eq, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { new AssignmentStatement(Identifier.For("i3"),
                        new BinaryExpression(Identifier.For("i3"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        NodeType.Nop, sourceContext) }), sourceContext)));

                //i4 = n0 - 1;
                trueBlock2.Statements.Add(new AssignmentStatement(Identifier.For("i4"),
                    new BinaryExpression(Identifier.For("left_n0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop, sourceContext));

                Literal l4;
                if (leftType == SystemTypes.Double)
                    l4 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l4 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l4 = new Literal((Int64)0, leftType);
                else l4 = new Literal(0, leftType);

                //while ((i4 > -1) && (left[i1, i4] == 0)) i4--;
                trueBlock2.Statements.Add(new While(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i4"), new Literal(-1, SystemTypes.Int32), NodeType.Gt, sourceContext),
                        new BinaryExpression(new Indexer(Identifier.For("left"),
                                (isRight) ?
                                new ExpressionList(new Expression[2] { Identifier.For("i4"), Identifier.For("i1") }) :
                                new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i4") }),
                                leftType, sourceContext),
                            l4, NodeType.Eq, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { new AssignmentStatement(Identifier.For("i4"),
                        new BinaryExpression(Identifier.For("i4"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                        NodeType.Nop, sourceContext) }), sourceContext)));

                Block trueBlockInt1 = new Block(new StatementList());

                //        temp_left = left[i0, i2];    
                //        left[i0, i2] = left[i1, i2]; 
                //        left[i1, i2] = temp_left;    
                For forChange = GenerateFor(2, Identifier.For("left_n0"), null, sourceContext);
                forChange.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_left"),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                        leftType, sourceContext), 
                    NodeType.Nop, sourceContext));
                forChange.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ? 
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), 
                        leftType, sourceContext),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext), 
                    NodeType.Nop, sourceContext));
                forChange.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    Identifier.For("temp_left"),
                    NodeType.Nop, sourceContext));
                trueBlockInt1.Statements.Add(forChange);

                if (rightDim == 2)
                {
                    For forChange_ = new For(); 
                    if (isRight) forChange_ = GenerateFor(2, Identifier.For("right_n0"), null, sourceContext);
                    else forChange_ = GenerateFor(2, Identifier.For("right_n1"), null, sourceContext);

                    forChange_.Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), 
                            rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), 
                            rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));
                    trueBlockInt1.Statements.Add(forChange_);
                }

                else if (rightDim == 1)
                {
                    trueBlockInt1.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlockInt1.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlockInt1.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));
                }
                trueBlockInt1.Statements.Add(new AssignmentStatement(Identifier.For("i0"),
                    new BinaryExpression(Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Sub_Ovf, sourceContext),
                    NodeType.Nop, sourceContext));

                //if ((i3 != i4) || (i3 != i1)) //line i1 has another structure
                //{
                //    forChange; 
                //    maybe assignment
                //    i0--;
                //}
                trueBlock2.Statements.Add(new If(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i3"), Identifier.For("i4"), NodeType.Ne, sourceContext),
                        new BinaryExpression(Identifier.For("i3"), Identifier.For("i1"), NodeType.Ne, sourceContext),
                        NodeType.LogicalOr, sourceContext),
                    trueBlockInt1,
                    new Block(new StatementList(), sourceContext)));

                //if ((i1 == i2) && (i0 != i1))
                for0.Body.Statements.Add(new If(new BinaryExpression(
                        new BinaryExpression(Identifier.For("i1"), Identifier.For("i2"), NodeType.Eq, sourceContext),
                        new BinaryExpression(Identifier.For("i0"), Identifier.For("i1"), NodeType.Ne, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    trueBlock2,
                    new Block(new StatementList(), sourceContext)));
                trueBlock1.Statements.Add(for0);

                method.Body.Statements.Add(
                    new If(new BinaryExpression(Identifier.For("startPos"), new Literal(0, SystemTypes.Int32), NodeType.Eq, sourceContext),
                        trueBlock1,
                        new Block(new StatementList(), sourceContext)));

                //the second part, the first was if (startPos == 0) {...}
                //for (i0 = startPos; i0 < n0; i0++)
                For for1 = GenerateFor(0, Identifier.For("startPos"), Identifier.For("left_n0"), null, sourceContext);

                Block trueBlock3 = new Block(new StatementList());
                //i1 = i0 + 1;
                trueBlock3.Statements.Add(new AssignmentStatement(
                    Identifier.For("i1"),
                    new BinaryExpression(Identifier.For("i0"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                    NodeType.Nop, sourceContext));

                Literal l5;
                if (leftType == SystemTypes.Double)
                    l5 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l5 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l5 = new Literal((Int64)0, leftType);
                else l5 = new Literal(0, leftType);

                //while ((i1 < n0) && ((left[i1, i0] == 0) || ((i0 == i1Prev) || (i1 == i0Prev)))) i1++;
                trueBlock3.Statements.Add(new While(
                    new BinaryExpression(
                        new BinaryExpression(Identifier.For("i1"), Identifier.For("left_n0"), NodeType.Lt, sourceContext),
                        new BinaryExpression(
                            new BinaryExpression(
                                new Indexer(Identifier.For("left"),
                                    (isRight) ?
                                    new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i1") }) :
                                    new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                                    leftType, sourceContext),
                                l5, NodeType.Eq, sourceContext),
                            new BinaryExpression(
                                new BinaryExpression(Identifier.For("i0"), Identifier.For("i1Prev"), NodeType.Eq, sourceContext),
                                new BinaryExpression(Identifier.For("i1"), Identifier.For("i0Prev"), NodeType.Eq, sourceContext),
                                NodeType.LogicalOr, sourceContext),
                            NodeType.LogicalOr, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { new AssignmentStatement(Identifier.For("i1"),
                        new BinaryExpression(Identifier.For("i1"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        NodeType.Nop, sourceContext) }), sourceContext)));

                //forChange; i0Prev = i0; i1Prev = i1;
                Block trueBlock4 = new Block(new StatementList());
                //trueBlock4.Statements.Add(forChange.Clone() as For);
                /*forChange*/
                For forChange2 = GenerateFor(2, Identifier.For("left_n0"), null, sourceContext);
                forChange2.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_left"),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    NodeType.Nop, sourceContext));
                forChange2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    NodeType.Nop, sourceContext));
                forChange2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    Identifier.For("temp_left"),
                    NodeType.Nop, sourceContext));
                trueBlock4.Statements.Add(forChange2);

                if (rightDim == 2)
                {
                    For forChange2_ = new For();
                    if (isRight) forChange2_ = GenerateFor(2, Identifier.For("right_n0"), null, sourceContext);
                    else forChange2_ = GenerateFor(2, Identifier.For("right_n1"), null, sourceContext);

                    forChange2_.Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), 
                            rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange2_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), 
                            rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }), 
                            rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange2_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));
                    trueBlock4.Statements.Add(forChange2_);
                }

                if (rightDim == 1)
                {
                    trueBlock4.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlock4.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlock4.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));
                }

                /*end forChange*/
                trueBlock4.Statements.Add(new AssignmentStatement(
                    Identifier.For("i0Prev"), Identifier.For("i0"), NodeType.Nop, sourceContext));
                trueBlock4.Statements.Add(new AssignmentStatement(
                    Identifier.For("i1Prev"), Identifier.For("i1"), NodeType.Nop, sourceContext));

                Block falseBlock1 = new Block(new StatementList());
                //i1 = 0;
                falseBlock1.Statements.Add(new AssignmentStatement(
                    Identifier.For("i1"), new Literal(0, SystemTypes.Int32), NodeType.Nop, sourceContext));

                Literal l6;
                if (leftType == SystemTypes.Double)
                    l6 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l6 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l6 = new Literal((Int64)0, leftType);
                else l6 = new Literal(0, leftType);

                //while ((i1 < i0) && ((left[i1, i0] == 0) || (i0 == i1Prev) || (i1 == i0Prev))) i1++;
                falseBlock1.Statements.Add(new While(
                    new BinaryExpression(
                        new BinaryExpression(Identifier.For("i1"), Identifier.For("i0"), NodeType.Lt, sourceContext),
                        new BinaryExpression(
                            new BinaryExpression(
                                new Indexer(Identifier.For("left"),
                                    (isRight) ?
                                    new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i1") }) :
                                    new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i0") }),
                                    leftType, sourceContext),
                                l6, NodeType.Eq, sourceContext),
                            new BinaryExpression(
                                new BinaryExpression(Identifier.For("i0"), Identifier.For("i1Prev"), NodeType.Eq, sourceContext),
                                new BinaryExpression(Identifier.For("i1"), Identifier.For("i0Prev"), NodeType.Eq, sourceContext),
                                NodeType.LogicalOr, sourceContext),
                            NodeType.LogicalOr, sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList( 
                        new Statement[] { new AssignmentStatement(Identifier.For("i1"),
                        new BinaryExpression(Identifier.For("i1"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf, sourceContext),
                        NodeType.Nop, sourceContext) }), sourceContext)));

                //forChange; if (left[i1, i1] == 0) ChangePos(left, right, i1, i0, i1);
                Block trueBlock5 = new Block(new StatementList());
                //trueBlock5.Statements.Add(forChange.Clone() as For);
                /*forChange*/
                For forChange1 = GenerateFor(2, Identifier.For("left_n0"), null, sourceContext);
                forChange1.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("temp_left"),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    NodeType.Nop, sourceContext));
                forChange1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    NodeType.Nop, sourceContext));
                forChange1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(
                        Identifier.For("left"),
                        (isRight) ?
                        new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                        new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                        leftType, sourceContext),
                    Identifier.For("temp_left"),
                    NodeType.Nop, sourceContext));
                trueBlock5.Statements.Add(forChange1);

                if (rightDim == 2)
                {
                    For forChange1_ = new For();
                    if (isRight) forChange1_ = GenerateFor(2, Identifier.For("right_n0"), null, sourceContext);
                    else forChange1_ = GenerateFor(2, Identifier.For("right_n1"), null, sourceContext);

                    forChange1_.Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange1_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i0") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i2") }), rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    forChange1_.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            (isRight) ?
                            new ExpressionList(new Expression[2] { Identifier.For("i2"), Identifier.For("i1") }) :
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i2") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));

                    trueBlock5.Statements.Add(forChange1_);
                }
                
                if (rightDim == 1)
                {
                    trueBlock5.Statements.Add(new AssignmentStatement(
                        Identifier.For("temp_right"),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlock5.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i0") }), rightType, sourceContext),
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }), rightType, sourceContext),
                        NodeType.Nop, sourceContext));
                    trueBlock5.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            Identifier.For("right"),
                            new ExpressionList(new Expression[1] { Identifier.For("i1") }),
                            rightType, sourceContext),
                        Identifier.For("temp_right"),
                        NodeType.Nop, sourceContext));
                }

                Literal l7;
                if (leftType == SystemTypes.Double)
                    l7 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l7 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l7 = new Literal((Int64)0, leftType);
                else l7 = new Literal(0, leftType);

                /*end forChange*/
                trueBlock5.Statements.Add(new If(
                    new BinaryExpression(
                        new Indexer(Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i1"), Identifier.For("i1") }),
                            leftType, sourceContext),
                        l7,
                        NodeType.Eq, sourceContext),
                    new Block(new StatementList(
                        new Statement[] { new AssignmentStatement( 
                            Identifier.For("left"),
                            new MethodCall(
                                new MemberBinding(null, mathRoutines.GetMembersNamed(name)[0]),
                                new ExpressionList(new Expression[] { Identifier.For("left"), Identifier.For("right"), 
                                    Identifier.For("i1"), Identifier.For("i0"), Identifier.For("i1") }),
                                NodeType.Call, leftArrayType),
                            NodeType.Nop, sourceContext) }
                        ), sourceContext),
                    new Block(new StatementList())));

                //if (i1 < i0) {...}
                falseBlock1.Statements.Add(new If(
                    new BinaryExpression(Identifier.For("i1"), Identifier.For("i0"), NodeType.Lt, sourceContext),
                    trueBlock5, new Block(new StatementList())));

                //if (i1 < n0)
                trueBlock3.Statements.Add(new If(
                    new BinaryExpression(Identifier.For("i1"), Identifier.For("left_n0"), NodeType.Lt, sourceContext),
                    trueBlock4, falseBlock1));

                Literal l8;
                if (leftType == SystemTypes.Double)
                    l8 = new Literal((Double)0, leftType);
                else if (leftType == SystemTypes.Single)
                    l8 = new Literal((Single)0, leftType);
                else if (leftType == SystemTypes.Int64)
                    l8 = new Literal((Int64)0, leftType);
                else l8 = new Literal(0, leftType);

                for1.Body.Statements.Add(new If(
                    new BinaryExpression(new Indexer(
                            Identifier.For("left"),
                            new ExpressionList(new Expression[2] { Identifier.For("i0"), Identifier.For("i0") }), leftType, sourceContext),
                        l8,
                        NodeType.Eq, sourceContext),
                    trueBlock3, new Block(new StatementList())));

                method.Body.Statements.Add(for1);

                method.ReturnType = leftArrayType;
                Return ret = new Return();
                ret.Expression = Identifier.For("left");
                method.Body.Statements.Add(ret);

                //mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetGeneralizedCompareOp
        /// <summary>
        /// Returns an appropriate function for an array-scalar generalized compare operation (less, greater,...);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="leftType">Left array base type (elements type)</param>
        /// <param name="rightType">>Right type (scalar type)</param>
        /// <param name="opType">Type of operator (like NodeType.Ne)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetGeneralizedArrayScalarOp(int leftDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST indices,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(leftDim > 0);
            NodeType realOp = 0; //Real operation which will be used; this operation is opposite to opType;

            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;

            string cur_name = "Generalized", cur_nameTemp = "Generalized";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            cur_name += "Array";
            cur_nameTemp += "Array";
            int index_info = 11; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            if (opType == NodeType.Eq)
            {
                name = Identifier.For(cur_nameTemp + "ScalarEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ne;
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For(cur_nameTemp + "ScalarNonEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Eq;
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For(cur_nameTemp + "ScalarLess" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ge;
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For(cur_nameTemp + "ScalarLessEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Gt;
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For(cur_nameTemp + "ScalarGreater" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Le;
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For(cur_nameTemp + "ScalarGreaterEqual" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Lt;
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_nameTemp + "Scalar" + "_" + ovlOp.ToString() + "_" + leftDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = 0;
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Boolean GeneralizedScalarEqualNType1Type2(Type1[Ndim] A, Type2 b)
                //{
                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //            ...
                //                for(int in = 0; in < A.GetLength(n - 1); in++)
                //                    if ( A[i1, i2, ..., in] != b )
                //                        return false;
                //    return true;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightType));

                AddingParameters(method, cur_name, index_info, indices, "", "left", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(leftDim, "", leftDim, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, leftArrayType, cur_name, index_info, indices, "", "left", leftDim, bvIndices, sourceContext);

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(leftDim);
                for (int i = 0; i < leftDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvIndices, "", null, "", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source = new ExpressionList();

                AddingComplexIndexing(indices, index_source, cur_name, index_info, leftDim, "", sourceContext);

                If ifFor = new If();
                // TODO: case when operator is overloaded

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = realOp;
                    internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source, leftType);
                    internalOp.Operand2 = Identifier.For("right");
                    ifFor.Condition = internalOp;
                }
                else
                {
                    ifFor.Condition = new UnaryExpression(
                        new MethodCall(ovlOp,
                            new ExpressionList(new Expression[] { 
                                new Indexer(Identifier.For("left"), index_source, leftType),
                                Identifier.For("right") }),
                            NodeType.Call),
                        NodeType.LogicalNot, sourceContext);
                }

                ifFor.TrueBlock = new Block();
                Return ret_false = new Return();
                ret_false.Expression = new Literal(false, SystemTypes.Boolean, sourceContext);
                ifFor.TrueBlock = new Block();
                ifFor.TrueBlock.Statements = new StatementList(1);
                ifFor.TrueBlock.Statements.Add(ret_false);

                ((For)forStatements[leftDim - 1]).Body.Statements.Add(ifFor);

                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == leftDim - 1))
                {
                    ((For)forStatements[leftDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (leftDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = SystemTypes.Boolean;

                Return ret_true = new Return();
                ret_true.Expression = new Literal(true, SystemTypes.Boolean, sourceContext);
                method.Body.Statements.Add(ret_true);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }

        /// <summary>
        /// Returns appropriate function for a generalized scalar-array compare operation (less, greater, ...);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (scalar type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Mul)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetGeneralizedScalarArrayOp(int rightDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST indices,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            NodeType realOp = 0; //Real operation which will be used; this operation is opposite to opType;

            int[] bvIndices; //array wich shows where indices are boolean vectors
            int bvIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string cur_name = "GeneralizedScalar", cur_nameTemp = "GeneralizedScalar";
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    if ((indices[i].type is INTEGER_TYPE) || (indices[i].type is CARDINAL_TYPE))
                    {
                        cur_name += "S";
                        cur_nameTemp += "S" + "_" + indices[i].type.ToString() + "_";
                    }
                    else if (indices[i].type is RANGE_TYPE)
                    {
                        cur_name += "R";
                        cur_nameTemp += "R";
                        if (indices[i] is ARRAY_RANGE)
                        {
                            cur_nameTemp += "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            cur_nameTemp += (indices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (indices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(indices[i].type)).base_type is INTEGER_TYPE)
                        {
                            cur_name += "I";
                            cur_nameTemp += "I" + (((ARRAY_TYPE)(indices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            cur_name += "C";
                            cur_nameTemp += "C" + (((ARRAY_TYPE)(indices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(indices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            cur_name += "B";
                            cur_nameTemp += "B";
                            bvIndicesLength++;
                        }
                    }
                }
            }
            cur_name += "Array";
            cur_nameTemp += "Array";
            int index_info = 17; //the index in cur_name from which you can read info about the type of index (S, R, N or B)
            bvIndices = new int[bvIndicesLength];

            if (opType == NodeType.Eq)
            {
                name = Identifier.For(cur_nameTemp + "Equal" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ne;
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For(cur_nameTemp + "NonEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Eq;
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For(cur_nameTemp + "Less" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ge;
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For(cur_nameTemp + "LessEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Gt;
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For(cur_nameTemp + "Greater" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Le;
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For(cur_nameTemp + "GreaterEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Lt;
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_nameTemp + "_" + ovlOp.ToString() + "_" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = 0;
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Boolean GeneralizedScalarEqualNType1Type2(Type2 a, Type1[Ndim] B)
                //{
                //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
                //        for (int i2 = 0; i2 < A.GetLength(1); i2++)
                //            ...
                //                for(int in = 0; in < A.GetLength(n - 1); in++)
                //                    if ( a != B[i1, i2, ..., in] )
                //                        return false;
                //    return true;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                AddingParameters(method, cur_name, index_info, indices, "", "right", sourceContext);
                
                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(rightDim, "", rightDim, "", 0, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, rightArrayType, cur_name, index_info, indices, "", "right", rightDim, bvIndices, sourceContext);

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(rightDim);
                for (int i = 0; i < rightDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, null, "", bvIndices, "", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source = new ExpressionList();
                AddingComplexIndexing(indices, index_source, cur_name, index_info, rightDim, "", sourceContext);

                If ifFor = new If();
                // TODO: case when operator is overloaded

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = realOp;
                    internalOp.Operand1 = Identifier.For("left");
                    internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source, rightType);
                    ifFor.Condition = internalOp;
                }
                else
                {
                    ifFor.Condition = new UnaryExpression(
                        new MethodCall(ovlOp,
                            new ExpressionList(new Expression[] { 
                                Identifier.For("left"),
                                new Indexer(Identifier.For("right"), index_source, rightType) }),
                            NodeType.Call),
                        NodeType.LogicalNot, sourceContext);
                }

                ifFor.TrueBlock = new Block();
                Return ret_false = new Return();
                ret_false.Expression = new Literal(false, SystemTypes.Boolean, sourceContext);
                ifFor.TrueBlock = new Block();
                ifFor.TrueBlock.Statements = new StatementList(1);
                ifFor.TrueBlock.Statements.Add(ret_false);

                ((For)forStatements[rightDim - 1]).Body.Statements.Add(ifFor);

                if ((bvIndicesLength > 0) && (bvIndices[bvIndicesLength - 1] == rightDim - 1))
                {
                    ((For)forStatements[rightDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("j" + (rightDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = SystemTypes.Boolean;

                Return ret_true = new Return();
                ret_true.Expression = new Literal(true, SystemTypes.Boolean, sourceContext);
                method.Body.Statements.Add(ret_true);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        
        /// <summary>
        /// Returns an appropriate generalized function for a array-array standard operation (+, -, .*, ./, div, mod);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Mul)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetGeneralizedArrayArrayOp(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            //Debug.Assert(leftDim == rightDim);
            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            NodeType realOp = 0; //Real operation which will be used; this operation is opposite to opType;

            if (opType == NodeType.Eq)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ne;
            }
            else if (opType == NodeType.Ne)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayNonEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Eq;
            }
            else if (opType == NodeType.Lt)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayrLess" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Ge;
            }
            else if (opType == NodeType.Le)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayrLessEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Gt;
            }
            else if (opType == NodeType.Gt)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayGreater" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Le;
            }
            else if (opType == NodeType.Ge)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "ArrayGreaterEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = NodeType.Lt;
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For("Generalized" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + "Array" + "_" + ovlOp.ToString() + "_" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
                realOp = 0;
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type[Ndim] GeneralizedEqualNType1Type2(Type1[Ndim] A, Type2[Ndim] B)
                //{
                //    int k1 = A.GetLength(0);
                //    ...
                //    int kn = A.GetLength(n - 1);
                //    if ((k1 != B.GetLength(0)) || ... || (kn != B.GetLength(n - 1)))
                //        throw new LengthsNotEqualException();
                //    Type[Ndim] Res;
                //    Res = new Type[k1, ..., kn];
                //    for (int i1 = 0; i1 < k1; i1++)
                //        for (int i2 = 0; i2 < k2; i2++)
                //            ...
                //                for(int in = 0; in < kn; in++)
                //                  if (A[i1, i2, ..., in] != B[i1, i2, ..., in])
                //                      return false;
                //    return true;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = leftDim;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = rightDim;

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(leftDim, "left_", leftDim, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                int returnDim = (leftDim < rightDim) ? leftDim : rightDim;
                AddingNi(method.Body, leftArrayType, strLeftIndices, 0, leftIndices, "left_", "left", returnDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, rightArrayType, strRightIndices, 0, rightIndices, "right_", "right", returnDim, bvRightIndices, sourceContext);
                AddingNiChecking(method.Body, strRightIndices, 0, rightIndices, "left_", "right_", returnDim, bvRightIndices, sourceContext);

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("left_n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvLeftIndices, "left_", bvRightIndices, "right_", sourceContext); ;

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source_left = new ExpressionList();
                ExpressionList index_source_right = new ExpressionList();
                
                AddingComplexIndexing(leftIndices, index_source_left, strLeftIndices, 0, leftDim, "left_", sourceContext);
                AddingComplexIndexing(rightIndices, index_source_right, strRightIndices, 0, rightDim, "right_", sourceContext);
                
                If ifFor = new If();
                // TODO: case when operator is overloaded

                BinaryExpression internalOp = null;
                if (opType != NodeType.MethodCall)
                {
                    internalOp = new BinaryExpression();
                    internalOp.SourceContext = sourceContext;
                    internalOp.NodeType = realOp;
                    internalOp.Operand1 = new Indexer(Identifier.For("left"), index_source_left, leftType);
                    internalOp.Operand2 = new Indexer(Identifier.For("right"), index_source_right, rightType);
                    ifFor.Condition = internalOp;
                }
                else
                {
                    ifFor.Condition = new UnaryExpression(
                        new MethodCall( ovlOp,
                            new ExpressionList(new Expression[] { 
                                new Indexer(Identifier.For("left"), index_source_left, leftType),
                                new Indexer(Identifier.For("right"), index_source_right, rightType) }),
                            NodeType.Call),
                        NodeType.LogicalNot, sourceContext);
                }

                ifFor.TrueBlock = new Block();
                Return ret_false = new Return();
                ret_false.Expression = new Literal(false, SystemTypes.Boolean, sourceContext);
                ifFor.TrueBlock = new Block();
                ifFor.TrueBlock.Statements = new StatementList(1);
                ifFor.TrueBlock.Statements.Add(ret_false);

                ((For)forStatements[leftDim - 1]).Body.Statements.Add(ifFor);

                if ((bvLeftIndicesLength > 0) &&
                    (bvLeftIndices[bvLeftIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("left_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                if ((bvRightIndicesLength > 0) &&
                    (bvRightIndices[bvRightIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("right_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = SystemTypes.Boolean;

                Return ret_true = new Return();
                ret_true.Expression = new Literal(true, SystemTypes.Boolean, sourceContext);
                method.Body.Statements.Add(ret_true);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetDeepCopy
        /// <summary>
        /// Returns a function for a deep copy of sourceArray to the destination array;
        /// returns reference to the destination array
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="sourceDim">source array rank (dimension)</param>
        /// <param name="sourceType">>source type (elements type)</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetDeepCopy(int destDim, int sourceDim, TypeNode destType, TypeNode sourceType,
            EXPRESSION_LIST destIndices, EXPRESSION_LIST sourceIndices,
            NodeType opType, Expression ovlOp,
            SourceContext sourceContext)
        {
            Debug.Assert(destDim > 0);
            //Debug.Assert(sourceDim == destDim);
            int[] bvSourceIndices; //array wich shows where in the source indices are boolean vectors
            int bvSourceIndicesLength = 0;
            int[] bvDestIndices; //array wich shows where in the dest indices are boolean vectors
            int bvDestIndicesLength = 0;

            int destRealDim = destDim, sourceRealDim = sourceDim; //we need it to compute real dim of arrays
            //for example: a[1, ..] := b[1, ..];
            //if returnDim is 2, then it causes errors; so, returnDim should be equal to realReturnDim = 1.

            Identifier name = Identifier.Empty;
            string strSourceIndices = "", strDestIndices = "", strSourceIndicesTemp = "", strDestIndicesTemp = "";
            if (sourceIndices != null)
            {
                for (int i = 0; i < sourceIndices.Length; i++)
                {
                    if ((sourceIndices[i].type is INTEGER_TYPE) || (sourceIndices[i].type is CARDINAL_TYPE))
                    {
                        strSourceIndices += "S";
                        strSourceIndicesTemp += "S" + "_" + sourceIndices[i].type.ToString() + "_";
                        sourceRealDim--;
                    }
                    else if (sourceIndices[i].type is RANGE_TYPE)
                    {
                        strSourceIndices += "R";
                        strSourceIndicesTemp += "R";
                        if (sourceIndices[i] is ARRAY_RANGE)
                        {
                            strSourceIndicesTemp += "_";
                            strSourceIndicesTemp += (sourceIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strSourceIndicesTemp += (sourceIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (sourceIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strSourceIndices += "I";
                            strSourceIndicesTemp += "I" + (((ARRAY_TYPE)(sourceIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strSourceIndices += "C";
                            strSourceIndicesTemp += "C" + (((ARRAY_TYPE)(sourceIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strSourceIndices += "B";
                            strSourceIndicesTemp += "B";
                            bvSourceIndicesLength++;
                        }
                    }
                }
            }
            bvSourceIndices = new int[bvSourceIndicesLength];

            if (destIndices != null)
            {
                for (int i = 0; i < destIndices.Length; i++)
                {
                    if ((destIndices[i].type is INTEGER_TYPE) || (destIndices[i].type is CARDINAL_TYPE))
                    {
                        strDestIndices += "S";
                        strDestIndicesTemp += "S" + "_" + destIndices[i].type.ToString() + "_";
                        destRealDim--;
                    }
                    else if (destIndices[i].type is RANGE_TYPE)
                    {
                        strDestIndices += "R";
                        strDestIndicesTemp += "R";
                        if (destIndices[i] is ARRAY_RANGE)
                        {
                            strDestIndicesTemp += "_";
                            strDestIndicesTemp += (destIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strDestIndicesTemp += (destIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (destIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(destIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strDestIndices += "I";
                            strDestIndicesTemp += "I" + (((ARRAY_TYPE)(destIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(destIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strDestIndices += "C";
                            strDestIndicesTemp += "C" + (((ARRAY_TYPE)(destIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(destIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strDestIndices += "B";
                            strDestIndicesTemp += "B";
                            bvDestIndicesLength++;
                        }
                    }
                }
            }
            bvDestIndices = new int[bvDestIndicesLength];

            if (opType != NodeType.MethodCall)
            {
                name = Identifier.For("DeepCopy" + strSourceIndicesTemp + "Array" + strDestIndicesTemp + "Array" + sourceDim.ToString() + "d" + sourceType.Name + destType.Name);
            }
            else
            {
                name = Identifier.For("DeepCopy" + strSourceIndicesTemp + "Array" + strDestIndicesTemp + "Array" + "_" + ovlOp.ToString() + "_" + sourceDim.ToString() + "d" + sourceType.Name + destType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                // Here source is sourceArray, destination is destination array

                //void DeepCopyNType1Type2(Type1[Ndim] sourceArray, Type2[Ndym] destinationArray)
                //{
                //    if ((sourceArray.GetLength(0) != destinationArray.GetLength(0)) || ... ||
                //        (sourceArray.GetLength(n - 1) != destinationArray.GetLength(n - 1)))
                //            throw new LengthsNotEqualException();
                //    for (int i1 = 0; i1 < sourceArray.GetLength(0); i1++)
                //        for (int i2 = 0; i2 < sourceArray.GetLength(1); i2++)
                //            ...
                //                for(int in = 0; in < sourceArray.GetLength(n - 1); in++)
                //                    destinationArray[i1, i2, ..., in] =
                //                        sourceArray[i1, i2, ..., in];
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression sourceArrayType = new ArrayTypeExpression();
                sourceArrayType.ElementType = sourceType;
                sourceArrayType.Rank = sourceDim;

                ArrayTypeExpression destArrayType = new ArrayTypeExpression();
                destArrayType.ElementType = destType;
                destArrayType.Rank = destDim;

                method.Parameters.Add(new Parameter(Identifier.For("dest"), destArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("source"), sourceArrayType));

                int returnDim = (destRealDim < sourceRealDim) ? destRealDim : sourceRealDim;
                //in fact returnDim is equal to destDim, because we return dest;
                //but we use this formula to create the correct number of nested for cycles

                AddingParameters(method, strDestIndices, 0, destIndices, "dest_", "dest", sourceContext);
                AddingParameters(method, strSourceIndices, 0, sourceIndices, "source_", "source", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "source_", returnDim, "dest_", returnDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, sourceArrayType, strSourceIndices, 0, sourceIndices, "source_", "source", returnDim, bvSourceIndices, sourceContext);
                AddingNi(method.Body, destArrayType, strDestIndices, 0, destIndices, "dest_", "dest", returnDim, bvDestIndices, sourceContext);
                AddingNiChecking(method.Body, strDestIndices, 0, destIndices, "source_", "dest_", returnDim, bvDestIndices, sourceContext);

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("source_n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvSourceIndices, "source_", bvDestIndices, "dest_", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source = new ExpressionList();
                ExpressionList index_target = new ExpressionList();
                
                AddingComplexIndexing(sourceIndices, index_source, strSourceIndices, 0, sourceDim, "source_", sourceContext);
                AddingComplexIndexing(destIndices, index_target, strDestIndices, 0, destDim, "dest_", sourceContext);

                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;
                asgnFor.Source = new Indexer(Identifier.For("source"), index_source, sourceType);
                asgnFor.Target = new Indexer(Identifier.For("dest"), index_target, destType);

                if (opType != NodeType.MethodCall)
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);
                }
                else
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(
                        new ExpressionStatement(
                            new MethodCall( 
                                ovlOp,
                                new ExpressionList(new Expression[] { 
                                    new UnaryExpression(asgnFor.Target, NodeType.RefAddress) , asgnFor.Source }),
                                NodeType.Call)));
                }

                if ((bvSourceIndicesLength > 0) &&
                    (bvSourceIndices[bvSourceIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("source_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                if ((bvDestIndicesLength > 0) &&
                    (bvDestIndices[bvDestIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("dest_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = destArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("dest");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }

        /// <summary>
        /// Returns a function for a deep copy of source array to a new array;
        /// Creates this new array and returns a reference to it;
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="sourceDim">source array rank (dimension)</param>
        /// <param name="sourceType">>source type (elements type)</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetDeepCopyWithMemoryAlloc(int destDim, int sourceDim, TypeNode destType, TypeNode sourceType, 
            EXPRESSION_LIST sourceIndices,
            NodeType opType, Expression ovlOp,
            SourceContext sourceContext)
        {
            Debug.Assert(destDim > 0);
            //Debug.Assert(sourceDim == destDim);
            int[] bvSourceIndices; //array wich shows where in the source indices are boolean vectors
            int bvSourceIndicesLength = 0;

            Identifier name = Identifier.Empty;
            string strSourceIndices = "", strSourceIndicesTemp = "";
            if (sourceIndices != null)
            {
                for (int i = 0; i < sourceIndices.Length; i++)
                {
                    if ((sourceIndices[i].type is INTEGER_TYPE) || (sourceIndices[i].type is CARDINAL_TYPE))
                    {
                        strSourceIndices += "S";
                        strSourceIndicesTemp += "S" + "_" + sourceIndices[i].type.ToString() + "_";
                    }
                    else if (sourceIndices[i].type is RANGE_TYPE)
                    {
                        strSourceIndices += "R";
                        strSourceIndicesTemp += "R";
                        if (sourceIndices[i] is ARRAY_RANGE)
                        {
                            strSourceIndicesTemp += "_";
                            strSourceIndicesTemp += (sourceIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strSourceIndicesTemp += (sourceIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (sourceIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strSourceIndices += "I";
                            strSourceIndicesTemp += "I" + (((ARRAY_TYPE)(sourceIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strSourceIndices += "C";
                            strSourceIndicesTemp += "C" + (((ARRAY_TYPE)(sourceIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(sourceIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strSourceIndices += "B";
                            strSourceIndicesTemp += "B";
                            bvSourceIndicesLength++;
                        }
                    }
                }
            }
            bvSourceIndices = new int[bvSourceIndicesLength];

            if (opType != NodeType.MethodCall)
            {
                name = Identifier.For("DeepCopyWithMemoryAlloc" + strSourceIndicesTemp + "Array" + "Array" + destDim.ToString() + "d" + sourceType.Name + destType.Name);
            }
            else
            {
                name = Identifier.For("DeepCopyWithMemoryAlloc" + strSourceIndicesTemp + "Array" + "Array" + "_" + ovlOp.ToString() + "_" + destDim.ToString() + "d" + sourceType.Name + destType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //Type[Ndim] ElementWisePlusNType1Type2(Type1[Ndim] A, Type2[Ndim] B)
                //{
                //    int k1 = A.GetLength(0);
                //    ...
                //    int kn = A.GetLength(n - 1);
                //    if ((k1 != B.GetLength(0)) || ... || (kn != B.GetLength(n - 1)))
                //        throw new LengthsNotEqualException();
                //    Type[Ndim] Res;
                //    Res = new Type[k1, ..., kn];
                //    for (int i1 = 0; i1 < k1; i1++)
                //        for (int i2 = 0; i2 < k2; i2++)
                //            ...
                //                for(int in = 0; in < kn; in++)
                //                    Res[i1, i2, ..., in] = A[i1, i2, ..., in] + B[i1, i2, ..., in];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression sourceArrayType = new ArrayTypeExpression();
                sourceArrayType.ElementType = sourceType;
                sourceArrayType.Rank = sourceDim;

                ArrayTypeExpression destArrayType = new ArrayTypeExpression();
                destArrayType.ElementType = destType;
                destArrayType.Rank = destDim;

                int returnDim = (destDim < sourceDim) ? destDim : sourceDim;

                method.Parameters.Add(new Parameter(Identifier.For("dest"), destArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("source"), sourceArrayType));

                AddingParameters(method, strSourceIndices, 0, sourceIndices, "source_", "source", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(returnDim, "source_", returnDim, "dest_", returnDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                AddingNi(method.Body, sourceArrayType, strSourceIndices, 0, sourceIndices, "source_", "source", returnDim, bvSourceIndices, sourceContext);

                //Res = new Type[n0, ..., n returnDim - 1];
                //resArrayType == destArrayType

                ExpressionList sizes = new ExpressionList();
                for (int i = 0; i < returnDim; i++)
                {
                    sizes.Add(Identifier.For("source_n" + i.ToString()));
                }

                method.Body.Statements.Add(GenerateLocalResArray(destArrayType, sizes, sourceContext));

                //for (int i1 = 0; i1 < A.GetLength(0); i1++) 
                //  for ....
                StatementList forStatements = new StatementList(returnDim);
                for (int i = 0; i < returnDim; i++)
                {
                    forStatements.Add(GenerateFor(i, Identifier.For("source_n" + i.ToString()), null, sourceContext));
                }

                AddingWhileStmIntoFor(method.Body, forStatements, bvSourceIndices, "source_", null, "dest_", sourceContext);

                //generating index [i0, ... i returnDim - 1]
                ExpressionList index_source = new ExpressionList();
                ExpressionList index_dest = new ExpressionList();
                
                AddingComplexIndexing(sourceIndices, index_source, strSourceIndices, 0, sourceDim, "source_", sourceContext);
                for (int i = 0; i < returnDim; i++)
                {
                    index_dest.Add(Identifier.For("i" + i.ToString()));
                }

                AssignmentStatement asgnFor = new AssignmentStatement();
                // TODO: case when operator is overloaded
                asgnFor.Operator = NodeType.Nop;
                asgnFor.Source = new Indexer(Identifier.For("source"), index_source, sourceType);
                asgnFor.Target = new Indexer(Identifier.For("res"), index_dest, destType);

                if (opType != NodeType.MethodCall)
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(asgnFor);
                }
                else
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(
                        new ExpressionStatement(
                            new MethodCall(
                                ovlOp,
                                new ExpressionList(new Expression[] { 
                                    new UnaryExpression(asgnFor.Target, NodeType.RefAddress), asgnFor.Source }),
                                NodeType.Call)));
                }

                if ((bvSourceIndicesLength > 0) &&
                    (bvSourceIndices[bvSourceIndicesLength - 1] == returnDim - 1))
                {
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(new AssignmentStatement(
                        Identifier.For("source_j" + (returnDim - 1).ToString()),
                        new Literal(1, SystemTypes.Int32),
                        NodeType.Add));
                }

                method.Body.Statements.Add(forStatements[0]);

                method.ReturnType = destArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetMainLeftLUDivisionOp
        /// <summary>
        /// Returns an appropriate function for a array-array standard operation (+, -, .*, ./, div, mod, less, greater);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetMainLeftLUDivisionOp(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            TYPE leftArrType, TYPE rightArrType, TYPE resArrType,
            SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;
            
            #region Name
            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + 
                "ArrayMainLeftLUDivision" + 
                rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            #endregion

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;
            
                method.Parameters.Add(new Parameter(Identifier.For("left"), new ArrayTypeExpression(leftType, leftDim)));
                method.Parameters.Add(new Parameter(Identifier.For("right"), new ArrayTypeExpression(rightType, rightDim)));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(1, "left_", leftDim, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                ArrayTypeExpression ate_res = resArrType.convert() as ArrayTypeExpression;
                ArrayTypeExpression ate_left = leftArrType.convert() as ArrayTypeExpression;
                ArrayTypeExpression ate_right = rightArrType.convert() as ArrayTypeExpression;

                AddingNi(method.Body, ate_left, strLeftIndices, 0, leftIndices, "left_", "left", leftDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, ate_right, strRightIndices, 0, rightIndices, "right_", "right", rightDim, bvRightIndices, sourceContext);

                //if (left_n0 != left_n1) throw new IncompatibleSizesException(...);
                method.Body.Statements.Add(
                    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                        Identifier.For("left_n1"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));
                //if (left_n0 != right_n0) throw new IncompatibleSizesException(...);
                method.Body.Statements.Add(
                    GenerateIfStatementWithThrow(Identifier.For("left_n0"),
                        Identifier.For("right_n0"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));

                # region //Double[,] __math_aux_matrix;
                string arr_name = "__math_aux_matrix";
                LocalDeclarationsStatement lds = new LocalDeclarationsStatement();
                lds.Constant = false;
                lds.Declarations = new LocalDeclarationList(1);
                lds.InitOnly = false;
                lds.Type = new ArrayTypeExpression(ate_res.ElementType, ate_left.Rank);
                LocalDeclaration ld = new LocalDeclaration();
                ld.Name = Identifier.For(arr_name);
                ld.InitialValue = new Literal(null, SystemTypes.Object, sourceContext);
                lds.Declarations.Add(ld);
                method.Body.Statements.Add(lds);
                #endregion

                #region //Double[(,)] __math_aux_right_part;
                string arr_right_part_name = "__math_aux_right_part";
                LocalDeclarationsStatement lds2 = new LocalDeclarationsStatement();
                lds2.Constant = false;
                lds2.Declarations = new LocalDeclarationList(1);
                lds2.InitOnly = false;
                lds2.Type = new ArrayTypeExpression(ate_right.ElementType, ate_right.Rank);
                LocalDeclaration ld2 = new LocalDeclaration();
                ld2.Name = Identifier.For(arr_right_part_name);
                ld2.InitialValue = new Literal(null, SystemTypes.Object, sourceContext);
                lds2.Declarations.Add(ld2);
                method.Body.Statements.Add(lds2);
                #endregion

                #region //__math_aux_matrix = DeepCopy(__math_aux_matrix, left_operand, indices);
                MethodCall mathCall1 = new MethodCall();
                mathCall1.Operands = new ExpressionList();
                mathCall1.Operands.Add(Identifier.For(arr_name));
                if (leftIndices != null)
                {
                    mathCall1.Operands.Add(Identifier.For("left"));
                    int n_s = 0;
                    int n_r = 0;
                    for (int i = 0; i < leftIndices.Length; i++)
                    {
                        if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                        {
                            mathCall1.Operands.Add(Identifier.For("left_n_s" + n_s.ToString()));
                            n_s++;
                        }
                        else if (leftIndices[i].type is RANGE_TYPE)
                        {
                            mathCall1.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_from"));
                            mathCall1.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_wasToWritten"));
                            mathCall1.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_to"));
                            mathCall1.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_by"));
                            n_r++;
                        }
                        else if (leftIndices[i].type is ARRAY_TYPE)
                        {
                            mathCall1.Operands.Add(Identifier.For("left_n_r" + n_r.ToString()));
                            n_r++;
                        }
                    }

                    mathCall1.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_left.Rank,
                        leftDim,
                        ate_res.ElementType,
                        leftType,
                        leftIndices,
                        NodeType.Nop, null,
                        sourceContext));
                }
                else
                {
                    mathCall1.Operands.Add(Identifier.For("left"));
                    mathCall1.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_left.Rank,
                        leftDim,
                        ate_res.ElementType,
                        leftType,
                        null,
                        NodeType.Nop, null,
                        sourceContext));
                }

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall1,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_right_part = DeepCopy(__math_aux_right_part, right_operand, indices);
                MethodCall mathCall11 = new MethodCall();
                mathCall11.Operands = new ExpressionList();
                mathCall11.Operands.Add(Identifier.For(arr_right_part_name));
                if (rightIndices != null)
                {
                    mathCall11.Operands.Add(Identifier.For("right"));
                    int n_s = 0;
                    int n_r = 0;
                    for (int i = 0; i < rightIndices.Length; i++)
                    {
                        if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                        {
                            mathCall11.Operands.Add(Identifier.For("right_n_s" + n_s.ToString()));
                            n_s++;
                        }
                        else if (rightIndices[i].type is RANGE_TYPE)
                        {
                            mathCall11.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_from"));
                            mathCall11.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_wasToWritten"));
                            mathCall11.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_to"));
                            mathCall11.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_by"));
                            n_r++;
                        }
                        else if (rightIndices[i].type is ARRAY_TYPE)
                        {
                            mathCall11.Operands.Add(Identifier.For("right_n_r" + n_r.ToString()));
                            n_r++;
                        }
                    }

                    mathCall11.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_right.Rank,
                        rightDim,
                        ate_right.ElementType,
                        rightType,
                        rightIndices,
                        NodeType.Nop, null,
                        sourceContext));
                }
                else
                {
                    mathCall11.Operands.Add(Identifier.For("right"));
                    mathCall11.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_right.Rank,
                        rightDim,
                        ate_right.ElementType,
                        rightType,
                        null,
                        NodeType.Nop, null,
                        sourceContext));
                }

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_right_part_name),
                    mathCall11,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_matrix = ChangeLinePosition(__math_aux_matrix, __math_aux_right_part, 0, -1, -1)
                MethodCall mathCall22 = new MethodCall();
                mathCall22.Operands = new ExpressionList();
                mathCall22.Operands.Add(Identifier.For(arr_name));
                mathCall22.Operands.Add(Identifier.For(arr_right_part_name));
                mathCall22.Operands.Add(new Literal(0, SystemTypes.Int32));
                mathCall22.Operands.Add(new Literal(-1, SystemTypes.Int32));
                mathCall22.Operands.Add(new Literal(-1, SystemTypes.Int32));
                mathCall22.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetChangeLinePos(
                        ate_res.ElementType,
                        ate_right.ElementType,
                        ate_right.Rank,
                        false,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall22,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_matrix = LUDecomposition(__math_aux_matrix)
                MethodCall mathCall2 = new MethodCall();
                mathCall2.Operands = new ExpressionList();
                mathCall2.Operands.Add(Identifier.For(arr_name));
                mathCall2.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetLUDecomposition(
                        ate_res.ElementType,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall2,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //Double[(,)] __math_slu_res;
                string arr_res_name = "__math_slu_res";
                LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                lds1.Constant = false;
                lds1.Declarations = new LocalDeclarationList(1);
                lds1.InitOnly = false;
                lds1.Type = new ArrayTypeExpression(ate_res.ElementType, ate_res.Rank);
                LocalDeclaration ld1 = new LocalDeclaration();
                ld1.Name = Identifier.For(arr_res_name);
                ConstructArray new_array = new ConstructArray();
                new_array.ElementType = ate_res.ElementType;
                new_array.Operands = new ExpressionList();
                new_array.Rank = ate_res.Rank;
                new_array.Operands.Add(
                    new MethodCall(
                        new MemberBinding(
                            Identifier.For(arr_right_part_name),
                            SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call, SystemTypes.Int32));
                if (ate_res.Rank > 1)
                {
                    new_array.Operands.Add(
                        new MethodCall(
                            new MemberBinding(
                                Identifier.For(arr_right_part_name),
                                SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                                NodeType.Call, SystemTypes.Int32));
                }
                ld1.InitialValue = new_array;
                lds1.Declarations.Add(ld1);
                method.Body.Statements.Add(lds1);
                #endregion

                #region //solving
                MethodCall mathCall3 = new MethodCall();
                mathCall3.Operands = new ExpressionList();
                mathCall3.Operands.Add(Identifier.For(arr_name));
                mathCall3.Operands.Add(Identifier.For(arr_right_part_name));
                mathCall3.Operands.Add(Identifier.For(arr_res_name));
                mathCall3.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetLeftLUDivision(
                        ate_res.ElementType,
                        ate_right.ElementType,
                        ate_res.ElementType,
                        ate_res.Rank,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_res_name),
                    mathCall3,
                    NodeType.Nop,
                    sourceContext));
                #endregion
                ////*********/////

                method.ReturnType = new ArrayTypeExpression(ate_res.ElementType, ate_res.Rank);

                Return ret = new Return();
                ret.Expression = Identifier.For("__math_slu_res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetMainRightLUDivisionOp
        /// <summary>
        /// Returns an appropriate function for a array-array standard operation (+, -, .*, ./, div, mod, less, greater);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Left array rank (dimension)</param>
        /// <param name="rightDim">Right array rank (dimension)</param>
        /// <param name="leftType">>Left type (elements type)</param>
        /// <param name="rightType">Right array base type (elements type)</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetMainRightLUDivisionOp(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType,
            EXPRESSION_LIST leftIndices, EXPRESSION_LIST rightIndices,
            TYPE leftArrType, TYPE rightArrType, TYPE resArrType,
            SourceContext sourceContext)
        {
            Debug.Assert(rightDim > 0);
            int[] bvLeftIndices; //array wich shows where in the left indices are boolean vectors
            int bvLeftIndicesLength = 0;
            int[] bvRightIndices; //array wich shows where in the right indices are boolean vectors
            int bvRightIndicesLength = 0;

            #region Name
            Identifier name = Identifier.Empty;
            string strLeftIndices = "", strRightIndices = "", strLeftIndicesTemp = "", strRightIndicesTemp = "";
            if (leftIndices != null)
            {
                for (int i = 0; i < leftIndices.Length; i++)
                {
                    if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                    {
                        strLeftIndices += "S";
                        strLeftIndicesTemp += "S" + "_" + leftIndices[i].type.ToString() + "_";
                    }
                    else if (leftIndices[i].type is RANGE_TYPE)
                    {
                        strLeftIndices += "R";
                        strLeftIndicesTemp += "R";
                        if (leftIndices[i] is ARRAY_RANGE)
                        {
                            strLeftIndicesTemp += "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strLeftIndicesTemp += (leftIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (leftIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strLeftIndices += "I";
                            strLeftIndicesTemp += "I" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strLeftIndices += "C";
                            strLeftIndicesTemp += "C" + (((ARRAY_TYPE)(leftIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(leftIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strLeftIndices += "B";
                            strLeftIndicesTemp += "B";
                            bvLeftIndicesLength++;
                        }
                    }
                }
            }
            bvLeftIndices = new int[bvLeftIndicesLength];

            if (rightIndices != null)
            {
                for (int i = 0; i < rightIndices.Length; i++)
                {
                    if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                    {
                        strRightIndices += "S";
                        strRightIndicesTemp += "S" + "_" + rightIndices[i].type.ToString() + "_";
                    }
                    else if (rightIndices[i].type is RANGE_TYPE)
                    {
                        strRightIndices += "R";
                        strRightIndicesTemp += "R";
                        if (rightIndices[i] is ARRAY_RANGE)
                        {
                            strRightIndicesTemp += "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).from.type.ToString() + "_";
                            strRightIndicesTemp += (rightIndices[i] as ARRAY_RANGE).to.type.ToString() + "_";
                        }
                    }
                    else if (rightIndices[i].type is ARRAY_TYPE)
                    {
                        if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is INTEGER_TYPE)
                        {
                            strRightIndices += "I";
                            strRightIndicesTemp += "I" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as INTEGER_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is CARDINAL_TYPE)
                        {
                            strRightIndices += "C";
                            strRightIndicesTemp += "C" + (((ARRAY_TYPE)(rightIndices[i].type)).base_type as CARDINAL_TYPE).width;
                        }
                        else if (((ARRAY_TYPE)(rightIndices[i].type)).base_type is BOOLEAN_TYPE)
                        {
                            strRightIndices += "B";
                            strRightIndicesTemp += "B";
                            bvRightIndicesLength++;
                        }
                    }
                }
            }
            bvRightIndices = new int[bvRightIndicesLength];

            name = Identifier.For("ElementWise" + strLeftIndicesTemp + "Array" + strRightIndicesTemp + 
                "ArrayMainRightLUDivision" +
                rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            #endregion

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();
                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters.Add(new Parameter(Identifier.For("left"), new ArrayTypeExpression(leftType, leftDim)));
                method.Parameters.Add(new Parameter(Identifier.For("right"), new ArrayTypeExpression(rightType, rightDim)));

                AddingParameters(method, strLeftIndices, 0, leftIndices, "left_", "left", sourceContext);
                AddingParameters(method, strRightIndices, 0, rightIndices, "right_", "right", sourceContext);

                LocalDeclarationsStatement forVariables = GenerateLocalIntVariables(1, "left_", leftDim, "right_", rightDim, sourceContext);
                method.Body.Statements.Add(forVariables);

                ArrayTypeExpression ate_res = resArrType.convert() as ArrayTypeExpression;
                ArrayTypeExpression ate_left = leftArrType.convert() as ArrayTypeExpression;
                ArrayTypeExpression ate_right = rightArrType.convert() as ArrayTypeExpression;
                
                AddingNi(method.Body, ate_left, strLeftIndices, 0, leftIndices, "left_", "left", leftDim, bvLeftIndices, sourceContext);
                AddingNi(method.Body, ate_right, strRightIndices, 0, rightIndices, "right_", "right", rightDim, bvRightIndices, sourceContext);

                //if (right_n0 != right_n1) throw new IncompatibleSizesException(...);
                method.Body.Statements.Add(
                    GenerateIfStatementWithThrow(Identifier.For("right_n0"),
                        Identifier.For("right_n1"),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine,
                        sourceContext.StartColumn,
                        sourceContext));
                if ((resArrType as ARRAY_TYPE).dimensions.Length == 1)
                {
                    //if (right_n0 != left_n0) throw new IncompatibleSizesException(...);
                    method.Body.Statements.Add(
                        GenerateIfStatementWithThrow(Identifier.For("right_n0"),
                            Identifier.For("left_n0"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine,
                            sourceContext.StartColumn,
                            sourceContext));
                }
                if ((resArrType as ARRAY_TYPE).dimensions.Length == 2)
                {
                    //if (right_n0 != left_n1) throw new IncompatibleSizesException(...);
                    method.Body.Statements.Add(
                        GenerateIfStatementWithThrow(Identifier.For("right_n0"),
                            Identifier.For("left_n1"),
                            NodeType.Ne,
                            STANDARD.IncompatibleSizesException,
                            sourceContext.StartLine,
                            sourceContext.StartColumn,
                            sourceContext));
                }

                # region //Double[,] __math_aux_matrix;
                string arr_name = "__math_aux_matrix";
                LocalDeclarationsStatement lds = new LocalDeclarationsStatement();
                lds.Constant = false;
                lds.Declarations = new LocalDeclarationList(1);
                lds.InitOnly = false;
                lds.Type = new ArrayTypeExpression(ate_res.ElementType, ate_right.Rank);
                LocalDeclaration ld = new LocalDeclaration();
                ld.Name = Identifier.For(arr_name);
                ld.InitialValue = new Literal(null, SystemTypes.Object, sourceContext);
                lds.Declarations.Add(ld);
                method.Body.Statements.Add(lds);
                #endregion

                #region //Double[(,)] __math_aux_left_part;
                string arr_left_part_name = "__math_aux_left_part";
                LocalDeclarationsStatement lds2 = new LocalDeclarationsStatement();
                lds2.Constant = false;
                lds2.Declarations = new LocalDeclarationList(1);
                lds2.InitOnly = false;
                lds2.Type = new ArrayTypeExpression(ate_left.ElementType, ate_left.Rank);
                LocalDeclaration ld2 = new LocalDeclaration();
                ld2.Name = Identifier.For(arr_left_part_name);
                ld2.InitialValue = new Literal(null, SystemTypes.Object, sourceContext);
                lds2.Declarations.Add(ld2);
                method.Body.Statements.Add(lds2);
                #endregion

                #region //__math_aux_matrix = DeepCopy(__math_aux_matrix, right_operand, indices);
                MethodCall mathCall1 = new MethodCall();
                mathCall1.Operands = new ExpressionList();
                mathCall1.Operands.Add(Identifier.For(arr_name));
                if (rightIndices != null)
                {
                    mathCall1.Operands.Add(Identifier.For("right"));
                    int n_s = 0;
                    int n_r = 0;
                    for (int i = 0; i < rightIndices.Length; i++)
                    {
                        if ((rightIndices[i].type is INTEGER_TYPE) || (rightIndices[i].type is CARDINAL_TYPE))
                        {
                            mathCall1.Operands.Add(Identifier.For("right_n_s" + n_s.ToString()));
                            n_s++;
                        }
                        else if (rightIndices[i].type is RANGE_TYPE)
                        {
                            mathCall1.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_from"));
                            mathCall1.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_wasToWritten"));
                            mathCall1.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_to"));
                            mathCall1.Operands.Add(Identifier.For("right_n_r" + n_r.ToString() + "_by"));
                            n_r++;
                        }
                        else if (rightIndices[i].type is ARRAY_TYPE)
                        {
                            mathCall1.Operands.Add(Identifier.For("right_n_r" + n_r.ToString()));
                            n_r++;
                        }
                    }

                    mathCall1.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_right.Rank,
                        rightDim,
                        ate_res.ElementType,
                        rightType,
                        rightIndices,
                        NodeType.Nop, null,
                        sourceContext));
                }
                else
                {
                    mathCall1.Operands.Add(Identifier.For("right"));
                    mathCall1.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_right.Rank,
                        rightDim,
                        ate_res.ElementType,
                        rightType,
                        null,
                        NodeType.Nop, null,
                        sourceContext));
                }

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall1,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_left_part = DeepCopy(__math_aux_left_part, left_operand, indices);
                MethodCall mathCall11 = new MethodCall();
                mathCall11.Operands = new ExpressionList();
                mathCall11.Operands.Add(Identifier.For(arr_left_part_name));
                if (leftIndices != null)
                {
                    mathCall11.Operands.Add(Identifier.For("left"));
                    int n_s = 0;
                    int n_r = 0;
                    for (int i = 0; i < leftIndices.Length; i++)
                    {
                        if ((leftIndices[i].type is INTEGER_TYPE) || (leftIndices[i].type is CARDINAL_TYPE))
                        {
                            mathCall11.Operands.Add(Identifier.For("left_n_s" + n_s.ToString()));
                            n_s++;
                        }
                        else if (leftIndices[i].type is RANGE_TYPE)
                        {
                            mathCall11.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_from"));
                            mathCall11.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_wasToWritten"));
                            mathCall11.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_to"));
                            mathCall11.Operands.Add(Identifier.For("left_n_r" + n_r.ToString() + "_by"));
                            n_r++;
                        }
                        else if (leftIndices[i].type is ARRAY_TYPE)
                        {
                            mathCall11.Operands.Add(Identifier.For("left_n_r" + n_r.ToString()));
                            n_r++;
                        }
                    }

                    mathCall11.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_left.Rank,
                        leftDim,
                        ate_left.ElementType,
                        leftType,
                        leftIndices,
                        NodeType.Nop, null,
                        sourceContext));
                }
                else
                {
                    mathCall11.Operands.Add(Identifier.For("left"));
                    mathCall11.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetDeepCopyWithMemoryAlloc(
                        ate_left.Rank,
                        leftDim,
                        ate_left.ElementType,
                        leftType,
                        null,
                        NodeType.Nop, null,
                        sourceContext));
                }

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_left_part_name),
                    mathCall11,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_matrix = ChangeLinePosition(__math_aux_matrix, __math_aux_left_part, 0, -1, -1)
                MethodCall mathCall22 = new MethodCall();
                mathCall22.Operands = new ExpressionList();
                mathCall22.Operands.Add(Identifier.For(arr_name));
                mathCall22.Operands.Add(Identifier.For(arr_left_part_name));
                mathCall22.Operands.Add(new Literal(0, SystemTypes.Int32));
                mathCall22.Operands.Add(new Literal(-1, SystemTypes.Int32));
                mathCall22.Operands.Add(new Literal(-1, SystemTypes.Int32));
                mathCall22.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetChangeLinePos(
                        ate_res.ElementType,
                        ate_left.ElementType,
                        ate_left.Rank,
                        true,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall22,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //__math_aux_matrix = LUDecomposition(__math_aux_matrix)
                MethodCall mathCall2 = new MethodCall();
                mathCall2.Operands = new ExpressionList();
                mathCall2.Operands.Add(Identifier.For(arr_name));
                mathCall2.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetLUDecomposition(
                        ate_res.ElementType,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_name),
                    mathCall2,
                    NodeType.Nop,
                    sourceContext));
                #endregion

                #region //Double[(,)] __math_slu_res;
                string arr_res_name = "__math_slu_res";
                LocalDeclarationsStatement lds1 = new LocalDeclarationsStatement();
                lds1.Constant = false;
                lds1.Declarations = new LocalDeclarationList(1);
                lds1.InitOnly = false;
                lds1.Type = new ArrayTypeExpression(ate_res.ElementType, ate_res.Rank);
                LocalDeclaration ld1 = new LocalDeclaration();
                ld1.Name = Identifier.For(arr_res_name);
                ConstructArray new_array = new ConstructArray();
                new_array.ElementType = ate_res.ElementType;
                new_array.Operands = new ExpressionList();
                new_array.Rank = ate_res.Rank;
                new_array.Operands.Add(
                    new MethodCall(
                        new MemberBinding(
                            Identifier.For(arr_left_part_name),
                            SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call, SystemTypes.Int32));
                if (ate_res.Rank > 1)
                {
                    new_array.Operands.Add(
                        new MethodCall(
                            new MemberBinding(
                                Identifier.For(arr_left_part_name),
                                SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0]),
                                new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                                NodeType.Call, SystemTypes.Int32));
                }
                ld1.InitialValue = new_array;
                lds1.Declarations.Add(ld1);
                method.Body.Statements.Add(lds1);
                #endregion

                #region //solving
                MethodCall mathCall3 = new MethodCall();
                mathCall3.Operands = new ExpressionList();
                mathCall3.Operands.Add(Identifier.For(arr_name));
                mathCall3.Operands.Add(Identifier.For(arr_left_part_name));
                mathCall3.Operands.Add(Identifier.For(arr_res_name));
                mathCall3.Callee = new MemberBinding(null,
                    CONTEXT.globalMath.GetRightLUDivision(
                        ate_res.ElementType,
                        ate_left.ElementType,
                        ate_res.ElementType,
                        ate_res.Rank,
                        sourceContext));

                method.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For(arr_res_name),
                    mathCall3,
                    NodeType.Nop,
                    sourceContext));
                #endregion
                ////*********/////

                method.ReturnType = new ArrayTypeExpression(ate_res.ElementType, ate_res.Rank);

                Return ret = new Return();
                ret.Expression = Identifier.For("__math_slu_res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseSparseOp
        /// <summary>
        /// Returns appropriate function for an unary sparse array standard operation (+, -);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Array rank (dimensions length)</param>
        /// <param name="leftType">Array base type</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Neg)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseSparseOp(int leftDim, TypeNode leftType,
            TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert((leftDim == 1) || (leftDim == 2));
            
            Identifier name = Identifier.Empty;

            string cur_name = "ElementWise";
            cur_name += "Sparse";

            if (opType == NodeType.Neg)
            {
                name = Identifier.For(cur_name + "Negative" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if (opType == NodeType.UnaryPlus)
            {
                name = Identifier.For(cur_name + "UnPlus" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if ((opType == 0) && (ovlOp is TernaryExpression)) //ABS
            {
                name = Identifier.For(cur_name + "ABS" + leftDim.ToString() + "d" + leftType.Name);
            }
            else if (opType == NodeType.MethodCall)
            {
                name = Identifier.For(cur_name + "_" + ovlOp.ToString() + "_" + leftDim.ToString() + "d" + leftType.Name);
            }

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                //static SparseMatrix<int> ElementWiseSparseNegative2Int32(SparseMatrix<int> A)
                //{
                //    int i = 0, j = 0;
                //    SparseMatrix<int> Res = new SparseMatrix<int>(3, 3, A.M, A.N);
                //    RowSPA<int> rowSPA;
                //    for (i = 0; i < A.M; i++)
                //    {
                //        rowSPA = new RowSPA<int>(A, i);
                //        for (j = 0; j < rowSPA.RealLength; j++)
                //            rowSPA.Data[j] = -rowSPA.Data[j];
                //        Res.AssignSPAToRow(i, rowSPA);
                //    }
                //    return Res;
                //}

                //-----
                //static SparseVector<int> ElementWiseSparseNegative1Int32(SparseVector<int> V)
                //{
                //    int j = 0;
                //    SparseVector<int> Res = new SparseVector<int>(-1, -1, V);
                //    for (j = 0; j < V.AN.Count; j++)
                //        Res.AN[j] = -V.AN[j];
                //    return Res;
                //}

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                Module module = CONTEXT.symbolTable;

                TypeNode leftArrayType;
                if (leftDim == 2)
                    leftArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, leftType);
                else // (leftDim == 1)
                    leftArrayType = STANDARD.SparseVector.GetTemplateInstance(module, leftType);

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));

                int returnDim = leftDim;

                LocalDeclarationsStatement ldsIJ = new LocalDeclarationsStatement();
                ldsIJ.Constant = false;
                ldsIJ.Declarations = new LocalDeclarationList();
                ldsIJ.InitOnly = false;
                ldsIJ.Type = SystemTypes.Int32;

                Construct intConstruct = new Construct(
                    new MemberBinding(null, SystemTypes.Int32),
                    new ExpressionList(), SystemTypes.Int32, sourceContext);

                if (leftDim == 2)
                    ldsIJ.Declarations.Add(new LocalDeclaration(Identifier.For("i"), intConstruct));
                
                ldsIJ.Declarations.Add(new LocalDeclaration(Identifier.For("j"), intConstruct));

                method.Body.Statements.Add(ldsIJ);

                TypeNode returnArrayType = null;
                Construct sparseConstructor = new Construct();
                sparseConstructor.SourceContext = sourceContext;
                sparseConstructor.Operands = new ExpressionList();

                if (returnDim == 2)
                {
                    returnArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, returnType);
                    sparseConstructor.Constructor = new MemberBinding(null, 
                        returnArrayType.GetConstructor(
                        SystemTypes.Int64, SystemTypes.Int32, SystemTypes.Int32, SystemTypes.Int32));

                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartLine, SystemTypes.Int64));
                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartColumn, SystemTypes.Int32));
                    sparseConstructor.Operands.Add(new QualifiedIdentifier(
                        Identifier.For("left"), Identifier.For("M")));
                    sparseConstructor.Operands.Add(new QualifiedIdentifier(
                        Identifier.For("left"), Identifier.For("N")));
                }
                else // (returnDim == 1)
                {
                    returnArrayType = STANDARD.SparseVector.GetTemplateInstance(module, returnType);

                    sparseConstructor.Constructor = new MemberBinding(null,
                        returnArrayType.GetConstructor(SystemTypes.Int64, SystemTypes.Int32, returnArrayType));

                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartLine, SystemTypes.Int64));
                    sparseConstructor.Operands.Add(new Literal(
                        sourceContext.StartColumn, SystemTypes.Int32));
                    sparseConstructor.Operands.Add(Identifier.For("left"));
                }

                LocalDeclarationsStatement ldsRes = new LocalDeclarationsStatement();
                ldsRes.Constant = false;
                ldsRes.InitOnly = false;
                ldsRes.Type = returnArrayType;
                ldsRes.Declarations = new LocalDeclarationList(1);
                ldsRes.Declarations.Add(new LocalDeclaration(Identifier.For("res"), sparseConstructor));
                method.Body.Statements.Add(ldsRes);

                TypeNode rowSPAType = null;
                if (leftDim == 2) //generate RowSPA
                {
                    rowSPAType = STANDARD.RowSPA.GetTemplateInstance(module, leftType);
                    LocalDeclarationsStatement ldsRowSPA = new LocalDeclarationsStatement();
                    ldsRowSPA.Constant = false;
                    ldsRowSPA.Declarations = new LocalDeclarationList(1);
                    ldsRowSPA.InitOnly = false;
                    ldsRowSPA.Type = rowSPAType;
 
                    ldsRowSPA.Declarations.Add(new LocalDeclaration(Identifier.For("rowSPA"), new Literal(null)));
                    method.Body.Statements.Add(ldsRowSPA);
                }

                StatementList forStatements = new StatementList();
                For forStm1 = null;
                Expression leftOperand = null, rightOperand = null;
                AssignmentStatement asgnFor = new AssignmentStatement();

                if (leftDim == 1)
                {
                    //for (j = 0; j < V.AN.Count; j++)
                    forStm1 = new For();
                    forStm1.Initializer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                            Identifier.For("j"), new Literal(0, SystemTypes.Int32),
                            NodeType.Nop, sourceContext)});
                    forStm1.Condition = new BinaryExpression(
                        Identifier.For("j"),
                        new QualifiedIdentifier(
                            new QualifiedIdentifier(Identifier.For("left"), Identifier.For("AN"), sourceContext),
                            Identifier.For("Count"), sourceContext),
                        NodeType.Lt, sourceContext);
                    forStm1.Incrementer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                                Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)});
                        //new ExpressionStatement( 
                        //    new PostfixExpression(Identifier.For("j"), NodeType.Increment, sourceContext)) });

                    forStm1.SourceContext = sourceContext;
                    forStm1.Body = new Block(new StatementList(), sourceContext);
                    forStm1.Body.Statements.Add(asgnFor);

                    rightOperand = new Indexer(
                        new QualifiedIdentifier(Identifier.For("left"), Identifier.For("AN"), sourceContext),
                        new ExpressionList(new Expression[] {Identifier.For("j")}),
                        leftType, sourceContext);

                    leftOperand = new Indexer(
                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AN"), sourceContext),
                        new ExpressionList(new Expression[] {Identifier.For("j")}),
                        returnType, sourceContext);
                }
                else //leftDim == 2
                {
                    For forStm2 = new For();

                    //for (i = 0; i < A.M; i++)
                    forStm1 = new For();
                    forStm1.Initializer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                            Identifier.For("i"), new Literal(0, SystemTypes.Int32),
                            NodeType.Nop, sourceContext)});
                    forStm1.Condition = new BinaryExpression(
                        Identifier.For("i"),
                        new QualifiedIdentifier(Identifier.For("left"), Identifier.For("M"), sourceContext),
                        NodeType.Lt, sourceContext);
                    forStm1.Incrementer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                                Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)});
                        //new ExpressionStatement( 
                        //    new PostfixExpression(Identifier.For("i"), NodeType.Increment, sourceContext)) });

                    forStm1.SourceContext = sourceContext;
                    forStm1.Body = new Block(new StatementList(), sourceContext);
                                       
                    //rowSPA = new RowSPA<int>(A, i);
                    forStm1.Body.Statements.Add(
                        new AssignmentStatement(
                            Identifier.For("rowSPA"),
                            new Construct(
                                new MemberBinding(null, rowSPAType.GetConstructors()[0]),
                                new ExpressionList(new Expression[] {
                                    Identifier.For("left"), Identifier.For("i")}),
                                sourceContext),
                            NodeType.Nop,
                            sourceContext));

                    forStm1.Body.Statements.Add(forStm2);

                    //Res.AssignSPAToRow(i, rowSPA);
                    forStm1.Body.Statements.Add(new ExpressionStatement(
                        new MethodCall(
                            new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AssignSPAToRow"), sourceContext),
                            new ExpressionList(new Expression[] {
                                Identifier.For("i"), Identifier.For("rowSPA")}),
                            NodeType.Call, 
                            SystemTypes.Void,
                            sourceContext),
                        sourceContext));

                    //for (j = 0; j < rowSPA.RealLength; j++)
                    forStm2.Initializer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                            Identifier.For("j"), new Literal(0, SystemTypes.Int32),
                            NodeType.Nop, sourceContext)});
                    forStm2.Condition = new BinaryExpression(
                        Identifier.For("j"),
                        new QualifiedIdentifier(Identifier.For("rowSPA"), Identifier.For("RealLength"), sourceContext),
                        NodeType.Lt, sourceContext);
                    forStm2.Incrementer = new StatementList(new Statement[] {
                        new AssignmentStatement(
                                Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)});
                        //new ExpressionStatement(
                        //    new PostfixExpression(Identifier.For("j"), NodeType.Increment, sourceContext)) });

                    forStm2.SourceContext = sourceContext;
                    forStm2.Body = new Block(new StatementList(), sourceContext);
                    forStm2.Body.Statements.Add(asgnFor);

                    rightOperand = new Indexer(
                        new QualifiedIdentifier(Identifier.For("rowSPA"), Identifier.For("Data"), sourceContext),
                        new ExpressionList(new Expression[] {Identifier.For("j")}),
                        leftType, sourceContext);

                    leftOperand = new Indexer(
                        new QualifiedIdentifier(Identifier.For("rowSPA"), Identifier.For("Data"), sourceContext),
                        new ExpressionList(new Expression[] {Identifier.For("j")}),
                        returnType, sourceContext);
                }
 
                asgnFor.Operator = NodeType.Nop;

                if (opType != 0)
                {
                    if (opType != NodeType.MethodCall)
                    {
                        UnaryExpression internalOp = new UnaryExpression();
                        internalOp.NodeType = opType;
                        internalOp.Operand = rightOperand;
                        asgnFor.Source = internalOp;

                        //type conversion (int16 + int16 = int32 in .net, but int16 in Zonnon)
                        //it doesn't work implicitly on Mono, but does on Windows
                        if ((returnType == SystemTypes.Int8) || (returnType == SystemTypes.Int16)
                                || (returnType == SystemTypes.UInt8 || (returnType == SystemTypes.UInt16)))
                        {
                            //We have to convert the result
                            BinaryExpression castop = new BinaryExpression(
                                internalOp,
                                new MemberBinding(null, returnType),
                                NodeType.Castclass,
                                sourceContext);

                            asgnFor.Source = castop;
                        }
                    }
                    else //MethodCall
                    {
                        asgnFor.Source = new MethodCall(
                            ovlOp,
                            new ExpressionList(new Expression[] { rightOperand }),
                            NodeType.Call, returnType);
                    }
                }
                else if (ovlOp is TernaryExpression)
                {
                    VariableDeclaration local = new VariableDeclaration();
                    local.Initializer = rightOperand;
                    local.Name = Identifier.For("Absolute Value");
                    local.Type = leftType;
                    ((For)forStatements[returnDim - 1]).Body.Statements.Add(local);

                    BinaryExpression comp = new BinaryExpression();
                    comp.Operand1 = local.Name;
                    comp.Operand2 = new Literal(0, SystemTypes.Int32);
                    comp.NodeType = NodeType.Lt;
                    comp.SourceContext = sourceContext;

                    TernaryExpression cond = new TernaryExpression();
                    cond.Operand1 = comp;
                    cond.Operand2 = new UnaryExpression(local.Name, NodeType.Neg);
                    cond.Operand3 = local.Name;
                    cond.NodeType = NodeType.Conditional;
                    cond.SourceContext = sourceContext;

                    asgnFor.Source = cond;
                }

                asgnFor.Target = leftOperand;

                method.Body.Statements.Add(forStm1);

                method.ReturnType = returnArrayType;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetElementWiseSparseSparseOp
        /// <summary>
        /// Returns appropriate function for a binary sparse-sparse array standard operation (+, -, .*, ./, mod, div, comparison);
        /// if this function doesn't exist then this function will be created
        /// </summary>
        /// <param name="leftDim">Array rank (dimensions length)</param>
        /// <param name="leftType">Array base type</param>
        /// <param name="returnType">Return array base type (elements type)</param>
        /// <param name="opType">Type of operator (like NodeType.Neg)</param>
        /// <param name="ovlOp">If the operator was overloaded, the appropriate operator</param>
        /// <param name="sourceContext">Current source context</param>
        /// <returns></returns>
        public Member GetElementWiseSparseSparseOp(int leftDim, int rightDim, TypeNode leftType, TypeNode rightType,
            TypeNode returnType,
            NodeType opType, Expression ovlOp, SourceContext sourceContext)
        {
            Debug.Assert((leftDim == 1) || (leftDim == 2));
            Debug.Assert((rightDim == 1) || (rightDim == 2));
            Debug.Assert(leftDim == rightDim);

            bool isComparison = false;
            Identifier name = Identifier.Empty;

            string cur_name = "ElementWise";
            cur_name += "SparseSparse";

            if (opType == NodeType.Add || opType == NodeType.Add_Ovf || opType == NodeType.Add_Ovf_Un)
            {
                cur_name += "Plus" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            }
            else if (opType == NodeType.Sub || opType == NodeType.Sub_Ovf || opType == NodeType.Sub_Ovf_Un)
            {
                cur_name += "Minus" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            }
            else if (opType == NodeType.Mul || opType == NodeType.Mul_Ovf || opType == NodeType.Mul_Ovf_Un)
            {
                cur_name += "Mult" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            }
            else if (opType == NodeType.Rem || opType == NodeType.Rem_Un)
            {
                cur_name += "Mod" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            }
            else if (opType == NodeType.Div || opType == NodeType.Div_Un)
            {
                cur_name += "Div" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
            }
            else if (opType == NodeType.Eq)
            {
                cur_name += "Equal" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            else if (opType == NodeType.Ne)
            {
                cur_name += "NonEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            else if (opType == NodeType.Lt)
            {
                cur_name += "Less" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            else if (opType == NodeType.Le)
            {
                cur_name += "LessEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            else if (opType == NodeType.Gt)
            {
                cur_name += "Greater" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            else if (opType == NodeType.Ge)
            {
                cur_name += "GreaterEqual" + rightDim.ToString() + "d" + leftType.Name + rightType.Name;
                isComparison = true;
            }
            //else if (opType == NodeType.MethodCall)
            //{
            //    cur_name += "_" + ovlOp.ToString() + "_" + rightDim.ToString() + "d" + leftType.Name + rightType.Name);
            //}
            name = Identifier.For(cur_name);

            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function

                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                method.Parameters = new ParameterList();

                Module module = CONTEXT.symbolTable;

                TypeNode leftArrayType, rightArrayType;
                if (leftDim == 2)
                {
                    leftArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, leftType);
                    rightArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, rightType);
                }
                else // (leftDim == 1)
                {
                    leftArrayType = STANDARD.SparseVector.GetTemplateInstance(module, leftType);
                    rightArrayType = STANDARD.SparseVector.GetTemplateInstance(module, rightType);
                }

                method.Parameters.Add(new Parameter(Identifier.For("left"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("right"), rightArrayType));

                int returnDim = leftDim;

                LocalDeclarationsStatement ldsIJ = new LocalDeclarationsStatement();
                ldsIJ.Constant = false;
                ldsIJ.Declarations = new LocalDeclarationList();
                ldsIJ.InitOnly = false;
                ldsIJ.Type = SystemTypes.Int32;

                Construct intConstruct = new Construct(
                    new MemberBinding(null, SystemTypes.Int32),
                    new ExpressionList(), SystemTypes.Int32, sourceContext);

                ldsIJ.Declarations.Add(new LocalDeclaration(Identifier.For("i"), intConstruct));
                ldsIJ.Declarations.Add(new LocalDeclaration(Identifier.For("j"), intConstruct));

                method.Body.Statements.Add(ldsIJ);

                method.Body.Statements.Add(GenerateIfStatementWithThrow(
                    new QualifiedIdentifier(
                        Identifier.For("left"), Identifier.For("N")),
                    new QualifiedIdentifier(
                        Identifier.For("right"), Identifier.For("N")),
                    NodeType.Ne,
                    STANDARD.IncompatibleSizesException,
                    sourceContext.StartLine, sourceContext.StartColumn,
                    sourceContext));

                if (returnDim == 2)
                {
                    method.Body.Statements.Add(GenerateIfStatementWithThrow(
                        new QualifiedIdentifier(
                            Identifier.For("left"), Identifier.For("M")),
                        new QualifiedIdentifier(
                            Identifier.For("right"), Identifier.For("M")),
                        NodeType.Ne,
                        STANDARD.IncompatibleSizesException,
                        sourceContext.StartLine, sourceContext.StartColumn,
                        sourceContext));
                }

                TypeNode returnArrayType = null;
                ArrayTypeExpression returnArrayTypeExpr = null;

                if (!isComparison)
                {
                    Construct sparseConstructor = new Construct();
                    sparseConstructor.SourceContext = sourceContext;
                    sparseConstructor.Operands = new ExpressionList();

                    //sparse array should be of equal size
                    if (returnDim == 2)
                    {
                        returnArrayType = STANDARD.SparseMatrix.GetTemplateInstance(module, returnType);
                        sparseConstructor.Constructor = new MemberBinding(null,
                            returnArrayType.GetConstructor(
                            SystemTypes.Int64, SystemTypes.Int32, SystemTypes.Int32, SystemTypes.Int32));

                        sparseConstructor.Operands.Add(new Literal(
                            sourceContext.StartLine, SystemTypes.Int64));
                        sparseConstructor.Operands.Add(new Literal(
                            sourceContext.StartColumn, SystemTypes.Int32));
                        sparseConstructor.Operands.Add(new QualifiedIdentifier(
                            Identifier.For("left"), Identifier.For("M")));
                        sparseConstructor.Operands.Add(new QualifiedIdentifier(
                            Identifier.For("left"), Identifier.For("N")));
                    }
                    else // (returnDim == 1)
                    {
                        returnArrayType = STANDARD.SparseVector.GetTemplateInstance(module, returnType);

                        sparseConstructor.Constructor = new MemberBinding(null,
                            returnArrayType.GetConstructor(SystemTypes.Int64, SystemTypes.Int32, SystemTypes.Int32));

                        sparseConstructor.Operands.Add(new Literal(
                            sourceContext.StartLine, SystemTypes.Int64));
                        sparseConstructor.Operands.Add(new Literal(
                            sourceContext.StartColumn, SystemTypes.Int32));
                        sparseConstructor.Operands.Add(new QualifiedIdentifier(
                            Identifier.For("left"), Identifier.For("N")));
                    }

                    LocalDeclarationsStatement ldsRes = new LocalDeclarationsStatement();
                    ldsRes.Constant = false;
                    ldsRes.InitOnly = false;
                    ldsRes.Type = returnArrayType;
                    ldsRes.Declarations = new LocalDeclarationList(1);
                    ldsRes.Declarations.Add(new LocalDeclaration(Identifier.For("res"), sparseConstructor));
                    method.Body.Statements.Add(ldsRes);
                }
                else //IsComparison, so, the returnArrayType is a dense array, not a sparse one
                {
                    returnArrayTypeExpr = new ArrayTypeExpression();
                    returnArrayTypeExpr.ElementType = returnType;
                    returnArrayTypeExpr.Rank = returnDim;
                    if (leftDim == 1)
                    {
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayTypeExpr,
                            new ExpressionList(new Expression[] { 
                                new QualifiedIdentifier(Identifier.For("left"), Identifier.For("N"))}), 
                        sourceContext));
                    }
                    else
                    {
                        method.Body.Statements.Add(GenerateLocalResArray(returnArrayTypeExpr,
                            new ExpressionList(new Expression[] { 
                                new QualifiedIdentifier(Identifier.For("left"), Identifier.For("M")),
                                new QualifiedIdentifier(Identifier.For("left"), Identifier.For("N"))}),
                        sourceContext));
                    }
                }

                //if we divide, then all the elements in the "right" should be non zeros :)
                if ((opType == NodeType.Div) || (opType == NodeType.Div_Un)
                    || (opType == NodeType.Rem) || (opType == NodeType.Rem_Un))
                {
                    Expression numberOfElements = null;
                    if (leftDim == 1)
                        numberOfElements = new QualifiedIdentifier(Identifier.For("right"), Identifier.For("N"), sourceContext);
                    else //leftDim == 2
                        numberOfElements = new BinaryExpression(
                            new QualifiedIdentifier(Identifier.For("right"), Identifier.For("M"), sourceContext),
                            new QualifiedIdentifier(Identifier.For("right"), Identifier.For("N"), sourceContext),
                            NodeType.Mul_Ovf,
                            sourceContext);

                    method.Body.Statements.Add(GenerateIfStatementWithThrow(
                    new QualifiedIdentifier(
                        new QualifiedIdentifier(Identifier.For("right"), Identifier.For("AN"), sourceContext),
                        Identifier.For("Count")),
                    numberOfElements,
                    NodeType.Ne,
                    STANDARD.ZeroDivisionException, 
                    sourceContext.StartLine, sourceContext.StartColumn,
                    sourceContext));
                }

                TypeNode leftSPAType, rightSPAType;
                if (leftDim == 1)
                {
                    leftSPAType = STANDARD.RowVectorSPA.GetTemplateInstance(module, leftType);
                    rightSPAType = STANDARD.RowVectorSPA.GetTemplateInstance(module, rightType);
                }
                else
                {
                    leftSPAType = STANDARD.RowSPA.GetTemplateInstance(module, leftType);
                    rightSPAType = STANDARD.RowSPA.GetTemplateInstance(module, rightType);
                }
                method.Body.Statements.Add(
                    new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("leftSPA"), new Literal(null)),
                        leftSPAType));
                method.Body.Statements.Add(
                    new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("rightSPA"), new Literal(null)),
                        rightSPAType));

                For forStm1 = new For();
                For forStm2 = new For();

                //for (i = 0; i < A.M; i++)
                forStm1.Initializer = new StatementList(new Statement[] {
                    new AssignmentStatement(
                        Identifier.For("i"), new Literal(0, SystemTypes.Int32),
                        NodeType.Nop, sourceContext)});
                if (leftDim == 2)
                    forStm1.Condition = new BinaryExpression(
                        Identifier.For("i"),
                        new QualifiedIdentifier(Identifier.For("left"), Identifier.For("M"), sourceContext),
                        NodeType.Lt, sourceContext);
                else //leftDim == 1
                    forStm1.Condition = new BinaryExpression(
                        Identifier.For("i"),
                        new Literal(1, SystemTypes.Int32, sourceContext),
                        NodeType.Lt, sourceContext);
                forStm1.Incrementer = new StatementList(new Statement[] {
                    new AssignmentStatement(
                            Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)});

                forStm1.SourceContext = sourceContext;
                forStm1.Body = new Block(new StatementList(), sourceContext);

                //rowSPA = new RowSPA<int>(A, i);
                forStm1.Body.Statements.Add(
                    new AssignmentStatement(
                        Identifier.For("leftSPA"),
                        new Construct(
                            new MemberBinding(null, leftSPAType.GetConstructors()[0]),
                            new ExpressionList(new Expression[] {
                                Identifier.For("left"), Identifier.For("i")}),
                            sourceContext),
                        NodeType.Nop,
                        sourceContext));
                forStm1.Body.Statements.Add(
                    new AssignmentStatement(
                        Identifier.For("rightSPA"),
                        new Construct(
                            new MemberBinding(null, rightSPAType.GetConstructors()[0]),
                            new ExpressionList(new Expression[] {
                                Identifier.For("right"), Identifier.For("i")}),
                            sourceContext),
                        NodeType.Nop,
                        sourceContext));

                if (leftDim == 2)
                    forStm1.Body.Statements.Add(new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("count"), intConstruct, NodeType.Nop),
                        SystemTypes.Int32));

                forStm1.Body.Statements.Add(forStm2);

                //for (j = 0; j < leftRowSPA.Length; j++)
                forStm2.Initializer = new StatementList(new Statement[] {
                    new AssignmentStatement(
                        Identifier.For("j"), new Literal(0, SystemTypes.Int32),
                        NodeType.Nop, sourceContext)});
                forStm2.Condition = new BinaryExpression(
                    Identifier.For("j"),
                    new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Length"), sourceContext),
                    NodeType.Lt, sourceContext);
                forStm2.Incrementer = new StatementList(new Statement[] {
                    new AssignmentStatement(
                            Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)});

                forStm2.SourceContext = sourceContext;
                forStm2.Body = new Block(new StatementList(), sourceContext);

                forStm2.Body.Statements.Add(new LocalDeclarationsStatement(
                            new LocalDeclaration(
                                Identifier.For("leftIndex"), 
                                new MethodCall(
                                    new MemberBinding(Identifier.For("leftSPA"),
                                        STANDARD.SPA.GetTemplateInstance(module, leftType).GetMembersNamed(Identifier.For("IndexOfElemInIndices"))[0]),
                                    new ExpressionList(new Expression[] {
                                        Identifier.For("j")}),
                                    NodeType.Call),
                                NodeType.Nop),
                        SystemTypes.Int32));

                forStm2.Body.Statements.Add(new LocalDeclarationsStatement(
                            new LocalDeclaration(
                                Identifier.For("rightIndex"),
                                new MethodCall(
                                    new MemberBinding(Identifier.For("rightSPA"),
                                        STANDARD.SPA.GetTemplateInstance(module, rightType).GetMembersNamed(Identifier.For("IndexOfElemInIndices"))[0]),
                                    new ExpressionList(new Expression[] {
                                        Identifier.For("j")}),
                                    NodeType.Call),
                                NodeType.Nop),
                        SystemTypes.Int32));

                //if (leftRowSPA.Flags[j] && rightRowSPA.Flags[j])
                //{
                //    Res.AN.Add(leftRowSPA.Data[j] + rightRowSPA.Data[j]);
                //    Res.JA.Add(j);
                //    count++;
                //}
                If if1 = new If(
                    new BinaryExpression(
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Flags"), sourceContext),
                            new ExpressionList(new Expression[] {Identifier.For("j")}),
                            sourceContext),
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Flags"), sourceContext),
                            new ExpressionList(new Expression[] {Identifier.For("j")}),
                            sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList(), sourceContext),
                    new Block(new StatementList(), sourceContext));

                if (!isComparison)
                {
                    if1.TrueBlock.Statements.Add(
                        new LocalDeclarationsStatement(new LocalDeclaration(
                            Identifier.For("temp"),
                            new BinaryExpression(
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Data"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("leftIndex") }),
                                    sourceContext),
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Data"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("rightIndex") }),
                                    sourceContext),
                                opType, sourceContext),
                            NodeType.Nop),
                        returnType));

                    Statement if1stm1 =
                        new ExpressionStatement(
                            new MethodCall(
                                new QualifiedIdentifier(
                                    new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AN"), sourceContext),
                                    Identifier.For("Add"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("temp") }),
                                NodeType.Call), sourceContext);

                    Statement if1stm2 =
                        new ExpressionStatement(
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("JA"), sourceContext),
                                        Identifier.For("Add"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("j") }),
                                    NodeType.Call), sourceContext);

                    Statement if1stm3 = null;
                    if (leftDim == 2)
                        if1stm3 =
                            new AssignmentStatement(Identifier.For("count"), new Literal(1, SystemTypes.Int32), NodeType.Add);

                    if ((opType == NodeType.Add) || (opType == NodeType.Add_Ovf) || (opType == NodeType.Add_Ovf_Un)
                        || (opType == NodeType.Sub) || (opType == NodeType.Sub_Ovf) || (opType == NodeType.Sub_Ovf_Un))
                    {
                        //we need to check whether the result is not zero
                        StatementList stlIntIf = new StatementList();
                        stlIntIf.Add(if1stm1);
                        stlIntIf.Add(if1stm2);
                        if (leftDim == 2) stlIntIf.Add(if1stm3);
                        If ifInt = new If(
                            new BinaryExpression(
                                Identifier.For("temp"), new Literal(0, returnType), NodeType.Ne),
                            new Block(stlIntIf, sourceContext),
                            new Block(new StatementList(), sourceContext));

                        if1.TrueBlock.Statements.Add(ifInt);
                    }
                    else
                    {
                        if1.TrueBlock.Statements.Add(if1stm1);
                        if1.TrueBlock.Statements.Add(if1stm2);
                        if (leftDim == 2)
                            if1.TrueBlock.Statements.Add(if1stm3);
                    }
                }
                else //it's comparison
                {
                    ExpressionList exprListInt = new ExpressionList();
                    if (leftDim == 2) exprListInt.Add(Identifier.For("i"));
                    exprListInt.Add(Identifier.For("j"));

                    if1.TrueBlock.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("res"),
                            exprListInt,
                            sourceContext),
                        new BinaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Data"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("leftIndex") }),
                                sourceContext),
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Data"), sourceContext),
                                new ExpressionList(new Expression[] { Identifier.For("rightIndex") }),
                                sourceContext),
                            opType, sourceContext),
                        NodeType.Nop,
                        sourceContext));
                }

                forStm2.Body.Statements.Add(if1);

                if ((opType == NodeType.Add) || (opType == NodeType.Add_Ovf) || (opType == NodeType.Add_Ovf_Un)
                    || (opType == NodeType.Sub) || (opType == NodeType.Sub_Ovf) || (opType == NodeType.Sub_Ovf_Un)
                    || (isComparison))
                {
                    //if (leftRowSPA.Flags[j] && !rightRowSPA.Flags[j])
                    //{
                    //    Res.AN.Add(leftRowSPA.Data[j]);
                    //    Res.JA.Add(j);
                    //    count++;
                    //}
                    If if2 = new If(
                    new BinaryExpression(
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Flags"), sourceContext),
                            new ExpressionList(new Expression[] {Identifier.For("j")}),
                            sourceContext),
                        new UnaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Flags"), sourceContext),
                                new ExpressionList(new Expression[] {Identifier.For("j")}),
                                sourceContext),
                            NodeType.LogicalNot,
                            sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList(), sourceContext),
                    new Block(new StatementList(), sourceContext));

                    if (!isComparison)
                    {
                        if2.TrueBlock.Statements.Add(
                            new ExpressionStatement(
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AN"), sourceContext),
                                        Identifier.For("Add"), sourceContext),
                                    new ExpressionList(new Expression[] {
                                    new Indexer(
                                        new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Data"), sourceContext),
                                        new ExpressionList(new Expression[] {Identifier.For("leftIndex")}),
                                        sourceContext)}),
                                    NodeType.Call), sourceContext));
                        if2.TrueBlock.Statements.Add(
                            new ExpressionStatement(
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("JA"), sourceContext),
                                        Identifier.For("Add"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("j") }),
                                    NodeType.Call), sourceContext));
                        if (leftDim == 2)
                            if2.TrueBlock.Statements.Add(
                                new AssignmentStatement(Identifier.For("count"), new Literal(1, SystemTypes.Int32), NodeType.Add));
                    }
                    else //it's comparison: compare leftSPA.Data[leftIndex] with zero
                    {
                        ExpressionList exprListInt = new ExpressionList();
                        if (leftDim == 2) exprListInt.Add(Identifier.For("i"));
                        exprListInt.Add(Identifier.For("j"));

                        if2.TrueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("res"),
                                exprListInt,
                                sourceContext),
                            new BinaryExpression(
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Data"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("leftIndex") }),
                                    sourceContext),
                                new Literal(0, returnType, sourceContext),
                                opType, sourceContext),
                            NodeType.Nop,
                            sourceContext));
                    }

                    forStm2.Body.Statements.Add(if2);

                    //if (!leftRowSPA.Flags[j] && rightRowSPA.Flags[j])
                    //{
                    //    Res.AN.Add(rightRowSPA.Data[j]);
                    //    Res.JA.Add(j);
                    //    count++;
                    //}
                    If if3 = new If(
                    new BinaryExpression(
                        new UnaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Flags"), sourceContext),
                                new ExpressionList(new Expression[] {Identifier.For("j")}),
                                sourceContext),
                            NodeType.LogicalNot,
                            sourceContext),
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Flags"), sourceContext),
                            new ExpressionList(new Expression[] {Identifier.For("j")}),
                            sourceContext),
                        NodeType.LogicalAnd, sourceContext),
                    new Block(new StatementList(), sourceContext),
                    new Block(new StatementList(), sourceContext));

                    Expression internalArg = new Indexer(
                        new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Data"), sourceContext),
                        new ExpressionList(new Expression[] {Identifier.For("rightIndex")}),
                        sourceContext);
                    if ((opType == NodeType.Sub) || (opType == NodeType.Sub_Ovf) || (opType == NodeType.Sub_Ovf_Un))
                        internalArg = new UnaryExpression(internalArg, NodeType.Neg, sourceContext);

                    if (!isComparison)
                    {
                        if3.TrueBlock.Statements.Add(
                            new ExpressionStatement(
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("AN"), sourceContext),
                                        Identifier.For("Add"), sourceContext),
                                    new ExpressionList(new Expression[] { internalArg }),
                                    NodeType.Call), sourceContext));
                        if3.TrueBlock.Statements.Add(
                            new ExpressionStatement(
                                new MethodCall(
                                    new QualifiedIdentifier(
                                        new QualifiedIdentifier(Identifier.For("res"), Identifier.For("JA"), sourceContext),
                                        Identifier.For("Add"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("j") }),
                                    NodeType.Call), sourceContext));
                        if (leftDim == 2)
                            if3.TrueBlock.Statements.Add(
                                new AssignmentStatement(Identifier.For("count"), new Literal(1, SystemTypes.Int32), NodeType.Add));
                    }
                    else //it's comparison: compare rightSPA.Data[rightIndex] with zero
                    {
                        ExpressionList exprListInt = new ExpressionList();
                        if (leftDim == 2) exprListInt.Add(Identifier.For("i"));
                        exprListInt.Add(Identifier.For("j"));

                        if3.TrueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("res"),
                                exprListInt,
                                sourceContext),
                            new BinaryExpression(
                                new Literal(0, returnType, sourceContext),
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Data"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("rightIndex") }),
                                    sourceContext),
                                opType, sourceContext),
                            NodeType.Nop,
                            sourceContext));
                    }

                    forStm2.Body.Statements.Add(if3);

                    //if operation is .= , .<= or .>= we have to set corresponding element in Res array as true
                    if ((isComparison) && ((opType == NodeType.Eq) || (opType == NodeType.Le) || (opType == NodeType.Ge)))
                    {
                        //if (!leftRowSPA.Flags[j] && !rightRowSPA.Flags[j])
                        //{...}
                        If if4 = new If(
                        new BinaryExpression(
                            new UnaryExpression(
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("leftSPA"), Identifier.For("Flags"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("j") }),
                                    sourceContext),
                                NodeType.LogicalNot,
                                sourceContext),
                            new UnaryExpression(
                                new Indexer(
                                    new QualifiedIdentifier(Identifier.For("rightSPA"), Identifier.For("Flags"), sourceContext),
                                    new ExpressionList(new Expression[] { Identifier.For("j") }),
                                    sourceContext),
                                NodeType.LogicalNot,
                                sourceContext),
                            NodeType.LogicalAnd, sourceContext),
                        new Block(new StatementList(), sourceContext),
                        new Block(new StatementList(), sourceContext));

                        ExpressionList exprListInt = new ExpressionList();
                        if (leftDim == 2) exprListInt.Add(Identifier.For("i"));
                        exprListInt.Add(Identifier.For("j"));

                        if4.TrueBlock.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("res"),
                                exprListInt,
                                sourceContext),
                            new Literal(true, SystemTypes.Boolean),
                            NodeType.Nop,
                            sourceContext));

                        forStm2.Body.Statements.Add(if4);
                    }
                }
                
                //Res.IA[j + 1] = Res.IA[j] + count;
                if ((leftDim == 2) && (!isComparison))
                    forStm1.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(
                            new QualifiedIdentifier(Identifier.For("res"), Identifier.For("IA"), sourceContext),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add_Ovf)}), 
                            sourceContext),
                        new BinaryExpression(
                            new Indexer(
                                new QualifiedIdentifier(Identifier.For("res"), Identifier.For("IA"), sourceContext),
                                new ExpressionList(new Expression[] {Identifier.For("i")}),
                                sourceContext),
                            Identifier.For("count"),
                            NodeType.Add_Ovf),
                        NodeType.Nop,
                        sourceContext));

                method.Body.Statements.Add(forStm1);

                if (!isComparison)
                    method.ReturnType = returnArrayType;
                else
                    method.ReturnType = returnArrayTypeExpr;

                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);

                mathRoutines.Members.Add(method);
            }
            // ELSE
            // Function for this kind of operation has been already created. So just return it.\

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region Optimized Functions
        #region GetOptimizedMMTransposedPlusConst
        /// <summary>
        /// Returns appropriate function for: A := B * C! + k
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedMMTransposed(TypeNode leftType, TypeNode rightType, 
            TypeNode returnType, TypeNode constType,
            bool isWithConstant,
            SourceContext sourceContext)
        {
            string s = "";
            if (isWithConstant) s = "PlusConst";
            Identifier name = Identifier.For(
                "OptimizedMMTransposed" + s + leftType.Name + rightType.Name + returnType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = 2;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 2;
                
                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));
                if (isWithConstant)
                    method.Parameters.Add(new Parameter(Identifier.For("constanta"), constType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("M"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength), 
                            new ExpressionList(new Expression[] {new Literal(0, SystemTypes.Int32)}),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("N"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength), 
                            new ExpressionList(new Expression[] {new Literal(1, SystemTypes.Int32)}),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("K"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("C"), getLength), 
                            new ExpressionList(new Expression[] {new Literal(1, SystemTypes.Int32)}),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("mb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("nb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("kb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                
                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("minTemp"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                For forj0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j0"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i0"), Identifier.For("K"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i0"), Identifier.For("kb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k0"), Identifier.For("N"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                Member mathMin = STANDARD.systemMath.GetMethod(Identifier.For("Min"), SystemTypes.Int32, SystemTypes.Int32);

                For forj = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("j0"))}),
                    new BinaryExpression(Identifier.For("j"), 
                        new MethodCall(
                            new MemberBinding(null, mathMin),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add),
                                Identifier.For("M")}),
                            NodeType.Call, SystemTypes.Int32),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), Identifier.For("i0"))}),
                    new BinaryExpression(Identifier.For("i"),
                        new MethodCall(
                            new MemberBinding(null, mathMin),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("i0"), Identifier.For("kb"), NodeType.Add),
                                Identifier.For("K")}),
                            NodeType.Call, SystemTypes.Int32),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("k0"))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("border"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork2 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("border"))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("minTemp"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //C[j, i] += A[j, k] * B[i, k];
                fork2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("i") })),
                    new BinaryExpression(
                        new Indexer(Identifier.For("A"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                        new Indexer(Identifier.For("B"),
                            new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("k") })),
                        NodeType.Mul),
                    NodeType.Add));

                //C[j, i] += A[j, k] * B[i, k] + A[j, k + 1] * B[i, k + 1] +
                //             A[j, k + 2] * B[i, k + 2] + A[j, k + 3] * B[i, k + 3] +
                //             A[j, k + 4] * B[i, k + 4];
                BinaryExpression ab0 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("k") })),
                    NodeType.Mul);
                
                BinaryExpression ab1 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab2 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab3 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab4 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), 
                            new BinaryExpression(Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab34 = new BinaryExpression(ab3, ab4, NodeType.Add);
                BinaryExpression ab234 = new BinaryExpression(ab2, ab34, NodeType.Add);
                BinaryExpression ab1234 = new BinaryExpression(ab1, ab234, NodeType.Add);
                BinaryExpression ab01234 = new BinaryExpression(ab0, ab1234, NodeType.Add);

                fork1.Body.Statements.Add(
                    new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("i") })),
                        ab01234,
                        NodeType.Add));

                //minTemp = Math.Min(k0 + nb, N);
                fori.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("minTemp"),
                    new MethodCall(
                        new MemberBinding(null, mathMin),
                        new ExpressionList(new Expression[] {
                            new BinaryExpression(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add),
                            Identifier.For("N")}),
                        NodeType.Call, SystemTypes.Int32),
                    NodeType.Nop));
                
                fori.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(5, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(minTemp / step) * step;
                fori.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("minTemp"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                fori.Body.Statements.Add(fork1);
                fori.Body.Statements.Add(fork2);
                forj.Body.Statements.Add(fori);
                fork0.Body.Statements.Add(forj);
                fori0.Body.Statements.Add(fork0);
                forj0.Body.Statements.Add(fori0);    
                method.Body.Statements.Add(forj0);

                if (isWithConstant)
                {
                    //for (int j = 0; j < M; j++)
                    //    for (int i = 0; i < K; i++)
                    //        C[j, i] += constanta;

                    For forj1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                    For fori1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"), Identifier.For("K"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                    fori1.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("C"), 
                            new ExpressionList(new Expression[] {Identifier.For("j"), Identifier.For("i")})),
                        Identifier.For("constanta"),
                        NodeType.Add));

                    forj1.Body.Statements.Add(fori1);
                    method.Body.Statements.Add(forj1);
                }
                
                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedMMTransposedPlusConst_WithCopying
        /// <summary>
        /// Returns appropriate function for: A := B * C! + k
        /// During the multiplication we copy rows of B and C to accelerate the multiplication
        /// if this function doesn't exist then creates it too
        /// We copy rows from A and B
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedMMTransposed_WithCopying(TypeNode leftType, TypeNode rightType,
            TypeNode returnType, TypeNode constType,
            bool isWithConstant,
            SourceContext sourceContext)
        {
            string s = "";
            if (isWithConstant) s = "PlusConst";
            Identifier name = Identifier.For(
                "OptimizedMMTransposed" + s + "_WithCopying" + leftType.Name + rightType.Name + returnType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = 2;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 2;

                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));
                if (isWithConstant)
                    method.Parameters.Add(new Parameter(Identifier.For("constanta"), constType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("M"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("N"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("K"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("C"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                //leftType[] aTemp = new leftType[N];
                ConstructArray aTempConstructor = new ConstructArray();
                aTempConstructor.ElementType = leftType;
                aTempConstructor.Rank = 1;
                aTempConstructor.Operands = new ExpressionList();
                aTempConstructor.Operands.Add(Identifier.For("N"));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(
                        Identifier.For("aTemp"),
                        aTempConstructor),
                    new ArrayTypeExpression(leftType, 1)));

                //rightType[] bTemp = new rightType[N];
                ConstructArray bTempConstructor = new ConstructArray();
                bTempConstructor.ElementType = rightType;
                bTempConstructor.Rank = 1;
                bTempConstructor.Operands = new ExpressionList();
                bTempConstructor.Operands.Add(Identifier.For("N"));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(
                        Identifier.For("bTemp"),
                        bTempConstructor),
                    new ArrayTypeExpression(rightType, 1)));

                For forj = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j"),
                        Identifier.For("M"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("K"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For forkj = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("N"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //aTemp[k] = A[j, k];
                forkj.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("aTemp"), new ExpressionList(new Expression[] { Identifier.For("k") })),
                    new Indexer(Identifier.For("A"), new ExpressionList(new Expression[] {
                        Identifier.For("j"), Identifier.For("k")})),
                    NodeType.Nop));

                For forki = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("N"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //bTemp[k] = B[i, k];
                forki.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("bTemp"), new ExpressionList(new Expression[] { Identifier.For("k") })),
                    new Indexer(Identifier.For("B"), new ExpressionList(new Expression[] {
                        Identifier.For("i"), Identifier.For("k")})),
                    NodeType.Nop));

                For fork1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("border"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork2 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("border"))}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("N"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //C[j, i] += aTemp[k] * bTemp[k];
                fork2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("i") })),
                    new BinaryExpression(
                        new Indexer(Identifier.For("aTemp"),
                            new ExpressionList(new Expression[] { Identifier.For("k") })),
                        new Indexer(Identifier.For("bTemp"),
                            new ExpressionList(new Expression[] { Identifier.For("k") })),
                        NodeType.Mul),
                    NodeType.Add));

                #region C[j, i] += aTemp[k] * bTemp[k] + ... + aTemp[k + 19] * bTemp[k + 19];
                //C[j, i] += aTemp[k] * bTemp[k] + ... + aTemp[k + 19] * bTemp[k + 19]; 
                BinaryExpression ab0 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { Identifier.For("k") })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { Identifier.For("k") })),
                    NodeType.Mul);

                BinaryExpression ab1 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab2 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab3 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab4 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab5 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab6 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab7 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab8 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab9 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab10 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab11 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab12 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab13 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab14 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab15 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab16 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab17 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab18 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab19 = new BinaryExpression(
                    new Indexer(Identifier.For("aTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("bTemp"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab01 = new BinaryExpression(ab0, ab1, NodeType.Add);
                BinaryExpression ab02 = new BinaryExpression(ab01, ab2, NodeType.Add);
                BinaryExpression ab03 = new BinaryExpression(ab02, ab3, NodeType.Add);
                BinaryExpression ab04 = new BinaryExpression(ab03, ab4, NodeType.Add);
                BinaryExpression ab05 = new BinaryExpression(ab04, ab5, NodeType.Add);
                BinaryExpression ab06 = new BinaryExpression(ab05, ab6, NodeType.Add);
                BinaryExpression ab07 = new BinaryExpression(ab06, ab7, NodeType.Add);
                BinaryExpression ab08 = new BinaryExpression(ab07, ab8, NodeType.Add);
                BinaryExpression ab09 = new BinaryExpression(ab08, ab9, NodeType.Add);
                BinaryExpression ab010 = new BinaryExpression(ab09, ab10, NodeType.Add);
                BinaryExpression ab011 = new BinaryExpression(ab010, ab11, NodeType.Add);
                BinaryExpression ab012 = new BinaryExpression(ab011, ab12, NodeType.Add);
                BinaryExpression ab013 = new BinaryExpression(ab012, ab13, NodeType.Add);
                BinaryExpression ab014 = new BinaryExpression(ab013, ab14, NodeType.Add);
                BinaryExpression ab015 = new BinaryExpression(ab014, ab15, NodeType.Add);
                BinaryExpression ab016 = new BinaryExpression(ab015, ab16, NodeType.Add);
                BinaryExpression ab017 = new BinaryExpression(ab016, ab17, NodeType.Add);
                BinaryExpression ab018 = new BinaryExpression(ab017, ab18, NodeType.Add);
                BinaryExpression ab019 = new BinaryExpression(ab018, ab19, NodeType.Add);

                fork1.Body.Statements.Add(
                    new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("i") })),
                        ab019,
                        NodeType.Add));

                #endregion

                fori.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(20, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(N / step) * step;
                fori.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("N"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                fori.Body.Statements.Add(forki);
                fori.Body.Statements.Add(fork1);
                fori.Body.Statements.Add(fork2);
                forj.Body.Statements.Add(forkj);
                forj.Body.Statements.Add(fori);
                method.Body.Statements.Add(forj);

                if (isWithConstant)
                {
                    //for (int j = 0; j < M; j++)
                    //    for (int i = 0; i < K; i++)
                    //        C[j, i] += constanta;

                    For forj1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                    For fori1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"), Identifier.For("K"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                    fori1.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("i") })),
                        Identifier.For("constanta"),
                        NodeType.Add));

                    forj1.Body.Statements.Add(fori1);
                    method.Body.Statements.Add(forj1);
                }

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedColSumFunction
        /// <summary>
        /// Returns appropriate function for: a := sum of columns of B
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedColSumFunction(TypeNode leftType, SourceContext sourceContext)
        {
            Identifier name = Identifier.For(
                "ColSum" + leftType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = leftType;
                returnArrayType.Rank = 1;
                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                
                //Parameter resParam = new Parameter(Identifier.For("res"), returnArrayType);
                //resParam.Flags = ParameterFlags.Out;
                //method.Parameters.Add(resParam);
                
                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("m"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("n"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, leftType), new ExpressionList(), sourceContext),
                        NodeType.Nop),
                        leftType));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                //leftType[] res = new leftType[n];
                ConstructArray newResArray = new ConstructArray();
                newResArray.ElementType = leftType;
                newResArray.Rank = 1;
                newResArray.Operands = new ExpressionList();
                newResArray.Operands.Add(Identifier.For("n"));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("res"), newResArray),
                    returnArrayType));

                For fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("m"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For forj1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j"),
                        Identifier.For("border"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For forj2 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("border"))}),
                    new BinaryExpression(Identifier.For("j"),
                        Identifier.For("n"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //res[j] += A[i, j];
                forj2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                    NodeType.Add));

                //res[j] += A[i, j];
                //res[j + 1] += A[i, j + 1];
                //res[j + 2] += A[i, j + 2];
                //res[j + 3] += A[i, j + 3];
                //res[j + 4] += A[i, j + 4];
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));

                fori.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(5, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(n / step) * step;
                fori.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("n"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                fori.Body.Statements.Add(forj1);
                fori.Body.Statements.Add(forj2);
                method.Body.Statements.Add(fori);

                method.ReturnType = returnArrayType;
                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedRowSumFunction
        /// <summary>
        /// Returns appropriate function for: a := sum of rows of B
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedRowSumFunction(TypeNode leftType, SourceContext sourceContext)
        {
            Identifier name = Identifier.For(
                "RowSum" + leftType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = leftType;
                returnArrayType.Rank = 1;
                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));

                //Parameter resParam = new Parameter(Identifier.For("res"), returnArrayType);
                //resParam.Flags = ParameterFlags.Out;
                //method.Parameters.Add(resParam);

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("m"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("n"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("temp"),
                        new Construct(new MemberBinding(null, leftType), new ExpressionList(), sourceContext),
                        NodeType.Nop),
                        leftType));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                //leftType[] res = new leftType[n];
                ConstructArray newResArray = new ConstructArray();
                newResArray.ElementType = leftType;
                newResArray.Rank = 1;
                newResArray.Operands = new ExpressionList();
                newResArray.Operands.Add(Identifier.For("n"));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("res"), newResArray),
                    returnArrayType));

                For fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("m"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For forj1 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j"),
                        Identifier.For("border"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For forj2 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("border"))}),
                    new BinaryExpression(Identifier.For("j"),
                        Identifier.For("n"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //res[j] += A[i, j];
                forj2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                    NodeType.Add));

                //res[j] += A[i, j];
                //res[j + 1] += A[i, j + 1];
                //res[j + 2] += A[i, j + 2];
                //res[j + 3] += A[i, j + 3];
                //res[j + 4] += A[i, j + 4];
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));
                forj1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("res"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("j"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("i"), new BinaryExpression(
                            Identifier.For("j"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Add));

                fori.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(5, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(n / step) * step;
                fori.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("n"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                fori.Body.Statements.Add(forj1);
                fori.Body.Statements.Add(forj2);
                method.Body.Statements.Add(fori);

                method.ReturnType = returnArrayType;
                Return ret = new Return();
                ret.Expression = Identifier.For("res");
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedMatrixVectorMult
        /// <summary>
        /// Returns appropriate function for: V1 := M * V2
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedMatrixVectorMult(TypeNode leftType, TypeNode rightType,
            TypeNode returnType,
            SourceContext sourceContext)
        {
            Identifier name = Identifier.For(
                "OptimizedMatrixVectorMult" + leftType.Name + rightType.Name + returnType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 2;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = 1;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 1;

                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("M"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("N"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("mb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("nb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("minTemp"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("u"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                For forj00 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("u"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("u"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("u"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList( new Statement[] {
                        new AssignmentStatement(
                            new Indexer(Identifier.For("C"), new ExpressionList(new Expression[] { Identifier.For("u") })),
                            new Literal(0.0, returnType),
                            NodeType.Nop)})));

                method.Body.Statements.Add(forj00);

                For forj0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j0"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k0"), Identifier.For("N"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                Member mathMin = STANDARD.systemMath.GetMethod(Identifier.For("Min"), SystemTypes.Int32, SystemTypes.Int32);

                For forj = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("j0"))}),
                    new BinaryExpression(Identifier.For("j"),
                        new MethodCall(
                            new MemberBinding(null, mathMin),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add),
                                Identifier.For("M")}),
                            NodeType.Call, SystemTypes.Int32),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork1 = new For(
                    new StatementList(new Statement[] {}),
                    new BinaryExpression(
                        new BinaryExpression(Identifier.For("k"),
                            Identifier.For("border"),
                            NodeType.Lt),
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("k"), Identifier.For("step"), NodeType.Add),
                            Identifier.For("minTemp"),
                            NodeType.Lt),
                        NodeType.LogicalAnd),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork2 = new For(
                    new StatementList(new Statement[] {}),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("minTemp"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //C[j] += A[j, k] * B[k];
                fork2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new BinaryExpression(
                        new Indexer(Identifier.For("A"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                        new Indexer(Identifier.For("B"),
                            new ExpressionList(new Expression[] { Identifier.For("k") })),
                        NodeType.Mul),
                    NodeType.Add));

                #region C[j] += A[j, k] * B[k] + ... + A[j, k + 19] * B[k + 19];
                //C[j] += A[j, k] * B[k] + ... + A[j, k + 19] * B[k + 19]; 
                BinaryExpression ab0 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("k") })),
                    NodeType.Mul);

                BinaryExpression ab1 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab2 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab3 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab4 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab5 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab6 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab7 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab8 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab9 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab10 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab11 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab12 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab13 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab14 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab15 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab16 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab17 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab18 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab19 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab01 = new BinaryExpression(ab0, ab1, NodeType.Add);
                BinaryExpression ab02 = new BinaryExpression(ab01, ab2, NodeType.Add);
                BinaryExpression ab03 = new BinaryExpression(ab02, ab3, NodeType.Add);
                BinaryExpression ab04 = new BinaryExpression(ab03, ab4, NodeType.Add);
                BinaryExpression ab05 = new BinaryExpression(ab04, ab5, NodeType.Add);
                BinaryExpression ab06 = new BinaryExpression(ab05, ab6, NodeType.Add);
                BinaryExpression ab07 = new BinaryExpression(ab06, ab7, NodeType.Add);
                BinaryExpression ab08 = new BinaryExpression(ab07, ab8, NodeType.Add);
                BinaryExpression ab09 = new BinaryExpression(ab08, ab9, NodeType.Add);
                BinaryExpression ab010 = new BinaryExpression(ab09, ab10, NodeType.Add);
                BinaryExpression ab011 = new BinaryExpression(ab010, ab11, NodeType.Add);
                BinaryExpression ab012 = new BinaryExpression(ab011, ab12, NodeType.Add);
                BinaryExpression ab013 = new BinaryExpression(ab012, ab13, NodeType.Add);
                BinaryExpression ab014 = new BinaryExpression(ab013, ab14, NodeType.Add);
                BinaryExpression ab015 = new BinaryExpression(ab014, ab15, NodeType.Add);
                BinaryExpression ab016 = new BinaryExpression(ab015, ab16, NodeType.Add);
                BinaryExpression ab017 = new BinaryExpression(ab016, ab17, NodeType.Add);
                BinaryExpression ab018 = new BinaryExpression(ab017, ab18, NodeType.Add);
                BinaryExpression ab019 = new BinaryExpression(ab018, ab19, NodeType.Add);

                fork1.Body.Statements.Add(
                    new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("j") })),
                        ab019,
                        NodeType.Add));

                #endregion

                //minTemp = Math.Min(k0 + nb, N);
                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("minTemp"),
                    new MethodCall(
                        new MemberBinding(null, mathMin),
                        new ExpressionList(new Expression[] {
                            new BinaryExpression(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add),
                            Identifier.For("N")}),
                        NodeType.Call, SystemTypes.Int32),
                    NodeType.Nop));

                forj.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(20, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(minTemp / step) * step;
                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("minTemp"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("k"), Identifier.For("k0"), NodeType.Nop));

                forj.Body.Statements.Add(fork1);
                forj.Body.Statements.Add(fork2);
                fork0.Body.Statements.Add(forj);
                forj0.Body.Statements.Add(fork0);
                method.Body.Statements.Add(forj0);

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedVectorMatrixVectorMultMin
        /// <summary>
        /// Returns appropriate function for: V1 := V2 - M * V3
        /// if this function doesn't exist then creates it too
        /// C := D - A * B;
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedVectorMatrixVectorMultMin(TypeNode leftType, TypeNode leftMulType, TypeNode rightMulType,
            TypeNode returnType,
            SourceContext sourceContext)
        {
            Identifier name = Identifier.For(
                "OptimizedVectorMatrixVectorMultMin" + leftType.Name + leftMulType.Name + 
                rightMulType.Name + returnType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 1;

                ArrayTypeExpression leftMulArrayType = new ArrayTypeExpression();
                leftMulArrayType.ElementType = leftMulType;
                leftMulArrayType.Rank = 2;

                ArrayTypeExpression rightMulArrayType = new ArrayTypeExpression();
                rightMulArrayType.ElementType = rightMulType;
                rightMulArrayType.Rank = 1;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 1;

                method.Parameters.Add(new Parameter(Identifier.For("D"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("A"), leftMulArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightMulArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("M"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("N"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("mb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("nb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("j"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("minTemp"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));

                method.Body.Statements.Add(ldsLoopVariables);

                For forj00 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j0"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));
                forj00.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"), new ExpressionList(new Expression[] { Identifier.For("j0") })),
                    new Indexer(Identifier.For("D"), new ExpressionList(new Expression[] { Identifier.For("j0") })),
                    NodeType.Nop));
                method.Body.Statements.Add(forj00);

                For forj0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("j0"), Identifier.For("M"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k0"), Identifier.For("N"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                Member mathMin = STANDARD.systemMath.GetMethod(Identifier.For("Min"), SystemTypes.Int32, SystemTypes.Int32);

                For forj = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), Identifier.For("j0"))}),
                    new BinaryExpression(Identifier.For("j"),
                        new MethodCall(
                            new MemberBinding(null, mathMin),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("j0"), Identifier.For("mb"), NodeType.Add),
                                Identifier.For("M")}),
                            NodeType.Call, SystemTypes.Int32),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork1 = new For(
                    new StatementList(new Statement[] { }),
                    new BinaryExpression(
                        new BinaryExpression(Identifier.For("k"),
                            Identifier.For("border"),
                            NodeType.Lt),
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("k"), Identifier.For("step"), NodeType.Add),
                            Identifier.For("minTemp"),
                            NodeType.Lt),
                        NodeType.LogicalAnd),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fork2 = new For(
                    new StatementList(new Statement[] { }),
                    new BinaryExpression(Identifier.For("k"),
                        Identifier.For("minTemp"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //C[j] -= A[j, k] * B[k];
                fork2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"),
                        new ExpressionList(new Expression[] { Identifier.For("j") })),
                    new BinaryExpression(
                        new Indexer(Identifier.For("A"),
                            new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                        new Indexer(Identifier.For("B"),
                            new ExpressionList(new Expression[] { Identifier.For("k") })),
                        NodeType.Mul),
                    NodeType.Sub));

                #region C[j] -= A[j, k] * B[k] + ... + A[j, k + 19] * B[k + 19];
                //C[j] -= A[j, k] * B[k] + ... + A[j, k + 19] * B[k + 19]; 
                BinaryExpression ab0 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), Identifier.For("k") })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { Identifier.For("k") })),
                    NodeType.Mul);

                BinaryExpression ab1 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab2 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(2, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab3 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(3, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab4 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(4, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab5 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(5, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab6 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(6, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab7 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(7, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab8 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(8, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab9 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(9, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab10 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(10, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab11 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(11, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab12 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(12, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab13 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(13, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab14 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(14, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab15 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(15, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab16 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(16, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab17 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(17, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab18 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(18, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab19 = new BinaryExpression(
                    new Indexer(Identifier.For("A"),
                        new ExpressionList(new Expression[] { Identifier.For("j"), new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    new Indexer(Identifier.For("B"),
                        new ExpressionList(new Expression[] { new BinaryExpression(
                            Identifier.For("k"), new Literal(19, SystemTypes.Int32), NodeType.Add) })),
                    NodeType.Mul);

                BinaryExpression ab01 = new BinaryExpression(ab0, ab1, NodeType.Add);
                BinaryExpression ab02 = new BinaryExpression(ab01, ab2, NodeType.Add);
                BinaryExpression ab03 = new BinaryExpression(ab02, ab3, NodeType.Add);
                BinaryExpression ab04 = new BinaryExpression(ab03, ab4, NodeType.Add);
                BinaryExpression ab05 = new BinaryExpression(ab04, ab5, NodeType.Add);
                BinaryExpression ab06 = new BinaryExpression(ab05, ab6, NodeType.Add);
                BinaryExpression ab07 = new BinaryExpression(ab06, ab7, NodeType.Add);
                BinaryExpression ab08 = new BinaryExpression(ab07, ab8, NodeType.Add);
                BinaryExpression ab09 = new BinaryExpression(ab08, ab9, NodeType.Add);
                BinaryExpression ab010 = new BinaryExpression(ab09, ab10, NodeType.Add);
                BinaryExpression ab011 = new BinaryExpression(ab010, ab11, NodeType.Add);
                BinaryExpression ab012 = new BinaryExpression(ab011, ab12, NodeType.Add);
                BinaryExpression ab013 = new BinaryExpression(ab012, ab13, NodeType.Add);
                BinaryExpression ab014 = new BinaryExpression(ab013, ab14, NodeType.Add);
                BinaryExpression ab015 = new BinaryExpression(ab014, ab15, NodeType.Add);
                BinaryExpression ab016 = new BinaryExpression(ab015, ab16, NodeType.Add);
                BinaryExpression ab017 = new BinaryExpression(ab016, ab17, NodeType.Add);
                BinaryExpression ab018 = new BinaryExpression(ab017, ab18, NodeType.Add);
                BinaryExpression ab019 = new BinaryExpression(ab018, ab19, NodeType.Add);

                fork1.Body.Statements.Add(
                    new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("j") })),
                        ab019,
                        NodeType.Sub));

                #endregion

                //minTemp = Math.Min(k0 + nb, N);
                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("minTemp"),
                    new MethodCall(
                        new MemberBinding(null, mathMin),
                        new ExpressionList(new Expression[] {
                            new BinaryExpression(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add),
                            Identifier.For("N")}),
                        NodeType.Call, SystemTypes.Int32),
                    NodeType.Nop));

                forj.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(20, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(minTemp / step) * step;
                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("minTemp"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                forj.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("k"), Identifier.For("k0"), NodeType.Nop));

                forj.Body.Statements.Add(fork1);
                forj.Body.Statements.Add(fork2);
                fork0.Body.Statements.Add(forj);
                forj0.Body.Statements.Add(fork0);
                method.Body.Statements.Add(forj0);

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedVectorConstVectorMatrixMultMultMin
        /// <summary>
        /// Returns appropriate function for: V1 := V1 - c1 * V2 * M;
        /// if this function doesn't exist then creates it too
        /// C := C - q * A * B;
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedVectorConstVectorMatrixMultMultMin(TypeNode constType, 
            TypeNode leftMulType, TypeNode rightMulType,
            TypeNode returnType,
            SourceContext sourceContext)
        {
            Identifier name = Identifier.For(
                "OptimizedVectorConstVectorMatrixMultMultMin" + constType.Name + leftMulType.Name + rightMulType.Name + returnType.Name);
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftMulArrayType = new ArrayTypeExpression();
                leftMulArrayType.ElementType = leftMulType;
                leftMulArrayType.Rank = 1;

                ArrayTypeExpression rightMulArrayType = new ArrayTypeExpression();
                rightMulArrayType.ElementType = rightMulType;
                rightMulArrayType.Rank = 2;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 1;

                method.Parameters.Add(new Parameter(Identifier.For("q"), constType));
                method.Parameters.Add(new Parameter(Identifier.For("A"), leftMulArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightMulArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("N"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("K"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("B"), getLength),
                            new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("nb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("kb"),
                        new Literal(50, SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i0"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("k"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("minTemp"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("step"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("border"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                method.Body.Statements.Add(ldsLoopVariables);

                ConstructArray newDArray = new ConstructArray();
                newDArray.ElementType = returnType;
                newDArray.Operands = new ExpressionList(new Expression[] {Identifier.For("K")});
                newDArray.Rank = 1;

                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("D"), newDArray, NodeType.Nop), returnArrayType));

                For fork0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("k0"), Identifier.For("N"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori0 = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i0"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i0"), Identifier.For("K"), NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i0"), Identifier.For("kb"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                Member mathMin = STANDARD.systemMath.GetMethod(Identifier.For("Min"), SystemTypes.Int32, SystemTypes.Int32);

                For fork = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), Identifier.For("k0"))}),
                    new BinaryExpression(Identifier.For("k"),
                        new MethodCall(
                            new MemberBinding(null, mathMin),
                            new ExpressionList(new Expression[] {
                                new BinaryExpression(Identifier.For("k0"), Identifier.For("nb"), NodeType.Add),
                                Identifier.For("N")}),
                            NodeType.Call, SystemTypes.Int32),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("k"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori1 = new For(
                    new StatementList(new Statement[] { }),
                    new BinaryExpression(
                        new BinaryExpression(Identifier.For("i"),
                            Identifier.For("border"),
                            NodeType.Lt),
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("i"), Identifier.For("step"), NodeType.Add),
                            Identifier.For("minTemp"),
                            NodeType.Lt),
                        NodeType.LogicalAnd),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), Identifier.For("step"), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                For fori2 = new For(
                    new StatementList(new Statement[] { }),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("minTemp"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                //D[i] += r * B[k, i];
                fori2.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("D"),
                        new ExpressionList(new Expression[] { Identifier.For("i") })),
                    new BinaryExpression(
                        Identifier.For("r"),
                        new Indexer(Identifier.For("B"),
                            new ExpressionList(new Expression[] { Identifier.For("k"), Identifier.For("i") })),
                        NodeType.Mul),
                    NodeType.Add));

                #region D[i] += r * B[k, i]; ... D[i + 19] += r * B[k, i + 19];
                fori1.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("D"),
                        new ExpressionList(new Expression[] { Identifier.For("i") })),
                    new BinaryExpression(
                        Identifier.For("r"),
                        new Indexer(Identifier.For("B"),
                            new ExpressionList(new Expression[] { Identifier.For("k"), Identifier.For("i") })),
                        NodeType.Mul),
                    NodeType.Add));

                for (int i = 1; i < 20; i++)
                    fori1.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("D"),
                            new ExpressionList(new Expression[] { new BinaryExpression(
                                Identifier.For("i"), new Literal(i, SystemTypes.Int32), NodeType.Add) })),
                        new BinaryExpression(
                            Identifier.For("r"),
                            new Indexer(Identifier.For("B"),
                                new ExpressionList(new Expression[] { Identifier.For("k"), new BinaryExpression(
                                    Identifier.For("i"), new Literal(i, SystemTypes.Int32), NodeType.Add) })),
                            NodeType.Mul),
                        NodeType.Add));
                #endregion

                //leftMulType r = A[k];
                fork.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("r"),
                        new Indexer(Identifier.For("A"), new ExpressionList(new Expression[] { Identifier.For("k") })),
                        NodeType.Nop),
                    leftMulType));
                
                //minTemp = Math.Min(i0 + kb, K);
                fork.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("minTemp"),
                    new MethodCall(
                        new MemberBinding(null, mathMin),
                        new ExpressionList(new Expression[] {
                            new BinaryExpression(Identifier.For("i0"), Identifier.For("kb"), NodeType.Add),
                            Identifier.For("K")}),
                        NodeType.Call, SystemTypes.Int32),
                    NodeType.Nop));

                fork.Body.Statements.Add(
                    new AssignmentStatement(Identifier.For("step"), new Literal(20, SystemTypes.Int32), NodeType.Nop));

                //border = (int)(minTemp / step) * step;
                fork.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("border"),
                    new BinaryExpression(
                        new BinaryExpression(
                            new BinaryExpression(Identifier.For("minTemp"), Identifier.For("step"), NodeType.Div),
                            Identifier.For("step"),
                            NodeType.Mul),
                        new MemberBinding(null, SystemTypes.Int32),
                        NodeType.Castclass),
                    NodeType.Nop));

                fork.Body.Statements.Add(new AssignmentStatement(
                    Identifier.For("i"), Identifier.For("i0"), NodeType.Nop));

                //for (int i = 0; i < K; i++)
                //  C[i] -= constanta * D[i];
                For fori_last = new For(
                    new StatementList(new Statement[] {
                    new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"), Identifier.For("K"), NodeType.Lt),
                    new StatementList(new Statement[] {
                    new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));
                fori_last.Body.Statements.Add(new AssignmentStatement(
                    new Indexer(Identifier.For("C"), new ExpressionList(new Expression[] { Identifier.For("i") })),
                    new BinaryExpression(Identifier.For("q"),
                        new Indexer(Identifier.For("D"), new ExpressionList(new Expression[] { Identifier.For("i") })),
                        NodeType.Mul),
                    NodeType.Sub));

                fork.Body.Statements.Add(fori1);
                fork.Body.Statements.Add(fori2);
                fori0.Body.Statements.Add(fork);
                fork0.Body.Statements.Add(fori0);
                method.Body.Statements.Add(fork0);
                method.Body.Statements.Add(fori_last);

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedElementWiseMult
        /// <summary>
        /// Returns appropriate function for: V1 := V2 * V3; M1 := M2 * M3; M1 := M1 * M2;
        /// if this function doesn't exist then creates it too
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedElementWiseMult(int dim, TypeNode leftType, 
            TypeNode rightType, TypeNode returnType, bool areReceiverAndLeftArgumentEqual, 
            SourceContext sourceContext)
        {
            Identifier name = Identifier.For("OptimizedElementWiseMult" + dim.ToString() + 
                leftType.Name + rightType.Name + returnType.Name + areReceiverAndLeftArgumentEqual.ToString());
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = dim;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = dim;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = dim;

                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("m"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                if (dim == 2)
                {
                    method.Body.Statements.Add(new LocalDeclarationsStatement(
                        new LocalDeclaration(Identifier.For("n"),
                            new MethodCall(
                                new MemberBinding(Identifier.For("A"), getLength),
                                new ExpressionList(new Expression[] { new Literal(1, SystemTypes.Int32) }),
                                NodeType.Call,
                                SystemTypes.Int32),
                            NodeType.Nop),
                            SystemTypes.Int32));
                }

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                if (dim == 2)
                {
                    ldsLoopVariables.Declarations.Add(
                        new LocalDeclaration(Identifier.For("j"),
                            new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                            NodeType.Nop));
                }

                method.Body.Statements.Add(ldsLoopVariables);

                For fori = null, forj = null;

                fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("m"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                if (dim == 2)
                {
                    forj = new For(
                        new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(0, SystemTypes.Int32))}),
                        new BinaryExpression(Identifier.For("j"),
                            Identifier.For("n"),
                            NodeType.Lt),
                        new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("j"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                        new Block(new StatementList(), sourceContext));

                    if (areReceiverAndLeftArgumentEqual)
                        forj.Body.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("C"),
                                new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                            new Indexer(Identifier.For("B"),
                                new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                            NodeType.Mul));
                    else
                        forj.Body.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("C"),
                                new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                            new BinaryExpression(
                                new Indexer(Identifier.For("A"),
                                    new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                                new Indexer(Identifier.For("B"),
                                    new ExpressionList(new Expression[] { Identifier.For("i"), Identifier.For("j") })),
                                NodeType.Mul),
                            NodeType.Nop));
                }
                else
                {
                    if (areReceiverAndLeftArgumentEqual)
                        fori.Body.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("C"),
                                new ExpressionList(new Expression[] { Identifier.For("i") })),
                            new Indexer(Identifier.For("B"),
                                new ExpressionList(new Expression[] { Identifier.For("i") })),
                            NodeType.Mul));
                    else
                        fori.Body.Statements.Add(new AssignmentStatement(
                            new Indexer(Identifier.For("C"),
                                new ExpressionList(new Expression[] { Identifier.For("i") })),
                            new BinaryExpression(
                                new Indexer(Identifier.For("A"),
                                    new ExpressionList(new Expression[] { Identifier.For("i") })),
                                new Indexer(Identifier.For("B"),
                                    new ExpressionList(new Expression[] { Identifier.For("i") })),
                                NodeType.Mul),
                            NodeType.Nop));
                }

                if (dim == 2) 
                    fori.Body.Statements.Add(forj);
                method.Body.Statements.Add(fori);

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #region GetOptimizedVectorConstVectorMulPlusOrMinus
        /// <summary>
        /// Returns appropriate function for: V1 := V1 +- c1 * V2; V1 := V2 +- c1 * V3;
        /// if this function doesn't exist then creates it too
        /// C := A +- q * B;
        /// </summary>
        /// <returns></returns>
        public Member GetOptimizedVectorConstVectorMulPlusOrMinus(TypeNode leftType, TypeNode constType, 
            TypeNode rightType, TypeNode returnType, bool areReceiverAndLeftArgumentEqual,
            bool isMinus,
            SourceContext sourceContext)
        {
            string s = "Plus";
            NodeType op = NodeType.Add;
            if (isMinus)
            { 
                s = "Minus"; 
                op = NodeType.Sub; 
            }

            Identifier name = Identifier.For("OptimizedVectorConstVectorMul" + s +
                leftType.Name + constType.Name + rightType.Name + returnType.Name + 
                areReceiverAndLeftArgumentEqual.ToString());
            MemberList ml = mathRoutines.GetMembersNamed(name);

            if (ml.Length == 0)
            {
                // We need to generate a specific function
                Method method = new Method();
                method.Name = name;
                method.DeclaringType = mathRoutines;
                method.Flags = MethodFlags.Static | MethodFlags.Public;
                method.InitLocals = true;

                method.Parameters = new ParameterList();

                method.Body = new Block();
                method.Body.Checked = false; //???
                method.Body.HasLocals = true;
                method.Body.Statements = new StatementList();

                method.SourceContext = sourceContext;
                method.Body.SourceContext = sourceContext;

                ArrayTypeExpression leftArrayType = new ArrayTypeExpression();
                leftArrayType.ElementType = leftType;
                leftArrayType.Rank = 1;

                ArrayTypeExpression rightArrayType = new ArrayTypeExpression();
                rightArrayType.ElementType = rightType;
                rightArrayType.Rank = 1;

                ArrayTypeExpression returnArrayType = new ArrayTypeExpression();
                returnArrayType.ElementType = returnType;
                returnArrayType.Rank = 1;

                method.Parameters.Add(new Parameter(Identifier.For("A"), leftArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("q"), constType));
                method.Parameters.Add(new Parameter(Identifier.For("B"), rightArrayType));
                method.Parameters.Add(new Parameter(Identifier.For("C"), returnArrayType));

                Member getLength = SystemTypes.Array.GetMembersNamed(Identifier.For("GetLength"))[0];
                method.Body.Statements.Add(new LocalDeclarationsStatement(
                    new LocalDeclaration(Identifier.For("n"),
                        new MethodCall(
                            new MemberBinding(Identifier.For("A"), getLength),
                            new ExpressionList(new Expression[] { new Literal(0, SystemTypes.Int32) }),
                            NodeType.Call,
                            SystemTypes.Int32),
                        NodeType.Nop),
                        SystemTypes.Int32));

                LocalDeclarationsStatement ldsLoopVariables = new LocalDeclarationsStatement();
                ldsLoopVariables.Type = SystemTypes.Int32;
                ldsLoopVariables.Declarations = new LocalDeclarationList();
                ldsLoopVariables.Declarations.Add(
                    new LocalDeclaration(Identifier.For("i"),
                        new Construct(new MemberBinding(null, SystemTypes.Int32), new ExpressionList(), sourceContext),
                        NodeType.Nop));
                method.Body.Statements.Add(ldsLoopVariables);

                For fori = new For(
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(0, SystemTypes.Int32))}),
                    new BinaryExpression(Identifier.For("i"),
                        Identifier.For("n"),
                        NodeType.Lt),
                    new StatementList(new Statement[] {
                        new AssignmentStatement(Identifier.For("i"), new Literal(1, SystemTypes.Int32), NodeType.Add)}),
                    new Block(new StatementList(), sourceContext));

                if (areReceiverAndLeftArgumentEqual) // (C = C +- q * B;) ~ (C +- = q * B;)
                    fori.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("i") })),
                        new BinaryExpression(
                            Identifier.For("q"),
                            new Indexer(Identifier.For("B"),
                                new ExpressionList(new Expression[] { Identifier.For("i") })),
                            NodeType.Mul),
                        op));
                else // C = A +- q * B;
                    fori.Body.Statements.Add(new AssignmentStatement(
                        new Indexer(Identifier.For("C"),
                            new ExpressionList(new Expression[] { Identifier.For("i") })),
                        new BinaryExpression(
                            new Indexer(Identifier.For("A"),
                                new ExpressionList(new Expression[] { Identifier.For("i") })),
                            new BinaryExpression(
                                Identifier.For("q"),
                                new Indexer(Identifier.For("B"),
                                    new ExpressionList(new Expression[] { Identifier.For("i") })),
                                NodeType.Mul),
                            op),
                        NodeType.Nop));

                method.Body.Statements.Add(fori);

                method.ReturnType = SystemTypes.Int32;
                Return ret = new Return();
                ret.Expression = new Literal(1, SystemTypes.Int32);
                method.Body.Statements.Add(ret);
                mathRoutines.Members.Add(method);
            }

            return mathRoutines.GetMembersNamed(name)[0];
        }
        #endregion

        #endregion

        private void Init(string suffix)
        {
            mathRoutines = new Class();
            mathRoutines.Flags = TypeFlags.Sealed | TypeFlags.Public;
            mathRoutines.Name = Identifier.For("MathRoutines" + "_" + suffix);
            mathRoutines.Members = new MemberList();
            mathRoutinesIdentifier = mathRoutines.Name;
        }

        /// <summary>
        /// Creates global list of mathematical functions to be used
        /// by mathematical operators.
        /// 
        /// </summary>
        public MathGenerator(string suffix)
        {
            Init(suffix);
        }

    }
}
