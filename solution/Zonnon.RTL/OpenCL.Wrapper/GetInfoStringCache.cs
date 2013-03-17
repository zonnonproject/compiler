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

    sealed class GetInfoStringCache<TName> {
        GetInfoCallback<TName> _createCallback;
        IntPtr _handle;
        TName _name;
        String _value;

        public GetInfoStringCache(GetInfoCallback<TName> createCallback, IntPtr handle, TName name) {
            _createCallback = createCallback;
            _handle = handle;
            _name = name;
        }

        public String GetValue() {
            if (_value == null) {
                IntPtr ptr = Marshal.AllocHGlobal(IntPtr.Size);
                try {
                    ReturnCode code = _createCallback(_handle, _name, IntPtr.Zero, IntPtr.Zero, ptr);
                    OpenCLException.ThrowOnError(code);
                    IntPtr size = Marshal.ReadIntPtr(ptr);
                    if (size.ToInt32() > 0) {
                        ptr = Marshal.AllocHGlobal(size);
                        _createCallback(_handle, _name, size, ptr, IntPtr.Zero);
                        _value = Marshal.PtrToStringAnsi(ptr, size.ToInt32() - 1);
                    } else {
                        _value = "";
                    }
                } finally {
                    Marshal.FreeHGlobal(ptr);
                }
            }
            return _value;
        }
    }
}
