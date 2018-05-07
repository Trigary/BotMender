using System;
using System.Collections;
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
		[Tooltip("The camera this controller should use.")]
		public Camera Camera;
		
		private EditableStructure _structure;
		private BlockType _blockType;
		private byte _facingVariant;

		//TODO changes selected facing, change selected new block location events?

		public void Start() {
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
				Debug.Log("Destroying");
				StartCoroutine(Temp());
			}
		}

		private IEnumerator Temp() {
			ulong[] data = _structure.Save();
			GameObject structure = new GameObject("Structure");
			CompleteStructure complete = structure.AddComponent<CompleteStructure>();
			structure.AddComponent<HumanBotController>().Camera = Camera;
			yield return new WaitForFixedUpdate();
			
			complete.Load(data);
			Destroy(Camera.gameObject.GetComponent<CameraController>());
			Destroy(gameObject);
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

		private void Delete() { //TODO check structure validity (connections)
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
		}



		private bool GetSelected(out RaycastHit hit) {
			return Physics.Raycast(Camera.transform.position, Camera.transform.forward, out hit);
		}
	}
}
