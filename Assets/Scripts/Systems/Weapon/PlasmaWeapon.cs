using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Weapon {
	/// <summary>
	/// A hitscan weapon which fires a plasma beam.
	/// If a block was hit, all connected blocks are also damaged until the shot has run out of damage to deal.
	/// </summary>
	public class PlasmaWeapon : HitscanWeapon {
		public PlasmaWeapon(CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(structure, block, constants) {
		}



		protected override void ServerFireWeapon(Vector3 point, RealLiveBlock block) {
			//TODO currently no API is exposed which lets this scope get the connected blocks
			throw new System.NotImplementedException();
		}

		protected override void ClientFireWeapon(Vector3 point) {
			throw new System.NotImplementedException();
		}
	}
}
