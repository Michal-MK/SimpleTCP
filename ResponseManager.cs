using System;

namespace Igor.TCP {
	/// <summary>
	/// Class handing requests from the other side
	/// </summary>
	public class ResponseManager {

		internal TCPConnection connection;

		internal ResponseManager(TCPConnection connection) {
			this.connection = connection;
		}

		internal void HandleRequest(TCPRequest request, object obj) {
			byte[] rawData = SimpleTCPHelper.GetBytesFromObject(obj);
			HandleRequest(request, rawData);
		}

		internal void HandleRequest(TCPRequest request, byte[] data_ready) {
			byte[] data = new byte[data_ready.Length + DataIDs.PACKET_ID_COMPLEXITY];
			data[0] = request.packetID;
			data_ready.CopyTo(data, DataIDs.PACKET_ID_COMPLEXITY);

			connection._SendData(DataIDs.ResponseReceptionID, connection.myInfo.clientID, data);
		}

		/// <summary>
		/// Get response type of custom defined packet 'ID'
		/// </summary>
		public Type GetResponseType(byte ID) {
			return connection.dataIDs.requestTypeMap[ID];
		}
	}
}
