using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Puzzlets {
	#if UNITY_IOS
	public class PuzzletConnectionIOS : MonoBehaviour, IPuzzletInterop {

		private PuzzletConnection puzzletConnection;

		//extrenal functions for managing the bluetooth connection
		[DllImport ("__Internal")]
		private static extern void _setupConnection ();
		
		[DllImport ("__Internal")]
		private static extern void _setReceiver (string ss);
		
		[DllImport ("__Internal")]
		private static extern void _disconnect ();
		
		[DllImport ("__Internal")]
		private static extern void _connect ();
		
		[DllImport ("__Internal")]
		private static extern void _sendPacket (IntPtr data, int length);
		
		[DllImport ("__Internal")]
		private static extern void _checkBluetoothStatus ();

		public void Initialize(PuzzletConnection pc){
			puzzletConnection = pc;
			_setupConnection();
			_setReceiver(name);
		}
		
		//find and connect to a Play Tray
		public void Connect () {
			_connect();
		}
		
		//disconnect from the active connection
		public void Disconnect () {
			_disconnect();
		}

		public void SendMessage(byte[] data){
			byte[] sanitizedData;
			if(data.Length < 3){
				sanitizedData = new byte[3];
				data.CopyTo(sanitizedData, 0);
			} else {
				sanitizedData = data;
			}
			IntPtr dataPtr = Marshal.AllocHGlobal(sanitizedData.Length);
			Marshal.Copy(sanitizedData, 0, dataPtr, sanitizedData.Length);
			_sendPacket (dataPtr, sanitizedData.Length);
		}

		public void PuzzletReceiveMessageIOS(string message){
			puzzletConnection.HandleMessage(PuzzletUtility.ParseMessage(message));
		}

		public void PuzzletConnectedIOS(){
			puzzletConnection.PuzzletConnected();
		}

		public void PuzzletDisconnectedIOS(){
			puzzletConnection.PuzzletDisconnected();
		}

		public void PuzzletSeveralTooFarIOS(){
			ConnectionStatusDisplay.TooFar(true);
		}

		public void PuzzletSingleTooFarIOS(){
			ConnectionStatusDisplay.TooFar(false);
		}
	}
	#endif
}