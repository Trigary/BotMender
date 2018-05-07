using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class PropulsionSystem : IBotSystem {
		public abstract void MoveRotate(Rigidbody bot, float x, float y, float z);
	}
}
