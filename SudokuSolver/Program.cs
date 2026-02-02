using System;
using System.Diagnostics;
using System.IO;

namespace SudokuSolver
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to my Sudoku Solver!");

            while (true)
            {
                Console.WriteLine("\nSelect Mode:");
                Console.WriteLine("1. Interactive (Type single puzzles)");
                Console.WriteLine("2. Batch (Process a text file)");
                Console.WriteLine("Type 'exit' to quit.");

                Console.Write(">");
                string mode = Console.ReadLine()?.Trim();

                if (mode == "1")
                {
                    RunInteractiveMode();
                }
                else if (mode == "2")
                {
                    RunBatchMode();
                }
                else if (mode == "exit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid selection.");
                }
            }
        }

        private static void RunInteractiveMode()
        {
            Console.WriteLine("\n--- Interactive Mode ---");
            Console.WriteLine("Enter a puzzle string (or type 'menu' to go back).");

            Solver cachedSolver = null;

            while (true)
            {
                Console.Write("\nPuzzle > ");
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input)) continue;
                if (input.Equals("menu", StringComparison.OrdinalIgnoreCase)) break;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) Environment.Exit(0);

                try
                {
                    var board = new SudokuBoard(input);
                    Console.WriteLine($"Board recognized: {board.size}x{board.size}");
                    IO.Print(board);
                    if (cachedSolver == null || cachedSolver.size != board.size)
                    {
                        cachedSolver = new Solver(board.size);
                    }
                    Stopwatch sw = Stopwatch.StartNew();
                    bool result = cachedSolver.Solve(board);
                    sw.Stop();

                    if (result)
                    {
                        Console.WriteLine($"Solved in {sw.Elapsed.TotalSeconds:F8} sec ({sw.Elapsed.TotalMilliseconds:F4} ms)");
                        IO.Print(board);
                        IO.PrintAsString(board);
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
        private static void RunBatchMode()
        {
            Console.WriteLine("\n--- Batch Mode ---");
            Console.WriteLine("Enter the full file path (or 'menu' to go back):");

            Console.Write("Path > ");//     C:\Users\Darth\Downloads\17_clue.txt
            string path = "C:\\Users\\Darth\\Downloads\\17_clue.txt";  // Console.ReadLine()?.Trim('"'); 

            if (path == "menu") return;
            if (!File.Exists(path))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            Console.WriteLine("Processing... (This excludes file reading time)");

            Solver currentSolver = null;
            long totalAlgorithmTicks = 0;
            int solvedCount = 0;
            int totalPuzzles = 0;

            Stopwatch wallClock = Stopwatch.StartNew();
            long maxticks = 0;
            foreach (string line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var board = new SudokuBoard(line);
                    totalPuzzles++;

                    if (currentSolver == null || currentSolver.size != board.size)
                    {
                        currentSolver = new Solver(board.size);
                    }
                    long start = Stopwatch.GetTimestamp();
                    bool success = currentSolver.Solve(board);
                    long end = Stopwatch.GetTimestamp();

                    if (success)
                    {
                        solvedCount++;
                        totalAlgorithmTicks += (end - start);
                        maxticks = (end - start) > maxticks ? end - start : maxticks;
                    }
                    if (totalPuzzles % 1000 == 0) Console.Write(".");
                }
                catch
                {
                    //so it doesnt crash on bad inputs
                }
            }

            wallClock.Stop();
            double totalAlgoSeconds = (double)totalAlgorithmTicks / Stopwatch.Frequency;
            double avgMs = (totalAlgoSeconds * 1000) / (solvedCount > 0 ? solvedCount : 1);

            Console.WriteLine("\n\n--- Reuslts ---");
            Console.WriteLine($"Total Puzzles: {totalPuzzles}");
            Console.WriteLine($"Solved:        {solvedCount}");
            Console.WriteLine($"Algorithm Time: {totalAlgoSeconds:F4} sec (Total CPU time spent thinking)");
            Console.WriteLine($"Avg per Puzzle: {avgMs:F4} ms");
            Console.WriteLine($"Longest time:   {((double)maxticks / Stopwatch.Frequency):F4} seconds");
            Console.WriteLine($"Total Wall Time: {wallClock.Elapsed.TotalSeconds:F4} sec (Includes file I/O)");
        }
    }
}