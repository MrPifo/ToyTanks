using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

		public enum ButtonAudios { None, ButtonHover1, ButtonHover2, ButtonHover3, ButtonHover4, ButtonClick1, ButtonClick2 }

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

		public void OnPointerClick(PointerEventData eventData) {
			AudioPlayer.Play(clickAudio.ToString(), AudioType.UI, clickPitch - randomClickPitch, clickPitch + randomClickPitch);
		}

		public void OnPointerEnter(PointerEventData eventData) {
			AudioPlayer.Play(hoverAudio.ToString(), AudioType.UI, hoverPitch - randomHoverPitch, hoverPitch + randomHoverPitch);
		}

		public void OnPointerExit(PointerEventData eventData) {
			
		}
	}
}
