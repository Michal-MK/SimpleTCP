using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_STANDALONE || UNITY_ANDROID
using UnityEngine;
#endif


namespace Igor.TCP {
	/// <summary>
	/// Client that can connect to a TCPServer
	/// </summary>
	public class TCPClient {
		private readonly IPAddress address;
		private readonly ushort port;

		private ConnectionInfo server;

		/// <summary>
		/// Get connection to the server, allows client to server comunication, holds send and receiove functionality
		/// </summary>
		public TCPConnection getConnection { get { return server.connection; } }

		internal RequestManager requestHandler { get; }

		/// <summary>
		/// Access to datatypes for custom packets
		/// </summary>
		public ResponseManager responseHandler { get; }

		/// <summary>
		/// Get ID this client was assigned by the server
		/// </summary>
		public byte clientID { get; private set; }


		/// <summary>
		/// Initialize new TCPClient by connecting to 'ipAddress' on port 'port'
		/// </summary>
		public TCPClient(string ipAddress, ushort port)
			: this(new ConnectionData(ipAddress, port)) {
		}

		/// <summary>
		/// Initialize new TCPClient by connecting to a server defined in 'data'
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(ConnectionData data) {
			this.port = data.port;
			if (IPAddress.TryParse(data.ipAddress, out address)) {
				TcpClient serverBase = new TcpClient();
				serverBase.Connect(address, port);
				byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];
				NetworkStream stream = serverBase.GetStream();
				stream.Read(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				clientID = buffer[0];
				server = new ConnectionInfo(address, clientID, serverBase, new TCPConnection(serverBase));
				Console.WriteLine("Connection Established");
			}
			else {
				throw new WebException("Entered Invalid IP Address!", WebExceptionStatus.ConnectFailure);
			}
		}

		/// <summary>
		/// Set listening for incomming data from connected client 'clientID'
		/// </summary>
		public void SetListeningForData(byte clientID, bool state) {
			server.connection.listeningForData = state;
		}


		/// <summary>
		/// Define 'propID' for synchronization of public property named 'propetyName' from instance of a class 'instance' 
		/// </summary>
		public void SyncPropery(object instance, string propertyName, byte propID) {
			PropertyInfo info = instance.GetType().GetProperty(propertyName);
			server.connection.dataIDs.syncProps.Add(propID, new Tuple<object, PropertyInfo>(instance, info));
		}


		/// <summary>
		/// NIY, no guarantee of safety/funcionality when using this
		/// </summary>
		//TODO
		public void UpdateProp(byte id, object value) {
			server.connection.SendData(DataIDs.PropertySyncID, id, Helper.GetBytesFromObject<object>(value));
		}

		/// <summary>
		/// Shorthand for <see cref="DefineRequestEntry{TData}(byte)"/> and <see cref="DefineResponseEntry{TData}(byte, Func{TData})"/>, transimssion like.
		/// </summary>
		public void DefineTwoWayComuncation<TData>(byte ID, Func<TData> function) {
			server.connection.dataIDs.requestDict.Add(ID, typeof(TData));
			server.connection.dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// Shorthand for <see cref="CancelRequestID(byte)"/> and <see cref="CancelResponseID(byte)"/>, transimssion like.
		/// </summary>
		public void CancelTwoWayComunication(byte ID) {
			server.connection.dataIDs.requestDict.Remove(ID);
			server.connection.dataIDs.responseDict.Remove(ID);
		}

		/// <summary>
		/// Define custom request by specifying its 'TData' type with selected 'ID'
		/// </summary>
		public void DefineRequestEntry<TData>(byte ID) {
			server.connection.dataIDs.requestDict.Add(ID, typeof(TData));
		}

		/// <summary>
		/// Cancel custom request of 'TData' under 'ID'
		/// </summary>
		public void CancelRequestID(byte ID) {
			server.connection.dataIDs.requestDict.Remove(ID);
		}

		/// <summary>
		/// Define response 'function' to be called when request packet with 'ID' is received
		/// </summary>
		public void DefineResponseEntry<TData>(byte ID, Func<TData> function) {
			server.connection.dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// Cancel response to request with 'ID'
		/// </summary>
		public void CancelResponseID(byte ID) {
			server.connection.dataIDs.responseDict.Remove(ID);
		}

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		public async Task<TCPResponse> RaiseRequestAsync(byte ID) {
			TCPResponse data = await requestHandler.Request(ID);
			return data;
		}

		/// <summary>
		/// Disconnect from current server
		/// </summary>
		public void Disconnect() {
			if (getConnection != null) {
				getConnection.listeningForData = false;
				getConnection.Dispose(clientID);
				getConnection._OnClientDisconnected += GetConnection_OnClientDisconnected;
			}
		}

		private void GetConnection_OnClientDisconnected(object sender, byte e) {
			e = clientID;
			getConnection.senderThread.Abort();
			getConnection.receiverThread.Abort();
			server.baseClient.Close();
			server.baseClient.Dispose();
		}
	}
}