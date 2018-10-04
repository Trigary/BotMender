using Blocks.Info;
using UnityEngine;

namespace Blocks.Live {
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
		/// Damage the block with the given damage.
		/// The damage is internally limited to the remaining health and then returned.
		/// This method only changes the Health property (it doesn't destroy the GameObject if the health reaches 0, etc).
		/// CompleteStructure#Damaged should be called after this method is called.
		/// </summary>
		public uint Damage(uint damage) {
			if (Health > damage) {
				Health -= damage;
			} else {
				damage = Health;
				Health = 0;
			}
			return damage;
		}
	}
}
