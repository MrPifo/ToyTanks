using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using SimpleMan.Extensions;

namespace ToyTanks.LevelEditor.UI {
	public class SelectItem : MonoBehaviour {

		[SerializeField]
		private Button button;
		[SerializeField]
		private Image previewImg;
		[SerializeField]
		private Image selectionFrame;
		[SerializeField]
		private TMP_Text text;
		public bool isSelected;
		public bool animateSelect;
		public int value;

		public virtual void Select() {
			isSelected = true;
			selectionFrame.gameObject.SetActive(true);
			if(animateSelect) {
				selectionFrame.rectTransform.localScale = Vector3.zero;
				selectionFrame.rectTransform.DOScale(1f, 0.15f).SetEase(Ease.OutCubic);
			}
		}

		public virtual void Deselect() {
			selectionFrame.gameObject.SetActive(false);
			isSelected = false;
		}

		public void SetSprite(Sprite img) {
			previewImg.sprite = img;
		}

		public void SetText(string text) {
			this.text.SetText(text);
		}

		public void SetOnClick(UnityAction call) {
			button.onClick.RemoveAllListeners();
			button.onClick.AddListener(call);
		}
	}
}
