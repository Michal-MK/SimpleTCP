using System;

namespace Igor.TCP {

	/// <summary>
	/// Class representing a single packet definition
	/// </summary>
	internal class CustomPacket {
		/// <summary>
		/// The ID of the packet
		/// </summary>
		internal byte packetID { get; }

		/// <summary>
		/// Type of the data this packet carries
		/// </summary>
		internal Type dataType { get; }

		/// <summary>
		/// Callback action to receive the selected object
		/// </summary>
		internal Action<object,byte> action { get; }

		/// <summary>
		/// Default Constructor
		/// </summary>
		internal CustomPacket(byte packetID, Type dataType, Action<object,byte> action) {
			this.packetID = packetID;
			this.dataType = dataType;
			this.action = action;
		}
	}
}
