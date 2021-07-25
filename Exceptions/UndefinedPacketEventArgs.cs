using System;

namespace Igor.TCP {
	/// <summary>
	/// Event data for undefined packet ids event
	/// </summary>
	public class UndefinedPacketEventArgs : EventArgs {
		internal UndefinedPacketEventArgs(byte senderID, byte packetID, byte[] data) {
			UnknownData = data;
			PacketID = packetID;
			SenderID = senderID;
		}

		/// <summary>
		/// Type of the packet, null if can not be determined
		/// </summary>
		public byte[] UnknownData { get; }

		/// <summary>
		/// ID of the packet
		/// </summary>
		public byte PacketID { get; }
		
		/// <summary>
		/// ID of the sender
		/// </summary>
		public byte SenderID { get; }
	}
}
