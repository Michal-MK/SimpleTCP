﻿using System;

namespace SimpleTCP.Exceptions {
	/// <summary>
	/// Exception signifying that the packetID is already taken
	/// </summary>
	[Serializable]
	public class PacketIDTakenException : Exception {

		/// <summary>
		/// Default constructor
		/// </summary>
		internal PacketIDTakenException(byte packetID, Type packetDataType, string message) : base(message) {
			PacketID = packetID;
			PacketDataType = packetDataType;
		}

		/// <summary>
		/// The ID that was attempted to register
		/// </summary>
		public byte PacketID { get; set; }

		/// <summary>
		/// The type of data that is already registered
		/// </summary>
		public Type PacketDataType { get; set; }
	}
}
