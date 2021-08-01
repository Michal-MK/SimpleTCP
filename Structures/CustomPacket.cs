using System;

namespace SimpleTCP.Structures {

	/// <summary>
	/// Class representing a single packet definition
	/// </summary>
	internal class CustomPacket {
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		internal CustomPacket(byte packetID, Type dataType, Action<byte, object> action) {
			PacketID = packetID;
			DataType = dataType;
			ActionCallback = action;
		}

		/// <summary>
		/// The ID of the packet
		/// </summary>
		internal byte PacketID { get; }

		/// <summary>
		/// Type of the data this packet carries
		/// </summary>
		internal Type DataType { get; }

		/// <summary>
		/// Callback action to receive the selected object
		/// </summary>
		internal Action<byte, object> ActionCallback { get; }
	}
}
