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

    public sealed class Buffer : IDisposable {
        IntPtr _handle;
        UInt64 _size;
        Boolean _disposed;

        public Buffer(IntPtr handle, UInt64 size) {
            _handle = handle;
            _size = size;
        }

        public IntPtr Handle {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _handle;
            }
        }

        public UInt64 Size {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _size;
            }
        }

        public void Dispose() {
            if (!_disposed) {
                // (be): ignoring return value, nothing could be done on error anyway
                NativeMethods.ReleaseMemoryObject(_handle);
                _disposed = true;
            }
        }

        ~Buffer() {
            Dispose();
        }
    }
}
