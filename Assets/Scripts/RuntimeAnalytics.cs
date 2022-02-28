using System.Collections.Generic;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

public class RuntimeAnalytics {

	public static string UserDataFilePath => UnityEngine.Application.persistentDataPath + "/userdata.info";
	public static UserInfo PlayerInfo { private set; get; }
	private static Guid SessionGUID { set; get; }
	private static HttpClient client;
	private static Task UpdateSessionTask { set; get; }
	private static string serverKey = "2bda476c8cbe6972fe678a2c2eff6b6a6fd18bb19faf3f59271187fd0807fbb8";
	private static string PlayerGUID => PlayerInfo.playerGUID.ToString();
	private static bool HasBeenInitialized { get; set; }

	public static async Task Initialize() {
		await SessionStart();
		HasBeenInitialized = true;
		Logger.Log(Channel.System, "GameAnalytics have been initialized.");
	}

	public static async Task SessionStart() {
		SessionGUID = Guid.NewGuid();
		client = new HttpClient();

		if(File.Exists(UserDataFilePath) == false) {
			CreateUserInfo();
		} else {
			try {
				try {
					PlayerInfo = GetUserInfo();
				} catch {
					CreateUserInfo();
					PlayerInfo = GetUserInfo();
				}
				Logger.Log(Channel.Network, "Creating Session with UserID: " + PlayerInfo.playerGUID);

				var url = "https://sperlich.at/api/toytanks/sessionstart.php";
				var parameters = new Dictionary<string, string> {
					{ "serverKey", serverKey },
					{ "playerGUID", PlayerInfo.playerGUID.ToString() },
					{ "sessionStart", DateTime.UtcNow.ToString() },
					{ "sessionGUID", SessionGUID.ToString() },
				};
				var encodedContent = new FormUrlEncodedContent(parameters);
				var response = await client.PostAsync(url, encodedContent);
				if(response.IsSuccessStatusCode) {
					var status = ConvertResponse(await response.Content.ReadAsStringAsync());
					
					if(status.status == "success") {
						Logger.Log(Channel.Network, status.message);
					} else {
						Logger.Log(Channel.Network, "Error: " + status.message);
					}
				} else {
					Logger.LogError("Failed to create session.", null);
				}
			} catch(Exception e) {
				Logger.LogError("Something went wrong while creating the session.", e);
			}
		}

		UpdateSessionTask = UpdateSession();
	}

	private static async Task UpdateSession() {
		while(true && HasBeenInitialized) {
			await Task.Delay(3000);
			await UpdateSessionStatus();
		}
	}

	public static void CreateUserInfo() {
		File.Create(UserDataFilePath).Close();

		PlayerInfo = new UserInfo() {
			playerGUID = Guid.NewGuid(),
		};

		using var compressStream = new FileStream(UserDataFilePath, FileMode.Open, FileAccess.Write);
		using var ds = new DeflateStream(compressStream, CompressionLevel.Optimal);
		using var sw = new StreamWriter(ds);
		//sw.Write(JsonConvert.SerializeObject(PlayerInfo));
	}

	public static UserInfo GetUserInfo() {
		using var compressStream = new FileStream(UserDataFilePath, FileMode.Open, FileAccess.Read);
		using var ds = new DeflateStream(compressStream, CompressionMode.Decompress);
		using var sr = new StreamReader(ds);
		return JsonConvert.DeserializeObject<UserInfo>(sr.ReadToEnd());
	}

	public static async Task UpdateSessionStatus() {
		if(HasBeenInitialized) {
			try {
				var url = "https://sperlich.at/api/toytanks/sessionupdate.php";
				var parameters = new Dictionary<string, string> {
				{ "serverKey", serverKey },
				{ "playerGUID", PlayerInfo.playerGUID.ToString() },
				{ "sessionUpdate", DateTime.UtcNow.ToString() },
				{ "sessionGUID", SessionGUID.ToString() },
			};
				var encodedContent = new FormUrlEncodedContent(parameters);
				var response = await client.PostAsync(url, encodedContent);
				if(response.IsSuccessStatusCode) {
					var text = await response.Content.ReadAsStringAsync();
					if(text != string.Empty) {
						Logger.Log(Channel.System, "Result: " + text);
					}
				} else {
					Logger.LogError("Failed to update session.", null);
				}
			} catch(Exception e) {
				Logger.LogError("Failed to update session status!", e);
			}
		}
	}

	public static async Task AracadeLevelEnded(bool completed, ulong levelId, float time, int deaths, CampaignV1.Difficulty difficulty) {
		if(HasBeenInitialized) {
			try {
				var url = "https://sperlich.at/api/toytanks/arcadelevelinfo.php";
				var parameters = new Dictionary<string, string> {
				{ "serverKey", serverKey },
				{ "PLAYER_GUID", PlayerGUID },
				{ "DATE", DateTime.UtcNow.ToString() },
				{ "LEVEL_ID", levelId.ToString() },
				{ "DEATHS", deaths.ToString() },
				{ "DIFFICULTY", ((int)difficulty).ToString() },
				{ "TIME", time.ToString() },
				{ "COMPLETED", completed.ToString() },
			};
				var encodedContent = new FormUrlEncodedContent(parameters);
				var response = await client.PostAsync(url, encodedContent);
				if(response.IsSuccessStatusCode) {
					var text = await response.Content.ReadAsStringAsync();
					if(text != string.Empty) {
						Logger.Log(Channel.System, "Result: " + text);
					}
				} else {
					Logger.LogError("Failed to create session.", null);
				}
			} catch(Exception e) {
				Logger.LogError("Failed to update session status!", e);
			}
		}
	}

	private static (string status, string message) ConvertResponse(string response) {
		try {
			var obj = JObject.Parse(response);
			(string status, string message) resp = ("", "");
			resp.status = (string)obj["status"];
			if(resp.status == "success") {
				resp.message = (string)obj["message"];
			} else if(resp.status == "error") {
				resp.message = (string)obj["status"];
			} else {
				resp.message= "No error message found.";
			}
			return resp;
		} catch {
			return ("error", "Failed to parse response.");
		}
	}

	[Serializable]
	public class UserInfo {
		public Guid playerGUID;
	}
}
