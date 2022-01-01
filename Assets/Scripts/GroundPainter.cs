using Sperlich.Debug.Draw;
using UnityEngine;
using PixelTracery;

public class GroundPainter : MonoBehaviour {

	public int size;
	public float fadeSpeed;
    public Texture2D paintTexture;
	public Sprite trackTexture;
	public Material currentGroundMaterial;

	private void Awake() {
		paintTexture = new Texture2D(1024, 1024, TextureFormat.RGB24, false);
	}

	private void Update() {
		//var mouseLocation = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		//Ray camRay = Camera.main.ScreenPointToRay(mouseLocation);
		//Plane plane = new Plane(Vector3.up, -1f);
		//plane.Raycast(camRay, out float enter);
		//Vector3 hitPoint = camRay.GetPoint(enter);
		//hitPoint = new Vector3(hitPoint.x, hitPoint.z, 0);
		//Draw.Sphere(hitPoint, 0.5f, Color.white);
		//Paint(hitPoint);
	}

	public void Paint(Vector2 pos, Color color) {
		Vector2Int uvPos = new Vector2Int(0, 0);
		uvPos.x = (int)(pos.x * transform.localScale.x * 2 + 512);
		uvPos.y = (int)(pos.y * transform.localScale.z * 2 + 512);

		for(int x = -size; x < size; x++) {
			for(int y = -size; y < size; y++) {
				paintTexture.SetPixel(-(uvPos.x + x), -(uvPos.y + y), color);
			}
		}
		paintTexture.Apply();
		currentGroundMaterial.SetTexture("_PaintMap", paintTexture);
	}

	public void PaintTrack(Vector2 pos, Vector3 forward) {
		Vector2Int uvPos = PointToTextureUV(pos);

		for(int x = -size * 2; x < size * 2; x++) {
			for(int z = -size; z < size; z++) {
				Vector3 pxPos = new Vector3(uvPos.x, 0, uvPos.y);
				pxPos.x += x;
				pxPos.z += z;
				//paintTexture.SetPixel((int)pxPos.x, (int)pxPos.z, Color.gray);
			}
		}
		/*for(int x = trackTexture.width; x < trackTexture.width; x++) {
			for(int z = trackTexture.height; z < trackTexture.height; z++) {
				paintTexture.SetPixel(x, z, trackTexture.GetPixel(x, z));
			}
		}*/
		paintTexture.PixSprite(uvPos.x, uvPos.y, trackTexture);
		Debug.Log("AT: " + uvPos);
		paintTexture.Apply();
		currentGroundMaterial.SetTexture("_PaintMap", paintTexture);
	}

	public Vector2Int PointToTextureUV(Vector2 pos) {
		return new Vector2Int(-Mathf.RoundToInt(pos.x * transform.localScale.x * 2 + paintTexture.width / 2f), -Mathf.RoundToInt(pos.y * transform.localScale.z * 2 + paintTexture.height / 2f));
	}
}
