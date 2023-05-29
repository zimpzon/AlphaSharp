using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlphaSharp
{
	public class StateNode
	{
		public struct Action
		{
			public float Q;
			public int VisitCount;
			public int ChildIndex;
			public byte IsValidMove;
			public float ActionProbability;
		}

		public StateNode(int actionCount)
		{
			GameOver = -1;
			ParentIndex = -1;
			Actions = new Action[actionCount];
		}

		public long Lock;
		public int VisitCount;
		public int GameOver;
		public int ParentIndex;
		public float V;
		public Action[] Actions;
	}
}
