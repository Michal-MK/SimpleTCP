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

		/// <summary>
		/// Information about this client
		/// </summary>
		public TCPClientInfo clientInfo { get; set; }

		/// <summary>
		/// Get connection to the server, allows client to server communication, holds send and receive functionality
		/// </summary>
		public ClientToServerConnection getConnection { get; private set; }

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
		/// Initialize new TCPClient by connecting to 'ipAddress' on port 'port'
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(IPAddress ipAddress, ushort port)
			: this(new ConnectionData(ipAddress.ToString(), port)) {
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
			clientInfo = new TCPClientInfo(Environment.UserName, false, SimpleTCPHelper.GetActiveIPv4Address());
		}

		/// <summary>
		/// Connect to server with specified IP and port
		/// </summary>
		public void Connect() {
			TcpClient clientBase = new TcpClient();
			clientBase.Connect(address, port);
			byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];
			NetworkStream stream = clientBase.GetStream();
			stream.Read(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
			clientInfo.clientID = buffer[0];

			byte[] clientInfoArray = SimpleTCPHelper.GetBytesFromObject(clientInfo);

			stream.Write(clientInfoArray, 0, clientInfoArray.Length);

			TCPClientInfo serverInfo = new TCPClientInfo("Server", true, address);
			serverInfo.clientID = 0;
			getConnection = new ClientToServerConnection(clientBase, clientInfo, serverInfo, this);
			if (debugPrints) {
				Console.WriteLine("Connection Established");
			}
		}


		/// <summary>
		/// Quick setup of client meta-data
		/// </summary>
		/// <param name="clientName">If left empty Current user name is used</param>
		public TCPClientInfo SetUpClientInfo(string clientName) {
			clientInfo = new TCPClientInfo(clientName, false, address);
			return clientInfo;
		}


		/// <summary>
		/// Set listening for incoming data from connected client 'clientID'
		/// </summary>
		public bool isListeningForData {
			get { return getConnection.listeningForData; }
			set { getConnection.listeningForData = value; }
		}


		/// <summary>
		/// Define 'propID' for synchronization of public property named 'propetyName' from instance of a class 'instance' 
		/// </summary>
		public void SyncPropery(object instance, string propertyName, byte propID) {
			getConnection.dataIDs.syncedProperties.Add(propID, new PropertySynchronization(propID,instance,propertyName));
		}

		#region Communication Definitions

		/// <summary>
		/// Shorthand for <see cref="DefineRequestEntry{TData}(byte)"/> and <see cref="DefineResponseEntry{TData}(byte, Func{TData})"/>, transmission like.
		/// </summary>
		public void DefineTwoWayComunication<TData>(byte packetID, Func<TData> function) {
			getConnection.dataIDs.requestTypeMap.Add(packetID, typeof(TData));
			getConnection.dataIDs.responseFunctionMap.Add(packetID, function);
		}

		/// <summary>
		/// Shorthand for <see cref="CancelRequestID(byte)"/> and <see cref="CancelResponseID(byte)"/>, transmission like.
		/// </summary>
		public void CancelTwoWayComunication(byte packetID) {
			getConnection.dataIDs.requestTypeMap.Remove(packetID);
			getConnection.dataIDs.responseFunctionMap.Remove(packetID);
		}

		/// <summary>
		/// Define custom request by specifying its 'TData' type with selected 'ID'
		/// </summary>
		public void DefineRequestEntry<TData>(byte ID) {
			getConnection.dataIDs.requestTypeMap.Add(ID, typeof(TData));
		}

		/// <summary>
		/// Cancel custom request of 'TData' under 'ID'
		/// </summary>
		public void CancelRequestID(byte ID) {
			getConnection.dataIDs.requestTypeMap.Remove(ID);
		}

		/// <summary>
		/// Define response 'function' to be called when request packet with 'ID' is received
		/// </summary>
		public void DefineResponseEntry<TData>(byte ID, Func<TData> function) {
			getConnection.dataIDs.responseFunctionMap.Add(ID, function);
		}

		/// <summary>
		/// Cancel response to request with 'ID'
		/// </summary>
		public void CancelResponseID(byte ID) {
			getConnection.dataIDs.responseFunctionMap.Remove(ID);
		}

		#endregion

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		public async Task<TCPResponse> RaiseRequestAsync(byte ID) {
			TCPResponse data = await getConnection.requestHandler.Request(ID);
			return data;
		}

		/// <summary>
		/// Disconnect from current server and dispose of this client
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void Disconnect() {
			if (getConnection != null) {
				getConnection.DisconnectFromServer(clientInfo.clientID);
				getConnection = null;
			}
			else {
				throw new NullReferenceException("Attempting to disconnect from server while this client is not connected to anything");
			}
		}
	}
}