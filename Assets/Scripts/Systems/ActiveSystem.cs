using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class ActiveSystem : BotSystem {
		protected ActiveSystem(RealLiveBlock block) : base(block) { }



		public abstract void Activate(Rigidbody bot);
	}
}
