using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour {

	public GameObject banner;
	public TMP_Text level;
	public TMP_Text lives;
	public TMP_Text singleMessage;
	public Slider progressBar;
	public float value;
	public UnityEvent onBegin;

	void Awake() {
		banner.SetActive(false);
	}

	public void InvokeLoadEnter() {
		onBegin.Invoke();
		onBegin.RemoveAllListeners();
	}

	public void SetProgress(float value) {
		progressBar.value = value;
	}

	public void SetInfo(string level, string lives) {
		this.level.gameObject.SetActive(true);
		this.lives.gameObject.SetActive(true);
		singleMessage.gameObject.SetActive(false);
		this.level.SetText(level);
		this.lives.SetText(lives);
	}

	public void SetSingleMessage(string message) {
		level.gameObject.SetActive(false);
		lives.gameObject.SetActive(false);
		singleMessage.gameObject.SetActive(true);
		singleMessage.SetText(message);
	}
}
