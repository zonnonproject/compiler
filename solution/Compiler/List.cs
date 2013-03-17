using System; 
using System.Collections;
using System.Reflection;
using System.Compiler;

namespace ETH.Zonnon.Compiler
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

public sealed class COMPILATION_UNIT_LIST  //////////////////////////////////
{
    private COMPILATION_UNIT[] compilation_units;
    private int length = 0;

    public COMPILATION_UNIT_LIST (       ) { this.compilation_units = new COMPILATION_UNIT[8]; }
    public COMPILATION_UNIT_LIST ( int n ) { this.compilation_units = new COMPILATION_UNIT[n]; }

    public void Add ( COMPILATION_UNIT compilation_unit )
    {
        int n = this.compilation_units.Length;
        int i = this.length++;
        if ( i == n )
        {
            COMPILATION_UNIT[] newCUs = new COMPILATION_UNIT[n+8];
            for ( int j = 0; j < n; j++ ) newCUs[j] = compilation_units[j];
            this.compilation_units = newCUs;
        }
        this.compilation_units[i] = compilation_unit;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public COMPILATION_UNIT this [ int index ] { get { return this.compilation_units[index]; }
                                                 set { this.compilation_units[index] = value; } }

    public COMPILATION_UNIT find ( COMPILATION_UNIT cu )
    {
        COMPILATION_UNIT result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( compilation_units[i] == cu ) { result = compilation_units[i]; break; }
        }
        return result;
    }

    public COMPILATION_UNIT find ( Identifier name )
    {
        COMPILATION_UNIT result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( compilation_units[i].name.Name == name.Name ) { result = compilation_units[i]; break; }
        }
        return result;
    }
}

public sealed class IMPORT_LIST  //////////////////////////////////
{
    private IMPORT_DECLARATION[] imports;
    private int length = 0;

    public IMPORT_LIST (       ) { this.imports = new IMPORT_DECLARATION[8]; }
    public IMPORT_LIST ( int n ) { this.imports = new IMPORT_DECLARATION[n]; }

    public void Add ( IMPORT_DECLARATION import )
    {
        int n = this.imports.Length;
        int i = this.length++;
        if ( i == n )
        {
            IMPORT_DECLARATION[] newImports = new IMPORT_DECLARATION[n+8];
            for ( int j = 0; j < n; j++ ) newImports[j] = imports[j];
            this.imports = newImports;
        }
        this.imports[i] = import;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public IMPORT_DECLARATION this [ int index ] { get { return this.imports[index]; }
                                                   set { this.imports[index] = value; } }

    public IMPORT_DECLARATION find ( IMPORT_DECLARATION import )
    {
        IMPORT_DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( imports[i] == import ) { result = imports[i]; break; }
        }
        return result;
    }

    public IMPORT_DECLARATION find ( Identifier name )
    {
        IMPORT_DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( imports[i].name.Name == name.Name ) { result = imports[i]; break; }
        }
        return result;
    }
}

public sealed class NAMESPACE_LIST  //////////////////////////////////
{
    private NAMESPACE[] namespaces;
    private int length = 0;

    public NAMESPACE_LIST (       ) { this.namespaces = new NAMESPACE[8]; }
    public NAMESPACE_LIST ( int n ) { this.namespaces = new NAMESPACE[n]; }

    public void Add ( NAMESPACE ns )
    {
        int n = this.namespaces.Length;
        int i = this.length++;
        if ( i == n )
        {
            NAMESPACE[] new_namespaces = new NAMESPACE[n+8];
            for ( int j = 0; j < n; j++ ) new_namespaces[j] = namespaces[j];
            this.namespaces = new_namespaces;
        }
        this.namespaces[i] = ns;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public NAMESPACE this [ int index ] { get { return this.namespaces[index]; }
                                          set { this.namespaces[index] = value; } }

    public NAMESPACE find ( NAMESPACE ns )
    {
        NAMESPACE result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( namespaces[i] == ns ) { result = namespaces[i]; break; }
        }
        return result;
    }

    public NAMESPACE find ( Identifier name )
    {
        NAMESPACE result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( namespaces[i].name.Name == name.Name ) { result = namespaces[i]; break; }
        }
        return result;
    }
}

public sealed class DEFINITION_LIST  ///////////////////////////////////////
{
    private DEFINITION[] definitions;
    private int length = 0;

    public DEFINITION_LIST (       ) { this.definitions = new DEFINITION[8]; }
    public DEFINITION_LIST ( int n ) { this.definitions = new DEFINITION[n]; }

    public void Add ( DEFINITION definition )
    {
        int n = this.definitions.Length;
        int i = this.length++;
        if ( i == n )
        {
            DEFINITION[] new_definitions = new DEFINITION[n+8];
            for ( int j=0; j<n; j++ ) new_definitions[j] = definitions[j];
            this.definitions = new_definitions;
        }
        this.definitions[i] = definition;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public DEFINITION this [ int index ] { get { return this.definitions[index]; }
                                           set { this.definitions[index] = value; } }

    public DEFINITION find ( DEFINITION def )
    {
        DEFINITION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( definitions[i] == def ) { result = definitions[i]; break; }
        }
        return result;
    }

