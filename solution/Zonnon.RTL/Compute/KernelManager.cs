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

namespace Zonnon.RTL.Compute {
    using System;
    using Zonnon.RTL.OpenCL.Wrapper;
    using System.Collections.Generic;

    public static class KernelManager {
        static Dictionary<String, CompiledProgram> _compiledPrograms = new Dictionary<String, CompiledProgram>();

        public static Kernel GetKernelForProgram(String source) {
            CompiledProgram program;
            if (!_compiledPrograms.TryGetValue(source, out program)) {
                // compile it
                program = ComputeManager.Context.Compile(source, "-Werror");
                _compiledPrograms.Add(source, program);
            }
            Kernel kernel = program.CreateKernel("Kernel");
            return kernel;
        }

    }
}
