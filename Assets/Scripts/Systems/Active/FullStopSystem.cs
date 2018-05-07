using UnityEngine;

namespace Assets.Scripts.Systems.Active {
	public class FullStopSystem : ActiveSystem {
		public override void Activate(Rigidbody bot) {
			bot.velocity = Vector3.zero;
		}
	}
}
