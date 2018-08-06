using System;

namespace Igor.TCP {
	[Serializable]
	public class TCPRequest : IRequest {
		public TCPRequest(byte packetID/*, TCPClientInfo info*/) {
			this.packetID = packetID;
			//this.info = info;
		}

		public byte packetID { get; internal set; }

		//public TCPClientInfo info { get; internal set; }
	}

	public interface IRequest {
		byte packetID { get; }
		//TCPClientInfo info { get; }
	}
}