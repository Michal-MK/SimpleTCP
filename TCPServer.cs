﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Igor.TCP {

	/// <summary>
	/// TCP Server instance. Accepts client connections
	/// </summary>
	public class TCPServer {

		private TcpListener clientConnectionListener;

		private Dictionary<byte, ServerToClientConnection> connectedClients = new Dictionary<byte, ServerToClientConnection>();

		internal bool listenForClientConnections = false;

		private IPAddress currentAddress;
		private ushort currentPort;

		private ManualResetEventSlim serverStartEvnt = new ManualResetEventSlim();


		/// <summary>
		/// Client ID used by packets that come from the server
		/// </summary>
		public const byte ServerPacketOrigin = 0;

		/// <summary>
		/// Configuration of this server
		/// </summary>
		public ServerConfiguration serverConfiguration { get; }

		/// <summary>
		/// Called when client connects to this server
		/// </summary>
		public event EventHandler<ClientConnectedEventArgs> OnConnectionEstablished;

		/// <summary>
		/// Called when client disconnects from this server
		/// </summary>
		public event EventHandler<ClientDisconnectedEventArgs> OnClientDisconnected;

		/// <summary>
		/// Called when server reroutes data
		/// </summary>
		public event EventHandler<DataReroutedEventArgs> OnDataRerouted;

		/// <summary>
		/// Called when the server successfully starts
		/// </summary>
		public event EventHandler OnServerStarted;


		/// <summary>
		/// Create new <see cref="TCPServer"/>
		/// </summary>
		public TCPServer(ServerConfiguration configuration) {
			serverConfiguration = configuration;
		}

		#region Start/Stop Server

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		public async Task Start(ushort port) {
			currentAddress = SimpleTCPHelper.GetActiveIPv4Address();
			currentPort = port;
			await StartServer(currentAddress, port);
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		public async Task Start(string ipAddress, ushort port) {
			if (IPAddress.TryParse(ipAddress, out currentAddress)) {
				currentPort = port;
				await StartServer(currentAddress, port);
			}
			else {
				Console.WriteLine("Unable to parse IP address string '{0}'", ipAddress);
				return;
			}
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		public async Task Start(IPAddress address, ushort port) {
			await StartServer(address, port);
		}

		/// <summary>
		/// Actual server starting function
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		private Task StartServer(IPAddress address, ushort port) {
			try {
				return Task.Run(() => {
					Console.WriteLine(address);
					listenForClientConnections = true;
					new Thread(new ThreadStart(async delegate () { await ListenForClientConnection(address, port); })) { Name = "Client Connection Listener" }.Start();
					serverStartEvnt.Wait();
					OnServerStarted?.Invoke(this, EventArgs.Empty);
				});
			}
			catch {
				throw new ServerStartException("Unable to start server at " + address.ToString() + ":" + port);
			}
		}

		/// <summary>
		/// Stops the server
		/// </summary>
		public async Task Stop() {
			await Task.Run(delegate () {
				listenForClientConnections = false;
				clientConnectionListener.Stop();
				foreach (ServerToClientConnection connection in connectedClients.Values.ToArray()) {
					connection.DisconnectClient(connection.connectedClientInfo.clientID);
				}
			});
		}

		#endregion

		#region Getting connections to the server

		/// <summary>
		/// Get client connection by ID, NPE if client id is not found/connected
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public TCPConnection GetConnection(byte id) {
			if (connectedClients.ContainsKey(id)) {
				return connectedClients[id];
			}
			if (id == 0) {
				throw new InvalidOperationException("ID '0' is reserved to the server, client numbering starts from '1'!");
			}
			throw new NullReferenceException("Client with ID " + id + " is not connected to the server!");
		}

		/// <summary>
		/// Get client connection by IP address
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public TCPConnection GetConnection(IPAddress address) {
			foreach (var item in connectedClients) {
				if (item.Value.connectedClientInfo.clientAddress == address) {
					return item.Value;
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
				Dictionary<byte, ServerToClientConnection>.Enumerator enumer = connectedClients.GetEnumerator();
				int i = 0;
				while (enumer.MoveNext()) {
					KeyValuePair<byte, ServerToClientConnection> kv = enumer.Current;
					connections[i] = kv.Value.connectedClientInfo;
					i++;
				}
				return connections;
			}
		}

		#endregion


		/// <summary>
		/// Set listening for incoming data from connected client 'clientID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SetListeningForData(byte clientID, bool state) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].listeningForData = state;
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Set listening for incoming client connection attempts
		/// </summary>
		public bool isListeningForClients {
			get {
				return listenForClientConnections;
			}
			set {
				if (!listenForClientConnections && value) {
					StartServer(currentAddress, currentPort);
				}
				if (!value) {
					clientConnectionListener.Stop();
				}
				listenForClientConnections = value;
			}
		}

		private async Task ListenForClientConnection(IPAddress address, ushort port) {
			clientConnectionListener = new TcpListener(address, port);
			clientConnectionListener.Start();
			serverStartEvnt.Set();
			while (listenForClientConnections) {
				TcpClient newlyConnected = await clientConnectionListener.AcceptTcpClientAsync();
				byte ID = (byte)(connectedClients.Count + 1);
				NetworkStream newStream = newlyConnected.GetStream();
				newStream.Write(new byte[] { ID }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				byte[] clientInfo = new byte[1024];

				int bytesRead = newStream.Read(clientInfo, 0, clientInfo.Length);
				TCPClientInfo connectedClientInfo = (TCPClientInfo)SimpleTCPHelper.GetObject(typeof(TCPClientInfo), clientInfo);
				TCPClientInfo serverInfo = new TCPClientInfo("Server", true, SimpleTCPHelper.GetActiveIPv4Address());
				serverInfo.clientID = 0;
				ServerToClientConnection conn = new ServerToClientConnection(newlyConnected, serverInfo, connectedClientInfo, this);
				conn.dataIDs.OnRerouteRequest += DataIDs_OnRerouteRequest;
				conn._OnClientDisconnected += ClientDisconnected;
				connectedClients.Add(ID, conn);
				OnConnectionEstablished?.Invoke(this, new ClientConnectedEventArgs(this, connectedClientInfo));
			}
		}

		private void ClientDisconnected(object sender, byte e) {
			OnClientDisconnected?.Invoke(this, new ClientDisconnectedEventArgs(e));
			connectedClients.Remove(e);
		}

		private void DataIDs_OnRerouteRequest(object sender, DataReroutedEventArgs e) {
			if (connectedClients.ContainsKey(e.forwardedClient)) {
				connectedClients[e.forwardedClient]._SendDataRerouted(e.universalID, e.originClient, e.data);
				OnDataRerouted?.Invoke(this, e);
				return;
			}
			throw new NullReferenceException("Client with ID " + e.forwardedClient + " is not connected to the server!");
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property for client 'clientID' named 'propetyName' from instance of a class 'instance', publish changes by calling UpdateProp()
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SyncPropery(byte clientID, object instance, string propertyName, byte propID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].dataIDs.syncedProperties.Add(propID, new PropertySynchronization(propID, instance, propertyName));
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
				connectedClients[clientID].SendData(DataIDs.PropertySyncID, ID, SimpleTCPHelper.GetBytesFromObject(value));
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		#region Communication Definitions

		/// <summary>
		/// Shorthand for Request and response id definition, when both sides are senders 
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void DefineTwoWayComunication<TData>(byte clientID, byte packetID, Func<TData> function) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].dataIDs.requestTypeMap.Add(packetID, typeof(TData));
				connectedClients[clientID].dataIDs.responseFunctionMap.Add(packetID, function);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Cancel two way communication for client 'clientID' with id 'ID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void CancelTwoWayComunication(byte clientID, byte packetID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].dataIDs.requestTypeMap.Remove(packetID);
				connectedClients[clientID].dataIDs.responseFunctionMap.Remove(packetID);
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
				connectedClients[clientID].dataIDs.requestTypeMap.Add(ID, typeof(TData));
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
				connectedClients[clientID].dataIDs.requestTypeMap.Remove(ID);
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
				connectedClients[clientID].dataIDs.responseFunctionMap.Add(ID, function);
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
				connectedClients[clientID].dataIDs.responseFunctionMap.Remove(ID);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Define rerouting of all packets from 'fromClient' coming to this server to under 'packetID' to be sent to 'toClient'
		/// </summary>
		public void DefineRerouteID(byte fromClient, byte toClient, byte packetID) {
			ReroutingInfo info = new ReroutingInfo(fromClient, toClient);
			info.SetPacketInfo(packetID);
			DataIDs.AddToReroute(packetID, info);
		}

		#endregion

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public async Task<TCPResponse> RaiseRequestAsync(byte clientID, byte ID) {
			if (connectedClients.ContainsKey(clientID)) {
				return await connectedClients[clientID].requestHandler.Request(ID);
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Send 'data' to all connected clients, sent under <see cref="DataIDs.UserDefined"/> packet.
		/// </summary>
		/// <exception cref="UndefinedPacketException"></exception>
		public void SendToAll<TData>(byte dataID, TData data) {
			foreach (ServerToClientConnection info in connectedClients.Values) {
				if (info.dataIDs.customIDs.ContainsKey(dataID)) {
					info._SendData(dataID, 0, SimpleTCPHelper.GetBytesFromObject(data));
				}
				else {
					throw new UndefinedPacketException("Packet is not defined!", dataID, typeof(TData));
				}
			}
		}
	}
}
