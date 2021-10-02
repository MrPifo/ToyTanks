using CarterGames.Assets.AudioManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer {

	public static void Play(string track, float randomMinPitch = 1f, float randomMaxPitch = 1f, float volume = 1f) {
		if(AudioManager.instance != null && AudioManager.instance.HasClip(track)) {
			AudioManager.instance.Play(track, 0, GraphicSettings.MainVolume * GraphicSettings.SoundEffectVolume * volume, Random.Range(randomMinPitch, randomMaxPitch));
		}
	}
}
