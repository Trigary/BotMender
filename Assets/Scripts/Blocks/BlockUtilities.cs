using UnityEngine;

namespace Blocks {
	public static class BlockUtilities {
		public static void SetColor(GameObject block, Color color, bool enableTransparency) {
			Material material = block.GetComponent<Renderer>().material;
			if (enableTransparency) {
				material.SetInt("_Mode", 3);
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				material.SetInt("_ZWrite", 0);
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHABLEND_ON");
				material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
				material.renderQueue = 3000;
			}
			material.color = color;
		}

		public static void RemoveCollider(GameObject block, bool immediate) {
			Component collider = block.GetComponent<Collider>();
			if (immediate) {
				Object.DestroyImmediate(collider);
			} else {
				Object.Destroy(collider);
			}
		}
	}
}
