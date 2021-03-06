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
using System.CodeDom.Compiler;
using System.Compiler;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Zonnon.RTL;
using ETH.Zonnon;

public class CommandLineCompiler
{
    [STAThread]
    public static int Main ( string[] args )
    {
        // Process compiler options
        ZonnonCompilerParameters options = parseCommandLineParameters(args);
		
		// Display help and do nothing 
		if(options.DisplayHelp)
		{
			#region Help
			System.Console.WriteLine("zc.exe [parameters...] source file [source files ...]");
			System.Console.WriteLine("Example: zc.exe /entry:run test.znn");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /file:source-file-name   ");
			System.Console.WriteLine("                  alternative way to pass source-file names under Linux");
			System.Console.WriteLine("                  when it starts with /. E.g. for /root/q.znn use");
			System.Console.WriteLine("                  /file:/root/q.znn");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /out:assembly-name-without-extension");
			System.Console.WriteLine("                  the name of output assembly.");
			System.Console.WriteLine("                  If parameter is not specified ");
			System.Console.WriteLine("                  then the name of the (first) source file is ");
			System.Console.WriteLine("                  taken (without extension).");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /entry:startup-module-name ");
			System.Console.WriteLine("                  program module (in Zonnon terminology) the ");
			System.Console.WriteLine("                  execution should start from.");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /ref:assembly-file  ");
			System.Console.WriteLine("                  the assembly which is used in the program ");
			System.Console.WriteLine("                  (via import declarations). ");
            System.Console.WriteLine("");
            System.Console.WriteLine("   /version:0.0.0.0  ");
            System.Console.WriteLine("                  sets versions of the assembly and the file ");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /safe          affects the way of handling exceptions in the ");
			System.Console.WriteLine("                  executable programs generated by the compiler.");
			System.Console.WriteLine("                  If /safe parameter is specified then no stack ");
			System.Console.WriteLine("                  is printed out but just the message about the ");
			System.Console.WriteLine("                  exception.");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /xml           compiler generates xml file with semantic ");
			System.Console.WriteLine("                  infromation of the entire compilation.");
			System.Console.WriteLine("");
			System.Console.WriteLine("   /quiet         compiler doesn't output its title");
            System.Console.WriteLine("");
            System.Console.WriteLine("   /mathopt       compilation with mathematical optimizations");
            System.Console.WriteLine("");
            System.Console.WriteLine("   /compute       use OpenCL");
            #endregion
			return 0;
		}
        
		// If it is not suppressed by the 'quiet' option, print the compiler title and info.
        if ( !options.Quiet )
        {
            System.Reflection.Assembly assembly = typeof(ETH.Zonnon.ZonnonCompiler).Assembly;

            object[] attributes = assembly.GetCustomAttributes(false);

            string version   = assembly.GetName(false).Version.ToString();
            string title     = null;
            string copyright = null;

            DateTime dt = File.GetLastWriteTime(assembly.GetModules()[0].FullyQualifiedName);
            string date = dt.ToLongDateString();
            string time = dt.ToLongTimeString();

            // We take compiler title & copyright from the corresponding
            // assembly attributes (see Compiler's AssemblyInfo.cs file).
            for ( int i=0, n=attributes.Length; i<n; i++ )
            {
                AssemblyTitleAttribute titleAttr = attributes[i] as AssemblyTitleAttribute;
                if ( titleAttr != null ) { title = titleAttr.Title; continue; }

                AssemblyCopyrightAttribute copyrightAttr = attributes[i] as AssemblyCopyrightAttribute;
                if ( copyrightAttr != null ) copyright = copyrightAttr.Copyright;
            }
			
			if(copyright == null) copyright = "(c) ETH Zurich";
			if(title == null) {title = "ETH Zonnon compiler (rotor/mono edition)";
				System.Console.WriteLine("{0},\n\rVersion {1} of {2}, {3}",title,version,date,time);
			} else
            System.Console.WriteLine("{0}, Version {1} of {2}, {3}",title,version,date,time);

         // System.Reflection.Assembly rtl = typeof(Zonnon.RTL.Input).Assembly;
         // string rtl_version = rtl.GetName(false).Version.ToString();
         // System.Console.WriteLine("Zonnon RTL, Version {0}",rtl_version);

            System.Console.WriteLine("{0}",copyright);
        }

        // Print messages if there were errors in compiler options,
        // and complete the execution.
        if ( options.Messages.Count > 0 )
        {
            for ( int i=0, n=options.Messages.Count; i<n; i++ )
                System.Console.WriteLine(options.Messages[i]);
            return 1;
        }

		// Create the Zonnon compiler
        ETH.Zonnon.ZonnonCompiler compiler = new ETH.Zonnon.ZonnonCompiler();
///     compiler.options = options;
        //System.Diagnostics.Debugger.Launch();
        // Launch the compilation process
        CompilerResults results;

#if TRACE	  
		System.Console.WriteLine("START compilation");
#endif        
        switch (options.SourceFiles.Count)
        {
            case 0 :  // NO sources; message has been issued before
                return 1;

            case 1 :  // only ONE source file                
                results = compiler.CompileAssemblyFromFile(options,options.SourceFiles[0]);
                break;

            default : // SEVERAL source files
            {
                string[] sources = new string[options.SourceFiles.Count];
                options.SourceFiles.CopyTo(sources,0);
                results = compiler.CompileAssemblyFromFileBatch(options,sources);
                break;
            }
        }
#if TRACE	  
		System.Console.WriteLine("END compilation");
#endif        

        // Print compiler messages
//		XmlDocument doc = null;
//		XmlNode report = null;
//		XmlNode result = null;
//		if(options.ExportReport != null)
//		{
//			doc = new XmlDocument();
//			report = doc.CreateNode(XmlNodeType.Element, "report", "");
//			result = doc.CreateNode(XmlNodeType.Element, "result", "");
//			report.AppendChild(result);
//		}
#if TRACE
        System.Console.WriteLine("PASS Creating TextWriter");
#endif        
        TextWriter doc = null;
        if (options.ExportReport != null)
        {
            doc = new StreamWriter(options.ExportReport);
#if TRACE
            System.Console.WriteLine("PASS TextWriter created");
#endif        

        }

        int errCount = results.Errors.Count;		
        int trueErrCount = results.Errors.Count;
        for (int i = 0, n = results.Errors.Count; i < n; i++)
            if (int.Parse(results.Errors[i].ErrorNumber) == 0) trueErrCount--;
        

        if ( trueErrCount == 0 )  // Only "end of messages" message
        {
#if TRACE
            System.Console.WriteLine("PASS report success");
#endif        
            // ERROR.Success();
            Console.WriteLine("Compilation completed successfully");
			if(doc != null) doc.WriteLine("success");
        }
        else
        {
#if TRACE
            System.Console.WriteLine("PASS report failures");
#endif        
          // foreach ( CompilerError e in results.Errors )
            int j = 0;

			if(doc != null) doc.WriteLine("failure");
//			XmlNode messages = null;
//			if(report != null) 
//			{
//				result.InnerText = "failure";
//				messages = doc.CreateNode(XmlNodeType.Element, "messages", "");
//				report.AppendChild(messages);
//			}
			
            for ( int i=0, n=errCount; i<n; i++ )
            {
#if TRACE
                System.Console.WriteLine("PASS report one problem");
#endif        

                CompilerError e = results.Errors[i];

                // The idea of the code below is to prevent the output of CCI messages 
                // in case there are some compiler messages. (This means the compiler 
                // itself has already issued messages, and there is no need to issue 
                // cryptic CCI messages which typically seem to be unrelated to the source 
                // (from the user's point of view).

                int x = int.Parse(e.ErrorNumber);
                if (x == 0) continue;
#if TRACE
                System.Console.WriteLine("PASS report with code {0}", x);
#endif        

                if ( x >= 2000 ) j++;  // count only non-warning messages

                if ( x == (int)ErrorKind.EndOfMessages )  // special final compiler message
                {
                    if ( j==0 ) continue; // No Zonnon messages BUT some CCI messages; continue
                    break;                // End of Zonnon error messages; don't issue CCI messages
                }
                Console.Write(e.ErrorNumber);
                Console.Write(": ");
                Console.Write(e.FileName);
                Console.Write('(');
                Console.Write(e.Line);
                Console.Write(',');
                Console.Write(e.Column);
                Console.Write("): ");
                Console.WriteLine(e.ErrorText);

#if TRACE
                System.Console.WriteLine("PASS write problem to XML");
#endif        

                // Prepare an XML version of the error report if needed
				if(doc != null)
				{
					doc.Write(e.ErrorNumber);
					doc.Write(", ");
					doc.Write(e.FileName);
					doc.Write(", ");
					doc.Write(e.Line);
					doc.Write(", ");
					doc.WriteLine(e.ErrorText);
				}
//				if(messages != null) 
//				{
//					XmlNode message = doc.CreateNode(XmlNodeType.Element, "message", "");
//					message.Attributes.Append(doc.CreateAttribute("ErrorNumber"));
//					message.Attributes["ErrorNumber"].Value = e.ErrorNumber;
//					message.Attributes.Append(doc.CreateAttribute("FileName"));
//					message.Attributes["FileName"].Value = e.FileName;
//					message.Attributes.Append(doc.CreateAttribute("Line"));
//					message.Attributes["Line"].Value = e.Line.ToString();
//					message.Attributes.Append(doc.CreateAttribute("Column"));
//					message.Attributes["Column"].Value = e.Column.ToString();
//					message.Attributes.Append(doc.CreateAttribute("ErrorText"));
//					message.Attributes["ErrorText"].Value = e.ErrorText;
//					messages.AppendChild(message);
//				}
#if TRACE
                System.Console.WriteLine("PASS end of problem");
#endif        
            }
#if TRACE
            System.Console.WriteLine("PASS end of all problems");
#endif        

        }
		if(doc != null) doc.Close();
//		if(doc != null)
//		{ //Save report
//			System.Xml.XmlTextWriter writer = new System.Xml.XmlTextWriter(options.ExportReport, System.Text.Encoding.UTF8);
//			writer.Formatting = System.Xml.Formatting.Indented;
//			writer.WriteStartDocument(true);
//			report.WriteTo(writer);
//			writer.Flush();
//			writer.Close();
//		}
        // Return the result of the compiler's invokation
#if TRACE
        System.Console.WriteLine("EXIT compiler");
#endif        
        return results.NativeCompilerReturnValue;
	}

