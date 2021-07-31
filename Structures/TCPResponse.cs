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
		internal TCPResponse(byte packetID, byte[] rawData, Type type) {
			PacketID = packetID;
			RawData = rawData;
			DataType = type;
		}
		
		/// <summary>
		/// Create new response for 'packetID' with no data
		/// </summary>
		internal TCPResponse(byte packetID, Type type) {
			PacketID = packetID;
			RawData = new byte[0];
			DataType = type;
		}

		/// <summary>
		/// Raw byte[] data from the other side, holds requested information
		/// </summary>
		public byte[] RawData { get; }

		/// <summary>
		/// Represents user defined ID for data reception
		/// </summary>
		public byte PacketID { get; }

		/// <summary>
		/// The type of data this packet carries
		/// </summary>
		public Type DataType { get; set; }

		/// <summary>
		/// Attempts conversion to requested type, will fail if the object's name-space differs between Assemblies!
		/// <para>It is advised to create your own byte[] to object converter</para>
		/// </summary>
		public object GetObject => SimpleTCPHelper.GetObject(DataType, RawData);
	}
}
