namespace Igor.TCP {

	/// <summary>
	/// Class holding data necessary for rerouting packets from client to client
	/// </summary>
	internal class ReroutingInfo {
		internal ReroutingInfo(byte toClient, byte packetID) {
			this.toClient = toClient;
			this.packetID = packetID;
		}

		/// <summary>
		/// ID this packet is meant for
		/// </summary>
		internal byte toClient;

		/// <summary>
		/// ID of the packet
		/// </summary>
		internal byte packetID;
	}
}