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
    using System.Runtime.InteropServices;
    using System.Collections.Generic;

    public sealed class Kernel : IDisposable {
        public IntPtr Handle { get; private set; }
        Boolean _disposed;
        // keep track of arguments to ensure that garbage collector does not free them
        Dictionary<Int32, Object> _arguments;

        public Kernel(IntPtr handle) {
            Handle = handle;
            _arguments = new Dictionary<Int32, Object>();
        }

        public void SetGlobalArgument(Int32 index, Buffer buffer) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            _arguments[index] = buffer;
            SetValueArgument(index, buffer.Handle);
        }

        public void SetLocalArgument(Int32 index, UInt64 size) {
            SetArgument(index, (Int64)size, IntPtr.Zero);
        }

        public void SetValueArgument<T>(Int32 index, T data) where T : struct {
            Int32 size = Marshal.SizeOf(typeof(T));
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try {
                SetArgument(index, size, handle.AddrOfPinnedObject());
            } finally {
                handle.Free();
            }
        }

        public void SetValueArgumentDynamic(Int32 index, Object value) {
            Int32 size = Marshal.SizeOf(value);
            GCHandle handle = GCHandle.Alloc(value, GCHandleType.Pinned);
            try {
                SetArgument(index, size, handle.AddrOfPinnedObject());
            } finally {
                handle.Free();
            }
        }

        void SetArgument(Int32 index, Int64 size, IntPtr value) {
            ReturnCode code = NativeMethods.SetKernelArgument(Handle, index, new IntPtr(size), value);
            OpenCLException.ThrowOnError(code);
        }

        public void Dispose() {
            if (!_disposed) {
                // (be): ignoring return value, nothing could be done on error anyway
                NativeMethods.ReleaseKernel(Handle);
                _disposed = true;
            }
        }

        ~Kernel() {
            Dispose();
        }

    }
}
