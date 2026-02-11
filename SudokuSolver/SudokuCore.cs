global using SolverMask = System.UInt32;
using System;
namespace SudokuSolver
{
    // ==========================================
    // 1. CONFIGURATION
    // ==========================================
    public static class SudokuConfig
    {
        //Calculate the bit-depth (32 or 64) automatically using unsafe size check
        private static readonly int BitDepth = sizeof(SolverMask) * 8;

        /// <summary>
        /// The maximum board size supported.
        /// If 32-bit: Max is 25 (largest square < 32).
        /// If 64-bit: Max is 49 (largest square < 64).
        /// </summary>
        public static readonly int MaxBoardSize = BitDepth == 32 ? 25 : 49;
        public const int ProgressInterval = 1000;
        public static void ValidateSize(int size)
        {
            if (size > MaxBoardSize)
            {
                throw new NotSupportedException(
                    $"Solver is using {BitDepth}-bit logic (SolverMask), which supports sizes up to {MaxBoardSize}x{MaxBoardSize}. " +
                    $"Received request for {size}x{size}.");
            }
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than 0.");
            }
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
