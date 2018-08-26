using System;

namespace Igor.TCP {
	/// <summary>
	/// Class handing requests from the other side
	/// </summary>
	public class ResponseManager {

		internal DataIDs dataIDs;

		internal ResponseManager(DataIDs dataIDs) {
			this.dataIDs = dataIDs;
		}

		internal void HandleRequest(TCPRequest request, object obj) {
			byte[] rawData = Helper.GetBytesFromObject(obj);
			byte[] data = new byte[rawData.Length + DataIDs.PACKET_ID_COMPLEXITY];
			data[0] = request.packetID;
			rawData.CopyTo(data, DataIDs.PACKET_ID_COMPLEXITY);

			dataIDs.connection.SendData(DataIDs.ResponseReceptionID, data);
		}

		internal void HandleRequest(TCPRequest request, byte[] data_ready) {
			byte[] data = new byte[data_ready.Length + DataIDs.PACKET_ID_COMPLEXITY];
			data[0] = request.packetID;
			data_ready.CopyTo(data, 1);

			dataIDs.connection.SendData(DataIDs.ResponseReceptionID, data);
		}

		/// <summary>
		/// Get response type of custom defined packet 'ID'
		/// </summary>
		public Type GetResponseType(byte ID) {
			return dataIDs.requestDict[ID];
		}
	}
}
