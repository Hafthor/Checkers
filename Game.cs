public class Game {
    // TODO: Multiple jumps
    public bool isWhiteTurn = true;
    private char[,] board = new char[8, 8]; // 8x8 board with x(black) and o(white) - capital letters are kings

    public Game() {
        //       abcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefghabcdefgh
        var b = ".o.o.o.oo.o.o.o..o.o.o.o................x.x.x.x..x.x.x.xx.x.x.x.";
        for (int r = 0, i = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                board[r, c] = b[i++];
    }

    public Game(Game save) => Restore(save);

    public void Restore(Game save) {
        Array.Copy(save.board, board, 64);
        isWhiteTurn = save.isWhiteTurn;
    }

    public override string ToString() {
        char[] chars = new char[64 + 8];
        for (int r = 0, i = 0; r < 8; r++) {
            for (int c = 0; c < 8; c++)
                chars[i++] = board[r, c];
            chars[i++] = '\n';
        }
        return new string(chars);
    }

    public List<char> Move(List<(int row, int col)> moves) {
        if (!MoveLegal(moves, isWhiteTurn)) throw new Exception("Illegal move");
        var captures = MoveInternal(moves);
        isWhiteTurn = !isWhiteTurn;
        return captures;
    }

    public static bool IsWhite(char piece) => piece is 'o' or 'O';
    public static bool IsKing(char piece) => piece is 'O' or 'X';

    public static string MoveString(List<(int row, int col)> moves) =>
        string.Join("-", moves.Select(m => RC(m.row, m.col)));

    public static List<(int row, int col)> ParseMove(string cmd) => 
        cmd.Split("-").Select(s => RC(s)).ToList();

    public bool MoveLegal(List<(int row, int col)> moves, bool isWhite) {
        var from = moves[0];
        var piece = board[from.row, from.col];
        if (piece == '.' || IsWhite(piece) != isWhite) return false;
        bool mustCapture = moves.Count > 2;
        var prev = from;
        HashSet<(int row, int col)> visited = new();
        visited.Add(from);
        for (int i = 1; i < moves.Count; prev=moves[i++]) {
            var to = moves[i];
            if (to.row is < 0 or >= 8 || to.col is < 0 or >= 8) return false;
            if (!visited.Add(to)) return false;
            if (board[to.row, to.col] != '.') return false;
            int rowDiff = to.row - prev.row;
            if (rowDiff == 0) return false;
            if (rowDiff > 0 != isWhite && !IsKing(piece)) return false;
            int colDiff = Math.Abs(prev.col - to.col);
            if (Math.Abs(rowDiff) != colDiff || colDiff > 2) return false;
            if (colDiff == 1) return !mustCapture;
            var capture = board[(prev.row + to.row) / 2, (prev.col + to.col) / 2];
            if (capture == '.' || IsWhite(capture) == isWhite) return false;
        }
        return true;
    }

    private List<char> MoveInternal(List<(int row, int col)> moves) {
        var from = moves[0];
        var prev = from;
        List<char> captures = new(moves.Count - 2);
        for (int i = 1; i < moves.Count; prev = moves[i++]) {
            var to = moves[i];
            var piece = board[to.row, to.col] = board[prev.row, prev.col];
            if (to.row is 0 or 7) board[to.row, to.col] = char.ToUpper(piece); // promote
            if (Math.Abs(prev.row - to.row) == 2) {
                var capture = board[(prev.row + to.row) / 2, (prev.col + to.col) / 2];
                board[(prev.row + to.row) / 2, (prev.col + to.col) / 2] = '.';
                captures.Add(capture);
            }
            board[prev.row, prev.col] = '.';
        }
        return captures;
    }

    public List<(List<(int row, int col)> move, char piece, List<char> captures, int score)> LegalMoves() {
        return LegalMoves(isWhiteTurn);
    }

    private List<(List<(int row, int col)> move, char piece, List<char> captures, int score)> LegalMoves(bool isWhite) {
        List<(List<(int row, int col)> move, char piece, List<char> captures, int score)> moves = new();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] != '.' && IsWhite(board[r, c]) == isWhite)
                    moves.AddRange(LegalMoves((r, c)));
        return moves;
    }

    private static readonly (int dx, int dy)[]
        KingDirections = { (-1, -1), (1, 1), (-1, 1), (1, -1) },
        WhiteDirections = { (-1, 1), (1, 1) },
        BlackDirections = { (1, -1), (-1, -1) };

    private List<(List<(int row, int col)> move, char piece, List<char> captures, int score)> LegalMoves((int row, int col) from) {
        List<(List<(int row, int col)> move, char piece, List<char> captures, int score)> moves = new();
        var piece = board[from.row, from.col];
        bool isWhite = IsWhite(piece), isKing = IsKing(piece);
        List<(int row, int col)> move = new() { (from.row, from.col) };
        foreach (var dir in isKing ? KingDirections : isWhite ? WhiteDirections : BlackDirections) {
            move.Add((from.row + dir.dy, from.col + dir.dx));
            if (MoveLegal(move, isWhite))
                moves.Add((move.ToList(), piece, new List<char>(), from.row + dir.dy is 0 or 7 ? 2 : 0));
            move.RemoveAt(1);

            move.Add((from.row + dir.dy * 2, from.col + dir.dx * 2));
            if (MoveLegal(move, isWhite)) {
                var capture = board[from.row + dir.dy, from.col + dir.dx];
                moves.Add((move.ToList(), piece, new List<char>() { capture }, Value(capture, from.row + dir.dy) + from.row + dir.dy * 2 is 0 or 7 ? 2 : 0));
                MultiJump(from.row + dir.dy * 2, from.col + dir.dx * 2, dir.dy);
            }
            move.RemoveAt(1);
        }
        return moves;

        void MultiJump(int fromRow, int fromCol, int dy) {
            move.Add((fromRow + dy * 2, fromCol - 2));
            if (MoveLegal(move, isWhite)) {
                var capture = board[fromRow + dy, fromCol - 1];
                moves.Add((move.ToList(), piece, new List<char>() { capture }, Value(capture, fromRow + dy) + fromRow + dy * 2 is 0 or 7 ? 2 : 0));
                MultiJump(fromRow + dy * 2, fromCol - 2, dy);
            }
            move.RemoveAt(move.Count - 1);

            move.Add((fromRow + dy * 2, fromCol + 2));
            if (MoveLegal(move, isWhite)) {
                var capture = board[fromRow + dy, fromCol + 1];
                moves.Add((move.ToList(), piece, new List<char>() { capture }, Value(capture, fromRow + dy) + fromRow + dy * 2 is 0 or 7 ? 2 : 0));
                MultiJump(fromRow + dy * 2, fromCol + 2, dy);
            }
            move.RemoveAt(move.Count - 1);
        }
    }

    public (List<(int row, int col)> move, char piece, List<char> captures, int score) BestMove(Random random, int depth) {
        var saves = new Game[depth];
        for (int i = 0; i < depth; i++) saves[i] = new Game(this);
        return BestMoveInternal(isWhiteTurn, random, depth, saves);
    }

    private (List<(int row, int col)> move, char piece, List<char> captures, int score) BestMoveInternal(bool isWhite, Random random, int depth, Game[] saves) {
        (List<(int row, int col)> move, char piece, List<char> captures, int score) bestMove = default;
        var (bestScore, bestMovesCount, moves) = (int.MinValue, 0, LegalMoves(isWhite));
        if (depth == 0)
            foreach (var move in moves)
                BestScore(move.score, move);
        else {
            saves[depth - 1].Restore(this);
            foreach (var move in moves) {
                var capture = MoveInternal(move.move);
                BestScore(move.score - BestMoveInternal(!isWhite, random, depth - 1, saves).score, move);
                Restore(saves[depth - 1]);
            }
        }
        return bestMove;

        void BestScore(int score, (List<(int row, int col)> move, char piece, List<char> captures, int score) move) {
            if (score > bestScore)
                (bestScore, bestMove, bestMovesCount) = (score, move, 1);
            else if (score == bestScore && random.Next(++bestMovesCount) == 0)
                bestMove = move;
        }
    }

    private static int Value(char piece, int row) => piece switch {
        '.' => 0,
        'x' => row == 0 ? 3 : row == 1 ? 2 : 1, // about to be promoted=2
        'o' => row == 7 ? 3 : row == 6 ? 2 : 1, // about to be promoted=2
        'X' or 'O' => 3,
        _ => throw new Exception("unknown piece")
    };

    public int Score() => Score(isWhiteTurn) - Score(!isWhiteTurn);

    private int Score(bool isWhite) {
        int sum = 0;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] != '.' && isWhite == IsWhite(board[r, c]))
                    sum += Value(board[r, c], r);
        return sum;
    }

    private static string RC(int row, int col) => "" + (char)('a' + col) + (char)('8' - row);
    private static (int row, int col) RC(string rc) => ('8' - rc[1], rc[0] - 'a');
}