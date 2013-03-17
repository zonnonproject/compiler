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

    class GetInfoArrayCache<TName, TValue> where TValue : struct {
        Func<UInt32> _getLengthCallback;
        GetInfoCallback<TName> _createCallback;
        IntPtr _handle;
        TName _name;
        Boolean _created;
        TValue[] _values;

        public GetInfoArrayCache(Func<UInt32> getLengthCallback, GetInfoCallback<TName> createCallback, IntPtr handle, TName name) {
            _getLengthCallback = getLengthCallback;
            _createCallback = createCallback;
            _handle = handle;
            _name = name;
        }

        public TValue[] GetValue() {
            if (!_created) {
                UInt32 length = _getLengthCallback();
                Int32 size = Marshal.SizeOf(typeof(TValue));
                IntPtr buffer = Marshal.AllocHGlobal(new IntPtr(length * size));
                try {
                    ReturnCode code = _createCallback(_handle, _name, new IntPtr(size * length), buffer, IntPtr.Zero);
                    _values = new TValue[length];
                    for (Int32 index = 0; index < length; index += 1) {
                        _values[index] = (TValue)Marshal.PtrToStructure(new IntPtr(buffer.ToInt64() + index * size), typeof(TValue));
                    }
                    _created = true;
                } finally {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            return (TValue[])_values.Clone();
        }
    }
}
