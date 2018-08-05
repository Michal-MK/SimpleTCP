using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Igor.TCP {
	public class TCPRequest {

		private TCPConnection connection;
		private object currentRequestObject;

		ManualResetEventSlim evnt = new ManualResetEventSlim();

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
			evnt.Reset();
			return await Task.Run(delegate () {
				byte[] idBytes = BitConverter.GetBytes(ID);
				connection.SendData(ID, idBytes);
				connection.OnRequestAnswered += Connection_OnRequestAnswered;
				evnt.Wait();
				return (T)currentRequestObject;
			});
		}

		private void Connection_OnRequestAnswered(object sender, object e) {
			currentRequestObject = e;
			evnt.Set();
		}
	}
}