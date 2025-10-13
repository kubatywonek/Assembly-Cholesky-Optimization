using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSlibrary
{
    public class Processor
    {
        public static int tescik()
        {
            return 1;
        }
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

        public static double[,] Inverse(double[,] A)
        {
            if (A is null) throw new ArgumentNullException(nameof(A));
            int n = A.GetLength(0);
            if (A.GetLength(1) != n) throw new ArgumentException("Macierz musi być kwadratowa.");

            // macierz rozszerzona [A | I]
            var aug = new double[n, 2 * n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                    aug[i, j] = A[i, j];
                for (int j = 0; j < n; ++j)
                    aug[i, n + j] = (i == j) ? 1.0 : 0.0;
            }

            const double EPS = 1e-15; // tolerancja 
            for (int col = 0; col < n; ++col)
            {
                // szukanie max elementu w kolumnie
                int pivotRow = col;
                double maxAbs = Math.Abs(aug[col, col]);
                for (int r = col + 1; r < n; ++r)
                {
                    double val = Math.Abs(aug[r, col]);
                    if (val > maxAbs)
                    {
                        maxAbs = val;
                        pivotRow = r;
                    }
                }

                if (maxAbs < EPS)
                    throw new InvalidOperationException("Macierz jest osobliwa lub blisko osobliwa. Nie można obliczyć odwrotności.");

                if (pivotRow != col)
                {
                    for (int c = 0; c < 2 * n; ++c)
                    {
                        double tmp = aug[col, c];
                        aug[col, c] = aug[pivotRow, c];
                        aug[pivotRow, c] = tmp;
                    }
                }

                // dzielenie przez pivot
                double pivot = aug[col, col];
                for (int c = 0; c < 2 * n; ++c)
                    aug[col, c] /= pivot;

                // redukcja pozostałych wierszy
                for (int r = 0; r < n; ++r)
                {
                    if (r == col) continue;
                    double factor = aug[r, col];
                    if (factor == 0.0) continue;
                    for (int c = col; c < 2 * n; ++c)
                        aug[r, c] -= factor * aug[col, c];
                }
            }

            // macierz odwrotna -> [I | A^-1]
            var inv = new double[n, n];
            for (int i = 0; i < n; ++i)
                for (int j = 0; j < n; ++j)
                    inv[i, j] = aug[i, n + j];

            return inv;
        }
    }
}
