using System;
using Blocks.Live;
using DoubleSocket.Utility.BitBuffer;
using Structures;

namespace Systems.Weapon {
	/// <summary>
	/// A weapon system which fires physics-controlled projectile shots.
	/// </summary>
	public abstract class ProjectileWeapon : WeaponSystem {
		protected ProjectileWeapon(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(id, structure, block, constants) {
		}



		public override bool ServerTryExecuteWeaponFiring(float inaccuracy) {
			//server returns an initial state of the projectile
			//the server should send a networkedphysics state update and the projectile state,
			//then I can use NetworkedPhysics
			throw new NotImplementedException();
		}



		public override void ClientExecuteWeaponFiring(BitBuffer buffer) {
			throw new NotImplementedException();
		}
	}
}
