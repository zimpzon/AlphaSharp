namespace AlphaSharp
{
    public static class GameOver
    {
        public enum Status
        {
            GameIsNotOver,
            Player1Won,
            Player2Won,
            Draw, // this will mark the state as a draw and future visits to the state will stop with a draw result.
            DrawDueToMaxMovesReached, // this will not mark the state as a draw, so future visits to the state will not stop the game.
        }

        public static bool IsDraw(Status status)
            => status == Status.Draw || status == Status.DrawDueToMaxMovesReached;

        public static Status InvertResult(Status status)
        {
            if (status == Status.Player1Won)
                return Status.Player2Won;
            else if (status == Status.Player2Won)
                return Status.Player1Won;
            else
                return status;
        }

        public static float ValueForPlayer1(Status status)
        {
            float v = 0.0f; // draw

            if (status == Status.Player1Won)
                v = 1.0f;
            else if (status == Status.Player2Won)
                v = -1.0f;

            return v;
        }
    }
}

