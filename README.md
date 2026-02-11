# High-Performance Sudoku Solver (.NET 8)

A robust, bitwise-optimized Sudoku solver engineered for extreme speed, safety, and architectural clarity. This project demonstrates advanced C# concepts, including **Span-based memory management**, **bitwise constraint propagation**, and **asynchronous signal handling**.

## ?? Key Features
* **Bitwise Engine:** Utilizes 32-bit bitmasks for $O(1)$ constraint checking.
* **Zero-Allocation Solving:** Employs `stackalloc` and `Span<T>` to keep recursion off the Heap, eliminating GC overhead.
* **Advanced Heuristics:** * **MRV (Minimum Remaining Values):** Prunes the search tree by picking the most constrained cells first.
    * **Hidden Single Detection:** Identifies forced moves within units to skip unnecessary recursion.
* **Graceful Interrupts:** Custom `CancelKeyPress` handling allows for "Soft Cancellation" finishing current tasks before exiting safely.

---

## ??? Architecture
The solution follows a **Separation of Concerns** model, utilizing a dedicated MSTest project to ensure engine reliability without cluttering production binaries.

| Project | Type | Responsibility |
| :--- | :--- | :--- |
| **SudokuSolver.Core** | Class Library | Logic, bitwise engine, and data structures. |
| **SudokuSolver.CLI** | Console App | Main UI, batch processing, and file handling. |
| **SudokuSolver.Tests** | MSTest Project | Unit testing and thread-safety audits. |

---

## ? Performance Benchmarks

The solver was tested against a standardized dataset of medium-to-hard puzzles to validate the efficiency of the bitwise engine.

| Metric | Result |
| :--- | :--- |
| **Dataset Size** | 49151 Puzzles |
| **Total Execution Time** | ~1.08 Seconds |
| **Average Solve Speed** | **~45,510 Puzzles / Sec** |
| **Average Latency** | **~0.0219 ms per puzzle** |

---

## ?? Technical Implementation

### 1. Memory Efficiency (`stackalloc` & `Span<T>`)
To avoid Garbage Collector (GC) pressure during deep recursion, the solver uses `stackalloc`. Allocating bitmasks on the **Stack** ensures:
* **Near-Zero Latency:** Stack access is significantly faster than heap access.
* **Thread Safety:** Each thread has its own stack, making the solver inherently thread-safe.

### 2. Bitwise Constraint Propagation
Instead of using arrays or lists to track used numbers, we use a single `uint` as a bitmask.
* **Isolation of Lowest Bit:** Uses the $x \ \& \ -x$ bit-negation trick to isolate candidates in $O(1)$ time.
* **Candidate Calculation:** Uses bitwise `NOT` on combined masks to determine legal moves instantly.

---

## ?? Testing & Quality Assurance
The solution includes a comprehensive test suite covering validation, accuracy, and concurrency.

### Running Tests:
```bash
dotnet test SudokuSolver.sln
