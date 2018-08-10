using System;
using System.Collections.Generic;
using System.Linq;
using Systems;
using Blocks;
using Blocks.Info;
using Blocks.Placed;
using DoubleSocket.Utility.ByteBuffer;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Structures {
	/// <summary>
	/// A structure which is editable. It doesn't have health and it may contain not connected blocks.
	/// </summary>
	public class EditableStructure : MonoBehaviour {
		public int RealBlockCount { get; private set; }

		private readonly IDictionary<BlockPosition, IPlacedBlock> _blocks = new Dictionary<BlockPosition, IPlacedBlock>();
		[CanBeNull] private BlockPosition _mainframePosition;
		private bool _activeSystemPresent;
		private WeaponSystem.Type _weaponType = WeaponSystem.Type.None;
		private int _weaponCount;

		public void Start() {
			if (_blocks.Count == 0) {
				BlockPosition position;
				Assert.IsTrue(BlockPosition.FromVector(transform.position, out position),
					"Failed to get a BlockPosition from the EditableStructure position.");
				Assert.IsTrue(TryAddBlock(position, (MultiBlockInfo)BlockFactory.GetInfo(BlockType.Mainframe), 0),
					"Failed to place the Mainframe.");
			}
		}



		/// <summary>
		/// Determines whether the block can be added checking whether a block is already
		/// present at the position and whether this new block can connect to another block.
		/// </summary>
		public bool CanAddBlock(BlockPosition position, BlockInfo info, byte rotation) {
			if (info.Type == BlockType.Mainframe) {
				if (_mainframePosition != null) {
					return false;
				}
			} else if (SystemFactory.IsActiveSystem(info.Type)) {
				if (_activeSystemPresent) {
					return false;
				}
			} else {
				WeaponSystem.Type weaponType = SystemFactory.GetWeaponType(info.Type);
				if (weaponType != WeaponSystem.Type.None && weaponType != _weaponType && _weaponType != WeaponSystem.Type.None) {
					return false;
				}
			}

			SingleBlockInfo single = info as SingleBlockInfo;
			if (single != null) {
				return !_blocks.ContainsKey(position)
					&& CanConnect(position, Rotation.RotateSides(single.ConnectSides, rotation));
			} else {
				KeyValuePair<BlockPosition, BlockSides>[] positions;
				return ((MultiBlockInfo)info).GetRotatedPositions(position, rotation, out positions)
					&& !positions.Any(pair => _blocks.ContainsKey(pair.Key))
					&& positions.Any(pair => CanConnect(pair.Key, pair.Value));
			}
		}

		/// <summary>
		/// Returns whether a block (which may be a part of a multi block) exists at the specified position.
		/// </summary>
		public bool IsPositionOccupied(BlockPosition position) {
			return _blocks.ContainsKey(position);
		}



		/// <summary>
		/// Tries to add the block. Returns the same as #CanAddBlock.
		/// </summary>
		public bool TryAddBlock(BlockPosition position, BlockInfo info, byte rotation) {
			if (!CanAddBlock(position, info, rotation)) {
				return false;
			}

			SingleBlockInfo single = info as SingleBlockInfo;
			if (single != null) {
				_blocks.Add(position, BlockFactory.MakeSinglePlaced(transform, single, rotation, position));
			} else {
				PlacedMultiBlockPart[] parts;
				PlacedMultiBlockParent parent = BlockFactory.MakeMultiPlaced(transform, (MultiBlockInfo)info,
					rotation, position, out parts);
				if (parent == null) {
					return false;
				}

				_blocks.Add(position, parent);
				foreach (PlacedMultiBlockPart part in parts) {
					_blocks.Add(part.Position, part);
				}
			}

			RealBlockCount++;
			if (info.Type == BlockType.Mainframe) {
				_mainframePosition = position;
			} else if (SystemFactory.IsActiveSystem(info.Type)) {
				_activeSystemPresent = true;
			} else {
				WeaponSystem.Type weaponType = SystemFactory.GetWeaponType(info.Type);
				if (weaponType != WeaponSystem.Type.None) {
					_weaponType = weaponType; //may or may not be first time set
					_weaponCount++;
				}
			}
			return true;
		}



		/// <summary>
		/// Removes the block at the specified position (also deleted the GameObject).
		/// Works for both multi and single blocks.
		/// </summary>
		public void RemoveBlock(BlockPosition position) {
			IPlacedBlock block;
			if (!_blocks.TryGetValue(position, out block)) {
				throw new AssertionException("Can't remove block at a position where no blocks exist.", null);
			}
			RemoveBlock(block);
		}

		private void RemoveBlock(IPlacedBlock block) {
			RealBlockCount--;
			if (block.Position.Equals(_mainframePosition)) {
				_mainframePosition = null;
			} else if (SystemFactory.IsActiveSystem(block.Type)) {
				_activeSystemPresent = false;
			} else if (SystemFactory.GetWeaponType(block.Type) != WeaponSystem.Type.None) {
				if (--_weaponCount == 0) {
					_weaponType = WeaponSystem.Type.None;
				}
			}

			PlacedSingleBlock single = block as PlacedSingleBlock;
			if (single != null) {
				Destroy(single.gameObject);
				_blocks.Remove(single.Position);
				return;
			}

			PlacedMultiBlockParent parent = block as PlacedMultiBlockParent;
			if (parent == null) {
				parent = ((PlacedMultiBlockPart)block).Parent;
			}

			Destroy(parent.gameObject);
			Assert.IsTrue(_blocks.Remove(parent.Position), "The parent of the multi block is not present.");
			foreach (PlacedMultiBlockPart part in parent.Parts) {
				Assert.IsTrue(_blocks.Remove(part.Position), "A part of the multi block is not present.");
			}
		}



		/// <summary>
		/// Get all errors regarding the structure which are included in the enum.
		/// </summary>
		public Errors GetStructureErrors() {
			Errors errors = Errors.None;
			if (_mainframePosition == null) {
				errors |= Errors.NoMainframe;
			}
			if (_weaponCount == 0) {
				errors |= Errors.NoWeapons;
			}
			return errors;
		}

		/// <summary>
		/// Returns the blocks which aren't connected to the mainframe. If there are none, then the structure is valid.
		/// Returns null if the mainframe is not present.
		/// </summary>
		[CanBeNull]
		public IDictionary<BlockPosition, IPlacedBlock> GetNotConnectedBlocks() {
			if (_mainframePosition == null) {
				return null;
			}

			IDictionary<BlockPosition, IPlacedBlock> blocks = new Dictionary<BlockPosition, IPlacedBlock>(_blocks);
			StructureUtilities.RemoveConnected(blocks[_mainframePosition], blocks);
			return blocks;
		}



		/// <summary>
		/// Serialises the structure into the specified buffer.
		/// Data structure of the blocks (each character represents 1 byte): TT XYZR
		/// </summary>
		public void Serialize(ByteBuffer buffer) {
			foreach (IPlacedBlock block in _blocks.Values) {
				RealPlacedBlock real = block as RealPlacedBlock;
				if (real != null) {
					buffer.Write((ushort)block.Type);
					buffer.Write(real.Position.X);
					buffer.Write(real.Position.Y);
					buffer.Write(real.Position.Z);
					buffer.Write(real.Rotation);
				}
			}
		}

		/// <summary>
		/// Overrides the current structure (read: removes all previous blocks) with the serialized one in the buffer.
		/// Lazely validates the data and returns false, if it is found invalid.
		/// No checks are not made, #GetNotConnectedBlocks should be called after this method.
		/// </summary>
		public bool Deserialize(ByteBuffer buffer) { //TODO update
			foreach (IPlacedBlock block in _blocks.Values.ToList()) {
				RealPlacedBlock real = block as RealPlacedBlock;
				if (real != null) {
					RemoveBlock(real);
				}
			}

			try {
				while (buffer.BytesLeft >= RealPlacedBlock.SerializedSize) {
					ushort type = buffer.ReadUShort();
					if (type >= BlockFactory.TypeCount) {
						return false;
					}

					BlockPosition position;
					if (!BlockPosition.FromComponents(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte(), out position)) {
						return false;
					}

					BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(type));
					SingleBlockInfo single = info as SingleBlockInfo;
					bool result = single != null
						? TryAddBlock(position, single, buffer.ReadByte())
						: TryAddBlock(position, (MultiBlockInfo)info, buffer.ReadByte());

					if (!result) {
						return false;
					}
				}
			} catch (Exception e) {
				Debug.Log("Exception caught while deserializing into an EditableStructure: " + e);
				return false;
			}

			if (buffer.BytesLeft != 0) {
				Debug.Log(buffer.BytesLeft + " bytes were left out after deserializing into an EditableStructure.");
				return false;
			}
			return true;
		}



		/// <summary>
		/// Determines whether a block at the specified position with the specified
		/// connection sides can connect to any other block.
		/// </summary>
		private bool CanConnect(BlockPosition position, BlockSides rotatedConnectSides) {
			if (_blocks.Count == 0) {
				return true;
			}

			for (int bit = 0; bit < 6; bit++) {
				BlockSides side = rotatedConnectSides & (BlockSides)(1 << bit);
				BlockPosition offseted;
				IPlacedBlock block;
				if (side == BlockSides.None
					|| !position.GetOffseted(side, out offseted)
					|| !_blocks.TryGetValue(offseted, out block)) {
					continue;
				}

				int inverseBit = bit % 2 == 0 ? bit + 1 : bit - 1;
				if ((block.ConnectSides & (BlockSides)(1 << inverseBit)) != BlockSides.None) {
					return true;
				}
			}
			return false;
		}



		/// <summary>
		/// Errors regarding the structure.
		/// </summary>
		[Flags]
		public enum Errors {
			None = 0,
			NoMainframe = 1 << 0,
			NoWeapons = 1 << 1
		}
	}
}
