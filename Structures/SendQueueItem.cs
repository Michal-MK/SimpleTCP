namespace Igor.TCP {
	/// <summary>
	/// Structure to hold all necessary data to correctly send a packet to the other side
	/// </summary>
	internal struct SendQueueItem {

		/// <summary>
		/// The ID to send this packet to
		/// </summary>
		internal byte packetID;

		/// <summary>
		/// The original sender of the packet
		/// </summary>
		internal byte originClientID;

		/// <summary>
		/// The actual data to send
		/// </summary>
		internal byte[] rawData;

		/// <summary>
		/// Default constructor
		/// </summary>
		internal SendQueueItem(byte packetID, byte originClientID, byte[] rawData) {
			this.packetID = packetID;
			this.originClientID = originClientID;
			this.rawData = rawData;
		}
	}
}
