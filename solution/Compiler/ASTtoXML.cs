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
using System.Xml;

namespace ETH.Zonnon
{
    //OLD:
    //public override void generateXML(XmlNode parent, XmlDocument docSource, string compilationFileName)

    public sealed class ASTtoXML: ASTVisitor
    {
        private XmlNode parent;
        private XmlDocument docSource;
        private string compilationFileName;

        public ASTtoXML(XmlNode parent, XmlDocument docSource, string compilationFileName)
        {
            this.parent = parent;
            this.docSource = docSource;
            this.compilationFileName = compilationFileName;
        }

        private void VisitInContext(NODE node, XmlNode context)
        {
            XmlNode save = parent; 
            parent = context; // Sent new context
            Visit(node);
            parent = save; // Restore context

        }

        protected override void Visit_STATEMENT_LIST(STATEMENT_LIST node)
        {
            /* not implemented */

        }
        protected override void Visit_IDENT_LIST(IDENT_LIST node)
        {
            /* not implemented */

        }
        protected override void Visit_COMMENT(COMMENT node)
        {				
			if (node.sourceContext.Document.Name.ToString().EndsWith((compilationFileName)))
			{
				XmlNode comment = docSource.CreateNode(XmlNodeType.Element, "comment", "");
				comment.Attributes.Append(docSource.CreateAttribute("id"));
				comment.Attributes["id"].Value = "i" + node.unique.ToString();
				comment.Attributes.Append(docSource.CreateAttribute("line"));
				comment.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
				comment.Attributes.Append(docSource.CreateAttribute("endline"));
				comment.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
				string str = node.str;
				int brPos = -2;
				int oldPos=0;
				do 
				{
					oldPos = brPos+2;
					brPos = str.IndexOf("\n", oldPos);
					XmlNode commentLine = docSource.CreateNode(XmlNodeType.Element, "commentline", "");
					if ((brPos < str.Length) && (brPos > 0) && (brPos-oldPos-1 > 0))
						commentLine.InnerText = str.Substring(oldPos, brPos-oldPos-1);
					else
						if (oldPos<str.Length-2)
							commentLine.InnerText = str.Substring(oldPos, str.Length-1-oldPos);
					int t = commentLine.InnerText.IndexOf("\n");
					if (t < 0)
					{
						if (commentLine.InnerText.Length > 0)
							comment.AppendChild(commentLine);
					}
					if ((brPos >= str.Length-1) || (brPos <= 0))
						break;
				}
				while (brPos > -1);
				
				// only store comments that contain information
				if (comment.ChildNodes.Count > 0)
					parent.AppendChild(comment);
			}

        }
        protected override void Visit_COMMENT_LIST(COMMENT_LIST node)
        {
            /* not implemented */

        }
        protected override void Visit_IF_PAIR(IF_PAIR node)
        {
            XmlNode ifclause = docSource.CreateNode(XmlNodeType.Element, "ifclause", "");
            ifclause.Attributes.Append(docSource.CreateAttribute("line"));
            ifclause.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            ifclause.Attributes.Append(docSource.CreateAttribute("endline"));
            ifclause.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.condition != null)
            {
                XmlNode conditionnode = docSource.CreateNode(XmlNodeType.Element, "condition", "");
                
                VisitInContext(node.condition, conditionnode);

                ifclause.AppendChild(conditionnode);
            }

            if (node.statements.Length != 0)
                for (int i = 0, n = node.statements.Length; i < n; i++)
                {
                    VisitInContext(node.statements[i], ifclause);
                }

            parent.AppendChild(ifclause);


        }
        protected override void Visit_CASE_ITEM(CASE_ITEM node)
        {
            XmlNode caseitem = docSource.CreateNode(XmlNodeType.Element, "caseitem", "");
            caseitem.Attributes.Append(docSource.CreateAttribute("line"));
            caseitem.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            caseitem.Attributes.Append(docSource.CreateAttribute("endline"));
            caseitem.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            for (int i = 0, n = node.getRangeCount(); i < n; i++)
            {
                VisitInContext(node.getRange(i), caseitem);
            }
            if (node.getStatementsCount() > 0)
            {
                for (int i = 0, n = node.getStatementsCount(); i < n; i++)
                {
                    VisitInContext(node.getStmt(i), caseitem);
                }
            }

            parent.AppendChild(caseitem);


        }
        protected override void Visit_RANGE(RANGE node)
        {
            XmlNode range = docSource.CreateNode(XmlNodeType.Element, "range", "");
            range.Attributes.Append(docSource.CreateAttribute("line"));
            range.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            range.Attributes.Append(docSource.CreateAttribute("endline"));
            range.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.left_border != null)
            {
                XmlNode leftborder = docSource.CreateNode(XmlNodeType.Element, "leftborder", "");
                VisitInContext(node.left_border, leftborder);
                range.AppendChild(leftborder);
            }

            if (node.right_border != null)
            {
                XmlNode rightborder = docSource.CreateNode(XmlNodeType.Element, "leftborder", "");
                VisitInContext(node.right_border, rightborder);
                range.AppendChild(rightborder);
            }

