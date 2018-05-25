using System.Collections.Generic;
using Assets.Scripts.Blocks;
using Assets.Scripts.Utilities;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Systems {
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

		private readonly IDictionary<BlockPosition, BotSystem> _systems = new Dictionary<BlockPosition, BotSystem>();
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

			PropulsionSystem propulsion = system as PropulsionSystem;
			if (propulsion != null) {
				_propulsions.Add(propulsion);
				return;
			}

			WeaponSystem weapon = system as WeaponSystem;
			if (weapon != null) {
				_weapons.Add(weapon);
				return;
			}

			Assert.IsNull(_active, "The active system can only be set once.");
			_active = system as ActiveSystem;
		}

		/// <summary>
		/// Make sure that no excess memory is allocated.
		/// </summary>
		public void Finished() {
			_propulsions.TrimExcess();
			_weapons.TrimExcess();
		}

		/// <summary>
		/// If a system is present at a position, remove it. Returns whether a system was removed.
		/// </summary>
		public bool TryRemove(BlockPosition position) {
			BotSystem system;
			if (!_systems.TryGetValue(position, out system)) {
				return false;
			}
			_systems.Remove(position);

			PropulsionSystem propulsion = system as PropulsionSystem;
			if (propulsion != null) {
				_propulsions.Remove(propulsion);
				return true;
			}

			WeaponSystem weapon = system as WeaponSystem;
			if (weapon != null) {
				_weapons.Remove(weapon);
				return true;
			}

			_active = null;
			return true;
		}



		/// <summary>
		/// Informs the instance that a fixed amount of time has passed:
		/// energy regeneration and accuracy restoration should be applied.
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
		}

		/// <summary>
		/// Executes the propulsion systems.
		/// </summary>
		public void MoveRotate(Rigidbody bot, float x, float y, float z) {
			foreach (PropulsionSystem system in _propulsions) {
				system.MoveRotate(bot, x, y, z);
			}
		}

		/// <summary>
		/// Rotates the weapons.
		/// </summary>
		public void TrackTarget(Vector3 target) {
			foreach (WeaponSystem system in _weapons) {
				system.TrackTarget(target);
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
				if (!system.IsOnCooldown() && system.Constants.Energy <= _energy && system.TryFireWeapon(bot, _realInaccuracy)) {
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

		/// <summary>
		/// Executes the active system.
		/// </summary>
		public void UseActive(Rigidbody bot) {
			if (_active != null) {
				_active.Activate(bot);
			}
		}
	}
}
