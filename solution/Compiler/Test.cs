using System;

namespace Compiler
{
    // TYPE t = ARRAY const_expr OF type;

    using Console = System.Console;
	public struct ArrayTypeConst
	{
        private int[] array;

        private void create ( ) { if ( array == null ) array = new int[10]; }

        public int this[int i] 
        { 
            get { create(); return array[i]; }
            set { create(); array[i] = value; } 
        }
	}

    // TYPE t = ARRAY expr OF type;

    public struct ArrayTypeExpr
    {
        private int[] array;

        public ArrayTypeExpr ( int s ) { array = new int[s]; }

        public int this[int i]
        {
            get { return array[i]; }
            set { array[i] = value; }
        }
    }
}
