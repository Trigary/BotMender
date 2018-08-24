using System.Collections.Generic;
using System.Linq;
using Systems;
using Blocks;
using Blocks.Info;
using Blocks.Live;
using Blocks.Placed;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using Playing;
using UnityEngine;
using UnityEngine.Assertions;

namespace Structures {
	/// <summary>
	/// A structure which is no longer editable, but is damagable and destructable.
	/// Internally creates a Rigidbody which is destroyed when the script is destroyed.
	/// </summary>
	public class CompleteStructure : MonoBehaviour {
		public const float RigidbodyDragMultiplier = 0.0025f;
		public const float RigidbodyDragOffset = 0.0025f;
		public const float RigidbodyAngularDrag = 3f;

		public byte Id { get; private set; }
		public uint MaxHealth { get; private set; }
		public uint Health { get; private set; }
		public uint Mass { get; private set; }
		public Vector3 MovementInput { get; private set; } = Vector3.zero;
		private readonly IDictionary<BlockPosition, ILiveBlock> _blocks = new Dictionary<BlockPosition, ILiveBlock>();
		private readonly SystemManager _systems = new SystemManager();
		private BlockPosition _mainframePosition;
		private Rigidbody _body;

		private void Awake() {
			_body = gameObject.AddComponent<Rigidbody>();
			_body.angularDrag = RigidbodyAngularDrag;
		}



		/// <summary>
		/// Loads the structure created from the given buffer into a new GameObject.
		/// Also creates all required components for the GameObject.
		/// No validation is done, the EditableStructure should be used for that.
		/// </summary>
		[CanBeNull]
		public static CompleteStructure Create(BitBuffer buffer, byte id, string gameObjectName) {
			CompleteStructure structure = new GameObject(gameObjectName).AddComponent<CompleteStructure>();
			structure.Id = id;
			structure.Deserialize(buffer);
			structure.MaxHealth = structure.Health;
			structure._systems.Finished();
			structure.ApplyMass(false);
			return structure;
		}

		private void Deserialize(BitBuffer buffer) {
			while (buffer.TotalBitsLeft >= RealPlacedBlock.SerializedBitsSize) {
				ushort type = (ushort)buffer.ReadBits(14);
				BlockPosition.FromComponents((int)buffer.ReadBits(7), (int)buffer.ReadBits(7),
					(int)buffer.ReadBits(7), out BlockPosition position);

				BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(type));
				if (info.Type == BlockType.Mainframe) {
					_mainframePosition = position;
				}

				RealLiveBlock block;
				if (info is SingleBlockInfo single) {
					block = BlockFactory.MakeSingleLive(transform, single, Rotation.Deserialize(buffer), position);
				} else {
					block = BlockFactory.MakeMultiLive(transform, (MultiBlockInfo)info, Rotation.Deserialize(buffer),
						position, out LiveMultiBlockPart[] parts);
					foreach (LiveMultiBlockPart part in parts) {
						_blocks.Add(part.Position, part);
					}
				}

				Health += info.Health;
				Mass += info.Mass;
				_blocks.Add(position, block);
				if (SystemFactory.Create(_systems.NextId, this, block, out BotSystem system)) {
					_systems.Add(position, system);
				}
			}
		}



		/// <summary>
		/// Should only be called by the NetworkedPhyiscs class.
		/// This method applies the player input, simulating a part of or a while FixedUpdate (see: timestepMultiplier).
		/// Does not replace the FixedUpdate call, this method relies on it being called before the next normal physics step.
		/// </summary>
		public void SimulatedPhysicsUpdate(float timestepMultiplier) {
			_systems.MoveRotate(_body, MovementInput, timestepMultiplier);
		}

		private void FixedUpdate() {
			_systems.Tick(_body);
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
		/// Applies the state given as the parameter. Should only be called by a client.
		/// </summary>
		public void UpdateWholeState(BotState state) {
			MovementInput = state.MovementInput;
			_systems.TrackedPosition = state.TrackedPosition;
			transform.position = state.Position;
			transform.rotation = state.Rotation;
			_body.velocity = state.Velocity;
			_body.angularVelocity = state.AngularVelocity;
		}

		/// <summary>
		/// Applies the input update received from the client specified in the buffer.
		/// Should only be called by the server or the local client.
		/// </summary>
		public void UpdateInputOnly(Vector3 movementInput, Vector3? trackedPosition) {
			MovementInput = movementInput;
			if (trackedPosition.HasValue) {
				_systems.TrackedPosition = trackedPosition.Value;
			}
		}

		/// <summary>
		/// Serializeses this bot's current state and its ID into the specified buffer.
		/// Should only be called by the server.
		/// </summary>
		public void SerializeState(BitBuffer buffer) {
			BotState.SerializeState(buffer, Id, MovementInput, _systems.TrackedPosition, transform, _body);
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
