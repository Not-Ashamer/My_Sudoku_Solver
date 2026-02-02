using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SudokuSolver
{
    public class Solver
    {
        private readonly int size;
        private readonly int boxSize;
        private readonly int[] boxByCell;//maps a particular cell's position to the box it is in
        private readonly uint baseMask;
        public Solver(int size)
        {
            this.size = size;
            boxSize = (int)Math.Sqrt(size);
            baseMask = (1u << size) - 1;
            boxByCell = new int[size * size];
            for (int i = 0; i < boxByCell.Length; i++)
            {
                int row = i / size, col = i % size;
                boxByCell[i] = (row / boxSize) * boxSize + col / boxSize;
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

                    int r = i / size;
                    int c = i % size;
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

                int r = i / size;
                int c = i % size;
                int b = boxByCell[i];

                uint candidates = ~(rows[r] | cols[c] | boxes[b]) & baseMask;

                int count = BitOperations.PopCount(candidates);

                if (count == 0) return false;

                if (count < minOptions)
                {
                    minOptions = count;
                    bestIndex = i;
                    bestCandidates = candidates;
                    if (count == 1) break;
                }
            }
            int bestR = bestIndex / size;
            int bestC = bestIndex % size;
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
    }
}
