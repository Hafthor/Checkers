public class Game {
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

    public char Move(int fromRow, int fromCol, int toRow, int toCol) {
        if (!MoveLegal(fromRow, fromCol, toRow, toCol, isWhiteTurn)) throw new Exception("Illegal move");
        var capture = MoveInternal(fromRow, fromCol, toRow, toCol);
        if (capture == '.') isWhiteTurn = !isWhiteTurn;
        return capture;
    }

    public static bool IsWhite(char piece) => piece is 'o' or 'O';
    public static bool IsKing(char piece) => piece is 'O' or 'X';

    public static string MoveString(int fromRow, int fromCol, int toRow, int toCol) =>
        "" + RC(fromRow, fromCol) + "-" + RC(toRow, toCol);

    public static (int fromRow, int fromCol, int toRow, int toCol) ParseMove(string cmd) {
        var parts = cmd.Split("-");
        (int row, int col) from = RC(parts[0]), to = RC(parts[1]);
        return (from.row, from.col, to.row, to.col);
    }

    public bool MoveLegal(int fromRow, int fromCol, int toRow, int toCol, bool isWhite) {
        if (toRow is < 0 or >= 8 || toCol is < 0 or >= 8 || fromRow == toRow || fromCol == toCol) return false;
        var piece = board[fromRow, fromCol];
        if (piece == '.') return false;
        if (board[toRow, toCol] != '.') return false;
        if (IsWhite(piece) != isWhite) return false;
        int rowDiff = toRow - fromRow;
        if (rowDiff == 0) return false;
        if (rowDiff > 0 != isWhite && !IsKing(piece)) return false;
        int colDiff = Math.Abs(fromCol - toCol);
        if (Math.Abs(rowDiff) != colDiff || colDiff > 2) return false;
        if (colDiff == 1) return true;
        var capture = board[(fromRow + toRow) / 2, (fromCol + toCol) / 2];
        return capture != '.' && IsWhite(capture) != isWhite;
    }

    private char MoveInternal(int fromRow, int fromCol, int toRow, int toCol) {
        board[toRow, toCol] = board[fromRow, fromCol];
        board[fromRow, fromCol] = '.';
        if (Math.Abs(fromRow - toRow) == 2) {
            var capture = board[(fromRow + toRow) / 2, (fromCol + toCol) / 2];
            board[(fromRow + toRow) / 2, (fromCol + toCol) / 2] = '.';
            return capture;
        }
        return '.';
    }

    public List<(int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score)> LegalMoves() {
        return LegalMoves(isWhiteTurn);
    }

    private List<(int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score)> LegalMoves(bool isWhite) {
        List<(int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score)> moves = new();
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (board[r, c] != '.' && IsWhite(board[r, c]) == isWhite)
                    foreach (var m in LegalMoves(r, c))
                        moves.Add((r, c, m.toRow, m.toCol, board[r, c], m.capture, 0));
        return moves;
    }

    private static readonly (int dx, int dy)[]
        KingDirections = { (-1, -1), (1, 1), (-1, 1), (1, -1) },
        WhiteDirections = { (-1, 1), (1, 1) },
        BlackDirections = { (1, -1), (-1, -1) };

    private List<(int toRow, int toCol, char capture)> LegalMoves(int fromRow, int fromCol) {
        List<(int toRow, int toCol, char capture)> moves = new();
        var piece = board[fromRow, fromCol];
        bool isWhite = IsWhite(piece), isKing = IsKing(piece);
        foreach (var dir in isKing ? KingDirections : isWhite ? WhiteDirections : BlackDirections) {
            if (MoveLegal(fromRow, fromCol, fromRow + dir.dy, fromCol + dir.dx, isWhite))
                moves.Add((fromRow + dir.dy, fromCol + dir.dx, '.'));
            if (MoveLegal(fromRow, fromCol, fromRow + dir.dy * 2, fromCol + dir.dx * 2, isWhite))
                moves.Add((fromRow + dir.dy * 2, fromCol + dir.dx * 2, board[fromRow + dir.dy, fromCol + dir.dx]));
        }
        return moves;
    }

    public (int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score) BestMove(Random random, int depth) {
        var saves = new Game[depth];
        for (int i = 0; i < depth; i++) saves[i] = new Game(this);
        return BestMoveInternal(isWhiteTurn, random, depth, saves);
    }

    private (int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score) BestMoveInternal(bool isWhite, Random random, int depth, Game[] saves) {
        (int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score) bestMove = default;
        var (bestScore, bestMovesCount, moves) = (int.MinValue, 0, LegalMoves(isWhite));
        if (depth == 0)
            foreach (var move in moves)
                BestScore(move.score, move);
        else {
            saves[depth - 1].Restore(this);
            foreach (var move in moves) {
                var capture = MoveInternal(move.fromRow, move.fromCol, move.toRow, move.toCol);
                if (capture == '.')
                    BestScore(move.score - BestMoveInternal(!isWhite, random, depth - 1, saves).score, move);
                else
                    BestScore(move.score + BestMoveInternal(isWhite, random, depth, saves).score, move);
                Restore(saves[depth - 1]);
            }
        }
        return bestMove;

        void BestScore(int score, (int fromRow, int fromCol, int toRow, int toCol, char piece, char capture, int score) move) {
            if (score > bestScore)
                (bestScore, bestMove, bestMovesCount) = (score, move, 1);
            else if (score == bestScore && random.Next(++bestMovesCount) == 0)
                bestMove = move;
        }
    }
    
    private static int Value(char piece, int row) => piece switch {
        '.' => 0,
        'x' => row == 1 ? 2 : 1, // about to be promoted=2
        'o' => row == 6 ? 2 : 1, // about to be promoted=2
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