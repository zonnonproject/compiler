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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Buffer = Zonnon.RTL.OpenCL.Wrapper.Buffer;

    class ScheduledTaskInfo {
        public Codelet Codelet { get; private set; }
        public ComputeDevice ComputeDevice { get; private set; }
        public TimeSpan Duration { get; private set; }
        public DateTime FinishTime { get; set; }

        public ScheduledTaskInfo(Codelet codelet, ComputeDevice computeDevice, TimeSpan duration, DateTime finishTime) {
            Codelet = codelet;
            ComputeDevice = computeDevice;
            Duration = duration;
            FinishTime = finishTime;
        }
    }

    class Task {
        Codelet[] _codelets;
        DataUse[] _uses;
        internal ScheduledTaskInfo ScheduledTaskInfo { get; private set; }
        public Int32 Id { get; private set; }

        public Task(Int32 id, Codelet[] codelets, DataUse[] uses) {
            Debug.Assert(codelets != null);
            _codelets = codelets;
            Debug.Assert(uses != null);
            _uses = uses;
            Id = id;
            DependencyManager.RegisterOperation(this);
            List<Object> predecessorConditions = new List<Object>();
            foreach (var use in uses) {
                if (use.Data.WaitingTasks.Count > 0) {
                    predecessorConditions.Add(use.Data.WaitingTasks[use.Data.WaitingTasks.Count - 1].Condition);
                }
                Object waitFinishedObject = new Object();
                DependencyManager.RegisterOperation(waitFinishedObject);
                use.Data.WaitingTasks.Add(new TaskDataCondition(this, waitFinishedObject));
            }
            DependencyManager.RegisterContinuation(predecessorConditions, Schedule);
            //Console.WriteLine("Task {0} created", Id);
        }

        public void Schedule() {
            Debug.Assert(_uses.All(use => Object.ReferenceEquals(use.Data.WaitingTasks[0].Task, this)));
            // because we are fist in all the data queues we know that there is no other task copying to/from this data
            // find best device to schedule task onto. this depends on the codelet duration and data availability
            // assume that al data transfers are serialized

            foreach (Codelet codelet in _codelets) {
                ComputeDevice[] devices;
                if (ComputeManager.Devices.TryGetValue(codelet.DeviceType, out devices)) {
                    foreach (ComputeDevice device in devices) {
                        DateTime startTime = DateTime.Now;
                        foreach (DataUse use in _uses) {
                            DateTime dataAvailableTime = use.Data.GetAvailabilityTime(use.Range, use.DataAccess, device.MemorySpace);
                            if (dataAvailableTime > startTime) {
                                startTime = dataAvailableTime;
                            }
                        }
                        TimeSpan duration = (TimeSpan)codelet.GetExpectedTime.DynamicInvoke(device);
                        DateTime finishTime = device.GetIdleTime() + duration;
                        if (ScheduledTaskInfo == null || ScheduledTaskInfo.FinishTime > finishTime) {
                            ScheduledTaskInfo = new ScheduledTaskInfo(codelet, device, duration, finishTime);
                        }
                    }
                }
            }
            Debug.Assert(ScheduledTaskInfo != null);
            // schedule task on device
            List<Object> operations = new List<Object>();
            foreach (DataUse use in _uses) {
                operations.Add(use.Data.StartPrepareRange(this, use.Range, use.DataAccess, ScheduledTaskInfo.ComputeDevice.MemorySpace));
            }
            DependencyManager.RegisterContinuation(operations, Start);
            //Console.WriteLine("Task {0} scheduled on device {1}", Id, ScheduledTaskInfo.ComputeDevice.Id);
        }

        void Start() {
            // all data is present on device. start operation
            ScheduledTaskInfo.ComputeDevice.EnqueueTask(this);
            Object[] arguments = new Object[_uses.Length + 1];
            arguments[0] = ScheduledTaskInfo.ComputeDevice;
            for (Int32 index = 0; index < _uses.Length; index++) {
                arguments[index + 1] = _uses[index].Data.GetCodeletArgumentForMemorySpace(_uses[index].Range, ScheduledTaskInfo.ComputeDevice.MemorySpace);
            }
            Object operation = ScheduledTaskInfo.Codelet.Start.DynamicInvoke(arguments);
            DependencyManager.RegisterContinuation(new Object[] { operation }, Finish);
            //Console.WriteLine("Task {0} started", Id);
        }

        void Finish() {
            ScheduledTaskInfo.ComputeDevice.DequeueTask(this);
            foreach (DataUse use in _uses) {
                use.Data.FinishTask(this, use.Range, use.DataAccess, ScheduledTaskInfo.ComputeDevice.MemorySpace);
            }
            DependencyManager.FinishOperationLocked(this);
            //Console.WriteLine("Task {0} finished", Id);
        }
    }
}
