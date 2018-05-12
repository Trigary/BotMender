using Assets.Scripts.Blocks.Live;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public abstract class WeaponSystem : BotSystem {
		private readonly Transform _turret;
		private readonly Vector3 _turretOffset;
		private readonly float _minPitch;
		private readonly float _maxPitch;
		private readonly float _yawLimit;
		protected Vector3 TurretHeading { get { return _turret.forward; } }
		protected Vector3 TurretEnd { get { return _turret.position + _turret.rotation * _turretOffset; } }

		protected WeaponSystem(RealLiveBlock block, Vector3 offset, float yawLimit, float minPitch, float maxPitch) : base(block) {
			_turret = block.transform.Find("Turret");
			_turretOffset = offset;
			_minPitch = minPitch;
			_maxPitch = maxPitch;
			_yawLimit = yawLimit;
		}


		public abstract void FireWeapons(Rigidbody bot);



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
