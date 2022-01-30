using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public enum Channel : uint {
	/// <summary>
	/// Logs to do with AI
	/// </summary>
	Default,
	/// <summary>
	/// Logs to do with graphics/rendering
	/// </summary>
	Rendering,
	/// <summary>
	/// Logs to do with UI system
	/// </summary>
	UI,
	/// <summary>
	/// Logs to do with sound
	/// </summary>
	Audio,
	/// <summary>
	/// Logs to do with loading
	/// </summary>
	Loading,
	/// <summary>
	/// Logs to do with platform services
	/// </summary>
	Platform,
	/// <summary>
	/// Logs asserts
	/// </summary>
	Assert,
	/// <summary>
	/// Logs to do with systems/generation
	/// </summary>
	System,
	/// <summary>
	/// Logs to do with progress/game saving
	/// </summary>
	SaveGame,
	/// <summary>
	/// Logs to do with GraphicSettings.
	/// </summary>
	Graphics,
	/// <summary>
	/// Logs to do with Gameplay.
	/// </summary>
	Gameplay,
	/// <summary>
	/// Logs to do with Networking/API
	/// </summary>
	Network,
	/// <summary>
	/// Logs to do with PlayerStats
	/// </summary>
	PlayerStats,
}

public class Logger {

	public static string FileLogPath => Application.persistentDataPath + "/ToyTanks.log";

	public static void LogError(string message, Exception e, Channel channel = Channel.Default) {
		message = message + "\n ErrorMessage: \n" + e.Message;

		string consoleString = string.Empty;
		string fileString = string.Empty;
		DateTime date = DateTime.Now;

		consoleString = $"[<color={channelToColour[channel]}>" + channel.ToString() + "</color>] " + message;
		fileString = "[" + date.ToShortTimeString() + "]" + $"[" + channel.ToString() + "] " + message;
		fileString = Encoding.UTF8.GetString(Encoding.Default.GetBytes(fileString));

		WriteToLogfile(fileString);
		Debug.LogError(consoleString);
	}
	public static void Log(Channel channel, string message) => Log(message, channel);
	public static void Log(string message, Channel channel) {
		string consoleString = string.Empty;
		string fileString = string.Empty;
		DateTime date = DateTime.Now;
		
		consoleString = $"[<color={channelToColour[channel]}>" + channel.ToString() + "</color>] " + message;
		fileString = "[" + date.ToShortTimeString() + "]" + $"[" + channel.ToString() + "] " + message;
		fileString = Encoding.UTF8.GetString(Encoding.Default.GetBytes(fileString));

		WriteToLogfile(fileString);
		Debug.Log(consoleString);
	}

	public static void Initialize() {
		ClearLogFile();
	}

	public static void ClearLogFile() {
		if(File.Exists(FileLogPath)) {
			File.WriteAllText(FileLogPath, "");
		}
	}

	private static void WriteToLogfile(string message) {
		try {
			if (File.Exists(FileLogPath) == false) {
				using (var fs = File.Create(FileLogPath)) {}
			}
			using(StreamWriter sw = new StreamWriter(FileLogPath, true, Encoding.UTF8)) {
				sw.WriteLine(message);
			}
		} catch {

		}
	}

	/// <summary>
	/// Map a channel to a colour, using Unity's rich text system
	/// </summary>
	private static readonly Dictionary<Channel, string> channelToColour = new Dictionary<Channel, string> {
		{ Channel.System,       "blue" },
		{ Channel.Rendering,    "green" },
		{ Channel.Default,      "white" },
		{ Channel.SaveGame,     "orange" },
		{ Channel.UI,           "purple" },
		{ Channel.Audio,        "teal" },
		{ Channel.Loading,      "olive" },
		{ Channel.Platform,     "lightblue" },
		{ Channel.Assert,       "red" },
		{ Channel.Graphics,     "yellow" },
		{ Channel.Gameplay,     "cyan" },
		{ Channel.Network,		"pink" },
		{ Channel.PlayerStats,   "red"},
	};

}
