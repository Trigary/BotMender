using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class WeaponSystem : IBotSystem {
		protected readonly Vector3 _offset;

		protected WeaponSystem(Vector3 offset) {
			_offset = offset;
		}



		public abstract void Fire(Rigidbody bot, Vector3 target);
	}
}
