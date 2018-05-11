using UnityEngine;

namespace Assets.Scripts.Systems.Weapon {
	public class LaserSystem : WeaponSystem {
		public LaserSystem(Transform self, Vector3 offset) : base(self, offset, 120, -60, 30) {

		}
		


		public override void FireWeapons(Rigidbody bot) {
			Debug.DrawRay(TurretEnd, TurretHeading * 10);
		}
	}
}
