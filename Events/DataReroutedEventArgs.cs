using System;

namespace Igor.TCP {
	/// <summary>
	/// Basic rerouting information
	/// </summary>
	public class DataReroutedEventArgs : EventArgs {

		internal DataReroutedEventArgs(byte forwardedClient, byte originClient, byte packetID, byte[] data) {
			this.forwardedClient = forwardedClient;
			this.originClient = originClient;
			this.data = data;
			this.packetID = packetID;
		}

		/// <summary>
		/// The client that will receive this packet
		/// </summary>
		public byte forwardedClient { get; }

		/// <summary>
		/// The client sent this packet
		/// </summary>
		public byte originClient { get; }

		internal byte[] data { get; }

		/// <summary>
		/// ID of the packet
		/// </summary>
		public byte packetID { get; }
	}
}