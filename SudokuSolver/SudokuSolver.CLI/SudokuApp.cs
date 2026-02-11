using System;

namespace SudokuSolver.SudokuSolver.CLI
	{
	/// <summary>
	/// The main application controller. Handles the primary menu loop, user input selection,
	/// and global state management for cancellation requests.
	/// </summary>
	public class SudokuApp
		{
		/// <summary>
		/// A thread-safe flag indicating if the user has requested a cancellation (via Ctrl+C).
		/// Volatile ensures the value is immediately visible to worker threads (like BatchRunner).
		/// </summary>
		public static volatile bool IsCancelled = false;

		/// <summary>
		/// Runs the main application loop. Displays the menu and assigns execution 
		/// to specific runners (Interactive or Batch) based on user input.
		/// </summary>
		public void Run()
			{
			//Set up the global handler for Ctrl+C to prevent instant termination
			Console.CancelKeyPress += (sender, e) =>
			{
				e.Cancel = true; // Prevent the process from terminating
				IsCancelled = true; // Signal running tasks to stop
				Console.WriteLine("\nStop signal received. Finishing current task...");
			};

			Console.WriteLine("Welcome to my Sudoku Solver!");

			while (true)
				{
				Console.WriteLine("\nSelect Mode:");
				Console.WriteLine("1. Interactive (Type single puzzles)");
				Console.WriteLine("2. Batch (Process a text file)");
				Console.WriteLine("Type 'exit' to quit.");
				Console.Write(">");

				string mode = Console.ReadLine();

				//Handle Ctrl+Z gracefully
				if (mode == null) continue;

				mode = mode.Trim();

				if (mode == "1")
					{
					//Delegates to the Interactive runner
					new InteractiveRunner().Run();
					}
				else if (mode == "2")
					{
					//Delegates to the Batch runner
					new BatchRunner().Run();
					}
				else if (mode == "exit")
					{
					Console.WriteLine("Goodbye!");
					break;
					}
				else
					{
					Console.WriteLine("Invalid selection.");
					}
				}
			}
		}
	}
