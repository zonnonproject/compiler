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
using System.Collections;
using System.Reflection;
using System.Compiler;
using System.Xml;

namespace ETH.Zonnon
{

/////// LISTs ////////////////////////////////////////////////////////////////////////////


public sealed class NODE_LIST  //////////////////////////////////
{
    private NODE[] nodes;
    private int length = 0;

    public NODE_LIST (       ) { this.nodes = new NODE[8]; }
    public NODE_LIST ( int n ) { this.nodes = new NODE[n]; }

    public void Add ( NODE node )
    {
        int n = this.nodes.Length;
        int i = this.length++;
        if ( i == n )
        {
            NODE[] new_nodes = new NODE[n+8];
            for ( int j = 0; j < n; j++ ) new_nodes[j] = nodes[j];
            this.nodes = new_nodes;
        }
        this.nodes[i] = node;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public NODE this [ int index ] { get { return this.nodes[index]; }
                                     set { this.nodes[index] = value; } }

    public NODE find ( NODE node )
    {
        NODE result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( nodes[i] == node ) { result = nodes[i]; break; }
        }
        return result;
    }

    public NODE find ( Identifier name )
    {
        NODE result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( nodes[i].name.Name == name.Name ) { result = nodes[i]; break; }
        }
        return result;
    }
}
    
public sealed class UNIT_DECL_LIST  //////////////////////////////////
{
    // Consists of DECLARATIONs: either UNIT_DECL or UNKNOWN_DECL
    private UNIT_DECL[] units;
    private int length = 0;

    public UNIT_DECL_LIST (       ) { this.units = new UNIT_DECL[8]; }
    public UNIT_DECL_LIST ( int n ) { this.units = new UNIT_DECL[n]; }

    public void Add ( UNIT_DECL unit )
    {
        int n = this.units.Length;
        int i = this.length++;
        if ( i == n )
        {
            UNIT_DECL[] newUnits = new UNIT_DECL[n+8];
            for ( int j = 0; j < n; j++ ) newUnits[j] = units[j];
            this.units = newUnits;
        }
        this.units[i] = unit;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public UNIT_DECL this [ int index ] { get { return this.units[index]; }
                                            set { this.units[index] = value; } }

    public UNIT_DECL find ( UNIT_DECL unit )
    {
        UNIT_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( units[i] == unit ) { result = units[i]; break; }
        }
        return result;
    }

    public UNIT_DECL find ( Identifier name )
    {
        UNIT_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( units[i].name.Name == name.Name ) { result = units[i]; break; }
        }
        return result;
    }
}

public sealed class IMPORT_LIST  //////////////////////////////////
{
    private IMPORT_DECL[] imports;
    private int length = 0;

    public IMPORT_LIST (       ) { this.imports = new IMPORT_DECL[8]; }
    public IMPORT_LIST ( int n ) { this.imports = new IMPORT_DECL[n]; }

    public void Add ( IMPORT_DECL import )
    {
        int n = this.imports.Length;
        int i = this.length++;
        if ( i == n )
        {
            IMPORT_DECL[] newImports = new IMPORT_DECL[n+8];
            for ( int j = 0; j < n; j++ ) newImports[j] = imports[j];
            this.imports = newImports;
        }
        this.imports[i] = import;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public IMPORT_DECL this [ int index ] { get { return this.imports[index]; }
                                            set { this.imports[index] = value; } }

    public IMPORT_DECL find ( IMPORT_DECL import )
    {
        IMPORT_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( imports[i] == import ) { result = imports[i]; break; }
        }
        return result;
    }

    public IMPORT_DECL find ( Identifier name )
    {
        IMPORT_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( imports[i].name.Name == name.Name ) { result = imports[i]; break; }
        }
        return result;
    }
}

public sealed class NAMESPACE_DECL_LIST  //////////////////////////////////
{
    private NAMESPACE_DECL[] namespaces;
    private int length = 0;

    public NAMESPACE_DECL_LIST (       ) { this.namespaces = new NAMESPACE_DECL[8]; }
    public NAMESPACE_DECL_LIST ( int n ) { this.namespaces = new NAMESPACE_DECL[n]; }

