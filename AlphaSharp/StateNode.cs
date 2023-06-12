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

		public StateNode(int actionCount, int idx)
		{
			Actions = new Action[actionCount];
			Idx = idx;
		}

		public int Idx;
		public int VisitCount;
		public GameOver.Status GameOver;
		public Action[] Actions;
	}
}
