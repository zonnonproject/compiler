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
using System.IO;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zonnon.RTL
{
    #region Sparse Vector
    public class SparseVector<T>
    {
        public List<T> AN
        {
            get;
            private set;
        }

        public List<int> JA
        {
            get;
            private set;
        }

        private int curCountOfElems;

        public int N
        { get; private set; }

        public int GetLength(int dim)
        {
            if (dim == 0) return N;
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Additional information about j, s, n, nzmax you can find here:
        /// SPARSE MATRICES IN MATLAB: DESIGN AND IMPLEMENTATION by JOHN R. GILBERT, CLEVE MOLER, ROBERT SCHREIBER
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="columnNumber"></param>
        /// <param name="j"></param>
        /// <param name="s"></param>
        /// <param name="n"></param>
        /// <param name="nzmax"></param>
        /// <returns></returns>
        protected void construct(long lineNumber, int colNumber,
            int[] j, T[] s, int n, int nzmax)
        {
            int jLength = j.Length;
            int sLength = s.Length;

            if (jLength != sLength) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Lengths of index vector j and value vector are not equal");
            if (nzmax < sLength) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Maximal number of elements in sparse vector is less than length of value vector");
            if (nzmax > n) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Maximal number of elements in sparse vector is greater than its number of elements according to the size");

            AN = new List<T>(s);
            JA = new List<int>(j);

            curCountOfElems = sLength;

            this.N = n;

            if (j[0] < 0) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Incorrect j vector index values; they should be >= 0");
            if (j[jLength - 1] >= n) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Incorrect j vector index values; they should be < N");
        }

        protected SparseVector() { }

        public SparseVector(long lineNumber, int colNumber,
            int[] j, T[] s, int n, int nzmax)
        {
            construct(lineNumber, colNumber, j, s, n, nzmax);
        }

        public SparseVector(long lineNumber, int colNumber,
            int[] j, T[] s, int n)
        {
            construct(lineNumber, colNumber, j, s, n, s.Length);
        }

        public SparseVector(long lineNumber, int colNumber,
            int[] j, T[] s)
        {
            construct(lineNumber, colNumber, j, s,
                j.Max<int>() + 1, s.Length);
        }

        public SparseVector(long lineNumber, int colNumber,
            int n)
        {
            this.N = n;
            AN = new List<T>();
            JA = new List<int>();
        }

        public SparseVector(long lineNumber, int colNumber,
            T[] denseVector, T zero)
        {
            N = denseVector.GetLength(0);
            curCountOfElems = 0;
            Zero = zero;

            List<int> JAList = new List<int>();
            List<T> ANList = new List<T>();

            for (int j = 0; j < N; j++)
            {
                if (!isZero(denseVector[j]))
                {
                    JAList.Add(j);
                    ANList.Add(denseVector[j]);
                    curCountOfElems++;
                }
            }

            JA = JAList;
            AN = ANList;
        }

        public SparseVector(long lineNumber, int colNumber,
            SparseVector<T> sv)
        {
            N = sv.N;

            curCountOfElems = sv.curCountOfElems;

            JA = new List<int>(sv.JA);
            AN = new List<T>(sv.AN);
        }

        public T[] ToDenseVector(long lineNumber, int colNumber)
        {
            T[] res = new T[N];

            for (int j = 0; j < curCountOfElems; j++)
                res[JA[j]] = AN[j];

            return res;
        }

        public T Zero
        {
            get;
            set;
        }

        protected bool isZero(T elem)
        {
            return (((IComparable)elem).CompareTo(Zero) == 0);
        }

        public T GetElem(long lineNumber, int colNumber, int index)
        {
            if (index < 0) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));
            if (index >= N) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));

            int i = JA.IndexOf(index);
            if (i != -1) return AN[i];
            return Zero;
        }

        public void SetElem(long lineNumber, int colNumber, int index, T elem)
        {
            if (index < 0) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));
            if (index >= N) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));

            int i = JA.IndexOf(index);
            if (i != -1)
            {
                if (!isZero(elem))
                    AN[i] = elem;
                else
                {
                    for (int j = i; j < curCountOfElems - 1; j++)
                    {
                        AN[j] = AN[j + 1];
                        JA[j] = JA[j + 1];
                    }
                    JA[--curCountOfElems] = -1;
                }
            }
            else
            {
                if (!isZero(elem))
                {
                    if (curCountOfElems == AN.Count)
                    {
                        AN.Add(elem);
                        JA.Add(-1);
                    }

                    AN[curCountOfElems] = elem;
                    JA[curCountOfElems] = index;
                    curCountOfElems++;
                }
            }
        }
    }
    #endregion
}