    public void Add ( NAMESPACE_DECL ns )
    {
        int n = this.namespaces.Length;
        int i = this.length++;
        if ( i == n )
        {
            NAMESPACE_DECL[] new_namespaces = new NAMESPACE_DECL[n+8];
            for ( int j = 0; j < n; j++ ) new_namespaces[j] = namespaces[j];
            this.namespaces = new_namespaces;
        }
        this.namespaces[i] = ns;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public NAMESPACE_DECL this [ int index ] { get { return this.namespaces[index]; }
                                               set { this.namespaces[index] = value; } }

    public NAMESPACE_DECL find ( NAMESPACE_DECL ns )
    {
        NAMESPACE_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( namespaces[i] == ns ) { result = namespaces[i]; break; }
        }
        return result;
    }

    public NAMESPACE_DECL find ( Identifier name )
    {
        NAMESPACE_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( namespaces[i].name.Name == name.Name ) { result = namespaces[i]; break; }
        }
        return result;
    }
}

public sealed class DEFINITION_DECL_LIST  ///////////////////////////////////////
{
    private DEFINITION_DECL[] definitions;
    private int length = 0;

    public DEFINITION_DECL_LIST (       ) { this.definitions = new DEFINITION_DECL[8]; }
    public DEFINITION_DECL_LIST ( int n ) { this.definitions = new DEFINITION_DECL[n]; }

    public void Add ( DEFINITION_DECL definition )
    {
        int n = this.definitions.Length;
        int i = this.length++;
        if ( i == n )
        {
            DEFINITION_DECL[] new_definitions = new DEFINITION_DECL[n+8];
            for ( int j=0; j<n; j++ ) new_definitions[j] = definitions[j];
            this.definitions = new_definitions;
        }
        this.definitions[i] = definition;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public DEFINITION_DECL this [ int index ] { get { return this.definitions[index]; }
                                           set { this.definitions[index] = value; } }

    public DEFINITION_DECL find ( DEFINITION_DECL def )
    {
        DEFINITION_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( definitions[i] == def ) { result = definitions[i]; break; }
        }
        return result;
    }

    public DEFINITION_DECL find ( Identifier name )
    {
        DEFINITION_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( definitions[i].name.Name == name.Name ) { result = definitions[i]; break; }
        }
        return result;
    }
}

public sealed class DECLARATION_LIST  ////////////////////////////////
{
    private DECLARATION[] declarations;
    private int length = 0;

    public DECLARATION_LIST (       ) { this.declarations = new DECLARATION[8]; }
    public DECLARATION_LIST ( int n ) { this.declarations = new DECLARATION[n]; }

    public void Add ( DECLARATION dcl )
    {
        int n = this.declarations.Length;
        int i = this.length++;
        if ( i == n )
        {
            DECLARATION[] new_declarations = new DECLARATION[n+8];
            for ( int j=0; j<n; j++ ) new_declarations[j] = declarations[j];
            this.declarations = new_declarations;
        }
        this.declarations[i] = dcl;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public DECLARATION this [ int index ] { get { return this.declarations[index]; }
                                            set { this.declarations[index] = value; } }

    public void replace(DECLARATION decl, DECLARATION newdecl)
    {
        for (int i = 0, n = Length; i < n; i++)
        {
            if (declarations[i] == decl) { declarations[i] = newdecl; break; }
        }
    }


    public DECLARATION find ( DECLARATION decl )
    {
        DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i] == decl ) { result = declarations[i]; break; }
        }
        return result;
    }

    public DECLARATION find(Identifier name)
    {
        return find(name, false);
    }
    public DECLARATION find ( Identifier name, bool excludeImportSpace)
    {
        if (name.Name.Length == 0) return null; // Fix: find out why this happens
        DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            Identifier decl_name = declarations[i].name;
            if ( decl_name == null ) continue;
            if (excludeImportSpace && declarations[i] is IMPORTSPACE_DECL) continue;
            if (excludeImportSpace && declarations[i] is IMPORT_DECL) continue;
            if (declarations[i] is OPERATOR_DECL)
                //Separate case in case of looking for an operator by its code
                //TODO: Replace the code by its synonym before the search
            {
                OPERATOR_DECL opertator = declarations[i] as OPERATOR_DECL;
                if (opertator.code.CompareTo(name.Name) == 0)
                {
                    result = declarations[i]; break;
                }
            }
            if ( decl_name.Name == name.Name ) { result = declarations[i]; break; }
        }
        return result;
    }
}

