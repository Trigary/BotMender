using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems.Weapon {
	/// <summary>
	/// Fires a laser beam.
	/// </summary>
	public class LaserSystem : WeaponSystem {
		private static readonly ConstantsContainer ClassConstants = new ConstantsContainer(120, -60, 30, 300, 5, 1, 0.25f, 8);

		public LaserSystem(RealLiveBlock block, Vector3 offset) : base(block, ClassConstants, offset) { }



		protected override void FireWeapon(Rigidbody bot, Vector3 point, RealLiveBlock block) {
			Debug.DrawLine(TurretEnd, point, Color.red);
			if (block != null) {
				block.Damage(400);
			}
		}
	}
}
