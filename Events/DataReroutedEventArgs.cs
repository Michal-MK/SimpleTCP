using System;

namespace Igor.TCP {
	/// <summary>
	/// Basic rerouting information
	/// </summary>
	public class DataReroutedEventArgs : EventArgs {

		internal DataReroutedEventArgs(byte forwardedClient, byte universalID, byte[] data, bool isUserDefined) {
			this.forwardedClient = forwardedClient;
			this.data = data;
			this.universalID = universalID;
			this.isUserDefined = isUserDefined;
		}

		/// <summary>
		/// The client that will receive this packet
		/// </summary>
		public byte forwardedClient { get; }

		internal byte[] data { get; }

		/// <summary>
		/// PacketID, if 'isUserDefined' is true, this filed holds the DataID under <see cref="DataIDs.UserDefined"/>
		/// </summary>
		public byte universalID { get; }

		/// <summary>
		/// Is this data defined by user
		/// </summary>
		public bool isUserDefined { get; }
	}
}