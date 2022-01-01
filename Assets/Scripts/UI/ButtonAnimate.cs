using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ToyTanks.UI {
	[RequireComponent(typeof(Button))]
	public class ButtonAnimate : MonoBehaviour {

		[Range(0f, 2f)]
		public float scale = 1.1f;
		[Range(0.01f, 1f)]
		public float speed = 0.01f;
		public bool disableInteraction;
		public bool isMouseDown;
		public Button.ButtonClickedEvent onClick;
		EventTrigger trigger;
		float originalScale;

		void Awake() {
			originalScale = transform.localScale.x * transform.localScale.y * transform.localScale.z;

			trigger = gameObject.AddComponent<EventTrigger>();
			EventTrigger.Entry mEnter = new EventTrigger.Entry();
			mEnter.eventID = EventTriggerType.PointerEnter;
			mEnter.callback.AddListener((eventData) => { MouseEnter(); });
			trigger.triggers.Add(mEnter);

			EventTrigger.Entry mExit = new EventTrigger.Entry();
			mExit.eventID = EventTriggerType.PointerExit;
			mExit.callback.AddListener((eventData) => { MouseExit(); });
			trigger.triggers.Add(mExit);

			EventTrigger.Entry mClick = new EventTrigger.Entry();
			mClick.eventID = EventTriggerType.PointerClick;
			mClick.callback.AddListener((eventData) => { MouseClick(); });
			trigger.triggers.Add(mClick);
		}

		public virtual void MouseEnter() {
			if(disableInteraction == false) {
				transform.DOScale(scale, speed);
			}
		}

		public virtual void MouseExit() {
			if(disableInteraction == false) {
				transform.DOScale(originalScale, speed);
			}
		}

		public virtual void MouseClick() {
			onClick.Invoke();
		}
	}
}