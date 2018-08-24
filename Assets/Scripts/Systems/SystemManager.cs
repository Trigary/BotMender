using System.Collections.Generic;
using Blocks;
using UnityEngine;
using UnityEngine.Assertions;
using Utilities;

namespace Systems {
	/// <summary>
	/// A class which manages a bot's systems.
	/// </summary>
	public class SystemManager {
		public const float FiringPause = 0.075f; //in seconds
		public const float EnergyFillRate = 0.25f;
		public const float MinFiringInaccuracy = 0.25f;
		public const float MaxInaccuracy = 10;
		public const float FiringInaccuracyFading = 4; //in seconds
		public const float MovingInaccuracyScale = 0.5f;

		public byte NextId => (byte)_systemIds.Count;
		public Vector3 TrackedPosition { get; set; } = Vector3.zero;
		private readonly IDictionary<BlockPosition, BotSystem> _systems = new Dictionary<BlockPosition, BotSystem>();
		private readonly List<BotSystem> _systemIds = new List<BotSystem>();
		private readonly HashSet<PropulsionSystem> _propulsions = new HashSet<PropulsionSystem>();
		private readonly CircularList<WeaponSystem> _weapons = new CircularList<WeaponSystem>();
		private ActiveSystem _active;
		private float _firingPauseEnds;
		private float _energy = 1;
		private float _firingInaccuracy = MinFiringInaccuracy;
		private float _realInaccuracy;



		/// <summary>
		/// Add a new system to the storage.
		/// </summary>
		public void Add(BlockPosition position, BotSystem system) {
			_systems.Add(position, system);
			_systemIds.Add(system);
			if (system is PropulsionSystem propulsion) {
				_propulsions.Add(propulsion);
			} else if (system is WeaponSystem weapon) {
				_weapons.Add(weapon);
			} else {
				Assert.IsNull(_active, "The active system can only be set once.");
				_active = system as ActiveSystem;
			}
		}

		/// <summary>
		/// Make sure that no excess memory is allocated.
		/// </summary>
		public void Finished() {
			_systemIds.TrimExcess();
			_propulsions.TrimExcess();
			_weapons.TrimExcess();
		}



		/// <summary>
		/// If a system with the specified ID is present return it, otherwise return null.
		/// </summary>
		// ReSharper disable once AnnotateCanBeNullTypeMember
		public BotSystem TryGet(byte id) {
			return _systemIds.Count > id ? _systemIds[id] : null;
		}

		/// <summary>
		/// If a system is present at a position, remove it. Returns whether a system was removed.
		/// </summary>
		public bool TryRemove(BlockPosition position) {
			if (!_systems.TryGetValue(position, out BotSystem system)) {
				return false;
			}

			_systems.Remove(position);
			_systemIds.RemoveAt(system.Id);
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
		public void Tick(Rigidbody bot) {
			_energy += EnergyFillRate * Time.fixedDeltaTime;
			if (_energy > 1) {
				_energy = 1;
			}

			_firingInaccuracy -= FiringInaccuracyFading * Time.fixedDeltaTime;
			if (_firingInaccuracy < MinFiringInaccuracy) {
				_firingInaccuracy = MinFiringInaccuracy;
			}

			_realInaccuracy = _firingInaccuracy + bot.velocity.sqrMagnitude * MovingInaccuracyScale;
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
		public void MoveRotate(Rigidbody bot, Vector3 direction, float timestepMultiplier) {
			foreach (PropulsionSystem system in _propulsions) {
				system.MoveRotate(bot, direction, timestepMultiplier);
			}
		}

		/// <summary>
		/// Executes the weapon systems.
		/// </summary>
		public void FireWeapons(Rigidbody bot) {
			if (_firingPauseEnds > Time.time) {
				return;
			}

			foreach (WeaponSystem system in _weapons) {
				if (system.IsOnCooldown() || !(system.Constants.Energy <= _energy) || !system.TryFireWeapon(bot, _realInaccuracy)) {
					continue;
				}

				_firingPauseEnds = Time.time + FiringPause;
				_energy -= system.Constants.Energy;

				_firingInaccuracy += system.Constants.Inaccuracy;
				if (_firingInaccuracy > MaxInaccuracy) {
					_firingInaccuracy = MaxInaccuracy;
				}
				break;
			}
		}

		/// <summary>
		/// Executes the active system.
		/// </summary>
		public void UseActive(Rigidbody bot) {
			_active?.Activate(bot);
		}
	}
}
