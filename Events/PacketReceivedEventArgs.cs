using System;

namespace Igor.TCP {
	/// <summary>
	/// Predefined packets data holder
	/// </summary>
	public class PacketReceivedEventArgs<TPacket> : EventArgs {
		/// <summary>
		/// Received data
		/// </summary>
		public TPacket Data { get; }

		/// <summary>
		/// Packet origin
		/// </summary>
		public byte ClientID { get; }

		internal PacketReceivedEventArgs(TPacket data, byte clientID) {
			this.Data = data;
			this.ClientID = clientID;
		}
	}
}
