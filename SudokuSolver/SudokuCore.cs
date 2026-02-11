global using SolverMask = System.UInt32;
using System;

namespace SudokuSolver
	{
	// ==========================================
	// 1. CONFIGURATION
	// ==========================================

	/// <summary>
	/// Centralized configuration for the Sudoku Solver.
	/// Handles bit-depth detection, token definitions, and size validation.
	/// </summary>
	public static class SudokuConfig
		{
		/// <summary>
		/// Automatically detects if the SolverMask is 32-bit (uint) or 64-bit (ulong).
		/// </summary>
		private static readonly int BitDepth = sizeof (SolverMask) * 8;

		/// <summary>
		/// The maximum board size supported by the current bit-depth.
		/// 32-bit (uint) supports up to 25x25; 64-bit (ulong) supports up to 49x49.
		/// </summary>
		public static readonly int MaxBoardSize = BitDepth == 32 ? 25 : 49;

		/// <summary>
		/// Determines how often the BatchRunner prints a progress indicator '.' to the console.
		/// </summary>
		public const int ProgressInterval = 1000;

		/// <summary>
		/// The standard character set used for Sudoku values.
		/// '0' is empty, 1-9 and A-Z are values 1-35.
		/// </summary>
		public const string Tokens = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		/// <summary>
		/// Validates that the requested Sudoku size does not exceed the bitwise capacity of the solver.
		/// </summary>
		/// <param name="size">The side length of the board.</param>
		/// <exception cref="NotSupportedException">Thrown if size exceeds bitwise mask limits.</exception>
		public static void ValidateSize ( int size )
			{
			if (size > MaxBoardSize)
				{
				throw new NotSupportedException (
					$"Solver is using {BitDepth}-bit logic (SolverMask), which supports sizes up to {MaxBoardSize}x{MaxBoardSize}. " +
					$"Received request for {size}x{size}.");
				}
			if (size < 1)
				{
				throw new ArgumentOutOfRangeException (nameof (size), "Size must be greater than 0.");
				}
			}
		}

	// ==========================================
	// 2. CUSTOM EXCEPTIONS
	// ==========================================

	/// <summary>
	///Thrown when a puzzle string length is mathematically incompatible with a square Sudoku grid.
	/// Eg: A string of 10 characters cannot form a square board.
	/// </summary>
	public class InvalidSudokuSizeException : Exception
		{
		public InvalidSudokuSizeException ( string message ) : base (message) { }

		/// <summary>
		///Initializes an exception with a standardized message based on the invalid length provided.
		/// </summary>
		/// <param name="actualLength">The character count of the input string.</param>
		public InvalidSudokuSizeException ( int actualLength )
			: base ($"The length {actualLength} is invalid. Board size must be a perfect square of a perfect square.") { }
		}

	/// <summary>
	///Thrown when the board structure is valid, but the characters within are illegal or 
	/// exceed the values allowed for the specific board size.
	/// </summary>
	public class InvalidSudokuContentException : Exception
		{
		/// <summary>
		///The specific character that caused the parsing failure.
		/// </summary>
		public char InvalidChar { get; }

		/// <summary>
		///The zero-based index in the input string where the illegal character was found.
		/// </summary>
		public int Index { get; }

		public InvalidSudokuContentException ( string message ) : base (message) { }

		/// <summary>
		///Initializes a detailed exception identifying the exact character and position of the error.
		/// </summary>
		/// <param name="c">The illegal character.</param>
		/// <param name="index">The position of the character.</param>
		public InvalidSudokuContentException ( char c, int index )
			: base ($"Invalid character '{c}' found at index {index}.")
			{
			InvalidChar = c;
			Index = index;
			}
		}
	}
