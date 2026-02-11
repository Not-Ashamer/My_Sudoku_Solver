using System;
using System.Text;
using SudokuSolver.SudokuSolver.Core;

/// <summary>
/// Provides utility methods for formatting Sudoku boards for console display 
/// and converting board states back into standardized string formats.
/// </summary>
public static class IO
	{
	/// <summary>
	/// Prints a visually formatted representation of the Sudoku board to the console.
	/// Includes sub-grid dividers and uses <see cref="SudokuConfig.Tokens"/> for character mapping.
	/// </summary>
	/// <param name="board">The <see cref="SudokuBoard"/> instance to display.</param>
	public static void Print ( SudokuBoard board )
		{
		Console.WriteLine ($"\n--- {board.size}x{board.size} Solution ---");

		for (int r = 0; r < board.size; r++)
			{
			// Print horizontal sub-grid divider (e.g., after row 3 and 6 in a 9x9)
			if (r > 0 && r % board.squareSize == 0)
				{
				PrintHorizontalDivider (board);
				}

			for (int c = 0; c < board.size; c++)
				{
				// Print vertical sub-grid divider
				if (c > 0 && c % board.squareSize == 0)
					{
					Console.Write ("| ");
					}

				int val = board[r, c];

				// Use the centralized tokens from SudokuConfig for character mapping
				char display = val == 0 ? ' ' : SudokuConfig.Tokens[val];

				Console.Write (display + " ");
				}
			Console.WriteLine ();
			}
		Console.WriteLine ();
		}

	/// <summary>
	/// Helper method to print a decorative horizontal line separating Sudoku sub-grids.
	/// </summary>
	/// <param name="board">The board context used to calculate the divider length.</param>
	private static void PrintHorizontalDivider ( SudokuBoard board )
		{
		for (int c = 0; c < board.size; c++)
			{
			if (c > 0 && c % board.squareSize == 0)
				{
				Console.Write ("+-");
				}
			Console.Write ("--");
			}
		Console.WriteLine ();
		}

	/// <summary>
	/// Converts the current state of a <see cref="SudokuBoard"/> into a flat string representation.
	/// </summary>
	/// <param name="board">The board to convert.</param>
	/// <returns>A string of tokens corresponding to the board's cell values.</returns>
	public static string ReceiveAsString ( SudokuBoard board )
		{
		StringBuilder sb = new StringBuilder (board.cells.Length);
		foreach (int val in board.cells)
			{
			// Append the token corresponding to the numeric cell value
			sb.Append (SudokuConfig.Tokens[val]);
			}
		return sb.ToString ();
		}
	}
