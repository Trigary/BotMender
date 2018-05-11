using System;
using UnityEngine;
using Assets.Scripts.Blocks;
using Assets.Scripts.Blocks.Info;
using Assets.Scripts.Blocks.Placed;
using Assets.Scripts.Playing;
using Assets.Scripts.Structures;

namespace Assets.Scripts.Building {
	/// <summary>
	/// Allows the player to interact with the structure.
	/// </summary>
	[RequireComponent(typeof(EditableStructure))]
	public class BuildingController : MonoBehaviour {
		private Camera _camera;
		private EditableStructure _structure;
		private BlockType _blockType;
		private byte _facingVariant;

		//TODO changes selected facing, change selected new block location events?

		public void Awake() {
			_camera = Camera.main;
			_structure = GetComponent<EditableStructure>();
			_blockType = BlockType.ArmorSlope1;
		}



		public void Update() {
			Rotate(Input.GetAxisRaw("MouseScroll"));
			if (Input.GetButtonDown("Fire3")) {
				Switch();
			}

			if (Input.GetButtonDown("Fire1")) {
				Place();
			} else if (Input.GetButtonDown("Fire2")) {
				Delete();
			}
			
			//TODO remove
			if (Input.GetButtonDown("Ability")) {
				CompleteStructure complete = CompleteStructure.Create(_structure.Serialize());
				if (complete == null) {
					Debug.Log("Failed to create CompleteStructure");
				} else {
					complete.gameObject.AddComponent<HumanBotController>();
					_camera.gameObject.AddComponent<PlayingCameraController>().Structure = complete;
					Destroy(_camera.gameObject.GetComponent<BuildingCameraController>());
					Destroy(gameObject);
					Debug.Log("CompleteStructure created, EditableStructure destroyed");
				}
			}
		}



		private void Rotate(float rawInput) {
			if (rawInput > 0) {
				_facingVariant++;
			} else if (rawInput < 0) {
				_facingVariant--;
			}
		}

		private void Switch() {
			BlockType[] values = BlockFactory.BlockTypes;
			int index = Array.IndexOf(values, _blockType);
			_blockType = values[(index + 1) % values.Length];
			Debug.Log("Switched to: " + _blockType); //TODO remove
		}

		private void Place() {
			RaycastHit hit;
			if (!GetSelected(out hit)) {
				return;
			}

			BlockPosition position;
			if (!BlockPosition.FromVector(hit.point + hit.normal / 2, out position)) {
				return;
			}

			byte rotation = Rotation.GetByte(BlockSide.FromNormal(hit.normal), _facingVariant);
			BlockInfo info = BlockFactory.GetInfo(_blockType);

			SingleBlockInfo singleInfo = info as SingleBlockInfo;
			if (singleInfo != null) {
				_structure.TryAddBlock(position, singleInfo, rotation);
			} else {
				_structure.TryAddBlock(position, info as MultiBlockInfo, rotation);
			}
		}

		private void Delete() {
			RaycastHit hit;
			if (!GetSelected(out hit)) {
				return;
			}

			GameObject block = hit.transform.gameObject;
			RealPlacedBlock component = block.GetComponent<RealPlacedBlock>();
			if (component == null) {
				return;
			}

			_structure.RemoveBlock(component.Position);
			//TODO check structure validity (connections)
		}



		private bool GetSelected(out RaycastHit hit) {
			return Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit);
		}
	}
}
