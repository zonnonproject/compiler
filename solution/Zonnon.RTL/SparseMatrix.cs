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
    #region Sparse Matrix
    public class SparseMatrix<T>
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

        public int[] IA
        {
            get;
            private set;
        }

        private int curCountOfElems;

        public int M
        { get; private set; }

        public int N
        { get; private set; }

        public int GetLength(int dim)
        {
            if (dim == 0) return M;
            if (dim == 1) return N;
            throw new IndexOutOfRangeException();
        }

        /// <summary>
        /// Additional information about i, j, s, m, n, nzmax you can find here:
        /// SPARSE MATRICES IN MATLAB: DESIGN AND IMPLEMENTATION by JOHN R. GILBERT, CLEVE MOLER, ROBERT SCHREIBER
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <param name="colNumber"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="s"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="nzmax"></param>
        /// <returns></returns>
        protected void construct(long lineNumber, int colNumber,
            int[] i, int[] j, T[] s, int m, int n, int nzmax)
        {
            int iLength = i.Length;
            int jLength = j.Length;
            int sLength = s.Length;

            if (iLength != jLength) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Lengths of indices vectors are not equal");
            if (iLength != sLength) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Lengths of index vector i and value vector are not equal");
            if (nzmax < sLength) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Maximal number of elements in sparse matrix is less than length of value vector");
            if (nzmax > m * n) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Maximal number of elements in sparse matrix is greater than its number of elements according to the sizes");

            Array i_copy = new int[iLength];
            Array.Copy(i, i_copy, iLength);
            Array.Sort(i_copy, j);
            Array.Sort(i, s);

            AN = new List<T>(s);
            JA = new List<int>(j);

            //if (nzmax >= sLength)
            //{
            //    for (int k = jLength; k < nzmax; k++)
            //        JA.Add(-1);
            //}

            curCountOfElems = sLength;

            this.M = m;
            this.N = n;

            if (i[0] < 0) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Incorrect i vector index values; they should be >= 0");
            if (i[iLength - 1] >= m) throw new Exception(lineNumber.ToString() + ", " + colNumber.ToString() +
                "Incorrect i vector index values; they should be < M");

            IA = new int[m + 1];

            int iaLength = 0;
            int jaLength = 0;

            for (int curIIndex = 0; iaLength < IA.Length; )
            {
                IA[iaLength] = curIIndex;

                int curINextIndex = curIIndex;
                while ((curINextIndex < iLength) && (iaLength == i[curINextIndex]))
                    curINextIndex++;

                iaLength++;

                for (int k = curIIndex; k < curINextIndex; k++)
                    JA[jaLength++] = j[k];

                curIIndex = curINextIndex;
            }
        }

        protected SparseMatrix() { }

        public SparseMatrix(long lineNumber, int colNumber,
            int[] i, int[] j, T[] s, int m, int n, int nzmax)
        {
            construct(lineNumber, colNumber, i, j, s, m, n, nzmax);
        }

        public SparseMatrix(long lineNumber, int colNumber,
            int[] i, int[] j, T[] s, int m, int n)
        {
            construct(lineNumber, colNumber, i, j, s, m, n, s.Length);
        }

        public SparseMatrix(long lineNumber, int colNumber,
            int[] i, int[] j, T[] s)
        {
            construct(lineNumber, colNumber, i, j, s,
                i.Max<int>() + 1, j.Max<int>() + 1, s.Length);
        }

        public SparseMatrix(long lineNumber, int colNumber,
            int m, int n)
        {
            this.M = m;
            this.N = n;
            AN = new List<T>();
            IA = new int[m + 1];
            JA = new List<int>();
        }

        public SparseMatrix(long lineNumber, int colNumber,
            T[,] denseMatrix, T zero)
        {
            M = denseMatrix.GetLength(0);
            N = denseMatrix.GetLength(1);
            Zero = zero;
            curCountOfElems = 0;

            IA = new int[M + 1];
            List<int> JAList = new List<int>();
            List<T> ANList = new List<T>();

            int iaCurIndex = 0;
            IA[0] = 0;

            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    if (!isZero(denseMatrix[i, j]))
                    {
                        JAList.Add(j);
                        ANList.Add(denseMatrix[i, j]);
                        iaCurIndex++;
                        curCountOfElems++;
                    }
                }
                IA[i + 1] = iaCurIndex;
            }

            JA = JAList;
            AN = ANList;
        }

        public SparseMatrix(long lineNumber, int colNumber,
            SparseMatrix<T> sm)
        {
            M = sm.M;
            N = sm.N;

            IA = new int[sm.IA.Length];

            curCountOfElems = sm.curCountOfElems;

            Array.Copy(sm.IA, IA, IA.Length);
            JA = new List<int>(sm.JA);
            AN = new List<T>(sm.AN);
        }

        public T[,] ToDenseMatrix(long lineNumber, int colNumber)
        {
            T[,] res = new T[M, N];

            for (int i = 0; i < M; i++)
            {
                for (int j = IA[i]; j < IA[i + 1]; j++)
                    res[i, JA[j]] = AN[j];
            }

            return res;
        }

        public T Zero
        {
            get;
            set;
        }

        protected virtual bool isZero(T elem)
        {
            return (((IComparable)elem).CompareTo(Zero) == 0);
        }

        public T GetElem(long lineNumber, int colNumber, int index1, int index2)
        {
            if ((index1 < 0) || (index2 < 0)) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));
            if ((index1 >= M) || (index2 >= N)) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));

            int j;
            for (j = IA[index1]; j < IA[index1 + 1]; j++)
                if (JA[j] == index2) break;
            if (j < IA[index1 + 1]) return AN[j];
            return Zero;
        }

        public void SetElem(long lineNumber, int colNumber, int index1, int index2, T elem)
        {
            if ((index1 < 0) || (index2 < 0)) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));
            if ((index1 >= M) || (index2 >= N)) throw new System.IndexOutOfRangeException(
                (lineNumber.ToString() + ", " + colNumber.ToString() + " OutOfRange exception: illegal index"));

            int j;
            for (j = IA[index1]; j < IA[index1 + 1]; j++)
                if (JA[j] == index2) break;
            if (j < IA[index1 + 1])
            {
                if (!isZero(elem))
                    AN[j] = elem;
                else
                {
                    for (int k = j; k < curCountOfElems - 1; k++)
                    {
                        AN[k] = AN[k + 1];
                        JA[k] = JA[k + 1];
                    }
                    JA[--curCountOfElems] = -1;
                    for (int k = index1; k < IA.Length; k++)
                    {
                        IA[k] -= 1;
                    }
                    IA[0] = 0;
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

                    for (int k = index1 + 1; k < IA.Length; k++)
                    {
                        IA[k] += 1;
                    }
                    for (int k = curCountOfElems - 1; k >= IA[index1 + 1] - 1; k--)
                    {
                        AN[k + 1] = AN[k];
                        JA[k + 1] = JA[k];
                    }
                    AN[IA[index1 + 1] - 1] = elem;
                    JA[IA[index1 + 1] - 1] = index2;
                    curCountOfElems++;
                }
            }
        }

        public SparseMatrix<T> Transpose(long lineNumber, int colNumber)
        {
            SparseMatrix<T> res = new SparseMatrix<T>(lineNumber, colNumber, N, M);
            ColSPA<T> colSPA;

            for (int i = 0; i < N; i++)
            {
                colSPA = new ColSPA<T>(this, i);
                res.AssignSPAToRow(i, colSPA);
            }
            return res;
        }

        public void AssignSPAToRow(int rowIndex, SPA<T> spa)
        {
            if (spa.Length != N) throw new Exception("Incorrect SPA langth; impossible to assign to a matrix row");

            int prevLength = IA[rowIndex + 1] - IA[rowIndex];

            if (IA[rowIndex] < AN.Count)
            {
                AN.RemoveRange(IA[rowIndex], prevLength);
                JA.RemoveRange(IA[rowIndex], prevLength);
            }

            AN.InsertRange(IA[rowIndex], spa.Data);
            JA.InsertRange(IA[rowIndex], spa.Indices);

            int diff = prevLength - spa.RealLength;
            if (diff != 0)
                for (int i = rowIndex + 1; i < IA.Length; i++)
                    IA[i] -= diff;
        }
    }
    #endregion
}
