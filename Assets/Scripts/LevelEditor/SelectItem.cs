using UnityEngine;
using UnityEngine.UI;

namespace ToyTanks.LevelEditor {
	public abstract class SelectItem : MonoBehaviour {

		public Image previewImg;
		public Image backgroundImg;

		public virtual void Select() {
			LevelEditor.DeselectEverything();
			backgroundImg.gameObject.SetActive(true);
		}

		public virtual void Deselect() {
			backgroundImg.gameObject.SetActive(false);
		}

		public void SetSprite(Sprite img) {
			previewImg.sprite = img;
		}
	}
}
