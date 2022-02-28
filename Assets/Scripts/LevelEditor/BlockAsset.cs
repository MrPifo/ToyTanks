using System;
using ToyTanks.LevelEditor;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "Asset", menuName = "Themes/Asset", order = 1)]
public class BlockAsset : ScriptableObject {
	public BlockType block;
	public GameObject prefab;
	public Material material;
	public Sprite preview;
	public Vector3 Size => prefab.GetComponent<LevelBlock>().Size;
}