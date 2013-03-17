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

    public static class ComputeHelper {
        public static Boolean AreDimensionsEqual(UInt64[] dimensions1, UInt64[] dimensions2) {
            Int64 length = dimensions1.LongLength;
            if (dimensions2.LongLength != length) {
                return false;
            }
            Int32 index = 0;
            while (index < length) {
                if (dimensions1[index] != dimensions2[index]) {
                    return false;
                }
                index++;
            }
            return true;
        }

        public static UInt64[] GetDimensions(Array array) {
            UInt64[] result = new UInt64[array.Rank];
            for (int dimension = 0; dimension < array.Rank; dimension += 1) {
                result[dimension] = (UInt64)array.GetLength(dimension);
            }
            return result;
        }
    }
}
