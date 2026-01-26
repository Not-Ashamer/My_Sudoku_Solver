public static class IO
{
    private const string Tokens = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    public static void Print(SudokuBoard board)
    {
        Console.WriteLine($"\n--- {board.size}x{board.size} Solution ---");

        for (int r = 0; r < board.size; r++)
        {
            if (r > 0 && r % board.squareSize == 0)
            {
                PrintHorizontalDivider(board);
            }
            for (int c = 0; c < board.size; c++)
            {
                if (c > 0 && c % board.squareSize == 0)
                {
                    Console.Write("| ");
                }

                int val = board[r, c];

                char display = val == 0 ? ' ' : Tokens[val];

                Console.Write(display + " ");
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }

    private static void PrintHorizontalDivider(SudokuBoard board)
    {
        for (int c = 0; c < board.size; c++)
        {
            if (c > 0 && c % board.squareSize == 0)
            {
                Console.Write("+-");
            }
            Console.Write("--");
        }
        Console.WriteLine();
    }
}