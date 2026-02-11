using System;
using System.Diagnostics;
using System.IO;

namespace SudokuSolver
{
    public class BatchRunner
    {
        public void Run()
        {
            Console.WriteLine("\n--- Batch Mode ---");
            Console.WriteLine("Enter the full file path (or 'menu' to go back):");
            Console.Write("Path > ");
            string path = Console.ReadLine();
            path = !String.IsNullOrWhiteSpace(path) ? path :"";
            path = path.Trim('"'); 

            if (path == "menu") return;
            if (!File.Exists(path))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            ProcessFile(path);
        }

        private void ProcessFile(string path)
        {
            Console.WriteLine("Processing... (This excludes file reading time)");

            Solver currentSolver = null;
            long totalAlgorithmTicks = 0;
            int solvedCount = 0;
            int totalPuzzles = 0;
            long maxTicks = 0;

            Stopwatch wallClock = Stopwatch.StartNew();

            try
            {
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
                            long duration = end - start;
                            totalAlgorithmTicks += duration;
                            if (duration > maxTicks) maxTicks = duration;
                        }
                        if (totalPuzzles % SudokuConfig.ProgressInterval == 0) Console.Write(".");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
            catch (IOException ex)
            {
                Console.WriteLine($"\nCritical Error reading file: {ex.Message}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("\nError: Access to the file is denied.");
            }
            wallClock.Stop();
            PrintStats(totalPuzzles, solvedCount, totalAlgorithmTicks, maxTicks, wallClock.Elapsed.TotalSeconds);
        }

        private void PrintStats(int total, int solved, long totalTicks, long maxTicks, double wallTime)
        {
            double totalAlgoSeconds = (double)totalTicks / Stopwatch.Frequency;
            double avgMs = (totalAlgoSeconds * 1000) / (solved > 0 ? solved : 1);

            Console.WriteLine("\n\n--- Results ---");
            Console.WriteLine($"Total Puzzles:   {total}");
            Console.WriteLine($"Solved:          {solved}");
            Console.WriteLine($"Algorithm Time:  {totalAlgoSeconds:F4} sec (Total CPU time spent thinking)");
            Console.WriteLine($"Avg per Puzzle:  {avgMs:F4} ms");
            Console.WriteLine($"Longest time:    {((double)maxTicks / Stopwatch.Frequency):F4} seconds");
            Console.WriteLine($"Total Wall Time: {wallTime:F4} sec (Includes file I/O)");
        }
    }
}