using System.Collections.Generic;
using System.Linq;
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
		/// Loads the structure created from the given buffer into a new GameObject.
		/// Also creates all required components for the GameObject.
		/// No validation is done, the EditableStructure should be used for that.
		/// </summary>
		[CanBeNull]
		public static CompleteStructure Create(ByteBuffer buffer, string gameObjectName) {
			CompleteStructure structure = new GameObject(gameObjectName).AddComponent<CompleteStructure>();
			structure.Deserialize(buffer);
			structure.MaxHealth = structure.Health;
			structure._systems.Finished();
			structure.ApplyMass(false);
			return structure;
		}

		private void Deserialize(ByteBuffer buffer) {
			while (buffer.BytesLeft > 0) {
				ushort type = buffer.ReadUShort();
				BlockPosition.FromComponents(buffer.ReadByte(), buffer.ReadByte(), buffer.ReadByte(), out BlockPosition position);

				BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(type));
				if (info.Type == BlockType.Mainframe) {
					_mainframePosition = position;
				}

				RealLiveBlock block;
				if (info is SingleBlockInfo single) {
					block = BlockFactory.MakeSingleLive(transform, single, buffer.ReadByte(), position);
				} else {
					block = BlockFactory.MakeMultiLive(transform, (MultiBlockInfo)info, buffer.ReadByte(),
						position, out LiveMultiBlockPart[] parts);
					foreach (LiveMultiBlockPart part in parts) {
						_blocks.Add(part.Position, part);
					}
				}

				Health += info.Health;
				Mass += info.Mass;
				_blocks.Add(position, block);
				if (SystemFactory.Create(block, out BotSystem system)) {
					_systems.Add(position, system);
				}
			}
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
			foreach (RealLiveBlock real in blocks.Values.OfType<RealLiveBlock>()) {
				Health -= real.Health;
				RemoveBlock(real);
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
			foreach (RealLiveBlock real in _blocks.Values.OfType<RealLiveBlock>()) {
				center += real.transform.localPosition * real.Info.Mass;
				mass += real.Info.Mass;
			}

			center /= mass;
			if (keepLocation) {
				transform.position += center;
			}

			foreach (RealLiveBlock real in _blocks.Values.OfType<RealLiveBlock>()) {
				real.transform.position -= center;
			}
			_body.mass = (float)Mass / 1000;
		}
	}
}
