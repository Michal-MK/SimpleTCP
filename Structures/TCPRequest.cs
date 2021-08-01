using System;

namespace SimpleTCP.Structures {
	[Serializable]
	internal class TCPRequest {
		internal TCPRequest(byte packetID) {
			PacketID = packetID;
		}

		internal byte PacketID { get; set; }
	}
}