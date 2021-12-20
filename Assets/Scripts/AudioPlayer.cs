using CarterGames.Assets.AudioManager;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioPlayer {

	public static void Play(string track, AudioType audioType, float randomMinPitch = 1f, float randomMaxPitch = 1f, float sourceVolume = 1f) {
		if(AudioManager.instance != null && track != "" && AudioManager.instance.HasClip(track)) {
			float globalVolume = GraphicSettings.MainVolume / 100f * sourceVolume;
			switch(audioType) {
				case AudioType.Default:
					break;
				case AudioType.SoundEffect:
					globalVolume *= GraphicSettings.SoundEffectsVolume / 100f;
					break;
				case AudioType.Music:
					break;
				default:
					break;
			}
			AudioManager.instance.Play(track, 0, globalVolume, Random.Range(randomMinPitch, randomMaxPitch));
		}
	}
}

