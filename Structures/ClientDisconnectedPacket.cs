using Igor.TCP.Enums;
using System;

namespace Igor.TCP.Enums {
	/// <summary>
	/// Reasons for the disconnect
	/// </summary>
	public enum DisconnectType {
		/// <summary>
		/// Client disconnected successfully
		/// </summary>
		Success,
		/// <summary>
		/// Client dropped connection to the server
		/// </summary>
		Interrupted,
		/// <summary>
		/// Client disconnected by server (kicked)
		/// </summary>
		Kicked
	}
}

namespace Igor.TCP {

	[Serializable]
	internal struct ClientDisconnectedPacket {

		internal ClientDisconnectedPacket(byte sender, byte disconnectedClientID, DisconnectType disconnectType) {
			this.sender = sender;
			this.disconnectedClientID = disconnectedClientID;
			this.disconnectType = disconnectType;
		}

		public byte sender { get; }

		public byte disconnectedClientID { get; }

		public DisconnectType disconnectType { get; }
	}
}
