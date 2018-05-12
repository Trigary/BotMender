using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems.Weapon {
	public class LaserSystem : WeaponSystem {
		public LaserSystem(RealLiveBlock block, Vector3 offset) : base(block, offset, 120, -60, 30) { }



		public override void FireWeapons(Rigidbody bot) {
			Debug.DrawRay(TurretEnd, TurretHeading * 10);
		}
	}
}
