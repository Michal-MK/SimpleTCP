using System;
using System.Threading;
using System.Threading.Tasks;

namespace Igor.TCP {
	internal class RequestCreator : IDisposable {
		internal TCPConnection connection;
		internal ManualResetEventSlim evnt = new ManualResetEventSlim();

		internal RequestCreator(TCPConnection connection) {
			this.connection = connection;
		}

		private TCPResponse currentResponseObject;

		internal async Task<TCPResponse> Request(byte ID, Type type) {
			evnt.Reset();
			return await Task.Run(delegate () {
				byte request = ID;
				connection.SendData(DataIDs.REQUEST_RECEPTION_ID, request);
				connection.OnResponse += Connection_OnResponse;
				evnt.Wait();
				currentResponseObject.DataType = type;
				return currentResponseObject;
			});
		}

		private void Connection_OnResponse(object sender, TCPResponse e) {
			currentResponseObject = e;
			evnt.Set();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				evnt.Dispose();
				disposedValue = true;
			}
		}

		~RequestCreator() {
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
