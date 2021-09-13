using System;
using System.Collections.Generic;

[Serializable]
public class SaveV1 : ISaveGame {

	public int SaveGameVersion => 1;
	public string LastPlayedUtcTimestamp { get; set; }

	public List<World> Worlds { get; set; }

	public SaveGame.Campaign CurrentCampaign { get; set; }

	[Serializable]
	public class World {
		public Worlds world;
		public Level[] levels;
	}

	[Serializable]
	public class Level {
		public int LevelId;
		public bool IsUnlocked;
	}
}