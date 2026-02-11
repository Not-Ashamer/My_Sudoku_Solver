namespace SudokuSolver
	{
	/// <summary>
	/// The entry point for the Sudoku Solver application.
	/// Responsible for initializing and starting the main application controller.
	/// </summary>
	class Program
		{
		/// <summary>
		/// Main execution method. Boots the <see cref="SudokuApp"/>.
		/// </summary>
		/// <param name="args">Command-line arguments (currently unused).</param>
		static void Main( string[] args )
			{
			var app = new SudokuApp();
			app.Run();
			}
		}
	}
