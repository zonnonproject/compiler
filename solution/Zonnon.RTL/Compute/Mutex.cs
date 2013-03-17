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
    using System.Threading;

    public sealed class Mutex {
        public Object _lock = new Object();
        public Boolean _taken;

        public void Enter() {
            Monitor.Enter(_lock);
            if (_taken) {
                throw new LockRecursionException();
            }
            _taken = true;
        }

        public void Exit() {
            _taken = false;
            Monitor.Exit(_lock);
        }

        public void Wait() {
            _taken = false;
            Monitor.Wait(_lock);
            _taken = true;
        }

        public void Pulse() {
            _taken = false;
            Monitor.Pulse(_lock);
            _taken = true;
        }
    }
}
