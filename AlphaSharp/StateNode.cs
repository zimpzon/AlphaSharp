namespace AlphaSharp
{
	public class StateNode
	{
		public struct Action
		{
			public float Q;
			public int VisitCount;
			public byte IsValidMove;
			public float ActionProbability;

            public override readonly string ToString()
				=> $"Q: {Q}, VisitCount: {VisitCount}, IsValidMove: {IsValidMove}, ActionProbability: {ActionProbability}";
        }

		public StateNode(int actionCount)
		{
			GameOver = int.MinValue;
			Actions = new Action[actionCount];
		}

		public long Lock;
		public int VisitCount;
		public int GameOver;
		public Action[] Actions;
	}
}
