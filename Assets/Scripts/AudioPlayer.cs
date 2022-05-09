using System.Collections;
using System.Collections.Generic;
using JSAM;
using UnityEngine;

public class AudioPlayer {

	public static AudioManager Instance => AudioManager.instance;

	public static void Play(Sounds track, AudioType audioType, float pitch, float volume) => Play(track, audioType, pitch, pitch, volume); 
	public static void Play(Sounds track, AudioType audioType, float randomMinPitch = 1f, float randomMaxPitch = 1f, float sourceVolume = 1f) {
		if(Instance != null && AudioManager.GetSound(track).file != null) {
			float globalVolume = GraphicSettings.MainVolume / 100f * sourceVolume;
			var clip = AudioManager.GetSound(track);
			switch (audioType) {
				case AudioType.Default:
					break;
				case AudioType.SoundEffect:
					globalVolume *= GraphicSettings.SoundEffectsVolume / 100f;
					break;
				case AudioType.Music:
					break;
				case AudioType.UI:
					globalVolume *= GraphicSettings.SoundEffectsVolume / 100f;
					break;
				default:
					break;
			}
			clip.relativeVolume = globalVolume;
			if(randomMinPitch != 1f || randomMaxPitch != 1f) {
				clip.pitchShift = 0f;
				clip.startingPitch = Random.Range(randomMinPitch, randomMaxPitch);
			}
			try {
				AudioManager.PlaySound(track);
			} catch {

			}
		}
	}
}

