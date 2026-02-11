using System;
using System.Numerics;
using System.IO;

namespace SudokuSolver.SudokuSolver.Core
{
	/// <summary>
	/// Core solver logic using bitwise backtracking and constraint propagation.
	/// Utilizes pre-calculated lookup tables to minimize coordinate calculations during recursion.
	/// </summary>
	public class Solver
    {
		/// <summary>
		/// The side length of the Sudoku board (e.g., 9, 16, 25).
		/// </summary>
		public readonly int size;

		/// <summary>
		/// The side length of the internal sub-grids (e.g., 3 for a 9x9 board).
		/// </summary>
		private readonly int boxSize;

		/// <summary>
		/// Lookup table mapping a flat cell index to its corresponding row index.
		/// </summary>
		private readonly int[] rowByCell;

		/// <summary>
		/// Lookup table mapping a flat cell index to its corresponding column index.
		/// </summary>
		private readonly int[] colByCell;

		/// <summary>
		/// Lookup table mapping a flat cell index to its corresponding sub-grid (box) index.
		/// </summary>
		private readonly int[] boxByCell;

		/// <summary>
		/// A 2D lookup table where cellsInBox[boxIndex] contains the flat indices of all cells in that box.
		/// Used for rapid "Hidden Single" scans within sub-grids.
		/// </summary>
		private readonly int[][] cellsInBox;

		/// <summary>
		/// A bitmask where the first 'size' bits are set to 1. 
		/// Used to determine the set of all possible numbers for a specific board size.
		/// </summary>
		private readonly SolverMask baseMask;

		/// <summary>
		/// Initializes a new instance of the <see cref="Solver"/> class.
		/// Pre-calculates lookup tables for cell coordinates to optimize the backtracking algorithm.
		/// </summary>
		/// <param name="size">The side length of the Sudoku board.</param>
		/// <exception cref="ArgumentException">Thrown if size is not a perfect square.</exception>
		public Solver( int size )
			{
			//Validates all necessary constraints and requirements
			SudokuConfig.ValidateSize(size);
			this.size = size;
			boxSize = (int)Math.Sqrt(size);
			if(boxSize * boxSize != size)
				throw new ArgumentException("Board size must be a perfect square (e.g., 4, 9, 16, 25).");
			// Pre-calculate the bitmask representing all possible values for this board size.
			// Thus preventing shifting bits inside the recursive solver.
			baseMask = size == sizeof(SolverMask) * 8
				? ~(SolverMask)0
				: ((SolverMask)1 << size) - 1;

			// Initialize the lookup tables
			rowByCell = new int[size * size];
			colByCell = new int[size * size];
			boxByCell = new int[size * size];
			cellsInBox = new int[size][];
			for(int i = 0; i < size; i++) cellsInBox[i] = new int[size];

			// Map every flat index to its Row, Column, and Box coordinates once.
			for(int i = 0; i < size * size; i++)
				{
				int row = i / size;
				int col = i % size;

				rowByCell[i] = row;
				colByCell[i] = col;

				// Calculate sub-grid index aka box using integer division
				int b = row / boxSize * boxSize + col / boxSize;
				boxByCell[i] = b;

				// Map which flat index belongs to which position in a specific box
				int internalRow = row % boxSize;
				int internalCol = col % boxSize;
				cellsInBox[b][internalRow * boxSize + internalCol] = i;
				}
			}
		/// <summary>
		/// The main entry point for the solving algorithm. 
		/// Performs memory allocation and triggers the backtracking process.
		/// </summary>
		/// <param name="board">The <see cref="SudokuBoard"/> to be solved. Values are modified in-place.</param>
		/// <returns>True if a valid solution was found; otherwise, false.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the board is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the board size does not match the solver's configuration.</exception>
		public bool Solve( SudokuBoard board )
			{
			if(board == null) throw new ArgumentNullException(nameof(board));
			if(board.size != size) throw new ArgumentException($"Size mismatch: Board {board.size} != Solver {size}");

			// We allocate the bitmasks on the stack instead of the heap. 
			// This is much faster and prevents garbage collection pressure during batch processing.
			Span<SolverMask> rows = stackalloc SolverMask[size];
			Span<SolverMask> cols = stackalloc SolverMask[size];
			Span<SolverMask> boxes = stackalloc SolverMask[size];
			Span<int> emptyIndices = stackalloc int[board.cells.Length];
			int[] cells = board.cells;

			// Initialize masks and check if the starting board is already invalid (violations)
			if(!InitializeBoardState(cells, rows, cols, boxes, emptyIndices, out int countEmpty))
				{
				return false; // Board has pre-existing conflicts (eg: there are immediate discrepancies)
				}

			// Start recursive Backtracking
			return Backtracking(cells, rows, cols, boxes, emptyIndices, countEmpty);
			}

