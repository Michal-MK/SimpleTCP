using System;

namespace Igor.TCP {
	[Serializable]
	internal class TCPRequest {
		internal TCPRequest(byte packetID) {
			this.packetID = packetID;
		}

		internal byte packetID { get; set; }
	}
}