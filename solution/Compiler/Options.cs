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
using System.Compiler;
using System.CodeDom.Compiler;
using System.IO;
using System.Collections.Specialized;

using RTL = Zonnon.RTL;

namespace ETH.Zonnon
{
    public class ZonnonCompilerParameters :  CompilerOptions
    {
        public ZonnonCompilerParameters ( ) 
        { 
        }
#if !ROTOR
        internal ZonnonCompilerParameters( ZonnonProjectManager.ZonnonProjectOptions options ) 
          : base(new ETH.Zonnon.Integration.CompilerOptions(options))
        {
            this.Success = options.Success;
            if ( options.MainClass == "" ) this.MainModule = null;
            else                           this.MainModule = options.MainClass;
            this.MainClass = null;
            // this.EmbeddedDialogue = options.EmbeddedDialogue; // Obsolete
            this.GenerateXML = options.GenerateXML;
            this.Output = options.Output;
            this.Quiet = options.Quiet;
            this.SafeMode = options.SafeMode;
            this.Debug = options.Debug;
            this.DebugT = options.DebugT;
            this.Messages = options.Messages;
            this.SourceFiles = options.SourceFiles;
            this.UseComputeMath = options.UseComputeMath;
            this.TargetInformation.Version = options.Version;
            this.TargetInformation.ProductVersion = options.Version;               

            string mscorlibLocation = typeof(System.Console).Assembly.Location;
            string systemLocation   = typeof(System.CodeDom.Compiler.CodeCompiler).Assembly.Location;
            string rtlLocation      = typeof(RTL.CommonException).Assembly.Location;
            this.ReferencedAssemblies.Add(mscorlibLocation);
            this.ReferencedAssemblies.Add(systemLocation);
            this.ReferencedAssemblies.Add(rtlLocation); // "Zonnon.RTL.dll");
        }

#endif
        public bool   Success          = true;
        public string MainModule       = null;
        public bool   EmbeddedDialogue = false;
        public bool   GenerateXML      = false;
        public string XMLDocument      = null;
        public string Output           = null;
        public bool   Quiet            = false;
        public bool   SafeMode         = false;
        public bool   Debug            = false;
        public bool   DebugT           = false;
		public bool   DisplayHelp	   = false;
        public bool   MathOpt          = false;
		public string ExportReport	   = null;
        public bool UseComputeMath = false;

        public StringCollection Messages    = new StringCollection();
        public StringCollection SourceFiles = new StringCollection();
    }
}