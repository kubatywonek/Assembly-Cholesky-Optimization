# Cholesky Linear Equation Solver 🚀

[![Language: C#](https://img.shields.io/badge/Language-C%23-blue.svg)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![Language: Assembly x64](https://img.shields.io/badge/Language-Assembly_x64-red.svg)](https://en.wikipedia.org/wiki/X86_assembly_language)

This project is an optimization gains comparison for solving systems of linear equations using **Cholesky decomposition**. The main goal of this application is to benchmark and compare the performance (execution time and hardware utilization) between a high-level math library written in C# and a highly optimized low-level implementation in x64 Assembly.

## 🧠 About the Project

Cholesky decomposition is an efficient method for solving linear equations where the coefficient matrix is symmetric and positive-definite. This project serves as a practical study of code optimization and computer architecture.

The Assembly implementation has been heavily optimized utilizing the advanced **SSE/AVX** (SIMD - Single Instruction, Multiple Data) instruction sets. Furthermore, to maximize the potential of modern multi-core processors, both libraries (C# and ASM) are fully **multi-threaded**.

## ✨ Key Features

- **Dual Implementation:** A side-by-side comparison of the algorithm written in pure C# versus x64 Assembly.
- **Hardware Optimization:** Hardware-accelerated matrix calculations using vectorization (SSE/AVX).
- **Multi-threading:** Parallel task execution in both libraries to significantly speed up mathematical operations.
- **Performance Benchmarking:** The application measures and visualizes the exact "optimization reward" gained from using low-level, hardware-specific instructions.

## 📂 Repository Structure

- 📁 **`ASMlibrary/`** – A dynamic library containing the x64 Assembly code, serving as the highly optimized computational core.
- 📁 **`CSlibrary/`** – The reference implementation of the Cholesky algorithm written in C#.
- 📁 **`projekt/`** – The main application module responsible for library integration, execution, and performance benchmarking.

## 🛠 Technologies Built With

- **C# (.NET)** - Main logic, high-level implementation, and benchmarking
- **x64 Assembly (MASM)** - Low-level, high-performance mathematical procedures
- **C/C++** - Interface integration support

## 🚀 Getting Started

1. Clone the repository to your local machine:
   ```bash
   git clone [https://github.com/kubatywonek/Assembly-Cholesky-Optimization.git](https://github.com/kubatywonek/Assembly-Cholesky-Optimization.git)

2. Open the solution in Visual Studio (recommended due to built-in support for C# and the Microsoft Macro Assembler).

3. Ensure the Build Target platform is set to x64. The 64-bit registers and SIMD instructions will not compile under an x86 architecture.

4. Build and run the solution (the executable will be generated from the projekt folder).

## Author

[Jakub Tywonek - @kubatywonek](https://www.github.com/kubatywonek)

***
