using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Puzzlets {

	public enum MessageHeaders {
		DATA_COMMAND = 0x10,
		HANDSHAKE = 0xFF,
		SIMPLE_COMMAND = 0x03
	}
	
	public enum SimpleCommands {
		SCAN_PING = 0x04,
		CONNECTED_PING = 0x05,
		START_SCAN = 0x06,
		STOP_SCAN = 0x07,
		GAME_CLOSE = 0x08,
		COMMUNICATION_ERROR = 0x00
	}
	
	public enum DataCommands {
		POCKET_DATA = 0x11,
		ROW_DATA = 0x12
	}

	public static class PuzzletUtility {

#if UNITY_IOS || UNITY_ANDROID
		public static int[,] MinimumFirmwareVersion = new int[2,3] {{1,6,0},{1,6,0}};
#elif UNITY_STANDALONE || UNITY_EDITOR
		public static int[,] MinimumFirmwareVersion = new int[2,3] {{1,6,0},{1,6,0}};
#endif

		public static byte[] ParseMessage(string message){
			try{
				string[] tokens = message.Split('.');
				byte[] numericMessage = new byte[tokens.Length];
				for(int ii = 0; ii < tokens.Length; ii++){
					numericMessage[ii] = Byte.Parse(tokens[ii]);
				}
				return numericMessage;
			} catch (Exception e){
				Debug.LogWarning (e.Message);
				Debug.LogWarning (message);
				return new byte[2] {(byte)MessageHeaders.SIMPLE_COMMAND,(byte)SimpleCommands.COMMUNICATION_ERROR};
			}
		}

		public static bool CheckFirmwareVersion(){
			int hwv = PuzzletConnection.HardwareVersion;
			for(int ii = 0; ii < 3; ii++){
				if(PuzzletConnection.FirmwareDigits[ii] > MinimumFirmwareVersion[hwv,ii]){
					return true;
				} else if(PuzzletConnection.FirmwareDigits[ii] < MinimumFirmwareVersion[hwv,ii]){
					return false;
				}
			}
			return true;
		}

		public static bool CheckFirmwareVersion(int[] minVersion){
			for(int ii = 0; ii < 3; ii++){
				if(PuzzletConnection.FirmwareDigits[ii] > minVersion[ii]){
					return true;
				} else if(PuzzletConnection.FirmwareDigits[ii] < minVersion[ii]){
					return false;
				}
			}
			return true;
		}

		public static string FirmwareString(int[] firmwareDigits)
		{
			return string.Format("{0}.{1}.{2}.{3}",firmwareDigits[0],
				firmwareDigits[1],
				firmwareDigits[2],
				PuzzletConnection.HardwareVersion);
		}

		public static string RequiredFirmwareString(){
			return string.Format("{0}.{1}.{2}.{3}",MinimumFirmwareVersion[PuzzletConnection.HardwareVersion,0],
			                     MinimumFirmwareVersion[PuzzletConnection.HardwareVersion,1],
			                     MinimumFirmwareVersion[PuzzletConnection.HardwareVersion,2],
			                     PuzzletConnection.HardwareVersion);
		}

		public static string FirmwareString(){
			return string.Format("{0}.{1}.{2}.{3}",PuzzletConnection.FirmwareDigits[0],
			                     									 PuzzletConnection.FirmwareDigits[1],
			                                       PuzzletConnection.FirmwareDigits[2],
			                                       PuzzletConnection.HardwareVersion);
		}
			
#if UNITY_STANDALONE || UNITY_EDITOR
		public static string[] GetPlayTrayPorts()
		{		
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
			string[] devices = System.IO.Ports.SerialPort.GetPortNames();
			
			//print out all the port names
			for(int ii = 0; ii < devices.Length; ii++){
				//add '\\.\ to the front of the com name so we can connecto to com10 and higher
				devices[ii] = "\\\\.\\" + devices[ii];
				Debug.Log (devices[ii]);
			}
			return devices;
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
			string[] rawDevices = System.IO.Ports.SerialPort.GetPortNames();
			if (rawDevices.Length == 0){
				rawDevices = System.IO.Directory.GetFiles("/dev/");		
			}
			List<string> devices = new List<string>();
			foreach(string dev in rawDevices){
				if(dev.StartsWith("/dev/tty.usb") || dev.StartsWith("/dev/ttyUSB")){
					devices.Add(dev);
				}
			}
			return devices.ToArray();
#endif
		}
#endif
	}
}