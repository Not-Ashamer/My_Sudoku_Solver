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
            Span<int> emptyIndices = stackalloc int[cells.Length];
            int countEmpty = 0;
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
                    if ((rows[r] & bit) != 0 || (cols[c] & bit) != 0 || (boxes[b] & bit) != 0)
                    {
                        return false;
                    }
                    rows[r] |= bit;
                    cols[c] |= bit;
                    boxes[b] |= bit;
                }
            }
            return Backtracking(cells, rows, cols, boxes,emptyIndices, countEmpty);
        }
        private bool Backtracking(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes, Span<int> emptyIndices, int emptyCount)
        {
            if (emptyCount == 0) return true;

            int bestK = -1;         
            int bestCellIndex = -1; 
            int minOptions = int.MaxValue;
            uint bestCandidates = 0;
            for (int k = 0; k < emptyCount; k++)
            {
                int i = emptyIndices[k]; 

                int r = rowByCell[i];
                int c = colByCell[i];
                int b = boxByCell[i];

                uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;
                int count = BitOperations.PopCount(candidates);

                if (count == 0) return false; 

                if (count < minOptions)
                {
                    minOptions = count;
                    bestK = k;
                    bestCellIndex = i;
                    bestCandidates = candidates;

                    if (count == 1) goto SkipHidden; 
                }
            }

            if (minOptions > 1)
            {
                var hidden = FindHiddenSingle(cells, rows, cols, boxes);
                if (hidden.found)
                {
                    bestCellIndex = hidden.index;
                    bestCandidates = hidden.mask;
                    for (int k = 0; k < emptyCount; k++)
                    {
                        if (emptyIndices[k] == bestCellIndex)
                        {
                            bestK = k;
                            break;
                        }
                    }
                    goto SkipHidden;
                }
            }

        SkipHidden:
            int lastIndex = emptyCount - 1;
            int swappedVal = emptyIndices[lastIndex];

            emptyIndices[bestK] = swappedVal;
            emptyIndices[lastIndex] = bestCellIndex; 

            int bestR = rowByCell[bestCellIndex];
            int bestC = colByCell[bestCellIndex];
            int bestB = boxByCell[bestCellIndex];

            while (bestCandidates != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bestCandidates);
                uint bit = 1u << bitIndex;

                cells[bestCellIndex] = bitIndex + 1;
                rows[bestR] |= bit;
                cols[bestC] |= bit;
                boxes[bestB] |= bit;
                if (Backtracking(cells, rows, cols, boxes, emptyIndices, emptyCount - 1))
                    return true;
                cells[bestCellIndex] = 0;
                rows[bestR] &= ~bit;
                cols[bestC] &= ~bit;
                boxes[bestB] &= ~bit;

                bestCandidates &= ~bit;
            }
            emptyIndices[bestK] = bestCellIndex;
            emptyIndices[lastIndex] = swappedVal;

            return false;
        }
        private (int index, uint mask, bool found) FindHiddenSingle(int[] cells, Span<uint> rows, Span<uint> cols, Span<uint> boxes)
        {
            for (int r = 0; r < size; r++)
            {
                uint seenOnce = 0;
                uint seenTwice = 0;
                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;
                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    seenTwice |= seenOnce & candidates;
                    seenOnce |= candidates;
                }

                uint hidden = seenOnce & ~seenTwice;
                if (hidden == 0) continue;

                uint targetBit = (uint)(hidden & -hidden); 
                for (int c = 0; c < size; c++)
                {
                    int idx = r * size + c;
                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    if ((candidates & targetBit) != 0)
                        return (idx, targetBit, true);
                }
            }
            for (int c = 0; c < size; c++)
            {
                uint seenOnce = 0;
                uint seenTwice = 0;

                for (int r = 0; r < size; r++)
                {
                    int idx = r * size + c;
                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    seenTwice |= seenOnce & candidates;
                    seenOnce |= candidates;
                }

                uint hidden = seenOnce & ~seenTwice;
                if (hidden == 0) continue;

                uint targetBit = (uint)(hidden & -hidden);
                for (int r = 0; r < size; r++)
                {
                    int idx = r * size + c;
                    if (cells[idx] != 0) continue;

                    int b = boxByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    if ((candidates & targetBit) != 0)
                        return (idx, targetBit, true);
                }
            }
            for (int b = 0; b < size; b++)
            {
                uint seenOnce = 0;
                uint seenTwice = 0;
                for (int k = 0; k < size; k++)
                {
                    int idx = cellsInBox[b][k];
                    if (cells[idx] != 0) continue;

                    int r = rowByCell[idx];
                    int c = colByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    seenTwice |= seenOnce & candidates;
                    seenOnce |= candidates;
                }

                uint hidden = seenOnce & ~seenTwice;
                if (hidden == 0) continue;

                uint targetBit = ((uint)(hidden & -hidden));
                for (int k = 0; k < size; k++)
                {
                    int idx = cellsInBox[b][k];
                    if (cells[idx] != 0) continue;

                    int r = rowByCell[idx];
                    int c = colByCell[idx];
                    uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                    if ((candidates & targetBit) != 0)
                        return (idx, targetBit, true);
                }
            }

            return (-1, 0, false);
        }
    }
}
