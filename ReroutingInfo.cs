namespace Igor.TCP {

	/// <summary>
	/// Class holding data necessary for rerouting packets from client to client
	/// </summary>
	internal class ReroutingInfo {
		internal ReroutingInfo(byte from, byte to) {
			fromClient = from;
			toClient = to;
		}

		internal void SetPacketInfo(byte packetID) {
			this.packetID = packetID;
		}

		/// <summary>
		/// ID this packet originated from
		/// </summary>
		internal byte fromClient;

		/// <summary>
		/// ID this packet is meant for
		/// </summary>
		internal byte toClient;

		/// <summary>
		/// ID of the packet
		/// </summary>
		internal byte packetID;

		/// <summary>
		/// ID if the carried data
		/// </summary>
		internal byte dataID;

		/// <summary>
		/// Was this data user defined
		/// </summary>
		internal bool isUserDefined;
	}
}