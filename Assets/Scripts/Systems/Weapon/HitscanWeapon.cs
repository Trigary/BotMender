using Blocks.Live;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using Networking;
using Structures;
using UnityEngine;
using Utilities;

namespace Systems.Weapon {
	/// <summary>
	/// A weapon system which fires projectiles which travel at an infinite speed:
	/// the impact point is known the instant the weapon is fired.
	/// The shots are not influenced by physics (eg. gravity).
	/// This is the alternative to the ProjectileWeapon system base.
	/// </summary>
	public abstract class HitscanWeapon : WeaponSystem {
		protected HitscanWeapon(CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(structure, block, constants) {
		}



		public override bool ServerTryExecuteWeaponFiring(float inaccuracy) {
			if (TurretHeadingAngleDifference > MaxTurretHeadingAngleDifference) {
				return false;
			}

			Vector3 point;
			RealLiveBlock block;
			Vector3 direction = GetInaccurateHeading(inaccuracy);

			if (Physics.Raycast(TurretEnd, direction, out RaycastHit hit)) {
				if (hit.transform == Structure.transform) {
					return false;
				}
				point = hit.point;
				block = hit.collider.gameObject.GetComponent<RealLiveBlock>();
			} else {
				point = TurretEnd + direction * 500;
				block = null;
			}

			ServerFireWeapon(point, block);
			Structure.Body.AddForceAtPosition(Turret.rotation * Constants.Kickback, TurretEnd, ForceMode.Impulse);
			ClientFireWeapon(point);
			NetworkServer.SendTcpToClients(TcpPacketType.Server_System_Execute, buffer => {
				buffer.Write(Structure.Id);
				Block.Position.Serialize(buffer);
				buffer.Write(point);
			});
			return true;
		}

		public override void ClientExecuteWeaponFiring(BitBuffer buffer) {
			ClientFireWeapon(buffer.ReadVector3());
		}



		protected abstract void ServerFireWeapon(Vector3 point, [CanBeNull] RealLiveBlock block);

		protected abstract void ClientFireWeapon(Vector3 point);
	}
}
