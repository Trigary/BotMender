using Blocks.Live;
using DoubleSocket.Utility.BitBuffer;
using Networking;
using Playing.Networking;
using Structures;
using UnityEngine;

namespace Systems.Weapon {
	/// <summary>
	/// A system which controls a weapon.
	/// All weapon systems must have a colliderless child GameObject named "Turret".
	/// </summary>
	public abstract class WeaponSystem : BotSystem {
		protected const float MaxTurretHeadingAngleDifference = 5;

		public readonly WeaponConstants Constants;
		protected Vector3 TurretHeading => Turret.forward;
		protected Vector3 TurretEnd => Turret.position + Turret.rotation * Constants.TurretOffset;
		protected readonly Transform Turret;
		protected float TurretHeadingAngleDifference { get; private set; }
		private readonly float _turretRotationMultiplier = 1;
		private float _cooldownEnds;

		protected WeaponSystem(CompleteStructure structure, RealLiveBlock block, WeaponConstants constants)
			: base(structure, block) {
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
		/// Called whenever the weapon was fired. Internally calculates sets the cooldown end timestamp.
		/// </summary>
		public void UpdateCooldown() {
			_cooldownEnds = Time.time + Constants.Cooldown;
		}



		/// <summary>
		/// Rotate the weapon's barrel towards the target coordinates.
		/// Also sets the amount of angles which were left out due to the rotation speed limit
		/// </summary>
		public void TrackPosition(Vector3 position) {
			Vector3 direction = Quaternion.Inverse(Block.transform.rotation) * (position - Turret.position);
			Vector3 euler = Quaternion.LookRotation(direction, Block.transform.up).eulerAngles;
			euler.x = ClampRotation(euler.x, Constants.MinPitch, Constants.MaxPitch);
			euler.y = ClampRotation(euler.y, Constants.YawLimit * -1, Constants.YawLimit);

			Quaternion target = Quaternion.Euler(euler);
			Turret.localRotation = Quaternion.RotateTowards(Turret.localRotation, target,
				Constants.RotationSpeed * _turretRotationMultiplier);
			TurretHeadingAngleDifference = Quaternion.Angle(Turret.localRotation, target);
		}

		private static float ClampRotation(float value, float min, float max) {
			if (value > 180) {
				value -= 360;
			}
			return Mathf.Clamp(value, min, max);
		}



		/// <summary>
		/// Returns whether the weapon can be fired towards the specified position in its current state.
		/// If it can, the weapon is fired and the client gets notified with all necessary information.
		/// A weapon can't be fired it it would hit the bot itself or if the angle between
		/// the desired and the actual facing is too great.
		/// </summary>
		public abstract bool ServerTryExecuteWeaponFiring(float inaccuracy);

		/// <summary>
		/// The server instructs the client to fire this weapon.
		/// All relevant information is specified in the buffer.
		/// Kickback shouldn't be applied client-side: the UDP packets take care of that - don't apply it twice.
		/// </summary>
		public abstract void ClientExecuteWeaponFiring(BitBuffer buffer);



		protected Vector3 GetInaccurateHeading(float inaccuracy) {
			return Quaternion.Euler(inaccuracy * Random.Range(-1f, 1f), inaccuracy * Random.Range(-1f, 1f), 0) * TurretHeading;
		}



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
		/// A single firing weapon type only fires when the fire button is clicked.
		/// Other weapon types firing continuously until the fire button is released.
		/// </summary>
		public static bool IsSingleFiringType(Type type) {
			return type == Type.Plasma || type == Type.Beam;
		}

		/// <summary>
		/// Constants regarding a specific weapon.
		/// The turret offset's and the kickback's value is in world space units.
		/// The yaw and the pitch are specified in degrees, the MinPitch is usually negative.
		/// The rotation speed is specified in 'degrees / second'.
		/// The cooldown is in seconds.
		/// The energy is a value between 0 and 1.
		/// The inaccuracy is specified in degrees.
		/// </summary>
		public class WeaponConstants {
			public readonly Type Type;
			public readonly Vector3 TurretOffset, Kickback;
			public readonly float YawLimit, MinPitch, MaxPitch, RotationSpeed, Cooldown, Inaccuracy, Energy;

			public WeaponConstants(Type type, Vector3 turretOffset, float yawLimit, float minPitch, float maxPitch,
									float rotationSpeed, float kickback, float cooldown, float energy, float inaccuracy) {
				Type = type;
				TurretOffset = turretOffset;
				YawLimit = yawLimit;
				MinPitch = minPitch;
				MaxPitch = maxPitch;
				RotationSpeed = rotationSpeed * NetworkedPhysics.TimestepSeconds;
				Kickback = new Vector3(0, 0, kickback * -1);
				Cooldown = cooldown;
				Energy = energy;
				Inaccuracy = inaccuracy;
			}
		}
	}
}
