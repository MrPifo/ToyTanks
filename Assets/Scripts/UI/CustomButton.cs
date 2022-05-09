using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

		public enum AnimationMode { None, Scale }

		public AnimationMode animationMode = AnimationMode.None;
		public JSAM.Sounds hoverAudio;
		public JSAM.Sounds clickAudio;
		[Range(0f, 2f)]
		public float hoverPitch = 1f;
		[Range(0f, 2f)]
		public float clickPitch = 1f;
		[Range(0, 0.5f)]
		public float randomHoverPitch = 0.1f;
		[Range(0, 0.5f)]
		public float randomClickPitch = 0.1f;
		private bool isHovered;
		private Button button;
		private Sprite defaultImg;

		private void Awake() {
			button = GetComponent<Button>();
			defaultImg = button.image.sprite;
		}

		public void SetOnClick(UnityAction action) {
			GetComponent<Button>().onClick.AddListener(action);
		}

		public void OnPointerClick(PointerEventData eventData) {
			AudioPlayer.Play(clickAudio, AudioType.UI, clickPitch - randomClickPitch, clickPitch + randomClickPitch);
		}

		public void OnPointerEnter(PointerEventData eventData) {
			Select();
		}

		public void OnPointerExit(PointerEventData eventData) {
			Deselect();
		}

		public void Select() {
			if (button.spriteState.highlightedSprite != null) {
				button.image.sprite = button.spriteState.highlightedSprite;
			}
			isHovered = true;
			AudioPlayer.Play(hoverAudio, AudioType.UI, hoverPitch - randomHoverPitch, hoverPitch + randomHoverPitch);
			switch (animationMode) {
				case AnimationMode.Scale:
					transform.DOScale(1.1f, 0.1f);
					break;
			}
		}

		public void Deselect() {
			button.image.sprite = defaultImg;
			isHovered = false;
			switch (animationMode) {
				case AnimationMode.Scale:
					transform.DOScale(1f, 0.1f);
					break;
			}
		}

		protected void OnEnable() {
			transform.localScale = Vector3.one;
		}
	}
}
