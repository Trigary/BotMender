using System;
using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Systems;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Structures {
	/// <summary>
	/// A structure which is no longer editable, but is damagable and destructable.
	/// Internally creates a Rigidbody.
	/// </summary>
	public class CompleteStructure : MonoBehaviour {
		public uint MaxHealth { get; private set; }
		public uint Health { get; private set; }
		public uint Mass { get; private set; }
		private readonly IDictionary<BlockPosition, ILiveBlock> _blocks = new Dictionary<BlockPosition, ILiveBlock>();
		private readonly SystemStorage _systems = new SystemStorage();
		private BlockPosition _mainframePosition;
		private Rigidbody _body;

		public void Awake() {
			_body = gameObject.AddComponent<Rigidbody>();
		}



		/// <summary>
		/// Loads this structure using the given serialized blocks.
		/// Lazely validates the data and returns null, if it is found invalid.
		/// No checks are made, the EditableStructure should be used for that.
		/// </summary>
		[CanBeNull]
		public static CompleteStructure Create(ulong[] serialized, string gameObjectName = "CompleteStructure") {
			CompleteStructure structure = new GameObject(gameObjectName).AddComponent<CompleteStructure>();
			if (!structure.Deserialize(serialized)) {
				Destroy(structure.gameObject);
				return null;
			}

			structure.MaxHealth = structure.Health;
			structure._systems.Finished();
			structure.ApplyMass();
			return structure;
		}

		private bool Deserialize(ulong[] serialized) {
			try {
				foreach (ulong value in serialized) {
					byte[] bytes = BitConverter.GetBytes(value);
					uint type = BitConverter.ToUInt32(bytes, 4);
					if (type >= BlockFactory.TypeCount) {
						return false;
					}

					BlockPosition position;
					if (!BlockPosition.FromComponents(bytes[0], bytes[1], bytes[2], out position)) {
						return false;
					}

					BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType((int)type));
					if (info.Type == BlockType.Mainframe) {
						_mainframePosition = position;
					}

					SingleBlockInfo single = info as SingleBlockInfo;
					RealLiveBlock block;
					if (single != null) {
						block = BlockFactory.MakeSingleLive(transform, single, bytes[3], position);
					} else {
						LiveMultiBlockPart[] parts;
						block = BlockFactory.MakeMultiLive(transform, (MultiBlockInfo)info, bytes[3], position, out parts);
						foreach (LiveMultiBlockPart part in parts) {
							_blocks.Add(part.Position, part);
						}
					}

					Health += info.Health;
					Mass += info.Mass;
					_blocks.Add(position, block);

					BotSystem system;
					if (SystemFactory.Create(block, out system)) {
						_systems.Add(position, system);
					}
				}
			} catch (Exception e) {
				Debug.Log("Exception caught while deserializing into a CompleteStructure: " + e);
				return false;
			}
			return true;
		}



		/// <summary>
		/// Should only be called by a RealLiveBlock instance when it is damaged.
		/// </summary>
		public void Damaged(RealLiveBlock block, uint damage) { //TODO events, etc.
			Health -= damage;
			if (block.Health != 0) {
				return;
			}

			if (block.Info.Type == BlockType.Mainframe) {
				//TODO destroy the structure
				return;
			}
			
			RemoveBlock(block);
			RemoveNotConnectedBlocks();
			ApplyMass();
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void RemoveBlock(RealLiveBlock block) {
			Mass -= block.Info.Mass;
			_systems.TryRemove(block.Position);

			Assert.IsTrue(_blocks.Remove(block.Position), "The block is not present.");
			LiveMultiBlockParent parent = block as LiveMultiBlockParent;
			if (parent == null) {
				return; //block is LiveSingleBlock
			}

			foreach (LiveMultiBlockPart part in parent.Parts) {
				Assert.IsTrue(_blocks.Remove(part.Position), "A part of the multi block is not present.");
			}
		}

		private void RemoveNotConnectedBlocks() {
			IDictionary<BlockPosition, ILiveBlock> blocks = new Dictionary<BlockPosition, ILiveBlock>(_blocks);
			StructureUtilities.RemoveConnected(blocks[_mainframePosition], -1, blocks);
			foreach (ILiveBlock block in blocks.Values) {
				RealLiveBlock real = block as RealLiveBlock;
				if (real != null) {
					Health -= real.Health;
					RemoveBlock(real);
				}
			}
		}



		/// <summary>
		/// Executes the propulsion systems.
		/// </summary>
		public void MoveRotate(float x, float y, float z) {
			_systems.MoveRotate(_body, x, y, z);
		}

		/// <summary>
		/// Rotates the weapons.
		/// </summary>
		public void TrackTarget(Vector3 target) {
			_systems.TrackTarget(target);
		}

		/// <summary>
		/// Executes the weapon systems.
		/// </summary>
		public void FireWeapons() {
			_systems.FireWeapons(_body);
		}

		/// <summary>
		/// Executes the active system.
		/// </summary>
		public void UseActive() {
			_systems.UseActive(_body);
		}



		private void ApplyMass() {
			Vector3 center = new Vector3();
			uint mass = 0;
			foreach (ILiveBlock block in _blocks.Values) {
				RealLiveBlock real = block as RealLiveBlock;
				if (real == null) {
					continue;
				}
				center += real.transform.localPosition * real.Info.Mass;
				mass += real.Info.Mass;
			}

			center /= mass;
			transform.position += center;
			foreach (ILiveBlock block in _blocks.Values) {
				RealLiveBlock real = block as RealLiveBlock;
				if (real != null) {
					real.transform.position -= center;
				}
			}
			_body.mass = (float)Mass / 1000;
		}
	}
}
