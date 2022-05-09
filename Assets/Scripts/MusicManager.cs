using System.Collections;
using System.Collections.Generic;
using JSAM;
using UnityEngine;

public class MusicManager : Singleton<MusicManager> {

    public Music playingAmbient;
	public Music playingMusicTheme;

    public static void PlayAmbient(WorldTheme world) {
		switch (world) {
			case WorldTheme.Woody:
				AudioManager.PlayMusic3D(Music.AmbientWoody, Camera.main.transform, LoopMode.Looping);
				break;
			case WorldTheme.Fir:
				AudioManager.PlayMusic3D(Music.AmbientWoody, Camera.main.transform, LoopMode.Looping);
				break;
			case WorldTheme.Snowy:
				AudioManager.PlayMusic3D(Music.AmbientWinter, Camera.main.transform, LoopMode.Looping);
				break;
			case WorldTheme.Garden:
				AudioManager.PlayMusic3D(Music.AmbientGarden, Camera.main.transform, LoopMode.Looping);
				break;
		}
	}

	public static void PlayMusic(WorldTheme world) {
		switch (world) {
			case WorldTheme.Woody:
				AudioManager.PlayMusic(Music.WoodyTheme);
				Instance.playingMusicTheme = Music.WoodyTheme;
				break;
			case WorldTheme.Fir:
				//AudioManager.PlayMusic3D(Music.AmbientWoody, Camera.main.transform, LoopMode.Looping);
				break;
			case WorldTheme.Snowy:
				//AudioManager.PlayMusic3D(Music.AmbientWinter, Camera.main.transform, LoopMode.Looping);
				break;
			case WorldTheme.Garden:
				//AudioManager.PlayMusic3D(Music.AmbientGarden, Camera.main.transform, LoopMode.Looping);
				break;
		}
	}

	public static void StopMusic() => AudioManager.StopMusic(Instance.playingMusicTheme);
}
