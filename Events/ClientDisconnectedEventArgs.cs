using System;
using Igor.TCP.Enums;

namespace Igor.TCP {
	/// <summary>
	/// Client Disconnected event data
	/// </summary>
	public class ClientDisconnectedEventArgs: EventArgs {

		internal ClientDisconnectedEventArgs(byte clientID, DisconnectType disconnectType) {
			this.clientID = clientID;
			this.disconnectType = disconnectType;
		}

		/// <summary>
		/// ID of the disconnected client
		/// </summary>
		public byte clientID { get; }

		/// <summary>
		/// The way client disconnected
		/// </summary>
		public DisconnectType disconnectType { get; }
	}

}