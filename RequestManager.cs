using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace Igor.TCP {
	public class RequestManager {
		internal TCPConnection connection;
		internal ManualResetEventSlim evnt = new ManualResetEventSlim();

		//Dictionary<Tuple<IPAddress, byte>, bool> verifiedConnections = new Dictionary<Tuple<IPAddress, byte>, bool>();
		//byte[] verifiedConnections = new byte[255];

		internal RequestManager(TCPConnection connection) {
			this.connection = connection;
		}

		private TCPResponse currentResponseObject;
		internal async Task<TCPResponse> Request(byte ID) {
			evnt.Reset();
			if (!connection.dataIDs.idDict.ContainsKey(ID)) {
				throw new Exception(string.Format("Byte {0} is not a valid Request identifier, Call 'DefineRequestResponseID<TData>(byte, Func<TData>)' to set it + its response datatype", ID));
			}
			return await Task.Run(delegate () {
				//if(verifiedConnections[ID] == 0) {
					TCPRequest request = new TCPRequest(ID/*, new TCPClientInfo(connection.isServer, Helper.GetActivePIv4Address())*/);
					using (MemoryStream ms = new MemoryStream()) {
						BinaryFormatter bf = new BinaryFormatter();
						bf.Serialize(ms, request);
						connection.SendData(DataIDs.RequestReceptionID, ms.ToArray());
						connection._OnResponse += Connection_OnResponse;
						evnt.Wait();
						//verifiedConnections[currentResponseObject.packetID] = 1;
						return currentResponseObject;
					}
				//}
				//else {
				//	connection.SendData(DataIDs.RequestReceptionID, ID);
				//	connection._OnResponse += Connection_OnResponse;
				//	evnt.Wait();
				//	return currentResponseObject;
				//}
			});
		}

		private void Connection_OnResponse(object sender, TCPResponse e) {
			currentResponseObject = e;
			evnt.Set();
		}
	}
}
