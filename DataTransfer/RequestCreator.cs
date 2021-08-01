using System;
using System.Threading;
using System.Threading.Tasks;
using SimpleTCP.Connections;
using SimpleTCP.Structures;

namespace SimpleTCP.DataTransfer {
	internal class RequestCreator : IDisposable {
		private readonly TCPConnection conn;
		private readonly ManualResetEventSlim evnt = new();

		private TCPResponse? currentResponseObject;

		internal RequestCreator(TCPConnection connection) {
			conn = connection;
		}

		internal async Task<TCPResponse> Request(byte id, Type type) {
			evnt.Reset();
			return await Task.Run(() => {
				conn.SendData(DataIDs.REQUEST_RECEPTION_ID, id);
				conn.OnResponse += Connection_OnResponse;
				evnt.Wait();
				currentResponseObject!.DataType = type;
				return currentResponseObject;
			});
		}

		private void Connection_OnResponse(object sender, TCPResponse e) {
			currentResponseObject = e;
			evnt.Set();
		}

		#region IDisposable Support

		private bool disposedValue;

		public void Dispose() {
			if (disposedValue) return;

			evnt.Dispose();
			disposedValue = true;
		}

		#endregion
	}
}