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
		}

		public StateNode(int actionCount)
		{
			GameOver = -1;
			Actions = new Action[actionCount];
		}

		public long Lock;
		public int VisitCount;
		public int GameOver;
		public Action[] Actions;
	}
}
