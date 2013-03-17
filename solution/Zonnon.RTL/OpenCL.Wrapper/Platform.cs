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
    using System.Linq;

    public sealed class Platform {
        public IntPtr Handle { get; private set; }

        public static Platform[] GetPlatforms() {
            Int32 count;
            ReturnCode code = NativeMethods.GetPlatformIDs(0, null, out count);
            OpenCLException.ThrowOnError(code);
            IntPtr[] handles = new IntPtr[count];
            code = NativeMethods.GetPlatformIDs(count, handles, out count);
            OpenCLException.ThrowOnError(code);
            return handles.Select(handle => new Platform(handle)).ToArray();
        }

        public Platform(IntPtr handle) {
            Handle = handle;
        }

        public Device[] GetDevices(DeviceType types) {
            Int32 count;
            ReturnCode code = NativeMethods.GetDeviceIDs(Handle, types, 0, null, out count);
            if (code == ReturnCode.DeviceNotFound) {
                return new Device[0];
            } else {
                OpenCLException.ThrowOnError(code);
                IntPtr[] handles = new IntPtr[count];
                code = NativeMethods.GetDeviceIDs(Handle, types, count, handles, out count);
                OpenCLException.ThrowOnError(code);
                return handles.Select(handle => new Device(handle, this)).ToArray();
            }
        }
    }
}
