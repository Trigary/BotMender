using DoubleSocket.Utility.ByteBuffer;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// Extension methods for the ByteBuffer class, making it easy-to-use with Unity's data types.
	/// </summary>
	public static class ByteBufferExtensions {
		/// <summary>
		/// Writes the specified Vector3 into the buffer as 3 floats.
		/// </summary>
		public static void Write(this ByteBuffer buffer, Vector3 value) {
			buffer.Write(value.x);
			buffer.Write(value.y);
			buffer.Write(value.z);
		}

		/// <summary>
		/// Writes the specified Quaternion into the buffer as 4 floats.
		/// </summary>
		public static void Write(this ByteBuffer buffer, Quaternion value) { //TODO smallest 3 encoding
			buffer.Write(value.x);
			buffer.Write(value.y);
			buffer.Write(value.z);
			buffer.Write(value.w);
		}




		/// <summary>
		/// Reads a Vector3 from the buffer from 3 floats.
		/// </summary>
		public static Vector3 ReadVector3(this ByteBuffer buffer) {
			return new Vector3(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
		}

		/// <summary>
		/// Reads a Quaternion from the buffer from 4 floats.
		/// </summary>
		public static Quaternion ReadQuaternion(this ByteBuffer buffer) {
			return new Quaternion(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
		}
	}
}
