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
				throw new Exception(string.Format("Byte {0} is not a valid Request identifier, Call 'DefineRequestResponseID<TData>(byte, Func<TData>)' to set it + its response datatype", ID));
			}
			return await Task.Run(delegate () {
				byte request = ID;
				connection.SendData(DataIDs.RequestReceptionID, request/*ms.ToArray()*/);
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
