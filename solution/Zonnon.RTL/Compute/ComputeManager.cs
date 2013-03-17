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
    using System.Threading;
    using Zonnon.RTL.OpenCL.Wrapper;
    using System.Linq;
    using System.Diagnostics;

    public static class ComputeManager {
        internal static Mutex Mutex = new Mutex();
        internal static Context Context;
        internal static Dictionary<Type, ComputeDevice[]> Devices;
        static Thread _dependencyThread;
        static Int32 _taskCount;

        static ComputeManager() {
            // get opencl devices
            Device[] devices = (
                from platform in Platform.GetPlatforms()
                from device in platform.GetDevices(DeviceType.Gpu)
                select device
            ).ToArray();
            Debug.Assert(devices.Length > 0);
            Context = new Context(devices);
            // TODO: transfer speeds
            ComputeDevice[] computeDevices = new ComputeDevice[devices.Length];
            for (Int32 index = 0; index < devices.Length; index += 1) {
                computeDevices[index] = OpenCLComputeDevice.Create(index, Context, devices[index], 1.0);
            }
            Devices = new Dictionary<Type, ComputeDevice[]>();
            Devices.Add(typeof(OpenCLComputeDevice), computeDevices);
            // TODO: CPU devices
            _dependencyThread = new Thread(DependencyManager.Run);
            _dependencyThread.IsBackground = true;
            _dependencyThread.Start();
        }

        public static void SubmitTask(Codelet[] codelets, DataUse[] uses) {
            Mutex.Enter();
            Task task = new Task(_taskCount, codelets, uses);
            _taskCount++;
            Mutex.Exit();
        }
    }
}
