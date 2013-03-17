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

namespace Zonnon.RTL.OpenCL.Wrapper {
    using System;

    public sealed class CompiledProgram : IDisposable {
        public IntPtr Handle { get; private set; }
        Boolean _disposed;

        public CompiledProgram(IntPtr handle) {
            Handle = handle;
        }

        public Kernel CreateKernel(String name) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            ReturnCode code;
            IntPtr kernelHandle = NativeMethods.CreateKernel(Handle, name, out code);
            OpenCLException.ThrowOnError(code);
            return new Kernel(kernelHandle);
        }

        public void Dispose() {
            if (!_disposed) {
                // (be): ignoring return value, nothing could be done on error anyway
                NativeMethods.ReleaseProgram(Handle);
                _disposed = true;
            }
        }

        ~CompiledProgram() {
            Dispose();
        }

    }
}
