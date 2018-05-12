using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class PropulsionSystem : BotSystem {
		protected PropulsionSystem(RealLiveBlock block) : base(block) { }



		public abstract void MoveRotate(Rigidbody bot, float x, float y, float z);
	}
}
