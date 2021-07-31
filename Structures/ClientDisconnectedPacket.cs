using Igor.TCP.Enums;
using System;

namespace Igor.TCP {

	[Serializable]
	internal struct ClientDisconnectedPacket {

		internal ClientDisconnectedPacket(byte sender, byte disconnectedClientID, DisconnectType disconnectType) {
			Sender = sender;
			DisconnectedClientID = disconnectedClientID;
			DisconnectType = disconnectType;
		}

		internal byte Sender { get; }

		internal byte DisconnectedClientID { get; }

		internal DisconnectType DisconnectType { get; }
	}
}
