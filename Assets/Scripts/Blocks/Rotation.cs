using DoubleSocket.Utility.BitBuffer;
using UnityEngine;
using UnityEngine.Assertions;

namespace Blocks {
	/// <summary>
	/// Byte structure: FFZZYYXX.
	/// X, Y and Z specify the rotation amount (range: [0; 3]) around that axis.
	/// FF specifies the axis the rotation is facing (range: [0; 2]).
	/// </summary>
	public static class Rotation {
		public const int SerializedBitsSize = 5;

		private static readonly BlockSides[][] Sides = {
			new[] {BlockSides.Top, BlockSides.Front, BlockSides.Bottom, BlockSides.Back}, //X
			new[] {BlockSides.Front, BlockSides.Right, BlockSides.Back, BlockSides.Left}, //Y
			new[] {BlockSides.Right, BlockSides.Top, BlockSides.Left, BlockSides.Bottom} //Z
		};



		/// <summary>
		/// The Y axis will face towards the specified facing.
		/// The variant specifies extra rotation around the facing axis.
		/// It can be any number, it DOESN'T have to be less than 4.
		/// </summary>
		public static byte GetByte(BlockSides facing, byte variant) {
			switch (facing) {
				case BlockSides.Right:
					return GetByte(variant, 0, 3, 0);
				case BlockSides.Left:
					return GetByte(variant, 0, 1, 0);
				case BlockSides.Top:
					return GetByte(0, variant, 0, 1);
				case BlockSides.Bottom:
					return GetByte(2, variant, 0, 1);
				case BlockSides.Front:
					return GetByte(variant, 1, 1, 2);
				case BlockSides.Back:
					return GetByte(variant, 1, 3, 2);
				default:
					throw new AssertionException("Invalid facing: " + facing, null);
			}
		}

		/// <summary>
		/// Serializes a rotation into a buffer using 5 bits.
		/// </summary>
		public static void Serialize(BitBuffer buffer, byte rotation) {
			BlockSides facing = GetFacing(rotation);
			buffer.WriteBits(BlockSide.ToOrdinal(facing), 3);
			buffer.WriteBits((ulong)GetAmount(rotation, (facing & BlockSides.Y) != BlockSides.None ? 1 : 0), 2);
		}

		/// <summary>
		/// Deserializes a rotation from a buffer's first 5 bits.
		/// </summary>
		public static byte Deserialize(BitBuffer buffer) {
			return GetByte(BlockSide.FromOrdinal((byte)buffer.ReadBits(3)), (byte)buffer.ReadBits(2));
		}

		/// <summary>
		/// The parameters specify how many times the object should be rotated around the axises.
		/// They can be any numbers, they DON'T have to be less than 4
		/// (except 'extra', which specifies extra information).
		/// </summary>
		private static byte GetByte(int x, int y, int z, int extra) {
			return (byte)((x % 4) | ((y % 4) << 2) | ((z % 4) << 4) | (extra << 6));
		}

		/// <summary>
		/// Returns the rotation amount (0-3, both inclusive) around the specified axis (0-2, both inclusive).
		/// Can also be used to get the axis the rotation is facing using the axis: 3
		/// </summary>
		public static int GetAmount(byte rotation, int axis) {
			return (rotation & (3 << (2 * axis))) >> (2 * axis);
		}

		/// <summary>
		/// Returns the exact BlockSide the rotation is facing. If the axis is enough, use GetAmount(rotation, 3)
		/// </summary>
		public static BlockSides GetFacing(byte rotation) {
			switch (GetAmount(rotation, 3)) {
				case 0:
					return GetAmount(rotation, 2) == 3 ? BlockSides.Right : BlockSides.Left;
				case 1:
					return GetAmount(rotation, 0) == 0 ? BlockSides.Top : BlockSides.Bottom;
				case 2:
					return GetAmount(rotation, 2) == 1 ? BlockSides.Front : BlockSides.Back;
				default:
					throw new AssertionException("Invalid rotation: " + rotation, null);
			}
		}

		/// <summary>
		/// Returns a Quaternion which represents the rotation specified by the byte.
		/// </summary>
		public static Quaternion GetQuaternion(byte rotation) {
			return Quaternion.Euler(
				GetAmount(rotation, 0) * 90,
				GetAmount(rotation, 1) * 90,
				GetAmount(rotation, 2) * 90
			);
		}

		/// <summary>
		/// Rotates all of the specified sides by the specified rotation around all axises.
		/// </summary>
		public static BlockSides RotateSides(BlockSides sides, byte rotation) {
			if (rotation == 0 || sides == BlockSides.None || sides == BlockSides.All) {
				return sides;
			}

			int facingAxis = GetAmount(rotation, 3);
			int variantStorage = facingAxis == 1 ? 1 : 0;
			int output = (int)sides;

			for (int axis = 2; axis >= 0; axis--) {
				if (axis == variantStorage) {
					continue;
				}

				int amount = GetAmount(rotation, axis);
				if (amount == 0) {
					continue;
				}

				output = RotateSidesAroundAxis(output, amount, axis);
			}

			int variant = GetAmount(rotation, variantStorage);
			output = RotateSidesAroundAxis(output, facingAxis == 2 ? (4 - variant) % 4 : variant, facingAxis);
			return (BlockSides)output;
		}

		/// <summary>
		/// Rotates all of the specified sides amount times around the axis.
		/// </summary>
		private static int RotateSidesAroundAxis(int sides, int amount, int axis) {
			int output = sides;
			BlockSides[] array = Sides[axis];
			int removed = 0;
			int added = 0;

			for (int bit = 0; bit < 6; bit++) {
				if (bit / 2 == axis) {
					continue;
				}

				int mask = 1 << bit;
				int side = output & mask;
				if (side == 0) {
					continue;
				}

				for (int i = 0; i < array.Length; i++) {
					if (side != (int)array[i]) {
						continue;
					}

					removed |= mask;
					added |= (int)array[(i + amount) % 4];
					break;
				}
			}
			return (output & ~removed) | added;
		}
	}
}
