using Sperlich.Types;
using UnityEngine;

[CreateAssetMenu(fileName = "TankPart", menuName = "Tanks/TankPart", order = 1)]
public class TankPartAsset : ScriptableObject {

	public enum TankPartType { Body, Head }
	public int id;
	public string partName;
	public string partDescription;
	public string unlockHint;
	public TankPartType type;
	public GameObject prefab;
	public Sprite icon;

}
