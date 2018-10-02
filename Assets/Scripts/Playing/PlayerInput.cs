using DoubleSocket.Utility.BitBuffer;
using UnityEngine;
using Utilities;

namespace Playing {
	/// <summary>
	/// A utility class containing methods regarding the player's movement input and tracked position.
	/// </summary>
	public static class PlayerInput {
		public const int SerializedBitsSize = 6 + 3 * 32;



		/// <summary>
		/// Reads the current movement input and returns it.
		/// </summary>
		public static Vector3 ReadMovementInput() {
			return new Vector3(Input.GetAxisRaw("Rightward"), Input.GetAxisRaw("Upward"), Input.GetAxisRaw("Forward"));
		}



		/// <summary>
		/// Serializes only the movement direction into the buffer.
		/// </summary>
		public static void SerializeMovementInput(BitBuffer buffer, Vector3 movementInput) {
			int serialized = 0;
			SetInputAxis(ref serialized, movementInput.x, 0);
			SetInputAxis(ref serialized, movementInput.y, 2);
			SetInputAxis(ref serialized, movementInput.z, 4);
			buffer.WriteBits((ulong)serialized, 6);
		}

		/// <summary>
		/// Deserializes only the movement direction from the buffer.
		/// </summary>
		public static Vector3 DeserializeMovementInput(BitBuffer buffer) {
			int input = (int)buffer.ReadBits(6);
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



		/// <summary>
		/// Serializeses the specified parameters into the specified buffer.
		/// </summary>
		public static void Serialize(BitBuffer buffer, Vector3 movementInput, Vector3 trackedPosition) {
			int serialized = 0;
			SetInputAxis(ref serialized, movementInput.x, 0);
			SetInputAxis(ref serialized, movementInput.y, 2);
			SetInputAxis(ref serialized, movementInput.z, 4);
			buffer.WriteBits((ulong)serialized, 6);
			buffer.Write(trackedPosition);
		}

		private static void SetInputAxis(ref int serialized, float value, int offset) {
			if (value > 0) {
				serialized |= 1 << offset;
			} else if (value < 0) {
				serialized |= 1 << (offset + 1);
			}
		}

		/// <summary>
		/// Deserializeses to the out parameters from the specified buffer.
		/// </summary>
		public static void Deserialize(BitBuffer buffer, out Vector3 movementInput, out Vector3 trackedPosition) {
			int input = (int)buffer.ReadBits(6);
			movementInput = new Vector3(GetInputAxis(input, 0), GetInputAxis(input, 2), GetInputAxis(input, 4));
			trackedPosition = buffer.ReadVector3();
		}
	}
}
