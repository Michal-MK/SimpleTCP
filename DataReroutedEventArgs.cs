using System;

namespace Igor.TCP {
	internal class DataReroutedEventArgs : EventArgs {

		internal DataReroutedEventArgs(byte forwardedClient, byte packetID, byte[] data) {
			this.forwardedClient = forwardedClient;
			this.data = data;
			this.packetID = packetID;
		}

		internal byte forwardedClient { get; }
		internal byte[] data { get; }
		internal byte packetID { get; }
	}
}