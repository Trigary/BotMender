using Assets.Scripts.Blocks.Live;
using JetBrains.Annotations;
using UnityEngine;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// A system which controls a weapon.
	/// </summary>
	public abstract class WeaponSystem : BotSystem {
		public readonly ConstantsContainer Constants;
		protected Vector3 TurretHeading { get { return _turret.forward; } }
		protected Vector3 TurretEnd { get { return _turret.position + _turret.rotation * _turretOffset; } }
		private readonly Transform _turret;
		private readonly Vector3 _turretOffset;
		private float _cooldownEnds;

		protected WeaponSystem(RealLiveBlock block, ConstantsContainer constants, Vector3 offset) : base(block) {
			Constants = constants;
			_turret = block.transform.Find("Turret");
			_turretOffset = offset;
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
		public void TrackTarget(Vector3 target) {
			Vector3 direction = Quaternion.Inverse(Block.transform.rotation) * (target - _turret.position);
			Vector3 euler = Quaternion.LookRotation(direction, Block.transform.up).eulerAngles;
			euler.x = ClampRotation(euler.x, Constants.MinPitch, Constants.MaxPitch);
			euler.y = ClampRotation(euler.y, Constants.YawLimit * -1, Constants.YawLimit);
			_turret.localRotation = Quaternion.RotateTowards(_turret.localRotation,
				Quaternion.Euler(euler), Constants.RotationSpeed * Time.fixedDeltaTime);
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
		public bool TryFireWeapon(Rigidbody bot, float inaccuracy) {
			Vector3 point;
			Transform hitTransform;
			Vector3 direction = Quaternion.Euler(inaccuracy * Random.Range(-1f, 1f),
				inaccuracy * Random.Range(-1f, 1f), 0) * TurretHeading;

			RaycastHit hit;
			if (Physics.Raycast(TurretEnd, direction, out hit)) {
				if (hit.transform == bot.transform) {
					return false;
				}
				point = hit.point;
				hitTransform = hit.transform;
			} else {
				point = TurretEnd + direction * 10000;
				hitTransform = null;
			}

			FireWeapon(bot, point, hitTransform == null ? null : hitTransform.GetComponent<RealLiveBlock>());
			_cooldownEnds = Time.time + Constants.Cooldown;
			bot.AddForceAtPosition(_turret.rotation * Constants.Kickback, TurretEnd, ForceMode.Impulse);
			return true;
		}

		protected abstract void FireWeapon(Rigidbody bot, Vector3 point, [CanBeNull] RealLiveBlock block);



		/// <summary>
		/// Types of weapons.
		/// </summary>
		public enum Type {
			None,
			Laser
		}

		/// <summary>
		/// Constants regarding a specific weapon.
		/// The yaw and the pitch are specified in degrees, the MinPitch is usually negative.
		/// The rotation speed is specified in 'degrees / second'.
		/// The kickback's value is in world space units.
		/// The cooldown is in seconds.
		/// The energy is a value between 0 and 1.
		/// The inaccuracy is specified in angles.
		/// </summary>
		public class ConstantsContainer {
			public readonly float YawLimit, MinPitch, MaxPitch, RotationSpeed, Cooldown, Inaccuracy, Energy;
			public readonly Vector3 Kickback;
			
			public ConstantsContainer(float yawLimit, float minPitch, float maxPitch, float rotationSpeed,
									float kickback, float cooldown, float energy, float inaccuracy) {
				YawLimit = yawLimit;
				MinPitch = minPitch;
				MaxPitch = maxPitch;
				RotationSpeed = rotationSpeed;
				Kickback = new Vector3(0, 0, kickback * -1);
				Cooldown = cooldown;
				Energy = energy;
				Inaccuracy = inaccuracy;
			}
		}
	}
}
