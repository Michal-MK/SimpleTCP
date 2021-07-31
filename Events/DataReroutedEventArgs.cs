using System;

namespace Igor.TCP {
	/// <summary>
	/// Basic rerouting information
	/// </summary>
	public class DataReroutedEventArgs : EventArgs {

		internal DataReroutedEventArgs(byte forwardedClient, byte originClient, byte packetID, byte[] data) {
			ForwardedClient = forwardedClient;
			OriginClient = originClient;
			Data = data;
			PacketID = packetID;
		}

		/// <summary>
		/// The client that will receive this packet
		/// </summary>
		public byte ForwardedClient { get; }

		/// <summary>
		/// The client that sent this packet
		/// </summary>
		public byte OriginClient { get; }

		/// <summary>
		/// The raw data that were rerouted
		/// </summary>
		internal byte[] Data { get; }

		/// <summary>
		/// ID of the packet
		/// </summary>
		public byte PacketID { get; }
	}
}