		/// <summary>
		/// Scans the board to identify empty cells and populate initial bitmasks for rows, columns, and boxes.
		/// Validates the initial state for any rule violations.
		/// </summary>
		/// <param name="cells">The flat array of board cell values.</param>
		/// <param name="rows">Span to store used numbers for each row.</param>
		/// <param name="cols">Span to store used numbers for each column.</param>
		/// <param name="boxes">Span to store used numbers for each sub-grid.</param>
		/// <param name="emptyIndices">A buffer to store the flat indices of empty cells.</param>
		/// <param name="countEmpty">The total number of empty cells found.</param>
		/// <returns>True if the initial board is valid; false if a conflict (duplicate) is detected.</returns>
		private bool InitializeBoardState( int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, Span<int> emptyIndices, out int countEmpty )
			{
			countEmpty = 0;
			for(int i = 0; i < cells.Length; i++)
				{
				int val = cells[i];
				if(val == 0)
					{
					// Record empty cell position for the backtracking phase (Optimization)
					emptyIndices[countEmpty++] = i;
					}
				else
					{
					// --- BITMASK LOGIC ---
					// Map the value (e.g., 1-9) to a 0-indexed bit position (e.g., 0-8)
					int bitIndex = val - 1;
					SolverMask bit = (SolverMask)1 << bitIndex;

					// Retrieve pre-calculated coordinates
					int r = rowByCell[i];
					int c = colByCell[i];
					int b = boxByCell[i];

					// Checking: Using Bitwise AND (&) to see if this number's bit is already set in this row, col, or box.
					if((rows[r] & bit) != 0 || (cols[c] & bit) != 0 || (boxes[b] & bit) != 0)
						{
						return false; // Constraint violation: Number already exists in this unit
						}

					// Recording: Using Bitwise OR (|=) to flip the values for this number in its respective masks.
					rows[r] |= bit;
					cols[c] |= bit;
					boxes[b] |= bit;
					}
				}
			return true;
			}

