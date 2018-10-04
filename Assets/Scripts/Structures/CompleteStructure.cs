using System.Collections.Generic;
using System.Linq;
using Systems;
using Systems.Weapon;
using Blocks;
using Blocks.Info;
using Blocks.Live;
using Blocks.Placed;
using DoubleSocket.Utility.BitBuffer;
using JetBrains.Annotations;
using Networking;
using Playing;
using UnityEngine;
using UnityEngine.Assertions;

namespace Structures {
	/// <summary>
	/// A structure which is no longer editable, but is damagable and destructable.
	/// Internally creates a Rigidbody which is destroyed when the behaviour is destroyed.
	/// </summary>
	public class CompleteStructure : MonoBehaviour {
		public const float RigidbodyDragMultiplier = 0.0025f;
		public const float RigidbodyDragOffset = 0.0025f;
		public const float RigidbodyAngularDrag = 3f;
		public const uint MinHealthPercentage = 5;

		public byte Id { get; private set; }
		public uint MaxHealth { get; private set; }
		public uint Health { get; private set; }
		public uint Mass { get; private set; }
		public Rigidbody Body { get; private set; }
		public Vector3 MovementInput { get; private set; } = Vector3.zero;
		public WeaponSystem.Type WeaponType => _systems.WeaponType;
		private readonly IDictionary<BlockPosition, ILiveBlock> _blocks = new Dictionary<BlockPosition, ILiveBlock>();
		private SystemManager _systems;
		private BlockPosition _mainframePosition;

		private void Awake() {
			Body = gameObject.AddComponent<Rigidbody>();
			Body.angularDrag = RigidbodyAngularDrag;
			_systems = new SystemManager(this);
		}

		private void OnDestroy() {
			Destroy(Body);
		}



		/// <summary>
		/// Loads the structure created from the given buffer into a new GameObject.
		/// Also creates all required components for the GameObject.
		/// No validation is done, the EditableStructure should be used for that.
		/// </summary>
		[CanBeNull]
		public static CompleteStructure Create(BitBuffer buffer, byte id) {
			CompleteStructure structure = new GameObject("CompleteStructure#" + id).AddComponent<CompleteStructure>();
			structure.Id = id;
			structure.Deserialize(buffer);
			structure.MaxHealth = structure.Health;
			structure._systems.Finished();
			structure.ApplyMass(false);
			return structure;
		}

		private void Deserialize(BitBuffer buffer) {
			while (buffer.TotalBitsLeft >= RealPlacedBlock.SerializedBitsSize) {
				ushort type = (ushort)buffer.ReadBits(BlockFactory.BlockTypeSerializedBitsSize);
				BlockPosition position = BlockPosition.Deserialize(buffer);

				BlockInfo info = BlockFactory.GetInfo(BlockFactory.GetType(type));
				if (info.Type == BlockType.Mainframe) {
					_mainframePosition = position;
				}

				byte rotation = Rotation.Deserialize(buffer);
				RealLiveBlock block;
				if (info is SingleBlockInfo single) {
					block = BlockFactory.MakeSingleLive(transform, single, rotation, position);
				} else {
					block = BlockFactory.MakeMultiLive(transform, (MultiBlockInfo)info, rotation,
						position, out LiveMultiBlockPart[] parts);
					foreach (LiveMultiBlockPart part in parts) {
						_blocks.Add(part.Position, part);
					}
				}

				Health += info.Health;
				Mass += info.Mass;
				_blocks.Add(position, block);
				if (SystemFactory.Create(this, block, out BotSystem system)) {
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
			_systems.MoveRotate(MovementInput, timestepMultiplier);
		}

		private void FixedUpdate() {
			_systems.Tick();
			Body.drag = Body.velocity.sqrMagnitude * RigidbodyDragMultiplier + RigidbodyDragOffset;
		}



		/// <summary>
		/// Called whenever the client receives the information that damage was dealt server-side.
		/// </summary>
		public void DamagedClient(BitBuffer buffer) {
			int count = buffer.TotalBitsLeft / (BlockPosition.SerializedBitsSize + 32);
			KeyValuePair<RealLiveBlock, uint>[] damages = new KeyValuePair<RealLiveBlock, uint>[count];
			for (int i = 0; i < count; i++) {
				RealLiveBlock block = (RealLiveBlock)_blocks[BlockPosition.Deserialize(buffer)];
				uint damage = buffer.ReadUInt();
				block.Damage(damage);
				damages[i] = new KeyValuePair<RealLiveBlock, uint>(block, damage);
			}
			DamageApply(damages);
		}

		/// <summary>
		/// Called by the server whenever one or more RealLiveBlock instances are damaged.
		/// </summary>
		public void DamagedServer(KeyValuePair<RealLiveBlock, uint>[] damages) {
			DamageApply(damages);
			NetworkServer.SendTcpToClients(TcpPacketType.Server_Structure_Damage, buffer => {
				buffer.Write(Id);
				foreach (KeyValuePair<RealLiveBlock, uint> damage in damages) {
					damage.Key.Position.Serialize(buffer);
					buffer.Write(damage.Value);
				}
			});
		}

		private void DamageApply(KeyValuePair<RealLiveBlock, uint>[] damages) {
			int status = 0;
			foreach (KeyValuePair<RealLiveBlock, uint> damage in damages) {
				Health -= damage.Value;
				if (Health * 100 < MaxHealth * MinHealthPercentage) {
					status = 1;
					break;
				}

				RealLiveBlock block = damage.Key;
				if (block.Health == 0) {
					if (block.Info.Type == BlockType.Mainframe) {
						status = 1;
						break;
					} else {
						RemoveBlock(block);
						status = 2;
					}
				}
			}

			if (status == 1) {
				Destroy(gameObject);
			} else if (status == 2) {
				RemoveNotConnectedBlocks();
				ApplyMass(true);
			}
		}

		private void RemoveBlock(RealLiveBlock block) {
			Mass -= block.Info.Mass;
			Destroy(block.gameObject);

			Assert.IsTrue(_blocks.Remove(block.Position), "The block is not present.");
			if (block is LiveMultiBlockParent parent) {
				foreach (LiveMultiBlockPart part in parent.Parts) {
					Assert.IsTrue(_blocks.Remove(part.Position), "A part of the multi block is not present.");
				}
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
		/// Applies the state given as the parameter. Should only be called by a client-only instance.
		/// </summary>
		public void UpdateWholeState(BotState state) {
			MovementInput = state.MovementInput;
			_systems.TrackedPosition = state.TrackedPosition;
			transform.position = state.Position;
			transform.rotation = state.Rotation;
			Body.velocity = state.Velocity;
			Body.angularVelocity = state.AngularVelocity;
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
			BotState.SerializeState(buffer, Id, MovementInput, _systems.TrackedPosition, transform, Body);
		}



		/// <summary>
		/// If a system is present at a position return it, otherwise return null.
		/// </summary>
		public BotSystem TryGetSystem(BlockPosition position) {
			return _systems.TryGet(position);
		}

		/// <summary>
		/// Attempts to fire any weapon and informs the client if it was successful.
		/// </summary>
		public void ServerTryWeaponFiring() {
			_systems.ServerTryWeaponFiring();
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
			Body.mass = (float)Mass / 1000;
		}
	}
}
