using System.Collections.Generic;
using Assets.Scripts.Blocks;
using NUnit.Framework;
using UnityEngine;

namespace Assets.Scripts.Systems {
	public class SystemStorage {
		private readonly Dictionary<BlockPosition, IBotSystem> _systems = new Dictionary<BlockPosition, IBotSystem>();
		private readonly HashSet<PropulsionSystem> _propulsions = new HashSet<PropulsionSystem>();
		private readonly HashSet<WeaponSystem> _weapons = new HashSet<WeaponSystem>();
		private ActiveSystem _active;



		/// <summary>
		/// Add a new system to the storage.
		/// </summary>
		public void Add(BlockPosition position, IBotSystem system) {
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

			ActiveSystem active = system as ActiveSystem;
			Assert.IsNull(_active, "Active system can only be set once.");
			_active = active;
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
			IBotSystem system;
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
		/// Executes the propulsion systems.
		/// </summary>
		public void MoveRotate(Rigidbody bot, float x, float y, float z) {
			foreach (PropulsionSystem system in _propulsions) {
				system.MoveRotate(bot, x, y, z);
			}
		}

		/// <summary>
		/// Executes the weapon systems.
		/// </summary>
		public void Fire(Rigidbody bot, Vector3 target) {
			foreach (WeaponSystem system in _weapons) {
				system.Fire(bot, target);
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
