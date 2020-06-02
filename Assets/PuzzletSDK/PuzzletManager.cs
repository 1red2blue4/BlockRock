using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Puzzlets;

//struct for sending updates on tiles being removed and added to the board
public struct PuzzletData {
	public int X;
	public int Y;
	public PuzzletKey ID;

	public PuzzletData (int x, int y, PuzzletKey id){
		X = x;
		Y = y;
		ID = id;
	}
}

public class PuzzletManager : MonoBehaviour {

	//basic values about the Puzzlet board
	private static int gridWidth = 6;
	private static int gridHeight = 5;

	public static int PuzzletGridWidth
	{
		get{
			return gridWidth;
		}
	}
	public static int PuzzletGridHeight
	{
		get{
			return gridHeight;
		}
	}

	//make the PuzzletManager a singleton
	private static PuzzletManager instance;
	public static PuzzletManager Instance
	{
		get{
			return instance;
		}
	}

	//store the current state of the board
	private int[] prevRawArray = new int[PuzzletGridWidth * PuzzletGridHeight];
	public static PuzzletKey[,] Tiles = new PuzzletKey[PuzzletGridWidth,PuzzletGridHeight];

	//accessor for the number of Puzzlet on the board
	public static int NumPuzzlet {
		get {
			int count = 0;
			for(int xx = 0; xx < PuzzletGridWidth; xx++){
				for(int yy = 0; yy < PuzzletGridHeight; yy++){
					if(Tiles[xx,yy] != PuzzletKey.____NA){
						count++;
					}
				}
			}
			return count;
		}
	}

	//list of all objects registered to receive updates on the state of the board
	private static List<IPuzzletReceiver> receivers = new List<IPuzzletReceiver>();

	void Awake () {
		//make it a singleton
		if(instance != null){
			Destroy(gameObject);
			return;
		}
		instance = this;

		InterpretBlocks();
	}
	
	public static void RegisterReceiver(IPuzzletReceiver ilr){
		receivers.Add(ilr);
	}
	
	public void InterpretBlocks () {
		int[] masterArray;

		masterArray = PuzzletConnection.RawPuzzletData;

		//detect any changes to the array
		PuzzletData[] removedTiles, addedTiles;
		bool changed = detectChange(masterArray,prevRawArray,out removedTiles, out addedTiles);
		
		//update the prevRawArray
		if(prevRawArray.Length != masterArray.Length){
			prevRawArray = new int[masterArray.Length];
		}
		System.Array.Copy (masterArray,prevRawArray,masterArray.Length);

		//update the array of tiles
		for(int xx = 0; xx < PuzzletGridWidth; xx++){
			for(int yy = 0; yy < PuzzletGridHeight; yy++){
				if(System.Enum.IsDefined(typeof(PuzzletKey), masterArray[xx+yy*PuzzletGridWidth])){
					Tiles[xx,yy] = (PuzzletKey)masterArray[xx+yy*PuzzletGridWidth];
				} else {
					Tiles[xx,yy] = PuzzletKey.____NA;
				}
			}
		}

		if(changed){
			//notify registered parties about the changes while removing destroyed receivers
			int a = 0;

			while(a < receivers.Count)
			{
				if(receivers[a] != null)
				{
					receivers[a].PuzzletChanged(removedTiles, addedTiles);
					++a;
				}
				else
					receivers.RemoveAt(a);
			}
		}
	}
	
	private bool detectChange(int[] newInstructionArray, int[] oldInstructionArray, out PuzzletData[] instructionsOut, out PuzzletData[] instructionsIn){
		List<PuzzletData> outList = new List<PuzzletData>();
		List<PuzzletData> inList = new List<PuzzletData>();
		
		//compare each new value to the corresponding old value
		for(int ii = 0; ii < newInstructionArray.Length && ii < oldInstructionArray.Length; ii++){
			//if they're different, add the changes to inList and outList
			if(oldInstructionArray[ii] != newInstructionArray[ii]){
				//if the old value is defined as a tile, add it as a removed tile
				if(System.Enum.IsDefined(typeof(PuzzletKey),oldInstructionArray[ii]) && oldInstructionArray[ii] != (int)PuzzletKey.____NA){
					outList.Add(new PuzzletData(ii % PuzzletGridWidth, ii / PuzzletGridWidth, (PuzzletKey)oldInstructionArray[ii]));
				}
				//if the new value is defined as a tile, add it as an added tile
				if(System.Enum.IsDefined(typeof(PuzzletKey),newInstructionArray[ii]) && newInstructionArray[ii] != (int)PuzzletKey.____NA){
					inList.Add (new PuzzletData(ii % PuzzletGridWidth, ii / PuzzletGridWidth, (PuzzletKey)newInstructionArray[ii]));
				}
			}
		}
		
		//convert the lists to arrays
		instructionsOut = outList.ToArray();
		instructionsIn = inList.ToArray();
		
		//if there are any changes, return true, otherwise return false
		return (instructionsOut.Length + instructionsIn.Length > 0)? true : false;
	}
}