public sealed class VARIABLE_DECL_LIST  /////// for formals /////////////////////////
{
    private VARIABLE_DECL[] declarations;
    private int length = 0;

    public VARIABLE_DECL_LIST (       ) { this.declarations = new VARIABLE_DECL[8]; }
    public VARIABLE_DECL_LIST ( int n ) { this.declarations = new VARIABLE_DECL[n]; }

    public void Add ( VARIABLE_DECL dcl )
    {
        int n = this.declarations.Length;
        int i = this.length++;
        if ( i == n )
        {
            VARIABLE_DECL[] new_declarations = new VARIABLE_DECL[n+8];
            for ( int j=0; j<n; j++ ) new_declarations[j] = declarations[j];
            this.declarations = new_declarations;
        }
        this.declarations[i] = dcl;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public VARIABLE_DECL this [ int index ] { get { return this.declarations[index]; }
                                              set { this.declarations[index] = value; } }

    public VARIABLE_DECL find ( VARIABLE_DECL decl )
    {
        VARIABLE_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i] == decl ) { result = declarations[i]; break; }
        }
        return result;
    }

    public VARIABLE_DECL find ( Identifier name )
    {
        VARIABLE_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i].name.Name == name.Name ) { result = declarations[i]; break; }
        }
        return result;
    }
}

public sealed class ACTIVITY_DECL_LIST  /////////////////////////////////////////////
{
    private ACTIVITY_DECL[] activities;
    private int length = 0;

    public ACTIVITY_DECL_LIST (       ) { this.activities = new ACTIVITY_DECL[8]; }
    public ACTIVITY_DECL_LIST ( int n ) { this.activities = new ACTIVITY_DECL[n]; }

    public void Add ( ACTIVITY_DECL act )
    {
        int n = this.activities.Length;
        int i = this.length++;
        if ( i == n )
        {
            ACTIVITY_DECL[] new_activities = new ACTIVITY_DECL[n+8];
            for ( int j=0; j<n; j++ ) new_activities[j] = activities[j];
            this.activities = new_activities;
        }
        this.activities[i] = act;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public ACTIVITY_DECL this [ int index ] { get { return this.activities[index]; }
                                              set { this.activities[index] = value; } }

    public ACTIVITY_DECL find ( ACTIVITY_DECL act )
    {
        ACTIVITY_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( activities[i] == act ) { result = activities[i]; break; }
        }
        return result;
    }
}

public sealed class OPERATOR_DECL_LIST  /////////////////////////////////////////////
{
    private OPERATOR_DECL[] operators;
    private int length = 0;

    public OPERATOR_DECL_LIST (       ) { this.operators = new OPERATOR_DECL[8]; }
    public OPERATOR_DECL_LIST ( int n ) { this.operators = new OPERATOR_DECL[n]; }

    public void Add ( OPERATOR_DECL op )
    {
        int n = this.operators.Length;
        int i = this.length++;
        if ( i == n )
        {
            OPERATOR_DECL[] new_operators = new OPERATOR_DECL[n+8];
            for ( int j=0; j<n; j++ ) new_operators[j] = operators[j];
            this.operators = new_operators;
        }
        this.operators[i] = op;
    }

    public int Length { get { return this.length; }
        set { this.length = value; } 
    }

    public OPERATOR_DECL this [ int index ] { get { return this.operators[index]; }
        set { this.operators[index] = value; } 
    }
}

public sealed class STATEMENT_LIST : NODE ///////////////////////////////////////
{
    public override Node convert ( ) { return null; }
    public override bool validate ( ) { return true; }
    public override TYPE type { get{return null;} set{} }
    public override NODE resolve ( ) { return null; }    
#if DEBUG
    public override void report ( int shift ) { }
#endif

    private STATEMENT[] statements;
    private int length = 0;

    public STATEMENT_LIST (       ) : base(ASTNodeType.STATEMENT_LIST, null) { this.statements = new STATEMENT[8]; }
    public STATEMENT_LIST ( int n ) : base(ASTNodeType.STATEMENT_LIST, null) { this.statements = new STATEMENT[n]; }

