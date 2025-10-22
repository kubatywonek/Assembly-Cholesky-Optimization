using CSlibrary; //temporary
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace projekt
{
    public partial class Form1 : Form
    {
        Cholesky[] choleskySolvers = new Cholesky[5];
        bool ready = false;
        double[,] A;
        double[] b;
        string[] variables;
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Transpose(double[,] src, int rows, int cols, double[,] dst);
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Multiply_SSE2(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);
        [DllImport(@"C:\Users\YoloT\source\repos\projektJA\projekt\x64\Debug\ASMlibrary.dll")]
        static extern void Multiply_AVX(double[,] multiplicantA, int Arows, int Acols, double[,] multiplierB, int Bcols, double[,] dst);

        public Form1()
        {
            InitializeComponent();
            InitializeThreadSlider();
            for(int i = 0; i < choleskySolvers.Length; ++i){
                choleskySolvers[i] = null;
            }
            A = null;
            b = null;
            variables = null;
            ReloadButton.Enabled = false;
        }
        private void InitializeThreadSlider()
        {
            threadSlider.Minimum = 1;
            threadSlider.Maximum = 64;
            threadSlider.Value = Environment.ProcessorCount;
            ThreadsNum.Text = threadSlider.Value.ToString();
        }

        private void threadSlider_Scroll(object sender, EventArgs e)
        {
            ThreadsNum.Text = threadSlider.Value.ToString();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (var FD = new OpenFileDialog()){
                FD.Filter = "Text files (*.txt)|*.txt";
                FD.Title = "Wybierz plik z równaniami";
                if (FD.ShowDialog() != DialogResult.OK) return;

                try{
                    var (A, b, variables) = ParseEquationsFromText(FD.FileName);
                    MessageBox.Show($"Wczytano macierz {A.GetLength(0)}x{A.GetLength(1)}.\nZmienne (kolejność): {string.Join(", ", variables)}",
                                    "Sukces", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Indicator.Text = "Dane wczytane poprawnie.";
                    Indicator.ForeColor = Color.Green;
                    Indicator.Checked = true;
                    ready = true;
                    this.A = A;
                    this.b = b;
                    this.variables = variables;
                }
                catch (Exception ex){
                    MessageBox.Show("Błąd podczas wczytywania: " + ex.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Indicator.Text = "Brak danych.";
                    Indicator.ForeColor = Color.Red;
                    Indicator.Checked = false;
                    ready = false;
                }
            }
        }

        /// parsuje plik .txt z równaniami n*<współczynnik><zmienna> = <wyraz>
        /// separator dziesiętny '.' oraz tylko lewa strona równania posiada wyrażenia ze zmiennymi
        /// jeśli układ jest ponadokreślony dopasowuje do kwadratowej przez zachowanie pierwszych nVar równań
        /// zwraca macierz A (nVar x nVar), wektor b (nVar) oraz tablicę zmiennych (kolejność występowania)
        private (double[,] A, double[] b, string[] variables) ParseEquationsFromText(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Plik nie istnieje.", path);

            var rawLines = File.ReadAllLines(path);

            // lista współczynników każdego równania (słownik zmienna->współczynnik)
            var eqVarCoeffs = new List<Dictionary<string, double>>(capacity: rawLines.Length);
            // lista wyrazów wolnych
            var eqRhs = new List<double>(capacity: rawLines.Length);
            // kolejność pierwszego wystąpienia zmiennych
            var variableOrder = new List<string>();
            // zbiór zmiennych (NonCaseSensitive)
            var varSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int idx = 0; idx < rawLines.Length; idx++){
                var raw = rawLines[idx];
                if (raw == null) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.Contains(',')) throw new FormatException($"Przecinek w linii {idx + 1}. Separator dziesiętny to znak '.'.");
                var parts = line.Split('=');
                if (parts.Length != 2) throw new FormatException($"Równanie w linii {idx + 1} powinno zawierać jeden znak '='.");

                var left = parts[0].Trim();
                var right = parts[1].Trim();

                if (left.Length == 0 || right.Length == 0) throw new FormatException($"Niekompletna linia {idx + 1}.");
                if (!IsPlainDecimal(right)) throw new FormatException($"Nieprawidłowy wyraz wolny w linii {idx + 1}: '{right}'.");

                double rhs = double.Parse(right, NumberStyles.Float, CultureInfo.InvariantCulture);

                var coeffs = ParseLeftSideStrict(left, idx + 1);

                // uzupełnianie kolejności zmiennych (pierwsze wystąpienie w pliku)
                foreach (var varName in coeffs.Keys){
                    if (!varSet.Contains(varName)){
                        varSet.Add(varName);
                        variableOrder.Add(varName);
                    }
                }
                eqVarCoeffs.Add(coeffs);
                eqRhs.Add(rhs);
            }

            if (eqVarCoeffs.Count == 0) throw new InvalidOperationException("Nieprawidłowa zawartość pliku.");

            int nEq = eqVarCoeffs.Count;
            int nVar = variableOrder.Count;

            // sprawdzenie kwadratowości:
            if (nEq < nVar) throw new InvalidOperationException($"Układ niedookreślony: liczba równań = {nEq}, liczba zmiennych = {nVar}.");
            else if (nEq > nVar){
                int removed = nEq - nVar;
                eqVarCoeffs = eqVarCoeffs.Take(nVar).ToList();
                eqRhs = eqRhs.Take(nVar).ToList();
                nEq = nVar;
                MessageBox.Show($"Układ ponadokreślony.\n" + $"Dodatkowe {removed} równanie(a) zostało(y) odrzucone.",
                                "Uwaga - ponadokreślony układ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            double[,] A = new double[nVar, nVar];
            double[] b = new double[nVar];

            for (int i = 0; i < nVar; i++){
                var dict = eqVarCoeffs[i];
                for (int j = 0; j < nVar; j++)
                {
                    string varName = variableOrder[j];
                    if (dict.TryGetValue(varName, out double c)) A[i, j] = c;
                    else A[i, j] = 0.0;
                }
                b[i] = eqRhs[i];
            }

            return (A, b, variableOrder.ToArray());
        }

        /// oczekuje fragmentów w postaci <+/-><liczba><zmienna>.
        /// akceptuje tylko kropkę jako separator dziesiętny.
        /// zwraca Dictionary zmienna->współczynnik (sumuje powtarzające się zmienne).
        private Dictionary<string, double> ParseLeftSideStrict(string left, int lineNumber)
        {
            var normalized = left.Replace("−", "-").Replace(" ", "");
            if (normalized.Length == 0) throw new FormatException($"Lewy człon pusty w linii {lineNumber}.");

            if (normalized[0] != '+' && normalized[0] != '-') normalized = "+" + normalized;

            var tokens = Regex.Split(normalized, @"(?=[+-])").Where(t => !string.IsNullOrEmpty(t)).ToArray();

            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // tokenRegex: znak +- opcjonalna liczba w formacie d[.d] + nazwa zmiennej
            var tokenRegex = new Regex(@"^([+-])(?:(\d+(?:\.\d+)?)?)?([A-Za-z]\w*)$", RegexOptions.Compiled);

            foreach (var tok in tokens){
                var m = tokenRegex.Match(tok);
                if (!m.Success) throw new FormatException($"Wystąpił bład podczas wczytywania '{tok}' w linii {lineNumber}.");

                string sign = m.Groups[1].Value;
                string numPart = m.Groups[2].Success ? m.Groups[2].Value : null;
                string varName = m.Groups[3].Value;

                double signMult = sign == "+" ? 1.0 : -1.0;
                double coeff = 1.0;
                if (!string.IsNullOrEmpty(numPart)) coeff = double.Parse(numPart, NumberStyles.Float, CultureInfo.InvariantCulture);
                coeff *= signMult;

                // jeśli zmienna się powtórzy, współczynniki są sumowane
                if (dict.ContainsKey(varName)) dict[varName] += coeff;
                else dict[varName] = coeff;
            }

            return dict;
        }

        /// sprawdza format liczby: <+/->liczba[.liczba]
        private bool IsPlainDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            return Regex.IsMatch(s.Trim(), @"^[+-]?\d+(?:\.\d+)?$");
        }

        /// tworzy szybki podgląd na stan macierzy.
        /// wyświetla zawartość macierzy przy pomocy Console.Write().
        /// funkcja deweloperska do testów.
        private void LogMatrix(double[,] matrix)
        {
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    Console.Write(matrix[i, j] + "\t");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            return;
        }

        /// tworzy szybki podgląd na stan wektora.
        /// wyświetla zawartość wektora przy pomocy Console.Write().
        /// funkcja deweloperska do testów.
        private void LogVector(double[] vec)
        {
            for (int i = 0; i < vec.GetLength(0); i++)
            {
                Console.Write(vec[i] + "\t");
            }
            Console.WriteLine();
            return;
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if(ready == false){
                MessageBox.Show("Brak wczytanych danych. Załaduj dane przed rozpoczęciem przetwarzania.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            for (int i = 0; i < choleskySolvers.Length; ++i) choleskySolvers[i] = new Cholesky(this.A, this.b);
            int num = threadSlider.Value;
            foreach(Cholesky choleskySolver in choleskySolvers) choleskySolver.SetThreads(num);
            List<double[]> results = new List<double[]>();
            try{
                // ASMButton -> true - ASM, false - C#          RegularizationCheckbox -> true - potentially Tikhonov, false - none
                foreach (Cholesky choleskySolver in choleskySolvers) results.Add(choleskySolver.Solve(ASMButton.Checked, RegularizationCheckbox.Checked));
                for (int i = 1; i < results.Count; ++i) if (!results[i].SequenceEqual(results[i - 1])) throw new Exception("Niespójne wyniki!");
                SaveResults(results[0]);
                float accTime = 0f;
                foreach (Cholesky choleskySolver in choleskySolvers) accTime += choleskySolver.GetTime();
                accTime /= choleskySolvers.Length;
                Time.Text = accTime.ToString("F2") + " ms";
            }
            catch (Exception ex){
                MessageBox.Show("Błąd podczas przetwarzania: " + ex.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally{
                for(int i = 0; i < choleskySolvers.Length; ++i) choleskySolvers[i] = null;
                ready = false;
                Indicator.Text = "Brak danych.";
                Indicator.ForeColor = Color.Red;
                Indicator.Checked = false;
                ReloadButton.Enabled = true;
            }
            
        }

        private void CButton_CheckedChanged(object sender, EventArgs e)
        {
            ASMButton.Checked = !CButton.Checked;
        }

        private void ASMButton_CheckedChanged(object sender, EventArgs e)
        {
            CButton.Checked = !ASMButton.Checked;
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            if (A != null && b != null && variables != null)
            {
                ReloadButton.Enabled = false;
                Indicator.Text = "Dane wczytane poprawnie.";
                Indicator.ForeColor = Color.Green;
                Indicator.Checked = true;
                ready = true;
            }
            else MessageBox.Show("Nie wczytano danych. Załaduj dane przed ponownym przetwarzaniem.", "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // function that saves results to a text file
        // default location is desktop
        // file has timestamp in name to avoid overwriting
        private void SaveResults(double[] results)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = $"CholeskyResults_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt";
            string fullPath = Path.Combine(desktopPath, fileName);
            StreamWriter output = new StreamWriter(fullPath);
            for (int i = 0; i < results.Length; i++){
                    output.WriteLine($"{variables[i]} = {results[i]}");
            }
            output.Close();
            return;
        }
    }
}
