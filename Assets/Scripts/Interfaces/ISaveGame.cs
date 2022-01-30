using System;

public interface ISaveGame {
	public int SaveGameVersion { get; }
	public DateTime LastModified { get; set; }
}