using System;

namespace Igor.TCP {
	/// <summary>
	/// Class handing requests from the other side
	/// </summary>
	public class RequestHandler {

		internal readonly TCPConnection connection;

		internal RequestHandler(TCPConnection connection) {
			this.connection = connection;
		}

		internal void HandleRequest(TCPRequest request, object obj) {
			byte[] rawData = SimpleTCPHelper.GetBytesFromObject(obj);
			HandleRequest(request, rawData);
		}

		internal void HandleRequest(TCPRequest request, byte[] dataReady) {
			byte[] data = new byte[dataReady.Length + DataIDs.PACKET_ID_COMPLEXITY];
			data[0] = request.PacketID;
			dataReady.CopyTo(data, DataIDs.PACKET_ID_COMPLEXITY);

			connection.SendData(DataIDs.RESPONSE_RECEPTION_ID, connection.myInfo.ID, data);
		}

		/// <summary>
		/// Get response type of custom defined packet 'ID'
		/// </summary>
		public Type GetResponseType(byte id) {
			return connection.dataIDs.requestTypeMap[id];
		}
	}
}