	private static void PreprocessArgs(ArrayList args, ZonnonCompilerParameters options) 
    {
		ListDictionary optionsMap = new ListDictionary();
		
		// Collect compiler options from text files.
		for (int i = 0, n = args.Count; i < n; i++) 
        {
			string arg = args[i] as string;
			if ( arg[0] != '@' ) continue;
			
            string fileName = arg.Substring(1);
            if ( !File.Exists(fileName) ) 
            {
                options.Messages.Add("Could not find file '" + fileName + "'");
                continue;
            }

			StreamReader sr = new StreamReader(fileName);
            string content = sr.ReadToEnd();
            content = content.Replace("\n", " ");
            content = content.Replace("\r", " ");
            sr.Close();			
			
			char[] ch = {' '};
			string[] ops = content.Split(ch);
			optionsMap[i] = ops;
		}
		
		// Insert collected options into command line compiler string.
		for (int k = args.Count-1; k >= 0; k--) 
        {
			string[] ops = (string[]) optionsMap[k];
			if (ops == null) continue;
			
			// Remove file name.
			args.RemoveAt(k);
			
			int j = k;
			// Insert the file content instead.
			foreach ( string op in ops )
            {
				if ( op.Length != 0 ) args.Insert(j++, op);		
			}				
		}
	}
	
