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
using System.Text;

namespace ETH.Zonnon
{

    public class TreeNUtil {

        public static Block createElementInitializerInternal(TYPE type, Expression indexer, int level, List<Expression> nonConstantDimensions, bool skipFirstLevel, SourceContext sourceContext)
        {            
            // Generates internal part of array initializer:
            // either object constructor or, again, array initializer.
            // Check if the type is ARRAY.
            if (!(type is ARRAY_TYPE) && !(type is OBJECT_TYPE))
                return null;

            if (type is ARRAY_TYPE)
            {
                EXPRESSION_LIST declaredDimensions = ((ARRAY_TYPE)type).dimensions;
                int Rank = declaredDimensions.Length;
                List<Expression> dimensions = new List<Expression>();
                // Check if all dimensions are constants or there is an expression available.                
                for (int i = 0, n = Rank; i < n; i++)
                    if (declaredDimensions[i] == null || declaredDimensions[i].calculate() == null)
                    {
                        if (nonConstantDimensions == null)
                            return null;
                        if (nonConstantDimensions.Count > 0)
                        {
                            dimensions.Add(nonConstantDimensions[0]);
                            nonConstantDimensions.RemoveAt(0);
                        } else {
                            ERROR.MissingParameters(Rank, type.sourceContext);
                            return null;
                        }                    
                    }
                    else {
                        long d = (long)declaredDimensions[i].calculate();
                        Literal dim = new Literal((int)d, SystemTypes.Int32);
                        dimensions.Add(dim);
                    }
                    

                Block block = new Block(new StatementList());

                // Generate array initializer:
                //  x = new object[n];

                if (skipFirstLevel && level == 0)
                    goto Bypass;
                AssignmentStatement array_initializer = new AssignmentStatement();
                array_initializer.NodeType = NodeType.AssignmentStatement;
                array_initializer.Operator = NodeType.Nop;  // this means "normal" assignment, but not += etc.

                // Generate 'new object[n]'
                ConstructArray array_construct = new ConstructArray();

                Node elem_type = ((ARRAY_TYPE)type).base_type.convert();
                if (elem_type is ArrayTypeExpression)
                {
                    ArrayTypeExpression arr_type = (elem_type.Clone()) as ArrayTypeExpression;
                    //  for ( int i=0, n=arr_type.Rank; i<n; i++ )
                    //      arr_type.Sizes[i] = -1;
                    elem_type = arr_type;
                }
                array_construct.ElementType = (TypeNode)elem_type;
                array_construct.Rank = Rank;
                array_construct.SourceContext = sourceContext;
                array_construct.Type = (TypeNode)type.convert();
                array_construct.Operands = new ExpressionList();
                for (int i = 0; i < Rank; i++)
                {
                    array_construct.Operands.Add(dimensions[i]);
                }

                array_initializer.Source = array_construct;
                array_initializer.Target = indexer;
                array_initializer.SourceContext = sourceContext;
                block.Statements.Add(array_initializer);

            Bypass:

                // Generate x[i0,i1,...] for passing to the recursive call.
                Indexer new_indexer = new Indexer();
                new_indexer.Object = indexer;
                new_indexer.Type = type.convert() as TypeNode;
                new_indexer.ElementType = ((ARRAY_TYPE)type).base_type.convert() as TypeNode;
                new_indexer.Operands = new ExpressionList();
                for (int i = 0; i < Rank; i++)
                {
                    Identifier index = Identifier.For("_i" + (level + i).ToString());
                    new_indexer.Operands.Add(index);
                }

                // Generate the last part (see comment, part 4, below).
                Block elem_initializers =
                    createElementInitializerInternal(((ARRAY_TYPE)type).base_type, new_indexer, level + Rank, nonConstantDimensions, true, sourceContext);
                if (elem_initializers == null)
                    return block;
                // We do not need loops to initialize elements...
                // Return just array initializer.
                // Otherwise go generate initializers.

                // Generate
                // 1) int i0, i1, ...;
                // 2) for (int i1=0; i1<n; i1++)
                //        for (int i2=0; i2<m; i2++)
                //            ...
                //
                // Generate recursively:
                // 3) Initializers for array elements:  x[i1,i2,...] = new object[n];
                // 4) Initializers for every element (the similar loop(s)).

                // Generate int i0, i1, ...;
                for (int i = 0; i < Rank; i++)
                {
                    VariableDeclaration locali =
                        new VariableDeclaration(Identifier.For("_i" + (level + i).ToString()), SystemTypes.Int32, null);
                    block.Statements.Add(locali);
                }

                // Generate loop headers:
                //      for (int i1=0; i1<n; i1++)
                //          for (int i2=0; i2<m; i2++)
                //              ...

                Block owner = block;  // where to put generated for-node
                for (int i = 0; i < Rank; i++)
                {
                    For forStatement = new For();
                    // forStatement.NodeType;
                    forStatement.SourceContext = sourceContext;

                    // Making for-statement's body
                    forStatement.Body = new Block();
                    forStatement.Body.Checked = true;
                    forStatement.Body.HasLocals = false;
                    // forStatement.Body.NodeType;
                    // forStatement.Body.Scope;
                    forStatement.Body.SourceContext = sourceContext;
                    forStatement.Body.SuppressCheck = false;
                    forStatement.Body.Statements = new StatementList();
                    // Now leave the body empty...

                    // Making condition: i<n
                    BinaryExpression condition = new BinaryExpression();
                    condition.NodeType = NodeType.Lt;
                    condition.Operand1 = Identifier.For("_i" + (level + i).ToString());
                    condition.Operand2 = dimensions[i];
                    condition.SourceContext = sourceContext;
                    forStatement.Condition = condition;

                    // Making incrementer: i+=1
                    forStatement.Incrementer = new StatementList();
                    AssignmentStatement assignment = new AssignmentStatement();
                    assignment.NodeType = NodeType.AssignmentStatement;
                    assignment.Operator = NodeType.Add;  // Hope this means +=
                    // assignment.OperatorOverload
                    assignment.Source = new Literal((int)1, SystemTypes.Int32);
                    assignment.SourceContext = sourceContext;
                    assignment.Target = Identifier.For("_i" + (level + i).ToString());
                    forStatement.Incrementer.Add(assignment);

                    // Making initializer: i=0
                    forStatement.Initializer = new StatementList();
                    AssignmentStatement initializer = new AssignmentStatement();
                    initializer.NodeType = NodeType.AssignmentStatement;
                    initializer.Operator = NodeType.Nop;  // this means "normal" assignment, but not += etc.
                    initializer.Source = new Literal(0, SystemTypes.Int32);
                    initializer.Target = Identifier.For("_i" + (level + i).ToString());
                    initializer.SourceContext = sourceContext;

                    forStatement.Initializer.Add(initializer);

                    owner.Statements.Add(forStatement);
                    owner = forStatement.Body; // for next iteration
                }

                // Adding element initializers generated in advance.
                owner.Statements.Add(elem_initializers);

                return block;
            }
            else if (type is OBJECT_TYPE)
            {
                // Check if the type is VAL-object.
                if (!((OBJECT_TYPE)type).ObjectUnit.modifiers.Value)
                    return null;

                Block block = new Block(new StatementList());

                // Generate 'new obj'


                DECLARATION objct = ((OBJECT_TYPE)type).ObjectUnit;

                // We do it for only own value types. They have
                // extra constcutor that takes ont fictive intgere. Might have
                // Chtck and call it
                Construct construct = new Construct();

                // Strange thing: CCI expects _class_ in Construst.Constructor,
                // but not a constructor itself!..
                // construct.Constructor = new MemberBinding(null,((TypeNode)objct.convert()).GetConstructors()[0]); //  NODE.convertTypeName(objct);
                construct.Constructor = new MemberBinding(null, (TypeNode)objct.convert());
                construct.Constructor.Type = SystemTypes.Type;

                construct.Operands = new ExpressionList();
                construct.SourceContext = sourceContext;
                construct.Type = (TypeNode)objct.convert();
                construct.Operands.Add(new Literal(1, SystemTypes.Int32));

                // Generate x[i0,i1,...] = new obj;
                AssignmentStatement main_initializer = new AssignmentStatement();
                main_initializer.NodeType = NodeType.AssignmentStatement;
                main_initializer.Operator = NodeType.Nop;  // this means "normal" assignment, but not += etc.
                main_initializer.Source = construct;
                main_initializer.Target = indexer;
                main_initializer.SourceContext = sourceContext;



                block.Statements.Add(main_initializer);
                return block;
            }
            else
                if (type is EXTERNAL_TYPE)
                {
                    // Only value types might need extra calls
                    Struct str = ((EXTERNAL_TYPE)type).entity as Struct;

                    InstanceInitializer ctr = str.GetConstructor(new TypeNode[] { SystemTypes.Int32 });
                    bool possibly_was_our_structure = (ctr != null);
                    if (ctr == null)
                        ctr = str.GetConstructor(new TypeNode[0] { });

                    // TO_DO: When metadata is available replace this with
                    // more consistent check

                    Block block = new Block(new StatementList());

                    // Generate 'new obj'
                    Construct construct = new Construct();


                    construct.Constructor = new MemberBinding(null, ctr);
                    construct.Operands = new ExpressionList();
                    construct.Type = str;
                    // We do it for only own value types. They have
                    // extra constcutor that takes ont fictive intgere. Might have
                    // Chtck and call it
                    if (possibly_was_our_structure)
                        construct.Operands.Add(Literal.Int32MinusOne);

                    // Generate x[i0,i1,...] = new obj;
                    AssignmentStatement main_initializer = new AssignmentStatement();
                    main_initializer.NodeType = NodeType.AssignmentStatement;
                    main_initializer.Operator = NodeType.Nop;  // this means "normal" assignment, but not += etc.
                    main_initializer.Source = construct;
                    main_initializer.Target = indexer;
                    main_initializer.SourceContext = sourceContext;

                    indexer.Type = construct.Type;

                    block.Statements.Add(main_initializer);
                    return block;
                }
                else
                    return null;
        }


