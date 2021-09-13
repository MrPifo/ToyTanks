public interface ISaveGame {
	public int SaveGameVersion { get; }
	public string LastPlayedUtcTimestamp { get; set; }
}