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

namespace Zonnon.RTL.Compute {
    using System;
    using Zonnon.RTL.OpenCL.Wrapper;
    using System.Collections.Generic;
    using System.Diagnostics;

    public abstract class ComputeDevice {
        /* invariants
         * Queue != null
         * FinishTime == null <=> Queue.Count == 0
         */

        //struct TaskFinishTime {
        //    public Task Task { get; private set; }
        //    public DateTime FinishTime { get; set; }

        //    public TaskFinishTime(Task task, DateTime finishTime) {
        //        Task = task;
        //        FinishTime = finishTime;
        //    }
        //}

        public Int32 Id { get; private set; }
        internal DeviceMemorySpace MemorySpace { get; private set; }
        List<Task> _tasks;

        internal ComputeDevice(Int32 id, DeviceMemorySpace memorySpace) {
            Id = id;
            MemorySpace = memorySpace;
            _tasks = new List<Task>();
        }

        internal DateTime GetIdleTime() {
            if (_tasks.Count == 0) {
                return DateTime.Now;
            } else {
                return _tasks[_tasks.Count - 1].ScheduledTaskInfo.FinishTime;
            }
        }

        internal void EnqueueTask(Task task) {
            DateTime beginTime;
            if (_tasks.Count == 0) {
                beginTime = DateTime.Now;
            } else {
                beginTime = _tasks[_tasks.Count - 1].ScheduledTaskInfo.FinishTime;
            }
            task.ScheduledTaskInfo.FinishTime = beginTime + task.ScheduledTaskInfo.Duration;
            _tasks.Add(task);
            //Console.WriteLine("Task {0} enqueued on device {1}", task.Id, Id);
        }

        internal void DequeueTask(Task task) {
            Int32 index = _tasks.IndexOf(task);
            Debug.Assert(index != -1);
            _tasks.RemoveAt(index);
            DateTime previousFinishTime;
            if (index == 0) {
                previousFinishTime = DateTime.Now;
            } else {
                previousFinishTime = _tasks[index - 1].ScheduledTaskInfo.FinishTime;
            }
            while (index < _tasks.Count) {
                previousFinishTime += _tasks[index].ScheduledTaskInfo.Duration;
                _tasks[index].ScheduledTaskInfo.FinishTime = previousFinishTime;
                index += 1;
            }
        }
    }

    public class OpenCLComputeDevice : ComputeDevice {
        public CommandQueue CommandQueue { get; private set; }
        Device _device;

        internal static OpenCLComputeDevice Create(Int32 id, Context context, Device device, double hostTransferSpeed) {
            CommandQueue queue = new CommandQueue(context, device, CommandQueueFlags.None);
            DeviceMemorySpace space = new DeviceMemorySpace(hostTransferSpeed, queue);
            return new OpenCLComputeDevice(id, device, queue, space);
        }

        OpenCLComputeDevice(Int32 id, Device device, CommandQueue queue, DeviceMemorySpace memorySpace)
            : base(id, memorySpace) {
            CommandQueue = queue;
            _device = device;
        }

        public static Type Type {
            get {
                return typeof(OpenCLComputeDevice);
            }
        }

        public UInt64[] GetMatrixMatrixMultiplicationGlobalRange(UInt64[] operationSize) {
            return new UInt64[] { 1024, 1024 };
        }

        public UInt64 GetMatrixMatrixMultiplicationLocalSize() {
            return 16;
        }

        public UInt64[] GetElementwiseGlobalSize(UInt64 size) {
            UInt64 value = Math.Min(size, (UInt64)_device.MaxWorkItemSizes[0].ToInt64());
            return new UInt64[] { value };
        }

        public UInt64[] GetMatrixVectorMultiplicationGlobalSize(UInt64[] size) {
            UInt64 value = Math.Min(size[0], (UInt64)_device.MaxWorkItemSizes[0].ToInt64());
            return new UInt64[] { value };
        }

        public UInt64[] GetVectorMatrixMultiplicationGlobalSize(UInt64[] size) {
            UInt64 value = Math.Min(size[1], (UInt64)_device.MaxWorkItemSizes[0].ToInt64());
            return new UInt64[] { value };
        }

        public UInt64 GetReductionSize(UInt64 size) {
            UInt64 max = Math.Min(size, (UInt64)_device.MaxWorkGroupSize.ToInt64());
            UInt64 workGroupSize = 1;
            while (workGroupSize <= max) {
                workGroupSize *= 2;
            }
            return workGroupSize / 2;
        }
    }
}
