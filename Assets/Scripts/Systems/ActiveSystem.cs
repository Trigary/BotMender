using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class ActiveSystem : IBotSystem {
		public abstract void Activate(Rigidbody bot);
	}
}
