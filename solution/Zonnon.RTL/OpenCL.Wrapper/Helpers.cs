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
    using System.Collections.Generic;

    static class Helpers {
        public static Boolean TryGetEventHandles(EventObject[] events, out IntPtr[] handles, out Int32 handlesLength) {
            List<IntPtr> handlesList = null;
            if (events != null) {
                foreach (EventObject e in events) {
                    if (e != null) {
                        if (handlesList == null) {
                            handlesList = new List<IntPtr>();
                        }
                        handlesList.Add(e.Handle);
                    }
                }
            }
            if(handlesList != null) {
                handles = handlesList.ToArray();
                handlesLength = handlesList.Count;
            } else {
                handles = null;
                handlesLength = 0;
            }
            return true;
        }
        //public static Boolean TryGetEventHandles(EventObject[] events, out IntPtr[] handles, out Int32 handlesLength) {
        //    Boolean result = true;
        //    if (events != null && events.Length > 0) {
        //        handles = new IntPtr[events.Length];
        //        Int32 index = 0;
        //        while (result == true && index < events.Length) {
        //            if (events[index] != null) {
        //                handles[index] = events[index].Handle;
        //            } else {
        //                result = false;
        //            }
        //            index += 1;
        //        }
        //        handlesLength = index;
        //    } else {
        //        handles = null;
        //        handlesLength = 0;
        //    }
        //    return result;
        //}
    }
}
