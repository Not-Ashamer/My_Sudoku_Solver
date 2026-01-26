using System;
namespace SudokuSolver
{
    class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Welcome to my Sudoku Solver!");
            while (true)
            {
                Console.Write("Enter Puzzle: ");
                string input = Console.ReadLine();
                if (input == "exit")
                {
                    Console.WriteLine("Goodbye!");
                    break;
                }

                try
                {
                    var board = new SudokuBoard(input);

                    Console.WriteLine($"Solving {board.size}x{board.size} board...");
                    IO.Print(board);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}