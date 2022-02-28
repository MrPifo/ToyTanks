#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

[ExecuteInEditMode]
public class PreviewGenerator : MonoBehaviour {

	public Camera camera;
	public Transform tankModelsContainer;
	public Transform blockModelsContainer;
	public Texture2D icon;

	[Header("Tank Settings")]
	public Vector3 blocksPreviewDirection;
	public Vector3 blocksOffset;

	[Header("Tank Settings")]
	public Vector3 tanksPreviewDirection;
	public Vector3 tanksOffset;

	private static string BasePath;

    public void GenereateTankIcons() {
		BasePath = Application.dataPath + "/Addressables/";
		RuntimePreviewGenerator.PreviewRenderCamera = camera;
		RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
		RuntimePreviewGenerator.OffsetPosition = tanksOffset;
		RuntimePreviewGenerator.PreviewDirection = tanksPreviewDirection;
		RuntimePreviewGenerator.OrthographicMode = true;
		RuntimePreviewGenerator.MarkTextureNonReadable = false;
		RuntimePreviewGenerator.RenderSupersampling = 2;

		foreach(Transform model in tankModelsContainer) {
			RuntimePreviewGenerator.OffsetPosition = tanksOffset;
			RuntimePreviewGenerator.OffsetPosition += new Vector3(0, model.transform.position.y, 0);
			Texture2D tex = RuntimePreviewGenerator.GenerateModelPreview(model, 512, 512, false, true);
			SaveTexture(tex, model.name + "_icon", "Tanks/Preview/");
		}
		AssetDatabase.Refresh();
    }

	public void GenerateBlockIcons() {
		BasePath = Application.dataPath + "/Addressables/";
		RuntimePreviewGenerator.PreviewRenderCamera = camera;
		RuntimePreviewGenerator.BackgroundColor = new Color(0, 0, 0, 0);
		RuntimePreviewGenerator.OffsetPosition = blocksOffset;
		RuntimePreviewGenerator.PreviewDirection = blocksPreviewDirection;
		RuntimePreviewGenerator.OrthographicMode = true;
		RuntimePreviewGenerator.MarkTextureNonReadable = false;
		RuntimePreviewGenerator.RenderSupersampling = 2;

		foreach(Transform theme in blockModelsContainer) {
			foreach(Transform model in theme) {
				RuntimePreviewGenerator.OffsetPosition = tanksOffset;
				RuntimePreviewGenerator.OffsetPosition += new Vector3(0, model.transform.position.y, 0);
				Texture2D tex = RuntimePreviewGenerator.GenerateModelPreview(model, 512, 512, false, true);
				if(theme.name == "Extra") {
					SaveTexture(tex, model.name + "_icon", "Themes/Extras/Previews/");
				} else {
					SaveTexture(tex, model.name + "_icon", "Themes/" + theme.name + "/Previews/");
				}
			}
		}
		AssetDatabase.Refresh();
	}

	public void SaveTexture(Texture2D texture, string name, string relativePath) {
		texture.alphaIsTransparency = true;
		texture.requestedMipmapLevel = 0;
		byte[] bytes = ConvertBlackPixelsToTransparent(texture).EncodeToPNG();
		var folderPath = BasePath + relativePath;
		var filePath = folderPath + name + ".png";
		if(Directory.Exists(folderPath) == false) {
			Directory.CreateDirectory(folderPath);
		}
		File.WriteAllBytes(filePath, bytes);
		Debug.Log("File saved to: " + filePath);
	}

	public Texture2D ConvertBlackPixelsToTransparent(Texture2D texture) {
		for(int x = 0; x < texture.width; x++) {
			for(int y = 0; y < texture.height; y++) {
				Color color = texture.GetPixel(x, y);
				if(color == Color.black) {
					texture.SetPixel(x, y, new Color(0, 0, 0, 0));
				}
			}
		}
		return texture;
	}

	[CustomEditor(typeof(PreviewGenerator))]
	class PreviewGeneratorEditor : Editor {

		public override void OnInspectorGUI() {
			DrawDefaultInspector();
			var builder = (PreviewGenerator)target;

			if(GUILayout.Button("Render Tank Icons")) {
				builder.GenereateTankIcons();
			}
			GUILayout.Space(10);
			if(GUILayout.Button("Render Block Icons")) {
				builder.GenerateBlockIcons();
			}
		}
	}
}
#endif