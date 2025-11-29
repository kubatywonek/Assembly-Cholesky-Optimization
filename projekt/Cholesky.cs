using CSlibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace projekt
{

    internal class Cholesky
    {
        
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Transpose(double[,] src, int rows, int cols, double[,] dst);
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Multiply_Basic(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Multiply_SSE2(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Multiply_AVX(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        
        /*
        [DllImport(@"ASMlibrary.dll")]
        static extern void Transpose(double[,] src, int rows, int cols, double[,] dst);
        [DllImport(@"ASMlibrary.dll")]
        static extern void Multiply_Basic(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        [DllImport(@"ASMlibrary.dll")]
        static extern void Multiply_SSE2(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        [DllImport(@"ASMlibrary.dll")]
        static extern void Multiply_AVX(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        */
        double[,] A;
        double[] b;
        int nThreads;
        Stopwatch timer;
        static bool Tikhonov;
        public Cholesky(double[,] CoefMatrix, double[] ResVector)
        {
            Tikhonov = false;
            this.nThreads = 1;
            this.timer = new Stopwatch();
            double[,] CoefTransposed = Processor.Transpose(CoefMatrix);
            if (CoefMatrix.GetLength(0) != CoefMatrix.GetLength(1)) throw new ArgumentException("Macierz współczynników musi być kwadratowa.");
            if (CoefTransposed != null) {
                this.A = Processor.Multiply(CoefTransposed, CoefMatrix);
                this.b = Processor.Multiply(CoefTransposed, ResVector);
            }
            else throw new ArgumentException("Błąd podczas transpozycji macierzy współczynników.");
        }
        public void SetThreads(int threads) { this.nThreads = threads; }
        public long GetTime() { return timer.ElapsedTicks; }
        public double[] SolveDebug(int num){
            if(num == 0) return SolveC(4);
            else if(num == 1) return SolveSSE(2);
            else return SolveAVX(4);
        }

        public double[] Solve(bool asm, bool regularization){
            Tikhonov = regularization;
            int blockSize = setSize(A.GetLength(0), nThreads);
            double[] result = new double[b.GetLength(0)];
            if (asm)
            {
                if (blockSize == 2){
                    timer.Start();
                    result = SolveSSE(blockSize);
                    timer.Stop();
                }
                else{
                    timer.Start();
                    result = SolveAVX(blockSize);
                    timer.Stop();
                }
            }
            else{
                timer.Start();
                result = SolveC(blockSize);
                timer.Stop();
            }
            return result;
        }

        private double[] SolveC(int blockSize){
            int size = A.GetLength(0);
            double[,] L = new double[size, size];

            for (int k = 0; k < size; k += blockSize){                          // panel index = k
                int panelSize = Math.Min(blockSize, size - k);                  // rozmiar panelu (blockSize lub mniejszy na końcu macierzy)

                // 1. Panel factor -> zrefaktoryzuj panel A[k,k] (blockSize x blockSize) metodą nieblokową
                double[,] panel = GetBlock(this.A, k, k, panelSize, panelSize);
                double[,] L_kk = CholeskyUnblocked(panel);
                SetBlock(L, k, k, L_kk); // zapis

                // 2. rozwiązywanie bloków -> dla wszystkich i>pIndex (blokowo - równolegle)
                double[,] invL_kk = Inverse(L_kk);                    // inv(L_kk)
                double[,] invL_kkT = Processor.Transpose(invL_kk);              // inv(L_kk)^T == inv(L_kk^T)

                var iBlocks = new List<int>();
                for (int i = k + panelSize; i < size; i += blockSize) iBlocks.Add(i); // iBlocks: lista indeksów bloków poniżej panelu kk

                if (iBlocks.Count > 0){
                    var opts = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts, i => {              // Równoległe obliczenie L[i,k] = A[i,k] * inv(L_kk^T)
                        int rows_i = Math.Min(blockSize, size - i);     // rozmiar bloku i-tego (jeśli liczba wierszy < blockSize obliczenia także działają)
                        double[,] A_ik = GetBlock(this.A, i, k, rows_i, panelSize);
                        double[,] L_ik = Processor.Multiply(A_ik, invL_kkT);
                        SetBlock(L, i, k, L_ik);
                    });
                }

                // 3. aktualizacja macierzy A:
                // dla wszystkich bloków (i, j >= k+blockSize):
                // A[i,j] -= L[i,k] * L[j,k]^T
                // dla paneli (i==j), A[i,j] -= L[i,k]*L[i,k]^T (symetryczny)
                if (iBlocks.Count > 0){
                    var opts2 = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts2, i =>
                    {
                        int rows_i = Math.Min(blockSize, size - i);
                        double[,] L_ik = GetBlock(L, i, k, rows_i, panelSize); // L[i,k] - pierwszy operand dla wiersza

                        // j zaczyna się od i, i idzie w prawo -> aktualizacja bloków symetrycznych
                        for (int j = i; j < size; j += blockSize)
                        {
                            int rows_j = Math.Min(blockSize, size - j);
                            double[,] L_jk = GetBlock(L, j, k, rows_j, panelSize);

                            // L_ik * (L_jk)^T  => (rows_i x blockSize) * (blockSize x rows_j) = rows_i x rows_j
                            double[,] L_jk_T = Processor.Transpose(L_jk); // L[j,k]^T - drugi operand
                            double[,] product = Processor.Multiply(L_ik, L_jk_T); // rows_i x rows_j

                            double[,] Aij = GetBlock(this.A, i, j, rows_i, rows_j);
                            SubtractAndSave(Aij, product);
                            SetBlock(this.A, i, j, Aij);

                            if (i != j)     // A[j,i] = Aij^T
                            {
                                double[,] Aji = Processor.Transpose(Aij);
                                SetBlock(this.A, j, i, Aji);
                            }
                        }
                    });
                }
            }

            // L * y = b
            double[] y = ForwardSubstitution(L, this.b);

            // L^T * x = y
            return BackwardSubstitutionFromLower(L, y);
        }

        private double[] SolveAVX(int blockSize){
            int size = A.GetLength(0);
            double[,] L = new double[size, size];

            for (int k = 0; k < size; k += blockSize)
            {                          // panel index = k
                int panelSize = Math.Min(blockSize, size - k);                  // rozmiar panelu (blockSize lub mniejszy na końcu macierzy)

                // 1. Panel factor -> zrefaktoryzuj panel A[k,k] (blockSize x blockSize) metodą nieblokową
                double[,] panel = GetBlock(this.A, k, k, panelSize, panelSize);
                double[,] L_kk = CholeskyUnblocked(panel);
                SetBlock(L, k, k, L_kk); // zapis

                // 2. rozwiązywanie bloków -> dla wszystkich i>pIndex (blokowo - równolegle)
                double[,] invL_kk = Inverse(L_kk);                    // inv(L_kk)

                double[,] invL_kkT;
                Transpose(invL_kk, panelSize, panelSize, invL_kkT = new double[panelSize, panelSize]); // inv(L_kk)^T == inv(L_kk^T)

                var iBlocks = new List<int>();
                for (int i = k + panelSize; i < size; i += blockSize) iBlocks.Add(i); // iBlocks: lista indeksów bloków poniżej panelu kk

                if (iBlocks.Count > 0)
                {
                    var opts = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts, i => {              // Równoległe obliczenie L[i,k] = A[i,k] * inv(L_kk^T)
                        int rows_i = Math.Min(blockSize, size - i);     // rozmiar bloku i-tego (jeśli liczba wierszy < blockSize obliczenia także działają)
                        double[,] A_ik = GetBlock(this.A, i, k, rows_i, panelSize);
                        double[,] L_ik;
                        Multiply_AVX(A_ik, rows_i, panelSize, invL_kkT, panelSize, L_ik = new double[rows_i, panelSize]);
                        SetBlock(L, i, k, L_ik);
                    });
                }

                // 3. aktualizacja macierzy A:
                // dla wszystkich bloków (i, j >= k+blockSize):
                // A[i,j] -= L[i,k] * L[j,k]^T
                // dla paneli (i==j), A[i,j] -= L[i,k]*L[i,k]^T (symetryczny)
                if (iBlocks.Count > 0)
                {
                    var opts2 = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts2, i =>
                    {
                        int rows_i = Math.Min(blockSize, size - i);
                        double[,] L_ik = GetBlock(L, i, k, rows_i, panelSize); // L[i,k] - pierwszy operand dla wiersza

                        // j zaczyna się od i, i idzie w prawo -> aktualizacja bloków symetrycznych
                        for (int j = i; j < size; j += blockSize)
                        {
                            int rows_j = Math.Min(blockSize, size - j);
                            double[,] L_jk = GetBlock(L, j, k, rows_j, panelSize);

                            // L_ik * (L_jk)^T  => (rows_i x blockSize) * (blockSize x rows_j) = rows_i x rows_j
                            double[,] L_jk_T;
                            Transpose(L_jk, rows_j, panelSize, L_jk_T = new double[panelSize, rows_j]);
                            double[,] product;
                            if(rows_i == blockSize && rows_j == blockSize)Multiply_AVX(L_ik, rows_i, panelSize, L_jk_T, rows_j, product = new double[rows_i, rows_j]);
                            else Multiply_Basic(L_ik, rows_i, panelSize, L_jk_T, rows_j, product = new double[rows_i, rows_j]);

                            double[,] Aij = GetBlock(this.A, i, j, rows_i, rows_j);
                            SubtractAndSave(Aij, product);
                            SetBlock(this.A, i, j, Aij);

                            if (i != j)     // A[j,i] = Aij^T
                            {
                                double[,] Aji;
                                Transpose(Aij, rows_i, rows_j, Aji = new double[rows_j, rows_i]);
                                SetBlock(this.A, j, i, Aji);
                            }
                        }
                    });
                }
            }

            // L * y = b
            double[] y = ForwardSubstitution(L, this.b);

            // L^T * x = y
            return BackwardSubstitutionFromLower(L, y);
        }

        private double[] SolveSSE(int blockSize)
        {
            int size = A.GetLength(0);
            double[,] L = new double[size, size];

            for (int k = 0; k < size; k += blockSize)
            {                          // panel index = k
                int panelSize = Math.Min(blockSize, size - k);                  // rozmiar panelu (blockSize lub mniejszy na końcu macierzy)

                // 1. Panel factor -> zrefaktoryzuj panel A[k,k] (blockSize x blockSize) metodą nieblokową
                double[,] panel = GetBlock(this.A, k, k, panelSize, panelSize);
                double[,] L_kk = CholeskyUnblocked(panel);
                SetBlock(L, k, k, L_kk); // zapis

                // 2. rozwiązywanie bloków -> dla wszystkich i>pIndex (blokowo - równolegle)
                double[,] invL_kk = Inverse(L_kk);                    // inv(L_kk)

                double[,] invL_kkT;
                Transpose(invL_kk, panelSize, panelSize, invL_kkT = new double[panelSize, panelSize]); // inv(L_kk)^T == inv(L_kk^T)

                var iBlocks = new List<int>();
                for (int i = k + panelSize; i < size; i += blockSize) iBlocks.Add(i); // iBlocks: lista indeksów bloków poniżej panelu kk

                if (iBlocks.Count > 0)
                {
                    var opts = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts, i => {              // Równoległe obliczenie L[i,k] = A[i,k] * inv(L_kk^T)
                        int rows_i = Math.Min(blockSize, size - i);     // rozmiar bloku i-tego (jeśli liczba wierszy < blockSize obliczenia także działają)
                        double[,] A_ik = GetBlock(this.A, i, k, rows_i, panelSize);
                        double[,] L_ik;
                        Multiply_SSE2(A_ik, rows_i, panelSize, invL_kkT, panelSize, L_ik = new double[rows_i, panelSize]);
                        SetBlock(L, i, k, L_ik);
                    });
                }

                // 3. aktualizacja macierzy A:
                // dla wszystkich bloków (i, j >= k+blockSize):
                // A[i,j] -= L[i,k] * L[j,k]^T
                // dla paneli (i==j), A[i,j] -= L[i,k]*L[i,k]^T (symetryczny)
                if (iBlocks.Count > 0)
                {
                    var opts2 = new ParallelOptions { MaxDegreeOfParallelism = nThreads };
                    Parallel.ForEach(iBlocks, opts2, i =>
                    {
                        int rows_i = Math.Min(blockSize, size - i);
                        double[,] L_ik = GetBlock(L, i, k, rows_i, panelSize); // L[i,k] - pierwszy operand dla wiersza

                        // j zaczyna się od i, i idzie w prawo -> aktualizacja bloków symetrycznych
                        for (int j = i; j < size; j += blockSize)
                        {
                            int rows_j = Math.Min(blockSize, size - j);
                            double[,] L_jk = GetBlock(L, j, k, rows_j, panelSize);

                            // L_ik * (L_jk)^T  => (rows_i x blockSize) * (blockSize x rows_j) = rows_i x rows_j
                            double[,] L_jk_T;
                            Transpose(L_jk, rows_j, panelSize, L_jk_T = new double[panelSize, rows_j]);
                            double[,] product;
                            if (rows_i == blockSize && rows_j == blockSize) Multiply_SSE2(L_ik, rows_i, panelSize, L_jk_T, rows_j, product = new double[rows_i, rows_j]);
                            else Multiply_Basic(L_ik, rows_i, panelSize, L_jk_T, rows_j, product = new double[rows_i, rows_j]);

                            double[,] Aij = GetBlock(this.A, i, j, rows_i, rows_j);
                            SubtractAndSave(Aij, product);
                            SetBlock(this.A, i, j, Aij);

                            if (i != j)     // A[j,i] = Aij^T
                            {
                                double[,] Aji;
                                Transpose(Aij, rows_i, rows_j, Aji = new double[rows_j, rows_i]);
                                SetBlock(this.A, j, i, Aji);
                            }
                        }
                    });
                }
            }

            // L * y = b
            double[] y = ForwardSubstitution(L, this.b);

            // L^T * x = y
            return BackwardSubstitutionFromLower(L, y);
        }

        /// zwraca blok macierzy A o wymiarach rows x cols, zaczynający się od pozycji (row, col)
        /// używane do przydzialania bloków do wątków
        private static double[,] GetBlock(double[,] A, int row, int col, int rows, int cols)
        {
            double[,] B = new double[rows, cols];
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    B[i, j] = A[row + i, col + j];
            return B;
        }

        /// ustawia blok macierzy A o wymiarach B.GetLength(0) x B.GetLength(1), zaczynający się od pozycji (row, col)
        private static void SetBlock(double[,] A, int row, int col, double[,] B)
        {
            int rows = B.GetLength(0), cols = B.GetLength(1);
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < cols; ++j)
                    A[row + i, col + j] = B[i, j];
        }

        private static double[,] Inverse(double[,] A)
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

        /// Nieblokowa wersja dekompozycji Cholesky'ego dla małych macierzy
        private static double[,] CholeskyUnblocked(double[,] A)
        {
            int n = A.GetLength(0);

            double[,] L = new double[n, n];
            for (int k = 0; k < n; ++k)
            {
                double sum = 0.0;
                for (int s = 0; s < k; ++s) sum += L[k, s] * L[k, s];
                double d = A[k, k] - sum;
                if (d <= 0.0 || double.IsNaN(d)){
                    if(!Tikhonov)throw new InvalidOperationException("Macierz nie jest dodatnio określona.");
                    else {
                        TikhonovReg(A);
                        sum = 0.0;
                        for (int s = 0; s < k; ++s) sum += L[k, s] * L[k, s];
                        d = A[k, k] - sum;
                    }
                }
                    
                L[k, k] = Math.Sqrt(d);

                for (int i = k + 1; i < n; ++i)
                {
                    double s2 = 0.0;
                    for (int t = 0; t < k; ++t) s2 += L[i, t] * L[k, t];
                    L[i, k] = (A[i, k] - s2) / L[k, k];
                }
            }
            return L;
        }

        /// Rozwiązuje układ L*y = b metodą podstawiania w przód
        private static double[] ForwardSubstitution(double[,] L, double[] b)
        {
            int n = L.GetLength(0);
            var y = new double[n];
            for (int i = 0; i < n; ++i)
            {
                double s = 0.0;
                for (int j = 0; j < i; ++j) s += L[i, j] * y[j];
                y[i] = (b[i] - s) / L[i, i];
            }
            return y;
        }

        /// Rozwiązuje układ L^T*x = y metodą podstawiania w tył
        private static double[] BackwardSubstitutionFromLower(double[,] L, double[] y)
        {
            int n = L.GetLength(0);
            var x = new double[n];
            for (int i = n - 1; i >= 0; --i)
            {
                double s = 0.0;
                for (int j = i + 1; j < n; ++j) s += L[j, i] * x[j]; // L^T[i,j] = L[j,i]
                x[i] = (y[i] - s) / L[i, i];
            }
            return x;
        }

        private static int setSize(int matrixSize, int nThreads)
        {
            if(matrixSize < 16) return 2;
            if (nThreads >= 8)
            {
                if (matrixSize >= 128) return 16;
                else if (matrixSize >= 64) return 8;
                else if (matrixSize >= 32) return 4;
                else return 2;
            }
            else if (nThreads >= 4)
            {
                if (matrixSize >= 64) return 8;
                else return 4;
            }
            else return 2;
        }

        private static void SubtractAndSave(double[,] A, double[,] B)
        {
            int row = A.GetLength(0), col = A.GetLength(1);
            for (int i = 0; i < row; ++i)
                for (int j = 0; j < col; ++j) A[i, j] -= B[i, j];
        }

        private static void TikhonovReg(double[,] matrix)
        {
            const float LAMBDA_COEF = 0.001f;
            if (matrix is null) throw new ArgumentNullException(nameof(matrix));
            if(matrix.GetLength(0) != matrix.GetLength(1)) throw new ArgumentException("Niepoprawna macierz do regularyzacji.");
            for(int i = 0; i < matrix.GetLength(0); ++i)
            {
                matrix[i, i] += LAMBDA_COEF;
            }
        }
    }
}
