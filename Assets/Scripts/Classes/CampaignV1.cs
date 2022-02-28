using System;
using UnityEngine;

[Serializable]
public class CampaignV1 {

	public enum Difficulty { Easy, Medium, Hard, Original }

	public float PrettyTime => Mathf.Round(time * 100f) / 100f;
	public WorldTheme CurrentWorld => Game.GetWorld(levelId).WorldType;

	public Difficulty difficulty;
	public ulong levelId;
	public byte lives;
	public int score;
	public float time;
	public float liveGainChance;

	
}