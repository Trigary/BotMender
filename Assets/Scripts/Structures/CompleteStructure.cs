using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Live;
using Assets.Scripts.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Structures {
	/// <summary>
	/// A structure which is no longer editable, but is damagable and destructable.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
	public class CompleteStructure : MonoBehaviour {
		public uint MaxHealth { get; private set; }
		public uint Health { get; private set; }
		public uint Mass { get; private set; }
		private readonly Dictionary<BlockPosition, ILiveBlock> _blocks = new Dictionary<BlockPosition, ILiveBlock>();
		private readonly SystemStorage _systems = new SystemStorage();
		private Rigidbody _body;

		public void Start() {
			_body = GetComponent<Rigidbody>();
		}



		/// <summary>
		/// Loads this structure using the given serialized blocks.
		/// The data is treated as valid. Can only be called once.
		/// </summary>
		// ReSharper disable once ParameterTypeCanBeEnumerable.Global
		public void Load(ulong[] serialized) { //TODO static method instead which also creates the GameObject
			Assert.IsTrue(MaxHealth == 0, "The load method can only be called once.");

			foreach (ulong value in serialized) {
				byte[] bytes = BitConverter.GetBytes(value);
				BlockType type = BlockFactory.BlockTypes[BitConverter.ToUInt32(bytes, 4)];

				BlockPosition position;
				BlockPosition.FromComponents(bytes[0], bytes[1], bytes[2], out position);

				BlockInfo info = BlockFactory.GetInfo(type);
				SingleBlockInfo single = info as SingleBlockInfo;
				RealLiveBlock block;
				if (single != null) {
					block = BlockFactory.MakeSingleLive(transform, single, bytes[3], position);
				} else {
					block = CreateMulti(position, (MultiBlockInfo)info, bytes[3]);
				}

				Health += info.Health;
				Mass += info.Mass;
				_blocks.Add(position, block);

				IBotSystem system;
				if (SystemFactory.Create(block, out system)) {
					_systems.Add(position, system);
				}
			}

			MaxHealth = Health;
			_systems.Finished();
			ApplyCenterOfMass();
		}

		private LiveMultiBlockParent CreateMulti(BlockPosition position, MultiBlockInfo info, byte rotation) {
			KeyValuePair<BlockPosition, BlockSides>[] positions;
			info.GetRotatedPositions(position, rotation, out positions);

			BlockSides parentSides = BlockSides.None;
			LiveMultiBlockPart[] parts = new LiveMultiBlockPart[positions.Length - 1];
			int partsIndex = 0;

			foreach (KeyValuePair<BlockPosition, BlockSides> pair in positions) {
				if (pair.Key.Equals(position)) {
					parentSides = pair.Value;
					continue;
				}

				LiveMultiBlockPart part = new LiveMultiBlockPart(pair.Value, pair.Key);
				parts[partsIndex++] = part;
				_blocks.Add(pair.Key, part);
			}

			LiveMultiBlockParent parent = BlockFactory.MakeMultiLive(transform, info, rotation, position, parentSides, parts);
			foreach (LiveMultiBlockPart part in parts) {
				part.Initialize(parent);
			}

			return parent;
		}



		/// <summary>
		/// Should only be called by a RealLiveBlock instance when it is damaged.
		/// </summary>
		public void Damaged(RealLiveBlock block, uint damage) { //TODO events, etc.
			Health -= damage;
			if (block.Health != 0) {
				return;
			}

			//TODO no blocks left / Health == 0

			Mass -= block.Info.Mass;
			RemoveBlock(block);
			_systems.TryRemove(block.Position);
			//TODO connection checks
			ApplyCenterOfMass();
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private void RemoveBlock(RealLiveBlock block) {
			Assert.IsTrue(_blocks.Remove(block.Position), "No block exists at the specified position.");
			if (block is LiveSingleBlock) {
				return;
			}

			// ReSharper disable once PossibleNullReferenceException
			foreach (LiveMultiBlockPart part in (block as LiveMultiBlockParent).Parts) {
				Assert.IsTrue(_blocks.Remove(part.Position), "A part of the multi block is not present.");
			}
		}

		private void ApplyCenterOfMass() {
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
		}



		/// <summary>
		/// Executes the propulsion systems.
		/// </summary>
		public void MoveRotate(float x, float y, float z) {
			_systems.MoveRotate(_body, x, y, z);
		}

		/// <summary>
		/// Executes the weapon systems.
		/// </summary>
		public void Fire(Vector3 target) {
			_systems.Fire(_body, target);
		}

		/// <summary>
		/// Executes the active system.
		/// </summary>
		public void UseActive() {
			_systems.UseActive(_body);
		}
	}
}
