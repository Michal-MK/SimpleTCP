using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Igor.TCP {
	class TCPRequest {

		private TCPConnection connection;
		private NetworkStream stream;


		public TCPRequest(TCPConnection undrelyingConnection) {
			connection = undrelyingConnection;
			stream = connection.GetStream();
		}

		public void DefineRequest<TData>(byte ID, Func<TData> function) {
			connection.dataIDs.AddNew(ID, function);
		}

		public void Cancelrequest(byte ID) {
			connection.dataIDs.RemoveFunc(ID);
		}

		internal async Task<T> Request<T>(int iD) {
			byte[] idBytes = BitConverter.GetBytes(iD);
			stream.Write(idBytes, 0, idBytes.Length);
			Tuple<Type, object> data = await connection.ReceiveDataAsync();
			return (T)data.Item2;
		}
	}
}