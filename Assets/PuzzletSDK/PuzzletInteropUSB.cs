using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Puzzlets {
	#if UNITY_STANDALONE || UNITY_EDITOR
	public class PuzzletConnectionUSB : MonoBehaviour, IPuzzletInterop {

		internal const int Baud = 9600;

		private PuzzletConnection puzzletConnection;

		private string PortName = "";
		private string partialLine = "";

		private ISerialPort serialPort;

		private List<string> badConnections;

		public void Initialize(PuzzletConnection pc){
			puzzletConnection = pc;
			badConnections = new List<string>();
		}
		
		//find and connect to a Play Tray
		public void Connect () {
			PortName = "";
			partialLine = "";
			string[] playTrays = PuzzletUtility.GetPlayTrayPorts();
			Debug.Log ("all play trays");
			foreach(string playTray in playTrays){
				Debug.Log (playTray);
			}
			foreach(string playTray in playTrays){
				if(!badConnections.Contains(playTray)){
					PortName = playTray;
					break;
				}
			}
			if(PortName == ""){
				badConnections.Clear();
				StartCoroutine (RetryConnection ());
				return;
			}
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			serialPort = new WindowsSerial();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
			serialPort = new MacSerial();
#endif
			serialPort.OpenSerial(PortName, Baud);
			puzzletConnection.PuzzletConnected();
		}
		
		//disconnect from the active connection
		public void Disconnect () {
			if(serialPort != null){
				serialPort.CloseSerial();
			}
			serialPort = null;
			badConnections.Add (PortName);
			StopAllCoroutines();
		}
		
		public void SendMessage(byte[] data){
			if(serialPort != null && serialPort.PortOpen()){
				string line = data[0].ToString();
				for(int ii = 1; ii < data.Length; ii++){
					line += string.Format (".{0}",data[ii]);
				}
				bool connectionHealthy = serialPort.WriteLine(line);
				if(!connectionHealthy){
					ConnectionLost();
				}
			}
		}

		void Update () {
			if(serialPort != null && serialPort.PortOpen()){
				char cc;
				uint numRead;
				do{
					bool connectionHealthy = serialPort.ReadChar(out cc, out numRead);
					if(!connectionHealthy){
						ConnectionLost ();
						return;
					}
					if(numRead > 0){
						if(cc == '\r'){
							continue;
						} else if (cc == '\n' || (int)cc == 0) {
							puzzletConnection.HandleMessage(PuzzletUtility.ParseMessage (partialLine));
							partialLine = "";
						} else {
							partialLine += cc;
						}
					}
				} while (numRead > 0);
			}
		}

		private void ConnectionLost(){
			puzzletConnection.PuzzletDisconnected();
			if(serialPort != null){
				serialPort.CloseSerial ();
			}
			serialPort = null;
			badConnections.Add (PortName);
			StartCoroutine(RetryConnection());
		}

		IEnumerator RetryConnection () {
			for(int frame = 0; frame < 30; ++frame)
				yield return null;
			//yield return new WaitForSeconds(0.5f);
			Connect ();
		}
	}
	#endif
}