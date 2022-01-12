using DG.Tweening;
using SimpleMan.Extensions;
using Sperlich.PrefabManager;
using TMPro;
using UnityEngine;

public class StreakBubble : MonoBehaviour {

	public float streakExpire;
	public const int maxPitch = 10;
	private static int currentStreak;
	private float time;
	[SerializeField]
	private GameObject bubble;
	[SerializeField]
	private TMP_Text text;
	[SerializeField]
	private CanvasGroup group;
	private static StreakBubble _instance;
	public static StreakBubble Instance {
		get {
			if (_instance == null) {
				_instance = PrefabManager.Instantiate<StreakBubble>(PrefabTypes.StreakBubble);
			}
			return _instance;
		}
	}
	public static int Streak => currentStreak <= 0 ? 1 : currentStreak;

	private void Update() {
		if(Game.IsGamePlaying && Game.GamePaused == false && currentStreak > 0) {
			time += Time.deltaTime;
			//group.alpha = time.Remap(0, streakExpire, 1f, 0.5f);
			bubble.transform.localScale = Vector3.one * time.Remap(0, streakExpire, 1f, 0.5f);
			if(time > streakExpire) {
				time = 0;
				currentStreak = 0;
				Instance.bubble.Stretch(1f, 0f, 0.5f);
				Instance.Delay(0.25f, () => {
					Instance.bubble.Hide();
				});
			}
		}
	}

	public static void DisplayBubble(Vector3 position, int score) {
		currentStreak++;
		if(currentStreak > 1) {
			Instance.bubble.Show();
			Instance.time = 0;

			Instance.bubble.transform.position = position;
			Instance.text.SetText(currentStreak + "");

			Instance.bubble.transform.localScale = Vector3.zero;
			Instance.bubble.transform.DOScale(0, 0.1f).OnComplete(() => {
				Instance.bubble.Stretch(1f, 1.5f, 0.15f);
			});
			if(currentStreak < maxPitch) {
				AudioPlayer.Play("StreakPitch", AudioType.UI, Mathf.Min(currentStreak, maxPitch) / 4f, 2f);
			} else {
				AudioPlayer.Play("StreakComplete", AudioType.UI, 0.8f, 3f);
				Instance.Delay(0.15f, () => AudioPlayer.Play("StreakComplete", AudioType.UI, 1f, 3f));
				Instance.Delay(0.3f, () => AudioPlayer.Play("StreakComplete", AudioType.UI, 1.2f, 3f));
				Instance.Delay(0.5f, () => AudioPlayer.Play("StreakComplete", AudioType.UI, 1.5f, 3f));
			}
		}
	}

	public static void Interrupt() {
		Instance.StopAllCoroutines();
		Instance.bubble.Hide();
		currentStreak = 0;
		Instance.time = 0;
	}
}
