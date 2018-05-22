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
		public const float MinInaccuracy = 3f;
		public const float MaxInaccuracy = 30f;
		public const float InaccuracyFading = 0.05f;

		private readonly IDictionary<BlockPosition, BotSystem> _systems = new Dictionary<BlockPosition, BotSystem>();
		private readonly HashSet<PropulsionSystem> _propulsions = new HashSet<PropulsionSystem>();
		private readonly CircularList<WeaponSystem> _weapons = new CircularList<WeaponSystem>();
		private ActiveSystem _active;
		private float _inaccuracy;
		private float _energy = 100;



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
		/// accuracy restoration and energy regeneration should be applied.
		/// </summary>
		public void Tick() {
			_inaccuracy -= InaccuracyFading;
			if (_inaccuracy < MinInaccuracy) {
				_inaccuracy = MinInaccuracy;
			}

			if (_energy < 100) {
				_energy++;
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
			foreach (WeaponSystem system in _weapons) {
				if (!system.IsOnCooldown() && system.Constants.Energy <= _energy && system.TryFireWeapon(bot, _inaccuracy)) {
					_inaccuracy += system.Constants.Inaccuracy;
					if (_inaccuracy > MaxInaccuracy) {
						_inaccuracy = MaxInaccuracy;
					}

					_energy -= system.Constants.Energy;
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
