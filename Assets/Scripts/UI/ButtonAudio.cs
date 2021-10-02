using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

		public string hoverAudio = "Switch2";
		public string clickAudio = "";
		[Range(0f, 2f)]
		public float pitch = 1f;
		[Range(0, 0.5f)]
		public float randomPitchTreshold = 0.1f;

		public void OnPointerClick(PointerEventData eventData) {
			AudioPlayer.Play(clickAudio, pitch - randomPitchTreshold, pitch + randomPitchTreshold);
		}

		public void OnPointerEnter(PointerEventData eventData) {
			AudioPlayer.Play(hoverAudio, pitch - randomPitchTreshold, pitch + randomPitchTreshold);
		}

		public void OnPointerExit(PointerEventData eventData) {
			
		}
	}
}
