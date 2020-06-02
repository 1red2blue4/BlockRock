using UnityEngine;
using System.Collections;

namespace Puzzlets {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
	public class MacSerial : ISerialPort {
		private System.IO.Ports.SerialPort serialPort;

		public bool OpenSerial(string portName, int baudRate) {
			serialPort = new System.IO.Ports.SerialPort(portName, baudRate);
			serialPort.PortName = portName;
			serialPort.BaudRate = baudRate;
			serialPort.DataBits = 8;
			serialPort.Parity = System.IO.Ports.Parity.None;
			serialPort.StopBits = System.IO.Ports.StopBits.One;
			serialPort.ReadTimeout = 1; // since on windows we *cannot* have a separate read thread
			serialPort.WriteTimeout = 1000;
			try
			{serialPort.Open();}
			catch(System.Exception e)
			{
				Debug.Log(e.Message);
				return false;
			}
			return serialPort.IsOpen;
		}
		
		public bool CloseSerial() {
			serialPort.Close();
			return true;
		}

		public bool ReadChar(out char cc, out uint numRead) {
			if(serialPort.BytesToRead == 0){
				cc = '\n';
				numRead = 0;
				return true;
			}
			cc = (char)serialPort.ReadChar ();
			if(cc == (char)0xffff){
				numRead = 0;
				return false;
			}
			numRead = 1;
			return true;
		}

		public bool WriteLine(string line) {
			line += "\r";
			try{
				serialPort.Write(line);
			} catch (System.Exception e){
				Debug.LogWarning(e.Message);
				return false;
			}
			return true;
		}

		public bool PortOpen() {
			return serialPort.IsOpen;
		}
	}
#endif
}