        public static Method GetArrayInitializationMethod(String name, bool isPublic, TypeNode declaringType, ARRAY_TYPE type, SourceContext sourceContext) {
            //Array NewTypeName()
            //{
            //    Array Res;
            //    Res = new Array[A.GetLength(0), A.GetLength(1)];
            //    for (int i1 = 0; i1 < A.GetLength(0); i1++)
            //        Res[i1] = new Array[];
            //    return Res;
            //}

            TypeNode returnType = (TypeNode)type.convertAndGetType();
            Method method = new Method();
            method.Name = Identifier.For("New" + name);
            method.DeclaringType = declaringType;
            method.Flags = MethodFlags.Static;
            if (isPublic)
              method.Flags |= MethodFlags.Public;
            else
              method.Flags |= MethodFlags.Private;
            method.InitLocals = true;
            method.SourceContext = sourceContext;
            method.Body = new Block();
            method.Body.Checked = false; //???
            method.Body.HasLocals = true;
            method.Body.Statements = new StatementList();

            method.ReturnType = returnType;

            method.Parameters = new ParameterList();
            int count = 0;
            // Add parameters
            List<Expression> parameterAccess = new List<Expression>();
            ARRAY_TYPE arrayType = type;
            while (arrayType != null)
            {
                for (int i = 0; i < arrayType.dimensions.Length; i++)
                {
                    if (arrayType.dimensions[i] == null || arrayType.dimensions[i].calculate() == null)
                    {
                        Identifier paramName = Identifier.For("n" + count.ToString());
                        Parameter parameter = new Parameter(paramName, SystemTypes.Int32);
                        method.Parameters.Add(parameter);
                        parameterAccess.Add(paramName);
                        count++;
                    }
                }
                if (arrayType.base_type is ARRAY_TYPE)
                {
                    arrayType = (ARRAY_TYPE)arrayType.base_type;
                }
                else
                {
                    arrayType = null;
                }
            }

            Identifier resultName = Identifier.For("res");

            LocalDeclarationsStatement loc_stmt = new LocalDeclarationsStatement();
            loc_stmt.Constant = false;
            loc_stmt.Declarations = new LocalDeclarationList(1);
            loc_stmt.InitOnly = false;
            loc_stmt.SourceContext = sourceContext;
            loc_stmt.Type = returnType;

            LocalDeclaration local = new LocalDeclaration();            
            local.Name = resultName;
            local.SourceContext = sourceContext;
            loc_stmt.Declarations.Add(local);

            method.Body.Statements.Add(loc_stmt);

            method.Body.Statements.Add(createElementInitializerInternal(type, resultName, 0, parameterAccess, false, sourceContext));  

            Return ret = new Return();
            ret.Expression = resultName;
            
            method.Body.Statements.Add(ret);
            return method;
        }

    }

}