using UnityEngine;

[CreateAssetMenu(fileName = "LevelTheme", menuName = "Themes/Theme", order = 1)]
public class ThemeAsset : ScriptableObject {

	public WorldTheme theme;
	public Material floorMaterial;
}