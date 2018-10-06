using DoubleSocket.Utility.BitBuffer;
using UnityEngine;
using Utilities;

namespace Playing {
	/// <summary>
	/// A storage for a possible state of a bot, including its ID and its controlling player's inputs.
	/// </summary>
	public class BotState {
		public const int InputSerializedBitsSize = 6 + 3 * 32;
		public const int SerializedBitsSize = 53 * 8 + InputSerializedBitsSize;



		/// <summary>
		/// Serializeses the player input into the specified buffer.
		/// </summary>
		public static void SerializePlayerInput(BitBuffer buffer, Vector3 movementInput, Vector3 trackedPosition) {
			int serialized = 0;
			SetMovementInputAxis(ref serialized, movementInput.x, 0);
			SetMovementInputAxis(ref serialized, movementInput.y, 2);
			SetMovementInputAxis(ref serialized, movementInput.z, 4);
			buffer.WriteBits((ulong)serialized, 6);
			buffer.Write(trackedPosition);
		}

		private static void SetMovementInputAxis(ref int serialized, float value, int offset) {
			if (value > 0) {
				serialized |= 1 << offset;
			} else if (value < 0) {
				serialized |= 1 << (offset + 1);
			}
		}



		/// <summary>
		/// Deserializeses the player input from the specified buffer.
		/// </summary>
		public static void DeserializePlayerInput(BitBuffer buffer, out Vector3 movementInput, out Vector3 trackedPosition) {
			int input = (int)buffer.ReadBits(6);
			movementInput = new Vector3(GetInputAxis(input, 0), GetInputAxis(input, 2), GetInputAxis(input, 4));
			trackedPosition = buffer.ReadVector3();
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
		/// Serializes the state (found in the parameters) and ID of a bot into the specified buffer.
		/// </summary>
		public static void SerializeState(BitBuffer buffer, byte id, Vector3 movementInput,
										Vector3 trackedPosition, Transform transform, Rigidbody body) {
			buffer.Write(id);
			SerializePlayerInput(buffer, movementInput, trackedPosition);
			buffer.Write(transform.position);
			buffer.WriteCompressed(transform.rotation);
			buffer.Write(body.velocity);
			buffer.Write(body.angularVelocity);
		}



		public byte Id { get; private set; }
		public Vector3 MovementInput { get; private set; }
		public Vector3 TrackedPosition { get; private set; }
		public Vector3 Position { get; private set; }
		public Quaternion Rotation { get; private set; }
		public Vector3 Velocity { get; private set; }
		public Vector3 AngularVelocity { get; private set; }

		/// <summary>
		/// Deserializes the next state in the specified buffer into this instance.
		/// This is to make instances reusable.
		/// </summary>
		public void Update(BitBuffer buffer) {
			Id = buffer.ReadByte();
			DeserializePlayerInput(buffer, out Vector3 movementInput, out Vector3 trackedPosition);
			MovementInput = movementInput;
			TrackedPosition = trackedPosition;
			Position = buffer.ReadVector3();
			Rotation = buffer.ReadCompressedQuaternion();
			Velocity = buffer.ReadVector3();
			AngularVelocity = buffer.ReadVector3();
		}
	}
}
