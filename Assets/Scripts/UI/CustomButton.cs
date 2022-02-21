using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

		public enum ButtonAudios { None, ButtonHover1, ButtonHover2, ButtonHover3, ButtonHover4, ButtonClick1, ButtonClick2 }
		public enum AnimationMode { None, Scale }

		public AnimationMode animationMode = AnimationMode.None;
		public ButtonAudios hoverAudio = ButtonAudios.None;
		public ButtonAudios clickAudio = ButtonAudios.None;
		[Range(0f, 2f)]
		public float hoverPitch = 1f;
		[Range(0f, 2f)]
		public float clickPitch = 1f;
		[Range(0, 0.5f)]
		public float randomHoverPitch = 0.1f;
		[Range(0, 0.5f)]
		public float randomClickPitch = 0.1f;
		private bool onTransition;
		private bool isHovered;

		public void SetOnClick(UnityAction action) {
			GetComponent<Button>().onClick.AddListener(action);
		}

		public void OnPointerClick(PointerEventData eventData) {
			AudioPlayer.Play(clickAudio.ToString(), AudioType.UI, clickPitch - randomClickPitch, clickPitch + randomClickPitch);
		}

		public void OnPointerEnter(PointerEventData eventData) {
			isHovered = true;
			if(onTransition == false) {
				onTransition = true;
				AudioPlayer.Play(hoverAudio.ToString(), AudioType.UI, hoverPitch - randomHoverPitch, hoverPitch + randomHoverPitch);
				
				switch(animationMode) {
					case AnimationMode.Scale:
						transform.DOScale(1f, 0.1f).OnComplete(() => {
							onTransition = false;
						});
						break;
				}
			}
		}

		public void OnPointerExit(PointerEventData eventData) {
			isHovered = false;
			if(onTransition == false) {
				onTransition = true;
				switch(animationMode) {
					case AnimationMode.Scale:
						transform.DOScale(1f, 0.1f).OnComplete(() => {
							onTransition = false;
						});
						break;
				}
			}
		}

		void OnEnable() {
			transform.localScale = Vector3.one;
		}
	}
}
