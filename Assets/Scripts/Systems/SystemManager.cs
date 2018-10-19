using System.Collections.Generic;
using Systems.Active;
using Systems.Propulsion;
using Systems.Weapon;
using Blocks;
using Structures;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Systems {
	/// <summary>
	/// A class which manages a structure's systems.
	/// </summary>
	public class SystemManager {
		public const float FiringPause = 0.075f; //in seconds
		public const float EnergyFillRate = 0.25f;
		public const float MinFiringInaccuracy = 0.25f;
		public const float MaxInaccuracy = 10;
		public const float FiringInaccuracyFading = 4; //in seconds
		public const float MovingInaccuracyScale = 0.5f;

		public WeaponSystem.Type WeaponType { get; private set; } = WeaponSystem.Type.None;
		public Vector3 TrackedPosition { get; set; } = Vector3.zero;
		private readonly IDictionary<BlockPosition, BotSystem> _systems = new Dictionary<BlockPosition, BotSystem>();
		private readonly HashSet<PropulsionSystem> _propulsions = new HashSet<PropulsionSystem>();
		private readonly CircularList<WeaponSystem> _weapons = new CircularList<WeaponSystem>();
		private readonly CompleteStructure _structure;
		private ActiveSystem _active;
		private float _firingPauseEnds;
		private float _energy = 1;
		private float _firingInaccuracy = MinFiringInaccuracy;
		private float _realInaccuracy;

		public SystemManager(CompleteStructure structure) {
			_structure = structure;
		}



		/// <summary>
		/// Add a new system to the storage.
		/// </summary>
		public void Add(BlockPosition position, BotSystem system) {
			_systems.Add(position, system);
			if (system is PropulsionSystem propulsion) {
				_propulsions.Add(propulsion);
			} else if (system is WeaponSystem weapon) {
				if (WeaponType == WeaponSystem.Type.None) {
					WeaponType = weapon.Constants.Type;
				}
				_weapons.Add(weapon);
			} else {
				Assert.IsNull(_active, "The active system can only be set once.");
				_active = system as ActiveSystem;
			}
		}

		/// <summary>
		/// Make sure that no excess memory is allocated.
		/// Should only be called after all systems have been added.
		/// </summary>
		public void Finished() {
			_propulsions.TrimExcess();
			_weapons.TrimExcess();
		}



		/// <summary>
		/// If a system is present at a position return it, otherwise return null.
		/// </summary>
		// ReSharper disable once AnnotateCanBeNullTypeMember
		public BotSystem TryGet(BlockPosition position) {
			return _systems.TryGetValue(position, out BotSystem system) ? system : null;
		}

		/// <summary>
		/// If a system is present at a position, remove it. Returns whether a system was removed.
		/// </summary>
		public bool TryRemove(BlockPosition position) {
			if (!_systems.TryGetValue(position, out BotSystem system)) {
				return false;
			}

			_systems.Remove(position);
			if (system is PropulsionSystem propulsion) {
				_propulsions.Remove(propulsion);
			} else if (system is WeaponSystem weapon) {
				_weapons.Remove(weapon);
			} else {
				_active = null;
			}
			return true;
		}



		/// <summary>
		/// Informs the instance that a fixed amount of time has passed:
		/// energy regeneration, accuracy restoration and weapon rotation should be applied.
		/// </summary>
		public void Tick() {
			_energy += EnergyFillRate * Time.fixedDeltaTime;
			if (_energy > 1) {
				_energy = 1;
			}

			_firingInaccuracy -= FiringInaccuracyFading * Time.fixedDeltaTime;
			if (_firingInaccuracy < MinFiringInaccuracy) {
				_firingInaccuracy = MinFiringInaccuracy;
			}

			_realInaccuracy = _firingInaccuracy + _structure.Body.velocity.sqrMagnitude * MovingInaccuracyScale;
			if (_realInaccuracy > MaxInaccuracy) {
				_realInaccuracy = MaxInaccuracy;
			}

			foreach (WeaponSystem system in _weapons) {
				system.TrackPosition(TrackedPosition);
			}
		}



		/// <summary>
		/// Executes the propulsion systems.
		/// </summary>
		public void MoveRotate(Vector3 direction, float timestepMultiplier) {
			foreach (PropulsionSystem system in _propulsions) {
				system.MoveRotate(direction, timestepMultiplier);
			}
		}



		/// <summary>
		/// This method informs the server that the client currently wishes to fire weapons.
		/// The server determines whether a weapon can be fired towards the specified position in its current state.
		/// If it can, the weapon is fired and the client gets notified with all necessary information.
		/// </summary>
		public void ServerTryWeaponFiring() {
			if (_firingPauseEnds > Time.time) {
				return;
			}

			foreach (WeaponSystem system in _weapons) {
				if (system.IsOnCooldown() || system.Constants.Energy > _energy
					|| !system.ServerTryExecuteWeaponFiring(_realInaccuracy)) {
					continue;
				}

				system.UpdateCooldown();
				_firingPauseEnds = Time.time + FiringPause;
				_energy -= system.Constants.Energy;

				_firingInaccuracy += system.Constants.Inaccuracy;
				if (_firingInaccuracy > MaxInaccuracy) {
					_firingInaccuracy = MaxInaccuracy;
				}
				break;
			}
		}
	}
}
