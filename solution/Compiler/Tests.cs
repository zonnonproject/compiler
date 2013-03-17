using System;

namespace Compiler
{
	public class Tests
	{
        void xxx()
        {
            char ch;
            byte b = 5;
            ch = (char)b;
        }
        void yyy()
        {
            byte b = 3;
            short s;
            s = b;
        }
        void zzz(int c)
        {
            float[,] f;
            f = new float[10,10];
        }

        public delegate void pf ( int x );

        public void z1 ()
        {
            pf p = null;
            p = new pf(zzz);
        }

        public static void aaa()
        {
            Console.WriteLine("GetFolderPath: {0}", 
                              Environment.GetFolderPath(Environment.SpecialFolder.System));
             // This example produces the following results:
             // GetFolderPath: C:\WINNT\System32
        }
	}
}
