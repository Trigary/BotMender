using System;
using UnityEngine;

namespace Blocks {
	/// <summary>
	/// The sides of a block. Example usage: the sides where the block can connect to other blocks.
	/// </summary>
	[Flags]
	public enum BlockSides : byte {
		None = 0,
		Right = 1 << 0, //X+
		Left = 1 << 1, //X-
		Top = 1 << 2, //Y+
		Bottom = 1 << 3, //Y-
		Front = 1 << 4, //Z+
		Back = 1 << 5, //Z-
		X = Right | Left,
		Y = Top | Bottom,
		Z = Front | Back,
		All = X | Y | Z
	}

	/// <summary>
	/// Utility methods regarding the BlockSides flags enum.
	/// </summary>
	public static class BlockSide {
		/// <summary>
		/// Returns a BlockSides based on the raycast hit's normal.
		/// The normal should be aligned with the global axises,
		/// otherwise it will return a rounded side or 'None'.
		/// </summary>
		public static BlockSides FromNormal(Vector3 normal) {
			Vector3Int vector = new Vector3Int(
				Mathf.RoundToInt(normal.x),
				Mathf.RoundToInt(normal.y),
				Mathf.RoundToInt(normal.z)
			);

			if (vector.x == 1) {
				return BlockSides.Right;
			} else if (vector.x == -1) {
				return BlockSides.Left;
			} else if (vector.y == 1) {
				return BlockSides.Top;
			} else if (vector.y == -1) {
				return BlockSides.Bottom;
			} else if (vector.z == 1) {
				return BlockSides.Front;
			} else if (vector.z == -1) {
				return BlockSides.Back;
			} else {
				return BlockSides.None;
			}
		}

		/// <summary>
		/// Gets the ordinal value associated with the specified side.
		/// Returns 255 if the parameter contains multiple sides.
		/// </summary>
		public static byte ToOrdinal(BlockSides side) {
			switch (side) {
				case BlockSides.Right:
					return 0;
				case BlockSides.Left:
					return 1;
				case BlockSides.Top:
					return 2;
				case BlockSides.Bottom:
					return 3;
				case BlockSides.Front:
					return 4;
				case BlockSides.Back:
					return 5;
				default:
					return 255;
			}
		}

		/// <summary>
		/// Gets the side associated with the ordinal.
		/// Returns None if the ordinal is invalid.
		/// </summary>
		public static BlockSides FromOrdinal(byte ordinal) {
			return ordinal > 5 ? BlockSides.None : (BlockSides)(1 << ordinal);
		}
	}
}
