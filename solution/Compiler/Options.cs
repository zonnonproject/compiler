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
        public bool   UseComputeMath   = false;

        public StringCollection Messages    = new StringCollection();
        public StringCollection SourceFiles = new StringCollection();
    }
}