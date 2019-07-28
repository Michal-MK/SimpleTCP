using System;

namespace Igor.TCP {
	[Serializable]
	internal class TCPRequest {
		internal TCPRequest(byte packetID) {
			PacketID = packetID;
		}

		internal byte PacketID { get; set; }
	}
}