using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Structures;
using UnityEngine;

namespace Assets.Scripts.Blocks.Live {
	/// <summary>
	/// A live block which has a GameObject.
	/// </summary>
	public abstract class RealLiveBlock : MonoBehaviour, ILiveBlock {
		public BlockSides ConnectSides { get; protected set; }
		public BlockPosition Position { get; protected set; }
		public BlockInfo Info { get; protected set; }
		public byte Rotation { get; protected set; }
		public uint Health { get; private set; }

		protected void InitializeBase() {
			Health = Info.Health;
		}



		/// <summary>
		/// Damage the block with the given damage. If the health reaches 0, also destroy the GameObject.
		/// This method internally calls the CompleteStructure#Damaged method.
		/// The damage is internally limited to the remaining health of the block.
		/// </summary>
		public void Damage(uint damage) {
			if (Health > damage) {
				Health -= damage;
			} else {
				damage = Health;
				Health = 0;
			}
			transform.parent.GetComponent<CompleteStructure>().Damaged(this, damage);
		}
	}
}
