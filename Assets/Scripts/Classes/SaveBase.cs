using System;

[Serializable]
public abstract class SaveBase {

	public string SaveGameVersion { get; }
	public DateTime LastModified { get; set; }

}
