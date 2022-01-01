using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIScaleAnimation : MonoBehaviour {

	[Range(0f, 2f)]
	public float scale = 1.1f;
	[Range(0.01f, 1f)]
	public float speed = 0.01f;
	public bool disableInteraction;
	public bool isMouseDown;
	float originalScale;
	EventTrigger trigger;

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

	public virtual void MouseDown() {
		isMouseDown = true;
	}

	public virtual void MouseUp() {
		isMouseDown = false;
	}
}
