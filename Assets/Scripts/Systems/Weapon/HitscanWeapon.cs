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
	/// the impact point is known the instant the weapon was fired.
	/// The shots are not influenced by physics (eg. gravity).
	/// </summary>
	public abstract class HitscanWeapon : WeaponSystem {
		protected HitscanWeapon(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(id, structure, block, constants) {
		}



		public override bool ServerTryExecuteWeaponFiring(float inaccuracy) {
			if (TurretHeadingAngleDifference > MaxTurretHeadingAngleDifference) {
				return false;
			}

			Vector3 point;
			RealLiveBlock block;
			Vector3 direction = Quaternion.Euler(inaccuracy * Random.Range(-1f, 1f),
				inaccuracy * Random.Range(-1f, 1f), 0) * TurretHeading;

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
			NetworkServer.SendTcpToAll(TcpPacketType.Server_System_FireWeapon, buffer => {
				buffer.Write(Structure.Id);
				buffer.Write(Id);
				buffer.Write(point);
				//TODO can play around with passing relative (to the bot which was hit)
				//position of the block which was hit instead
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
