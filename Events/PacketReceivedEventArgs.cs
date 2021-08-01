using System;

namespace SimpleTCP.Events {
	/// <summary>
	/// Event data provided when a packet arrives
	/// </summary>
	public class PacketReceivedEventArgs<TPacket> : EventArgs {
		
		internal PacketReceivedEventArgs(TPacket data, byte clientID) {
			Data = data;
			ClientID = clientID;
		}

		/// <summary>
		/// The received data
		/// </summary>
		public TPacket Data { get; }

		/// <summary>
		/// The client that sent this packet
		/// </summary>
		public byte ClientID { get; }
	}
}
