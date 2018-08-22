using System;

namespace Igor.TCP {
	internal class DataReroutedEventArgs : EventArgs {

		internal DataReroutedEventArgs(byte forwardedClient, byte universalID, byte[] data, bool isUserDefined) {
			this.forwardedClient = forwardedClient;
			this.data = data;
			this.universalID = universalID;
			this.isUserDefined = isUserDefined;
		}

		internal byte forwardedClient { get; }
		internal byte[] data { get; }
		internal byte universalID { get; }
		internal bool isUserDefined { get; }
	}
}