    public void Add ( STATEMENT statement )
    {
        int n = this.statements.Length;
        int i = this.length++;
        if ( i == n )
        {
            STATEMENT[] new_statements = new STATEMENT[n+8];
            for ( int j = 0; j < n; j++ ) new_statements[j] = statements[j];
            this.statements = new_statements;
        }
        this.statements[i] = statement;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public STATEMENT this [ int index ] { get { return this.statements[index]; }
                                          set { this.statements[index] = value; } }
}

public sealed class EXPRESSION_LIST  ///////////////////////////////////////
{
    private EXPRESSION[] expressions;
    private int length = 0;

    public EXPRESSION_LIST (       ) { this.expressions = new EXPRESSION[8]; }
    public EXPRESSION_LIST ( int n ) { this.expressions = new EXPRESSION[n]; }

    public void Add ( EXPRESSION expression )
    {
        int n = this.expressions.Length;
        int i = this.length++;
        if ( i == n )
        {
            EXPRESSION[] new_expressions = new EXPRESSION[n+8];
            for ( int j = 0; j < n; j++ ) new_expressions[j] = expressions[j];
            this.expressions = new_expressions;
        }
        this.expressions[i] = expression;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public EXPRESSION this [ int index ] { get { return this.expressions[index]; }
                                           set { this.expressions[index] = value; } }
}

public sealed class TYPE_LIST  ///////////////////////////////////////
{
    private TYPE[] types;
    private int length = 0;

    public TYPE_LIST (       ) { this.types = new TYPE[8]; }
    public TYPE_LIST ( int n ) { this.types = new TYPE[n]; }

    public void Add ( TYPE type )
    {
        int n = this.types.Length;
        int i = this.length++;
        if ( i == n )
        {
            TYPE[] new_types = new TYPE[n+8];
            for ( int j = 0; j<n; j++ ) new_types[j] = types[j];
            this.types = new_types;
        }
        this.types[i] = type;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public TYPE this [ int index ] { get { return this.types[index]; }
                                     set { this.types[index] = value; } }

    public TYPE find ( TYPE type )
    {
        TYPE result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( types[i] == type ) { result = types[i]; break; }
        }
        return result;
    }
}

public sealed class ENUMERATOR_DECL_LIST  ///////////////////////////////////////
{
    private ENUMERATOR_DECL[] enumerators;
    private int length = 0;

    public ENUMERATOR_DECL_LIST (       ) { this.enumerators = new ENUMERATOR_DECL[8]; }
    public ENUMERATOR_DECL_LIST ( int n ) { this.enumerators = new ENUMERATOR_DECL[n]; }

    public void Add ( ENUMERATOR_DECL enumerator )
    {
        int n = this.enumerators.Length;
        int i = this.length++;
        if ( i == n )
        {
            ENUMERATOR_DECL[] new_enumerators = new ENUMERATOR_DECL[n+8];
            for ( int j = 0; j < n; j++ ) new_enumerators[j] = enumerators[j];
            this.enumerators = new_enumerators;
        }
        this.enumerators[i] = enumerator;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public ENUMERATOR_DECL this [ int index ] { get { return this.enumerators[index]; }
                                                set { this.enumerators[index] = value; } }

    public ENUMERATOR_DECL find ( Identifier name )
    {
        ENUMERATOR_DECL result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( enumerators[i].name.Name == name.Name ) { result = enumerators[i]; break; }
        }
        return result;
    }
}

public sealed class IF_PAIR_LIST  ///////////////////////////////////////
{
    private IF_PAIR[] if_pairs;
    private int length = 0;

    public IF_PAIR_LIST (       ) { this.if_pairs = new IF_PAIR[8]; }
    public IF_PAIR_LIST ( int n ) { this.if_pairs = new IF_PAIR[n]; }

    public void Add ( IF_PAIR if_pair )
    {
        int n = this.if_pairs.Length;
        int i = this.length++;
        if ( i == n )
        {
            IF_PAIR[] new_if_pairs = new IF_PAIR[n+8];
            for ( int j=0; j<n; j++ ) new_if_pairs[j] = if_pairs[j];
            this.if_pairs = new_if_pairs;
        }
        this.if_pairs[i] = if_pair;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public IF_PAIR this [ int index ] { get { return this.if_pairs[index]; }
                                        set { this.if_pairs[index] = value; } }
}

public sealed class RANGE_LIST  ///////////////////////////////////////
{
    private RANGE[] ranges;
    private int length = 0;

