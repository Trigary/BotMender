using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DoubleSocket.Utility.BitBuffer;
using UnityEngine;
using UnityEngine.Assertions;

namespace Blocks {
	/// <summary>
	/// Specifies a block coordinate in the building mode.
	/// </summary>
	public class BlockPosition : IEquatable<BlockPosition> {
		public static readonly IComparer<BlockPosition> AscendingComparer = new BlockPositionComparer();
		public const int SerializedBitsSize = AxisValueBitSize * 3;
		private const int AxisValueBitSize = 6;
		private const int MaxAxisValue = (1 << AxisValueBitSize) - 1; //Inclusive

		public readonly byte X;
		public readonly byte Y;
		public readonly byte Z;

		private BlockPosition(int x, int y, int z) {
			X = (byte)x;
			Y = (byte)y;
			Z = (byte)z;
		}



		/// <summary>
		/// Serializes the current BlockPosition into the specified buffer.
		/// </summary>
		public void Serialize(BitBuffer buffer) {
			buffer.WriteBits(X, AxisValueBitSize);
			buffer.WriteBits(Y, AxisValueBitSize);
			buffer.WriteBits(Z, AxisValueBitSize);
		}

		/// <summary>
		/// Deserializes a BlockPosition from the specified buffer.
		/// </summary>
		public static BlockPosition Deserialize(BitBuffer buffer) {
			return new BlockPosition((int)buffer.ReadBits(AxisValueBitSize),
				(int)buffer.ReadBits(AxisValueBitSize), (int)buffer.ReadBits(AxisValueBitSize));
		}



		/// <summary>
		/// Returns false if the position is out of bounds.
		/// </summary>
		public static bool FromVector(Vector3 position, out BlockPosition output) {
			int x = (byte)Mathf.RoundToInt(position.x);
			int y = (byte)Mathf.RoundToInt(position.y);
			int z = (byte)Mathf.RoundToInt(position.z);
			return FromComponents(x, y, z, out output);
		}

		public Vector3 ToVector() {
			return new Vector3(X, Y, Z);
		}



		/// <summary>
		/// Returns false if the offseted value is out of bounds.
		/// </summary>
		public bool GetOffseted(int x, int y, int z, out BlockPosition output) {
			return FromComponents(X + x, Y + y, Z + z, out output);
		}

		/// <summary>
		/// Returns false if the offseted value is out of bounds.
		/// </summary>
		// ReSharper disable once AnnotateCanBeNullParameter
		public bool GetOffseted(BlockSides side, out BlockPosition output) {
			switch (side) {
				case BlockSides.Right:
					output = X == MaxAxisValue ? null : new BlockPosition(X + 1, Y, Z);
					break;
				case BlockSides.Left:
					output = X == 0 ? null : new BlockPosition(X - 1, Y, Z);
					break;
				case BlockSides.Top:
					output = Y == MaxAxisValue ? null : new BlockPosition(X, Y + 1, Z);
					break;
				case BlockSides.Bottom:
					output = Y == 0 ? null : new BlockPosition(X, Y - 1, Z);
					break;
				case BlockSides.Front:
					output = Z == MaxAxisValue ? null : new BlockPosition(X, Y, Z + 1);
					break;
				case BlockSides.Back:
					output = Z == 0 ? null : new BlockPosition(X, Y, Z - 1);
					break;
				default:
					throw new AssertionException("Invalid side: " + side, null);
			}
			return output != null;
		}



		public override bool Equals(object obj) {
			return Equals(obj as BlockPosition);
		}

		public bool Equals(BlockPosition other) {
			return other != null &&
				X == other.X &&
				Y == other.Y &&
				Z == other.Z;
		}

		public override int GetHashCode() {
			int hashCode = -307843816;
			hashCode = hashCode * -1521134295 + X.GetHashCode();
			hashCode = hashCode * -1521134295 + Y.GetHashCode();
			hashCode = hashCode * -1521134295 + Z.GetHashCode();
			return hashCode;
		}



		// ReSharper disable once AnnotateCanBeNullParameter
		private static bool FromComponents(int x, int y, int z, out BlockPosition output) {
			output = x >= 0 && y >= 0 && z >= 0 && x <= MaxAxisValue && y <= MaxAxisValue && z <= MaxAxisValue
				? new BlockPosition(x, y, z) : null;
			return output != null;
		}

		private class BlockPositionComparer : IComparer<BlockPosition> {
			[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
			public int Compare(BlockPosition left, BlockPosition right) {
				int result;
				return (result = left.X.CompareTo(right.X)) != 0 ? result
					: (result = left.Y.CompareTo(right.Y)) != 0 ? result
					: left.Z.CompareTo(right.Z);
			}
		}
	}
}
