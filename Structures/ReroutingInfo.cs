namespace SimpleTCP.Structures {

	/// <summary>
	/// Class holding data necessary for rerouting packets from client to client
	/// </summary>
	internal class ReroutingInfo {
		
		internal ReroutingInfo(byte toClient, byte packetID) {
			ToClient = toClient;
			PacketID = packetID;
		}

		/// <summary>
		/// ID this packet is meant for
		/// </summary>
		internal byte ToClient { get; }

		/// <summary>
		/// ID of the packet
		/// </summary>
		internal byte PacketID { get; }
	}
}