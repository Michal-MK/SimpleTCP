using System;
using System.Net.Sockets;

namespace Igor.TCP {
	/// <summary>
	/// Client to server connection holder, processes higher level data
	/// </summary>
	public sealed class ClientToServerConnection : TCPConnection {
		internal event EventHandler _OnClientKickedFromServer;

		internal TCPClient client;

		internal ClientToServerConnection(TcpClient server, TCPClientInfo myInfo, TCPClientInfo serverInfo, TCPClient client)
			: base(server, myInfo, serverInfo) {
			this.client = client;
		}

		/// <summary>
		/// Disconnect and let server know that this client is disconnecting
		/// </summary>
		public void DisconnectFromServer(byte myID) {
			SendDataImmediate(DataIDs.ClientDisconnected, new byte[] { myID });
			Dispose();
		}

		/// <summary>
		/// Handle higher level data that only a Client can react to
		/// </summary>
		/// <param name="data"></param>
		protected override void HigherLevelDataReceived(ReceivedData data) {
			if (data.dataType == typeof(ClientDisconnectedPacket)) {
				_OnClientKickedFromServer?.Invoke(this, EventArgs.Empty);
			}
			if (data.dataType == typeof(OnPropertySynchronizationEventArgs)) {
				client.InvokeOnPropertySync(this, new OnPropertySynchronizationEventArgs() {
					syncID = (byte)data.receivedObject,
					propertyName = client.getConnection.dataIDs.syncedProperties[(byte)data.receivedObject].property.Name,
					instance = client.getConnection.dataIDs.syncedProperties[(byte)data.receivedObject].classInstance
				});
			}
		}
	}
}
