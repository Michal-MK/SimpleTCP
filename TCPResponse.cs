using System;

namespace Igor.TCP {

	[Serializable]
	public class TCPResponse : IResponse {

		public TCPResponse(/*byte packetType,*/ byte packetID, byte[] rawData/*, Type dataType, TCPClientInfo info*/) {
			//this.packetType = packetType;
			this.packetID = packetID;
			this.rawData = rawData;
			//this.info = info;
			//this.dataType = dataType;
		}

		public TCPResponse(/*byte packetType, */byte packetID/*, Type dataType, TCPClientInfo info*/) {
			//this.packetType = packetType;
			this.packetID = packetID;
			this.rawData = null;
			//this.info = info;
			//this.dataType = dataType;
		}

		public byte[] rawData { get; internal set; }



		//public TCPClientInfo info { get; internal set;}

		//public Type dataType { get; internal set; }

		///// <summary>
		///// Represent a TCPResponse type packet (255B)
		///// </summary>
		//public byte packetType { get; internal set; }

		/// <summary>
		/// Represents userdefined ID for data reception
		/// </summary>
		public byte packetID { get; internal set; }
	}

	public interface IResponse {
		byte[] rawData { get; }
		//Type dataType { get; }
		//byte packetType { get; }
		byte packetID { get; }
		//TCPClientInfo info { get; }
	}
}
