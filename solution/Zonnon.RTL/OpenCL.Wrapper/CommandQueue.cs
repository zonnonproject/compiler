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
    using System.Diagnostics;

    public sealed class CommandQueue : IDisposable {
        IntPtr _handle;
        Boolean _disposed;
        Context _context;
        Device _device;

        public CommandQueue(Context context, Device device, CommandQueueFlags flags) {
            if (context == null) {
                throw new ArgumentNullException("context");
            }
            if (device == null) {
                throw new ArgumentNullException("device");
            }
            _context = context;
            _device = device;
            ReturnCode code;
            _handle = NativeMethods.CreateCommandQueue(context.Handle, device.Handle, flags, out code);
            OpenCLException.ThrowOnError(code);
        }

        public IntPtr Handle {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _handle;
            }
        }

        public Context Context {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _context;
            }
        }

        public Device Device {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(GetType().FullName);
                }
                return _device;
            }
        }

        //public void ReadBuffer(Buffer source, Array destination) {
        //    if (_disposed) {
        //        throw new ObjectDisposedException(GetType().FullName);
        //    }
        //    if (source == null) {
        //        throw new ArgumentNullException("source");
        //    }
        //    if (destination == null) {
        //        throw new ArgumentNullException("destination");
        //    }
        //    GCHandle pinnedDestination = GCHandle.Alloc(destination, GCHandleType.Pinned);
        //    ReturnCode code = NativeMethods.ReadBuffer(
        //        _handle,
        //        source.Handle,
        //        OpenCLBoolean.True,
        //        IntPtr.Zero,
        //        new IntPtr(System.Buffer.ByteLength(destination)),
        //        pinnedDestination.AddrOfPinnedObject(),
        //        0,
        //        IntPtr.Zero,
        //        IntPtr.Zero
        //    );
        //    pinnedDestination.Free();
        //    OpenCLException.ThrowOnError(code);
        //}

        public EventObject StartReadBuffer(Buffer source, Array destination, Int64 destinationIndex, Int64 destinationCount, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            Int64 size = System.Buffer.ByteLength(destination);
            if (destinationIndex < 0 || destinationCount <= 0 || destinationIndex + destinationCount > size) {
                throw new ArgumentOutOfRangeException("destinationIndex/destinationCount");
            }
            Debug.Assert(destinationCount == (Int64)source.Size);
            IntPtr[] handles;
            Int32 handlesLength;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            GCHandle pinnedDestination = GCHandle.Alloc(destination, GCHandleType.Pinned);
            EventObject result = null;
            try {
                IntPtr newEvent;
                ReturnCode code = NativeMethods.BeginReadBuffer(
                    _handle,
                    source.Handle,
                    OpenCLBoolean.False,
                    IntPtr.Zero,
                    new IntPtr((Int64) destinationCount),
                    new IntPtr(pinnedDestination.AddrOfPinnedObject().ToInt64() + (Int64)destinationIndex),
                    handlesLength,
                    handles,
                    out newEvent
                );
                OpenCLException.ThrowOnError(code);
                result = new EventObject(newEvent, pinnedDestination);
            } finally {
                if (result == null) {
                    pinnedDestination.Free();
                }
            }
            return result;
        }

        //public void WriteBuffer(Array source, Buffer destination) {
        //    if (_disposed) {
        //        throw new ObjectDisposedException(GetType().FullName);
        //    }
        //    if (source == null) {
        //        throw new ArgumentNullException("source");
        //    }
        //    if (destination == null) {
        //        throw new ArgumentNullException("destination");
        //    }
        //    GCHandle pinnedSource = GCHandle.Alloc(source, GCHandleType.Pinned);
        //    ReturnCode code = NativeMethods.WriteBuffer(
        //        _handle,
        //        destination.Handle,
        //        OpenCLBoolean.True,
        //        IntPtr.Zero,
        //        new IntPtr(System.Buffer.ByteLength(source)),
        //        pinnedSource.AddrOfPinnedObject(),
        //        0,
        //        IntPtr.Zero,
        //        IntPtr.Zero
        //    );
        //    pinnedSource.Free();
        //    OpenCLException.ThrowOnError(code);
        //}

        public EventObject StartWriteBuffer(Array source, Int64 sourceIndex, Int64 sourceCount, Buffer destination, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            Int64 size = System.Buffer.ByteLength(source);
            if (sourceIndex < 0 || sourceCount <= 0 || sourceIndex + sourceCount > size) {
                throw new ArgumentOutOfRangeException("destinationIndex/destinationCount");
            }
            Debug.Assert(sourceCount == (Int64)destination.Size);
            IntPtr[] handles = null;
            Int32 handlesLength = 0;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            GCHandle pinnedSource = GCHandle.Alloc(source, GCHandleType.Pinned);
            EventObject result = null;
            try {
                IntPtr newEvent;
                ReturnCode code = NativeMethods.BeginWriteBuffer(
                    _handle,
                    destination.Handle,
                    OpenCLBoolean.False,
                    IntPtr.Zero,
                    new IntPtr(sourceCount),
                    new IntPtr(pinnedSource.AddrOfPinnedObject().ToInt64() + sourceIndex),
                    handlesLength,
                    handles,
                    out newEvent
                );
                OpenCLException.ThrowOnError(code);
                result = new EventObject(newEvent, pinnedSource);
            } finally {
                if (result == null) {
                    pinnedSource.Free();
                }
            }
            return result;
        }

        public EventObject StartReadBufferRect(Buffer source, Array destination, IntPtr hostOriginX, IntPtr hostOriginY, IntPtr hostOriginZ, IntPtr regionX, IntPtr regionY, IntPtr regionZ, IntPtr sizeX, IntPtr sizeXY, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            IntPtr[] handles = null;
            Int32 handlesLength = 0;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            GCHandle pinnedTarget = GCHandle.Alloc(destination, GCHandleType.Pinned);
            EventObject result = null;
            try {
                IntPtr newEvent;
                ReturnCode code = NativeMethods.BeginReadBufferRect(
                    _handle,
                    source.Handle,
                    OpenCLBoolean.False,
                    new IntPtr[] { IntPtr.Zero, IntPtr.Zero, IntPtr.Zero },
                    new IntPtr[] { hostOriginX, hostOriginY, hostOriginZ },
                    new IntPtr[] { regionX, regionY, regionZ },
                    IntPtr.Zero,
                    IntPtr.Zero,
                    sizeX,
                    sizeXY,
                    pinnedTarget.AddrOfPinnedObject(),
                    handlesLength,
                    handles,
                    out newEvent
                );
                OpenCLException.ThrowOnError(code);
                result = new EventObject(newEvent, pinnedTarget);
            } finally {
                if (result == null) {
                    pinnedTarget.Free();
                }
            }
            return result;
        }

        public EventObject StartWriteBufferRect(Array source, Buffer destination, IntPtr hostOriginX, IntPtr hostOriginY, IntPtr hostOriginZ, IntPtr regionX, IntPtr regionY, IntPtr regionZ, IntPtr sizeX, IntPtr sizeXY, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            IntPtr[] handles = null;
            Int32 handlesLength = 0;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            GCHandle pinnedSource = GCHandle.Alloc(source, GCHandleType.Pinned);
            EventObject result = null;
            try {
                IntPtr newEvent;
                ReturnCode code = NativeMethods.BeginWriteBufferRect(
                    _handle,
                    destination.Handle,
                    OpenCLBoolean.False,
                    new IntPtr[] { IntPtr.Zero, IntPtr.Zero, IntPtr.Zero },
                    new IntPtr[] { hostOriginX, hostOriginY, hostOriginZ },
                    new IntPtr[] { regionX, regionY, regionZ },
                    IntPtr.Zero,
                    IntPtr.Zero,
                    sizeX,
                    sizeXY,
                    pinnedSource.AddrOfPinnedObject(),
                    handlesLength,
                    handles,
                    out newEvent
                );
                OpenCLException.ThrowOnError(code);
                result = new EventObject(newEvent, pinnedSource);
            } finally {
                if (result == null) {
                    pinnedSource.Free();
                }
            }
            return result;
        }

        public EventObject StartCopyBuffer(Buffer source, Buffer destination, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (destination == null) {
                throw new ArgumentNullException("destination");
            }
            if (source.Size != destination.Size) {
                throw new ArgumentException("sizes of arguments are not equal");
            }
            IntPtr[] handles = null;
            Int32 handlesLength = 0;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            IntPtr newEvent;
            ReturnCode code = NativeMethods.CopyBuffer(
                _handle,
                source.Handle,
                destination.Handle,
                IntPtr.Zero,
                IntPtr.Zero,
                new IntPtr((Int64)source.Size),
                handlesLength,
                handles,
                out newEvent
            );
            OpenCLException.ThrowOnError(code);
            return new EventObject(newEvent, null);
        }

        public EventObject StartKernel(Kernel kernel, UInt64[] globalRange, UInt64[] localRange, params EventObject[] predecessors) {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            if (kernel == null) {
                throw new ArgumentNullException("kernel");
            }
            if (globalRange == null) {
                throw new ArgumentNullException("globalRange");
            }
            Int32 dimension = globalRange.Length;
            if (localRange != null && localRange.Length != dimension) {
                throw new ArgumentException("globalRange and localRange must have the same dimensions");
            }
            IntPtr[] handles;
            Int32 handlesLength;
            if (!Helpers.TryGetEventHandles(predecessors, out handles, out handlesLength)) {
                throw new ArgumentException("predecessors");
            }
            IntPtr newEvent;
            ReturnCode code = NativeMethods.EnqueueNDRangeKernel(
                _handle,
                kernel.Handle,
                dimension,
                null,
                ToUIntPtrArray(globalRange),
                ToUIntPtrArray(localRange),
                handlesLength,
                handles,
                out newEvent
            );
            OpenCLException.ThrowOnError(code);
            return new EventObject(newEvent, null);
        }

        public void CompleteEnqueuedCommands() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            ReturnCode code = NativeMethods.Finish(_handle);
            OpenCLException.ThrowOnError(code);
        }

        public void Flush() {
            if (_disposed) {
                throw new ObjectDisposedException(GetType().FullName);
            }
            ReturnCode code = NativeMethods.Flush(_handle);
            OpenCLException.ThrowOnError(code);
        }

        static UIntPtr[] ToUIntPtrArray(UInt64[] array) {
            UIntPtr[] result;
            if (array == null) {
                result = null;
            } else {
                result = Array.ConvertAll(array, e => new UIntPtr(e));
            }
            return result;
        }

        public void Dispose() {
            if (!_disposed) {
                // (be): ignoring return value, nothing could be done on error anyway
                NativeMethods.ReleaseCommandQueue(_handle);
                _disposed = true;
            }
        }

        ~CommandQueue() {
            Dispose();
        }
    }
}
