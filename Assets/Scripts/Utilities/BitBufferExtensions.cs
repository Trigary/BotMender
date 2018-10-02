using DoubleSocket.Utility.BitBuffer;
using Networking;
using UnityEngine;

namespace Utilities {
	/// <summary>
	/// Extension methods for the BitBuffer class, making it easy to use with Unity's data types.
	/// </summary>
	public static class BitBufferExtensions {
		//TODO compression: specify min value, max value and precision (as bit count or float or 1/x)
		//utility methods for min value = 0
		//should the min, max be inclusive or exclusive? maybe both variants




		/// <summary>
		/// Writes the specified Vector3 into the buffer as 3 floats.
		/// </summary>
		public static void Write(this BitBuffer buffer, Vector3 value) {
			buffer.Write(value.x);
			buffer.Write(value.y);
			buffer.Write(value.z);
		}

		/// <summary>
		/// Reads a Vector3 from the buffer from 3 floats.
		/// </summary>
		public static Vector3 ReadVector3(this BitBuffer buffer) {
			return new Vector3(buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadFloat());
		}



		/// <summary>
		/// Writes the specified Quaternion into using smallest-three encoding and 16 bit precision.
		/// </summary>
		public static void WriteCompressed(this BitBuffer buffer, Quaternion value) {
			float largestValue = float.MinValue;
			int largestIndex = -1;
			for (int i = 0; i < 4; i++) {
				if (value[i] >= largestValue) {
					largestValue = value[i];
					largestIndex = i;
				}
			}

			int sign = value[largestIndex] > 0 ? 1 : -1;
			buffer.WriteBits((ulong)largestIndex, 2);

			for (int i = 0; i < 4; i++) {
				if (i != largestIndex) {
					buffer.Write((short)(value[i] * sign * short.MaxValue));
				}
			}
		}

		/// <summary>
		/// Reads a smallest-three encoded, 16 bit precise Quaternion from the buffer.
		/// </summary>
		public static Quaternion ReadCompressedQuaternion(this BitBuffer buffer) {
			int largestIndex = (int)buffer.ReadBits(2);
			float a = buffer.ReadShort() / (float)short.MaxValue;
			float b = buffer.ReadShort() / (float)short.MaxValue;
			float c = buffer.ReadShort() / (float)short.MaxValue;
			float largestValue = Mathf.Sqrt(1 - (a * a + b * b + c * c));

			if (largestIndex == 0) {
				return new Quaternion(largestValue, a, b, c);
			} else if (largestIndex == 1) {
				return new Quaternion(a, largestValue, b, c);
			} else if (largestIndex == 2) {
				return new Quaternion(a, b, largestValue, c);
			} else {
				return new Quaternion(a, b, c, largestValue);
			}
		}



		/// <summary>
		/// Writes the timestamp as a 24-bit long value encoded using the GlobalBaseTimestamp.
		/// </summary>
		public static void WriteTimestamp(this BitBuffer buffer, long timestamp) {
			buffer.WriteBits((ulong)(timestamp - NetworkUtils.GlobalBaseTimestamp), 24);
		}

		/// <summary>
		/// Reads a 24-bit long value, decodes it into a timestamp using the GlobalBaseTimestamp and returns it.
		/// </summary>
		public static long ReadTimestamp(this BitBuffer buffer) {
			return (long)buffer.ReadBits(24) + NetworkUtils.GlobalBaseTimestamp;
		}
	}
}
