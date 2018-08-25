using Blocks.Live;
using Structures;
using UnityEngine;

namespace Systems.Weapon {
	/// <summary>
	/// A hitscan weapon which fires a laser pulse which only damages the block it hits.
	/// It has a visualized travel time.
	/// </summary>
	public class LaserSystem : HitscanWeapon {
		public const float MaxParticleLifeTime = 5f;
		private readonly ParticleSystem _particles;

		public LaserSystem(byte id, CompleteStructure structure, RealLiveBlock block, WeaponConstants constants) : base(id, structure, block, constants) {
			_particles = Turret.GetComponent<ParticleSystem>();
			ParticleSystem.ShapeModule shape = _particles.shape;
			shape.position = Constants.TurretOffset;
		}



		protected override void FireWeapon(Rigidbody bot, Vector3 point, RealLiveBlock block) {
			if (block != null) {
				block.Damage(400);
			}

			ParticleSystem.ShapeModule shape = _particles.shape;
			Vector3 path = point - TurretEnd;
			shape.rotation = (Quaternion.Inverse(_particles.transform.rotation) * Quaternion.LookRotation(path)).eulerAngles;

			ParticleSystem.MainModule main = _particles.main;
			main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Min(MaxParticleLifeTime,
				path.magnitude / main.startSpeed.Evaluate(0)));
			_particles.Emit(1);
		}
	}
}
