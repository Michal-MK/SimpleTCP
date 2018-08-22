namespace Igor.TCP {
	internal class ReroutingInfo {
		internal ReroutingInfo(byte from, byte to) {
			fromClient = from;
			toClient = to;
		}


		internal void SetPacketInfo(byte packetID) {
			this.packetID = packetID;
		}

		internal void SetPacketInfoUserDefined(byte dataID) {
			this.dataID = dataID;
			this.packetID = DataIDs.UserDefined;
			isUserDefined = true;
		}

		internal byte fromClient;
		internal byte toClient;
		internal byte packetID;
		internal byte dataID;
		internal bool isUserDefined;
	}
}