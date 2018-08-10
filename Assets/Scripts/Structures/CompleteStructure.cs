using System;
using System.Collections.Generic;
using Systems;
using Blocks;
using Blocks.Info;
using Blocks.Live;
using DoubleSocket.Utility.ByteBuffer;
using JetBrains.Annotations;
using Playing;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Structures {
	/// <summary>
	/// A structure which is no longer editable, but is damagable and destructable.
	/// Internally creates a Rigidbody which is destroyed when the script is destroyed.
	/// </summary>
	public class CompleteStructure : MonoBehaviour {
		public const float RigidbodyDragMultiplier = 0.0025f;
		public const float RigidbodyDragOffset = 0.0025f;
		public const float RigidbodyAngularDrag = 0.075f;

		public uint MaxHealth { get; private set; }
		public uint Health { get; private set; }
		public uint Mass { get; private set; }
		private readonly IDictionary<BlockPosition, ILiveBlock> _blocks = new Dictionary<BlockPosition, ILiveBlock>();
		private readonly SystemManager _systems = new SystemManager();
		private BlockPosition _mainframePosition;
		private Rigidbody _body;
		private byte _inputByte;
		private Vector3 _input = Vector3.zero;

		public void Awake() {
			_body = gameObject.AddComponent<Rigidbody>();
			_body.angularDrag = RigidbodyAngularDrag;
		}



		/// <summary>
		/// Loads this structure into the a new GameObject using the given serialized blocks.
		/// Also creates all required components for the GameObject.
		/// Lazily validates the data and returns false if it is found invalid.
		/// No checks are made, the EditableStructure should be used for that.
		/// </summary>
		[CanBeNull]
		public static CompleteStructure Create(ulong[] serialized, string gameObjectName) {
			CompleteStructure structure = new GameObject(gameObjectName).AddComponent<CompleteStructure>();
			if (!structure.Deserialize(serialized)) {
				Destroy(structure);
				return null;
			}

			structure.MaxHealth = structure.Health;
			structure._systems.Finished();
			structure.ApplyMass(false);
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
						if (block == null) {
							return false;
						}

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



		public void FixedUpdate() {
			_systems.Tick(_body);
			_systems.MoveRotate(_body, _input);
			_body.drag = _body.velocity.sqrMagnitude * RigidbodyDragMultiplier + RigidbodyDragOffset;
		}



		/// <summary>
		/// Should only be called by a RealLiveBlock instance when it is damaged.
		/// </summary>
		public void Damaged(RealLiveBlock block, uint damage) {
			Health -= damage;
			if (block.Health != 0) {
				return;
			}

			if (block.Info.Type == BlockType.Mainframe) {
				Destroy(gameObject);
				return;
			}

			RemoveBlock(block);
			RemoveNotConnectedBlocks();
			ApplyMass(true);
		}

		private void RemoveBlock(RealLiveBlock block) {
			Mass -= block.Info.Mass;
			_systems.TryRemove(block.Position);
			Destroy(block.gameObject);

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
			StructureUtilities.RemoveConnected(blocks[_mainframePosition], blocks);
			foreach (ILiveBlock block in blocks.Values) {
				RealLiveBlock real = block as RealLiveBlock;
				if (real != null) {
					Health -= real.Health;
					RemoveBlock(real);
				}
			}
		}



		/// <summary>
		/// Applies the StateUpdate received from the client. Should only be called by the server.
		/// </summary>
		public void UpdateState(byte input) {
			_inputByte = input;
			_input = PlayerInput.Deserialize(input);
		}

		/// <summary>
		/// Applies the StateUpdate received from the server. Should only be called by a client.
		/// </summary>
		public void UpdateState(byte input, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity) {
			_inputByte = input;
			_input = PlayerInput.Deserialize(input);
			transform.position = position;
			transform.rotation = rotation;
			_body.velocity = velocity;
			_body.angularVelocity = angularVelocity;
		}

		/// <summary>
		/// Serializeses this bot's current state into the specified buffer.
		/// </summary>
		public void SerializeState(ByteBuffer buffer) {
			buffer.Write(_inputByte);
			buffer.Write(transform.position);
			buffer.Write(transform.rotation);
			buffer.Write(_body.velocity);
			buffer.Write(_body.angularVelocity);
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



		private void ApplyMass(bool keepLocation) {
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
			if (keepLocation) {
				transform.position += center;
			}

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
