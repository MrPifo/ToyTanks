using DG.Tweening;
using MoreMountains.Feedbacks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossUI : MonoBehaviour {
    
	public Dictionary<BossAI, int> bosses = new Dictionary<BossAI, int>();
	public Slider bossBar;
	public MMFeedbacks bossUIInitFeedback;
	public MMFeedbacks bossUIHitFeedback;

	private static BossUI instance;
	public static BossUI Instance {
		get {
			if(instance == null) {
				instance = Instantiate(Resources.Load<GameObject>("BossCanvas")).GetComponent<BossUI>();
				instance.name = "BossUI";
				Logger.Log(Channel.UI, "Created Singleton of " + typeof(BossUI).Name);
			}
			return instance;
		}
	}

	protected void Awake() {
		if(instance == null) {
			instance = this as BossUI;
		} else {
			Destroy(gameObject);
		}
		Instance.bossBar.Hide();
	}

	public static void RegisterBoss(BossAI boss) {
		if(Instance.bosses.ContainsKey(boss)) {
			RemoveBoss(boss);
		}
		Instance.bossBar.Show();

		Instance.bosses.Add(boss, boss.MaxHealthPoints);
		Instance.bossBar.maxValue += boss.MaxHealthPoints;
		Instance.bossBar.value += boss.MaxHealthPoints;
	}

	public static void InitAnimateBossBar() {
		if(Instance.bosses.Count > 0) {
			Instance.bossBar.value = 0;
			Instance.bossBar.DOValue(Instance.bossBar.maxValue, 3f).SetEase(Ease.Linear);
			Instance.bossUIInitFeedback.PlayFeedbacks();
		}
	}

	public static void RemoveBoss(BossAI boss) {
		if(Instance.bosses.ContainsKey(boss)) {
			Instance.bosses.Remove(boss);
			Instance.bossBar.maxValue -= boss.MaxHealthPoints;

			if(Instance.bosses.Count == 0) {
				ResetBossBar();
			}
		}
	}

	public static void ResetBossBar() {
		Instance.bosses = new Dictionary<BossAI, int>();
		Instance.bossBar.value = 0;
		Instance.bossBar.maxValue = 0;
		Instance.bossBar.Hide();
	}

	public static void BossTakeDamage(BossAI boss, int amount) {
		Instance.bossBar.value -= amount;

		Instance.bossUIHitFeedback.PlayFeedbacks();
		Instance.bossBar.DOValue(Instance.bossBar.value, 0.25f).SetEase(Ease.Linear);
	}
}
