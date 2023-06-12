namespace AlphaSharp
{
    public static class GameOver
    {
        public enum Status
        {
            GameIsNotOver,
            Player1Won,
            Player2Won,
            Draw,
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

