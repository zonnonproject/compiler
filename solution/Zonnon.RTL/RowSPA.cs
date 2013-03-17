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
    public class RowSPA<T> : SPA<T>
    {
        private SparseMatrix<T> A;

        public RowSPA(SparseMatrix<T> A, int index)
        {
            this.A = A;
            Flags = new bool[A.N];
            this.Index = index;
        }

        public override sealed int Index
        {
            get { return index; }
            set
            {
                index = value;

                if ((index < 0) || (index >= A.M)) throw new System.IndexOutOfRangeException(
                    "OutOfRange exception: illegal index");

                int realLength = A.IA[index + 1] - A.IA[index];

                Data = new T[realLength];
                Indices = new int[realLength];

                for (int i = 0, curIndex = A.IA[index]; i < realLength; i++, curIndex++)
                {
                    Data[i] = A.AN[curIndex];
                    Indices[i] = A.JA[curIndex];
                    Flags[Indices[i]] = true;
                }
            }
        }
    }
}
