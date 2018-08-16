using System;
using System.Threading;
using System.Threading.Tasks;

namespace Igor.TCP {
	internal class RequestManager : IDisposable{
		internal TCPConnection connection;
		internal ManualResetEventSlim evnt = new ManualResetEventSlim();

		internal RequestManager(TCPConnection connection) {
			this.connection = connection;
		}

		#region Request raising + reception of responses

		private TCPResponse currentResponseObject;

		internal async Task<TCPResponse> Request(byte ID) {
			evnt.Reset();
			if (!connection.dataIDs.requestDict.ContainsKey(ID)) {
				throw new NotImplementedException(string.Format("Byte {0} is not a valid Request identifier, Call 'DefineRequestEntry<TData>(byte clientID, byte ID)' to set it + its response datatype", ID));
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

		#endregion

		public void Dispose() {
			evnt.Dispose();
		}
	}
}
