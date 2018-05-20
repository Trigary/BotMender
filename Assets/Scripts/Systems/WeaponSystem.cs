using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems {
	/// <summary>
	/// A system which controls a weapon.
	/// </summary>
	public abstract class WeaponSystem : BotSystem {
		protected Vector3 TurretHeading { get { return _turret.forward; } }
		protected Vector3 TurretEnd { get { return _turret.position + _turret.rotation * _turretOffset; } }
		private readonly Transform _turret;
		private readonly Vector3 _turretOffset;
		private readonly float _minPitch;
		private readonly float _maxPitch;
		private readonly float _yawLimit;

		protected WeaponSystem(RealLiveBlock block, Vector3 offset, float yawLimit, float minPitch, float maxPitch) : base(block) {
			_turret = block.transform.Find("Turret");
			_turretOffset = offset;
			_minPitch = minPitch;
			_maxPitch = maxPitch;
			_yawLimit = yawLimit;
		}



		/// <summary>
		/// Fire the weapons towards their current heading.
		/// </summary>
		public abstract void FireWeapons(Rigidbody bot);
		//TODO add weapon types
		//TODO cooldown: for a specific weapon type, method depends on weapon type (few weapons fires at a time depending on count & cooldown, etc.)
		//TODO should multiple weapon types be allowed on one bot? probably no
		//TODO inaccuracy: make it a parameter, so it can be displayed (and easier for server-side validation later)
		//TODO weapon kickback
		//TODO weapon rotation speed limit?
		//TODO only fire the weapon if it was able to look at the target? does rotation speed count? does being obfuscated by other blocks count?

		
		
		/// <summary>
		/// Rotate the weapon's barrel so it faces the target coordinates.
		/// </summary>
		public void TrackTarget(Vector3 target) {
			Vector3 direction = Quaternion.Inverse(Block.transform.rotation) * (target - _turret.position);
			Vector3 euler = Quaternion.LookRotation(direction, Block.transform.up).eulerAngles;
			euler.x = ClampRotation(euler.x, _minPitch, _maxPitch);
			euler.y = ClampRotation(euler.y, _yawLimit * -1, _yawLimit);
			_turret.localRotation = Quaternion.Euler(euler);
		}

		private static float ClampRotation(float value, float min, float max) {
			if (value > 180) {
				value -= 360;
			}
			return Mathf.Clamp(value, min, max);
		}
	}
}
