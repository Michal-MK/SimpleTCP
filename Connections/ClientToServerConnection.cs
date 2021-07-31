using System;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Client to server connection holder, processes higher level data
	/// </summary>
	public sealed class ClientToServerConnection : TCPConnection {
		internal event EventHandler _OnClientKickedFromServer;

		private readonly TCPClient client;

		internal ClientToServerConnection(TcpClient server, TCPClientInfo serverInfo, TCPClient client, TCPClientInfo myInfo, SerializationConfiguration config)
			: base(server, myInfo, serverInfo, client, config) {
			this.client = client;
		}

		/// <summary>
		/// Disconnect and let server know that this client is disconnecting
		/// </summary>
		public void DisconnectFromServer(byte myID) {
			SendDataImmediate(DataIDs.CLIENT_DISCONNECTED, SimpleTCPHelper.GetBytesFromObject(myInfo, client.Configuration));
			Dispose();
		}

		/// <summary>
		/// Handle higher level data that only a Client can react to
		/// </summary>
		protected override void HigherLevelDataReceived(ReceivedData data) {
			if (data.DataID == DataIDs.CLIENT_DISCONNECTED) {
				_OnClientKickedFromServer?.Invoke(client, EventArgs.Empty);
			}
			if (data.DataType == typeof(OnPropertySynchronizationEventArgs)) {
				byte key = ((byte[])data.ReceivedObject)[0];
				PropertySynchronization sync = client.Connection.dataIDs.syncedProperties[key];
				client.InvokeOnPropertySync(this, new OnPropertySynchronizationEventArgs(key, sync));
			}
		}
	}
}