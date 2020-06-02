using UnityEngine;
using System.Collections;

public interface IPuzzletReceiver {
	
	void PuzzletChanged(PuzzletData[] removedTiles, PuzzletData[] addedTiles);
	
}
