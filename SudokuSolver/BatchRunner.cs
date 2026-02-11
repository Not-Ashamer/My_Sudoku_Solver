using System;
using System.Diagnostics;
using System.IO;

namespace SudokuSolver
{
    public class BatchRunner
    {
        /// <summary>
        /// This function serves as the basis for the batch mode, continually taking in inputs, 
        /// and either calling ProcessFile to solve them, or exiting
        /// </summary>
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
		/// <summary>
		/// This function accepts the string path to a file with unsolved sudokus, and solves them until they are through. To exit halfway,
		/// type CTRL+C
		/// </summary>
		/// <param name="path">The file path to the newline-separated puzzle file.</param>
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
                    if (SudokuApp.IsCancelled)
                    {
                        Console.WriteLine("\nBatch run aborted by user. Returning to menu...");
                        SudokuApp.IsCancelled = false; 
                        break;
                    }
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
                        Console.WriteLine(ex.Message);//Prevents the throwing of one exception crashing the other many puzzles
                    }
                }

            }
            catch (PathTooLongException)
            {
                Console.WriteLine("Error: Path is too long.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"\nCritical Error reading file: {ex.Message}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("\nError: Access to the file is denied.");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Error: Path contains invalid characters.");
            }
            
            wallClock.Stop();
            PrintStats(totalPuzzles, solvedCount, totalAlgorithmTicks, maxTicks, wallClock.Elapsed.TotalSeconds);
        }
        /// <summary>
        /// Displays the final statistics of the batch run, including timing and success rates.
        /// </summary>
        /// <param name="total">Total number of puzzles processed.</param>
        /// <param name="solved">Number of puzzles successfully solved.</param>
        /// <param name="totalTicks">Accumulated ticks for algorithm execution.</param>
        /// <param name="maxTicks">The longest single solve time in ticks.</param>
        /// <param name="wallTime">Total execution time including I/O.</param>
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
