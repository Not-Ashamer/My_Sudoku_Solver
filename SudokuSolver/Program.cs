static void Main()
{
    Console.WriteLine("Welcome to my Sudoku Solver!");
    while (true)
    {
        Console.Write("Enter Puzzle: ");
        string input = Console.ReadLine();
        if (input == "exit") break;

        try
        {
            var board = new SudokuBoard(input);

            Console.WriteLine($"Solving {board.size}x{board.size} board...");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}