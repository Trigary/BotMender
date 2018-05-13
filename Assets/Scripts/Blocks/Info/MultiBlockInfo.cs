using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Blocks.Info {
	/// <summary>
	/// Information about a specific multi block type.
	/// </summary>
	public class MultiBlockInfo : BlockInfo {
		private readonly List<KeyValuePair<Vector3Int, BlockSides>> _partConnectSides = new List<KeyValuePair<Vector3Int, BlockSides>>();
		
		public MultiBlockInfo(BlockType type, uint health, uint mass, GameObject prefab) :
			base(type, health, mass, prefab) {
		}



		/// <summary>
		/// Sets the sides at which a specific part of the multi block can connect to other blocks.
		/// Returns itself for chaining. 0;0;0 must be set.
		/// </summary>
		public MultiBlockInfo Add(byte x, byte y, byte z, BlockSides connectSides) {
			_partConnectSides.Add(new KeyValuePair<Vector3Int, BlockSides>(new Vector3Int(x, y, z), connectSides));
			return this;
		}

		/// <summary>
		/// Makes sure that no excess memory is allocated.
		/// </summary>
		public void Finished() {
			_partConnectSides.TrimExcess();
		}



		/// <summary>
		/// Gets the positions of all parts in the multi blocks relative to the specified origin.
		/// Both the positions are and the connection points are rotated.
		/// Returns false if any of these positions is out of bounds.
		/// </summary>
		public bool GetRotatedPositions(BlockPosition origin, byte rotation, out KeyValuePair<BlockPosition, BlockSides>[] output) {
			output = new KeyValuePair<BlockPosition, BlockSides>[_partConnectSides.Count];
			Quaternion rotationQuaternion = Rotation.GetQuaternion(rotation);

			for (int index = 0; index < _partConnectSides.Count; index++) {
				KeyValuePair<Vector3Int, BlockSides> pair = _partConnectSides[index];
				Vector3Int offset = Vector3Int.RoundToInt(rotationQuaternion * pair.Key);

				BlockPosition position;
				if (!origin.GetOffseted(offset.x, offset.y, offset.z, out position)) {
					output = null;
					return false;
				}

				output[index] = new KeyValuePair<BlockPosition, BlockSides>(position, Rotation.RotateSides(pair.Value, rotation));
			}
			return true;
		}
	}
}
