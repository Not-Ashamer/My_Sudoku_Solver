using System;
using System.Numerics;
using System.IO;
namespace SudokuSolver
{
    public class Solver
    {
        public readonly int size;
        private readonly int boxSize;
        private readonly int[] rowByCell;//Maps cell's row
        private readonly int[] colByCell;//Maps cell's column
        private readonly int[] boxByCell;//Maps a particular cell's position to the box it is in
        private readonly int[][] cellsInBox;
        private readonly uint baseMask;

        public Solver(int size)
        {

            this.size = size;
            boxSize = (int)Math.Sqrt(size);
            baseMask = (1u << size) - 1;
            rowByCell = new int[size * size];
            colByCell = new int[size * size];
            boxByCell = new int[size * size];
            cellsInBox = new int[size][];
            for (int i = 0; i < size; i++)
            {
                cellsInBox[i] = new int[size];
            }
            for (int i = 0; i < size * size; i++)
            {
                int row = i / size;
                int col = i % size;
                rowByCell[i] = row;
                colByCell[i] = col;
                int b = (row / boxSize) * boxSize + col / boxSize;
                boxByCell[i] = b;
                int internalRow = row % boxSize;
                int internalCol = col % boxSize;
                int indexInBox = internalRow * boxSize + internalCol;

                cellsInBox[b][indexInBox] = i;
            }
        }
        public bool Solve(SudokuBoard board)
        {
            if (board.size != size) throw new ArgumentException($"Size mismatch: Board {board.size} != Solver {size}");
            Span<uint> rows = stackalloc uint[size];
            Span<uint> cols = stackalloc uint[size];
            Span<uint> boxes = stackalloc uint[size];
            Span<int> emptyIndices = stackalloc int[board.cells.Length];
            int[] cells = board.cells;
            if (!InitializeBoardState(cells, rows, cols, boxes, emptyIndices, out int countEmpty))
            {
                return false; //Board was already invalid
            }

            return Backtracking(cells, rows, cols, boxes, emptyIndices, countEmpty);
        }
        private bool InitializeBoardState(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes, Span<int> emptyIndices, out int countEmpty)
        {
            countEmpty = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                int val = cells[i];
                if (val == 0)
                {
                    emptyIndices[countEmpty++] = i;
                }
                else
                {
                    int bitIndex = val - 1;
                    uint bit = 1u << bitIndex;
                    int r = rowByCell[i];
                    int c = colByCell[i];
                    int b = boxByCell[i];

                    //Check for pre-existing conflicts
                    if ((rows[r] & bit) != 0 || (cols[c] & bit) != 0 || (boxes[b] & bit) != 0)
                    {
                        return false;
                    }
                    rows[r] |= bit;
                    cols[c] |= bit;
                    boxes[b] |= bit;
                }
            }
            return true;
        }
        private bool Backtracking(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes, Span<int> emptyIndices, int emptyCount)
        {
            if (emptyCount == 0) return true;

            //Find the best cell using MRV (Naked Single / Most Restricted)
            int bestK = 0;
            int bestCellIndex = emptyIndices[0];
            uint bestCandidates = 0;
            int minOptions = int.MaxValue;

            for (int k = 0; k < emptyCount; k++)
            {
                int i = emptyIndices[k];
                uint candidates = ~(rows[rowByCell[i]] | cols[colByCell[i]] | boxes[boxByCell[i]]) & baseMask;
                int count = BitOperations.PopCount(candidates);

                if (count == 0) return false; //Dead end, go back

                if (count < minOptions)
                {
                    minOptions = count;
                    bestK = k;
                    bestCellIndex = i;
                    bestCandidates = candidates;
                    if (count == 1) break; //Found a Naked Single, stop searching
                }
            }

            //If no Naked Single was found, look for a Hidden Single
            if (minOptions > 1)
            {
                var hidden = FindHiddenSingle(cells, rows, cols, boxes);
                if (hidden.found)
                {
                    bestCellIndex = hidden.index;
                    bestCandidates = hidden.mask;
                    //Update bestK so the swap logic moves the correct index
                    for (int k = 0; k < emptyCount; k++)
                    {
                        if (emptyIndices[k] == bestCellIndex)
                        {
                            bestK = k;
                            break;
                        }
                    }
                }
            }

            //Swap current best cell to the end of the "empty" list
            int lastIndex = emptyCount - 1;
            int swappedVal = emptyIndices[lastIndex];
            emptyIndices[bestK] = swappedVal;
            emptyIndices[lastIndex] = bestCellIndex;

            int r = rowByCell[bestCellIndex];
            int c = colByCell[bestCellIndex];
            int b = boxByCell[bestCellIndex];

            //Try candidates
            while (bestCandidates != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bestCandidates);
                uint bit = 1u << bitIndex;

                cells[bestCellIndex] = bitIndex + 1;
                rows[r] |= bit;
                cols[c] |= bit;
                boxes[b] |= bit;

                if (Backtracking(cells, rows, cols, boxes, emptyIndices, emptyCount - 1))
                    return true;

                //Backtrack 
                cells[bestCellIndex] = 0;
                rows[r] &= ~bit;
                cols[c] &= ~bit;
                boxes[b] &= ~bit;

                bestCandidates &= ~bit;
            }

            //Restore emptyIndices order before returning up the stack
            emptyIndices[bestK] = bestCellIndex;
            emptyIndices[lastIndex] = swappedVal;

            return false;
        }
        private (int index, uint mask, bool found) FindHiddenSingle(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes)
        {
            //Check Rows
            for (int r = 0; r < size; r++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, r, UnitType.Row);
                if (result.found) return result;
            }
            //Check Columns
            for (int c = 0; c < size; c++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, c, UnitType.Col);
                if (result.found) return result;
            }
            //Check Boxes
            for (int b = 0; b < size; b++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, b, UnitType.Box);
                if (result.found) return result;
            }
            return (-1, 0, false);
        }

        private enum UnitType { Row, Col, Box }//Saves on redundant code in hidden single loops

        private (int index, uint mask, bool found) ScanUnit(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes, int unitIdx, UnitType type)
        {
            uint seenOnce = 0, seenTwice = 0;

            for (int i = 0; i < size; i++)
            {
                int cellIdx = GetCellIndex(unitIdx, i, type);
                if (cells[cellIdx] != 0) continue;

                uint candidates = ~(rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & baseMask;
                seenTwice |= (seenOnce & candidates);
                seenOnce |= candidates;
            }

            uint hidden = seenOnce & ~seenTwice;
            if (hidden != 0)
            {
                uint targetBit = hidden & (uint)-(int)hidden; //Isolated lowest set bit
                for (int i = 0; i < size; i++)
                {
                    int cellIdx = GetCellIndex(unitIdx, i, type);
                    if (cells[cellIdx] != 0) continue;

                    uint candidates = ~(rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & baseMask;
                    if ((candidates & targetBit) != 0) return (cellIdx, targetBit, true);
                }
            }
            return (-1, 0, false);
        }

        private int GetCellIndex(int unitIdx, int memberIdx, UnitType type) => type switch
        {
            UnitType.Row => unitIdx * size + memberIdx,
            UnitType.Col => memberIdx * size + unitIdx,
            UnitType.Box => cellsInBox[unitIdx][memberIdx],
            _ => 0
        };
    }
}
