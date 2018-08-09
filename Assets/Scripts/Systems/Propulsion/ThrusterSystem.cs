using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Live;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.Scripts.Systems.Propulsion {
	/// <summary>
	/// Adds some force at the thruster's coordinates to the bot.
	/// </summary>
	public class ThrusterSystem : PropulsionSystem {
		protected readonly ThrusterConstants Constants;
		private readonly BlockSides _facing;

		public ThrusterSystem(RealLiveBlock block, ThrusterConstants constants) : base(block) {
			Constants = constants;
			_facing = Rotation.RotateSides(constants.Facing, block.Rotation);
		}



		public override void MoveRotate(Rigidbody bot, Vector3 direction) {
			float multiplier = ForceData(bot.transform, _facing, direction.x, direction.y, direction.z, out direction);
			if (multiplier == 0) {
				return;
			}

			bot.AddForceAtPosition(direction * multiplier * Constants.Force,
				bot.position + bot.rotation * (Block.transform.localPosition + Constants.Offset),
				ForceMode.Impulse);
		}

		private static float ForceData(Transform bot, BlockSides facing, float x, float y, float z, out Vector3 direction) {
			switch (facing) {
				case BlockSides.Right:
					direction = bot.right;
					return x < 0 ? 0f : x;
				case BlockSides.Left:
					direction = bot.right;
					return x > 0 ? 0f : x;
				case BlockSides.Top:
					direction = bot.up;
					return y < 0 ? 0f : y;
				case BlockSides.Bottom:
					direction = bot.up;
					return y > 0 ? 0f : y;
				case BlockSides.Front:
					direction = bot.forward;
					return z < 0 ? 0f : z;
				case BlockSides.Back:
					direction = bot.forward;
					return z > 0 ? 0f : z;
				default:
					throw new AssertionException("Invalid facing: " + facing, null);
			}
		}



		/// <summary>
		/// Constants regarding a specific thruster.
		/// The offset's and the force's value is in world space units.
		/// The facing must contain exactly 1 facing.
		/// </summary>
		public class ThrusterConstants {
			public readonly Vector3 Offset;
			public readonly float Force;
			public readonly BlockSides Facing;

			public ThrusterConstants(Vector3 offset, float force, BlockSides facing) {
				Offset = offset;
				Force = force;
				Facing = facing;
			}
		}
	}
}
