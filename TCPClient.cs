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
		/// Infromation about this client
		/// </summary>
		public TCPClientInfo clientInfo { get; set; }

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
		/// Log debug information into the console
		/// </summary>
		public bool debugPrints { get; set; } = false;


		/// <summary>
		/// Initialize new TCPClient by connecting to 'ipAddress' on port 'port'
		/// </summary>
		/// <exception cref="WebException"></exception>
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
				if (debugPrints) {
					Console.WriteLine("IP parsed successfully");
				}
			}
			else {
				throw new WebException("Entered Invalid IP Address!", WebExceptionStatus.ConnectFailure);
			}
		}
		
		/// <summary>
		/// Connect to server with specified IP and port
		/// </summary>
		public void Connect() {
			TcpClient serverBase = new TcpClient();
			serverBase.Connect(address, port);
			byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];
			NetworkStream stream = serverBase.GetStream();
			stream.Read(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
			if(clientInfo == null) {
				clientInfo = SetUpClientInfo();
			}
			clientInfo.clientID = clientID = buffer[0];
			
			byte[] clientInfoArray = Helper.GetBytesFromObject<TCPClientInfo>(clientInfo);

			stream.Write(clientInfoArray, 0, clientInfoArray.Length);
			server = new ConnectionInfo(address, clientID, serverBase, new TCPConnection(serverBase,clientInfo));
			Console.WriteLine("Connection Established");
		}


		/// <summary>
		/// Quick setup of client metadata
		/// </summary>
		/// <param name="clientName">If left empty Current user name is used</param>
		public TCPClientInfo SetUpClientInfo(string clientName = "") {
			if (clientName == "") {
				clientInfo = new TCPClientInfo(Environment.UserName, false, address);
			}
			else {
				clientInfo = new TCPClientInfo(clientName, false, address);
			}
			clientInfo.clientID = clientID;
			return clientInfo;
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
				getConnection.Dispose(clientID);
			}
		}
	}
}