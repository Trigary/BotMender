using System;
using UnityEngine;

namespace Assets.Scripts.Blocks {
	public class BlockPosition : IEquatable<BlockPosition> {
		private const byte Min = 0; //Inclusive
		private const byte Max = 255; //Inclusive

		public readonly byte X;
		public readonly byte Y;
		public readonly byte Z;

		private BlockPosition(int x, int y, int z) {
			X = (byte)x;
			Y = (byte)y;
			Z = (byte)z;
		}



		/// <summary>
		/// Returns false if the position is out of bounds.
		/// </summary>
		public static bool FromComponents(int x, int y, int z, out BlockPosition output) {
			if (IsValid(x, y, z)) {
				output = new BlockPosition(x, y, z);
				return true;
			}

			output = null;
			return false;
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
		public bool GetOffseted(BlockSides side, out BlockPosition output) {
			switch (side) {
				case BlockSides.Right:
					output = X == Max ? null : new BlockPosition(X + 1, Y, Z);
					break;
				case BlockSides.Left:
					output = X == Min ? null : new BlockPosition(X - 1, Y, Z);
					break;
				case BlockSides.Top:
					output = Y == Max ? null : new BlockPosition(X, Y + 1, Z);
					break;
				case BlockSides.Bottom:
					output = Y == Min ? null : new BlockPosition(X, Y - 1, Z);
					break;
				case BlockSides.Front:
					output = Z == Max ? null : new BlockPosition(X, Y, Z + 1);
					break;
				case BlockSides.Back:
					output = Z == Min ? null : new BlockPosition(X, Y, Z - 1);
					break;
				default:
					throw new ArgumentException("The specified side is invalid.");
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



		private static bool IsValid(int x, int y, int z) {
			return x >= Min && y >= Min && z >= Min && x <= Max && y <= Max && z <= Max;
		}
	}
}
