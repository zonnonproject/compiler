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
using System.Collections.Specialized;
using System.Xml;

using ETH.Zonnon;
using Zonnon.RTL;

public class Statistics
{
    public static int compilationSucceed = 0;
    public static int compilationFailed  = 0;
    public static int compilationAbort   = 0;
    public static int compilationNotPlanned = 0;

    public static int executionSucceed = 0;
    public static int executionFailed  = 0;
    public static int executionAbort   = 0;
    public static int executionNotPlanned = 0;

    public static int overallTests = 0;
    public static int successfulTests = 0;
    public static int failedTests = 0;

    public static void output ( )
    {        
        System.Console.WriteLine("STATISTICS");
        System.Console.WriteLine("Number of tests: {0}",overallTests);
        System.Console.WriteLine("        Success: {0} = {1}%",successfulTests,((float)successfulTests/(float)overallTests)*100);
    }

    public static void write ( XmlTextWriter writer )
    {
        writer.WriteElementString("amount_of_tests",overallTests.ToString());
        writer.WriteElementString("success",successfulTests.ToString());
        writer.WriteElementString("percentage",(((float)successfulTests/(float)overallTests)*100).ToString());
    }
}

public class Tester
{
    private static string formatTime ( DateTime dt, bool forFileName )
    {
        if ( forFileName )
            return "y" + dt.Year + "m" + dt.Month.ToString("00") + "d" + dt.Day.ToString("00") +
                   "h" + dt.Hour.ToString("00") + "m" + dt.Minute.ToString("00");
        else
            return dt.Year + "." + dt.Month.ToString("00") + "." + dt.Day.ToString("00") +
                   ", " + dt.Hour.ToString("00") + ":" + dt.Minute.ToString("00");
    }

    // Global info for creating test log-file

    static ZonnonCompilerParameters options = null;

    public static string testDirectory = "";
	public static string binaryDirectory = "";
    public static string outputDirectory = "";

    public static string compilerVersion = null;
    public static string compilerDateTime = null;
    public static string runDateTime = null;
    public static string logFileName = null;

    static ETH.Zonnon.ZonnonCompiler compiler = null;

    [STAThread]
    public static void Main ( string[] args )
    {
        // Process tester options
        options = parseTesterParameters(args, ref testDirectory);
        if ( options == null ) return;  // Errors in options; cannot launch tester

        // Create the name of the output directory
		if (binaryDirectory != "") 
			outputDirectory = binaryDirectory; 
		else
			outputDirectory = testDirectory + Path.DirectorySeparatorChar + "bin";

        // Take the information about compiler date/time and version
        System.Reflection.Assembly assembly = typeof(ETH.Zonnon.ZonnonCompiler).Assembly;
        compilerVersion   = assembly.GetName(false).Version.ToString();

        DateTime dt = File.GetLastWriteTime(assembly.GetModules()[0].FullyQualifiedName);
        compilerDateTime = formatTime(dt,false);

        // Fix the current time; we will treat this time
        // as the launch time for _all_tests_.
        dt = DateTime.Now;
        runDateTime = formatTime(dt,false);
        logFileName = binaryDirectory + Path.DirectorySeparatorChar;
#if ROTOR
 	    logFileName += "Mono Log ";
#else
        logFileName += "Log ";
#endif
        logFileName += formatTime(dt, true) + ".xml";

        // Create the Zonnon compiler
        compiler = new ETH.Zonnon.ZonnonCompiler();
//      compiler.options = options;

        // TESTING LOOP
        ProcessDirectory(new DirectoryInfo(testDirectory));

        TestResult.generateLog(logFileName);
     // TestResult.reportLog(testDirectory);
        Statistics.output();
	}

    private static void ProcessDirectory ( DirectoryInfo directory )
    {
        // We launch the compiler for every test from the test directory 
        // consequently, and then recursively go into all sub-directories.

        FileInfo[] files = directory.GetFiles("*.znn");
        FileInfo[] cmdfiles = directory.GetFiles("*.ts");
        DirectoryInfo[] dirs = directory.GetDirectories();

        if (cmdfiles.Length > 0)
        {   //Testing a project
            for (int i = 0, n = cmdfiles.Length; i < n; i++)
                ProcessCmdFile(cmdfiles[i]);
        }
        else //Normal set of tests
        {

            for (int i = 0, n = files.Length; i < n; i++)
                CompileAndExecute(files[i]);

            for (int i = 0, n = dirs.Length; i < n; i++)
                ProcessDirectory(dirs[i]);
        }
    }

