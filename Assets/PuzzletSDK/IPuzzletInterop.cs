using UnityEngine;
using System.Collections;

namespace Puzzlets {
	public interface IPuzzletInterop {
		void Initialize(PuzzletConnection puzzletConnection);
		void Connect();
		void Disconnect();
		void SendMessage(byte[] data);
	}
}
