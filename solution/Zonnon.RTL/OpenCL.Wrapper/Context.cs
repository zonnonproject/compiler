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
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Diagnostics;

    public sealed class Context : IDisposable {
        public IntPtr Handle { get; private set; }
        public Platform Platform { get; private set; }
        Device[] _devices;
        Boolean _disposed;
        NativeMethods.ContextNotificationCallback _callback;

        public Context(IntPtr handle) {
            Handle = handle;
        }

        public Context(Device[] devices) {
            if (devices == null) {
                throw new ArgumentNullException("devices");
            }
            if (devices.Length == 0) {
                throw new ArgumentException("must contain at least one device", "devices");
            }
            if (devices.Any(device => device == null)) {
                throw new ArgumentException("must not contain null values", "devices");
            }
            Platform = devices[0].Platform;
            if (devices.Any(device => device.Platform != Platform)) {
                throw new ArgumentException("all devices must belong to the same platform");
            }
            IntPtr[] properties = new IntPtr[3];
            properties[0] = new IntPtr((Int32)ContextProperty.Platform);
            properties[1] = Platform.Handle;
            IntPtr[] deviceHandles = devices.Select(device => device.Handle).ToArray();
            ReturnCode code;
            _callback = Callback;
            Handle = NativeMethods.CreateContext(properties, deviceHandles.Length, deviceHandles, _callback, IntPtr.Zero, out code);
            OpenCLException.ThrowOnError(code);
            _devices = (Device[])devices.Clone();
        }

        private void Callback(String errinfo, IntPtr private_info, IntPtr cb, IntPtr user_data) {
            throw new Exception("OpenCL: " + errinfo);
        }

        public Device[] Devices {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return (Device[])_devices.Clone();
            }
        }

        public Buffer CreateBuffer(UInt64 size, BufferFlags flags) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            ReturnCode code;
            IntPtr handle = NativeMethods.CreateBuffer(Handle, flags, new UIntPtr(size), IntPtr.Zero, out code);
            OpenCLException.ThrowOnError(code);
            return new Buffer(handle, size);
        }

        Object _lock = new Object();

        public CompiledProgram Compile(String source, String options) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            lock (_lock) {
                ReturnCode code;
                IntPtr handle = NativeMethods.CreateProgramWithSource(Handle, 1, new String[] { source }, new IntPtr[] { new IntPtr(source.Length) }, out code);
                OpenCLException.ThrowOnError(code);
                IntPtr[] devices = Array.ConvertAll(_devices, device => device.Handle);
                code = NativeMethods.BuildProgram(handle, devices.Length, devices, options, IntPtr.Zero, IntPtr.Zero);
                if (code == ReturnCode.BuildProgramFailure) {
                    Console.WriteLine(source);
                    String[] logs = Array.ConvertAll(_devices, device => {
                        //String log = null;
                        //IntPtr currentSize = new IntPtr(1);
                        //IntPtr actualSize;
                        //Boolean exit = false;
                        //do {
                        //    IntPtr characters = Marshal.AllocHGlobal(currentSize);
                        //    try {
                        //        System.Diagnostics.Debugger.Launch();
                        //        code = NativeMethods.GetProgramBuildInfoString(handle, device.Handle, ProgramBuildInfo.BuildLog, currentSize, characters, out actualSize);
                        //        OpenCLException.ThrowOnError(code);
                        //        if (currentSize == actualSize) {
                        //            log = Marshal.PtrToStringAnsi(characters, currentSize.ToInt32());
                        //            exit = true;
                        //        } else {
                        //            currentSize = actualSize;
                        //        }
                        //    } finally {
                        //        Marshal.FreeHGlobal(characters);
                        //    }
                        //} while (!exit);
                        IntPtr size = IntPtr.Zero;
                        ReturnCode code2 = NativeMethods.GetProgramBuildInfoString(handle, device.Handle, ProgramBuildInfo.BuildLog, IntPtr.Zero, IntPtr.Zero, out size);
                        OpenCLException.ThrowOnError(code2);
                        IntPtr characters = Marshal.AllocHGlobal(size);
                        code2 = NativeMethods.GetProgramBuildInfoString(handle, device.Handle, ProgramBuildInfo.BuildLog, size, characters, out size);
                        OpenCLException.ThrowOnError(code2);
                        String log = Marshal.PtrToStringAnsi(characters, size.ToInt32());
                        Marshal.FreeHGlobal(characters);
                        Console.WriteLine(log);
                        return log;
                    });
                    throw new BuildProgramFailureOpenCLException(logs);
                } else {
                    try {
                        OpenCLException.ThrowOnError(code);
                    } catch (OpenCLException) {
                        NativeMethods.ReleaseProgram(handle);
                        throw;
                    }
                    return new CompiledProgram(handle);
                }
            }
        }

        public void WaitForEvents(params EventObject[] events) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (events == null) {
                throw new ArgumentNullException("events");
            }
            if (events.Length == 0) {
                throw new ArgumentException("events");
            }
            IntPtr[] handles;
            Int32 handlesLength;
            if (!Helpers.TryGetEventHandles(events, out handles, out handlesLength)) {
                throw new ArgumentException("events");
            }
            ReturnCode code = NativeMethods.WaitForEvents(handlesLength, handles);
            OpenCLException.ThrowOnError(code);
        }

        public void Dispose() {
            if (!_disposed) {
                // (be): ignoring return value, nothing could be done on error anyway
                NativeMethods.ReleaseContext(Handle);
                _disposed = true;
            }
        }

        ~Context() {
            Dispose();
        }
    }
}
