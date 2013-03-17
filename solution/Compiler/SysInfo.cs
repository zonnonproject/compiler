using System;
using System.Windows.Forms;

namespace ETH.Zonnon.Compiler
{
	public class SysInfo
	{
		public SysInfo()
		{
		}

        public static void Info ( )
        {
         // SystemInformation.
            System.Console.WriteLine("System directory: {0}",System.Environment.SystemDirectory);
            System.Console.WriteLine("Version: {0}",System.Environment.Version.ToString());
            System.Console.WriteLine("Version: Major={0}, Minor={1}, Build={2}, Revision={3}",
                                     System.Environment.Version.Major.ToString(),
                                     System.Environment.Version.Minor.ToString(),
                                     System.Environment.Version.Build.ToString(),
                                     System.Environment.Version.Revision.ToString());
            System.Console.WriteLine("{0}","v"+System.Environment.Version.Major.ToString()+"."+
                                               System.Environment.Version.Minor.ToString()+"."+
                                               System.Environment.Version.Build.ToString());
            System.Console.WriteLine("{0}",System.Environment.SystemDirectory + System.IO.Path.DirectorySeparatorChar +
                                           "Microsoft.NET"                    + System.IO.Path.DirectorySeparatorChar +
                                           "Framework"                        + System.IO.Path.DirectorySeparatorChar +
                                           "v"+System.Environment.Version.Major.ToString()+"."+
                                               System.Environment.Version.Minor.ToString()+"."+
                                               System.Environment.Version.Build.ToString());
         // System.Console.WriteLine("{0}",Environment.GetFolderPath(System.Environment.SpecialFolder.System));
            System.Console.WriteLine("OS Version: {0}",System.Environment.OSVersion.ToString());
         // System.Console.WriteLine("{0}",AssemblyRef.System);
        }
	}
}
