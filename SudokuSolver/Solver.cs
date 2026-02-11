using System;
using System.Numerics;
using System.IO;

namespace SudokuSolver
{
    public class Solver
    {
        public readonly int size;
        private readonly int boxSize;
        private readonly int[] rowByCell;
        private readonly int[] colByCell;
        private readonly int[] boxByCell;
        private readonly int[][] cellsInBox;
        private readonly SolverMask baseMask;

        public Solver(int size)
        {
            SudokuConfig.ValidateSize(size);
            this.size = size;
            boxSize = (int)Math.Sqrt(size);

            // Check for perfect square
            if (boxSize * boxSize != size)
                throw new ArgumentException("Size must be a perfect square.");

            // Calculate mask safely for 32 or 64 bit
            baseMask = size == (sizeof(SolverMask) * 8)
                ? ~((SolverMask)0)
                : ((SolverMask)1 << size) - 1;

            rowByCell = new int[size * size];
            colByCell = new int[size * size];
            boxByCell = new int[size * size];
            cellsInBox = new int[size][];
            for (int i = 0; i < size; i++) cellsInBox[i] = new int[size];

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
                cellsInBox[b][internalRow * boxSize + internalCol] = i;
            }
        }

        public bool Solve(SudokuBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (board.size != size) throw new ArgumentException($"Size mismatch: Board {board.size} != Solver {size}");

            Span<SolverMask> rows = stackalloc SolverMask[size];
            Span<SolverMask> cols = stackalloc SolverMask[size];
            Span<SolverMask> boxes = stackalloc SolverMask[size];
            Span<int> emptyIndices = stackalloc int[board.cells.Length];
            int[] cells = board.cells;

            if (!InitializeBoardState(cells, rows, cols, boxes, emptyIndices, out int countEmpty))
            {
                return false;
            }

            return Backtracking(cells, rows, cols, boxes, emptyIndices, countEmpty);
        }

        private bool InitializeBoardState(int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, Span<int> emptyIndices, out int countEmpty)
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
                    SolverMask bit = (SolverMask)1 << bitIndex;

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
            return true;
        }

        private bool Backtracking(int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, Span<int> emptyIndices, int emptyCount)
        {
            if (emptyCount == 0) return true;

            int bestK = 0;
            int bestCellIndex = emptyIndices[0];
            SolverMask bestCandidates = 0;
            int minOptions = int.MaxValue;

            for (int k = 0; k < emptyCount; k++)
            {
                int i = emptyIndices[k];
                SolverMask candidates = ~(rows[rowByCell[i]] | cols[colByCell[i]] | boxes[boxByCell[i]]) & baseMask;
                int count = BitOperations.PopCount(candidates);

                if (count == 0) return false;

                if (count < minOptions)
                {
                    minOptions = count;
                    bestK = k;
                    bestCellIndex = i;
                    bestCandidates = candidates;
                    if (count == 1) break;
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
                }
            }

            int lastIndex = emptyCount - 1;
            int swappedVal = emptyIndices[lastIndex];
            emptyIndices[bestK] = swappedVal;
            emptyIndices[lastIndex] = bestCellIndex;

            int r = rowByCell[bestCellIndex];
            int c = colByCell[bestCellIndex];
            int b = boxByCell[bestCellIndex];

            while (bestCandidates != 0)
            {
                int bitIndex = BitOperations.TrailingZeroCount(bestCandidates);
                SolverMask bit = (SolverMask)1 << bitIndex;

                cells[bestCellIndex] = bitIndex + 1;
                rows[r] |= bit;
                cols[c] |= bit;
                boxes[b] |= bit;

                if (Backtracking(cells, rows, cols, boxes, emptyIndices, emptyCount - 1))
                    return true;

                cells[bestCellIndex] = 0;
                rows[r] &= ~bit;
                cols[c] &= ~bit;
                boxes[b] &= ~bit;

                bestCandidates &= ~bit;
            }

            emptyIndices[bestK] = bestCellIndex;
            emptyIndices[lastIndex] = swappedVal;

            return false;
        }

        private (int index, SolverMask mask, bool found) FindHiddenSingle(int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes)
        {
            for (int r = 0; r < size; r++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, r, UnitType.Row);
                if (result.found) return result;
            }
            for (int c = 0; c < size; c++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, c, UnitType.Col);
                if (result.found) return result;
            }
            for (int b = 0; b < size; b++)
            {
                var result = ScanUnit(cells, rows, cols, boxes, b, UnitType.Box);
                if (result.found) return result;
            }
            return (-1, 0, false);
        }

        private enum UnitType { Row, Col, Box }

        private (int index, SolverMask mask, bool found) ScanUnit(int[] cells, Span<SolverMask> rows, Span<SolverMask> cols, Span<SolverMask> boxes, int unitIdx, UnitType type)
        {
            SolverMask seenOnce = 0, seenTwice = 0;

            for (int i = 0; i < size; i++)
            {
                int cellIdx = GetCellIndex(unitIdx, i, type);
                if (cells[cellIdx] != 0) continue;

                SolverMask candidates = ~(rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & baseMask;
                seenTwice |= (seenOnce & candidates);
                seenOnce |= candidates;
            }

            SolverMask hidden = seenOnce & ~seenTwice;
            if (hidden == 0) return (-1, 0, false);

            SolverMask targetBit = hidden & (SolverMask)(-(long)hidden);

            for (int i = 0; i < size; i++)
            {
                int cellIdx = GetCellIndex(unitIdx, i, type);
                if (cells[cellIdx] != 0) continue;

                bool isPossible = false;
                if (type == UnitType.Row)
                {
                    if (((cols[colByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
                }
                else if (type == UnitType.Col)
                {
                    if (((rows[rowByCell[cellIdx]] | boxes[boxByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
                }
                else
                {
                    if (((rows[rowByCell[cellIdx]] | cols[colByCell[cellIdx]]) & targetBit) == 0) isPossible = true;
                }

                if (isPossible) return (cellIdx, targetBit, true);
            }
            return (-1, 0, false);
        }

        private int GetCellIndex(int unitIdx, int memberIdx, UnitType type) => type switch
        {
            UnitType.Row => unitIdx * size + memberIdx,
            UnitType.Col => memberIdx * size + unitIdx,
            UnitType.Box => cellsInBox[unitIdx][memberIdx],
            _ => throw new ArgumentException("Invalid UnitType")
        };
    }
}