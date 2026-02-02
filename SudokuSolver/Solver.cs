using System;
using System.Numerics;
using System.IO;
namespace SudokuSolver
{
    public class Solver
    {
        public readonly int size;
        private readonly int boxSize;
        private readonly int[] rowByCell;//maps cell's row
        private readonly int[] colByCell;//maps cell's column
        private readonly int[] boxByCell;//maps a particular cell's position to the box it is in
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
            if (board.size != size)
            {
                throw new ArgumentException($"How did this even happen? Also board size {board.size} != {size}");
            }
            Span<uint> rows = stackalloc uint[this.size];//using span and stackalloc means no garbage collection and no heap memory
            Span<uint> cols = stackalloc uint[this.size];//each index contains all the possibilities of a given row, box or column
            Span<uint> boxes = stackalloc uint[this.size];
            int[] cells = board.cells;
            int countEmpty = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                int val = cells[i];
                if (val == 0)
                {
                    countEmpty++;
                }
                else
                {
                    int bitIndex = val - 1;
                    uint bit = 1u << bitIndex;

                    int r = rowByCell[i];
                    int c = colByCell[i];
                    int b = boxByCell[i];
                    if ((rows[r] & bit) != 0 || (cols[c] & bit) != 0 || (boxes[b] & bit) != 0)
                    {
                        return false;
                    }
                    rows[r] |= bit;
                    cols[c] |= bit;
                    boxes[b] |= bit;
                }
            }
            return Backtracking(cells, rows, cols, boxes, countEmpty);
        }
        private bool Backtracking(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes, int emptyCount)
        {
            if (emptyCount == 0) return true;

            int bestIndex = -1;
            int minOptions = int.MaxValue;
            uint bestCandidates = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != 0) continue;

                int r = rowByCell[i];
                int c = colByCell[i];
                int b = boxByCell[i];

                uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                int count = BitOperations.PopCount(candidates);
                
                if (count == 0) return false;

                if (count < minOptions)
                {
                    minOptions = count;
                    bestIndex = i;
                    bestCandidates = candidates;
                    if (count == 1) goto SkipHidden; 
                }
            }

            if (minOptions > 1)
            {
                var hidden = FindHiddenSingle(cells, rows, cols, boxes);
                if (hidden.found)
                {
                    bestIndex = hidden.index;
                    bestCandidates = hidden.mask;
                }
            }
        SkipHidden:
            int bestR = rowByCell[bestIndex];
            int bestC = colByCell[bestIndex];
            int bestB = boxByCell[bestIndex];

            while (bestCandidates != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bestCandidates);
                uint bit = 1u << bitIndex;

                cells[bestIndex] = bitIndex + 1;
                rows[bestR] |= bit;
                cols[bestC] |= bit;
                boxes[bestB] |= bit;

                if (Backtracking(cells, rows, cols, boxes, emptyCount - 1))
                    return true;
                cells[bestIndex] = 0;
                rows[bestR] &= ~bit;
                cols[bestC] &= ~bit;
                boxes[bestB] &= ~bit;

                bestCandidates &= ~bit;
            }

            return false;
        }
        private (int index, uint mask, bool found) FindHiddenSingle(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes)
        {
            Span<int> counts = stackalloc int[size];
            Span<int> positions = stackalloc int[size];

            for (int r = 0; r < size; r++)
            {
                counts.Clear();

                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;

                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];

                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    while (candidates != 0)
                    {
                        int bit = BitOperations.TrailingZeroCount(candidates);
                        counts[bit]++;
                        positions[bit] = idx; 
                        candidates &= ~(1u << bit);
                    }
                }
                for (int num = 0; num < size; num++)
                {
                    if (counts[num] == 1) return (positions[num], 1u << num, true);
                }
            }

            for (int c = 0; c < size; c++)
            {
                counts.Clear();
                for (int r = 0; r < size; r++)
                {
                    int idx = r * size + c;

                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    while (candidates != 0)
                    {
                        int bit = BitOperations.TrailingZeroCount(candidates);
                        counts[bit]++;
                        positions[bit] = idx;
                        candidates &= ~(1u << bit);
                    }
                }

                for (int num = 0; num < size; num++)
                {
                    if (counts[num] == 1) return (positions[num], 1u << num, true);
                }
            }

            for (int b = 0; b < size; b++)
            {
                counts.Clear();

                for (int k = 0; k < size; k++)
                {
                    int idx = cellsInBox[b][k];

                    if (cells[idx] != 0) continue;

                    int r = rowByCell[idx];
                    int c = colByCell[idx];

                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    while (candidates != 0)
                    {
                        int bit = BitOperations.TrailingZeroCount(candidates);
                        counts[bit]++;
                        positions[bit] = idx;
                        candidates &= ~(1u << bit);
                    }
                }

                for (int num = 0; num < size; num++)
                {
                    if (counts[num] == 1) return (positions[num], 1u << num, true);
                }
            }

            return (-1, 0, false);
        }
    }
}
