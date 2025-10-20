using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSlibrary
{
    public class Processor
    {
        public static double[,] Transpose(double[,] A)
        {
            if (A != null) {
                int m = A.GetLength(0);
                int n = A.GetLength(1);
                var AT = new double[n, m];
                for (int i = 0; i < m; ++i)
                {
                    for (int j = 0; j < n; ++j)
                    {
                        AT[j, i] = A[i, j];
                    }
                }
                return AT;
            }
            else return null;
        }

        public static double[,] Multiply(double[,] A, double[,] B)
        {

            int aRows = A.GetLength(0);
            int aCols = A.GetLength(1);
            int bRows = B.GetLength(0);
            int bCols = B.GetLength(1);

            if (aCols != bRows) return null;

            var C = new double[aRows, bCols];

            for (int i = 0; i < aRows; ++i)
            {
                for (int k = 0; k < aCols; ++k)
                {
                    double aik = A[i, k];
                    if (aik == 0.0) continue;
                    for (int j = 0; j < bCols; ++j)
                    {
                        C[i, j] += aik * B[k, j];
                    }
                }
            }

            return C;
        }

        public static double[] Multiply(double[,] A, double[] B)
        {

            int aRows = A.GetLength(0);
            int aCols = A.GetLength(1);

            if (aCols != B.GetLength(0)) return null;

            var C = new double[aRows];

            for (int i = 0; i < aRows; ++i)
            {
                for (int k = 0; k < aCols; ++k)
                {
                    C[i] += A[i, k] * B[k];
                }
            }

            return C;
        }
    }
}
