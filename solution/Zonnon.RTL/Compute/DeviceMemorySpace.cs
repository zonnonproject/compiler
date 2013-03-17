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

    class DeviceMemorySpace {
        public Double HostTransferSpeed { get; private set; }
        public CommandQueue CommandQueue { get; private set; }

        public DeviceMemorySpace(Double hostTransferSpeed, CommandQueue commandQueue) {
            HostTransferSpeed = hostTransferSpeed;
            CommandQueue = commandQueue;
        }
    }
}
