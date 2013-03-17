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
    using System.Threading;

    class Dependency {
        public Int32 Count { get; set; }
        public Action Callback { get; private set; }

        public Dependency(Int32 count, Action callback) {
            Count = count;
            Callback = callback;
        }
    }

    public static class DependencyManager {
        static Dictionary<Object, List<Dependency>> _operations = new Dictionary<Object, List<Dependency>>();
        static Queue<Action> _readyQueue = new Queue<Action>();

        internal static void Run() {
            ComputeManager.Mutex.Enter();
            while (true) {
                if (_readyQueue.Count > 0) {
                    // dequeue work and execute it
                    //Console.WriteLine("executing continuation");
                    _readyQueue.Dequeue()();
                } else {
                    ComputeManager.Mutex.Wait();
                    //Console.WriteLine("got pulse");
                }
            }
#pragma warning disable 0162
            ComputeManager.Mutex.Exit();
#pragma warning restore 0162
        }

        // must be called locked to ComputeManager.Mutex
        public static void RegisterOperation(Object operation) {
            _operations.Add(operation, new List<Dependency>());
        }

        // must be called locked to ComputeManager.Mutex
        public static void RegisterContinuation(IEnumerable<Object> operations, Action callback) {
            Dependency dependency = new Dependency(0, callback);
            foreach (Object operation in operations) {
                List<Dependency> operationDependencies;
                if (operation != null && _operations.TryGetValue(operation, out operationDependencies)) {
                    operationDependencies.Add(dependency);
                    dependency.Count += 1;
                }
            }
            if (dependency.Count == 0) {
                // no dependency or all predecessors already finished
                _readyQueue.Enqueue(callback);
                ComputeManager.Mutex.Pulse();
            }
        }

        // must be called locked to ComputeManager.Mutex
        public static void RegisterOneContinuation(Object operation, Action callback) {
            Dependency dependency = new Dependency(0, callback);
            List<Dependency> operationDependencies;
            if (operation != null && _operations.TryGetValue(operation, out operationDependencies)) {
                operationDependencies.Add(dependency);
                dependency.Count += 1;
            }
            if (dependency.Count == 0) {
                // no dependency or all predecessors already finished
                _readyQueue.Enqueue(callback);
                ComputeManager.Mutex.Pulse();
            }
        }

        // must be called locked to ComputeManager.Mutex
        public static Object ContinueWith(IEnumerable<Object> conditions, Action action) {
            Object newCondition = new Object();
            RegisterOperation(newCondition);
            RegisterContinuation(conditions, () => {
                action();
                FinishOperationLocked(newCondition);
            });
            return newCondition;
        }

        // must be called locked to ComputeManager.Mutex
        public static Object ContinueOneWith(Object condition, Action action) {
            Object newCondition = new Object();
            RegisterOperation(newCondition);
            RegisterOneContinuation(condition, () => {
                action();
                FinishOperationLocked(newCondition);
            });
            return newCondition;
        }

        public static void FinishOperationLocked(Object operation) {
            //Console.WriteLine("finish operation locked");
            Debug.Assert(operation != null);
            Boolean queued = false;
            List<Dependency> dependencies = _operations[operation];
            foreach (Dependency dependency in dependencies) {
                dependency.Count -= 1;
                if (dependency.Count == 0) {
                    // all predecessors of a continuation finished
                    _readyQueue.Enqueue(dependency.Callback);
                    queued = true;
                    //Console.WriteLine("queued completion");
                }
            }
            _operations.Remove(operation);
            if (queued) {
                //Console.WriteLine("pulse");
                ComputeManager.Mutex.Pulse();
            }
        }

        public static void FinishOperation(Object operation) {
            //Console.WriteLine("finish operation");
            ComputeManager.Mutex.Enter();
            FinishOperationLocked(operation);
            ComputeManager.Mutex.Exit();
        }
    }
}
