using Blocks.Live;
using Structures;

namespace Systems.Weapon {
	/// <summary>
	/// A weapon system which fires projectiles which travel at an infinite speed:
	/// the impact point is known the instant the weapon was fired.
	/// The shots are not influenced by physics (eg. gravity).
	/// </summary>
	public abstract class HitscanWeapon : WeaponSystem {
		protected HitscanWeapon(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(id, structure, block, constants) {
		}

		//server returns the block or the position which was hit
		//(for blocks, do I introduce a 12-bit long block id or do I use the 3x7 bit block position?)
	}
}