            parent.AppendChild(range);


        }
        protected override void Visit_EXCEPTION(EXCEPTION node)
        {
            XmlNode exception = docSource.CreateNode(XmlNodeType.Element, "exception", "");
            exception.Attributes.Append(docSource.CreateAttribute("line"));
            exception.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            exception.Attributes.Append(docSource.CreateAttribute("endline"));
            exception.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            if (node.types.Length > 0)
            {
                XmlNode typesnode = docSource.CreateNode(XmlNodeType.Element, "types", "");
                for (int i = 0, n = node.types.Length; i < n; i++)
                {
                    XmlNode externaltype = docSource.CreateNode(XmlNodeType.Element, "externaltype", "");
                    externaltype.Attributes.Append(docSource.CreateAttribute("name"));
                    externaltype.Attributes["name"].Value = node.types[i].name.ToString();
                    typesnode.AppendChild(externaltype);
                }
                exception.AppendChild(typesnode);
            }
            for (int i = 0, n = node.statements.Length; i < n; i++)
            {
                VisitInContext(node.statements[i], exception);
            }


        }

        #region TYPE
        protected override void Visit_ANY_TYPE(ANY_TYPE node)
        {
            // Anytype should not apear in a resolved program any more

        }
        protected override void Visit_UNKNOWN_TYPE(UNKNOWN_TYPE node)
        {
            if (node.real_type != null)
            {
                Visit(node.real_type);
            }
            else
            {
                XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                objecttype.Attributes["name"].Value = node.unknown.name.Name;

                parent.AppendChild(objecttype);
            }			


        }
        protected override void Visit_ARRAY_TYPE(ARRAY_TYPE node)
        {
            XmlNode array = docSource.CreateNode(XmlNodeType.Element, "array", "");
            if (node.base_type is UNKNOWN_TYPE)
                node.resolve();
            for (int i = 0, n = node.dimensions.Length; i < n; i++)
            {
                XmlNode dimension = docSource.CreateNode(XmlNodeType.Element, "dimension", "");
                dimension.Attributes.Append(docSource.CreateAttribute("isopen"));

                if (node.dimensions[i] != null)
                {
                    dimension.Attributes["isopen"].Value = "false";
                    VisitInContext(node.dimensions[i], dimension);
                }
                else
                {
                    dimension.Attributes["isopen"].Value = "true";
                }
                array.AppendChild(dimension);
            }

            if (node.base_type != null) // because of an error
            {
                if (node.base_type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.base_type, node);

                    if (typeName != null)
                    {
                        XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                        objecttype.Attributes["name"].Value = typeName;
                        objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
                        objecttype.Attributes["ref"].Value = ((OBJECT_TYPE)node.base_type).ObjectUnit.generateXPath();
                        array.AppendChild(objecttype);
                    }
                    else
                    {
                        VisitInContext(node.base_type, array);
                    }
                }

                else if (node.base_type is INTERFACE_TYPE)
                {
                    XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");
                    if (((INTERFACE_TYPE)node.base_type).interfaces != null && ((INTERFACE_TYPE)node.base_type).interfaces.Length > 0)
                    {
                        string typeName;
                        for (int i = 0, n = ((INTERFACE_TYPE)node.base_type).interfaces.Length; i < n; i++)
                        {
                            typeName = DECLARATION.resolveInterfaceTypeNameInDeclaration((INTERFACE_TYPE)node.base_type, ((INTERFACE_TYPE)node.base_type).interfaces[i], node);

                            if (typeName != null)
                            {
                                XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                                implements.Attributes.Append(docSource.CreateAttribute("name"));
                                implements.Attributes["name"].Value = typeName;
                                implements.Attributes.Append(docSource.CreateAttribute("ref"));
                                implements.Attributes["ref"].Value = ((INTERFACE_TYPE)node.base_type).interfaces[i].generateXPath();

                                interfacetype.AppendChild(implements);
                            }
                        }
                    }
                    array.AppendChild(interfacetype);
                }
                else if (node.base_type.enclosing is TYPE_DECL)
                {
                    if (!(NODE.isBuiltIn(node.base_type.enclosing.name.ToString())))
                    {
                        string typeName = DECLARATION.resolveTypeTypeNameInDeclaration((TYPE)node.base_type, node);

                        XmlNode type = docSource.CreateNode(XmlNodeType.Element, "type", "");
                        type.Attributes.Append(docSource.CreateAttribute("name"));
                        type.Attributes["name"].Value = typeName; //base_type.enclosing.name.ToString();
                        type.Attributes.Append(docSource.CreateAttribute("ref"));
                        type.Attributes["ref"].Value = node.base_type.enclosing.generateXPath();

                        array.AppendChild(type);
                    }
                    else
                    {
                        VisitInContext(node.base_type, array);
                    }
                }
                else if (node.base_type.enclosing is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, array);
                }
                else
                {
                    VisitInContext(node.base_type, array);
                }
            }
            parent.AppendChild(array);


        }
        protected override void Visit_SET_TYPE(SET_TYPE node)
        {
            XmlNode settype = docSource.CreateNode(XmlNodeType.Element, "settype", "");
            //if (width != 32)
            //{
            XmlNode widthnode = docSource.CreateNode(XmlNodeType.Element, "width", "");
            XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");
            integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
            integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
            integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
            integerliteral.Attributes["value"].Value = node.width.ToString();
            widthnode.AppendChild(integerliteral);
            settype.AppendChild(widthnode);
            //}
            parent.AppendChild(settype);


        }
        protected override void Visit_RANGE_TYPE(RANGE_TYPE node)
        {
            XmlNode settype = docSource.CreateNode(XmlNodeType.Element, "rangetype", "");

            parent.AppendChild(settype);


        }
        protected override void Visit_INTERFACE_TYPE(INTERFACE_TYPE node)
        {
            XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");
            for (int i = 0, n = node.interfaces.Length; i < n; i++)
            {
                DECLARATION interfac = node.interfaces[i];
                if (interfac != null)
                {
                    XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");

                    implements.Attributes.Append(docSource.CreateAttribute("name"));
                    implements.Attributes["name"].Value = node.interfaces[i].name.ToString();
                    interfacetype.AppendChild(implements);
                }
            }
            parent.AppendChild(interfacetype);


        }
        protected override void Visit_OBJECT_TYPE(OBJECT_TYPE node)
        {
            XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
            objecttype.Attributes.Append(docSource.CreateAttribute("name"));
            objecttype.Attributes["name"].Value = node.ObjectUnit.name.ToString(); //NODE.generateFullName(object_unit);
            objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
            objecttype.Attributes["ref"].Value = node.ObjectUnit.generateXPath();
            parent.AppendChild(objecttype);


        }
        protected override void Visit_ACTIVITY_TYPE(ACTIVITY_TYPE node)
        {
            XmlNode activitytype = docSource.CreateNode(XmlNodeType.Element, "activitytype", "");
            activitytype.Attributes.Append(docSource.CreateAttribute("name"));
            activitytype.Attributes["name"].Value = NODE.generateFullName(node.activity);
            activitytype.Attributes.Append(docSource.CreateAttribute("ref"));
            activitytype.Attributes["ref"].Value = node.activity.generateXPath();
            parent.AppendChild(activitytype);


        }
        protected override void Visit_ABSTRACT_ACTIVITY_TYPE(ABSTRACT_ACTIVITY_TYPE node)
        {
            XmlNode abstractactivitytype = docSource.CreateNode(XmlNodeType.Element, "abstractactivitytype", "");
            abstractactivitytype.Attributes.Append(docSource.CreateAttribute("name"));
            if (node.protocol != null)
            {
                abstractactivitytype.Attributes["name"].Value = node.protocol.name.ToString();
                abstractactivitytype.Attributes.Append(docSource.CreateAttribute("ref"));
                abstractactivitytype.Attributes["ref"].Value = node.protocol.generateXPath();
            }
            parent.AppendChild(abstractactivitytype);


        }
        protected override void Visit_INTEGER_TYPE(INTEGER_TYPE node)
        {
            XmlNode integernode = docSource.CreateNode(XmlNodeType.Element, "integer", "");
            if (node.width != 32)
            {
                XmlNode widthnode = docSource.CreateNode(XmlNodeType.Element, "width", "");
                XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");
                integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
                integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
                integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
                integerliteral.Attributes["value"].Value = node.width.ToString();
                widthnode.AppendChild(integerliteral);
                integernode.AppendChild(widthnode);
            }
            parent.AppendChild(integernode);


        }
        protected override void Visit_REAL_TYPE(REAL_TYPE node)
        {
            XmlNode real = docSource.CreateNode(XmlNodeType.Element, "real", "");
            if (node.width != 32)
            {
                XmlNode widthnode = docSource.CreateNode(XmlNodeType.Element, "width", "");
                XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");
                integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
                integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
                integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
                integerliteral.Attributes["value"].Value = node.width.ToString();
                widthnode.AppendChild(integerliteral);
                real.AppendChild(widthnode);
            }
            parent.AppendChild(real);


        }
        protected override void Visit_FIXED_TYPE(FIXED_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_CHAR_TYPE(CHAR_TYPE node)
        {
            XmlNode charnode = docSource.CreateNode(XmlNodeType.Element, "chartype", "");
            if (node.width != 8)
            {
                XmlNode widthnode = docSource.CreateNode(XmlNodeType.Element, "width", "");
                XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");
                // There is some exception thrown in some projects, when trying to attach
                // line/endline int node typedeclaration...
                /*
                integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
                integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
                integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
                */
                integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
                integerliteral.Attributes["value"].Value = node.width.ToString();
                widthnode.AppendChild(integerliteral);
                charnode.AppendChild(widthnode);
            }
            parent.AppendChild(charnode);


        }
        protected override void Visit_STRING_TYPE(STRING_TYPE node)
        {
            XmlNode stringnode = docSource.CreateNode(XmlNodeType.Element, "string", "");
            parent.AppendChild(stringnode);


        }
        protected override void Visit_CARDINAL_TYPE(CARDINAL_TYPE node)
        {
            XmlNode cardinal = docSource.CreateNode(XmlNodeType.Element, "cardinal", "");
            if (node.width != 8)
            {
                XmlNode widthnode = docSource.CreateNode(XmlNodeType.Element, "width", "");
                XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");
                integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
                integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
                integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
                integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
                integerliteral.Attributes["value"].Value = node.width.ToString();
                widthnode.AppendChild(integerliteral);
                cardinal.AppendChild(widthnode);
            }
            parent.AppendChild(cardinal);


        }
        protected override void Visit_BOOLEAN_TYPE(BOOLEAN_TYPE node)
        {
            XmlNode boolean = docSource.CreateNode(XmlNodeType.Element, "boolean", "");
            parent.AppendChild(boolean);


        }
        protected override void Visit_VOID_TYPE(VOID_TYPE node)
        {
            XmlNode voidnode = docSource.CreateNode(XmlNodeType.Element, "void", "");
            parent.AppendChild(voidnode);			


        }
        protected override void Visit_ENUM_TYPE(ENUM_TYPE node)
        {
            XmlNode enumnode = docSource.CreateNode(XmlNodeType.Element, "enum", "");
            for (int i = 0, n = node.enumerators.Length; i < n; i++)
            {
                VisitInContext(node.enumerators[i], enumnode);                
            }
            parent.AppendChild(enumnode);


        }
        protected override void Visit_PROC_TYPE(PROC_TYPE node)
        {
            /* MOVE CODE HERE */

        }
        protected override void Visit_EXTERNAL_TYPE(EXTERNAL_TYPE node)
        {
            string typeName = node.entity.ToString();
            typeName = typeName.Replace("+", ".");
            XmlNode externaltype = docSource.CreateNode(XmlNodeType.Element, "externaltype", "");
            externaltype.Attributes.Append(docSource.CreateAttribute("name"));
            externaltype.Attributes["name"].Value = typeName;
            parent.AppendChild(externaltype);


        }
        #endregion
        #region EXPRESSION
        #region UNARY
        private void Visit_UNARY(UNARY node)
        {
            VisitInContext(node.operand, node.xmlUnaryNode);

        }
        protected override void Visit_NEGATION(NEGATION node)
        {
            node.xmlUnaryNode = docSource.CreateNode(XmlNodeType.Element, "unaryoperator", "");
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlUnaryNode.Attributes["operator"].Value = "~";
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlUnaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlUnaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            Visit_UNARY(node);
            parent.AppendChild(node.xmlUnaryNode);


        }
        protected override void Visit_UNARY_MINUS(UNARY_MINUS node)
        {
            node.xmlUnaryNode = docSource.CreateNode(XmlNodeType.Element, "unaryoperator", "");
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlUnaryNode.Attributes["operator"].Value = "-";
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlUnaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlUnaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_UNARY(node);

            parent.AppendChild(node.xmlUnaryNode);

        }
        protected override void Visit_UNARY_PLUS(UNARY_PLUS node)
        {
            node.xmlUnaryNode = docSource.CreateNode(XmlNodeType.Element, "unaryoperator", "");
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlUnaryNode.Attributes["operator"].Value = "+";
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlUnaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlUnaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_UNARY(node);

            parent.AppendChild(node.xmlUnaryNode);


        }
        protected override void Visit_TYPE_CONV(TYPE_CONV node)
        {
            node.xmlUnaryNode = docSource.CreateNode(XmlNodeType.Element, "typeconversion", "");
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlUnaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlUnaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.type != null)
            {
                VisitInContext(node.type, node.xmlUnaryNode);
            }
            Visit_UNARY(node);
            parent.AppendChild(node.xmlUnaryNode);


        }
        #endregion
        #region BINARY
        private void Visit_BINARY(BINARY node)
        {
            if (node.left_operand != null)
            {
                VisitInContext(node.left_operand, node.xmlBinaryNode);
            }
            if (node.right_operand != null)
            {
                VisitInContext(node.right_operand, node.xmlBinaryNode);
            }

        }

        protected override void Visit_ASSIGNMENT_OPERATOR(ASSIGNMENT_OPERATOR node)
        {
            Visit_BINARY(node);            

        }
        protected override void Visit_PLUS(PLUS node)
        {
            Visit_BINARY(node);            

        }
        protected override void Visit_MINUS(MINUS node)
        {
            Visit_BINARY(node);            
        }
        protected override void Visit_MULTIPLY(MULTIPLY node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "*";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
       protected override void Visit_DIVIDE(DIVIDE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "/";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
       protected override void Visit_DIVIDE_ELEMENTWISE(DIVIDE_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "./";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
       protected override void Visit_MULTIPLY_ELEMENTWISE(MULTIPLY_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".*";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);
            
        }
       protected override void Visit_PSEUDO_SCALAR_PRODUCT(PSEUDO_SCALAR_PRODUCT node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "+*";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
        protected override void Visit_TRANSPOSE(TRANSPOSE node)
        {
            node.xmlUnaryNode = docSource.CreateNode(XmlNodeType.Element, "unaryoperator", "");
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlUnaryNode.Attributes["operator"].Value = "'";
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlUnaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlUnaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlUnaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_UNARY(node);

            parent.AppendChild(node.xmlUnaryNode);
            
        }
       protected override void Visit_EXPR_ARRAY_ASSIGNMENT(EXPR_ARRAY_ASSIGNMENT node)
        {
            XmlNode assignment = docSource.CreateNode(XmlNodeType.Element, "assignment", "");
            assignment.Attributes.Append(docSource.CreateAttribute("line"));
            assignment.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            assignment.Attributes.Append(docSource.CreateAttribute("endline"));
            assignment.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.receiver != null)
            {
                XmlNode receivernode = docSource.CreateNode(XmlNodeType.Element, "receiver", "");
                Visit(node.receiver);

                assignment.AppendChild(receivernode);
            }

            //  right_part.generateXML(assignment, docSource, compilationFileName);

            parent.AppendChild(assignment);

        }
       protected override void Visit_RANGESTEP(RANGESTEP node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "\\";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_ARRAY_RANGE(ARRAY_RANGE node)
        {
            //node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            //node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            //node.xmlBinaryNode.Attributes["operator"].Value = "\\";
            //node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            //node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            //node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            //node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            //Visit_BINARY(node);

            //parent.AppendChild(node.xmlBinaryNode);

            XmlNode array_range = docSource.CreateNode(XmlNodeType.Element, "array range", "");
            array_range.Attributes.Append(docSource.CreateAttribute("line"));
            array_range.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            array_range.Attributes.Append(docSource.CreateAttribute("endline"));
            array_range.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(array_range);

        }
        protected override void Visit_LEFTDIVISION(LEFTDIVISION node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "\\";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_DIV(DIV node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "div";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
        protected override void Visit_MOD(MOD node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "mod";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_EXPONENT(EXPONENT node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "**";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_AND(AND node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "&"; //"&amp;";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_OR(OR node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "or";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_BINARY(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        #region RELATION
        private void Visit_RELATION(RELATION node)
        {
            Visit_BINARY(node);
        }
        protected override void Visit_EQUAL(EQUAL node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_NON_EQUAL(NON_EQUAL node)
        {
            node.resolveOperator();
            node.resolve();

            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "#";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_LESS(LESS node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "<"; //"&lt;";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_LESS_EQUAL(LESS_EQUAL node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "<="; //"&lt;=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);            
        }
        protected override void Visit_GREATER(GREATER node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ">"; //"&gt;";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_GREATER_EQUAL(GREATER_EQUAL node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ">="; //"&gt;=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_EQUAL_ELEMENTWISE(EQUAL_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_NON_EQUAL_ELEMENTWISE(NON_EQUAL_ELEMENTWISE node)
        {
            node.resolveOperator();
            node.resolve();

            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".#";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
        protected override void Visit_LESS_ELEMENTWISE(LESS_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".<"; //"&lt;";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_LESS_EQUAL_ELEMENTWISE(LESS_EQUAL_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".<="; //"&lt;=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_GREATER_ELEMENTWISE(GREATER_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".>"; //"&gt;";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);
        }
        protected override void Visit_GREATER_EQUAL_ELEMENTWISE(GREATER_EQUAL_ELEMENTWISE node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = ".>="; //"&gt;=";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);

        }
        protected override void Visit_IN(IN node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "in";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_IMPLEMENTS(IMPLEMENTS node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "implements";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        protected override void Visit_IS(IS node)
        {
            node.xmlBinaryNode = docSource.CreateNode(XmlNodeType.Element, "binaryoperator", "");
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("operator"));
            node.xmlBinaryNode.Attributes["operator"].Value = "is";
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlBinaryNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlBinaryNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlBinaryNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            Visit_RELATION(node);

            parent.AppendChild(node.xmlBinaryNode);


        }
        #endregion
        #endregion
        #region DESIGNATOR
        protected override void Visit_DEREFERENCE(DEREFERENCE node)
        {
            //TODO

        }
        protected override void Visit_INDEXER(INDEXER node)
        {
            XmlNode indexer = docSource.CreateNode(XmlNodeType.Element, "indexer", "");
            indexer.Attributes.Append(docSource.CreateAttribute("line"));
            indexer.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            indexer.Attributes.Append(docSource.CreateAttribute("endline"));
            indexer.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.left_part != null)
            {
                VisitInContext(node.left_part, indexer);
            }

            XmlNode indicesnode = docSource.CreateNode(XmlNodeType.Element, "indices", "");
            for (int i = 0, n = node.indices.Length; i < n; i++)
                if (node.indices[i] != null)
                {
                    VisitInContext(node.indices[i], indicesnode);
                }

            indexer.AppendChild(indicesnode);
            parent.AppendChild(indexer);


        }
        protected override void Visit_SELECTOR(SELECTOR node)
        {
            node.resolve();
            XmlNode selector = docSource.CreateNode(XmlNodeType.Element, "selector", "");
            selector.Attributes.Append(docSource.CreateAttribute("line"));
            selector.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            selector.Attributes.Append(docSource.CreateAttribute("endline"));
            selector.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            VisitInContext(node.left_part, selector);

            if ((node.member is OBJECT_DECL) || (node.member is DEFINITION_DECL) || (node.member is IMPLEMENTATION_DECL) || (node.member is MODULE_DECL))
            {
                XmlNode instance = docSource.CreateNode(XmlNodeType.Element, "instance", "");
                instance.Attributes.Append(docSource.CreateAttribute("name"));
                instance.Attributes["name"].Value = node.member.name.ToString();
                instance.Attributes.Append(docSource.CreateAttribute("ref"));
                instance.Attributes["ref"].Value = node.member.generateXPath();
                instance.Attributes.Append(docSource.CreateAttribute("line"));
                instance.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                instance.Attributes.Append(docSource.CreateAttribute("endline"));
                instance.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                selector.AppendChild(instance);
            }
            else if ((node.member is ROUTINE_DECL) || (node.member is FIELD_DECL) || (node.member is VARIABLE_DECL) || (node.member is CONSTANT_DECL) || (node.member is TYPE_DECL) || (node.member is PROTOCOL_DECL))
            {
                XmlNode instance = docSource.CreateNode(XmlNodeType.Element, "instance", "");
                instance.Attributes.Append(docSource.CreateAttribute("name"));
                instance.Attributes["name"].Value = node.member.name.ToString();
                instance.Attributes.Append(docSource.CreateAttribute("ref"));
                instance.Attributes["ref"].Value = node.member.generateXPath();
                instance.Attributes.Append(docSource.CreateAttribute("line"));
                instance.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                instance.Attributes.Append(docSource.CreateAttribute("endline"));
                instance.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                selector.AppendChild(instance);
            }
            else if (node.member is ENUMERATOR_DECL)
            {
                XmlNode enumerator = docSource.CreateNode(XmlNodeType.Element, "enumerator", "");
                enumerator.Attributes.Append(docSource.CreateAttribute("name"));
                enumerator.Attributes["name"].Value = node.member.name.ToString();
                enumerator.Attributes.Append(docSource.CreateAttribute("ref"));
                enumerator.Attributes["ref"].Value = node.member.generateXPath();
                enumerator.Attributes.Append(docSource.CreateAttribute("line"));
                enumerator.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                enumerator.Attributes.Append(docSource.CreateAttribute("endline"));
                enumerator.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                selector.AppendChild(enumerator);
            }
            else if ((node.member is EXTERNAL_DECL) && (node.member.type != null))
            {
                XmlNode externaldeclaration = docSource.CreateNode(XmlNodeType.Element, "externaldeclaration", "");
                externaldeclaration.Attributes.Append(docSource.CreateAttribute("name"));
                externaldeclaration.Attributes.Append(docSource.CreateAttribute("line"));
                externaldeclaration.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                externaldeclaration.Attributes.Append(docSource.CreateAttribute("endline"));
                externaldeclaration.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                if (node.member.name != null)
                    externaldeclaration.Attributes["name"].Value = node.member.name.ToString();
                else
                {
                    string temp = ((EXTERNAL_DECL)node.member).entity.ToString();
                    string[] components = temp.Split('.');
                    externaldeclaration.Attributes["name"].Value = components[components.Length - 1];
                }

                selector.AppendChild(externaldeclaration);
            }
            else
            {
                VisitInContext(node.member, selector);
            }

            parent.AppendChild(selector);


        }
        protected override void Visit_SAFEGUARD(SAFEGUARD node)
        {
            XmlNode safeguard = docSource.CreateNode(XmlNodeType.Element, "safeguard", "");
            safeguard.Attributes.Append(docSource.CreateAttribute("line"));
            safeguard.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            safeguard.Attributes.Append(docSource.CreateAttribute("endline"));
            safeguard.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            if (node.real_selector != null)
            {
                VisitInContext(node.real_selector, safeguard);
            }
            parent.AppendChild(safeguard);


        }
        protected override void Visit_CALL(CALL node)
        {
            XmlNode call = docSource.CreateNode(XmlNodeType.Element, "call", "");
            call.Attributes.Append(docSource.CreateAttribute("line"));
            call.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            call.Attributes.Append(docSource.CreateAttribute("endline"));
            call.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.callee != null)
            {
                VisitInContext(node.callee, call);
            }

            if (node.arguments != null && node.arguments.Length > 0)
            {
                XmlNode argumentsnode = docSource.CreateNode(XmlNodeType.Element, "arguments", "");
                for (int i = 0, n = node.arguments.Length; i < n; i++)
                {
                    if (node.arguments[i] == null) continue;
                    if (node.callee is INSTANCE)
                    {
                        if ((((INSTANCE)node.callee).entity.name.ToString() == "max") || (((INSTANCE)node.callee).entity.name.ToString() == "min"))
                        {
                            if (node.arguments[i] is INTEGER_LITERAL)
                            {
                                XmlNode intnode = docSource.CreateNode(XmlNodeType.Element, "integer", "");
                                argumentsnode.AppendChild(intnode);
                            }
                        }
                        else
                        {
                            VisitInContext(node.arguments[i], argumentsnode);
                        }
                    }
                    else
                    {
                        VisitInContext(node.arguments[i], argumentsnode);
                    }
                }
                call.AppendChild(argumentsnode);
            }
            parent.AppendChild(call);


        }
        protected override void Visit_SET_CTOR(SET_CTOR node)
        {
            XmlNode setctor = docSource.CreateNode(XmlNodeType.Element, "setctor", "");
            setctor.Attributes.Append(docSource.CreateAttribute("line"));
            setctor.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            setctor.Attributes.Append(docSource.CreateAttribute("endline"));
            setctor.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            for (int i = 0, n = node.elements.Length; i < n; i++)
            {
                VisitInContext(node.elements[i], setctor);
            }
            parent.AppendChild(setctor);


        }
        protected override void Visit_NEW(NEW node)
        {
            XmlNode newnode = docSource.CreateNode(XmlNodeType.Element, "new", "");
            newnode.Attributes.Append(docSource.CreateAttribute("line"));
            newnode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            newnode.Attributes.Append(docSource.CreateAttribute("endline"));
            newnode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            if (node.new_type != null)
            {
                if (node.resolved_new_type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInExpression((OBJECT_TYPE)node.resolved_new_type, node);

                    if (typeName != null)
                    {
                        XmlNode objectNode = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objectNode.Attributes.Append(docSource.CreateAttribute("name"));
                        objectNode.Attributes["name"].Value = typeName;
                        objectNode.Attributes.Append(docSource.CreateAttribute("ref"));
                        objectNode.Attributes["ref"].Value = ((OBJECT_TYPE)node.resolved_new_type).ObjectUnit.generateXPath();
                        newnode.AppendChild(objectNode);
                    }
                    else
                    {
                        VisitInContext(node.new_type, newnode);
                    }
                }
                else if (node.resolved_new_type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, newnode);
                }
                else if (node.resolved_new_type is ACTIVITY_TYPE)
                {
                    if (((ACTIVITY_TYPE)node.resolved_new_type).activity.getEnclosingUnit() == node.getEnclosingUnit())
                    {
                        XmlNode activitytype = docSource.CreateNode(XmlNodeType.Element, "activitytype", "");

                        activitytype.Attributes.Append(docSource.CreateAttribute("name"));
                        activitytype.Attributes["name"].Value = ((ACTIVITY_TYPE)node.resolved_new_type).activity.name.ToString();
                        activitytype.Attributes.Append(docSource.CreateAttribute("ref"));
                        activitytype.Attributes["ref"].Value = ((ACTIVITY_TYPE)node.resolved_new_type).activity.generateXPath();

                        newnode.AppendChild(activitytype);
                    }
                    else
                    {
                        VisitInContext(node.resolved_new_type, newnode);
                    }
                }
                else if (node.new_type.enclosing is TYPE_DECL)
                {
                    string typeName = DECLARATION.resolveTypeTypeNameInDeclaration(node.resolved_new_type, node);

                    XmlNode type = docSource.CreateNode(XmlNodeType.Element, "type", "");
                    type.Attributes.Append(docSource.CreateAttribute("name"));
                    type.Attributes.Append(docSource.CreateAttribute("ref"));

                    if (typeName != null)
                    {
                        type.Attributes["name"].Value = typeName;
                        type.Attributes["ref"].Value = node.new_type.enclosing.generateXPath();
                        newnode.AppendChild(type);
                    }
                    else
                    {
                        type.Attributes["name"].Value = node.new_type.enclosing.name.ToString();
                        type.Attributes["ref"].Value = node.new_type.enclosing.generateXPath();
                        newnode.AppendChild(type);
                    }
                }
                else
                {
                    VisitInContext(node.resolved_new_type, newnode);
                }
            }

            if ((node.arguments != null) & (node.arguments.Length > 0))
            {
                XmlNode argumentsnode = docSource.CreateNode(XmlNodeType.Element, "arguments", "");
                if (node.arguments != null && node.arguments.Length > 0)
                    for (int i = 0, n = node.arguments.Length; i < n; i++)
                    {
                        VisitInContext(node.arguments[i], argumentsnode);
                    }
                newnode.AppendChild(argumentsnode);
            }

            parent.AppendChild(newnode);


        }
        protected override void Visit_SELF(SELF node)
        {
            XmlNode self = docSource.CreateNode(XmlNodeType.Element, "self", "");

            self.Attributes.Append(docSource.CreateAttribute("line"));
            self.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            self.Attributes.Append(docSource.CreateAttribute("endline"));
            self.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(self);


        }
        protected override void Visit_OBJECT(OBJECT node)
        {
            XmlNode obj = docSource.CreateNode(XmlNodeType.Element, "object", "");

            obj.Attributes.Append(docSource.CreateAttribute("line"));
            obj.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            obj.Attributes.Append(docSource.CreateAttribute("endline"));
            obj.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(obj);


        }
        protected override void Visit_INSTANCE(INSTANCE node)
        {
            if (node.entity is UNKNOWN_DECL)
                node.resolve();

            XmlNode instance = docSource.CreateNode(XmlNodeType.Element, "instance", "");
            instance.Attributes.Append(docSource.CreateAttribute("name"));
            instance.Attributes.Append(docSource.CreateAttribute("line"));
            instance.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            instance.Attributes.Append(docSource.CreateAttribute("endline"));
            instance.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if ((node.entity is UNIT_DECL) && !(node.entity is EXTERNAL_DECL))
            {
                instance.Attributes["name"].Value = NODE.generateFullName(node.entity);
                instance.Attributes.Append(docSource.CreateAttribute("ref"));
                instance.Attributes["ref"].Value = node.entity.generateXPath();
            }
            else
            {
                if (NODE.isBuiltIn(node.entity.name.ToString()))
                {
                    if (node.entity is EXTERNAL_DECL)
                    {
                        instance.Attributes["name"].Value = ((EXTERNAL_DECL)node.entity).entity.ToString();
                    }
                    else
                    {
                        instance.Attributes["name"].Value = node.entity.name.ToString();
                    }
                }
                else
                {
                    if (node.entity is EXTERNAL_DECL)
                    {
                        if (node.originalName == "")
                            instance.Attributes["name"].Value = ((EXTERNAL_DECL)node.entity).entity.ToString(); // + "..." + entity.name.ToString(); //((EXTERNAL_DECL)entity).entity.ToString();
                        else
                            instance.Attributes["name"].Value = node.entity.name.ToString(); //((EXTERNAL_DECL)entity).entity.ToString();
                    }
                    else
                    {
                        instance.Attributes["name"].Value = node.entity.name.ToString();
                        instance.Attributes.Append(docSource.CreateAttribute("ref"));
                        instance.Attributes["ref"].Value = node.entity.generateXPath();
                    }
                }
            }
            parent.AppendChild(instance);


        }
        #region LITERAL
        protected override void Visit_ENUMERATOR(ENUMERATOR node)
        {
            XmlNode enumnode = docSource.CreateNode(XmlNodeType.Element, "enumerator", "");

            enumnode.Attributes.Append(docSource.CreateAttribute("name"));
            enumnode.Attributes["name"].Value = node.enumerator.name.ToString();
            enumnode.Attributes.Append(docSource.CreateAttribute("value"));
            enumnode.Attributes["value"].Value = node.enumerator.val.ToString();
            enumnode.Attributes.Append(docSource.CreateAttribute("line"));
            enumnode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            enumnode.Attributes.Append(docSource.CreateAttribute("endline"));
            enumnode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(enumnode);


        }
        protected override void Visit_STRING_LITERAL(STRING_LITERAL node)
        {
            if (node.str != null)
            {
                string s = node.str;
                // Only \0 must be done here
                /*
				s = s.Replace("<", "&lt;");
                s = s.Replace(">", "&gt;");
                s = s.Replace("\"", "&quot;");
                s = s.Replace("'", "&#39;");
				*/
                if (s == "\0")
                    s = "0X";
                XmlNode stringliteral = docSource.CreateNode(XmlNodeType.Element, "stringliteral", "");

                stringliteral.Attributes.Append(docSource.CreateAttribute("value"));
                stringliteral.Attributes["value"].Value = s;
                stringliteral.Attributes.Append(docSource.CreateAttribute("line"));
                stringliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                stringliteral.Attributes.Append(docSource.CreateAttribute("endline"));
                stringliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                parent.AppendChild(stringliteral);
            }

        }
        protected override void Visit_CHAR_LITERAL(CHAR_LITERAL node)
        {
            string s = node.ch.ToString();
            // Obviously, special signs are replaced when working with .NET XML
            // classes - only the \0 character remains to be done here
            /*
            s = s.Replace("<", "&lt;");
            s = s.Replace(">", "&gt;");
            s = s.Replace("\"", "&quot;");
            s = s.Replace("'", "&#39;");
            */
            if (s == "\0")
                s = "0X";
            XmlNode charliteral = docSource.CreateNode(XmlNodeType.Element, "charliteral", "");

            charliteral.Attributes.Append(docSource.CreateAttribute("value"));
            charliteral.Attributes["value"].Value = s;
            charliteral.Attributes.Append(docSource.CreateAttribute("line"));
            charliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            charliteral.Attributes.Append(docSource.CreateAttribute("endline"));
            charliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(charliteral);


        }
        protected override void Visit_INTEGER_LITERAL(INTEGER_LITERAL node)
        {
            XmlNode integerliteral = docSource.CreateNode(XmlNodeType.Element, "integerliteral", "");

            integerliteral.Attributes.Append(docSource.CreateAttribute("value"));
            integerliteral.Attributes["value"].Value = node.integer.ToString();
            integerliteral.Attributes.Append(docSource.CreateAttribute("line"));
            integerliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            integerliteral.Attributes.Append(docSource.CreateAttribute("endline"));
            integerliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(integerliteral);


        }
        protected override void Visit_REAL_LITERAL(REAL_LITERAL node)
        {
            XmlNode realliteral = docSource.CreateNode(XmlNodeType.Element, "realliteral", "");

            realliteral.Attributes.Append(docSource.CreateAttribute("value"));
            realliteral.Attributes["value"].Value = node.real.ToString();
            realliteral.Attributes.Append(docSource.CreateAttribute("line"));
            realliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            realliteral.Attributes.Append(docSource.CreateAttribute("endline"));
            realliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(realliteral);

        }
        protected override void Visit_CCI_LITERAL(CCI_LITERAL node)
        {
            XmlNode cciliteral = docSource.CreateNode(XmlNodeType.Element, "cciliteral", "");

            cciliteral.Attributes.Append(docSource.CreateAttribute("value"));
            cciliteral.Attributes["value"].Value = node.literal.Value.ToString();
            cciliteral.Attributes.Append(docSource.CreateAttribute("type"));
            cciliteral.Attributes["type"].Value = node.literal.Type.Name.ToString();
            cciliteral.Attributes.Append(docSource.CreateAttribute("line"));
            cciliteral.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            cciliteral.Attributes.Append(docSource.CreateAttribute("endline"));
            cciliteral.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(cciliteral);

        }
        protected override void Visit_NULL(NULL node)     
        {
            XmlNode nil = docSource.CreateNode(XmlNodeType.Element, "nil", "");
            nil.Attributes.Append(docSource.CreateAttribute("line"));
            nil.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            nil.Attributes.Append(docSource.CreateAttribute("endline"));
            nil.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            parent.AppendChild(nil);

        }
        #endregion
        #endregion
        #endregion
        #region STATEMENT
        protected override void Visit_ASSIGNMENT(ASSIGNMENT node)
        {
            XmlNode assignment = docSource.CreateNode(XmlNodeType.Element, "assignment", "");
            assignment.Attributes.Append(docSource.CreateAttribute("line"));
            assignment.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            assignment.Attributes.Append(docSource.CreateAttribute("endline"));
            assignment.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.receiver != null)
            {
                XmlNode receivernode = docSource.CreateNode(XmlNodeType.Element, "receiver", "");
                VisitInContext(node.receiver, receivernode);

                assignment.AppendChild(receivernode);
            }

            VisitInContext(node.right_part, assignment);

            parent.AppendChild(assignment);


        }
        protected override void Visit_CALL_STMT(CALL_STMT node)
        {
            XmlNode callstatement = docSource.CreateNode(XmlNodeType.Element, "callstatement", "");
            callstatement.Attributes.Append(docSource.CreateAttribute("line"));
            callstatement.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            callstatement.Attributes.Append(docSource.CreateAttribute("endline"));
            callstatement.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            VisitInContext(node.call, callstatement);

            parent.AppendChild(callstatement);


        }
        protected override void Visit_EXIT(EXIT node)
        {
            XmlNode exit = docSource.CreateNode(XmlNodeType.Element, "exit", "");
            exit.Attributes.Append(docSource.CreateAttribute("line"));
            exit.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            exit.Attributes.Append(docSource.CreateAttribute("endline"));
            exit.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(exit);


        }
        protected override void Visit_RETURN(RETURN node)
        {
            XmlNode returnnode = docSource.CreateNode(XmlNodeType.Element, "return", "");
            returnnode.Attributes.Append(docSource.CreateAttribute("line"));
            returnnode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            returnnode.Attributes.Append(docSource.CreateAttribute("endline"));
            returnnode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.return_value != null)
            {
                VisitInContext(node.return_value, returnnode);
            }

            parent.AppendChild(returnnode);


        }
        protected override void Visit_REPLY(REPLY node)
        {
            XmlNode reply = docSource.CreateNode(XmlNodeType.Element, "reply", "");
            reply.Attributes.Append(docSource.CreateAttribute("line"));
            reply.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            reply.Attributes.Append(docSource.CreateAttribute("endline"));
            reply.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            for (int i = 0, n = node.values_to_reply.Length; i < n; i++)
                if (node.values_to_reply[i] != null)
                {
                    VisitInContext(node.values_to_reply[i], reply);
                }

            parent.AppendChild(reply);


        }
        protected override void Visit_AWAIT(AWAIT node)
        {
            XmlNode await = docSource.CreateNode(XmlNodeType.Element, "await", "");
            await.Attributes.Append(docSource.CreateAttribute("line"));
            await.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            await.Attributes.Append(docSource.CreateAttribute("endline"));
            await.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.val != null)
            {
                VisitInContext(node.val, await);
            }

            parent.AppendChild(await);


        }
        protected override void Visit_SEND_RECEIVE(SEND_RECEIVE node)
        {
            XmlNode sendreceive = docSource.CreateNode(XmlNodeType.Element, "sendreceive", "");
            sendreceive.Attributes.Append(docSource.CreateAttribute("line"));
            sendreceive.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            sendreceive.Attributes.Append(docSource.CreateAttribute("endline"));
            sendreceive.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode receivers = docSource.CreateNode(XmlNodeType.Element, "receivers", "");
            for (int i = 0, n = node.leftParts.Length; i < n; i++)
            {
                VisitInContext(node.leftParts[i], receivers);
            }

            if (node.call != null)
            {
                VisitInContext(node.call, sendreceive);
            }

            parent.AppendChild(sendreceive);


        }
        protected override void Visit_LAUNCH(LAUNCH node)
        {
            XmlNode launch = docSource.CreateNode(XmlNodeType.Element, "launch", "");
            launch.Attributes.Append(docSource.CreateAttribute("line"));
            launch.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            launch.Attributes.Append(docSource.CreateAttribute("endline"));
            launch.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            // Expressions do not have an enclosing unit assigned.
            // BUT: within a new expression, we have to check whether we have
            // to use a fully qulified name or not. So, for a launch statement,
            // assign the launch statement as the parent of the new expression!
            if (node.call.enclosing == null)
                node.call.enclosing = node;
            VisitInContext(node.call, launch);
            parent.AppendChild(launch);


        }
        protected override void Visit_ACCEPT(ACCEPT node)
        {
            XmlNode accept = docSource.CreateNode(XmlNodeType.Element, "accept", "");
            accept.Attributes.Append(docSource.CreateAttribute("line"));
            accept.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            accept.Attributes.Append(docSource.CreateAttribute("endline"));
            accept.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            for (int i = 0, n = node.designators.Length; i < n; i++)
            {
                VisitInContext(node.designators[i], accept);
            }
            parent.AppendChild(accept);


        }
        protected override void Visit_IF(IF node)
        {
            XmlNode ifnode = docSource.CreateNode(XmlNodeType.Element, "if", "");
            ifnode.Attributes.Append(docSource.CreateAttribute("line"));
            ifnode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            ifnode.Attributes.Append(docSource.CreateAttribute("endline"));
            ifnode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            for (int i = 0, n = node.Alternatives.Length; i < n; i++)
            {
                VisitInContext(node.Alternatives[i], ifnode);
            }

            parent.AppendChild(ifnode);


        }
        protected override void Visit_CASE(CASE node)
        {
            XmlNode casenode = docSource.CreateNode(XmlNodeType.Element, "case", "");
            casenode.Attributes.Append(docSource.CreateAttribute("line"));
            casenode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            casenode.Attributes.Append(docSource.CreateAttribute("endline"));
            casenode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.condition != null)
            {
                VisitInContext(node.condition, casenode);
            }
            if (node.Cases.Length > 0)
            {
                XmlNode casesnode = docSource.CreateNode(XmlNodeType.Element, "cases", "");
                for (int i = 0, n = node.Cases.Length; i < n; i++)
                {
                    VisitInContext(node.Cases[i], casenode);
                }
                casenode.AppendChild(casesnode);
            }

            parent.AppendChild(casenode);


        }
        protected override void Visit_BLOCK(BLOCK node)
        {
            if (node.statements[0] is BLOCK && node.statements.Length == 1)
            {
                // keep parent intentionally
                Visit(node.statements[0]);
            }
            else
            {
                XmlNode block = null;
                if ((node.comments != null && node.comments.Length > 0) || (node.statements != null && node.statements.Length > 0))
                {
                    block = docSource.CreateNode(XmlNodeType.Element, "block", "");
                    block.Attributes.Append(docSource.CreateAttribute("line"));
                    block.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                    block.Attributes.Append(docSource.CreateAttribute("endline"));
                    block.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
                }

                if (node.modifiers.Locked)
                {
                    XmlNode locked = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                    locked.Attributes.Append(docSource.CreateAttribute("name"));
                    locked.Attributes["name"].Value = "locked";
                    block.AppendChild(locked);
                }

                if (node.modifiers.Barrier)
                {
                    XmlNode locked = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                    locked.Attributes.Append(docSource.CreateAttribute("name"));
                    locked.Attributes["name"].Value = "barrier";
                    block.AppendChild(locked);
                }

                if (node.modifiers.Concurrent)
                {
                    XmlNode locked = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                    locked.Attributes.Append(docSource.CreateAttribute("name"));
                    locked.Attributes["name"].Value = "concurrent";
                    block.AppendChild(locked);
                }

                if ((node.comments != null) && (node.comments.Length > 0))
                {
                    for (int i = 0; i < node.comments.Length; i++)
                    {
                        VisitInContext(node.comments[i], block);
                    }
                }

                if (node.statements != null && node.statements.Length > 0)
                {
                    for (int i = 0, n = node.statements.Length; i < n; i++)
                    {
                        VisitInContext(node.statements[i], block);
                    }
                    if (node.exceptions != null && node.exceptions.Length != 0)
                    {
                        for (int i = 0, n = node.exceptions.Length; i < n; i++)
                        {
                            VisitInContext(node.exceptions[i], block);
                        }
                    }
                    if (node.termination != null && node.termination.statements.Length > 0)
                    {
                        XmlNode terminationnode = docSource.CreateNode(XmlNodeType.Element, "termination", "");
                        for (int i = 0, n = node.termination.statements.Length; i < n; i++)
                        {
                            VisitInContext(node.termination.statements[i], terminationnode);
                        }
                        parent.AppendChild(terminationnode);
                    }
                }
                if ((node.comments != null && node.comments.Length > 0) || (node.statements != null && node.statements.Length > 0))
                {
                    parent.AppendChild(block);
                }
            }


        }
        #region CYCLE
        protected override void Visit_FOR(FOR node)
        {
            XmlNode fornode = docSource.CreateNode(XmlNodeType.Element, "for", "");
            fornode.Attributes.Append(docSource.CreateAttribute("line"));
            fornode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            fornode.Attributes.Append(docSource.CreateAttribute("endline"));
            fornode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode forvar = docSource.CreateNode(XmlNodeType.Element, "forvar", "");
            VisitInContext(node.forVar, forvar);
            fornode.AppendChild(forvar);

            XmlNode fromnode = docSource.CreateNode(XmlNodeType.Element, "from", "");
            VisitInContext(node.from, fromnode);
            fornode.AppendChild(fromnode);

            XmlNode tonode = docSource.CreateNode(XmlNodeType.Element, "to", "");
            VisitInContext(node.to, tonode);
            fornode.AppendChild(tonode);

            if (node.by != null)
            {
                XmlNode bynode = docSource.CreateNode(XmlNodeType.Element, "by", "");
                VisitInContext(node.by, bynode);
                fornode.AppendChild(bynode);
            }

            if (node.statements != null)
                for (int i = 0, n = node.statements.Length; i < n; i++)
                {
                    VisitInContext(node.statements[i], fornode);
                }

            parent.AppendChild(fornode);


        }
        protected override void Visit_WHILE(WHILE node)
        {
            XmlNode whilenode = docSource.CreateNode(XmlNodeType.Element, "while", "");
            whilenode.Attributes.Append(docSource.CreateAttribute("line"));
            whilenode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            whilenode.Attributes.Append(docSource.CreateAttribute("endline"));
            whilenode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode conditionnode = docSource.CreateNode(XmlNodeType.Element, "condition", "");
            if (node.condition != null)
            {
                VisitInContext(node.condition, conditionnode);
            }
            whilenode.AppendChild(conditionnode);

            if (node.statements != null)
            {
                for (int i = 0, n = node.statements.Length; i < n; i++)
                {
                    VisitInContext(node.statements[i], whilenode);
                }
            }

            parent.AppendChild(whilenode);


        }
        protected override void Visit_REPEAT(REPEAT node)
        {
            XmlNode repeat = docSource.CreateNode(XmlNodeType.Element, "repeat", "");
            repeat.Attributes.Append(docSource.CreateAttribute("line"));
            repeat.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            repeat.Attributes.Append(docSource.CreateAttribute("endline"));
            repeat.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.statements != null)
            {
                for (int i = 0, n = node.statements.Length; i < n; i++)
                {
                    VisitInContext(node.statements[i], repeat);
                }
            }

            XmlNode conditionnode = docSource.CreateNode(XmlNodeType.Element, "condition", "");
            if (node.condition != null)
            {
                VisitInContext(node.condition, conditionnode);
            }
            repeat.AppendChild(conditionnode);

            parent.AppendChild(repeat);


        }
        protected override void Visit_LOOP(LOOP node)
        {
            XmlNode loop = docSource.CreateNode(XmlNodeType.Element, "loop", "");
            loop.Attributes.Append(docSource.CreateAttribute("line"));
            loop.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            loop.Attributes.Append(docSource.CreateAttribute("endline"));
            loop.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            if (node.statements != null)
            {
                for (int i = 0, n = node.statements.Length; i < n; i++)
                {
                    VisitInContext(node.statements[i], loop);
                }
                parent.AppendChild(loop);
            }


        }
        #endregion
        #endregion
        #region DECLARATION
        #region UNIT_DECL
        // TODO: figure out which routine below might use it
        private void Visit_UNIT_DECL(UNIT_DECL node)
        {
            if ((node.comments != null) && (node.comments.Length > 0))
            {
                for (int i = 0; i < node.comments.Length; i++)
                {
                    VisitInContext(node.comments[i], node.xmlUnitNode);
                }
            }

            XmlNode declarations = docSource.CreateNode(XmlNodeType.Element, "declarations", "");

            if (node.locals != null && node.locals.Length > 0)
            {
                if (node is OBJECT_DECL)
                {
                    if ((((OBJECT_DECL)node).paramCount) > 0)
                    {
                        int i = 0, n = 0;
                        while (n < ((OBJECT_DECL)node).paramCount)
                        {
                            if (node.locals[i] is UNKNOWN_DECL)
                                node.resolve();
                            if ((node.locals[i] is FIELD_DECL))
                            {
                                n++; i++; continue;
                            }
                            else
                                if (node.locals[i] != null)
                                    if ((!(node.locals[i] is EXTERNAL_DECL)) && (!(node.locals[i] is UNKNOWN_DECL)))
                                    {
                                        VisitInContext(node.locals[i], declarations);
                                    }
                            i++;
                        }
                        for (int j = i; j < node.locals.Length; j++)
                        {
                            if (node.locals[j] is UNKNOWN_DECL)
                                node.resolve();
                            // We do not want external declarations as direct children of 
                            // a program unit
                            if ((!(node.locals[j] is EXTERNAL_DECL)) && (!(node.locals[j] is UNKNOWN_DECL)))
                            {
                                VisitInContext(node.locals[j], declarations);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0, n = node.locals.Length; i < n; i++)
                        {
                            if (node.locals[i] is UNKNOWN_DECL)
                                node.resolve();
                            // We do not want external declarations as direct children of 
                            // a program unit
                            if ((!(node.locals[i] is EXTERNAL_DECL)) && (!(node.locals[i] is UNKNOWN_DECL)))
                            {
                                VisitInContext(node.locals[i], declarations);
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0, n = node.locals.Length; i < n; i++)
                    {
                        if (node.locals[i] is UNKNOWN_DECL)
                            node.resolve();
                        // We do not want external declarations as direct children of 
                        // a program unit
                        if ((!(node.locals[i] is EXTERNAL_DECL)) && (!(node.locals[i] is UNKNOWN_DECL)))
                            if (!(node is NAMESPACE_DECL))
                            {
                                VisitInContext(node.locals[i], declarations);
                            }
                            else
                            {
                                VisitInContext(node.locals[i],node.xmlUnitNode);
                            }
                    }
                }
            }

            if (!(node is NAMESPACE_DECL) && (node.xmlUnitNode != null))
            {
                node.xmlUnitNode.AppendChild(declarations);
            }

            if (node.body == null)
            {
                // Do nothing
            }
            else if ((node.body.statements == null || node.body.statements.Length == 0) &&
                (node.body.exceptions == null || node.body.exceptions.Length == 0))
            {
                // Do nothing
            }
            else
            {
                VisitInContext(node.body, node.xmlUnitNode);
            }
            // add to parent is done in derived units

        }

        protected override void Visit_UNKNOWN_DECL(UNKNOWN_DECL node)
        {
            if (node.RealDeclaration != null)
                VisitInContext(node.RealDeclaration, parent);
            else
            {
                XmlNode instance = docSource.CreateNode(XmlNodeType.Element, "externaldeclaration", "");
                instance.Attributes.Append(docSource.CreateAttribute("name"));
                instance.Attributes["name"].Value = node.name.Name;
                instance.Attributes.Append(docSource.CreateAttribute("line"));
                instance.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                instance.Attributes.Append(docSource.CreateAttribute("endline"));
                instance.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                parent.AppendChild(instance);
            }


        }
        protected override void Visit_EXTERNAL_DECL(EXTERNAL_DECL node)
        {
            XmlNode externalDeclaration = docSource.CreateNode(XmlNodeType.Element, "externaldeclaration", "");
            externalDeclaration.Attributes.Append(docSource.CreateAttribute("name"));
            externalDeclaration.Attributes["name"].Value = node.name.Name;
            externalDeclaration.Attributes.Append(docSource.CreateAttribute("line"));
            externalDeclaration.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            externalDeclaration.Attributes.Append(docSource.CreateAttribute("endline"));
            externalDeclaration.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(externalDeclaration);


        }
        protected override void Visit_NAMESPACE_DECL(NAMESPACE_DECL node)
        {
            if (node != CONTEXT.globalTree)
            {
                bool found = false;
                for (int i = 0; i < node.locals.Length; i++)
                {
                    if (node.locals[i].name != null)
                    {
                        if (node.locals[i].sourceContext.Document.Name.ToString().EndsWith(compilationFileName))
                        {
                            found = true; ;
                        }
                    }
                }
                if (found)
                {
                    node.xmlUnitNode = docSource.CreateNode(XmlNodeType.Element, "namespace", "");
                    node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("name"));
                    node.xmlUnitNode.Attributes["name"].Value = node.name.ToString();
                    node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("id"));
                    node.xmlUnitNode.Attributes["id"].Value = "i" + node.unique.ToString();
                }

                if (node.xmlUnitNode == null)
                    node.xmlUnitNode = parent;

                Visit_UNIT_DECL(node);

                if (found)
                {
                    parent.AppendChild(node.xmlUnitNode);
                }
            }
            else
            {
                node.xmlUnitNode = parent; //docSource.CreateNode(XmlNodeType.Element, "namespace", "");
                Visit_UNIT_DECL(node);
            }


        }
        protected override void Visit_MODULE_DECL(MODULE_DECL node)
        {
            if (node.sourceContext.Document.Name.ToString().EndsWith(compilationFileName))
            {
                node.xmlUnitNode = docSource.CreateNode(XmlNodeType.Element, "module", "");

                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("name"));
                node.xmlUnitNode.Attributes["name"].Value = node.name.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("id"));
                node.xmlUnitNode.Attributes["id"].Value = "i" + node.unique.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("line"));
                node.xmlUnitNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("endline"));
                node.xmlUnitNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                modifier.Attributes.Append(docSource.CreateAttribute("name"));

                if (node.modifiers.Public)
                    modifier.Attributes["name"].Value = "public";
                else
                    modifier.Attributes["name"].Value = "private";

                node.xmlUnitNode.AppendChild(modifier);

                Visit_UNIT_DECL(node);

                parent.AppendChild(node.xmlUnitNode);
            }


        }
        protected override void Visit_OBJECT_DECL(OBJECT_DECL node)
        {
            if (node.sourceContext.Document.Name.ToString().EndsWith(compilationFileName))
            {
                node.xmlUnitNode = docSource.CreateNode(XmlNodeType.Element, "object", "");
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("name"));
                node.xmlUnitNode.Attributes["name"].Value = node.name.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("id"));
                node.xmlUnitNode.Attributes["id"].Value = "i" + node.unique;
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("line"));
                node.xmlUnitNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("endline"));
                node.xmlUnitNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                modifier.Attributes.Append(docSource.CreateAttribute("name"));

                if (node.modifiers.Public)
                    modifier.Attributes["name"].Value = "public";
                else
                    modifier.Attributes["name"].Value = "private";
                node.xmlUnitNode.AppendChild(modifier);

                XmlNode refmodifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                refmodifier.Attributes.Append(docSource.CreateAttribute("name"));

                if (node.modifiers.Reference)
                    refmodifier.Attributes["name"].Value = "ref";
                else
                    refmodifier.Attributes["name"].Value = "value";
                node.xmlUnitNode.AppendChild(refmodifier);

                if (node.modifiers.Protected)
                {
                    XmlNode protmodifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                    protmodifier.Attributes.Append(docSource.CreateAttribute("name"));
                    protmodifier.Attributes["name"].Value = "protected";

                    node.xmlUnitNode.AppendChild(protmodifier);
                }

                if (node.paramCount > 0)
                {
                    int i = 0, n = 0;
                    XmlNode parameters = docSource.CreateNode(XmlNodeType.Element, "parameters", "");
                    while (n < node.paramCount)
                    {
                        if (!(node.locals[i] is FIELD_DECL)) { i++; continue; }

                        XmlNode parameter = docSource.CreateNode(XmlNodeType.Element, "parameter", "");
                        parameter.Attributes.Append(docSource.CreateAttribute("name"));
                        parameter.Attributes["name"].Value = node.locals[i].name.ToString();
                        parameter.Attributes.Append(docSource.CreateAttribute("id"));
                        parameter.Attributes["id"].Value = "i" + node.locals[i].unique;
                        parameter.Attributes.Append(docSource.CreateAttribute("isref"));
                        parameter.Attributes["isref"].Value = "false";
                        parameter.Attributes.Append(docSource.CreateAttribute("line"));
                        parameter.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                        parameter.Attributes.Append(docSource.CreateAttribute("endline"));
                        parameter.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                        VisitInContext(node.locals[i].type, parameter);
                        parameters.AppendChild(parameter);
                        i++; n++;
                    }
                    node.xmlUnitNode.AppendChild(parameters);
                }

                if (node.definitions != null && node.definitions.Length > 0)
                {
                    for (int i = 0, n = node.definitions.Length; i < n; i++)
                    {
                        // TODO: traverse all imports to find out, whether the name must be qualified
                        XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                        implements.Attributes.Append(docSource.CreateAttribute("name"));


                        if (node.definitions[i] is EXTERNAL_DECL)
                        {
                            implements.Attributes["name"].Value = ((EXTERNAL_DECL)node.definitions[i]).entity.ToString();
                        }
                        else
                        {
                            // Not sure, whether name resolution is correct here.
                            // It works for all available Zonnon examples
                            UNIT_DECL tunit = (UNIT_DECL)node.definitions[i];
                            string strtunit = node.definitions[i].name.ToString();
                            while (tunit.getEnclosingUnit() != CONTEXT.globalTree)
                            {
                                tunit = (UNIT_DECL)tunit.getEnclosingUnit();
                                strtunit = tunit.name + "." + strtunit;
                            }

                            implements.Attributes["name"].Value = strtunit;
                        }
                        node.xmlUnitNode.AppendChild(implements);
                    }

                }
                Visit_UNIT_DECL(node);
                parent.AppendChild(node.xmlUnitNode);
            }

        }
        protected override void Visit_DEFINITION_DECL(DEFINITION_DECL node)
        {
            if (node.sourceContext.Document.Name.ToString().EndsWith(compilationFileName))
            {
                node.xmlUnitNode = docSource.CreateNode(XmlNodeType.Element, "definition", "");
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("name"));
                node.xmlUnitNode.Attributes["name"].Value = node.name.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("id"));
                node.xmlUnitNode.Attributes["id"].Value = "i" + node.unique.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("line"));
                node.xmlUnitNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("endline"));
                node.xmlUnitNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
                modifier.Attributes.Append(docSource.CreateAttribute("name"));
                if (node.modifiers.Public)
                    modifier.Attributes["name"].Value = "public";
                else
                    modifier.Attributes["name"].Value = "private";
                node.xmlUnitNode.AppendChild(modifier);

                // Not sure, whether node is already correct: maybe, name must be resolved somehow as well!
                if (node.base_definition != null)
                {
                    XmlNode refines = docSource.CreateNode(XmlNodeType.Element, "refines", "");
                    refines.Attributes.Append(docSource.CreateAttribute("name"));
                    refines.Attributes["name"].Value = node.base_definition.name.ToString();
                    refines.Attributes.Append(docSource.CreateAttribute("ref"));
                    refines.Attributes["ref"].Value = node.base_definition.generateXPath();
                    node.xmlUnitNode.AppendChild(refines);
                }
                // ...same here
                if (node.default_implementation != null)
                {
                    XmlNode defaultimplementation = docSource.CreateNode(XmlNodeType.Element, "defaultimplementation", "");
                    defaultimplementation.Attributes.Append(docSource.CreateAttribute("name"));
                    defaultimplementation.Attributes["name"].Value = node.default_implementation.name.ToString();
                    node.xmlUnitNode.AppendChild(defaultimplementation);
                }
                Visit_UNIT_DECL(node);
                parent.AppendChild(node.xmlUnitNode);
            }


        }
        protected override void Visit_IMPLEMENTATION_DECL(IMPLEMENTATION_DECL node)
        {
            if (node.sourceContext.Document.Name.ToString().EndsWith(compilationFileName))
            {
                node.xmlUnitNode = docSource.CreateNode(XmlNodeType.Element, "implementation", "");
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("name"));
                node.xmlUnitNode.Attributes["name"].Value = node.name.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("id"));
                node.xmlUnitNode.Attributes["id"].Value = "i" + node.unique;
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("line"));
                node.xmlUnitNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                node.xmlUnitNode.Attributes.Append(docSource.CreateAttribute("endline"));
                node.xmlUnitNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                if (node.implemented_definition != null)
                {
                    XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                    implements.Attributes.Append(docSource.CreateAttribute("name"));
                    implements.Attributes["name"].Value = node.implemented_definition.name.ToString();

                    node.xmlUnitNode.AppendChild(implements);
                }

                Visit_UNIT_DECL(node);
                if (parent != null)
                    parent.AppendChild(node.xmlUnitNode);
            }


        }
        #endregion
        #region ROUTINE_DECL

        private void Visit_ROUTINE_DECL(ROUTINE_DECL node)
        {
            if ((node.comments != null) && (node.comments.Length > 0))
            {
                for (int i = 0; i < node.comments.Length; i++)
                {
                    VisitInContext(node.comments[i], node.xmlRoutineNode);
                }
            }

            if (node.parameters != null && node.parameters.Length > 0)
            {
                XmlNode parametersnode = docSource.CreateNode(XmlNodeType.Element, "parameters", "");
                for (int i = 0, n = node.parameters.Length; i < n; i++)
                    VisitInContext(node.parameters[i], parametersnode);
                node.xmlRoutineNode.AppendChild(parametersnode);
            }

            if (node.return_type != null)
            {
                XmlNode returntype = docSource.CreateNode(XmlNodeType.Element, "returntype", "");

                if (node.return_type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.return_type, node);

                    XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                    objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                    objecttype.Attributes["name"].Value = typeName;
                    objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
                    objecttype.Attributes["ref"].Value = ((OBJECT_TYPE)node.return_type).ObjectUnit.generateXPath();
                    returntype.AppendChild(objecttype);
                }
                else if (node.return_type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.return_type, returntype);
                }
                else if (node.return_type is ACTIVITY_TYPE)
                {
                    VisitInContext(node.type, returntype);
                }
                else if (node.return_type is INTERFACE_TYPE)
                {
                    XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");
                    if (((INTERFACE_TYPE)node.return_type).interfaces != null && ((INTERFACE_TYPE)node.return_type).interfaces.Length > 0)
                    {
                        string typeName;
                        for (int i = 0, n = ((INTERFACE_TYPE)node.return_type).interfaces.Length; i < n; i++)
                        {
                            typeName = DECLARATION.resolveInterfaceTypeNameInDeclaration((INTERFACE_TYPE)node.return_type, ((INTERFACE_TYPE)node.return_type).interfaces[i], node);

                            if (typeName != null)
                            {
                                XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                                implements.Attributes.Append(docSource.CreateAttribute("name"));
                                implements.Attributes["name"].Value = typeName;
                                implements.Attributes.Append(docSource.CreateAttribute("ref"));
                                implements.Attributes["ref"].Value = ((INTERFACE_TYPE)node.return_type).interfaces[i].generateXPath();

                                interfacetype.AppendChild(implements);
                            }
                        }
                    }
                    returntype.AppendChild(interfacetype);
                }
                else if (node.return_type.enclosing is PROTOCOL_DECL)
                {
                    XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");

                    typenode.Attributes.Append(docSource.CreateAttribute("name"));
                    typenode.Attributes["name"].Value = node.type.enclosing.name.ToString();
                    typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                    typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                    returntype.AppendChild(typenode);
                }
                else if (node.return_type.enclosing is TYPE_DECL)
                {
                    if (!(NODE.isBuiltIn(node.return_type.enclosing.name.ToString())))
                    {
                        string typeName = DECLARATION.resolveTypeTypeNameInDeclaration((TYPE)node.return_type, node);

                        XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");
                        typenode.Attributes.Append(docSource.CreateAttribute("name"));
                        typenode.Attributes["name"].Value = typeName; //base_type.enclosing.name.ToString();
                        typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                        typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                        returntype.AppendChild(typenode);
                    }
                    else
                        VisitInContext(node.type, returntype);
                }
                else
                    VisitInContext(node.type, returntype);

                node.xmlRoutineNode.AppendChild(returntype);
            }

            DECLARATION prototype = (node is ACTIVITY_DECL) ? ((ACTIVITY_DECL)node).prototype : node.prototype;
            if (prototype != null)
            {
                prototype.resolve();
                XmlNode prototypenode = docSource.CreateNode(XmlNodeType.Element, "prototype", "");
                prototypenode.Attributes.Append(docSource.CreateAttribute("name"));

                if (prototype is EXTERNAL_DECL || prototype is UNKNOWN_DECL)
                {
                    prototypenode.Attributes["name"].Value = node.prototypeName; //((EXTERNAL_DECL)prototype).entity.ToString();
                }
                else
                {
                    UNIT_DECL tunit = (UNIT_DECL)prototype.getEnclosingUnit();
                    string tunits = ((UNIT_DECL)prototype.getEnclosingUnit()).name.ToString() + "." + prototype.name.ToString();
                    while (tunit.enclosing != CONTEXT.globalTree)
                    {
                        tunit = tunit.getEnclosingUnit();
                        tunits = tunit.name.ToString() + "." + tunits;
                    }
                    prototypenode.Attributes["name"].Value = tunits;
                    prototypenode.Attributes.Append(docSource.CreateAttribute("ref"));
                    prototypenode.Attributes["ref"].Value = prototype.generateXPath();
                }

                node.xmlRoutineNode.AppendChild(prototypenode);
            }
            if (node.locals != null && node.locals.Length > 0)
            {
                XmlNode declarations = docSource.CreateNode(XmlNodeType.Element, "declarations", "");

                for (int i = 0, n = node.locals.Length; i < n; i++)
                    VisitInContext(node.locals[i], declarations);
                node.xmlRoutineNode.AppendChild(declarations);
            }
            if (node.body != null)
            {
                VisitInContext(node.body, node.xmlRoutineNode);
            }

        }

        protected override void Visit_PROCEDURE_DECL(PROCEDURE_DECL node)
        {
            node.xmlRoutineNode = docSource.CreateNode(XmlNodeType.Element, "procedure", "");
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("name"));
            node.xmlRoutineNode.Attributes["name"].Value = node.name.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("id"));
            node.xmlRoutineNode.Attributes["id"].Value = "i" + node.unique.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlRoutineNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlRoutineNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));

            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            node.xmlRoutineNode.AppendChild(modifier);

            Visit_ROUTINE_DECL(node);

            parent.AppendChild(node.xmlRoutineNode);


        }
        protected override void Visit_OPERATOR_DECL(OPERATOR_DECL node)
        {
            string op;
            switch (node.name.ToString())
            {
                case "_Addition":
                    op = "+";
                    break;
                case "_Subtraction":
                    op = "-";
                    break;
                case "_Multiply":
                    op = "*";
                    break;
                default:
                    op = "/";
                    break;
            }

            node.xmlRoutineNode = docSource.CreateNode(XmlNodeType.Element, "operator", "");
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("name"));
            node.xmlRoutineNode.Attributes["name"].Value = op;
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("id"));
            node.xmlRoutineNode.Attributes["id"].Value = "i" + node.unique.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlRoutineNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlRoutineNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            node.xmlRoutineNode.AppendChild(modifier);

            Visit_ROUTINE_DECL(node);

            parent.AppendChild(node.xmlRoutineNode);


        }
        protected override void Visit_ACTIVITY_DECL(ACTIVITY_DECL node)
        {
            node.xmlRoutineNode = docSource.CreateNode(XmlNodeType.Element, "activity", "");
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("name"));
            node.xmlRoutineNode.Attributes["name"].Value = node.name.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("id"));
            node.xmlRoutineNode.Attributes["id"].Value = "i" + node.unique.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("line"));
            node.xmlRoutineNode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            node.xmlRoutineNode.Attributes.Append(docSource.CreateAttribute("endline"));
            node.xmlRoutineNode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));

            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "public";
            node.xmlRoutineNode.AppendChild(modifier);

            Visit_ROUTINE_DECL(node);

            parent.AppendChild(node.xmlRoutineNode);


        }
        #endregion
        #region SIMPLE_DECL
        protected override void Visit_IMPORT_DECL(IMPORT_DECL node)
        {
            node.resolve();
            node.imported_unit.resolve();
            XmlNode import = docSource.CreateNode(XmlNodeType.Element, "import", "");
            import.Attributes.Append(docSource.CreateAttribute("name"));
            import.Attributes.Append(docSource.CreateAttribute("id"));
            import.Attributes.Append(docSource.CreateAttribute("importedname"));
            import.Attributes.Append(docSource.CreateAttribute("line"));
            import.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            import.Attributes.Append(docSource.CreateAttribute("endline"));
            import.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            string importedName;
            if (node.imported_unit is EXTERNAL_DECL)
            {
                importedName = ((EXTERNAL_DECL)node.imported_unit).entity.ToString();
                importedName = importedName.Replace("+", ".");
                import.Attributes["name"].Value = (node.name != null) ? node.name.ToString() : ((EXTERNAL_DECL)node.imported_unit).entity.ToString();
                import.Attributes["id"].Value = "i" + node.unique.ToString();
                import.Attributes["importedname"].Value = importedName;
            }
            else if ((node.imported_unit.enclosing is NAMESPACE_DECL) && (node.imported_unit.enclosing != CONTEXT.globalTree))
            {
                importedName = IMPORT_DECL.generateFullName(node.imported_unit);
                import.Attributes["name"].Value = (node.name != null) ? node.name.ToString() : node.imported_unit.enclosing.name.ToString() + "." + node.imported_unit.name.ToString();
                import.Attributes["id"].Value = "i" + node.unique;
                import.Attributes["importedname"].Value = importedName;//imported_unit.enclosing.name.ToString() + "." + imported_unit.name.ToString();
            }
            else
            {
                importedName = IMPORT_DECL.generateFullName(node.imported_unit);
                importedName = importedName.Replace("+", ".");

                import.Attributes["name"].Value = (node.name != null) ? node.name.ToString() : node.imported_unit.name.ToString();
                import.Attributes["id"].Value = "i" + node.unique;
                import.Attributes["importedname"].Value = importedName; //imported_unit.enclosing.name.ToString() + "." + imported_unit.name.ToString();
                import.Attributes.Append(docSource.CreateAttribute("ref"));
                import.Attributes["ref"].Value = node.imported_unit.generateXPath();
            }

            parent.AppendChild(import);


        }
        #region VARIABLE_DECL
        private void Visit_VARIABLE_DECL(VARIABLE_DECL node)
        {
            /* do nothing */

        }
        protected override void Visit_PARAMETER_DECL(PARAMETER_DECL node)
        {
            node.resolve();

            XmlNode parameter = docSource.CreateNode(XmlNodeType.Element, "parameter", "");
            parameter.Attributes.Append(docSource.CreateAttribute("name"));
            parameter.Attributes["name"].Value = node.name.ToString();
            parameter.Attributes.Append(docSource.CreateAttribute("id"));
            parameter.Attributes["id"].Value = "i" + node.unique.ToString();
            parameter.Attributes.Append(docSource.CreateAttribute("isref"));
            parameter.Attributes["isref"].Value = node.Reference.ToString();
            parameter.Attributes.Append(docSource.CreateAttribute("line"));
            parameter.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            parameter.Attributes.Append(docSource.CreateAttribute("endline"));
            parameter.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            if (node.type != null)
            {
                if (node.type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.type, node);

                    if (typeName != null)
                    {
                        XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                        objecttype.Attributes["name"].Value = typeName;
                        objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
                        objecttype.Attributes["ref"].Value = ((OBJECT_TYPE)node.type).ObjectUnit.generateXPath();

                        parameter.AppendChild(objecttype);
                    }
                    else
                        VisitInContext(node.type, parameter);
                }
                else if (node.type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, parameter);
                }
                else if (node.type is INTERFACE_TYPE)
                {
                    XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");

                    if (((INTERFACE_TYPE)node.type).interfaces != null && ((INTERFACE_TYPE)node.type).interfaces.Length > 0)
                    {
                        string typeName;
                        for (int i = 0, n = ((INTERFACE_TYPE)node.type).interfaces.Length; i < n; i++)
                        {
                            typeName = DECLARATION.resolveInterfaceTypeNameInDeclaration((INTERFACE_TYPE)node.type, ((INTERFACE_TYPE)node.type).interfaces[i], node);

                            if (typeName != null)
                            {
                                XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                                implements.Attributes.Append(docSource.CreateAttribute("name"));
                                implements.Attributes["name"].Value = typeName;
                                implements.Attributes.Append(docSource.CreateAttribute("ref"));
                                implements.Attributes["ref"].Value = ((INTERFACE_TYPE)node.type).interfaces[i].generateXPath();

                                interfacetype.AppendChild(implements);
                            }
                        }
                    }
                    parameter.AppendChild(interfacetype);
                }
                else if (node.type.enclosing is TYPE_DECL)
                {
                    if (!(NODE.isBuiltIn(node.type.enclosing.name.ToString())))
                    {
                        string typeName = DECLARATION.resolveTypeTypeNameInDeclaration((TYPE)node.type, node);

                        XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");
                        typenode.Attributes.Append(docSource.CreateAttribute("name"));
                        typenode.Attributes["name"].Value = typeName; //base_type.enclosing.name.ToString();
                        typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                        typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                        parameter.AppendChild(typenode);
                    }
                    else
                        VisitInContext(node.type, parameter);
                }
                else
                    VisitInContext(node.type, parameter);
            }
            parent.AppendChild(parameter);

        }
        protected override void Visit_LOCAL_DECL(LOCAL_DECL node)
        {
            XmlNode local = docSource.CreateNode(XmlNodeType.Element, "local", "");
            local.Attributes.Append(docSource.CreateAttribute("name"));
            local.Attributes["name"].Value = node.name.ToString();
            local.Attributes.Append(docSource.CreateAttribute("id"));
            local.Attributes["id"].Value = "i" + node.unique;
            local.Attributes.Append(docSource.CreateAttribute("line"));
            local.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            local.Attributes.Append(docSource.CreateAttribute("endline"));
            local.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));
            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            local.AppendChild(modifier);

            // handle types, that are referenced through <type> and <objecttype>
            // resolve - node is needed for objecttype
            node.type.resolve();
            if (node.type != null)
            {
                if (node.type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.type, node);

                    if (typeName != null)
                    {
                        XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                        objecttype.Attributes["name"].Value = typeName;
                        objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
                        objecttype.Attributes["ref"].Value = ((OBJECT_TYPE)node.type).ObjectUnit.generateXPath();

                        local.AppendChild(objecttype);
                    }
                    else
                        VisitInContext(node.type, local);
                }
                else if (node.type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, local);
                }
                else if (node.type is ACTIVITY_TYPE)
                {
                    if (((ACTIVITY_TYPE)node.type).activity.getEnclosingUnit() == node.getEnclosingUnit())
                    {
                        XmlNode activitytype = docSource.CreateNode(XmlNodeType.Element, "activitytype", "");
                        activitytype.Attributes.Append(docSource.CreateAttribute("name"));
                        activitytype.Attributes["name"].Value = ((ACTIVITY_TYPE)node.type).activity.name.ToString();
                        activitytype.Attributes.Append(docSource.CreateAttribute("ref"));
                        activitytype.Attributes["ref"].Value = ((ACTIVITY_TYPE)node.type).activity.generateXPath();

                        local.AppendChild(activitytype);
                    }
                    else
                    {
                        VisitInContext(node.type, local);
                    }
                }
                else if (node.type is INTERFACE_TYPE)
                {
                    XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");

                    if (((INTERFACE_TYPE)node.type).interfaces != null && ((INTERFACE_TYPE)node.type).interfaces.Length > 0)
                    {
                        string typeName;
                        for (int i = 0, n = ((INTERFACE_TYPE)node.type).interfaces.Length; i < n; i++)
                        {
                            typeName = DECLARATION.resolveInterfaceTypeNameInDeclaration((INTERFACE_TYPE)node.type, ((INTERFACE_TYPE)node.type).interfaces[i], node);

                            if (typeName != null)
                            {
                                XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                                implements.Attributes.Append(docSource.CreateAttribute("name"));
                                implements.Attributes["name"].Value = typeName;
                                implements.Attributes.Append(docSource.CreateAttribute("ref"));
                                implements.Attributes["ref"].Value = ((INTERFACE_TYPE)node.type).interfaces[i].generateXPath();

                                interfacetype.AppendChild(implements);
                            }
                        }
                    }
                    local.AppendChild(interfacetype);
                }
                else if (node.type.enclosing is TYPE_DECL)
                {
                    if (!(NODE.isBuiltIn(node.type.enclosing.name.ToString())))
                    {
                        string typeName = DECLARATION.resolveTypeTypeNameInDeclaration((TYPE)node.type, node);

                        XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");
                        typenode.Attributes.Append(docSource.CreateAttribute("name"));
                        typenode.Attributes["name"].Value = typeName; //base_type.enclosing.name.ToString();
                        typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                        typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                        local.AppendChild(typenode);
                    }
                    else
                        VisitInContext(node.type, local);
                }
                else if (node.type.enclosing is PROTOCOL_DECL)
                {
                    XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");

                    typenode.Attributes.Append(docSource.CreateAttribute("name"));
                    typenode.Attributes["name"].Value = node.type.enclosing.name.ToString();
                    typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                    typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                    local.AppendChild(typenode);
                }
                else
                    VisitInContext(node.type, local);
            }
            parent.AppendChild(local);


        }
        protected override void Visit_FIELD_DECL(FIELD_DECL node)
        {
            XmlNode field = docSource.CreateNode(XmlNodeType.Element, "field", "");
            field.Attributes.Append(docSource.CreateAttribute("name"));
            field.Attributes["name"].Value = node.name.ToString();
            field.Attributes.Append(docSource.CreateAttribute("id"));
            field.Attributes["id"].Value = "i" + node.unique.ToString();
            field.Attributes.Append(docSource.CreateAttribute("line"));
            field.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            field.Attributes.Append(docSource.CreateAttribute("endline"));
            field.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));
            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            field.AppendChild(modifier);

            // handle types, that are referenced through <type>, <objecttype>, ...
            if (node.type != null)
            {
                if (node.type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.type, node);

                    if (typeName != null)
                    {
                        XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objecttype.Attributes.Append(docSource.CreateAttribute("name"));
                        objecttype.Attributes["name"].Value = typeName;
                        objecttype.Attributes.Append(docSource.CreateAttribute("ref"));
                        objecttype.Attributes["ref"].Value = ((OBJECT_TYPE)node.type).ObjectUnit.generateXPath();

                        field.AppendChild(objecttype);
                    }
                    else
                        VisitInContext(node.type, field);
                }
                else if (node.type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, field);
                }
                else if (node.type is ACTIVITY_TYPE)
                {
                    if (((ACTIVITY_TYPE)node.type).activity.getEnclosingUnit() == node.getEnclosingUnit())
                    {
                        XmlNode activitytype = docSource.CreateNode(XmlNodeType.Element, "activitytype", "");
                        activitytype.Attributes.Append(docSource.CreateAttribute("name"));
                        activitytype.Attributes["name"].Value = ((ACTIVITY_TYPE)node.type).activity.name.ToString();
                        activitytype.Attributes.Append(docSource.CreateAttribute("ref"));
                        activitytype.Attributes["ref"].Value = ((ACTIVITY_TYPE)node.type).activity.generateXPath();

                        field.AppendChild(activitytype);
                    }
                    else
                    {
                        VisitInContext(node.type, field);
                    }
                }
                else if (node.type is INTERFACE_TYPE)
                {
                    XmlNode interfacetype = docSource.CreateNode(XmlNodeType.Element, "interfacetype", "");

                    if (((INTERFACE_TYPE)node.type).interfaces != null && ((INTERFACE_TYPE)node.type).interfaces.Length > 0)
                    {
                        string typeName;
                        for (int i = 0, n = ((INTERFACE_TYPE)node.type).interfaces.Length; i < n; i++)
                        {
                            typeName = DECLARATION.resolveInterfaceTypeNameInDeclaration((INTERFACE_TYPE)node.type, ((INTERFACE_TYPE)node.type).interfaces[i], node);

                            if (typeName != null)
                            {
                                XmlNode implements = docSource.CreateNode(XmlNodeType.Element, "implements", "");
                                implements.Attributes.Append(docSource.CreateAttribute("name"));
                                implements.Attributes["name"].Value = typeName;
                                implements.Attributes.Append(docSource.CreateAttribute("ref"));
                                implements.Attributes["ref"].Value = ((INTERFACE_TYPE)node.type).interfaces[i].generateXPath();

                                interfacetype.AppendChild(implements);
                            }
                        }
                    }
                    field.AppendChild(interfacetype);
                }
                else if (node.type.enclosing is PROTOCOL_DECL)
                {
                    XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");

                    typenode.Attributes.Append(docSource.CreateAttribute("name"));
                    typenode.Attributes["name"].Value = node.type.enclosing.name.ToString();
                    typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                    typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                    field.AppendChild(typenode);
                }
                else if (node.type.enclosing is TYPE_DECL)
                {
                    if (!(NODE.isBuiltIn(node.type.enclosing.name.ToString())))
                    {
                        string typeName = DECLARATION.resolveTypeTypeNameInDeclaration((TYPE)node.type, node);

                        XmlNode typenode = docSource.CreateNode(XmlNodeType.Element, "type", "");
                        typenode.Attributes.Append(docSource.CreateAttribute("name"));
                        typenode.Attributes["name"].Value = typeName; //base_type.enclosing.name.ToString();
                        typenode.Attributes.Append(docSource.CreateAttribute("ref"));
                        typenode.Attributes["ref"].Value = node.type.enclosing.generateXPath();

                        field.AppendChild(typenode);
                    }
                    else
                        VisitInContext(node.type, field);
                }
            }
            parent.AppendChild(field);

        }
        #endregion
        protected override void Visit_CONSTANT_DECL(CONSTANT_DECL node)
        {
            XmlNode constantdeclaration = docSource.CreateNode(XmlNodeType.Element, "constantdeclaration", "");
            constantdeclaration.Attributes.Append(docSource.CreateAttribute("name"));
            constantdeclaration.Attributes["name"].Value = node.name.ToString();
            constantdeclaration.Attributes.Append(docSource.CreateAttribute("id"));
            constantdeclaration.Attributes["id"].Value = "i" + node.unique;
            constantdeclaration.Attributes.Append(docSource.CreateAttribute("line"));
            constantdeclaration.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            constantdeclaration.Attributes.Append(docSource.CreateAttribute("endline"));
            constantdeclaration.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));

            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            constantdeclaration.AppendChild(modifier);

            if (node.type != null)
            {
                VisitInContext(node.type, constantdeclaration);
            }
            if (node.initializer != null)
            {
                XmlNode initializernode = docSource.CreateNode(XmlNodeType.Element, "initializer", "");
                initializernode.Attributes.Append(docSource.CreateAttribute("value"));

                if (node.initializer is CCI_LITERAL)
                    initializernode.Attributes["value"].Value = ((CCI_LITERAL)node.initializer).literal.Value.ToString();
                else
                    if (node.initializer is INTEGER_LITERAL)
                        initializernode.Attributes["value"].Value = ((INTEGER_LITERAL)node.initializer).integer.ToString();
                    else
                        if (node.initializer is STRING_LITERAL)
                        {
                            string s = ((STRING_LITERAL)node.initializer).str;
                            // The string must be xml compatible - replace all special signs
                            // USing .NET xml classes, only \0 must be replaced
                            /*
                            s = s.Replace("<", "&lt;");
                            s = s.Replace(">", "&gt;");
                            s = s.Replace("\"", "&quot;");
                            s = s.Replace("'", "&#39;");
                            */
                            if (s == "\0")
                                s = "0X";
                            initializernode.Attributes["value"].Value = s;
                        }
                        else
                            if (node.initializer is CHAR_LITERAL)
                            {
                                string s = ((CHAR_LITERAL)node.initializer).ch.ToString();
                                // The string must be xml compatible - replace all special signs
                                /*
                                s = s.Replace("<", "&lt;");
                                s = s.Replace(">", "&gt;");
                                s = s.Replace("\"", "&quot;");
                                s = s.Replace("'", "&#39;");
                                */
                                if (s == "\0")
                                    s = "0X";
                                initializernode.Attributes["value"].Value = ((CHAR_LITERAL)node.initializer).ch.ToString();
                            }
                            else
                                if (node.initializer is REAL_LITERAL)
                                    initializernode.Attributes["value"].Value = ((REAL_LITERAL)node.initializer).real.ToString();
                constantdeclaration.AppendChild(initializernode);
            }
            parent.AppendChild(constantdeclaration);


        }
        protected override void Visit_TYPE_DECL(TYPE_DECL node)
        {
            XmlNode typedeclaration = docSource.CreateNode(XmlNodeType.Element, "typedeclaration", "");

            typedeclaration.Attributes.Append(docSource.CreateAttribute("name"));
            typedeclaration.Attributes["name"].Value = node.name.ToString();
            typedeclaration.Attributes.Append(docSource.CreateAttribute("id"));
            typedeclaration.Attributes["id"].Value = "i" + node.unique.ToString();
            typedeclaration.Attributes.Append(docSource.CreateAttribute("line"));
            typedeclaration.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            typedeclaration.Attributes.Append(docSource.CreateAttribute("endline"));
            typedeclaration.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));
            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "private";
            typedeclaration.AppendChild(modifier);

            if (node.type is UNKNOWN_TYPE)
                node.resolve();
            node.type.resolve();
            if (node.type != null)
            {
                if (node.type is OBJECT_TYPE)
                {
                    string typeName = DECLARATION.resolveObjectTypeNameInDeclaration((OBJECT_TYPE)node.type, node);

                    if (typeName != null)
                    {
                        XmlNode objecttype = docSource.CreateNode(XmlNodeType.Element, "objecttype", "");
                        objecttype.Attributes.Append(docSource.CreateAttribute("value"));
                        objecttype.Attributes["value"].Value = typeName;
                        objecttype.Attributes.Append(docSource.CreateAttribute("type"));
                        objecttype.Attributes["type"].Value = ((OBJECT_TYPE)node.type).ObjectUnit.generateXPath();

                        typedeclaration.AppendChild(objecttype);
                    }
                    else
                        VisitInContext(node.type, typedeclaration);

                }
                else if (node.type is EXTERNAL_TYPE)
                {
                    VisitInContext(node.type, typedeclaration);
                }
                else
                    VisitInContext(node.type, typedeclaration);
            }
            parent.AppendChild(typedeclaration);


        }
        protected override void Visit_ENUMERATOR_DECL(ENUMERATOR_DECL node)
        {
            XmlNode enumeratordeclaration = docSource.CreateNode(XmlNodeType.Element, "enumeratordeclaration", "");
            enumeratordeclaration.Attributes.Append(docSource.CreateAttribute("name"));
            enumeratordeclaration.Attributes["name"].Value = node.name.ToString();
            enumeratordeclaration.Attributes.Append(docSource.CreateAttribute("value"));
            enumeratordeclaration.Attributes["value"].Value = node.val.ToString();
            enumeratordeclaration.Attributes.Append(docSource.CreateAttribute("id"));
            enumeratordeclaration.Attributes["id"].Value = "i" + node.unique.ToString();
            enumeratordeclaration.Attributes.Append(docSource.CreateAttribute("line"));
            enumeratordeclaration.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            enumeratordeclaration.Attributes.Append(docSource.CreateAttribute("endline"));
            enumeratordeclaration.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            parent.AppendChild(enumeratordeclaration);


        }
        protected override void Visit_PROTOCOL_DECL(PROTOCOL_DECL node)
        {
            XmlNode protocol = docSource.CreateNode(XmlNodeType.Element, "protocol", "");
            protocol.Attributes.Append(docSource.CreateAttribute("name"));
            protocol.Attributes["name"].Value = node.name.ToString();
            protocol.Attributes.Append(docSource.CreateAttribute("id"));
            protocol.Attributes["id"].Value = "i" + node.unique.ToString();
            protocol.Attributes.Append(docSource.CreateAttribute("line"));
            protocol.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            protocol.Attributes.Append(docSource.CreateAttribute("endline"));
            protocol.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

            XmlNode modifier = docSource.CreateNode(XmlNodeType.Element, "modifier", "");
            modifier.Attributes.Append(docSource.CreateAttribute("name"));
            if (node.modifiers.Public)
                modifier.Attributes["name"].Value = "public";
            else
                modifier.Attributes["name"].Value = "public";
            protocol.AppendChild(modifier);

            if (node.keywords != null)
                VisitInContext(node.keywords, protocol);
            if (node.syntax != null)
                VisitInContext(node.syntax, protocol);
            parent.AppendChild(protocol);


        }
        #endregion
        #endregion

        #region EXTENSION
        protected override void Visit_SYNTAX(SYNTAX node)
        {
            XmlNode syntax = docSource.CreateNode(XmlNodeType.Element, "syntax", "");
            if (node.productions != null && node.productions.Length > 0)
            {
                for (int i = 0, n = node.productions.Length; i < n; i++)
                    VisitInContext(node.productions[i], syntax);
            }
            parent.AppendChild(syntax);


        }
        protected override void Visit_PRODUCTION(PRODUCTION node)
        {
            XmlNode production = docSource.CreateNode(XmlNodeType.Element, "production", "");
            production.Attributes.Append(docSource.CreateAttribute("name"));
            production.Attributes["name"].Value = node.name.ToString();
            production.Attributes.Append(docSource.CreateAttribute("id"));
            production.Attributes["id"].Value = "i" + node.unique.ToString();
            production.Attributes.Append(docSource.CreateAttribute("line"));
            production.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            production.Attributes.Append(docSource.CreateAttribute("endline"));
            production.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();
            VisitInContext(node.right_part, production);
            parent.AppendChild(production);


        }
        #region UNIT
        protected override void Visit_TERMINAL(TERMINAL node)
        {
            XmlNode terminal = docSource.CreateNode(XmlNodeType.Element, "terminal", "");
            terminal.Attributes.Append(docSource.CreateAttribute("name"));
            terminal.Attributes["name"].Value = node.enumerator.name.ToString();
            terminal.Attributes.Append(docSource.CreateAttribute("receive"));
            terminal.Attributes["receive"].Value = node.receive == true ? "true" : "false";
            parent.AppendChild(terminal);


        }
        protected override void Visit_TYPE_NAME(TYPE_NAME node)
        {
            XmlNode typename = docSource.CreateNode(XmlNodeType.Element, "typename", "");
            typename.Attributes.Append(docSource.CreateAttribute("receive"));
            typename.Attributes["receive"].Value = node.receive == true ? "true" : "false";
            VisitInContext(node.type_name, typename);
            parent.AppendChild(typename);


        }
        protected override void Visit_CONSTANT(CONSTANT node)
        {
            XmlNode constant = docSource.CreateNode(XmlNodeType.Element, "constant", "");
            constant.Attributes.Append(docSource.CreateAttribute("receive"));
            constant.Attributes["receive"].Value = node.receive == true ? "true" : "false";
            VisitInContext(node.literal, constant);
            parent.AppendChild(constant);


        }
        protected override void Visit_NONTERMINAL(NONTERMINAL node)
        {
            XmlNode nonterminal = docSource.CreateNode(XmlNodeType.Element, "nonterminal", "");
            nonterminal.Attributes.Append(docSource.CreateAttribute("name"));
            nonterminal.Attributes["name"].Value = node.production.name.ToString();
            nonterminal.Attributes.Append(docSource.CreateAttribute("ref"));
            nonterminal.Attributes["ref"].Value = node.production.name.ToString();
            parent.AppendChild(nonterminal);


        }
        protected override void Visit_UNKNOWN_NONTERMINAL(UNKNOWN_NONTERMINAL node)
        {
            /* do nothing */

        }
        protected override void Visit_SEQUENCE(SEQUENCE node)
        {
            if (node.sequence != null)
            {
                XmlNode sequencenode = docSource.CreateNode(XmlNodeType.Element, "sequence", "");
                sequencenode.Attributes.Append(docSource.CreateAttribute("line"));
                sequencenode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
                sequencenode.Attributes.Append(docSource.CreateAttribute("endline"));
                sequencenode.Attributes["endline"].Value = node.sourceContext.EndLine.ToString();

                for (int i = 0; i < node.sequence.Length; i++)
                {
                    VisitInContext(node.sequence[i], sequencenode);
                }
                parent.AppendChild(sequencenode);
            }


        }
        protected override void Visit_ALTERNATIVES(ALTERNATIVES node)
        {
            XmlNode alternativesnode = docSource.CreateNode(XmlNodeType.Element, "alternatives", "");
            alternativesnode.Attributes.Append(docSource.CreateAttribute("line"));
            alternativesnode.Attributes["line"].Value = node.sourceContext.StartLine.ToString();
            for (int i = 0; i < node.alternatives.Length; i++)
            {
                VisitInContext(node.alternatives[i], alternativesnode);
            }
            parent.AppendChild(alternativesnode);


        }
        #endregion
        #endregion
    }
}
