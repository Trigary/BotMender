using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Live;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Systems.Propulsion {
	public class ThrusterSystem : PropulsionSystem {
		private readonly Vector3 _offset;
		private readonly float _force;
		private readonly BlockSides _facing;

		public ThrusterSystem(RealLiveBlock block, Vector3 offset, float force) : base(block) {
			_offset = offset;
			_force = force;
			_facing = Rotation.GetFacing(block.Rotation);
		}



		public override void MoveRotate(Rigidbody bot, float x, float y, float z) {
			Vector3 direction;
			float multiplier = ForceData(bot.transform, _facing, x, y, z, out direction);
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (multiplier == 0) {
				return;
			}

			bot.AddForceAtPosition(direction * multiplier * _force,
				bot.position + bot.rotation * (Block.transform.localPosition + _offset),
				ForceMode.Impulse);
		}

		private static float ForceData(Transform bot, BlockSides facing, float x, float y, float z, out Vector3 direction) {
			switch (facing) {
				case BlockSides.Right:
				case BlockSides.Left:
					direction = bot.right;
					return x;
				case BlockSides.Top:
				case BlockSides.Bottom:
					direction = bot.up;
					return y;
				case BlockSides.Front:
				case BlockSides.Back:
					direction = bot.forward;
					return z;
				default:
					throw new AssertionException("Invalid facing: " + facing);
			}
		}
	}
}
