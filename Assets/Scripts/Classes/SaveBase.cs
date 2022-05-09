using System;

[Serializable]
public class SaveBase {

	public string SaveGameVersion { get; set; }
	public DateTime LastModified { get; set; }
	public Guid PlayerGuid { get; set; }

}
