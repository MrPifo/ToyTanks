using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using MoreMountains.Feedbacks;
using Sperlich.PrefabManager;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BossUI : MonoBehaviour {
    
	private static Dictionary<BossAI, int> Bosses = new Dictionary<BossAI, int>();
	public Slider bossBar;
	public MMFeedbacks bossUIInitFeedback;
	public MMFeedbacks bossUIHitFeedback;
	public TranslucentImage backgroundBlur;

	private static BossUI instance;
	public static BossUI Instance {
		get {
			if(instance == null) {
				if(FindObjectOfType<BossUI>() == null) {
					instance = PrefabManager.Instantiate<BossUI>(PrefabTypes.BossBar);
					instance.name = "BossUI";
					Logger.Log(Channel.UI, "Created Singleton of " + typeof(BossUI).Name);
				} else {
					instance = FindObjectOfType<BossUI>();
				}
			}
			return instance;
		}
	}

	public static void RegisterBoss(BossAI boss) {
		if(Bosses.ContainsKey(boss) == false) {
			Bosses.Add(boss, boss.MaxHealthPoints);
			Instance.bossBar.maxValue += boss.MaxHealthPoints;
			Instance.bossBar.value += boss.MaxHealthPoints;
		}
	}

	public static void InitAnimateBossBar() {
		Instance.bossBar.Show();
		if(Bosses.Count > 0) {
			Instance.bossBar.value = 0;
			Instance.backgroundBlur.source = FindObjectOfType<TranslucentImageSource>();
			//Instance.bossBar.transform.localScale = Vector3.one * 5;
			Instance.bossBar.DOValue(Instance.bossBar.maxValue, 3f).SetEase(Ease.Linear);
			//Instance.bossBar.transform.DOScale(1, 2);
			Instance.bossUIInitFeedback.PlayFeedbacks();
		}
	}

	public static void RemoveBoss(BossAI boss) {
		if(Bosses.ContainsKey(boss)) {
			Bosses.Remove(boss);
			Instance.bossBar.maxValue -= boss.MaxHealthPoints;

			if(Bosses.Count == 0) {
				ResetBossBar();
			}
		}
	}

	public static void ResetBossBar() {
		Bosses = new Dictionary<BossAI, int>();
		Instance.bossBar.value = 0;
		Instance.bossBar.maxValue = 0;
		Instance.bossBar.Hide();
		Debug.Log("<color=red>RESET</color>");
	}

	public static void BossTakeDamage(BossAI boss, int amount) {
		Instance.bossBar.value -= amount;

		Instance.bossUIHitFeedback.PlayFeedbacks();
		Instance.bossBar.DOValue(Instance.bossBar.value, 0.25f).SetEase(Ease.Linear);
	}
}
