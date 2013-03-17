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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zonnon.RTL
{
    public abstract class SPA<T>
    {
        protected int index;

        public abstract int Index
        {
            get;
            set;
        }

        public T[] Data
        {
            get;
            protected set;
        }

        public bool[] Flags
        {
            get;
            protected set;
        }

        public int[] Indices
        {
            get;
            protected set;
        }

        public int Length
        {
            get { return Flags.Length; }
        }

        public int RealLength
        {
            get { return Data.Length; }
        }

        public int IndexOfElemInIndices(int elem)
        {
            return Array.IndexOf(this.Indices, elem);
        }
    }
}
