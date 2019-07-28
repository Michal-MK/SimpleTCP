using System;

namespace Igor.TCP {
	/// <summary>
	/// Exception raised when receiving data via an undefined packet ID
	/// </summary>
	[Serializable]
	public class UndefinedPacketException : Exception {
		internal UndefinedPacketException(string message, byte packetID, Type dataType) : base(message) {
			UndefinedType = dataType;
			PacketID = packetID;
		}

		/// <summary>
		/// Type of the packet, null if can not be determined
		/// </summary>
		public Type UndefinedType { get; }

		/// <summary>
		/// ID of the packet
		/// </summary>
		public byte PacketID { get; }
	}
}
