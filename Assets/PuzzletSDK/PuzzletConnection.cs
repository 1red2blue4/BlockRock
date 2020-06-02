using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Puzzlets
{
	public interface PuzzletConnectionStateReceiver
	{void PuzzletConnectionStateChanged(PuzzletConnection.State lastState, PuzzletConnection.State state);}

	public class PuzzletConnection : MonoBehaviour
	{

		private static PuzzletConnection instance;
		public static PuzzletConnection Instance{
			get{
				return instance;
			}
		}

		public static int[] RawPuzzletData = new int[PuzzletManager.PuzzletGridWidth * PuzzletManager.PuzzletGridHeight];

		public static int[] FirmwareDigits = new int[3];
		public static int HardwareVersion;

		public Text debugText;

		private IPuzzletInterop puzzletInterop;

		public enum State
		{
			Disconnected,
			Handshaking,
			Connected,
			Idle
		}
		private static State stateInternal = State.Idle;
		private static State state
		{
			set
			{
				if(stateInternal == value)
					return;

				State lastState = stateInternal;
				stateInternal = value;

				int a = 0;

				while(a < receivers.Count)
				{
					if(receivers[a] != null)
					{
						receivers[a].PuzzletConnectionStateChanged(lastState, value);
						++a;
					}
					else
						receivers.RemoveAt(a);
				}
			}
			get{return stateInternal;}
		}

		public static bool Connected
		{get{return state == State.Connected;}}

		private bool shouldScanPuzzlets = false;
		private float pingTime = 0;
		private float connectedTime = 0;

		private const int MaxHandshakeAttempts = 3;
		private const float PingTimeout = 4f;
		private const float MinTimeToScan = 0.5f;
		private const float RestartScanDelay = 0.1f;
		//private const int MinFirmwareInt = 170, MaxFirmwareInt = 190; //from Cork's PuzzletSDK

		private static List<PuzzletConnectionStateReceiver> receivers = new List<PuzzletConnectionStateReceiver>();

		public static void RegisterReceiver(PuzzletConnectionStateReceiver receiver)
		{
			receivers.Add(receiver);
		}

		public const float interactionTimeout = 300;
		public static float timeSinceInteraction = 0;
		public static bool interactionTimedOut{get{return timeSinceInteraction >= interactionTimeout;}}
		static bool resumeScanOnInteraction = false;

		void Awake () {
			if (instance){
				enabled = false;
				Destroy(this);
				return;
			}
			
			instance = this;
			DontDestroyOnLoad(gameObject);

			state = State.Disconnected;

	//when editing IOS or Android mode, comment out UNITY_EDITOR from the compile conditional
	#if UNITY_STANDALONE || UNITY_EDITOR
			puzzletInterop = gameObject.AddComponent<PuzzletConnectionUSB>();
	#elif UNITY_IOS
			puzzletInterop = gameObject.AddComponent<PuzzletConnectionIOS>();
	#elif UNITY_ANDROID
			puzzletInterop = gameObject.AddComponent<PuzzletConnectionAndroid>();
	#endif

			puzzletInterop.Initialize(this);
			puzzletInterop.Connect();
		}

		void Update ()
		{
			if(debugText)
			{
				int lineCount = 0;
				for(int ii = debugText.text.Length - 1; ii > 0; ii--)
				{
					if(debugText.text[ii] == '\n')
					{
						lineCount++;
						if(lineCount == 40)
						{
							debugText.text = debugText.text.Substring(ii+1);
							break;
						}
					}
				}
			}

			if(state == State.Connected)
			{
				//interaction timeout
				timeSinceInteraction += Time.unscaledDeltaTime;

				if(Input.touchCount > 0 || Input.GetAxis("Mouse X") != 0 || Input.anyKeyDown)
					Interacted();

				if(interactionTimedOut && !resumeScanOnInteraction)
				{
					resumeScanOnInteraction = true;
					stopScanning();
				}

				//ping timeout
				pingTime += Time.unscaledDeltaTime;
				connectedTime += Time.unscaledDeltaTime;

				if(pingTime > PingTimeout)
				{
					PuzzletDisconnected ();
					puzzletInterop.Disconnect();
					puzzletInterop.Connect();
				}
			}
			//Debug.Log ("Connected? " + Connected + ", should scan? " + shouldScanPuzzlets);

		}

		//starts the scanning process on the Puzzlet Board
		public static void StartScanning () {
			Debug.Log ("Start Scanning");
			if(instance){
				instance.startScanning();
			}
		}

		private void startScanning () {
			//process the data we currently have (in almost all cases, it should just be empty)
			if(PuzzletManager.Instance){
				PuzzletManager.Instance.InterpretBlocks();
			}
			shouldScanPuzzlets = true;
			sendSimpleCommand(SimpleCommands.START_SCAN);
		}
		
		//stops the scanning process on the Puzzlet Board
		public static void StopScanning () {
			Debug.Log("Stop Scanning");
			if(instance){
				instance.stopScanning ();
			}
		}

		private void stopScanning () {
			shouldScanPuzzlets = false;
			ClearPuzzlet();
			sendSimpleCommand(SimpleCommands.STOP_SCAN);
		}

		public static void ResetScanning () {
			if(instance){
				Debug.Log("RESET SCANNING");
				instance.resetScanning();
			}
		}

		private void resetScanning () {
			stopScanning ();
			Invoke("startScanning", RestartScanDelay);
		}

		public static void ClearPuzzlet(){
			if(instance){
				instance.clearPuzzlet();
			}
		}

		private void clearPuzzlet(){
			RawPuzzletData = new int[PuzzletManager.PuzzletGridWidth * PuzzletManager.PuzzletGridHeight];
			if(PuzzletManager.Instance){
				PuzzletManager.Instance.InterpretBlocks();
			}
		}

		public void HandleMessage(byte[] message){
			if(debugText){
				debugText.text += string.Format ("{0}",message[0]);
				for(int ii = 1; ii < message.Length; ii++){
					debugText.text += string.Format (".{0}",message[ii]);
				}
				debugText.text += "\n";
			}

			//we got data, so we know that we're still connected
			pingTime = 0;
			switch((MessageHeaders)message[0]){
			case MessageHeaders.HANDSHAKE:
				//received handshake
				Debug.Log("Received return handshake");

				//read the firmware version
				if(message.Length >= 5){
				  //save the firmware version
					Debug.Log(string.Format("firmware version: {0}.{1}.{2}.{3}", message[1],message[2],message[3],message[4]));
					//FirmwareVersion = string.Format("{0}.{1}.{2}.{3}", message[1],message[2],message[3],message[4]);
					FirmwareDigits[0] = message[1];
					FirmwareDigits[1] = message[2];
					FirmwareDigits[2] = message[3];
					HardwareVersion = message[4];
					/*if(HardwareVersion >= MinFirmwareInt && HardwareVersion <= MaxFirmwareInt){
						Debug.LogWarning(string.Format("Bad firmware version number: {0}", HardwareVersion));
						HardwareVersion = 1;
					} else if(HardwareVersion > 50){
						Debug.LogError(string.Format("Uncaught bad firmware version number: {0}", HardwareVersion));
					}*/
				} else if (message.Length >= 4){
					//save the firmware version
					Debug.Log(string.Format("firmware version: {0}.{1}.{2}", message[1],message[2],message[3]));
					//FirmwareVersion = string.Format("{0}.{1}.{2}.{3}", message[1],message[2],message[3],message[4]);
					FirmwareDigits[0] = message[1];
					FirmwareDigits[1] = message[2];
					FirmwareDigits[2] = message[3];
					//any firmware version 1.5.x is read as hardware version 1 because bluetooth connections didn't send hardware version until 1.6
					if(FirmwareDigits[0] == 1 && FirmwareDigits[1] == 5){
						HardwareVersion = 1;
					} else {
						HardwareVersion = 0;
					}
				}

				state = State.Connected;
				startScanning();

				return;
			case MessageHeaders.SIMPLE_COMMAND:
				switch((SimpleCommands)message[1]){
				case SimpleCommands.CONNECTED_PING:
					//this ping requests a return ping and indicates that the board is not scanning
					sendSimpleCommand(SimpleCommands.CONNECTED_PING);
					
					if(shouldScanPuzzlets)
					{
						sendSimpleCommand(SimpleCommands.START_SCAN);
						Debug.Log("SENT START_SCAN");
					}
					//Debug.Log("CONNECTED_PING shouldScanPuzzlets=" + shouldScanPuzzlets);
					return;
				case SimpleCommands.SCAN_PING:
					//this ping requests a return ping and indicates that the board is scanning
					sendSimpleCommand(SimpleCommands.CONNECTED_PING);
					
					if(!shouldScanPuzzlets)
					{
						sendSimpleCommand(SimpleCommands.STOP_SCAN);
					}
					//Debug.Log("SCAN_PING shouldScanPuzzlets=" + shouldScanPuzzlets);
					return;
				}
				return;
			case MessageHeaders.DATA_COMMAND:
				Interacted();
				Debug.Log("DATA_COMMAND at " + Time.time);
				
				switch((DataCommands)message[1]){
				case DataCommands.POCKET_DATA:
					if(message.Length < 4){
						//not enough data for a pocket message
						return;
					}
					//pull out the index(byte 2) and value(byte 3)
					RawPuzzletData[message[2]] = (byte)message[3];
					if(PuzzletManager.Instance){
						PuzzletManager.Instance.InterpretBlocks();
					}

					//ResetScanOnDataCommandTypeChange(DataCommandType.pocket);
					return;
				case DataCommands.ROW_DATA:
					if(message.Length < 9){
						//not enough data for a row message
						return;
					}
					//start filling in data at the index
					for(int ii = 0; ii < 6; ii++){
						RawPuzzletData[message[2] + ii] = (byte)message[3+ii];
					}
					if(PuzzletManager.Instance){
						PuzzletManager.Instance.InterpretBlocks();
					}

					//ResetScanOnDataCommandTypeChange(DataCommandType.row);
					return;
				}
				return;
			}
		}

		public void PuzzletConnected(){
			if(debugText){
				debugText.text += "Connection Established\n";
			}

			StartCoroutine(ConfirmConnection ());
		}

		private IEnumerator ConfirmConnection(){
			state = State.Handshaking;
			for(int attempts = 0; attempts < MaxHandshakeAttempts && state == State.Handshaking; attempts++){
				if(debugText){
					debugText.text += "send handshake\n";
				}
				puzzletInterop.SendMessage(new byte[1] {(byte)MessageHeaders.HANDSHAKE});
				//yield return new WaitForSeconds(0.5f);
				for(int frame = 0; frame < 30; ++frame)
					yield return null;
			}

			if(state != State.Connected)
			{
				puzzletInterop.Disconnect();
				puzzletInterop.Connect();
			}
		}

		public void PuzzletDisconnected(){
			if(debugText){
				debugText.text += "Disconnected\n";
			}
			Debug.Log("Disconnected");
			state = State.Disconnected;
			pingTime = 0;
			connectedTime = 0;
			clearPuzzlet();
			StopAllCoroutines ();
		}

		private void sendSimpleCommand(SimpleCommands cmd){
			if(debugText){
				debugText.text += string.Format ("send {0}\n", cmd);
			}
			if(instance){
				instance.puzzletInterop.SendMessage(new byte[2] {(byte)MessageHeaders.SIMPLE_COMMAND,(byte)cmd});
			}
		}

		//disables the bluetooth connection when the application is paused and reconnects when it is unpaused
		void OnApplicationPause(bool pause){
			if(pause){
				stopScanning();
				shouldScanPuzzlets = true;
			} else {
				if(Connected)
				{
					Debug.Log("App resume & connected, resume scanning");
					startScanning();
				}
				else
				{
					Debug.Log("App resume & disconnected, connect");
					puzzletInterop.Connect();
				}
			}
		}

		void OnDestroy() {
			if(this.enabled){
				Debug.Log("disconnect on close");
				puzzletInterop.Disconnect();
			}
		}

		void Interacted()
		{
			timeSinceInteraction = 0;
			if(resumeScanOnInteraction)
			{
				resumeScanOnInteraction = false;
				startScanning();
			}
		}

			/*enum DataCommandType
		{
			none,
			pocket,
			row
		};
		DataCommandType lastDataCommandType = DataCommandType.none;

		void ResetScanOnDataCommandTypeChange(DataCommandType type)
		{
			Debug.Log("ResetScanOnDataCommandTypeChange");
			if(lastDataCommandType == DataCommandType.none)
			{
				lastDataCommandType = type;
				return;
			}

			if(type != lastDataCommandType)
				resetScanning();

			lastDataCommandType = type;
		}*/
	}
}
