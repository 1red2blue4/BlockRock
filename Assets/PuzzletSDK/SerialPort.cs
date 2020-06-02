using UnityEngine;
using System.Collections;

namespace Puzzlets {

	public interface ISerialPort {
		bool OpenSerial(string portName, int baudRate);
		bool CloseSerial();
		bool ReadChar(out char cc, out uint numRead);
		bool WriteLine(string line);
		bool PortOpen();
	}
}

