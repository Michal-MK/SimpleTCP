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
			Sender = sender;
			DisconnectedClientID = disconnectedClientID;
			DisconnectType = disconnectType;
		}

		public byte Sender { get; }

		public byte DisconnectedClientID { get; }

		public DisconnectType DisconnectType { get; }
	}
}
