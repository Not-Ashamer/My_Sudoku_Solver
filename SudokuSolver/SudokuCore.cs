using System;

namespace SudokuSolver
{
    // ==========================================
    // 1. CONFIGURATION
    // ==========================================
    public static class SudokuConfig
    {
        /// <summary>
        /// The integer type used for bitmasks. 
        /// </summary>
        public const string BitType = "uint";

        /// <summary>
        /// The maximum board size (width/height) supported by the current BitType.
        /// </summary>
        public static readonly int MaxBoardSize = BitType == "uint" ? 25 : 49;

        public static void ValidateSize(int size)
        {
            if (size > MaxBoardSize)
            {
                throw new NotSupportedException(
                    $"Solver is configured for '{BitType}' which supports a maximum size of {MaxBoardSize}x{MaxBoardSize}. " +
                    $"Received request for {size}x{size}.");
            }
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than 0.");
            }
        }
    }

    // ==========================================
    // 2. CUSTOM EXCEPTIONS
    // ==========================================

    /// <summary>
    /// Thrown when the board string length is invalid (e.g., length 10 or 36).
    /// </summary>
    public class InvalidSudokuSizeException : Exception
    {
        public InvalidSudokuSizeException(string message) : base(message) { }

        public InvalidSudokuSizeException(int actualLength)
            : base($"The length {actualLength} is invalid. Board size must be a perfect square of a perfect square.") { }
    }

    /// <summary>
    /// Thrown when the board structure is valid, but contains illegal characters (e.g., '?' or 'Z' in 9x9).
    /// </summary>
    public class InvalidSudokuContentException : Exception
    {
        public char InvalidChar { get; }
        public int Index { get; }

        public InvalidSudokuContentException(string message) : base(message) { }

        public InvalidSudokuContentException(char c, int index)
            : base($"Invalid character '{c}' found at index {index}.")
        {
            InvalidChar = c;
            Index = index;
        }
    }
}