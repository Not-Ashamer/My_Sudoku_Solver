using System;

namespace SudokuSolver.SudokuSolver.Core
	{
	/// <summary>
	/// Represents a square Sudoku grid of varying sizes (e.g., 9x9, 16x16).
	/// Responsible for validating input string integrity and providing access to cell data.
	/// </summary>
	public class SudokuBoard
		{
		/// <summary>
		/// The width/height of the board (e.g., 9 for a standard Sudoku).
		/// </summary>
		public int size { get; }

		/// <summary>
		/// The width/height of the internal sub-grids (e.g., 3 for a standard 9x9 Sudoku).
		/// </summary>
		public int squareSize { get; }

		/// <summary>
		/// The flat array representation of the board cells. 
		/// 0 represents an empty cell.
		/// </summary>
		public int[] cells { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SudokuBoard"/> class.
		/// Validates that the input length corresponds to a valid Sudoku size (n^2 x n^2).
		/// </summary>
		/// <param name="input">A single string representing the board (row by row). 
		/// '0' indicates an empty cell. Characters '1'-'9' and 'A'-'Z' represent values.</param>
		/// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
		/// <exception cref="ArgumentException">Thrown if input is empty.</exception>
		/// <exception cref="InvalidSudokuSizeException">Thrown if the input length does not form a valid square grid.</exception>
		public SudokuBoard(string input)
			{
			if (input == null) throw new ArgumentNullException(nameof(input), "Input cannot be null");
			if (string.IsNullOrWhiteSpace(input))
				throw new ArgumentException("Input cannot be empty.");

			int len = input.Length;
			size = (int)Math.Sqrt(len);
			squareSize = (int)Math.Sqrt(size);

			if (size * size != len)
				throw new InvalidSudokuSizeException($"The length {len} is invalid (Must be a perfect square).");
			if (squareSize * squareSize != size)
				throw new InvalidSudokuSizeException($"The size {size}x{size} is invalid (cannot form square boxes).");
			if (size > SudokuConfig.MaxBoardSize)
				throw new InvalidSudokuSizeException($"The size {size}x{size} is invalid (solver only supports sizes up to {SudokuConfig.MaxBoardSize}x{SudokuConfig.MaxBoardSize} due to uint bitmask limits).");
			if (size < 1)
				throw new InvalidSudokuSizeException("Size must be greater than 0.");

			cells = new int[len];
			ParseInput(input);
			}

		/// <summary>
		/// Parses the input string into integer values.
		/// Handles conversion of characters '1'-'9' and 'A'-'Z'.
		/// </summary>
		/// <param name="input">The raw input string.</param>
		private void ParseInput(string input)
			{
			for (int i = 0; i < input.Length; i++)
				{
				char c = char.ToUpper(input[i]);
				if (c == '0')
					{
					cells[i] = 0;
					}
				else
					{
					int val = SudokuConfig.Tokens.IndexOf(c);

					//1: Character completely unknown (e.g. '%')
					if (val == -1)
						throw new InvalidSudokuContentException(c, i);

					//2: Character valid, but too big for this board (e.g. 'A' in 9x9)
					if (val > size)
						throw new InvalidSudokuContentException($"Character '{c}' (value {val}) is too large for a {size}x{size} board.");

					cells[i] = val;
					}
				}
			}

		/// <summary>
		/// Gets or sets the value of a cell at the specified row and column.
		/// </summary>
		/// <param name="row">The zero-based row index.</param>
		/// <param name="col">The zero-based column index.</param>
		/// <returns>The value in the cell (0 for empty).</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if row or col is outside board dimensions.</exception>
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