		/// <summary>
		/// The core recursive backtracking algorithm. 
		/// Uses the Minimum Remaining Values heuristic as well as Hidden Single detection to solve the puzzle efficiently.
		/// </summary>
		/// <param name="cells">The flat array representing the board state.</param>
		/// <param name="rows">Bitmasks for used numbers in each row.</param>
		/// <param name="cols">Bitmasks for used numbers in each column.</param>
		/// <param name="boxes">Bitmasks for used numbers in each sub-grid.</param>
		/// <param name="emptyIndices">An array containing indices of all currently empty cells.</param>
		/// <param name="emptyCount">The number of cells remaining to be filled.</param>
		/// <returns>True if the board is successfully solved; otherwise, false.</returns>
		private bool Backtracking( int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, Span<int> emptyIndices, int emptyCount )
			{
			// Base Case: No more empty cells means the puzzle is solved
			if(emptyCount == 0) return true;

			// Phase One: MRV
			// We search for the cell with the fewest possible candidates to minimize the branching factor.
			int bestK = 0;
			int bestCellIndex = emptyIndices[0];
			SolverMask bestCandidates = 0;
			int minOptions = int.MaxValue;

			for(int k = 0; k < emptyCount; k++)
				{
				int i = emptyIndices[k];
				// Calculate possible candidates using bitwise NOT on the combined masks of row, col, and box
				SolverMask candidates = ~(rows[rowByCell[i]] | cols[colByCell[i]] | boxes[boxByCell[i]]) & baseMask;
				int count = BitOperations.PopCount(candidates);

				// If a cell has 0 candidates, this path is a dead end
				if(count == 0) return false;

				if(count < minOptions)
					{
					minOptions = count;
					bestK = k;
					bestCellIndex = i;
					bestCandidates = candidates;
					// Optimization: If we find a Naked Single (a cell with only 1 option), we stop searching immediately
					if(count == 1) break;
					}
				}

			// Phase 2: Hidden Singles
			// If the best cell has more than one option, we check if any number is forced into a specific cell
			// within a unit, which is even more restrictive than MRV.
			// NOTE: This is less efficient on easier puzzles than a blind guess, but speeds up harder puzzles tremendously
			if(minOptions > 1)
				{
				var hidden = FindHiddenSingle(cells, rows, cols, boxes);
				if(hidden.found)
					{
					bestCellIndex = hidden.index;
					bestCandidates = hidden.mask;

					// Update bestK to match the new bestCellIndex for the swap logic below
					for(int k = 0; k < emptyCount; k++)
						{
						if(emptyIndices[k] == bestCellIndex)
							{
							bestK = k;
							break;
							}
						}
					}
				}

			// Move the chosen cell to the "end" of the active emptyIndices range.
			// This allows us to pass 'emptyCount - 1' to the next recursion without re-allocating arrays. (Optimization)
			int lastIndex = emptyCount - 1;
			int swappedVal = emptyIndices[lastIndex];
			emptyIndices[bestK] = swappedVal;
			emptyIndices[lastIndex] = bestCellIndex;

			int r = rowByCell[bestCellIndex];
			int c = colByCell[bestCellIndex];
			int b = boxByCell[bestCellIndex];

			// Phase 3: Recurse
			// Iterate through every possible candidate for the chosen cell
			while(bestCandidates != 0)
				{
				// Get the index of the first set bit (the number to try)
				int bitIndex = BitOperations.TrailingZeroCount(bestCandidates);
				SolverMask bit = (SolverMask)1 << bitIndex;

				// Apply Move
				cells[bestCellIndex] = bitIndex + 1;
				rows[r] |= bit;
				cols[c] |= bit;
				boxes[b] |= bit;

				// Recursion Call
				if(Backtracking(cells, rows, cols, boxes, emptyIndices, emptyCount - 1))
					return true;

				// Backtrack: Undo the move to restore the state for the next candidate or parent call
				cells[bestCellIndex] = 0;
				rows[r] &= ~bit;
				cols[c] &= ~bit;
				boxes[b] &= ~bit;

				// Remove this candidate bit and try the next one
				bestCandidates &= ~bit;
				}

			// Restore the emptyIndices order before returning to maintain consistency in the parent call
			emptyIndices[bestK] = bestCellIndex;
			emptyIndices[lastIndex] = swappedVal;

			return false; // No solution found in this branch, try elsewhere
			}

		/// <summary>
		/// Orchestrates a scan across all rows, columns, and boxes to find a Hidden Single.
		/// A Hidden Single occurs when a number can only fit in one specific cell within a unit, 
		/// even if that cell has other potential candidates (It doesnt appear in any other cell in the group)
		/// </summary>
		/// <param name="cells">The current board state.</param>
		/// <param name="rows">Row bitmasks.</param>
		/// <param name="cols">Column bitmasks.</param>
		/// <param name="boxes">Box bitmasks.</param>
		/// <returns>
		/// A tuple containing:
		/// - index: The flat index of the cell found.
		/// - mask: The bitmask of the forced number.
		/// - found: True if a hidden single was identified.
		/// </returns>
		private (int index, SolverMask mask, bool found) FindHiddenSingle( int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes )
			{
			// Scan all Rows
			for(int r = 0; r < size; r++)
				{
				var result = ScanUnit(cells, rows, cols, boxes, r, UnitType.Row);
				if(result.found) return result;
				}

			// Scan all Columns
			for(int c = 0; c < size; c++)
				{
				var result = ScanUnit(cells, rows, cols, boxes, c, UnitType.Col);
				if(result.found) return result;
				}

			// Scan all Boxes
			for(int b = 0; b < size; b++)
				{
				var result = ScanUnit(cells, rows, cols, boxes, b, UnitType.Box);
				if(result.found) return result;
				}

			// No hidden singles found in any unit
			return (-1, 0, false);
			}
		/// <summary>
		/// Defines the type of Sudoku unit being processed.
		/// Used to redirect index calculations to the correct lookup tables.
		/// Important to maintain SRP
		/// </summary>
		private enum UnitType { Row, Col, Box }

