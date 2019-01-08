using System;

namespace Igor.TCP {

	[Serializable]
	internal struct ClientDisconnectedPacket {

		internal ClientDisconnectedPacket(byte sender, byte disconnectedClientID) {
			this.sender = sender;
			this.disconnectedClientID = disconnectedClientID;
		}

		public byte sender { get; }

		public byte disconnectedClientID { get; }
	}
}
