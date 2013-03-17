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

    class GetInfoStructCache<TName, TValue> where TValue : struct {
        GetInfoCallback<TName> _createCallback;
        IntPtr _handle;
        TName _name;
        Boolean _created;
        TValue _value;

        public GetInfoStructCache(GetInfoCallback<TName> createCallback, IntPtr handle, TName name) {
            _createCallback = createCallback;
            _handle = handle;
            _name = name;
        }

        public TValue GetValue() {
            if (!_created) {
                Int32 size = Marshal.SizeOf(typeof(TValue));
                IntPtr buffer = Marshal.AllocHGlobal(size);
                try {
                    ReturnCode code = _createCallback(_handle, _name, new IntPtr(size), buffer, IntPtr.Zero);
                    OpenCLException.ThrowOnError(code);
                    _value = (TValue)Marshal.PtrToStructure(buffer, typeof(TValue));
                    _created = true;
                } finally {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return _value;
        }
    }
}
