using System;
using SudokuSolver;
namespace SudokuSolver
{
    public class SudokuBoard
    {
        public int size { get; }
        public int squareSize { get; }
        public int[] cells { get; }
        /// <summary>
        /// This constructor makes sure the acquired size is valid for a proper sudoku board by 
        /// size (must some natural number raised to the fourth power)
        /// </summary>
        /// <param name="input"></param>
        /// <exception cref="ArgumentException"></exception>
        public SudokuBoard(string input)
        {

            if (input == null) throw new ArgumentNullException(nameof(input), "Input cannot be null");
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Input cannot be empty.");
            int len = input.Length;
            size = (int)Math.Sqrt(len);
            squareSize = (int)Math.Sqrt(size);
            if (size * size != len)
                throw new InvalidSudokuSizeException($"The length {len} is invalid (Must be a natural number raised to fourth power).");
            if (squareSize * squareSize != size)
                throw new InvalidSudokuSizeException($"The size {size}x{size} is invalid (cannot form square boxes).");
            if (size > SudokuConfig.MaxBoardSize)
                throw new InvalidSudokuSizeException($"The size {size}x{size} is invalid (solver only supports sizes up to 32x32 due to uint bitmask limits).");
            if (size < 1)
                throw new InvalidSudokuSizeException("Size must be greater than 0.");
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

                    // Case 1: Character completely unknown (e.g. '%')
                    if (val == -1)
                        throw new InvalidSudokuContentException(c, i);

                    // Case 2: Character valid, but too big for this board (e.g. 'A' in 9x9)
                    if (val > size)
                        throw new InvalidSudokuContentException($"Character '{c}' (value {val}) is too large for a {size}x{size} board.");

                    cells[i] = val;
                }
            }
        }
        public int this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= size || col < 0 || col >= size)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Cell ({row}, {col}) is out of bounds for a {size}x{size} board.");
                }
                return cells[row * size + col];
            }
            set
            {
                if (row < 0 || row >= size || col < 0 || col >= size)
                {
                    throw new ArgumentOutOfRangeException(
                        $"Cell ({row}, {col}) is out of bounds for a {size}x{size} board.");
                }
                cells[row * size + col] = value;
            }
        }
    }
}