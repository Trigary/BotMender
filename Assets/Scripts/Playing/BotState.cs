using DoubleSocket.Utility.BitBuffer;
using UnityEngine;
using Utilities;

namespace Playing {
	/// <summary>
	/// A storage for a possible state of a bot, including its ID.
	/// </summary>
	public class BotState {
		public const int SerializedBitsSize = 53 * 8 + PlayerInput.SerializedBitsSize;

		/// <summary>
		/// Serializes the state (found in the parameters) and ID of a bot into the specified buffer.
		/// </summary>
		public static void SerializeState(BitBuffer buffer, byte id, Vector3 movementInput,
										Vector3 trackedPosition, Transform transform, Rigidbody body) {
			buffer.Write(id);
			PlayerInput.SerializeMovementInput(buffer, movementInput);
			buffer.Write(trackedPosition);
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
			MovementInput = PlayerInput.DeserializeMovementInput(buffer);
			TrackedPosition = buffer.ReadVector3();
			Position = buffer.ReadVector3();
			Rotation = buffer.ReadCompressedQuaternion();
			Velocity = buffer.ReadVector3();
			AngularVelocity = buffer.ReadVector3();
		}
	}
}
