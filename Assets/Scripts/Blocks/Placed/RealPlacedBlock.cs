using UnityEngine;

namespace Blocks.Placed {
	/// <summary>
	/// A placed block which has a GameObject.
	/// </summary>
	public abstract class RealPlacedBlock : MonoBehaviour, IPlacedBlock {
		public const int SerializedBitsSize = 12 + BlockPosition.SerializedBitsSize + Blocks.Rotation.SerializedBitsSize;

		public BlockSides ConnectSides { get; protected set; }
		public BlockPosition Position { get; protected set; }
		public BlockType Type { get; protected set; }
		public byte Rotation { get; protected set; }
	}
}
