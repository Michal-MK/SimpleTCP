namespace Igor.TCP {
	/// <summary>
	/// Structure to hold all necessary data to correctly send a packet to the other side
	/// </summary>
	internal struct SendQueueItem {

		/// <summary>
		/// The ID to send this packet to
		/// </summary>
		internal byte PacketID { get; }

		/// <summary>
		/// The original sender of the packet
		/// </summary>
		internal byte OriginClientID { get; }

		/// <summary>
		/// The actual data to send
		/// </summary>
		internal byte[] RawData { get; }

		/// <summary>
		/// Default constructor
		/// </summary>
		internal SendQueueItem(byte packetID, byte originClientID, byte[] rawData) {
			PacketID = packetID;
			OriginClientID = originClientID;
			RawData = rawData;
		}
	}
}