    public RANGE_LIST (       ) { this.ranges = new RANGE[8]; }
    public RANGE_LIST ( int n ) { this.ranges = new RANGE[n]; }

    public void Add ( RANGE range )
    {
        int n = this.ranges.Length;
        int i = this.length++;
        if ( i == n )
        {
            RANGE[] new_ranges = new RANGE[n+8];
            for ( int j=0; j<n; j++ ) new_ranges[j] = ranges[j];
            this.ranges = new_ranges;
        }
        this.ranges[i] = range;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public RANGE this [ int index ] { get { return this.ranges[index]; }
                                      set { this.ranges[index] = value; } }
}

public sealed class CASE_ITEM_LIST  ///////////////////////////////////////
{
    private CASE_ITEM[] case_items;
    private int length = 0;

    public CASE_ITEM_LIST (       ) { this.case_items = new CASE_ITEM[8]; }
    public CASE_ITEM_LIST ( int n ) { this.case_items = new CASE_ITEM[n]; }

    public void Add ( CASE_ITEM case_item )
    {
        int n = this.case_items.Length;
        int i = this.length++;
        if ( i == n )
        {
            CASE_ITEM[] new_case_items = new CASE_ITEM[n+8];
            for ( int j=0; j<n; j++ ) new_case_items[j] = case_items[j];
            this.case_items = new_case_items;
        }
        this.case_items[i] = case_item;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public CASE_ITEM this [ int index ] { get { return this.case_items[index]; }
                                          set { this.case_items[index] = value; } }
}

public sealed class EXCEPTION_LIST  ///////////////////////////////////////
{
    private EXCEPTION[] exceptions;
    private int length = 0;

    public EXCEPTION_LIST (       ) { this.exceptions = new EXCEPTION[8]; }
    public EXCEPTION_LIST ( int n ) { this.exceptions = new EXCEPTION[n]; }

    public void Add ( EXCEPTION exception )
    {
        int n = this.exceptions.Length;
        int i = this.length++;
        if ( i == n )
        {
            EXCEPTION[] new_exceptions = new EXCEPTION[n+8];
            for ( int j=0; j<n; j++ ) new_exceptions[j] = exceptions[j];
            this.exceptions = new_exceptions;
        }
        this.exceptions[i] = exception;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public EXCEPTION this [ int index ] { get { return this.exceptions[index]; }
                                          set { this.exceptions[index] = value; } }
}

public sealed class IDENT_LIST : NODE  //////////////////////////////////
{
	public override Node convert ( ) { return null; }
	public override bool validate ( ) { return true; }
	public override TYPE type { get{return null;} set{} }
	public override NODE resolve ( ) { return null; }
    
#if DEBUG
	public override void report ( int shift ) { }
#endif

    private Identifier[] ids;
    private int length = 0;

    public IDENT_LIST (       ) : base(ASTNodeType.IDENT_LIST, null) { this.ids = new Identifier[8]; }
    public IDENT_LIST ( int n ) : base(ASTNodeType.IDENT_LIST, null) { this.ids = new Identifier[n]; }

    public Identifier ToIdentifier()
    {
        return new Identifier(ToString());
    }
    
    public void Add ( Identifier id )
    {
        int n = this.ids.Length;
        int i = this.length++;
        if ( i == n )
        {
            Identifier[] new_ids = new Identifier[n+8];
            for ( int j = 0; j < n; j++ ) new_ids[j] = ids[j];
            this.ids = new_ids;
        }
        this.ids[i] = id;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public Identifier this [ int index ] { get { return this.ids[index]; }
                                           set { this.ids[index] = value; } }

    public override string ToString ( )
    {
        return ToString(1);
    }

    public string ToString ( int EntireName )
    {
        string s = "";
        for ( int i=0, n=(Length-1)+EntireName; i<n; i++ )
        {
            s += ids[i];
            if ( i < n-1 ) s += ".";
        }
        return s;
    }
}

} // ETH.Zonnon.Compiler
