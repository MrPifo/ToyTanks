using System;
using ToyTanks.LevelEditor;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "ExtraBlock", menuName = "Themes/ExtraBlockAsset", order = 2)]
public class ExtraBlockAsset : ScriptableObject {
	public ExtraBlocks block;
	public GameObject prefab;
	public Sprite preview;
	public Vector3 Size => prefab.GetComponent<LevelExtraBlock>().Size;
}