using Microsoft.VisualStudio.TestTools.UnitTesting;
using SudokuSolver.SudokuSolver.Core;
using System;

namespace SudokuSolver.Tests
{
    [TestClass]
    public class SudokuTests
    {
        // --- GOOD PATH TESTS ---

        [TestMethod]
        public void Constructor_ValidEmpty9x9_CreatesBoard()
        {
            string input = new string('0', 81);
            var board = new SudokuBoard(input);

            Assert.AreEqual(9, board.size);
            Assert.AreEqual(3, board.squareSize);
            Assert.AreEqual(81, board.cells.Length);
            Assert.AreEqual(0, board.cells[0]);
        }

        [TestMethod]
        public void Constructor_ValidPartial9x9_ParsesNumbers()
        {
            char[] chars = new char[81];
            Array.Fill(chars, '0');
            chars[0] = '1';
            chars[80] = '9';
            string input = new string(chars);

            var board = new SudokuBoard(input);

            Assert.AreEqual(1, board.cells[0]);
            Assert.AreEqual(9, board.cells[80]);
        }

        // --- INVALID CONTENT TESTS (Using your custom exceptions) ---

        [TestMethod]
        public void Constructor_ValueTooLargeFor9x9_ThrowsException()
        {
            string input = "A" + new string('0', 80);

            var ex = Assert.Throws<InvalidSudokuContentException>(() => new SudokuBoard(input));

            StringAssert.Contains(ex.Message, "too large");
        }

        [TestMethod]
        public void Constructor_InvalidSymbol_ThrowsException()
        {
            string input = "!" + new string('0', 80);

            var ex = Assert.Throws<InvalidSudokuContentException>(() => new SudokuBoard(input));

            StringAssert.Contains(ex.Message, "Invalid character '!'");
        }

        // --- STRUCTURAL VALIDATION TESTS ---

