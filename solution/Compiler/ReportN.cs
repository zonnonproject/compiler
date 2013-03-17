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

namespace ETH.Zonnon
{
	public class REPORT
	{
		public REPORT()
		{
		}

        private static int ind = 4;

        private static void indent ( int n )
        {
            for (int i=0; i<n; i++ ) Console.Write(" ");
        }

        public static void report ( Node node, int shift )
        {
            if ( node == null )
            {
                indent(shift);
                Console.WriteLine("NULL ENTITY");
                return;
            }

            switch ( node.NodeType )
            {
                case NodeType.AliasDefinition :
                {
                    AliasDefinition alias = node as AliasDefinition;
                    indent(shift);
                    Console.WriteLine("using {0} = {1};", alias.Alias.Name, alias.AliasedUri.Name);
                    break;
                }

                case NodeType.CompilationUnit :
                case NodeType.CompilationUnitSnippet :
                {
                    CompilationUnit cu = node as CompilationUnit;

                    for ( int i=0, n=cu.Nodes.Length; i<n; i++ )
                    {
                        report(cu.Nodes[i],0);
                    }
                    break;
                }

                case NodeType.Namespace :
                {
                    Namespace ns = node as Namespace;

                    if ( ns.UsedNamespaces != null && ns.UsedNamespaces.Length != 0 )
                    {
                        indent(shift);
                        Console.WriteLine("using ");
                        for ( int i=0, n=ns.UsedNamespaces.Length; i<n; i++ )
                        {
                            Console.Write("{0}",ns.UsedNamespaces[i].Namespace.Name);
                            if ( i < n-1 ) Console.Write(", ");
                        }
                        Console.WriteLine();
                    }

                    indent(shift);
                    Console.WriteLine("namespace {0}",ns.FullNameId.Name);

                    indent(shift);
                    Console.WriteLine("{");

                    if ( ns.AliasDefinitions != null && ns.AliasDefinitions.Length != 0 )
                    {
                        for ( int i=0, n=ns.AliasDefinitions.Length; i<n; i++ )
                            report(ns.AliasDefinitions[i],shift+ind);
                    }

                    if ( ns.NestedNamespaces != null && ns.NestedNamespaces.Length != 0 )
                    {
                        for ( int i=0, n=ns.NestedNamespaces.Length; i<n; i++ )
                            report(ns.NestedNamespaces[i],shift+ind);
                    }

                    if ( ns.Types != null && ns.Types.Length != 0 )
                    {
                        for ( int i=0, n=ns.Types.Length; i<n; i++ )
                            report(ns.Types[i],shift+ind);
                    }

                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.Class :
                {
                    Class cls = node as Class;

                    if ( cls == SystemTypes.Object )
                    {
                        Console.Write(cls.Name);
                        break;
                    }

                    indent(shift);

                    if      ( cls.IsAbstract ) Console.Write("abstract ");
                    if      ( cls.IsPrivate )  Console.Write("private ");
                    else if ( cls.IsPublic )   Console.Write("public ");
                    else                       Console.Write("internal ");  // ??????????????
                    if      ( cls.IsSealed )   Console.Write("sealed ");

                    Console.Write("class ");
                    if ( cls.DeclaringType != null ) Console.Write("{0}::",cls.DeclaringType.Name.Name);
                    Console.Write("{0}",cls.Name!=null?cls.Name.Name:"<NONAME>");

                    if ( cls.BaseClass != null )
                        Console.Write(" : {0}",cls.BaseClass.Name.Name);

                    if ( cls.Interfaces != null && cls.Interfaces.Length != 0 )
                    {
                        if ( cls.BaseClass != null ) Console.Write(",");
                        else                         Console.Write(" :");

                        for ( int i=0, n=cls.Interfaces.Length; i<n; i++ )
                        {
                            Interface interfac = cls.Interfaces[i];
                            if ( interfac != null )
                                Console.Write(" {0}",interfac.Name.Name);
                            if ( i<n-1 ) Console.Write(",");
                        }
                    }
                    Console.WriteLine();

                    indent(shift);
                    Console.WriteLine("{");

                    if ( cls.Members != null && cls.Members.Length != 0 )
                    {
                        for ( int i=0, n=cls.Members.Length; i<n; i++ )
                        {
                            Member member = cls.Members[i];

                            if ( member == null )
                            {
                                indent(shift+ind);
                                Console.WriteLine("<UNRESOLVED MEMBER>");
                                continue;
                            }
                            report(member,shift+ind);
                        }
                    }
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.Struct :
                {
                    Struct struc = node as Struct;

                    indent(shift);

                    if      ( struc.IsAbstract ) Console.Write("abstract ");
                    if      ( struc.IsPrivate )  Console.Write("private ");
                    else if ( struc.IsPublic )   Console.Write("public ");
                    else                         Console.Write("internal ");  // ??????????????
                    if      ( struc.IsSealed )   Console.Write("sealed ");

                    Console.Write("struct ");
                    if ( struc.DeclaringType != null ) Console.Write("{0}::",struc.DeclaringType.Name.Name);
                    Console.Write("{0}",struc.Name!=null?struc.Name.Name:"<NONAME>");

                    if ( struc.Interfaces != null && struc.Interfaces.Length != 0 )
                    {
                        Console.Write(" :");
                        for ( int i=0, n=struc.Interfaces.Length; i<n; i++ )
                        {
                            Interface interfac = struc.Interfaces[i];
                            if ( interfac != null )
                                Console.Write(" {0}",interfac.Name.Name);
                            if ( i<n-1 ) Console.Write(",");
                        }
                    }
                    Console.WriteLine();

                    indent(shift);
                    Console.WriteLine("{");

                    if ( struc.Members != null && struc.Members.Length != 0 )
                    {
                        for ( int i=0, n=struc.Members.Length; i<n; i++ )
                        {
                            Member member = struc.Members[i];

                            if ( member == null )
                            {
                                indent(shift+ind);
                                Console.WriteLine("<UNRESOLVED MEMBER>");
                                continue;
                            }
                            report(member,shift+ind);
                        }
                    }
                    indent(shift);
                    Console.WriteLine("}");
                    break;
                }

                case NodeType.EnumNode :
                {
                    EnumNode enume = node as EnumNode;

                    indent(shift);
                    if ( enume.Name != null && enume.Name.Name != null )
                        Console.Write("enum {0} = ",enume.Name.Name);
                    else
                        Console.Write("enum <NONAME> = ");
                    Console.Write("{");

                    for ( int i=0, n=enume.Members.Length; i<n; i++ )
                    {
                        Field enumerator = (Field)enume.Members[i];
                        Console.Write("{0}",enumerator.Name.Name);
                        if ( enumerator.DefaultValue != null )
                            Console.Write(" = {0}",enumerator.DefaultValue.ToString());
                        if ( i<n-1 ) Console.Write(", ");
                    }
                    Console.WriteLine("};");

                    break;
                }

                case NodeType.Interface :
                {
                    Interface interfac = node as Interface;

                    indent(shift);

                    if ( interfac.IsAbstract )    Console.Write("abstract ");
                    if ( interfac.IsPrivate )     Console.Write("private ");
                    else if ( interfac.IsPublic ) Console.Write("public ");
                    else                          Console.Write("internal ");  // ???????????

                    Console.WriteLine("interface {0}",interfac.Name.Name);
                    indent(shift);
                    Console.WriteLine("{");

                    if ( interfac.Members != null && interfac.Members.Length != 0 )
                    {
                        for ( int i=0, n=interfac.Members.Length; i<n; i++ )
                        {
                            Member member = interfac.Members[i];

                            if ( member == null )
                            {
                                indent(shift+ind);
                                Console.WriteLine("<UNRESOLVED MEMBER>");
                                continue;
                            }
                            report(member,shift+ind);
                        }
                    }
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.Method :
                case NodeType.InstanceInitializer :
                {
                    Method method = node as Method;

                    indent(shift);

                    if ( method.IsAbstract ) Console.Write("abstract ");
                    if ( method.IsPublic )   Console.Write("public ");
                    if ( method.IsStatic )   Console.Write("static ");
                    if ( method.IsVirtual )  Console.Write("virtual ");
                    if ( method.IsPrivate )  Console.Write("private ");
                    if ( method.OverriddenMethod != null ) Console.Write("override ");

                    if ( method.ReturnType != null && method.ReturnType.Name != null )
                    {
                        Console.Write("{0} ",method.ReturnType.Name.Name);
                    }
                    if ( method.Name != null )
                    {
                        if ( method.ImplementedInterfaceMethods != null && method.ImplementedInterfaceMethods.Length != 0 )
                        {
                            Method interf = method.ImplementedInterfaceMethods[0];
                            if ( interf != null )
                            {
                                string name = interf.DeclaringType.Name.Name;
                                Console.Write("{0}.",name);
                            }
                        }
                        Console.Write("{0}",method.Name.Name);
                    }
                    Console.Write(" (");

                    if ( method.Parameters != null && method.Parameters.Length != 0 )
                    {
                        for ( int i=0, n=method.Parameters.Length; i<n; i++ )
                        {
                            Parameter par = method.Parameters[i];
                            if ( par == null ) continue;
                            if ( (par.Flags & ParameterFlags.In) != 0  ) Console.Write("in ");
                            if ( (par.Flags & ParameterFlags.Out) != 0 ) Console.Write("out ");

                            if ( par.Type != null && par.Type.Name != null )
                                Console.Write("{0}",par.Type.Name.Name);
                            else
                                report(par.Type,0);
                            Console.Write(" {0}",par.Name.Name);
                            if ( i<n-1 ) Console.Write(", ");
                        }
                    }
                    Console.Write(" )");

                    // method body
                    if ( method.Body != null )
                    {
                        Console.WriteLine();
                        report(method.Body,shift);
                    }
                    else
                    {
                        Console.WriteLine(";");
                    }
                    break;
                }

                case NodeType.DelegateNode :
                {
                    DelegateNode dn = node as DelegateNode;

                    indent(shift);
                    Console.Write("delegate ");

                    if ( dn.ReturnType != null && dn.ReturnType.Name != null )
                        Console.Write("{0} ",dn.ReturnType.Name.Name);
                    if ( dn.Name != null )
                        Console.Write("{0}",dn.Name.Name);
                    Console.Write(" (");

                    if ( dn.Parameters != null && dn.Parameters.Length != 0 )
                    {
                        for ( int i=0, n=dn.Parameters.Length; i<n; i++ )
                        {
                            Parameter par = dn.Parameters[i];
                            if ( par == null ) continue;
                            if ( (par.Flags & ParameterFlags.In) != 0  ) Console.Write("in ");
                            if ( (par.Flags & ParameterFlags.Out) != 0 ) Console.Write("out ");

                            if ( par.Type != null && par.Type.Name != null )
                                Console.Write("{0}",par.Type.Name.Name);
                            else
                                report(par.Type,0);
                            Console.Write(" {0}",par.Name.Name);
                            if ( i<n-1 ) Console.Write(", ");
                        }
                    }
                    Console.WriteLine(" );");
                    break;
                }

                case NodeType.StaticInitializer :
                {
                    StaticInitializer si = node as StaticInitializer;

                    indent(shift);

                    Console.WriteLine("static {0} ( )",si.Name.Name);

                    // body
                    if ( si.Body != null ) report(si.Body,shift);
                    else                   Console.WriteLine("NO BODY");
                    break;
                }

                case NodeType.FieldInitializerBlock :
                {
                    FieldInitializerBlock initializers = node as FieldInitializerBlock;

                    indent(shift);
                    if ( initializers.IsStatic ) Console.Write("static ");
                    Console.WriteLine("init {");
                    for ( int i=0, n=initializers.Statements.Length; i<n; i++ )
                    {
                        report(initializers.Statements[i],shift+ind);
                    }
                    indent(shift);
                    Console.WriteLine("}");
                    break;
                }

                case NodeType.Base :
                {
                    Console.Write("base");
                    break;
                }

                case NodeType.Field :
                {
                    Field field = node as Field;

                    indent(shift);

                    if      ( field.IsPrivate ) Console.Write("private ");
                    else if ( field.IsPublic )  Console.Write("public ");

                    if ( field.IsStatic ) Console.Write("static ");
                    if ( field.IsInitOnly ) Console.Write("readonly ");

                    if ( field.Type != null )
                    {
                        if ( field.Type.Name != null )
                            Console.Write("{0}",field.Type.Name.Name);
                        else
                            report(field.Type,0);
                    }
                    Console.Write(" {0}",field.Name.Name);

                    if ( field.Initializer != null )
                    {
                        Console.Write(" = ");
                        report(field.Initializer,0);
                    }
                    Console.WriteLine(";");

                    break;
                }

                case NodeType.VariableDeclaration :
                {
                    VariableDeclaration variable = node as VariableDeclaration;

                    indent(shift);
                    if ( variable.Type != null && variable.Type.Name != null )
                        Console.Write("{0}",variable.Type.Name.Name);
                    else
                        report(variable.Type,0);

                    Console.Write(" {0}",variable.Name.Name);

                    if ( variable.Initializer != null )
                    {
                        Console.Write(" = ");
                        report(variable.Initializer,0);
                    }
                    Console.WriteLine(";");

                    break;
                }

                case NodeType.LocalDeclarationsStatement :
                {
                    LocalDeclarationsStatement stmt = node as LocalDeclarationsStatement;

                    indent(shift);

                    TypeNode type = stmt.Type;
                    if ( type != null && type.Name != null )
                        Console.Write("{0}",type.Name.Name);
                    else
                        report(type,0);
                    Console.Write(" ");

                    LocalDeclarationList list = stmt.Declarations;
                    for ( int i=0, n=list.Length; i<n; i++ )
                    {
                        LocalDeclaration local = list[i];
                        Console.Write("{0}",local.Name.Name);
                        if ( local.InitialValue != null )
                        {
                            Console.Write(" = ");
                            report(local.InitialValue,0);
                        }
                        if ( i<n-1 ) Console.Write(", ");
                    }
                    Console.WriteLine(";");
                    break;
                }

                case NodeType.Property :
                {
                    Property property = node as Property;

                    indent(shift);

                    if      ( property.IsPrivate ) Console.Write("private ");
                    else if ( property.IsPublic )  Console.Write("public ");

                    if ( property.IsStatic ) Console.Write("static ");

                    if ( property != null )
                    {
                        if ( property.Type != null && property.Type.Name != null )
                            Console.Write("{0} ",property.Type.Name.Name);

                        if ( property.ImplementedTypes != null )
                        {
                            TypeNode typ = property.ImplementedTypes[0];
                            Console.Write("{0}.",typ.Name.Name);
                        }
                        if ( property.Name != null )
                            Console.WriteLine("{0}",property.Name.Name);
                    }
                    indent(shift);
                    Console.WriteLine("{");

                    if ( property.Getter != null ) report(property.Getter,shift+ind);
                    if ( property.Setter != null ) report(property.Setter,shift+ind);

                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

            case NodeType.Lock:
                {
                    Lock _lock = node as Lock;

                    indent(shift);
                    Console.Write("lock(");
                    report(_lock.Guard, shift);
                    Console.WriteLine(")");
                    report(_lock.Body, shift + ind);
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }
            case NodeType.Block:
                {
                    Block block = node as Block;
                    if (block == null || block.Statements == null) break;
                    indent(shift);
                    Console.WriteLine("{");

                    for ( int i=0, n=block.Statements.Length; i<n; i++ )
                    {
                        report(block.Statements[i],shift+ind);
                        Console.WriteLine();
                    }
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.MemberBinding :
                {
                    MemberBinding mb = node as MemberBinding;
                    if ( mb.TargetObject != null )
                    {
                        report(mb.TargetObject,0);
                    } else if (mb.BoundMember != null && mb.BoundMember.DeclaringType != null) {
                        Console.Write(mb.BoundMember.DeclaringType.Name);
                    }
                    Console.Write(".");
                    if ( mb.BoundMember.Name != null )
                        Console.Write(mb.BoundMember.Name.Name);
                    else
                        report(mb.BoundMember,0);
                    break;
                }

                case NodeType.AssignmentStatement :
                {
                    AssignmentStatement assignment = node as AssignmentStatement;

                    indent(shift);

                    report(assignment.Target,0);
                    switch ( assignment.Operator )
                    {
                        case NodeType.Nop: Console.Write(" = "); break;
                        case NodeType.Add: Console.Write(" += "); break;
                        case NodeType.Add_Ovf: Console.Write(" += "); break;
                        case NodeType.Add_Ovf_Un: Console.Write(" += "); break;
                        case NodeType.Sub: Console.Write(" -= "); break;
                        case NodeType.Sub_Ovf: Console.Write(" -= "); break;
                        case NodeType.Sub_Ovf_Un: Console.Write(" -= "); break;
                        case NodeType.Mul: Console.Write(" *= "); break;
                        case NodeType.Mul_Ovf: Console.Write(" *= "); break;
                        case NodeType.Mul_Ovf_Un: Console.Write(" *= "); break;
                    }
                    report(assignment.Source,0);
                    Console.Write(";");
                    break;
                }

                case NodeType.ExpressionStatement :
                {
                    ExpressionStatement exprStatement = node as ExpressionStatement;

                    indent(shift);

                    report(exprStatement.Expression,0);
                    Console.Write(";");
                    break;
                }

                case NodeType.Return :
                {
                    Return return_stmt = node as Return;

                    indent(shift);
                    Console.Write("return");
                    if ( return_stmt.Expression != null )
                    {
                        Console.Write(" ");
                        report(return_stmt.Expression,0);
                    }
                    Console.Write(";");

                    break;
                }

                case NodeType.Branch :
                {
                    Branch branch = node as Branch;

                    indent(shift);
                    Console.WriteLine("break; (???)");

                    break;
                }

                case NodeType.For :
                {
                    For for_stmt = node as For;

                    indent(shift);
                    Console.Write("for ( ");
                    for ( int i=0, n=for_stmt.Initializer.Length; i<n; i++ )
                        report(for_stmt.Initializer[i],0);
                    report(for_stmt.Condition,0);
                    Console.Write("; ");
                    for ( int i=0, n=for_stmt.Incrementer.Length; i<n; i++ )
                        report(for_stmt.Incrementer[i],0);
                    Console.WriteLine(")");

                    indent(shift);
                    Console.WriteLine("{");
                    report(for_stmt.Body,shift+ind);
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.While :
                {
                    While while_loop = node as While;

                    indent(shift);
                    Console.Write("while ( ");
                    report(while_loop.Condition,0);
                    Console.WriteLine(" )");

                    report(while_loop.Body,shift);

                    break;
                }

                case NodeType.DoWhile :
                {
                    DoWhile repeat = node as DoWhile;

                    indent(shift);
                    Console.WriteLine("do");
                    report(repeat.Body,shift);

                    indent(shift);
                    Console.Write("while (");
                    report(repeat.Condition,0);
                    Console.WriteLine(" );");

                    break;
                }

                case NodeType.If :
                {
                    If if_stmt = node as If;

                    indent(shift);
                    Console.Write("if ( ");
                    report(if_stmt.Condition,0);
                    Console.WriteLine(" )");

                    report(if_stmt.TrueBlock,shift);

                    if ( if_stmt.FalseBlock == null            ||
                         if_stmt.FalseBlock.Statements == null ||
                         if_stmt.FalseBlock.Statements.Length == 0 )
                        break;

                    indent(shift);
                    Console.WriteLine("else");
                    report(if_stmt.FalseBlock,shift);

                    break;
                }

                case NodeType.Switch :
                {
                    Switch swtch = node as Switch;

                    indent(shift);
                    Console.Write("switch ( ");
                    report(swtch.Expression,0);
                    Console.WriteLine(" )");

                    indent(shift);
                    Console.WriteLine("{");

                    for ( int i=0, n=swtch.Cases.Length; i<n; i++ )
                    {
                        indent(shift+ind);
                        if ( swtch.Cases[i].Label != null )
                        {
                            Console.Write("case ");
                            report(swtch.Cases[i].Label,0);
                            Console.WriteLine(":");
                        }
                        else
                        {
                            Console.WriteLine("default:");
                        }
                        report(swtch.Cases[i].Body,shift+ind);
                    }
                    indent(shift);
                    Console.WriteLine("}");

                    break;
                }

                case NodeType.Throw :
                {
                    Throw thro = node as Throw;

                    indent(shift);
                    Console.Write("throw (");
                    report(thro.Expression,0);
                    Console.Write(");");
                    break;
                }

                case NodeType.Exit :
                {
                    indent(shift);
                    Console.WriteLine("exit;");
                    break;
                }

                case NodeType.Continue:
                {
                    indent(shift);
                    Console.WriteLine("continue;");
                    break;
                }

                case NodeType.Try :
                {
                    Try trys = node as Try;

                    indent(shift);
                    Console.WriteLine("try {");
                    report(trys.TryBlock,shift+ind);
                    indent(shift);
                    Console.WriteLine("}");
                    if(trys.Catchers != null)
                    for ( int i=0, n=trys.Catchers.Length; i<n; i++ )
                    {
                        indent(shift);
                        if ( trys.Catchers[i].Type != null )
                            Console.Write("catch ( {0} ",trys.Catchers[i].Type.FullName);
                        else
                            Console.Write("catch ( ");
                        if ( trys.Catchers[i].Variable != null )
                            report(trys.Catchers[i].Variable,0);
                        Console.WriteLine(" ) {");
                        report(trys.Catchers[i].Block,shift+ind);
                        indent(shift);
                        Console.WriteLine("}");
                    }
                    if ( trys.Finally != null && trys.Finally.Block != null )
                    {
                        indent(shift);
                        Console.WriteLine("finally");
                        report(trys.Finally.Block,shift);
                    }
                    break;
                }

                case NodeType.BlockExpression :
                {
                    BlockExpression be = node as BlockExpression;

                    Console.WriteLine("(");
                    StatementList sl = be.Block.Statements;
                    for ( int i=0, n=sl.Length; i<n; i++ )
                    {
                        report(sl[i],shift+ind);
                    }
                    indent(shift);
                    Console.Write(")");
                    break;
                }

                case NodeType.ArrayTypeExpression :
                {
                    ArrayTypeExpression array = node as ArrayTypeExpression;

                    indent(shift);

                    if ( array.ElementType != null &&
                         array.ElementType.Name != null &&
                         array.ElementType.Name.Name != null )
                        Console.Write(array.ElementType.Name.Name);
                    else
                        report(array.ElementType,0);

                    Console.Write("[");
                    for ( int i=0, n=array.Rank; i<n; i++ )
                    {
                        if ( array.Sizes != null ) Console.Write(array.Sizes[i]);
                        if ( i < n-1 ) Console.Write(",");
                    }
                    Console.Write("]");

                    break;
                }

                case NodeType.Construct :
                {
                    Construct construct = node as Construct;

                    indent(shift);
                    Console.Write("new ");
                    report(construct.Constructor,0);
                    Console.Write("(");
                    if (construct.Operands != null)
                    {
                        for (int i = 0, n = construct.Operands.Length; i < n; i++)
                        {
                            report(construct.Operands[i], 0);
                            if (i < n - 1) Console.Write(",");
                        }
                    }
                    Console.Write(")");
                    break;
                }

                case NodeType.ConstructArray :
                {
                    ConstructArray initializer = node as ConstructArray;

                    Console.Write("new ");

                    if ( initializer.ElementType != null &&
                         initializer.ElementType.Name != null &&
                         initializer.ElementType.Name.Name != null )
                        Console.Write(initializer.ElementType.Name.Name);
                    else
                        report(initializer.ElementType,0);

                    Console.Write("[");
                    for ( int i=0, n=initializer.Operands.Length; i<n; i++ )
                    {
                        report(initializer.Operands[i],0);
                        if ( i < n-1 ) Console.Write(",");
                    }
                    Console.Write("]");

                    break;
                }

                case NodeType.ConstructDelegate :
                {
                    ConstructDelegate cd = node as ConstructDelegate;

                 // Console.Write("new {0}({1})",cd.DelegateType.Name.Name,cd.MethodName.Name);
                    Console.Write("new {0}(",cd.DelegateType.Name.Name);
                    report(cd.TargetObject,0);
                    Console.Write(".{0})",cd.MethodName.Name);
                 // cd.Type;
                    break;
                }

                default :
                {
                    if ( node is ZonnonCompilation )
                    {
                        ZonnonCompilation zc = node as ZonnonCompilation;
                        report(zc.CompilationUnits[0],shift);
                    }
                    // Expression?

                    else if ( node is MethodCall )
                    {
                        MethodCall call = node as MethodCall;

                        report(call.Callee,0);
                        Console.Write("(");

                        if ( call.Operands != null && call.Operands.Length != 0 )
                        {
                            for ( int i=0, n=call.Operands.Length; i<n; i++ )
                            {
                                report(call.Operands[i],0);
                                if ( i < n-1 ) Console.Write(",");
                            }
                        }

                        Console.Write(")");
                    }
                    else if ( node is Variable )
                    {
                        Variable variable = node as Variable;
                        Console.Write("{0}",variable.Name.Name);
                    }
                    else if ( node is Identifier )
                    {
                        Identifier identifier = node as Identifier;
                        Console.Write("{0}",identifier.Name);
                    }
                    else if ( node is QualifiedIdentifier )
                    {
                        QualifiedIdentifier qualid = node as QualifiedIdentifier;
                        report(qualid.Qualifier,0);
                        Console.Write(".{0}",qualid.Identifier==null?"<UNRESOLVED>":qualid.Identifier.Name);
                    }
                    else if ( node is Literal )
                    {
                        Literal literal = node as Literal;
                        if ( literal.Value == null )
                            Console.Write("null");
                        else
                        {
                            if ( literal.Value is string ) Console.Write("\"");
                            else if ( literal.Value is char ) Console.Write("'");
                            Console.Write("{0}",literal.Value.ToString());
                            if ( literal.Value is string ) Console.Write("\"");
                            else if ( literal.Value is char ) Console.Write("'");
                        }
                    }
                    else if ( node is Indexer )
                    {
                        Indexer indexer = node as Indexer;
                        report(indexer.Object,0);
                        Console.Write("[");
                        for ( int i=0, n=indexer.Operands.Length; i<n; i++ )
                        {
                            report(indexer.Operands[i],0);
                            if ( i < n-1 ) Console.Write(",");
                        }
                        Console.Write("]");
                    }
                    else if ( node is UnaryExpression )
                    {
                        UnaryExpression unexpr = node as UnaryExpression;

                        bool add_pars = unexpr.Operand is BinaryExpression ||
                                        unexpr.Operand is UnaryExpression;

                        switch ( unexpr.NodeType )
                        {
                            case NodeType.Add : Console.Write("+");  break;
                            case NodeType.Sub : Console.Write("-");  break;
                            case NodeType.Neg : Console.Write("-");  break;
                            case NodeType.Not : Console.Write("~");  break;
                            case NodeType.UnaryPlus: Console.Write("+"); break;
                            case NodeType.LogicalNot : Console.Write("!"); break;

                            case NodeType.Conv_U2 : Console.Write("(UInt16)"); break;
                            case NodeType.RefAddress : Console.Write("ref "); break;
                            case NodeType.Ckfinite : Console.Write("(Ckfinite)"); break;
                            default :           Console.Write("???");  break;
                        }

                        if ( add_pars ) Console.Write("(");
                        report(unexpr.Operand,0);
                        if ( add_pars ) Console.Write(")");
                    }
                    else if ( node is BinaryExpression )
                    {
                        BinaryExpression binexpr = node as BinaryExpression;

                        bool add_pars = binexpr.Operand1 is BinaryExpression ||
                                        binexpr.Operand1 is UnaryExpression;

                        if ( binexpr.NodeType == NodeType.Castclass )
                        {
                            Console.Write("(");
                            report(binexpr.Operand2,0);
                            Console.Write(")");

                            if ( add_pars ) Console.Write("(");
                            report(binexpr.Operand1,0);
                            if ( add_pars ) Console.Write(")");
                            break;
                        }

                        if ( add_pars ) Console.Write("(");
                        report(binexpr.Operand1,0);
                        if ( add_pars ) Console.Write(")");

                        switch ( binexpr.NodeType )
                        {
                            case NodeType.Add: Console.Write(" + "); break;
                            case NodeType.Add_Ovf: Console.Write(" + "); break;
                            case NodeType.Add_Ovf_Un: Console.Write(" + "); break;
                            case NodeType.Sub: Console.Write(" - "); break;
                            case NodeType.Sub_Ovf: Console.Write(" - "); break;
                            case NodeType.Sub_Ovf_Un: Console.Write(" - "); break;
                            case NodeType.Mul: Console.Write(" * "); break;
                            case NodeType.Mul_Ovf: Console.Write(" * "); break;
                            case NodeType.Mul_Ovf_Un: Console.Write(" * "); break;
                            case NodeType.Div: Console.Write(" / "); break;
                            // case NodeType.Div : Console.Write(" DIV "); break;  // "DIV" ?????
                            case NodeType.Rem: Console.Write(" % "); break;  // "MOD" ?????
                            case NodeType.Or: Console.Write(" | "); break;
                            case NodeType.And: Console.Write(" & "); break;
                            case NodeType.Eq: Console.Write(" == "); break;
                            case NodeType.Ne: Console.Write(" != "); break;
                            case NodeType.Lt: Console.Write(" < "); break;
                            case NodeType.Le: Console.Write(" <= "); break;
                            case NodeType.Gt: Console.Write(" > "); break;
                            case NodeType.Ge: Console.Write(" >= "); break;
                            case NodeType.LogicalOr: Console.Write(" || "); break;
                            case NodeType.LogicalAnd: Console.Write(" && "); break;
                            case NodeType.Is: Console.Write(" is "); break;
                            case NodeType.Comma: Console.Write(","); break;
                            // case OPERATORS.In           : expression.NodeType = NodeType  // "IN" break;
                            // case OPERATORS.Implements   : expression.NodeType = NodeType  // "IMPLEMENTS" break;
                            default: Console.Write(" !! "); break;
                        }

                        add_pars = binexpr.Operand2 is BinaryExpression ||
                                   binexpr.Operand2 is UnaryExpression;

                        if ( add_pars ) Console.Write("(");
                        report(binexpr.Operand2,0);
                        if ( add_pars ) Console.Write(")");
                    }
                    else if ( node is TernaryExpression )
                    {
                        TernaryExpression ter = node as TernaryExpression;
                        if ( ter.NodeType == NodeType.Conditional )
                        {
                            report(ter.Operand1,0);
                            Console.Write(" ? ");
                            report(ter.Operand2,0);
                            Console.Write(" : ");
                            report(ter.Operand3,0);
                        }
                    }
                    else if (node is PostfixExpression)
                    {
                        PostfixExpression postfixExpr = node as PostfixExpression;

                        report(postfixExpr.Expression, 0);

                        switch (postfixExpr.Operator)
                        {
                            case NodeType.Increment: Console.Write("++"); break;
                            case NodeType.Decrement: Console.Write("--"); break;
                            default: Console.Write("???"); break;
                        }
                    }
                    else if (node is LabeledStatement)
                    {
                        LabeledStatement lbst = node as LabeledStatement;
                        indent(shift);    
                        report(lbst.Label, 0);
                        Console.Write(" : ");
                        report(lbst.Statement, 0);
                      
                    }
                    else if (node is Goto)
                    {
                        Goto gt = node as Goto;
                        indent(shift);
                        Console.Write("goto ");
                        report(gt.TargetLabel, 0);
                    }
                    else
                    {
                        indent(shift);
                        Console.WriteLine("No code for reporting {0}:{1}",node.UniqueKey,node.ToString());
                    }
                    break;
                }
            }
        }

        private static void uname ( int shift, string name, Member member )
        {
            indent(shift);
            if ( member == null )
                Console.WriteLine("{0} NULL",name);
            else
                Console.WriteLine("{0} {1}:{2}",name,member.UniqueKey,member.Name);
        }

        private static void uname ( string name, Member member )
        {
            if ( member == null )
                Console.WriteLine("{0} NULL",name);
            else
                Console.Write("{0} {1}:{2}",name,member.UniqueKey,member.Name);
        }

        private static void uname ( string name, Module module )
        {
            if ( module == null )
                Console.WriteLine("{0} NULL",name);
            else
                Console.Write("{0} {1}:{2}",name,module.UniqueKey,module.Name);
        }

        private static void sh_text ( int shift, string text )
        {
            indent(shift);
            Console.Write(text);
        }

        private static void text ( string txt )
        {
            Console.Write(txt);
        }
	}
}
