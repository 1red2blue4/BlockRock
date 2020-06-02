using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Puzzlets
{
	public class FirmwareVersionCheck : MonoBehaviour, PuzzletConnectionStateReceiver
	{
		public GameObject updatePrompt;
		public GameObject[] texts;

		const string VersionNumberURL = "https://digitaldreamlabs.com/downloads/Firmware/FirmwareRequiredVersion.txt";

		int[][] requiredVersions;

		public bool passed = false;
		bool didTryDownloadVersion = false;

		public Text currentText;
		public Text needsText;

		IEnumerator DownloadLatestVersions()
		{
			WWW vNumPage = new WWW(VersionNumberURL);
			yield return vNumPage;

			if(!string.IsNullOrEmpty(vNumPage.error))
			{
				string[] hardwareSplit = vNumPage.text.Split(new char[]{'\n'});
				requiredVersions = new int[hardwareSplit.Length][];
				for(int ii = 0; ii < hardwareSplit.Length; ii++){
					string[] tempDigits = hardwareSplit[ii].Split(new char[]{','});
					try{
						int hardwareNum = int.Parse(tempDigits[3]);
						requiredVersions[hardwareNum] = new int[3];
						for(int xx = 0; xx < 3; xx++){
							requiredVersions[hardwareNum][xx] = int.Parse(tempDigits[xx]);
						}
					} catch (System.Exception ee){
						Debug.LogWarning(ee.Message);
						requiredVersions = null;
					}
				}
			}

			didTryDownloadVersion = true;
			Debug.Log("Finished fimware version download. Attempt " + (requiredVersions==null? "successful" : "failed"));
		}

		IEnumerator CheckVersion()
		{
			passed = false;

			while(!didTryDownloadVersion)
				yield return 0;

			while(!passed)
			{
				if(!PuzzletConnection.Connected)
					yield break;

				//check hardcoded and network firmwares
				bool passedHardcodedFirmware = PuzzletUtility.CheckFirmwareVersion();

				bool passedNetworkFirmware = true;
				if(requiredVersions != null)
					 passedNetworkFirmware = PuzzletUtility.CheckFirmwareVersion(requiredVersions[PuzzletConnection.HardwareVersion]);

				//show required version appropriate to which check failed (if they failed)
				if(!passedHardcodedFirmware)
					needsText.text = PuzzletUtility.RequiredFirmwareString().Substring(0,5);

				if(!passedNetworkFirmware)
					needsText.text = PuzzletUtility.FirmwareString(requiredVersions[PuzzletConnection.HardwareVersion]).Substring(0,5);

				currentText.text = PuzzletUtility.FirmwareString().Substring(0,5);

				//stay up or dismiss depending on check
				passed = passedHardcodedFirmware && passedNetworkFirmware;

				if(passed)
					updatePrompt.SetActive(false);
				else
				{
					updatePrompt.SetActive(true);
					yield return 0;
				}
			}
		}

		void Awake()
		{
			PuzzletConnection.RegisterReceiver(this);
			StartCoroutine(DownloadLatestVersions());

			//set texts to appropriate for device
			#if UNITY_STANDALONE || UNITY_EDITOR
			string deleteStr = "Bluetooth";
			#else
			string deleteStr = "USB";
			#endif

			for(int a = 0; a < texts.Length; ++a)
			{
				if(texts[a].name.Contains(deleteStr))
					Destroy(texts[a]);
			}
		}

		public void PuzzletConnectionStateChanged(PuzzletConnection.State lastState, PuzzletConnection.State state)
		{
			if(state == PuzzletConnection.State.Connected)
				StartCoroutine(CheckVersion());

			if(state == PuzzletConnection.State.Disconnected && lastState == PuzzletConnection.State.Connected)
				updatePrompt.SetActive(false);
		}
	}
}