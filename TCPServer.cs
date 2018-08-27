using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Igor.TCP {
	/// <summary>
	/// TCP Server, accepts client connections
	/// </summary>
	public class TCPServer {
		/// <summary>
		/// Used by packets coming from the server TODO
		/// </summary>
		public const byte ServerPacketOrigin = 254;

		private TcpListener clientConnectionListener;

		private Dictionary<byte, ConnectionInfo> connectedClients = new Dictionary<byte, ConnectionInfo>();

		internal bool listenForClientConnections = false;

		private IPAddress currentAddress;
		private ushort currentPort;

		/// <summary>
		/// Called when client connects to this server
		/// </summary>
		public event EventHandler<ClientConnectedEventArgs> OnConnectionEstablished;

		/// <summary>
		/// Called when client disconnects from this server
		/// </summary>
		public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;


		#region Start Server
		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		public void Start(ushort port) {
			currentAddress = Helper.GetActiveIPv4Address();
			currentPort = port;
			StartServer(currentAddress, port);
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		public void Start(string ipAddress, ushort port) {
			if (IPAddress.TryParse(ipAddress, out currentAddress)) {
				currentPort = port;
				StartServer(currentAddress, port);
			}
			else {
				Console.WriteLine("Unable to parse ip address string '{0}'", ipAddress);
				return;
			}
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		public void Start(IPAddress address, ushort port) {
			StartServer(address, port);
		}


		private void StartServer(IPAddress address, ushort port) {
			Console.WriteLine(address);
			listenForClientConnections = true;
			new Thread(new ThreadStart(delegate () { ListenForClientConnection(address, port); })) { Name = "Client Connection Listener" }.Start();
		}
		#endregion

		/// <summary>
		/// Get client connection by ID
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public TCPConnection GetConnection(byte id) {
			if (connectedClients.ContainsKey(id)) {
				return connectedClients[id].connection;
			}
			throw new NullReferenceException("Client with ID " + id + " is not connected to the server!");
		}

		/// <summary>
		/// Get client connection by IP address
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public TCPConnection GetConnection(IPAddress address) {
			foreach (var item in connectedClients) {
				if (item.Value.connectedAddress == address) {
					return item.Value.connection;
				}
			}
			throw new NullReferenceException("This IP address is not currently connected");
		}

		/// <summary>
		/// Get all connected clients
		/// </summary>
		public TCPClientInfo[] getConnectedClients {
			get {
				TCPClientInfo[] connections = new TCPClientInfo[connectedClients.Keys.Count];
				Dictionary<byte, ConnectionInfo>.Enumerator enumer = connectedClients.GetEnumerator();
				int i = 0;
				while (enumer.MoveNext()) {
					KeyValuePair<byte, ConnectionInfo> kv = enumer.Current;
					connections[i] = kv.Value.connection.clientInfo;
					i++;
				}
				return connections;
			}
		}


		/// <summary>
		/// Set listening for incoming data from connected client 'clientID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SetListeningForData(byte clientID, bool state) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.listeningForData = state;
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}


		/// <summary>
		/// Set listening for incoming client connection attempts
		/// </summary>
		public void SetIncommingClientConnection(bool state) {
			if (!listenForClientConnections) {
				StartServer(currentAddress, currentPort);
			}
			if (!state) {
				listenForClientConnections = state;
				clientConnectionListener.Stop();
			}
		}


		private void ListenForClientConnection(IPAddress address, ushort port) {
			clientConnectionListener = new TcpListener(address, port);
			clientConnectionListener.Start();
			while (listenForClientConnections) {
				TcpClient newlyConnected = clientConnectionListener.AcceptTcpClient();
				byte ID = (byte)connectedClients.Count;
				NetworkStream newStream = newlyConnected.GetStream();
				newStream.Write(new byte[] { ID }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				byte[] clientInfo = new byte[1024];

				int bytesRead = newStream.Read(clientInfo, 0, clientInfo.Length);
				TCPClientInfo info = (TCPClientInfo)Helper.GetObject(typeof(TCPClientInfo), clientInfo);
				TCPConnection connection = new TCPConnection(newlyConnected, info);
				connection.tcpClientIdentifier = ServerPacketOrigin;
				connection.dataIDs.OnRerouteRequest += DataIDs_OnRerouteRequest;
				connection._OnClientDisconnected += ClientDisconnected;

				ConnectionInfo conn = new ConnectionInfo(
					((IPEndPoint)newlyConnected.Client.RemoteEndPoint).Address,
					ID,
					newlyConnected,
					connection
				);

				connectedClients.Add(ID, conn);
				OnConnectionEstablished?.Invoke(this, new ClientConnectedEventArgs(this, info));
			}
		}

		private void ClientDisconnected(object sender, byte e) {
			OnClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(e));
			connectedClients.Remove(e);
		}

		private void DataIDs_OnRerouteRequest(object sender, DataReroutedEventArgs e) {
			if (connectedClients.ContainsKey(e.forwardedClient)) {
				connectedClients[e.forwardedClient].connection.SendData(e.isUserDefined ? DataIDs.UserDefined : e.universalID, e.data);
				return;
			}
			throw new NullReferenceException("Client with ID " + e.forwardedClient + " is not connected to the server!");
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property for client 'clientID' named 'propetyName' from instance of a class 'instance', publish changes by calling UpdateProp()
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SyncPropery<TProp>(byte clientID, object instance, TProp property, byte propID) {
			PropertyInfo info = property.GetType().GetProperty(property.GetType().Name, typeof(TProp));
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.syncProps.Add(propID, new Tuple<object, PropertyInfo>(instance, info));
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Sends updated property value to connected 'clientID' with property id 'ID' and set value;
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void UpdateProp(byte clientID, byte ID, object value) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.SendData(DataIDs.PropertySyncID, ID, Helper.GetBytesFromObject(value));
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Shorthand for Request and response id definition, when both sides are senders 
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void DefineTwoWayComunication<TData>(byte clientID, byte ID, Func<TData> function) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.requestDict.Add(ID, typeof(TData));
				connectedClients[clientID].connection.dataIDs.responseDict.Add(ID, function);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Cancel two way communication for client 'clientID' with id 'ID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void CancelTwoWayComunication(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.requestDict.Remove(ID);
				connectedClients[clientID].connection.dataIDs.responseDict.Remove(ID);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Define custom request by specifying its 'TData' type with selected 'ID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void DefineRequestEntry<TData>(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.requestDict.Add(ID, typeof(TData));
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Cancel custom request of 'TData' under 'ID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void CancelRequestID(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.requestDict.Remove(ID);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Define response 'function' to be called when request packet with 'ID' is received
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void DefineResponseEntry<TData>(byte clientID, byte ID, Func<TData> function) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.responseDict.Add(ID, function);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Cancel response to request with 'ID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void CancelResponseID(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].connection.dataIDs.responseDict.Remove(ID);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Define rerouting of all packets from 'fromClient' coming to this server to under 'packetID' to be sent to 'toClient'
		/// <para>If 'dataID' is provided, 'packetID' is replaced by <see cref="DataIDs.UserDefined"/> constant value.</para>
		/// </summary>
		public void DefineRerouteID(byte fromClient, byte toClient, byte packetID, byte? dataID = null) {
			ReroutingInfo info = new ReroutingInfo(fromClient, toClient);
			if (dataID != null) {
				info.SetPacketInfoUserDefined(dataID.Value);
				DataIDs.AddToReroute(dataID.Value, info);
			}
			else {
				info.SetPacketInfo(packetID);
				DataIDs.AddToReroute(packetID, info);
			}
		}

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public async Task<TCPResponse> RaiseRequestAsync(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				return await connectedClients[clientID].connection.requestHandler.Request(ID);
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Send 'data' to all connected clients, sent under <see cref="DataIDs.UserDefined"/> packet.
		/// </summary>
		/// <exception cref="UndefinedPacketException"></exception>
		public void SendToAll<TData>(byte dataID, TData data) {
			foreach (ConnectionInfo info in connectedClients.Values) {
				if (info.connection.dataIDs.idDict.ContainsKey(dataID)) {
					info.connection.SendUserDefinedData(dataID, Helper.GetBytesFromObject(data));
				}
				else {
					throw new UndefinedPacketException("Packet is not defined!", dataID, typeof(TData));
				}
			}
		}
	}
}