        [TestMethod]
        public void Constructor_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new SudokuBoard(null));
        }

        [TestMethod]
        public void Constructor_Empty_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(() => new SudokuBoard(""));
            StringAssert.Contains(ex.Message, "Input cannot be empty");
        }

        [TestMethod]
        public void Constructor_InvalidLength_NotSquareNumber_ThrowsException()
        {
            //Length 10 is bad
            string input = "0123456789";

            var ex = Assert.Throws<InvalidSudokuSizeException>(() => new SudokuBoard(input));
            StringAssert.Contains(ex.Message, "invalid");
        }

        [TestMethod]
        public void Constructor_InvalidDimensions_SizeNotSquare_ThrowsException()
        {
            //Length 36 -> Size 6. Sqrt(6) is irrational.
            string input = new string('0', 36);

            var ex = Assert.Throws<InvalidSudokuSizeException>(() => new SudokuBoard(input));
            StringAssert.Contains(ex.Message, "cannot form square boxes");
        }

        // --- OTHER BOARD SIZES ---

        [TestMethod]
        public void Constructor_Valid4x4String_CreatesBoardCorrectly()
        {
            string input = "1020304010203040";
            var board = new SudokuBoard(input);

            Assert.AreEqual(4, board.size);
            Assert.AreEqual(2, board.squareSize);
            Assert.AreEqual(1, board.cells[0]);
        }

        [TestMethod]
        public void Constructor_Valid16x16String_HandlesHexValues()
        {
            string input = new string('0', 255) + "G";
            var board = new SudokuBoard(input);

            Assert.AreEqual(16, board.size);
            Assert.AreEqual(4, board.squareSize);
            Assert.AreEqual(16, board.cells[255]);
        }

        // --- INDEXER TESTS ---

        [TestMethod]
        public void Indexer_GetAndSet_WorksCorrectly()
        {
            string input = new string('0', 81);
            var board = new SudokuBoard(input);

            board[0, 1] = 5;

            Assert.AreEqual(5, board.cells[1]);
            Assert.AreEqual(5, board[0, 1]);
        }

        [TestMethod]
        public void Indexer_OutOfBounds_ThrowsException()
        {
            string input = new string('0', 81);
            var board = new SudokuBoard(input);

            //Check Set
            Assert.Throws<ArgumentOutOfRangeException>(() => board[0, 9] = 1);

            //Check Get
            Assert.Throws<ArgumentOutOfRangeException>(() => { var x = board[9, 0]; });
        }

        [TestMethod]//All these puzzles taken from https://www.kaggle.com/datasets/rohanrao/sudoku
        [DataRow("070000043040009610800634900094052000358460020000800530080070091902100005007040802", "679518243543729618821634957794352186358461729216897534485276391962183475137945862")]
        [DataRow("048301560360008090910670003020000935509010200670020010004002107090100008150834029", "748391562365248791912675483421786935589413276673529814834962157296157348157834629")]
        [DataRow("310450900072986143906010508639178020150090806004003700005731009701829350000645010", "318457962572986143946312578639178425157294836284563791425731689761829354893645217")]
        [DataRow("007300054245080900003040070070960000000020760000801002008294016609108020000007003", "867319254245786931913542678472963185381425769596871342738294516659138427124657893")]
        [DataRow("165293004000001632023060090009175000500900018002030049098000006000000950000429381", "165293874974851632823764195489175263536942718712638549398517426241386957657429381")]
        [DataRow("000003610000015007000008090086000700030800100500120309005060904060900530403701008", "728493615349615827651278493186539742932847156574126389815362974267984531493751268")]
        [DataRow("405001068073628500009003070240790030006102005950000021507064213080217050612300007", "425971368173628594869543172241795836736182945958436721597864213384217659612359487")]
        [DataRow("862000500070254860300986001280093417000100086000468005037805190020017654410029730", "862731549179254863354986271286593417543172986791468325637845192928317654415629738")]
        [DataRow("070195326001862475060000180053709600900630000002581703000906000036270004029000860", "478195326391862475265347189853729641917634258642581793784916532536278914129453867")]
        [DataRow("671405093003680745000000010002000501068310420100000006456902108010078200200160059", "671425893923681745845793612792846531568319427134257986456932178319578264287164359")]
        [DataRow("073060000654000903000005060507201800360047591080056302000190280905003004148502039", "273469158654718923819325467597231846362847591481956372736194285925683714148572639")]
        [DataRow("007650423030004100041209758403100870095800300008060901304000007059376200200000539", "987651423532784196641239758463192875195847362728563941314925687859376214276418539")]
        [DataRow("700810006800436702000020090003901074000004000009000861002340900300002605000075040", "725819436891436752634527198283961574156784329479253861562348917347192685918675243")]
        [DataRow("408963200563002019002540800201390048836025070974608020609074530140239706020056000", "418963257563782419792541863251397648836425971974618325689174532145239786327856194")]
        [DataRow("920780640040036000068200300004002063800460010050903704000390128000000400000100906", "923781645741536892568249371194872563837465219256913784675394128319628457482157936")]
        [DataRow("280469000006002008000000200190235060050700010048910700000548127801600400500001000", "285469371436172598719853246197235864652784913348916752963548127821697435574321689")]
        [DataRow("402030000003900050506010900060000700030000520259403061010005298908062075300800410", "492536187173928654586714932861259743734681529259473861617345298948162375325897416")]
        [DataRow("950070260068102057000500098006205809007400300280007500000000724600001985045020600", "951874263368192457724563198416235879597486312283917546139658724672341985845729631")]
        [DataRow("470100000619800003302907816521793004806420001030610290247300560060009030003064007", "478136952619852743352947816521793684896425371734618295247381569165279438983564127")]
        [DataRow("630502100400000736900070405290800004001000200500200003109005870000000000050029000", "637542198425198736918673425296837514381954267574216983149365872762481359853729641")]
        [DataRow("746000009308400070129600000000000002932000617004010090090080004607094203400200060", "746351829358429176129678435871963542932845617564712398293186754617594283485237961")]
        [DataRow("000701053040005010000800200530108000962504000000209340406000009103000520200000634", "628791453349625718751843296534178962962534871817269345476352189193486527285917634")]
        [DataRow("003005264609802000010000900096000730180600095000050000960030000005080019030209546", "873195264659842173214367958596428731182673495347951682961534827425786319738219546")]
        [DataRow("020000100050000080800435006600927400002000007907040021000298000290061040178000902", "326879154754612389819435276681927435542183697937546821465298713293761548178354962")]
        public void Solve_GivenPuzzle_ReturnsCorrectSolution(string inputString, string expectedSolution)
        {
            var board = new SudokuBoard(inputString);
            var solver = new Solver(board.size);

            // Act
            bool success = solver.Solve(board);

            //1: The solver should claim success
            Assert.IsTrue(success, $"Solver returned false for valid puzzle: {inputString}");

            //2: The board's cells should match the expected solution string
            string actualSolution = IO.ReceiveAsString(board);

            Assert.AreEqual(expectedSolution, actualSolution, "The solved board does not match the known answer.");
        }
        [TestMethod]
        public void Solve_UnsolvablePuzzle_ReturnsFalse()
        {
            //This puzzle has a conflict (two 5s in the top row)
            string input = "500050000" + new string('0', 72);

            var board = new SudokuBoard(input);
            var solver = new Solver(board.size);

            bool result = solver.Solve(board);

            Assert.IsFalse(result, "Solver should return false for an impossible board.");
        }
        [TestMethod]
        public void Solve_ConsecutivePuzzles_ResetsStateCorrectly()
        {
            //Makes sure the batch solver cleans up properly
            var solver = new Solver(9);

            //Solve a valid puzzle
            var board1 = new SudokuBoard("003020600900305001001806400008102900700000008006708200002609500800203009005010300");
            Assert.IsTrue(solver.Solve(board1), "First puzzle failed.");

            //Immediately solve another valid puzzle without resetting solver instance
            var board2 = new SudokuBoard("405001068073628500009003070240790030006102005950000021507064213080217050612300007");
            bool result = solver.Solve(board2);

            Assert.IsTrue(result, "Second puzzle failed. The solver didn't clear its internal state.");
        }
        [TestMethod]
        public void Solver_SizeExceedsConfigLimit_ThrowsException()
        {
            //36x36 is a valid perfect square, but it exceeds the 25 limit of uint bitmasks
            int size = 36;

            //Expect NotSupportedException from SudokuConfig, not just ArgumentException
            var ex = Assert.Throws<NotSupportedException>(() => new Solver(size));

            StringAssert.Contains(ex.Message, $"Solver is using 32-bit logic (SolverMask), which supports sizes up to 25x25. ");
        }
        [TestMethod]
        public void Solve_BoardWithPreExistingConflict_ReturnsFalseImmediately()
        {
            //A board with two '1's in the first row (invalid state)
            string input = "11" + new string('0', 79);

            var board = new SudokuBoard(input);
            var solver = new Solver(board.size);

            bool result = solver.Solve(board);

            Assert.IsFalse(result, "Solver should immediately reject a board that violates Sudoku rules.");
        }
        [TestMethod]
        public void Solve_AlreadySolvedBoard_ReturnsTrue()
        {
            // A complete solution
            string solved = "483921657967345821251876493548132976729564138136798245372689514814253769695417382";

            var board = new SudokuBoard(solved);
            var solver = new Solver(board.size);

            bool result = solver.Solve(board);

            Assert.IsTrue(result, "Solver should accept an already solved board.");
            //Verify the board didn't change
            Assert.AreEqual(solved, IO.ReceiveAsString(board));
        }
        [TestMethod]
        public void Solve_ParallelExecution_IsThreadSafe()
        {
            // This test runs 100 solvers simultaneously on different threads.
            // Because our Solver uses 'stackalloc' (stack memory), it should be 
            // completely thread-safe without any locks.

            string puzzle = "003020600900305001001806400008102900700000008006708200002609500800203009005010300";
            string answer = "483921657967345821251876493548132976729564138136798245372689514814253769695417382";

            //Run 100 iterations in parallel
            System.Threading.Tasks.Parallel.For(0, 100, i =>
            {
                //Each thread gets its OWN board copy 
                var boardCopy = new SudokuBoard(puzzle);
                var solver = new Solver(boardCopy.size);
                bool success = solver.Solve(boardCopy);
                Assert.IsTrue(success, $"Thread {i} failed to solve.");
                Assert.AreEqual(answer, IO.ReceiveAsString(boardCopy));
            });
        }
    }
}