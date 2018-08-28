using Blocks.Live;
using Structures;

namespace Systems.Weapon {
	/// <summary>
	/// A projectile weapon which fires a high-explosive projectile which deals damage around its impact area.
	/// It is, but jast barely capable of indirect firing.
	/// </summary>
	public class ArtilleryWeapon : ProjectileWeapon {
		public ArtilleryWeapon(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants) : base(id, structure, block, constants) {
		}
	}
}
