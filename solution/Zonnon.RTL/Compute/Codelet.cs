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
    using Buffer = Zonnon.RTL.OpenCL.Wrapper.Buffer;

    public struct Codelet {
        public Type DeviceType { get; private set; }
        public Delegate Start { get; private set; }
        public Delegate GetExpectedTime { get; private set; }

        public Codelet(Type deviceType, Delegate getExpectedtime, Delegate start)
            : this() {
            DeviceType = deviceType;
            Start = start;
            GetExpectedTime = getExpectedtime;
        }
    }
}
