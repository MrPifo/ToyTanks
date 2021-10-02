using CarterGames.Assets.AudioManager;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class ButtonAnimate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

		Button button;
		MMFeedbacks enterFeedback;
		MMFeedbacks exitFeedback;
		public Button.ButtonClickedEvent onClick;

		void Awake() {
			button = GetComponent<Button>();
			enterFeedback = transform.GetChild(1).GetComponent<MMFeedbacks>();
			exitFeedback = transform.GetChild(2).GetComponent<MMFeedbacks>();
			button.onClick.RemoveAllListeners();
			button.onClick = onClick;
		}

		public void OnPointerEnter(PointerEventData eventData) {
			enterFeedback.PlayFeedbacks();
		}

		public void OnPointerExit(PointerEventData eventData) {
			exitFeedback.PlayFeedbacks();
		}
	}
}