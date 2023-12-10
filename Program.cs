public class Program {
    private static readonly Random random = new(0);

    public static void Main(string[] args) {
        var game = new Game();
        string lastWhiteCmd = "best4", lastBlackCmd = "best4";
        bool autoMode = false;
        for (;;) {
            Console.Write(game);
            Console.Write("Turn: " + (game.isWhiteTurn ? "white (o)" : "black (x)") + " Score: " + game.Score());
            var moves = game.LegalMoves().ToList();
            if (moves.Count == 0) {
                Console.WriteLine(" No legal moves!");
                break;
            }
            Console.WriteLine(" Moves: " + string.Join(", ", moves.Select(m => Game.MoveString(m.move))));
            var cmd = autoMode ? "" : Console.ReadLine();
            if (cmd == "") cmd = game.isWhiteTurn ? lastWhiteCmd : lastBlackCmd;
            if (cmd == "auto")
                autoMode = true;
            else if (cmd.StartsWith("best")) {
                _ = game.isWhiteTurn ? lastWhiteCmd = cmd : lastBlackCmd = cmd;
                var move = game.BestMove(random, int.Parse(cmd.Substring(4)));
                Console.WriteLine("Move: " + Game.MoveString(move.move));
                game.Move(move.move);
            } else if (cmd.Contains("-")) {
                var move = Game.ParseMove(cmd);
                if (!game.MoveLegal(move, game.isWhiteTurn)) {
                    Console.WriteLine("Invalid move");
                    continue;
                }
                Console.WriteLine("Move: " + cmd);
                game.Move(move);
            }
        }
    }
}