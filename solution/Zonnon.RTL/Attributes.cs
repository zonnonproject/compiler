using System;
using System.Compiler;

namespace Zonnon.RTL
{
	public class ZonnonAttr : System.Attribute
	{
		public ZonnonAttr ( ) { }

        public virtual AttributeNode genAttr ( )
        {
            AttributeNode attr = new AttributeNode();
            attr.Constructor = new QualifiedIdentifier(new QualifiedIdentifier(Identifier.For("Zonnon"),
                                                                               Identifier.For("RTL")),
                                                       null /*Identifier.For(attrName)*/);
            attr.Expressions = new ExpressionList();
            return attr;
        }

        public AttributeNode addName ( AttributeNode attr, string name )
        {
            ((QualifiedIdentifier)attr.Constructor).Identifier = Identifier.For(name);
            return attr;
        }
	}

    //------------------------------------------------------------------------------

    public class ZonnonStdType : ZonnonAttr
    {
        public int width;

        public ZonnonStdType ( int w ) { width = w; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(width,SystemTypes.Int32));
            return attr;
        }
    }

    public class IntegerType : ZonnonStdType
    {
        public IntegerType ( int w ) : base(w) { }
        public override AttributeNode genAttr ( ) { return addName(base.genAttr(),"IntegerType"); }
    }

    public class CardinalType : ZonnonStdType
    {
        public CardinalType ( int w ) : base(w) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"CardinalType"); }
    }

    public class RealType : ZonnonStdType
    {
        public RealType ( int w ) : base(w) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"RealType"); }
    }

    public class CharType : ZonnonStdType
    {
        public CharType ( int w ) : base(w) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"CharType"); }
    }

    public class BooleanType : ZonnonStdType
    {
        public BooleanType ( int w ) : base(w) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"BooleanType"); }
    }

    public class SetType : ZonnonStdType
    {
        public SetType ( int w ) : base(w) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"SetType"); }
    }

    //-----------------------------------------------------------------------------

    public class ArrayType : ZonnonAttr
    {
        public int elem_type_ref;
        public int dims;

        public ArrayType ( int r, int d ) { elem_type_ref = r; dims = d; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(elem_type_ref,SystemTypes.Int32));
            attr.Expressions.Add(new Literal(dims,SystemTypes.Int32));
            return addName(attr,"ArrayType"); 
        }
    }

    public class ProcType : ZonnonAttr
    {
        public int return_type_ref;
        public int pars;

        public ProcType ( int r, int p ) { return_type_ref = r; pars = p; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(return_type_ref,SystemTypes.Int32));
            attr.Expressions.Add(new Literal(pars,SystemTypes.Int32));
            return addName(attr,"ProcType"); 
        }
    }

    public class InterfaceType : ZonnonAttr
    {
        public int defs;

        public InterfaceType ( int d ) { defs = d; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(defs,SystemTypes.Int32));
            return addName(attr,"InterfaceType");  
        }
    }

    public class EnumType : ZonnonAttr
    {
        public int enums;

        public EnumType ( int e ) { enums = e; }
        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(enums,SystemTypes.Int32));
            return addName(attr,"EnumType"); 
        }
    }

    public class Parameter : ZonnonAttr
    {
        public bool var;
        public int  type_ref;

        public Parameter ( bool v, int r ) { var = v; type_ref = r; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(var,SystemTypes.Boolean));
            attr.Expressions.Add(new Literal(type_ref,SystemTypes.Int32));
            return addName(attr,"Parameter"); 
        }
    }

    public class Dimension : ZonnonAttr
    {
        public int size;

        public Dimension ( int s ) { size = s; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(size,SystemTypes.Int32));
            return addName(attr,"Dimension"); 
        }
    }

    public class Implemented : ZonnonAttr
    {
        public int def_ref;

        public Implemented ( int r ) { def_ref = r; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(def_ref,SystemTypes.Int32));
            return addName(attr,"Implemented"); 
        }
    }

    //------------------------------------------------------------------------

    public class ZonnonDecl : ZonnonAttr
    {
        public string name;
        public int    type_ref;
        public int    owner_ref;

        public ZonnonDecl ( string n, int t, int o ) { name=n; type_ref=t; owner_ref=o; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(name,SystemTypes.String));
            attr.Expressions.Add(new Literal(type_ref,SystemTypes.Int32));
            attr.Expressions.Add(new Literal(owner_ref,SystemTypes.Int32));
            return attr;
        }
    }

    public class Variable : ZonnonDecl
    {
        public Variable ( string n, int t, int o ) : base(n,t,o) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"Variable"); }
    }

    public class Constant : ZonnonDecl
    {
        public object val;

        public Constant ( string n, int t, int o, object v ) : base(n,t,o) { val = v; }

        public override AttributeNode genAttr() 
        { 
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(val,SystemTypes.Object));
            return addName(attr,"Constant"); 
        }
    }

    public class Type : ZonnonDecl
    {
        public Type ( string n, int t, int o ) : base(n,t,o) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"Type"); }
    }

    public class Enumerator : ZonnonDecl
    {
        public Enumerator ( string n, int t ) : base(n,t,t) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"Eumerator"); }
    }

    //---------------------------------------------------------------------------------------

    public class ZonnonUnit : ZonnonAttr
    {
        public string name;
        public int    end_ref;

        public ZonnonUnit ( string n, int e ) { name=n; end_ref=e; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(name,SystemTypes.String));
            attr.Expressions.Add(new Literal(end_ref,SystemTypes.Int32));
            return attr;
        }
    }

    public sealed class Object : ZonnonUnit
    {
        public int defs;

        public Object ( string n, int r, int d ) : base(n,r) { defs=d; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(defs,SystemTypes.Int32));
            return addName(attr,"Object");
        }
    }

    public sealed class Definition : ZonnonUnit
    {
        public int base_ref;

        public Definition ( string n, int r, int b ) : base(n,r) { base_ref=b; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(base_ref,SystemTypes.Int32));
            return addName(attr,"Definition");
        }
    }

    public sealed class Implementation : ZonnonUnit
    {
        public int def_ref;

        public Implementation ( string n, int r, int d ) : base(n,r) { def_ref=d; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(def_ref,SystemTypes.Int32));
            return addName(attr,"Implementation");
        }
    }

    public class ZonnonEndUnit : ZonnonAttr
    {
        public int unit_ref;

        public ZonnonEndUnit ( int r ) { unit_ref = r; }

        public override AttributeNode genAttr ( )
        {
            AttributeNode attr = base.genAttr();
            attr.Expressions.Add(new Literal(unit_ref,SystemTypes.Int32));
            return attr;
        }
    }

    public sealed class EndObject : ZonnonEndUnit
    {
        public EndObject ( int r ) : base(r) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"EndObject"); }
    }

    public sealed class EndDefinition : ZonnonEndUnit
    {
        public EndDefinition ( int r ) : base(r) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"EndDefinition"); }
    }

    public sealed class EndImplementation : ZonnonEndUnit
    {
        public EndImplementation ( int r ) : base(r) { }
        public override AttributeNode genAttr() { return addName(base.genAttr(),"EndImplementation"); }
    }
}
