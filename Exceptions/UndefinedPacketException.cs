using System;

namespace Igor.TCP {
	[Serializable]
	class UndefinedPacketException : Exception {
		public Type undefinedType { get; }
		public byte packetID { get; }

		public UndefinedPacketException(string message, byte packetID, Type dataType) : base(message) {
			undefinedType = dataType;
			this.packetID = packetID;
		}
	}
}
