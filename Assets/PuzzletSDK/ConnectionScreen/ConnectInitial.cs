using UnityEngine;
using System.Collections;
using Puzzlets;

public class ConnectInitial : MonoBehaviour {

	private const string VersionNumberURL = "http://digitaldreamlabs.com/downloads/Firmware/FirmwareVersion.txt";
	
	public Animator ConnectionAnim;
	
	private int touchID = -1;
	
	private int[][] latestVersions;

	void Start () {
		StartCoroutine(DownloadLatestVersions());
		StartCoroutine(CheckConnection());
	}

	IEnumerator DownloadLatestVersions(){
		WWW vNumPage = new WWW(VersionNumberURL);
		yield return vNumPage;
		if(!string.IsNullOrEmpty(vNumPage.error)){
			//leave the current version as null
			yield break;
		}
		string[] hardwareSplit = vNumPage.text.Split(new char[]{'\n'});
		latestVersions = new int[hardwareSplit.Length][];
		for(int ii = 0; ii < hardwareSplit.Length; ii++){
			string[] tempDigits = hardwareSplit[ii].Split(new char[]{','});
			try{
				int hardwareNum = int.Parse(tempDigits[3]);
				latestVersions[hardwareNum] = new int[3];
				for(int xx = 0; xx < 3; xx++){
					latestVersions[hardwareNum][xx] = int.Parse(tempDigits[xx]);
				}
			} catch (System.Exception ee){
				Debug.LogWarning(ee.Message);
				latestVersions = null;
				yield break;
			}
		}
	}

	IEnumerator CheckConnection () {
		//hold on the company logo for at least 2.5 seconds
		yield return new WaitForSeconds(2.5f);
		//for the next 7.5 seconds, skip to the title as soon as a connection is found
		for(int ii = 0; ii < 75; ii++){
			yield return new WaitForSeconds(.1f);
			if(PuzzletConnection.Connected){
				break;
			}
		}

		if(!PuzzletConnection.Connected){
			ConnectionAnim.SetBool("Invisible", false);
			ConnectionAnim.SetBool ("ConnectionEstablished", false);


			//wait for a connection or a command to jump to the demo
			while(!PuzzletConnection.Connected){
				yield return new WaitForSeconds(.1f);
			}
		}

		//turn the connection error off and go to the title screen
		bool requiredVersion = PuzzletUtility.CheckFirmwareVersion();
		bool upToDateVersion = true;
		if(latestVersions != null && !PuzzletUtility.CheckFirmwareVersion(latestVersions[PuzzletConnection.HardwareVersion])){
			upToDateVersion = false;
		}

		ConnectionAnim.SetBool("Invisible", false);
		ConnectionAnim.SetBool ("RequiredVersion", requiredVersion);
		ConnectionAnim.SetBool ("UpToDate", upToDateVersion);
		ConnectionAnim.SetBool ("ConnectionEstablished", true);
		Debug.Log (string.Format ("Required: {0}, Up To Date: {1}", requiredVersion, upToDateVersion));
		Debug.Log (string.Format ("Required Verson: {0}, Current Version: {1}", PuzzletUtility.RequiredFirmwareString(), PuzzletUtility.FirmwareString())); 

		yield return new WaitForSeconds(.5f);

#if UNITY_WEBPLAYER
		//if it's a webplayer, wait until the next scene is loaded
		while(!Application.CanStreamedLevelBeLoaded(LevelValues.TitleScreen)){
			yield return new WaitForSeconds(.1f);
		}
#endif
		if(requiredVersion && upToDateVersion){
			Application.LoadLevel(1); //always load second level, since this connection screen should always be first
		} else {
			StartCoroutine (CheckConnection ());
		}
	}

	void OnApplicationPause(bool paused){
		if(!paused){
			StopAllCoroutines();
			ConnectionAnim.SetBool("Invisible", false);
			ConnectionAnim.SetBool ("ConnectionEstablished", false);
			StartCoroutine (CheckConnection());
		}
	}
}
