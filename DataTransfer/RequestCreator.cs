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

		internal async Task<TCPResponse> Request(byte ID) {
			evnt.Reset();
			if (!connection.dataIDs.requestTypeMap.ContainsKey(ID)) {
				throw new NotImplementedException(string.Format("Byte {0} is not a valid Request identifier, " +
					"Is it not defined yet?", ID));
			}
			return await Task.Run(delegate () {
				byte request = ID;
				connection.SendData(DataIDs.RequestReceptionID, request);
				connection._OnResponse += Connection_OnResponse;
				evnt.Wait();
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
