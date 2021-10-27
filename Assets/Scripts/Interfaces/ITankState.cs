using System.Collections;

public interface ITankState {

	public abstract void ProcessState();
	public abstract void GoToNextState();
}
