using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	public class ResponseManager {

		internal DataIDs dataIDs;

		internal ResponseManager(DataIDs dataIDs) {
			this.dataIDs = dataIDs;
		}

		internal void HandleRequest(TCPRequest request, object obj) {
			BinaryFormatter bf = new BinaryFormatter();
			using (MemoryStream internalMS = new MemoryStream()) {
				bf.Serialize(internalMS, obj);
				byte[] rawData = internalMS.ToArray();
				byte[] data = new byte[rawData.Length + DataIDs.PACKET_ID_COMPLEXITY];
				data[0] = request.packetID;
				rawData.CopyTo(data, 1);

				dataIDs.connection.SendData(DataIDs.ResponseReceptionID, data);
			}
		}

		/// <summary>
		/// Get response type of custom defined packet 'ID'
		/// </summary>
		public Type GetResponseType(byte ID) {
			return dataIDs.requestDict[ID];
		}
	}
}
