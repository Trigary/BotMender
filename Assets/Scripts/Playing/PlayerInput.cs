using UnityEngine;

namespace Playing {
	/// <summary>
	/// A utility class containing methods regarding the player's input.
	/// </summary>
	public static class PlayerInput {
		/// <summary>
		/// Reads the current player input and serializes it into a byte.
		/// </summary>
		public static byte Serialize() {
			int input = 0;
			SetInputAxis(ref input, "Rightward", 0);
			SetInputAxis(ref input, "Upward", 2);
			SetInputAxis(ref input, "Forward", 4);
			return (byte)input;
		}

		private static void SetInputAxis(ref int input, string axis, int offset) {
			float value = Input.GetAxisRaw(axis);
			if (value > 0) {
				input |= 1 << offset;
			} else if (value < 0) {
				input |= 1 << (offset + 1);
			}
		}



		/// <summary>
		/// Converts the serialized byte representation of the player input into a Vector3 representation.
		/// </summary>
		public static Vector3 Deserialize(byte input) {
			return new Vector3(GetInputAxis(input, 0), GetInputAxis(input, 2), GetInputAxis(input, 4));
		}

		private static float GetInputAxis(byte input, int offset) {
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
