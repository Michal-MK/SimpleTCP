using System;

namespace Igor.TCP {

	/// <summary>
	/// Class containing response data to raised request
	/// </summary>
	[Serializable]
	public class TCPResponse {
		/// <summary>
		/// Create new response for 'packetID' containing 'rawData'
		/// </summary>
		public TCPResponse(byte packetID, byte[] rawData) {
			this.packetID = packetID;
			this.rawData = rawData;
		}
		/// <summary>
		/// Create new response for 'packetID' with no  data
		/// </summary>
		public TCPResponse(byte packetID) {
			this.packetID = packetID;
			this.rawData = null;
		}

		/// <summary>
		/// Received bytes from the other side
		/// </summary>
		public byte[] rawData { get; internal set; }

		/// <summary>
		/// Represents user defined ID for data reception
		/// </summary>
		public byte packetID { get; internal set; }
	}
}