    public DEFINITION find ( Identifier name )
    {
        DEFINITION result = null;
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

    public DECLARATION find ( DECLARATION decl )
    {
        DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i] == decl ) { result = declarations[i]; break; }
        }
        return result;
    }

    public DECLARATION find ( Identifier name )
    {
        DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            Identifier decl_name = declarations[i].name;
            if ( decl_name == null ) continue;
            if ( decl_name.Name == name.Name ) { result = declarations[i]; break; }
        }
        return result;
    }
}

public sealed class VARIABLE_DECLARATION_LIST  /////// for formals /////////////////////////
{
    private VARIABLE_DECLARATION[] declarations;
    private int length = 0;

    public VARIABLE_DECLARATION_LIST (       ) { this.declarations = new VARIABLE_DECLARATION[8]; }
    public VARIABLE_DECLARATION_LIST ( int n ) { this.declarations = new VARIABLE_DECLARATION[n]; }

    public void Add ( VARIABLE_DECLARATION dcl )
    {
        int n = this.declarations.Length;
        int i = this.length++;
        if ( i == n )
        {
            VARIABLE_DECLARATION[] new_declarations = new VARIABLE_DECLARATION[n+8];
            for ( int j=0; j<n; j++ ) new_declarations[j] = declarations[j];
            this.declarations = new_declarations;
        }
        this.declarations[i] = dcl;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public VARIABLE_DECLARATION this [ int index ] { get { return this.declarations[index]; }
                                                     set { this.declarations[index] = value; } }

    public VARIABLE_DECLARATION find ( VARIABLE_DECLARATION decl )
    {
        VARIABLE_DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i] == decl ) { result = declarations[i]; break; }
        }
        return result;
    }

    public VARIABLE_DECLARATION find ( Identifier name )
    {
        VARIABLE_DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( declarations[i].name.Name == name.Name ) { result = declarations[i]; break; }
        }
        return result;
    }
}

public sealed class ACTIVITY_DECLARATION_LIST  /////////////////////////////////////////////
{
    private ACTIVITY_DECLARATION[] activities;
    private int length = 0;

    public ACTIVITY_DECLARATION_LIST (       ) { this.activities = new ACTIVITY_DECLARATION[8]; }
    public ACTIVITY_DECLARATION_LIST ( int n ) { this.activities = new ACTIVITY_DECLARATION[n]; }

    public void Add ( ACTIVITY_DECLARATION act )
    {
        int n = this.activities.Length;
        int i = this.length++;
        if ( i == n )
        {
            ACTIVITY_DECLARATION[] new_activities = new ACTIVITY_DECLARATION[n+8];
            for ( int j=0; j<n; j++ ) new_activities[j] = activities[j];
            this.activities = new_activities;
        }
        this.activities[i] = act;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public ACTIVITY_DECLARATION this [ int index ] { get { return this.activities[index]; }
                                                     set { this.activities[index] = value; } }

    public ACTIVITY_DECLARATION find ( ACTIVITY_DECLARATION act )
    {
        ACTIVITY_DECLARATION result = null;
        for ( int i=0, n=Length; i<n; i++ )
        {
            if ( activities[i] == act ) { result = activities[i]; break; }
        }
        return result;
    }
}

public sealed class STATEMENT_LIST  ///////////////////////////////////////
{
    private STATEMENT[] statements;
    private int length = 0;

    public STATEMENT_LIST (       ) { this.statements = new STATEMENT[8]; }
    public STATEMENT_LIST ( int n ) { this.statements = new STATEMENT[n]; }

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

public sealed class ENUMERATOR_LIST  ///////////////////////////////////////
{
    private ENUMERATOR_DECLARATION[] enumerators;
    private int length = 0;

    public ENUMERATOR_LIST (       ) { this.enumerators = new ENUMERATOR_DECLARATION[8]; }
    public ENUMERATOR_LIST ( int n ) { this.enumerators = new ENUMERATOR_DECLARATION[n]; }

    public void Add ( ENUMERATOR_DECLARATION enumerator )
    {
        int n = this.enumerators.Length;
        int i = this.length++;
        if ( i == n )
        {
            ENUMERATOR_DECLARATION[] new_enumerators = new ENUMERATOR_DECLARATION[n+8];
            for ( int j = 0; j < n; j++ ) new_enumerators[j] = enumerators[j];
            this.enumerators = new_enumerators;
        }
        this.enumerators[i] = enumerator;
    }

    public int Length { get { return this.length; }
                        set { this.length = value; } }

    public ENUMERATOR_DECLARATION this [ int index ] { get { return this.enumerators[index]; }
                                                       set { this.enumerators[index] = value; } }

    public ENUMERATOR_DECLARATION find ( Identifier name )
    {
        ENUMERATOR_DECLARATION result = null;
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

public sealed class IDENT_LIST  //////////////////////////////////
{
    private Identifier[] ids;
    private int length = 0;

    public IDENT_LIST (       ) { this.ids = new Identifier[8]; }
    public IDENT_LIST ( int n ) { this.ids = new Identifier[n]; }

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
