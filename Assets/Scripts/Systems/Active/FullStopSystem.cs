using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems.Active {
	public class FullStopSystem : ActiveSystem {
		public FullStopSystem(RealLiveBlock block) : base(block) { }



		public override void Activate(Rigidbody bot) {
			bot.velocity = Vector3.zero;
		}
	}
}
