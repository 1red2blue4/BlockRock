using UnityEngine;
using System.Collections;

namespace Puzzlets {
	#if UNITY_ANDROID
	public class PuzzletConnectionAndroid : MonoBehaviour, IPuzzletInterop {
		
		private PuzzletConnection puzzletConnection;

		private AndroidJavaObject bleManagerInstance;
		private AndroidJavaObject bleService;

		public void Initialize(PuzzletConnection pc){
			puzzletConnection = pc;

			//create the java object that will manage the bluetooth on the device side
			using(var pluginClass = new AndroidJavaClass("com.digitaldreamlabs.bleplugin.BLEManager")){
				bleManagerInstance = pluginClass.CallStatic<AndroidJavaObject>("InitializeBluetooth", name);
			}

			StartCoroutine(SetFullscreen());
		}

		private IEnumerator SetFullscreen(){
			while(!bleManagerInstance.Call<bool>("SetFullscreen")){
				//yield return new WaitForSeconds(0.5f);
				for(int frame = 0; frame < 30; ++frame)
					yield return null;
			}
		}

		//find and connect to a Play Tray
		public void Connect () {
			bleService.Call ("PromptBluetooth");
			bleService.Call("ScanAndConnect");
		}
		
		//disconnect from the active connection
		public void Disconnect () {
			bleService.Call ("Disconnect");
		}
		
		public void SendMessage(byte[] data){
			byte[] sanitizedData;
			if(data.Length < 3){
				sanitizedData = new byte[3];
				data.CopyTo(sanitizedData, 0);
			} else {
				sanitizedData = data;
			}

			bleService.Call ("SendPacket", sanitizedData);
		}

		//message receiver to be notified when the bluetooth service has been created and can be retrieved
		public void BluetoothServiceReady(){
			bleService = bleManagerInstance.Call<AndroidJavaObject>("GetServiceInstance");
		}

		public void PuzzletReceiveMessageAndroid(string message){
			puzzletConnection.HandleMessage(PuzzletUtility.ParseMessage(message));
		}
		
		public void PuzzletConnectedAndroid(){
			puzzletConnection.PuzzletConnected();
		}
		
		public void PuzzletDisconnectedAndroid(){
			puzzletConnection.PuzzletDisconnected();
		}

		public void PuzzletSeveralTooFarAndroid(){
			ConnectionStatusDisplay.TooFar(true);
		}
		
		public void PuzzletSingleTooFarAndroid(){
			ConnectionStatusDisplay.TooFar(false);
		}
	}
	#endif
}
