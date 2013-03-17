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
using System.Collections.Generic;
using System.Text;
using System.Compiler;

namespace ETH.Zonnon
{
    public class Analyzer: System.Compiler.Analyzer
    {
        public Analyzer(TypeSystem t, Compilation c): base(t,c)
        {
        }
        /// <summary>
        /// Zonnon specific flow analysis.
        /// </summary>
        /// <param name="method"></param>
        protected override void LanguageSpecificAnalysis(Method method)
        {
        }
    }
}