		/// <summary>
		/// Scans a specific row, column, or box to identify "Hidden Singles."
		/// Uses bitwise frequency tracking to find numbers that appear exactly once in the candidates of the unit.
		/// </summary>
		/// <param name="cells">The current board values.</param>
		/// <param name="rows">Row constraint masks.</param>
		/// <param name="cols">Column constraint masks.</param>
		/// <param name="boxes">Box constraint masks.</param>
		/// <param name="unitIdx">The index of the unit (0-8 for a 9x9 board).</param>
		/// <param name="type">The <see cref="UnitType"/> (Row, Col, or Box).</param>
		/// <returns>A tuple indicating the cell index and the bitmask of the forced number if found.</returns>
		private (int index, SolverMask mask, bool found) ScanUnit( int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, int unitIdx, UnitType type )
			{
			// seenOnce: bits representing numbers that appeared at least once as a candidate.
			// seenTwice: bits representing numbers that appeared two or more times as a candidate.
			SolverMask seenOnce = 0, seenTwice = 0;

			// Count frequencies
			for(int i = 0; i < size; i++)
				{
				int cellIdx = GetCellIndex(unitIdx, i, type);
				if(cells[cellIdx] != 0) continue; // Skip already filled cells

				// Determine all legal candidates for this specific cell
				SolverMask candidates = ~(rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & baseMask;

				// Update frequency masks:
				// Any bit that was already in seenOnce and is also in candidates is now "seen twice."
				seenTwice |= seenOnce & candidates;
				// Add current candidates to seenOnce.
				seenOnce |= candidates;
				}

			// A Hidden value is one that appeared in seenOnce but NOT in seenTwice.
			SolverMask hidden = seenOnce & ~seenTwice;
			if(hidden == 0) return (-1, 0, false);

			// Get the lowest set bit (the first hidden single found) using bitwise negation trick.
			SolverMask targetBit = hidden & (SolverMask)(-hidden);

			// Find exactly which cell in the unit can host this forced number.
			for(int i = 0; i < size; i++)
				{
				int cellIdx = GetCellIndex(unitIdx, i, type);
				if(cells[cellIdx] != 0) continue;

				bool isPossible = false;
				// Check if the target number is legal for this specific cell index
				if(type == UnitType.Row)
					{
					if(((cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
					}
				else if(type == UnitType.Col)
					{
					if(((rows[rowByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
					}
				else 
					{
					if(((rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
					}

				if(isPossible) return (cellIdx, targetBit, true);
				}
			return (-1, 0, false);
			}

		/// <summary>
		/// Maps a local index within a specific unit (Row, Col, or Box) to its global flat index on the board.
		/// This allows algorithms to iterate through units without calculating coordinates manually.
		/// </summary>
		/// <param name="unitIdx">The index of the unit (e.g., which row or which box).</param>
		/// <param name="memberIdx">The index of the cell within that unit (0 to size-1).</param>
		/// <param name="type">The <see cref="UnitType"/> being accessed.</param>
		/// <returns>The zero-based flat index used for array access.</returns>
		/// <exception cref="ArgumentException">Thrown if an unsupported UnitType is provided.</exception>
		private int GetCellIndex( int unitIdx, int memberIdx, UnitType type ) => type switch
			{
				// Row-major mapping: (RowIndex * TotalWidth) + ColumnIndex
				UnitType.Row => unitIdx * size + memberIdx,

				// Column-major mapping: (RowIndex * TotalWidth) + ColumnIndex
				// Here, memberIdx acts as the row and unitIdx acts as the column
				UnitType.Col => memberIdx * size + unitIdx,

				// Box mapping: Retrieves the pre-calculated index from the lookup table
				UnitType.Box => cellsInBox[unitIdx][memberIdx],

				_ => throw new ArgumentException("Invalid UnitType")
				};
		}
}
