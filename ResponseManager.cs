using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	public class ResponseManager {

		internal DataIDs dataIDs;

		internal Dictionary<byte, Type> dataTypeForID = new Dictionary<byte, Type>();
		//byte[] verifiedConnections = new byte[255];

		internal ResponseManager(DataIDs dataIDs) {
			this.dataIDs = dataIDs;
		}

		internal void AddType(byte ID, Type t) {
			dataTypeForID.Add(ID, t);
		}

		internal void RemoveType(byte ID) {
			dataTypeForID.Remove(ID);
		}

		internal TCPResponse GetResponse(byte requestID) {
			//return new TCPResponse(DataIDs.ResponseReceptionID, requestID, dataTypeForID[requestID], GetInfo());
			return new TCPResponse(requestID);
		}

		internal TCPClientInfo GetInfo() {
			TCPClientInfo info = new TCPClientInfo(dataIDs.connection.isServer, Helper.GetActivePIv4Address());
			return info;
		}

		internal void HandleRequest(TCPRequest request, object obj) {
			BinaryFormatter bf = new BinaryFormatter();
			//if (verifiedConnections[request.packetID] == 0) {
			using (MemoryStream internalMS = new MemoryStream()) {
				bf.Serialize(internalMS, obj);
				byte[] rawData = internalMS.ToArray();
				byte[] data = new byte[rawData.Length + DataIDs.PACKET_ID_COMPLEXITY];
				data[0] = request.packetID;
				rawData.CopyTo(data, 1);

				dataIDs.connection.SendData(DataIDs.ResponseReceptionID, data);
				//verifiedConnections[resp.packetID] = 1;
			}
			//}
			//else {
			//	using (MemoryStream internalMS = new MemoryStream()) {
			//		bf.Serialize(internalMS, obj);
			//		byte[] data = internalMS.ToArray();
			//		byte[] trimmed = new byte[data.Length - 1];
			//		Array.Copy(data, 1, trimmed, 0, data.Length - 1);

			//		dataIDs.connection.SendData(DataIDs.ResponseReceptionID, request.packetID, trimmed);
			//	}
			//}

		}

		public Type GetResponseType(byte ID) {
			return dataIDs.requestDict[ID];
		}
	}
}
