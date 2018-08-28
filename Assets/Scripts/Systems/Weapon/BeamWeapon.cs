using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Weapon {
	/// <summary>
	/// A hitscan weapon which fires a laser beam.
	/// If a block was hit, all blocks behind it are also damaged until the shot has run out of damage to deal.
	/// </summary>
	public class BeamWeapon : HitscanWeapon {
		public BeamWeapon(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants) : base(id, structure, block, constants) {
		}



		protected override void ServerFireWeapon(Vector3 point, RealLiveBlock block) {
			throw new System.NotImplementedException();
		}

		protected override void ClientFireWeapon(Vector3 point) {
			throw new System.NotImplementedException();
		}
	}
}