    private static ZonnonCompilerParameters parseCommandLineParameters(string[] args)
    {
        ZonnonCompilerParameters options = new ZonnonCompilerParameters();
		
		ArrayList a = new ArrayList();
		
		foreach ( string s in args ) if(s.Length > 1) a.Add(s);
		PreprocessArgs(a,options);
		
		for ( int i=0, n=a.Count; i<n; i++ ) 
        {
            string arg = a[i] as string;
            int    colon;
            string key;

            // If no leading '/' then this should be a source file name
            if ( arg[0] != '/' )
            {
                if ( !File.Exists(arg) )
                {
                    string noFile = "Could not find file '" + arg + "'";
                    options.Messages.Add(noFile);
                }
                else
                    options.SourceFiles.Add(arg);
                continue;
            }

            // Taking parameter kind and parameter value(s)
            colon = arg.IndexOf(":",0,arg.Length);
			bool badArg = false;
            if ( colon == -1 )
            {
                key = arg.Substring(1);
                arg = "";
            }
            else
            {
                key = arg.Substring(1,colon-1);
                if(arg.Length>colon+1) arg = arg.Substring(colon+1);
				else badArg = true;
            }

            // Processing compiler parameters which are of the following form:
            //
            //     /key:arg(s)
            //
            // where 'key' is the parameter kind (out, xml, main, ref)
            // and 'arg(s)' is its value(s). Values depend on the parameter kind;
            // if there are several values they should be separated from each
            // other by comma (,) or semicolon (;).
			if(badArg){options.Messages.Add("No argument for option '"+key+"'");}else
            switch ( key.ToLower() )
            {
                case "debug" :   options.Debug = true;
                                 break;

                case "debugt":   options.DebugT = true;
                                 break;

                case "out" :     if ( options.Output != null )
                                     options.Messages.Add("Duplicated 'out' option");
                                 else
                                     options.Output = arg;
                                 break;

                case "xml" :     if ( options.GenerateXML )
                                     options.Messages.Add("Duplicated 'xml' option");
                                 else
                                     options.GenerateXML = true;
                                 break;

				case "file":	// For source files starting with / on Linux								
								if ( !File.Exists(arg) )
								{
									string noFile = "Could not find file '" + arg + "'";
									options.Messages.Add(noFile);
								}
								else
									options.SourceFiles.Add(arg);
								break;
								
                case "entry" :   if ( options.EmbeddedDialogue )
                                     options.Messages.Add("Options 'entry' and 'exe' cannot be specified together");
                                 else if ( options.MainModule != null )
                                     options.Messages.Add("Duplicated 'entry' option");
                                 else
                                     options.MainModule = arg;
                                 break;

                case "version":  Version version;
                                 try{
                                    version = new Version(arg);
                                 }catch(Exception){
                                     version = new Version();
                                     options.Messages.Add("Version is ill-formatted.");
                                 }
                                 options.TargetInformation.Version = version.ToString();
                                 options.TargetInformation.ProductVersion = version.ToString();
                                 break;
                case "mathopt":
                                 options.MathOpt = true;
                                 break;

                case "compute":
                                 options.UseComputeMath = true;
                                 break;
#if !ROTOR
                //case "exe" :     if ( options.EmbeddedDialogue )
                //                     options.Messages.Add("Duplicated 'exe' option");
                //                 else if ( options.MainModule != null )
                //                     options.Messages.Add("Options 'entry' and 'exe' cannot be specified together");
                //                 else
                //                 {
                //                     options.EmbeddedDialogue = true;
                //                     options.MainModule = "_start";
                //                 }
                //                 break;
#endif

                case "ref" :
                                if (!File.Exists(arg))
                                { // Try to reslove standard files
                                    if (arg.Contains("\\") || arg.Contains("/"))
                                    {
                                        //it means that full path was specified. Do not try to resolve
                                    }
                                    else
                                    {
                                        // Try to reslove
                                        try
                                        {
                                            string path = System.Environment.SystemDirectory + @"\..\assembly\GAC_MSIL\" + arg.Substring(0, arg.Length - 4);
                                            string[] op = Directory.GetDirectories(path, "2.0*");
                                            
                                            if (op.Length > 0)
                                            {
                                                string tryName = op[0] + Path.DirectorySeparatorChar + arg;
                                                string oldpath = arg;                                                
                                                if (File.Exists(tryName)) arg = tryName;
                                                Console.WriteLine("Arguments parser note: " + oldpath + " was extended to " + tryName);
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            // Ok... we've at least tried.. don't care.
                                        }
                                    }
                                }
                                 options.ReferencedAssemblies.Add(arg);
                                 break;

                case "safe" :    options.SafeMode = true;
                                 break;

                case "quiet" :   options.Quiet = true;
                                 break;

				case "report" :  options.ExportReport = arg;
								 break;

				case "help" :    options.DisplayHelp = true;
								 break;

                default :        options.Messages.Add("Illegal option '"+key+"'");
                                 break;
            }
        }

        // Postprocessing options...

        if ( options.SourceFiles.Count == 0 )
        {
            options.Messages.Add("No source file is given (/help - to display help)");
            options.Success = false;
        }
        if ( options.Messages.Count > 0 ) goto Finish;

        if ( options.Output == null )
        {
            options.Output = options.SourceFiles[0];
        }

        string sourceDir = Path.GetDirectoryName(options.SourceFiles[0]);
        string outputDir = Path.GetDirectoryName(options.Output);

        if ( sourceDir != "" )
            sourceDir += Path.DirectorySeparatorChar;
        // Otherwise, sourceDir is just empty

        if ( outputDir == "" )
        {
            outputDir = sourceDir;
            options.Output = outputDir + options.Output;
        }
        else
            outputDir += Path.DirectorySeparatorChar;

        if ( options.MainModule == null && !options.EmbeddedDialogue )
        {
            options.GenerateExecutable = false;
            options.Output = outputDir + Path.GetFileNameWithoutExtension(options.Output) + ".dll";
        }
        else
        {
            options.GenerateExecutable = true;
            options.Output = outputDir + Path.GetFileNameWithoutExtension(options.Output) + ".exe";
        }

        if ( options.GenerateXML )
        {
            string xmlDir = options.XMLDocument==null ? "" : Path.GetDirectoryName(options.XMLDocument);

            if ( xmlDir == "" )
                xmlDir = sourceDir;
            else
                xmlDir += Path.DirectorySeparatorChar;

            if ( options.XMLDocument == null )
                options.XMLDocument = xmlDir + Path.GetFileNameWithoutExtension(options.SourceFiles[0]) + ".xml";
            else
                options.XMLDocument = xmlDir + Path.GetFileNameWithoutExtension(options.XMLDocument) + ".xml";
        }

     // string dotNETDirectory = System.IO.Path.GetDirectoryName(System.Environment.SystemDirectory)
     //                                                             + System.IO.Path.DirectorySeparatorChar +
     //                          "Microsoft.NET"                    + System.IO.Path.DirectorySeparatorChar +
     //                          "Framework"                        + System.IO.Path.DirectorySeparatorChar +
     //                          "v"+System.Environment.Version.Major.ToString()+"."+
     //                              System.Environment.Version.Minor.ToString()+"."+
     //                              System.Environment.Version.Build.ToString()
     //                                                             + System.IO.Path.DirectorySeparatorChar;
     // options.ReferencedAssemblies.Add(dotNETDirectory+"mscorlib.dll");
     // options.ReferencedAssemblies.Add(dotNETDirectory+"System.dll");

     // string systemDirectory   = AssemblyNode.GetAssembly(typeof(System.CodeDom.Compiler.CodeCompiler).Assembly).Directory;
     // string mscorlibDirectory = AssemblyNode.GetAssembly(typeof(System.Console).Assembly).Directory;

		string mscorlibLocation = typeof(System.Console).Assembly.Location;
		string systemLocation   = typeof(System.CodeDom.Compiler.CodeCompiler).Assembly.Location;
		string rtlLocation      = typeof(Zonnon.RTL.CommonException).Assembly.Location;
#if TRACE	  
		System.Console.WriteLine("PASS mscorlibLocation = {0}", mscorlibLocation);
		System.Console.WriteLine("PASS systemLocation = {0}", systemLocation);
		System.Console.WriteLine("PASS rtlLocation = {0}", rtlLocation);
#endif
		options.ReferencedAssemblies.Add(mscorlibLocation);
		options.ReferencedAssemblies.Add(systemLocation);
		options.ReferencedAssemblies.Add(rtlLocation); // "Zonnon.RTL.dll");

        options.OutputAssembly = options.Output;
        options.IncludeDebugInformation = true; //false;

    Finish:
        return options;
    }
}
