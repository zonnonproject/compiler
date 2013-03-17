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
    public class ColSPA<T> : SPA<T>
    {
        private SparseMatrix<T> A;

        public ColSPA(SparseMatrix<T> A, int index)
        {
            this.A = A;
            Flags = new bool[A.M];
            this.Index = index;
        }

        public override sealed int Index
        {
            get { return index; }
            set
            {
                index = value;

                if ((index < 0) || (index >= A.N)) throw new System.IndexOutOfRangeException(
                    "OutOfRange exception: illegal index");

                Func<int, bool> compareWithInt = (i => i == index);
                int realLength = A.JA.Count<int>(compareWithInt);

                Data = new T[realLength];
                Indices = new int[realLength];

                int curJIndex = 0;
                int curIIndex = -1;
                for (int i = 0; i < realLength; i++)
                {
                    while (A.JA[curJIndex] != index)
                    {
                        curJIndex++;
                    }

                    while (A.IA[curIIndex + 1] <= curJIndex) curIIndex++;

                    Data[i] = A.AN[curJIndex++];
                    Indices[i] = curIIndex;
                    Flags[Indices[i]] = true;
                }
            }
        }
    }
}
