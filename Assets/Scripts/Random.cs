using System;
using System.Collections.Generic;

public class Randomizer {

	static System.Random _random;
	static System.Random random {
		get {
			if(_random == null) {
				_random = new System.Random((int)DateTime.Now.Ticks);
			}
			return _random;
		}
	}


	/// <summary>
	/// Returns an inclusive int within the Range of Min,Max
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	public static int Range(int min, int max) {
		return random.Next(min, (max + 1));
	}

	/// <summary>
	/// Returns an inclusive float within the Range of Min,Max
	/// </summary>
	/// <param name="min"></param>
	/// <param name="max"></param>
	/// <returns></returns>
	public static float Range(float min, float max) {
		return (float)random.NextDouble() * ((max + 1) - min) + min;
	}

	public static void RenewRandom() => _random = new System.Random((int)DateTime.Now.Ticks);
}
