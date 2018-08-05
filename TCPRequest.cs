using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Igor.TCP {
	public class TCPRequest {

		private TCPConnection connection;


		public TCPRequest(TCPConnection undrelyingConnection) {
			connection = undrelyingConnection;
		}

		public void DefineRequestID<TData>(byte ID, Func<TData> function) {
			connection.dataIDs.AddNew(ID, function);
		}

		public void Cancelrequest(byte ID) {
			connection.dataIDs.RemoveFunc(ID);
		}

		internal async Task<T> Request<T>(byte ID) {
			byte[] idBytes = BitConverter.GetBytes(ID);
			connection.SendData(ID, idBytes);
			Tuple<Type, object> data = await connection.ReceiveDataAsync();
			return (T)data.Item2;
		}
	}
}