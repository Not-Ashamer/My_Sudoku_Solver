using System;

namespace SudokuSolver
{
    public class SudokuApp
    {
        public void Run()
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
                    // Delegate to the Interactive runner
                    new InteractiveRunner().Run();
                }
                else if (mode == "2")
                {
                    // Delegate to the Batch runner
                    new BatchRunner().Run();
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
    }
}