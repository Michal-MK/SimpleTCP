using System;

namespace Igor.TCP {
	/// <summary>
	/// Predefined packets data holder
	/// </summary>
	public class PacketReceivedEventArgs<TPacket> : EventArgs {
		/// <summary>
		/// Received data
		/// </summary>
		public TPacket data { get; }

		/// <summary>
		/// Packet origin
		/// </summary>
		public byte clientID { get; }

		internal PacketReceivedEventArgs(TPacket data, byte clientID){
			this.data = data;
			this.clientID = clientID;
		}
	}
}
