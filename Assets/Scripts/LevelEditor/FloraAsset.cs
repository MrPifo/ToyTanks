using System;
using ToyTanks.LevelEditor;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Vegetation", menuName = "Themes/VegetationAsset", order = 2)]
public class FloraAsset : ScriptableObject {

	public FloraBlocks block;
	public GameObject prefab;
	public Sprite preview;
	public Vector3 Size => prefab.GetComponent<LevelFloraBlock>().Size;
}