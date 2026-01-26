public class SudokuBoard
{
    public int size { get; }
    public int squareSize { get; }
    public int[] cells { get; }

    public SudokuBoard(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be empty.");
        int len = input.Length;
        size = (int)Math.Sqrt(len);
        squareSize = (int)Math.Sqrt(size);
        if (size * size != len)
            throw new ArgumentException($"The length {len} is invalid. Must be a square number.");
        if (squareSize * squareSize != size)
            throw new ArgumentException($"The size {size}x{size} is invalid (cannot form square boxes).");
        cells = new int[len];
        ParseInput(input);
    }

    private void ParseInput(string input)
    {
        const string tokens = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        for (int i = 0; i < input.Length; i++)
        {
            char c = char.ToUpper(input[i]);

            if (c == '0')
            {
                cells[i] = 0;
            }
            else
            {
                int val = tokens.IndexOf(c);
                if (val == -1 || val > size)
                    throw new ArgumentException($"Invalid character '{c}' at index {i}.");

                cells[i] = val;
            }
        }
    }
    public int this[int row, int col]
    {
        get => cells[row * size + col];
        set => cells[row * size + col] = value;
    }
}