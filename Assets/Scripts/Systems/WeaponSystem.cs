using Blocks.Live;
using JetBrains.Annotations;
using Networking;
using Playing;
using Structures;
using UnityEngine;

namespace Systems {
	/// <summary>
	/// A system which controls a weapon.
	/// </summary>
	public abstract class WeaponSystem : BotSystem {
		public readonly WeaponConstants Constants;
		protected Vector3 TurretHeading => Turret.forward;
		protected Vector3 TurretEnd => Turret.position + Turret.rotation * Constants.TurretOffset;
		protected readonly Transform Turret;
		private readonly float _turretRotationMultiplier = 1;
		private float _cooldownEnds;

		protected WeaponSystem(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(id, structure, block) {
			Constants = constants;
			Turret = block.transform.Find("Turret");

			if (!NetworkUtils.IsServer && !NetworkUtils.IsLocal(Structure.Id)) {
				_turretRotationMultiplier *= 1.25f;
			}
		}



		/// <summary>
		/// Returns whether the specified weapon is currently on cooldown.
		/// </summary>
		public bool IsOnCooldown() {
			return _cooldownEnds > Time.time;
		}



		/// <summary>
		/// Rotate the weapon's barrel so it faces the target coordinates.
		/// </summary>
		public void TrackPosition(Vector3 position) {
			Vector3 direction = Quaternion.Inverse(Block.transform.rotation) * (position - Turret.position);
			Vector3 euler = Quaternion.LookRotation(direction, Block.transform.up).eulerAngles;
			euler.x = ClampRotation(euler.x, Constants.MinPitch, Constants.MaxPitch);
			euler.y = ClampRotation(euler.y, Constants.YawLimit * -1, Constants.YawLimit);

			Turret.localRotation = Quaternion.RotateTowards(Turret.localRotation,
				Quaternion.Euler(euler), Constants.RotationSpeed * _turretRotationMultiplier);
		}

		private static float ClampRotation(float value, float min, float max) {
			if (value > 180) {
				value -= 360;
			}
			return Mathf.Clamp(value, min, max);
		}



		/// <summary>
		/// Fire the weapon towards their current heading. Returns false if the shot would hit the bot itself.
		/// </summary>
		public bool TryFireWeapon(Rigidbody bot, float inaccuracy) { //TODO pass the target position parameter
			//TODO also return false if not ~facing the target position

			//TODO this method is also called by the server -> the firing should be cancellable
			//(undo inaccuracy change, energy, etc.)

			Vector3 point;
			RealLiveBlock block;
			Vector3 direction = Quaternion.Euler(inaccuracy * Random.Range(-1f, 1f),
				inaccuracy * Random.Range(-1f, 1f), 0) * TurretHeading;

			if (Physics.Raycast(TurretEnd, direction, out RaycastHit hit)) {
				if (hit.transform == bot.transform) {
					return false;
				}
				point = hit.point;
				block = hit.collider.gameObject.GetComponent<RealLiveBlock>();
			} else {
				point = TurretEnd + direction * 500;
				block = null;
			}

			FireWeapon(bot, point, block);
			_cooldownEnds = Time.time + Constants.Cooldown;
			bot.AddForceAtPosition(Turret.rotation * Constants.Kickback, TurretEnd, ForceMode.Impulse);
			return true;
		}

		protected abstract void FireWeapon(Rigidbody bot, Vector3 point, [CanBeNull] RealLiveBlock block);
		//TODO method 1: do fire weapon visuals (which may have to be deleted later),
		//inform the server of the target position and the system ID (the server should rotate 1 extra tick towards it)

		//TODO method 2: apply the response received from the server: expose a BitBuffer



		/// <summary>
		/// Types of weapons.
		/// </summary>
		public enum Type {
			None,
			Laser,
			Plasma,
			Beam,
			Artillery
		}

		/// <summary>
		/// Constants regarding a specific weapon.
		/// The turret offset's and the kickback's value is in world space units.
		/// The yaw and the pitch are specified in degrees, the MinPitch is usually negative.
		/// The rotation speed is specified in 'degrees / second'.
		/// The cooldown is in seconds.
		/// The energy is a value between 0 and 1.
		/// The inaccuracy is specified in angles.
		/// </summary>
		public class WeaponConstants {
			public readonly Vector3 TurretOffset, Kickback;
			public readonly float YawLimit, MinPitch, MaxPitch, RotationSpeed, Cooldown, Inaccuracy, Energy;

			public WeaponConstants(Vector3 turretOffset, float yawLimit, float minPitch, float maxPitch, float rotationSpeed,
									float kickback, float cooldown, float energy, float inaccuracy) {
				TurretOffset = turretOffset;
				YawLimit = yawLimit;
				MinPitch = minPitch;
				MaxPitch = maxPitch;
				RotationSpeed = rotationSpeed * NetworkedPhyiscs.TimestepSeconds;
				Kickback = new Vector3(0, 0, kickback * -1);
				Cooldown = cooldown;
				Energy = energy;
				Inaccuracy = inaccuracy;
			}
		}
	}
}
