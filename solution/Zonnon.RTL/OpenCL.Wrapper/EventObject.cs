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
    using System.Threading;
    using System.Diagnostics;

    public delegate void EventObjectCompletionCallback(EventObject eventObject);

    public class EventObject : IDisposable {
        IntPtr _handle;
        Boolean _disposed;
        GCHandle? _pinnedArgumentHandle;
        List<EventObjectCompletionCallback> _callbacks;
        Object _lock;
        Boolean _completed;
        NativeMethods.EventNotificationCallback _internalCallbackDelegate;
        GetInfoStructCache<CommandProfilingInfo, UInt64> _queuedTimeCache;
        GetInfoStructCache<CommandProfilingInfo, UInt64> _submittedTimeCache;
        GetInfoStructCache<CommandProfilingInfo, UInt64> _startedTimeCache;
        GetInfoStructCache<CommandProfilingInfo, UInt64> _finishedTimeCache;

        static HashSet<EventObject> rootCallbacks = new HashSet<EventObject>();

        public EventObject(IntPtr handle, GCHandle? pinnedArgumentHandle) {
            _handle = handle;
            _pinnedArgumentHandle = pinnedArgumentHandle;
            _lock = new Object();
            _callbacks = new List<EventObjectCompletionCallback>();
            _queuedTimeCache = new GetInfoStructCache<CommandProfilingInfo, UInt64>(NativeMethods.GetEventProfilingInfo, _handle, CommandProfilingInfo.Queued);
            _submittedTimeCache = new GetInfoStructCache<CommandProfilingInfo, UInt64>(NativeMethods.GetEventProfilingInfo, _handle, CommandProfilingInfo.Submitted);
            _startedTimeCache = new GetInfoStructCache<CommandProfilingInfo, UInt64>(NativeMethods.GetEventProfilingInfo, _handle, CommandProfilingInfo.Started);
            _finishedTimeCache = new GetInfoStructCache<CommandProfilingInfo, UInt64>(NativeMethods.GetEventProfilingInfo, _handle, CommandProfilingInfo.Ended);
            _internalCallbackDelegate = Callback;
            ReturnCode code = NativeMethods.SetEventCallback(_handle, CommandExecutionStatus.Complete, _internalCallbackDelegate, IntPtr.Zero);
            OpenCLException.ThrowOnError(code);
            lock (rootCallbacks)
            {
                rootCallbacks.Add(this);
            }
        }

        public void Dispose() {
            if (!_disposed) {
                NativeMethods.ReleaseEvent(_handle);
                if (_pinnedArgumentHandle.HasValue) {
                    _pinnedArgumentHandle.Value.Free();
                }
                _disposed = true;
            }
            lock (rootCallbacks)
            {
                rootCallbacks.Remove(this);
                // Console.WriteLine("Number of callbacks: {0}", rootCallbacks.Count);
            }
        }

        public void RegisterCompletionCallback(EventObjectCompletionCallback callback) {
            //Console.WriteLine("Registering Event Object Callback");
            lock (_lock) {
                if (_completed) {
                    //Console.WriteLine("QueueUserWorkItem");
                    ThreadPool.QueueUserWorkItem(state => callback(this));
                } else {
                    //Console.WriteLine("_callbacks.Add");
                    _callbacks.Add(callback);
                }
            }
            //Console.WriteLine("Registered Event Object Callback");
        }

        void Callback(IntPtr handle, CommandExecutionStatus status, IntPtr data) {
            //Console.WriteLine("Calling back");
            List<EventObjectCompletionCallback> callbacks;
            lock (_lock) {
                _completed = true;
                callbacks = _callbacks;
                _callbacks = null;
            }
            foreach (var callback in callbacks) {
                //Console.WriteLine("Calling back one");
                callback(this);
            }
            //Console.WriteLine("Called back");
        }

        public IntPtr Handle {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                return _handle;
            }
        }

        public CommandExecutionStatus ExecutionStatus {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                IntPtr valuePointer = Marshal.AllocHGlobal(IntPtr.Size);
                CommandExecutionStatus status;
                ReturnCode code = NativeMethods.GetEventInfo(Handle, EventInfo.ExecutionStatus, new IntPtr(sizeof(CommandExecutionStatus)), valuePointer, IntPtr.Zero);
                OpenCLException.ThrowOnError(code);
                status = (CommandExecutionStatus)Marshal.ReadInt32(valuePointer);
                Marshal.FreeHGlobal(valuePointer);
                return status;
            }
        }

        public UInt64 QueuedTime {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                return _queuedTimeCache.GetValue();
            }
        }

        public UInt64 SubmittedTime {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                return _submittedTimeCache.GetValue();
            }
        }

        public UInt64 StartedTime {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                return _startedTimeCache.GetValue();
            }
        }

        public UInt64 FinishedTime {
            get {
                if (_disposed) {
                    throw new ObjectDisposedException(typeof(EventObject).FullName);
                }
                return _finishedTimeCache.GetValue();
            }
        }

        public TimeSpan SubmitToFinishTimeSpan {
            get {
                UInt64 nanoseconds = FinishedTime - SubmittedTime;
                return TimeSpan.FromTicks((Int64)nanoseconds / 100);
            }
        }

        public TimeSpan StartToFinishTimeSpan {
            get {
                UInt64 nanoseconds = FinishedTime - StartedTime;
                return TimeSpan.FromTicks((Int64)nanoseconds / 100);
            }
        }
    }
}
