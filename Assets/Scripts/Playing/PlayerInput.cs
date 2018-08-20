using DoubleSocket.Utility.BitBuffer;
using UnityEngine;

namespace Playing {
	/// <summary>
	/// A utility class containing methods regarding the player's input.
	/// </summary>
	public static class PlayerInput {
		public const int SerializedBitsSize = 6;

		/// <summary>
		/// Reads the current player input and serializes it into a byte.
		/// </summary>
		public static byte Serialize() {
			int serialized = 0;
			SetInputAxis(ref serialized, Input.GetAxisRaw("Rightward"), 0);
			SetInputAxis(ref serialized, Input.GetAxisRaw("Upward"), 2);
			SetInputAxis(ref serialized, Input.GetAxisRaw("Forward"), 4);
			return (byte)serialized;
		}

		/// <summary>
		/// Serializes the specified iput into the buffer.
		/// </summary>
		public static void Serialize(BitBuffer buffer, Vector3 input) {
			int serialized = 0;
			SetInputAxis(ref serialized, input.x, 0);
			SetInputAxis(ref serialized, input.y, 0);
			SetInputAxis(ref serialized, input.z, 0);
			buffer.WriteBits((ulong)serialized, SerializedBitsSize);
		}

		private static void SetInputAxis(ref int serialized, float value, int offset) {
			if (value > 0) {
				serialized |= 1 << offset;
			} else if (value < 0) {
				serialized |= 1 << (offset + 1);
			}
		}



		/// <summary>
		/// Converts the serialized representation of the player input found in the buffer into a Vector3 representation.
		/// </summary>
		public static Vector3 Deserialize(BitBuffer buffer) {
			int input = (int)buffer.ReadBits(SerializedBitsSize);
			return new Vector3(GetInputAxis(input, 0), GetInputAxis(input, 2), GetInputAxis(input, 4));
		}

		private static float GetInputAxis(int input, int offset) {
			if ((input & (1 << offset)) == (1 << offset)) {
				return 1;
			} else if ((input & (1 << (offset + 1))) == (1 << (offset + 1))) {
				return -1;
			} else {
				return 0;
			}
		}
	}
}