    private static void ProcessCmdFile(FileInfo file)
    {
        // We launch the compiler for given test, then execute the code,
        // and then process the compilation/execution results,
        // and finally create the TestResult instance.
        options.ReferencedAssemblies.Clear();
        string mscorlibLocation = typeof(System.Console).Assembly.Location;
        string systemLocation = typeof(System.CodeDom.Compiler.CodeCompiler).Assembly.Location;
        string rtlLocation = typeof(Zonnon.RTL.CommonException).Assembly.Location;
        options.ReferencedAssemblies.Add(mscorlibLocation);
        options.ReferencedAssemblies.Add(systemLocation);
        options.ReferencedAssemblies.Add(rtlLocation); // "Zonnon.RTL.dll");


        // Take the test file and its name
        string cmdFileName = file.FullName;
        string directory = file.Directory.FullName;

        string shortFileName = Path.GetFileNameWithoutExtension(cmdFileName);

        string resPassed = "PASSED";
        string resError = "ERROR";
        string resNotRun = "NOT RUN";
        string resNotPassed = "NOT PASSED";
        string resSuccess = "SUCCESS";
        string resAbort = "ABORT";
        string resFail = "FAIL";
        //string resBad = "BAD TEST";
        string resBadCode = "BAD CODE";

        System.Console.WriteLine(">>>>> {0}", shortFileName);
        System.Console.WriteLine("FULL: {0}", cmdFileName);
        System.Console.WriteLine("SCRIPT");

        string compilationResult = resError;
        string executionResult = resNotRun;
        string commonResult = resNotPassed;

        StreamReader re = File.OpenText(cmdFileName);
        string line = null;
        int lineNum = 0;
        while ((line = re.ReadLine()) != null)
        {
            Console.WriteLine("Processing: "+line);
            string [] cmd = line.Split(' ');
            lineNum++;
            if (cmd.Length < 2) continue;
            switch (cmd[0].ToLower())
            {
                case "setref":
                    options.ReferencedAssemblies.Clear();
                    options.ReferencedAssemblies.Add(mscorlibLocation);
                    options.ReferencedAssemblies.Add(systemLocation);
                    options.ReferencedAssemblies.Add(rtlLocation); // "Zonnon.RTL.dll");

                    Console.WriteLine("References cleared");
                    for (int i = 1; i < cmd.Length; i++)
                    {
                        if (cmd[i].Length == 0) continue;
                        string arg = cmd[i];

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
                                        if (File.Exists(tryName))
                                        {                                            
                                            Console.WriteLine("Arguments parser note: " + arg + " was extended to " + tryName);
                                            arg = tryName;
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // Ok... we've at least tried.. don't care.
                                }
                            }
                        }
                        // One more attempt
                        if (!File.Exists(arg))
                        {
                            string  tryName = outputDirectory + Path.DirectorySeparatorChar + arg;
                            if (File.Exists(tryName))
                            {
                                Console.WriteLine("Arguments parser note: " + arg + " was extended to " + tryName);
                                arg = tryName;
                            }

                        }
                        Console.WriteLine("Add reference: "+arg);
                        options.ReferencedAssemblies.Add(arg);
                    }
                    break;
                case "setout":
                    options.OutputAssembly = outputDirectory + Path.DirectorySeparatorChar + cmd[1];
                    Console.WriteLine("Output is set to: " + options.OutputAssembly);
                    break;
                case "compile":
                    options.SourceFiles = new StringCollection();
                    Console.WriteLine("Compiling:");
                    for (int i = 1; i < cmd.Length; i++)
                    {
                        if (cmd[i].Length == 0) continue;
                        Console.WriteLine("\t\t" + directory + Path.DirectorySeparatorChar + cmd[i]);
                        options.SourceFiles.Add(directory + Path.DirectorySeparatorChar + cmd[i]);
                    }

                    CompilerResults results = null;
                    Exception compilationException = null;
                    try
                    {                    
                        string[] sources = new string[options.SourceFiles.Count];
                        options.SourceFiles.CopyTo(sources, 0);
                        
                        results = compiler.CompileAssemblyFromFileBatch(options, sources);

                        if (results.NativeCompilerReturnValue == 0)
                        {
                            Console.WriteLine("compiler returned success");
                            compilationResult = resSuccess;
                            Statistics.compilationSucceed++;
                        }
                        else
                        {
                            Console.WriteLine("compiler returned failure");
                            compilationResult = resError;
                            Statistics.compilationFailed++;
                        }

                        int errCount = results.Errors.Count;
                        if (errCount > 1)
                        {
                            for (int j = 0; j < errCount; j++)
                            {
                                CompilerError e = results.Errors[j];
                                Console.Write(e.ErrorNumber);
                                Console.Write(": ");
                                Console.Write('(');
                                Console.Write(e.Line);
                                Console.Write(',');
                                Console.Write(e.Column);
                                Console.Write("): ");
                                Console.WriteLine(e.ErrorText);
                            }
                        }
                        else
                            Console.WriteLine("no errors");
                    }
                    catch (Exception e)
                    {
                        compilationResult = resAbort;
                        compilationException = e;
                        Statistics.compilationAbort++;
                    }
                    if (compilationResult == resSuccess)
                    {
                        commonResult = resPassed;
                        Statistics.successfulTests++;
                    }
                    else
                    {
                        commonResult = resNotPassed;
                        Statistics.failedTests++;
                    }

                    Statistics.overallTests++;

                    System.Console.WriteLine("      Compilation Result: {0}", compilationResult);
                    TestResult testResultCompile = new TestResult();  // will add the new node to the common list
                    testResultCompile.testName = cmdFileName + " @" + lineNum.ToString()+" : "+line;
                    testResultCompile.compilerDateTime = compilerDateTime;
                    testResultCompile.compilerVersion = compilerVersion;
                    testResultCompile.runDateTime = runDateTime;
                    testResultCompile.compilationResult = compilationResult;
                    testResultCompile.executionResult = resNotRun;
                    testResultCompile.commonResult = commonResult;
                    testResultCompile.compilerResults = results;

                    testResultCompile.compilationException = compilationException;

                    break;

                case "setentry":
                    if (cmd[1].ToLower().CompareTo("no") == 0)
                    {
                        options.MainModule = null;
                        options.GenerateExecutable = false;
                    }
                    else
                    {
                        options.GenerateExecutable = true;
                        options.MainModule = cmd[1];
                    }
                    break;

                case "testrun":
                    // Launch the code
                    string fileToRun = outputDirectory + Path.DirectorySeparatorChar + cmd[1];
                    Exception executionException = null;
                    try
                    {
                        
                        Assembly assemblyToRun = Assembly.LoadFrom(fileToRun);
                        System.Reflection.Module module = assemblyToRun.GetModules(false)[0];
                        System.Type type = module.GetType("Zonnon.Main");

                        if (type == null)
                        {
                            executionResult = resBadCode;
                        }
                        else
                        {
                            MethodInfo mainMethod = type.GetMethod("Main");

                            if (mainMethod == null)
                            {
                                executionResult = resBadCode;
                                Statistics.executionNotPlanned++;
                            }
                            else
                            {
                                object res = mainMethod.Invoke(null, null);
                                if (res is int && (int)res == 1)
                                {
                                    executionResult = resSuccess;
                                    Statistics.executionSucceed++;
                                }
                                else
                                {
                                    executionResult = resFail;
                                    Statistics.executionFailed++;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        executionResult = resAbort;
                        executionException = e;
                        Statistics.executionAbort++;
                    }

                    Statistics.overallTests++;
                    if (executionResult == resSuccess)
                    {
                        commonResult = resPassed;
                        Statistics.successfulTests++;
                    }
                    else
                    {
                        commonResult = resNotPassed;
                        Statistics.failedTests++;
                    }
                    
                    System.Console.WriteLine("      Execution   Result: {0}", executionResult);

                    TestResult testResultRun = new TestResult();  // will add the new node to the common list
                    testResultRun.testName = cmdFileName + " @" + lineNum.ToString()+" : "+line;
                    testResultRun.compilerDateTime = compilerDateTime;
                    testResultRun.compilerVersion = compilerVersion;
                    testResultRun.runDateTime = runDateTime;
                    testResultRun.compilationResult = resSuccess;
                    testResultRun.executionResult = executionResult;
                    testResultRun.commonResult = commonResult;
                    testResultRun.compilerResults = null;

                    testResultRun.executionException = executionException;
                    break;


                default:
                    Console.WriteLine("!!!!!!!!!! ERROR IN SCRIPT !!!!!!!!!!");
                    Console.WriteLine(cmdFileName);
                    Console.WriteLine("Command "+cmd[0]+" is unknown");
                    Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.WriteLine("Press Ctrl-C to stop");
                    Console.ReadLine();
                    break;
            }

        }

        re.Close();
        
    }


    private static void CompileAndExecute ( FileInfo file )
    {
        // We launch the compiler for given test, then execute the code,
        // and then process the compilation/execution results,
        // and finally create the TestResult instance.

        // Take the test file and its name
        string   testFileName = file.FullName;
		string   binaryFileName = testFileName;
		binaryFileName = binaryFileName.Replace(@"/",@"_");
		binaryFileName = binaryFileName.Replace(@"\",@"_");
		binaryFileName = binaryFileName.Replace(@":",@"_");
		binaryFileName = binaryFileName.Replace(@"znn",@"exe");
		string   shortFileName = Path.GetFileNameWithoutExtension(testFileName);
        string   outputFileName = outputDirectory + Path.DirectorySeparatorChar +
									binaryFileName;  
								 // shortFileName + ".exe";

        string   resPassed    = "PASSED";
        string   resError     = "ERROR";
        string   resNotRun    = "NOT RUN";
        string   resNotPassed = "NOT PASSED";
        string   resSuccess   = "SUCCESS";
        string   resAbort     = "ABORT";
        string   resFail      = "FAIL";
        string   resBad       = "BAD TEST";
        string   resBadCode   = "BAD CODE";

        // Every test file name should be built by the following template:
        //
        //    T-CC-SS-PP-XX-NN-k.znn
        //    CCSSPP-kNNN.znn
        //
        // where
        //       CC - Chapter number from the Zonnon Language Report;
        //       SS - Section number;
        //       PP - Subsection number;
        //       XX - Additional number;
        //       k  - test kind: 't' for "normal" tests,
        //                       'x' for error tests,
        //                       'c' for compilation only tests;
        //       .znn - file extension

     // if ( Path.GetExtension(testFileName) != ".znn" ) return; -- we have already selected znn-files
     // if ( shortFileName.Length < 18 ) continue;

        Statistics.overallTests++;
        System.Console.WriteLine(">>>>> {0}", shortFileName);
        System.Console.WriteLine("FULL: {0}", testFileName);

        // char testKind = shortFileName[17];
        char testKind = shortFileName[shortFileName.Length-1];

        string compilationResult = resError;
        string executionResult   = resNotRun;
        string commonResult      = resNotPassed;

        // Prepare the "variable" part of the compiler options
        options.OutputAssembly = outputFileName;
        options.SourceFiles = new StringCollection();
        options.SourceFiles.Add(testFileName);

        // Launch the compilation process
        CompilerResults results = null;
        Exception compilationException = null;
        Exception executionException = null;

        try
        {
            results = compiler.CompileAssemblyFromFile(options,testFileName);

            if ( results.NativeCompilerReturnValue == 0 )
            {
                compilationResult = resSuccess;
                Statistics.compilationSucceed++;
            }
            else
            {
                compilationResult = resError;
                Statistics.compilationFailed++;
            }

            // Print compiler messages
            // (if necessary we can redirect stdout to a disk file to see diagnostics)

            // foreach ( CompilerError e in results.Errors )
            // {
            //     Console.Write(e.ErrorNumber);
            //     Console.Write(": ");
            //  // Console.Write(e.FileName);
            //     Console.Write('(');
            //     Console.Write(e.Line);
            //     Console.Write(',');
            //     Console.Write(e.Column);
            //     Console.Write("): ");
            //     Console.WriteLine(e.ErrorText);
            // }

            int errCount = results.Errors.Count;
            if ( errCount > 1 )
            {
                for ( int j=0; j<errCount; j++ )
                {
                    CompilerError e = results.Errors[j];
                    if ( int.Parse(e.ErrorNumber) == (int)ErrorKind.EndOfMessages )
                    {
                        if ( j==0 ) continue; // No Zonnon messages BUT some CCI messages; continue
                        break;                // End of Zonnon error messages; don't issue CCI messages
                    }

                    Console.Write(e.ErrorNumber);
                    Console.Write(": ");
                 // Console.Write(e.FileName);
                    Console.Write('(');
                    Console.Write(e.Line);
                    Console.Write(',');
                    Console.Write(e.Column);
                    Console.Write("): ");
                    Console.WriteLine(e.ErrorText);
                }
            }
        }
        catch ( Exception e )
        {
            compilationResult = resAbort;
            compilationException = e;
            Statistics.compilationAbort++;
        }

        if (testKind == 'c' || testKind == 'C')
        {
            Statistics.compilationNotPlanned++;
        }
        if (compilationResult == resSuccess && (testKind == 't' || testKind == 'T'))
        {
            // Launch the compiled code
            try
            {
                Assembly                 assemblyToRun = Assembly.LoadFrom(outputFileName);
                System.Reflection.Module module        = assemblyToRun.GetModules(false)[0];
                System.Type              type          = module.GetType("Zonnon.Main");

                if ( type == null )
                {
                    executionResult = resBadCode;
                }
                else
                {
                    MethodInfo mainMethod = type.GetMethod("Main");

                    if ( mainMethod == null )
                    {
                        executionResult = resBadCode;
                        Statistics.executionNotPlanned++;
                    }
                    else
                    {
                        object res = mainMethod.Invoke(null,null);
                        if ( res is int && (int)res == 1 )
                        {
                            executionResult = resSuccess;
                            Statistics.executionSucceed++;
                        }
                        else
                        {
                            executionResult = resFail;
                            Statistics.executionFailed++;
                        }
                    }
                }
            }
            catch ( Exception e )
            {
                // System.Console.WriteLine(e.StackTrace);
                // System.Console.WriteLine("================================");
                // System.Console.WriteLine(e.Source);
                // System.Console.WriteLine("================================");
                // System.Console.WriteLine(e.InnerException.StackTrace);

                System.Console.WriteLine(e.Message);

                executionResult = resAbort;
                executionException = e;
                Statistics.executionAbort++;
            }
        }

        switch ( testKind )
        {
            case 't' :
            case 'T':

                if ( compilationResult == resSuccess && executionResult == resSuccess )
                {
                    commonResult = resPassed;
                    Statistics.successfulTests++;
                }
                else
                {
                    commonResult = resNotPassed;
                    Statistics.failedTests++;
                }
                break;

            case 'x' :
            case 'X':

                if ( compilationResult != resSuccess && compilationResult != resAbort )
                {
                    commonResult = resPassed;
                    Statistics.successfulTests++;
                }
                else
                {
                    commonResult = resNotPassed;
                    Statistics.failedTests++;
                }
                break;

            case 'c' :
            case 'C':

                if ( compilationResult == resSuccess )
                {
                    commonResult = resPassed;
                    Statistics.successfulTests++;
                }
                else
                {
                    commonResult = resNotPassed;
                    Statistics.failedTests++;
                }
                break;

            default :

                commonResult = resBad;
                break;
        }

        // System.Console.WriteLine(">>>>> {0} FINISHED",shortFileName);
        System.Console.WriteLine("      Compilation Result: {0}",compilationResult);
        System.Console.WriteLine("      Execution   Result: {0}",executionResult);
        System.Console.WriteLine("      Common      Result: {0}",commonResult);

        // Create the report for the passed test
        TestResult testResult = new TestResult();  // will add the new node to the common list

        testResult.testName = Path.GetDirectoryName(testFileName) + Path.DirectorySeparatorChar +
                              Path.GetFileNameWithoutExtension(testFileName);
        testResult.compilerDateTime = compilerDateTime;
        testResult.compilerVersion = compilerVersion;
        testResult.runDateTime = runDateTime;
        testResult.compilationResult = compilationResult;
        testResult.executionResult = executionResult;
        testResult.commonResult = commonResult;

        testResult.compilerResults = results;

        testResult.compilationException = compilationException;
        testResult.executionException = executionException;
    }

    private static ZonnonCompilerParameters parseTesterParameters ( string[] args,
                                                                    ref string testDirectory )
    {
        ZonnonCompilerParameters options = new ZonnonCompilerParameters();

        for ( int i=0, n=args.Length; i<n; i++ )
        {
            string arg = args[i];
            int    colon;
            string key;

            // Taking parameter kind and parameter value(s)
            colon = arg.IndexOf(":",0,arg.Length);
            if ( colon == -1 )
            {
                key = arg.Substring(1);
                arg = "";
            }
            else
            {
                key = arg.Substring(1,colon-1);
                arg = arg.Substring(colon+1);
            }

            // Processing tester parameters.
            // Now the following possible tester parameters are:
            //
            //     /testdir:directoryname
            //
            // where directoryname is the name of the directory with the tests.
            // Notice that directoryname should not contain the trailing '\'.

            switch ( key.ToLower() )
            {
                case "testdir" :
                    testDirectory = arg;
                    break;
				case "binarydir" :
					binaryDirectory = arg;
					break;
				case "default":
					testDirectory = "T:\\";
					binaryDirectory = Directory.GetCurrentDirectory()+"\\";
					break;
                default :
                    options.Messages.Add("Illegal option '" + key + "'");
                    break;
            }
        }

        // Create the names for test & output directories

        if ( testDirectory == "" )
        {
            options.Messages.Add("No test directory is given");
			options.Messages.Add("Usage: tester.exe /testdir: /binarydir:");
			options.Messages.Add("or tester.exe /default");

            for ( int i=0, n=options.Messages.Count; i<n; i++ )
                System.Console.WriteLine(options.Messages[i]);

            return null;
        }

        // Set the "permanent" part of the compiler options
        options.GenerateExecutable = true;
        options.XMLDocument = null;
        options.GenerateXML = false;
        options.IncludeDebugInformation = true; //false;
        options.MainModule = "Main";
        options.EmbeddedDialogue = false;

        string mscorlibLocation = typeof(System.Console).Assembly.Location;
        string systemLocation   = typeof(System.CodeDom.Compiler.CodeCompiler).Assembly.Location;
        string rtlLocation = typeof(Zonnon.RTL.CommonException).Assembly.Location;
        options.ReferencedAssemblies.Add(mscorlibLocation);
        options.ReferencedAssemblies.Add(systemLocation);
        options.ReferencedAssemblies.Add(rtlLocation); //"Zonnon.RTL.dll");

     // options.Debug = true;

        return options;
    }
}

public class TestResult
{
    public static string     logFileName = null;

    public static TestResult header = null;
    public static TestResult current = null;
    public        TestResult next;

    public TestResult ( )
    {
        if ( header == null ) header = this;
        else                  current.next = this;
        current = this;

        testName = null;
        compilerDateTime = null;
        compilerVersion = null;
        runDateTime = null;
        compilationResult = null;
        executionResult = null;
        commonResult = null;

        compilationException = null;
        executionException = null;
    }

    public string testName;
    public string compilerDateTime;
    public string compilerVersion;
    public string testAnnotation;
    public string runDateTime;
    public string compilationResult; // SUCCESS, ERROR, ABORT
    public string executionResult;   // NOT RUN, SUCCESS, FAIL, ABORT
    public string commonResult;      // PASSED, NOT PASSED

    public CompilerResults compilerResults;
    public Exception compilationException, executionException;

    private static string formatResult ( string result )
    {
        while ( result.Length < 10 )
            result += " ";
        return result;
    }

    // generateLog
    // -----------
    //
    // <test name="..." date="..."     time="..."     version="..."
    //                  run_data="..." run_time="..." compile_result="..."
    //                  run_result="..."              commom_result="..."  >
    //     <comment>
    //        ...
    //     </comment>
    //     <diag line="..." column="...">
    //        ...
    //     </diag>
    // </test>
    //
    public static void generateLog ( string log )
    {
        XmlTextWriter writer = new XmlTextWriter(log,System.Text.Encoding.UTF8);

        writer.Formatting = Formatting.Indented;
        writer.WriteStartDocument(false);
     // writer.WriteComment("This is a comment");

        writer.WriteStartElement("test_results",null);

        writer.WriteElementString("log_file_name",Path.GetFileNameWithoutExtension(Tester.logFileName));
        writer.WriteElementString("test_directory",Tester.testDirectory);
        writer.WriteElementString("compiler_version",Tester.compilerVersion);
        writer.WriteElementString("execution_time",Tester.runDateTime);
        Statistics.write(writer);

        TestResult rrr = header;

        while ( rrr != null )
        {
            // Generate XML node for rrr

            writer.WriteStartElement("test",null);
            writer.WriteAttributeString("name",rrr.testName);
            writer.WriteAttributeString("compilation_result",rrr.compilationResult);
            writer.WriteAttributeString("execution_result",rrr.executionResult);
            writer.WriteAttributeString("common_result",rrr.commonResult);
        //  writer.WriteAttributeString("compiler_date_time",rrr.compilerDateTime);
            writer.WriteAttributeString("compiler_version",rrr.compilerVersion);
            writer.WriteAttributeString("execution_date_time",rrr.runDateTime);

            writer.WriteElementString("comment","-");

            if ( rrr.compilerResults != null )
            {
             // foreach ( CompilerError e in rrr.compilerResults.Errors )
                for ( int i=0; i<rrr.compilerResults.Errors.Count; i++ )
                {
                    CompilerError e = rrr.compilerResults.Errors[i];
                    if ( int.Parse(e.ErrorNumber) == (int)ErrorKind.EndOfMessages )
                    {
                        if ( i==0 ) continue; // No Zonnon messages BUT some CCI messages; continue
                        break;                // End of Zonnon error messages; don't issue CCI messages
                    }

                    writer.WriteStartElement("diag", null);
                    writer.WriteAttributeString("errNumber", "" + e.ErrorNumber);
                    writer.WriteAttributeString("line", "" + e.Line);
                    writer.WriteAttributeString("column", "" + e.Column);

                    writer.WriteElementString("message", e.ErrorText);

                    writer.WriteEndElement();
                } // foreach
            }

            XMLWriteException(writer, "CompilationException", rrr.compilationException);
            XMLWriteException(writer, "ExecutionException", rrr.executionException);

            writer.WriteEndElement();            
            writer.Flush();

            // Take the next test record
            rrr = rrr.next;
        }

        writer.WriteEndElement();
        writer.Close();
    }

    public static void XMLWriteException(XmlTextWriter writer, string type, Exception e)
    {
        if (e != null)
        {
            writer.WriteStartElement("exception");
            writer.WriteAttributeString("type", type);
            writer.WriteElementString("message", e.Source + ": " + e.Message);
            writer.WriteElementString("trace", e.StackTrace);
            if (e.InnerException != null)
            {
                writer.WriteElementString("messageInner", e.InnerException.Source + ": " + e.InnerException.Message);
                writer.WriteElementString("traceInner", e.InnerException.StackTrace);
            }
            writer.WriteEndElement();
        }
    }

    public static void reportLog ( string log )
    {
        TestResult rrr = header;

        System.Console.WriteLine("Test suite:         {0}",log);
        System.Console.WriteLine("Compiler version:   {0}",rrr.compilerVersion);
        System.Console.WriteLine("Compiler date/time: {0}",rrr.compilerDateTime);
        System.Console.WriteLine("Session date/time:  {0}",rrr.runDateTime);

        while ( rrr != null )
        {
            System.Console.WriteLine("{0}   {1}   {2}   {3}",
                                     rrr.testName,
                                     formatResult(rrr.compilationResult),
                                     formatResult(rrr.executionResult),
                                     formatResult(rrr.commonResult));
            rrr = rrr.next;
        }
    }
}

