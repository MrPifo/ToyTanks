using LeTai.Asset.TranslucentImage;
using ToyTanks.LevelEditor.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TranslucentButton : SelectItem {

    [SerializeField]
    private TranslucentImage translucentImage;

    public void SetSpriteBlending(float blendValue) {
        translucentImage.spriteBlending = blendValue;
	}

	public override void Select() {
		translucentImage.spriteBlending = 0f;
		isSelected = true;
		translucentImage.SetAllDirty();
	}

	public override void Deselect() {
		translucentImage.spriteBlending = 0.4f;
		translucentImage.SetAllDirty();
		isSelected = false;
	}
}
