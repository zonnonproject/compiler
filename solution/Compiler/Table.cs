using System;

namespace ETH.Zonnon.Compiler
{
    // TABLE
    // ------
    //
    public class TABLE
    {
        // Cannot create instances of class TABLE
        private TABLE() { }

        private static SYMBOL[] table;

        static TABLE()
        {
            table = new SYMBOL[Scanner.hash_module];
        }

        public static SYMBOL add ( string id, uint hash )
        {
            if ( table[hash] == null )
            {
                table[hash] = new SYMBOL(id);
                return table[hash];
            }

            SYMBOL element = table[hash];

            do
            {
                if ( element.identifier == id )
                    return element;

                if ( element.next == null )
                {
                    element.next = new SYMBOL(id);
                    return element.next;
                }
                else
                    element = element.next;
            }
            while(true);
        }

        public static void print ( )
        {
            for(int i=0; i<Scanner.hash_module; i++)
            {
                if ( table[i] == null ) continue;
                SYMBOL element = table[i];

                System.Console.Write("Hash = {0}: ",i);
                while ( element != null )
                {
                    System.Console.Write(" {0}",element.identifier);
                    element = element.next;
                }
                System.Console.WriteLine(" ");
            }
        }
	}

    public class SYMBOL
    {
        public string identifier;
        public SYMBOL next;

        public SYMBOL ( string id )
        {
            identifier = id;
        }
    }

}  // namespace ETH.Zonnon.Compiler
