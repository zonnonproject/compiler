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
    using Buffer = Zonnon.RTL.OpenCL.Wrapper.Buffer;
    using System.Diagnostics;
    using Zonnon.RTL.OpenCL.Wrapper;
    using System.Threading;

    [Flags]
    public enum DataAccess {
        Read = 0x01,
        Write = 0x10,
        ReadWrite = 0x11,
    }

    public struct DataUse {
        public Data Data { get; private set; }
        internal DataRange Range { get; private set; }
        public DataAccess DataAccess { get; private set; }

        public DataUse(Data data, DataRangeIndex[] range, DataAccess dataAccess)
            : this() {
            Data = data;
            Range = new DataRange(range);
            DataAccess = dataAccess;
        }
    }

    public class DataRangeIndex {
        public Int64 Value { get; private set; }

        public DataRangeIndex(Int64 index) {
            Value = index;
        }
    }

    internal struct DataRange {
        public DataRangeIndex[] Indices { get; private set; }

        public DataRange(DataRangeIndex[] range)
            : this() {
            Debug.Assert(range != null);
            Indices = range;
        }

        public Boolean Intersects(DataRange other) {
            Debug.Assert(Indices.Length == other.Indices.Length);
            for (Int32 index = 0; index < Indices.Length; index += 1) {
                if (Indices[index] != null && other.Indices[index] != null && Indices[index].Value != other.Indices[index].Value) {
                    return false;
                }
            }
            return true;
        }

        public override String ToString() {
            String result = "";
            foreach (DataRangeIndex index in Indices) {
                if (index != null) {
                    result += index.Value.ToString();
                } else {
                    result += "*";
                }
            }
            return result;
        }

        public static Boolean operator ==(DataRange range1, DataRange range2) {
            Debug.Assert(range1.Indices.Length == range2.Indices.Length);
            for (Int32 index = 0; index < range1.Indices.Length; index += 1) {
                DataRangeIndex index1 = range1.Indices[index];
                DataRangeIndex index2 = range2.Indices[index];
                if (index1 != null && index2 != null) {
                    if (index1.Value != index2.Value) {
                        return false;
                    }
                } else if (index1 != null || index2 != null) {
                    return false;
                }
            }
            return true;
        }

        public static Boolean operator !=(DataRange range1, DataRange range2) {
            return !(range1 == range2);
        }
    }

    class DataRangeCopy {
        public Buffer Buffer { get; private set; }
        public DeviceMemorySpace DeviceMemorySpace { get; private set; }
        public DataRange Range { get; private set; }
        public HashSet<Task> ReadingTasks { get; private set; }
        public Task WritingTask { get; set; }
        public Boolean Dirty { get; set; }

        public DataRangeCopy(Buffer buffer, DeviceMemorySpace space, DataRange range) {
            Buffer = buffer;
            DeviceMemorySpace = space;
            Range = range;
            ReadingTasks = new HashSet<Task>();
        }
    }

    struct TaskDataCondition {
        public Task Task { get; private set; }
        public Object Condition { get; private set; }

        public TaskDataCondition(Task task, Object condition)
            : this() {
            Task = task;
            Condition = condition;
        }
    }

    public class Data {
        // invariants:
        // for all DataRangeCopies it must hold that if the the ranges intersect then they are both purely read and not written.

        Array _hostArray;
        UInt64[] _dimensions;
        UInt64 _elementSize;
        Boolean _disposed;

        internal List<TaskDataCondition> WaitingTasks { get; private set; }
        internal List<DataRangeCopy> DataRangeCopies { get; private set; }

        public Data(Array hostArray) {
            Debug.Assert(hostArray != null);
            WaitingTasks = new List<TaskDataCondition>();
            DataRangeCopies = new List<DataRangeCopy>();
            _hostArray = hostArray;
            _dimensions = ComputeHelper.GetDimensions(hostArray);
            _elementSize = (UInt64)System.Buffer.ByteLength(hostArray);
            for (Int32 index = 0; index < hostArray.Rank; index += 1) {
                UInt64 dimensionLength = (UInt64)hostArray.GetLongLength(index);
                UInt64 newElementSize = _elementSize / dimensionLength;
                // must be divisable
                Debug.Assert(newElementSize * dimensionLength == _elementSize);
                _elementSize = newElementSize;
            }
        }

        public UInt64[] GetDimensions() {
            if (_disposed) {
                throw new ObjectDisposedException(typeof(Data).FullName);
            }
            return _dimensions;
        }

        internal Object GetCodeletArgumentForMemorySpace(DataRange range, DeviceMemorySpace space) {
            if (_disposed) {
                throw new ObjectDisposedException(typeof(Data).FullName);
            }
            Debug.Assert(space != null);
            DataRangeCopy copy = FindDataRangeCopy(range, space);
            return copy.Buffer;
        }

        DateTime Max(DateTime a, DateTime b) {
            if (a < b) {
                return b;
            } else {
                return a;
            }
        }

        Boolean IsRangeValid(DataRange range) {
            if (range.Indices.Length != _dimensions.Length) {
                return false;
            }
            for (Int32 index = 0; index < _dimensions.Length; index += 1) {
                DataRangeIndex value = range.Indices[index];
                if (value != null && (value.Value < 0 || value.Value >= (Int64)_dimensions[index])) {
                    return false;
                }
            }
            return true;
        }

        internal DateTime GetAvailabilityTime(DataRange range, DataAccess access, DeviceMemorySpace memorySpace) {
            if (_disposed) {
                throw new ObjectDisposedException(typeof(Data).FullName);
            }
            Debug.Assert(memorySpace != null);
            if (!IsRangeValid(range)) {
                throw new ArgumentOutOfRangeException();
            }
            Boolean transfer = false;
            DateTime result = DateTime.Now;
            foreach (DataRangeCopy copy in DataRangeCopies) {
                if (range.Intersects(copy.Range)) {
                    DateTime rangeLastFinishTime = DateTime.Now;
                    // maintain consistency
                    if (copy.WritingTask != null) {
                        rangeLastFinishTime = Max(rangeLastFinishTime, copy.WritingTask.ScheduledTaskInfo.FinishTime);
                    }
                    if ((access & DataAccess.Write) == DataAccess.Write) {
                        // task will be writing, so we also have to wait for reading tasks
                        foreach (Task task in copy.ReadingTasks) {
                            rangeLastFinishTime = Max(rangeLastFinishTime, task.ScheduledTaskInfo.FinishTime);
                        }
                    }
                    if ((range != copy.Range || memorySpace != copy.DeviceMemorySpace) && copy.Dirty) {
                        // copy is dirty, needs to transfer this copy to the host
                        rangeLastFinishTime += TimeSpan.FromSeconds(GetRangeSize(copy.Range) / copy.DeviceMemorySpace.HostTransferSpeed);
                        transfer = true;
                    }
                    result = Max(result, rangeLastFinishTime);
                }
            }
            if (transfer) {
                // host data was modified, transfer fresh bytes to the range
                result += TimeSpan.FromSeconds(GetRangeSize(range) / memorySpace.HostTransferSpeed);
            }
            return result;
        }

        UInt64 GetRangeSize(DataRange range) {
            Debug.Assert(range.Indices.Length == _dimensions.Length);
            UInt64 result = _elementSize;
            for (Int32 index = 0; index < _dimensions.Length; index += 1) {
                if (range.Indices[index] == null) {
                    result *= _dimensions[index];
                }
            }
            return result;
        }

        internal Object StartPrepareRange(Task task, DataRange range, DataAccess access, DeviceMemorySpace space) {
            if (_disposed) {
                throw new ObjectDisposedException(typeof(Data).FullName);
            }
            Debug.Assert(WaitingTasks.Count > 0);
            Debug.Assert(WaitingTasks[0].Task == task);
            if (!IsRangeValid(range)) {
                throw new ArgumentOutOfRangeException();
            }
            List<Object> rangeConditions = new List<Object>();
            Boolean writing = (access & DataAccess.Write) == DataAccess.Write;
            Boolean oneWasDirty = false;
            foreach (DataRangeCopy copyStuckInClusore in DataRangeCopies) {
                DataRangeCopy copy = copyStuckInClusore;
                if (range.Intersects(copy.Range)) {
                    List<Object> predecessorConditions = new List<Object>();
                    // must wait for all conflicting reads/writes
                    if (copy.WritingTask != null) {
                        predecessorConditions.Add(copy.WritingTask);
                    }
                    if (writing) {
                        foreach (Task readingTask in copy.ReadingTasks) {
                            predecessorConditions.Add(readingTask);
                        }
                    }
                    Object rangeCondition = new Object();
                    DependencyManager.RegisterOperation(rangeCondition);
                    DependencyManager.RegisterContinuation(predecessorConditions, () => {
                        // all conflicting tasks in this copy finished
                        Debug.Assert(copy.WritingTask == null);
                        if (writing) {
                            Debug.Assert(copy.ReadingTasks.Count == 0);
                        }
                        if (range == copy.Range && space == copy.DeviceMemorySpace) {
                            // current copy will be the one used for the task
                            DependencyManager.FinishOperationLocked(rangeCondition);
                        } else {
                            Object copyOrDeleteCondition = null;
                            if (copy.Dirty) {
                                // transfer bytes to host
                                Object readCondition = StartReadBufferRange(copy.Buffer, copy.Range, copy.DeviceMemorySpace.CommandQueue);
                                copyOrDeleteCondition = DependencyManager.ContinueOneWith(readCondition, () => {
                                    copy.Dirty = false;
                                });
                                oneWasDirty = true;
                            }
                            if (writing) {
                                copyOrDeleteCondition = DependencyManager.ContinueOneWith(copyOrDeleteCondition, () => {
                                    // copying is not necessary or is finished
                                    Debug.Assert(copy.Dirty == false);
                                    copy.Buffer.Dispose();
                                    Debug.Assert(DataRangeCopies.Remove(copy));
                                });
                            }
                            DependencyManager.RegisterOneContinuation(copyOrDeleteCondition, () => {
                                DependencyManager.FinishOperationLocked(rangeCondition);
                            });
                        }
                    });
                    rangeConditions.Add(rangeCondition);
                }
            }
            // dependencies of all conflicting ranges are in rangeConditions
            Object waitFinishedCondition = WaitingTasks[0].Condition;
            DependencyManager.RegisterContinuation(rangeConditions, () => {
                // now there should not be any conflicting ranges anymore
                // try to find the matching copy
                DataRangeCopy copy = FindDataRangeCopy(range, space);
                Object transferCondition = null;
                if (oneWasDirty || copy == null) {
                    if (copy == null) {
                        Buffer buffer = ComputeManager.Context.CreateBuffer(GetRangeSize(range), BufferFlags.ReadWrite);
                        copy = new DataRangeCopy(buffer, space, range);
                        DataRangeCopies.Add(copy);
                    }
                    // transfer host data to buffer
                    transferCondition = StartWriteBufferRange(copy.Buffer, range, space.CommandQueue);
                }
                DependencyManager.RegisterOneContinuation(transferCondition, () => {
                    // range was transferred to buffer or no transfer was necessary
                    Debug.Assert(WaitingTasks.Count > 0);
                    Debug.Assert(WaitingTasks[0].Task == task);
                    Debug.Assert(copy.WritingTask == null);
                    if (writing) {
                        copy.WritingTask = task;
                        copy.Dirty = true;
                    } else {
                        copy.ReadingTasks.Add(task);
                    }
                    WaitingTasks.RemoveAt(0);
                    DependencyManager.FinishOperationLocked(waitFinishedCondition);
                });
            });
            return waitFinishedCondition;
        }

        DataRangeCopy FindDataRangeCopy(DataRange range, DeviceMemorySpace space) {
            DataRangeCopy copy = null;
            foreach (DataRangeCopy otherCopy in DataRangeCopies) {
                if (range == otherCopy.Range && space == otherCopy.DeviceMemorySpace) {
                    copy = otherCopy;
                    break; // i hate that thing
                }
            }
            return copy;
        }

        struct CollapsedDimension {
            public Int64 Length { get; private set; }
            public Int64 IndexStart { get; private set; }
            public Int64 IndexLength { get; private set; }

            public CollapsedDimension(Int64 length, Int64 indexStart, Int64 indexLength)
                : this() {
                Debug.Assert(length > 0);
                Debug.Assert(indexStart >= 0);
                Debug.Assert(indexLength > 0);
                Debug.Assert(indexStart + indexLength <= length);
                Length = length;
                IndexStart = indexStart;
                IndexLength = indexLength;
            }

            public CollapsedDimension(Int64 length) : this(length, 0, length) { }

            public Boolean IsComplete { get { return IndexStart == 0 && IndexLength == Length; } }

            public Boolean IsSingle { get { return IndexLength == 1; } }
        }

        List<CollapsedDimension> CollapseRange(DataRange range) {
            List<CollapsedDimension> result = new List<CollapsedDimension>();
            Debug.Assert(range.Indices.Length == _dimensions.Length);
            for (Int32 index = 0; index < _dimensions.Length; index += 1) {
                if (range.Indices[index] != null) {
                    result.Add(new CollapsedDimension((Int64)_dimensions[index], (Int64)range.Indices[index].Value, 1));
                } else {
                    result.Add(new CollapsedDimension((Int64)_dimensions[index]));
                }
            }
            // treat element size as last dimension - will be collapsed with lower one afterwards
            result.Add(new CollapsedDimension((Int64)_elementSize));
            for (Int32 index = result.Count - 2; index >= 0; index -= 1) {
                CollapsedDimension lower = result[index];
                CollapsedDimension higher = result[index + 1];
                if (higher.IsComplete) {
                    // higher indexes all elements
                    if (lower.IsComplete) {
                        // and lower too
                        result[index] = new CollapsedDimension(lower.Length * higher.Length);
                    } else {
                        // but lower covers a range or single index
                        result[index] = new CollapsedDimension(lower.Length * higher.Length, lower.IndexStart * higher.Length, lower.IndexLength * higher.Length);
                    }
                    result.RemoveAt(index + 1);
                } else if (higher.IsSingle && lower.IsSingle) {
                    // both consist of a single index
                    result[index] = new CollapsedDimension(lower.Length * higher.Length, lower.IndexStart * higher.Length + higher.IndexStart, 1);
                    result.RemoveAt(index + 1);
                }
            }
            return result;
        }

        internal Object StartReadBufferRange(Buffer buffer, DataRange range, CommandQueue queue) {
            //Console.WriteLine("StartReadBufferRange");
            List<CollapsedDimension> collapsedDimensions = CollapseRange(range);
            EventObject eventObject;
            if (collapsedDimensions.Count == 1) {
                eventObject = queue.StartReadBuffer(buffer, _hostArray, collapsedDimensions[0].IndexStart, collapsedDimensions[0].IndexLength);
            } else if (collapsedDimensions.Count == 2 || collapsedDimensions.Count == 3) {
                if (collapsedDimensions.Count == 2) {
                    collapsedDimensions.Insert(0, new CollapsedDimension(1));
                }
                eventObject = queue.StartReadBufferRect(
                    buffer,
                    _hostArray,
                    new IntPtr(collapsedDimensions[2].IndexStart),
                    new IntPtr(collapsedDimensions[1].IndexStart),
                    new IntPtr(collapsedDimensions[0].IndexStart),
                    new IntPtr(collapsedDimensions[2].IndexLength),
                    new IntPtr(collapsedDimensions[1].IndexLength),
                    new IntPtr(collapsedDimensions[0].IndexLength),
                    new IntPtr(collapsedDimensions[2].Length),
                    new IntPtr(collapsedDimensions[2].Length * collapsedDimensions[1].Length)
                );
            } else {
                // not supported yet
                throw new NotSupportedException();
            }
            queue.Flush();
            Object condition = new Object();
            DependencyManager.RegisterOperation(condition);
            eventObject.RegisterCompletionCallback(e => {
                DependencyManager.FinishOperation(condition);
                eventObject.Dispose();
            });
            //Console.WriteLine("StartReadBufferRange finished");
            return condition;
        }

        internal Object StartWriteBufferRange(Buffer buffer, DataRange range, CommandQueue queue) {
            //Console.WriteLine("StartWriteBufferRange");
            List<CollapsedDimension> collapsedDimensions = CollapseRange(range);
            EventObject eventObject;
            if (collapsedDimensions.Count == 1) {
                eventObject = queue.StartWriteBuffer(_hostArray, collapsedDimensions[0].IndexStart, collapsedDimensions[0].IndexLength, buffer);
            } else if (collapsedDimensions.Count == 2 || collapsedDimensions.Count == 3) {
                if (collapsedDimensions.Count == 2) {
                    collapsedDimensions.Insert(0, new CollapsedDimension(1));
                }
                eventObject = queue.StartWriteBufferRect(
                    _hostArray,
                    buffer,
                    new IntPtr(collapsedDimensions[2].IndexStart),
                    new IntPtr(collapsedDimensions[1].IndexStart),
                    new IntPtr(collapsedDimensions[0].IndexStart),
                    new IntPtr(collapsedDimensions[2].IndexLength),
                    new IntPtr(collapsedDimensions[1].IndexLength),
                    new IntPtr(collapsedDimensions[0].IndexLength),
                    new IntPtr(collapsedDimensions[2].Length),
                    new IntPtr(collapsedDimensions[2].Length * collapsedDimensions[1].Length)
                );
            } else {
                // not supported yet
                throw new NotSupportedException();
            }
            queue.Flush();
            Object condition = new Object();
            DependencyManager.RegisterOperation(condition);
            eventObject.RegisterCompletionCallback(e => {
                DependencyManager.FinishOperation(condition);
                eventObject.Dispose();
            });
            //Console.WriteLine("StartWriteBufferRange finished");
            return condition;
        }

        public Array GetHostArray() {
            ComputeManager.Mutex.Enter();
            List<Object> taskDependencies = new List<Object>();
            // gather all tasks currently active on data and wait for them
            foreach (DataRangeCopy copy in DataRangeCopies) {
                if (copy.WritingTask != null) {
                    taskDependencies.Add(copy.WritingTask);
                }
                foreach (Task readingTask in copy.ReadingTasks) {
                    taskDependencies.Add(readingTask);
                }
            }
            foreach (TaskDataCondition waitingTask in WaitingTasks) {
                taskDependencies.Add(waitingTask.Task);
            }
            AutoResetEvent e = new AutoResetEvent(false);
            if (taskDependencies.Count > 0) {
                DependencyManager.RegisterContinuation(taskDependencies, () => e.Set());
                ComputeManager.Mutex.Exit();
                e.WaitOne();
                ComputeManager.Mutex.Enter();
            }
            // all tasks in all copies and all waiting tasks finished now. must transfer all dirty copies
            List<Object> rangeDependencies = new List<Object>();
            foreach (DataRangeCopy copy in DataRangeCopies) {
                Debug.Assert(copy.WritingTask == null);
                Debug.Assert(copy.ReadingTasks.Count == 0);
                if (copy.Dirty) {
                    rangeDependencies.Add(StartReadBufferRange(copy.Buffer, copy.Range, copy.DeviceMemorySpace.CommandQueue));
                }
            }
            if (rangeDependencies.Count > 0) {
                DependencyManager.RegisterContinuation(rangeDependencies, () => e.Set());
                ComputeManager.Mutex.Exit();
                e.WaitOne();
                ComputeManager.Mutex.Enter();
            }
            // all dirty ranges are now transferred back to host
            // must remove all copies since host array might get modified
            foreach (DataRangeCopy copy in DataRangeCopies) {
                copy.Buffer.Dispose();
            }
            DataRangeCopies.Clear();
            ComputeManager.Mutex.Exit();
            return _hostArray;
        }

        internal void FinishTask(Task task, DataRange range, DataAccess access, DeviceMemorySpace space) {
            DataRangeCopy copy = FindDataRangeCopy(range, space);
            Debug.Assert(copy != null);
            if ((access & DataAccess.Write) == DataAccess.Write) {
                Debug.Assert(copy.WritingTask == task);
                copy.WritingTask = null;
            } else {
                Debug.Assert(copy.ReadingTasks.Remove(task));
            }
        }
    }
}
