using System;
using System.Diagnostics;

namespace SudokuSolver
{
    public class InteractiveRunner
    {
        private Solver _cachedSolver;

        public void Run()
        {
            Console.WriteLine("\n--- Interactive Mode ---");
            Console.WriteLine("Enter a puzzle string (or type 'menu' to go back).");

            while (true)
            {
                Console.Write("\nPuzzle > ");
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input)) continue;
                if (input.Equals("menu", StringComparison.OrdinalIgnoreCase)) break;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);

                ProcessInput(input);
            }
        }

        private void ProcessInput(string input)
        {
            try
            {
                var board = new SudokuBoard(input);
                Console.WriteLine($"Board recognized: {board.size}x{board.size}");
                IO.Print(board);

                // Initialize solver only if needed or if size changes
                if (_cachedSolver == null || _cachedSolver.size != board.size)
                {
                    _cachedSolver = new Solver(board.size);
                }

                Stopwatch sw = Stopwatch.StartNew();
                bool result = _cachedSolver.Solve(board);
                sw.Stop();

                if (result)
                {
                    Console.WriteLine($"Solved in {sw.Elapsed.TotalSeconds:F8} sec ({sw.Elapsed.TotalMilliseconds:F4} ms)");
                    IO.Print(board);
                    Console.WriteLine(IO.RecieveAsString(board));
                }
                else
                {
                    Console.WriteLine($"Unsolvable. (Took {sw.Elapsed.TotalSeconds:F8} sec)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}