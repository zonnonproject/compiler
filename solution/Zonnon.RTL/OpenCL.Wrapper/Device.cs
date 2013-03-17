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

    public sealed class Device {
        public IntPtr Handle { get; private set; }
        public Platform Platform { get; private set; }

        GetInfoStructCache<DeviceInfo, UInt32> _maxComputeUnitsCache;
        GetInfoStructCache<DeviceInfo, IntPtr> _maxWorkGroupSizeCache;
        GetInfoStructCache<DeviceInfo, UInt32> _maxClockFrequencyCache;
        GetInfoStructCache<DeviceInfo, UInt64> _maxMemoryAllocationSizeCache;
        GetInfoStructCache<DeviceInfo, UInt64> _globalMemorySizeCache;
        GetInfoStringCache<DeviceInfo> _nameCache;
        GetInfoStructCache<DeviceInfo, UInt32> _maxWorkItemDimensions;
        GetInfoArrayCache<DeviceInfo, IntPtr> _maxWorkItemSizes;

        public Device(IntPtr handle, Platform platform) {
            Handle = handle;
            Platform = platform;
            _maxComputeUnitsCache = new GetInfoStructCache<DeviceInfo, UInt32>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxComputeUnits);
            _maxWorkGroupSizeCache = new GetInfoStructCache<DeviceInfo, IntPtr>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxWorkGroupSize);
            _maxClockFrequencyCache = new GetInfoStructCache<DeviceInfo, UInt32>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxClockFrequency);
            _maxMemoryAllocationSizeCache = new GetInfoStructCache<DeviceInfo, UInt64>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxMemoryAllocationSize);
            _globalMemorySizeCache = new GetInfoStructCache<DeviceInfo, UInt64>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.GlobalMemorySize);
            _nameCache = new GetInfoStringCache<DeviceInfo>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.Name);
            _maxWorkItemDimensions = new GetInfoStructCache<DeviceInfo,UInt32>(NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxWorkItemDimensions);
            _maxWorkItemSizes = new GetInfoArrayCache<DeviceInfo, IntPtr>(_maxWorkItemDimensions.GetValue, NativeMethods.GetDeviceInfo, handle, DeviceInfo.MaxWorkItemSizes);
        }

        public UInt32 MaxComputeUnits { get { return _maxComputeUnitsCache.GetValue(); } }
        public IntPtr MaxWorkGroupSize { get { return _maxWorkGroupSizeCache.GetValue(); } }
        public UInt32 MaxClockFrequency { get { return _maxClockFrequencyCache.GetValue(); } }
        public UInt64 MaxMemoryAllocationSize { get { return _maxMemoryAllocationSizeCache.GetValue(); } }
        public UInt64 GlobalMemorySize { get { return _globalMemorySizeCache.GetValue(); } }
        public String Name { get { return _nameCache.GetValue(); } }
        public IntPtr[] MaxWorkItemSizes { get { return _maxWorkItemSizes.GetValue(); } }
    }
}
