using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Igor.TCP {
	/// <summary>
	/// TCP Server instance. Accepts client connections
	/// </summary>
	public class TCPServer : IDisposable, IValueProvider, IRerouteCapable {
		internal readonly Dictionary<byte, ServerToClientConnection> connectedClients = new();

		private IPAddress currentAddress;
		private ushort currentPort;

		private TcpListener clientConnectionListener;
		private bool listenForClientConnections;

		private readonly ManualResetEventSlim serverStartEvnt = new();

		Dictionary<byte, Delegate> IValueProvider.ProvidedValues { get; } = new();
		Dictionary<byte, List<ReroutingInfo>> IRerouteCapable.RerouteDefinitions { get; } = new();

		/// <summary>
		/// Client ID used by packets that come from the server
		/// </summary>
		public const byte SERVER_PACKET_ORIGIN_ID = 0;

		/// <summary>
		/// Configuration of this server
		/// </summary>
		public ServerConfiguration ServerConfiguration { get; }

		/// <summary>
		/// Called when client connects to this server
		/// </summary>
		public event EventHandler<ClientConnectedEventArgs> OnClientConnected;

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
		/// Called when client connects to this server, but the capacity is full,
		/// allows to optionally kick other clients to make space for this one
		/// </summary>
		public event EventHandler<FullCapacityEventArgs> OnFullCapacity;

		/// <summary>
		/// Called when the client attempts to join the server
		/// </summary>
		public Func<TCPClientInfo, bool> OnClientConnectionAttempt { get; } = info => true;

		/// <summary>
		/// Get all connected clients
		/// </summary>
		public TCPClientInfo[] ConnectedClients => connectedClients.Select(s => s.Value.infoAboutOtherSide).ToArray();

		/// <summary>
		/// Create new <see cref="TCPServer"/>
		/// </summary>
		public TCPServer(ServerConfiguration configuration) {
			ServerConfiguration = configuration;
		}

		#region Start/Stop Server

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		/// <exception cref="ServerStartException">The port is already in use</exception>
		public async Task Start(ushort port) {
			currentAddress = SimpleTCPHelper.GetActiveIPv4Address();
			await StartServer(currentAddress, port);
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		/// <exception cref="ServerStartException">The address is invalid or the port is already in use</exception>
		public async Task Start(string ipAddress, ushort port) {
			if (IPAddress.TryParse(ipAddress, out currentAddress)) {
				await StartServer(currentAddress, port);
			}
			else {
				Console.WriteLine("Unable to parse IP address string '{0}'", ipAddress);
			}
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		/// <exception cref="ServerStartException">The address is invalid or the port is already in use</exception>
		public async Task Start(IPAddress address, ushort port) {
			await StartServer(address, port);
		}

		/// <summary>
		/// Actual server starting function
		/// </summary>
		/// <exception cref="ServerStartException">The address is invalid or the port is already in use</exception>
		private async Task StartServer(IPAddress address, ushort port) {
			currentPort = port;
			try {
				System.Diagnostics.Debug.WriteLine(address);
				listenForClientConnections = true;
				_ = Task.Run(() => ListenForClientConnection(address, port));
				await Task.Run(serverStartEvnt.Wait);
				OnServerStarted?.Invoke(this, EventArgs.Empty);
			}
			catch {
				throw new ServerStartException("Unable to start server at " + address + ":" + port);
			}
		}

		/// <summary>
		/// Stops the server
		/// </summary>
		public void Stop() {
			listenForClientConnections = false;
			clientConnectionListener.Stop();
			foreach (ServerToClientConnection connection in connectedClients.Values) {
				connection.DisconnectClient(connection.infoAboutOtherSide.ID);
			}
			connectedClients.Clear();
		}

		/// <exception cref="InvalidOperationException">When the ID is not connected</exception>
		public void DisconnectClient(byte clientID) {
			try {
				ServerToClientConnection a = connectedClients.Values.Single(s => s.infoAboutOtherSide.ID == clientID);
				a.DisconnectClient(clientID);
			}
			catch {
				throw new InvalidOperationException("Client with id: " + clientID + " is not connected!");
			}
		}

		#endregion

		#region Getting connections to the server

		/// <summary>
		/// Get client connection by 'ID'
		/// </summary>
		/// <exception cref="InvalidOperationException">When the ID is not connected or the ID of 0 is requested</exception>
		public TCPConnection GetConnection(byte id) {
			if (connectedClients.ContainsKey(id)) {
				return connectedClients[id];
			}
			if (id == 0) {
				throw new InvalidOperationException("ID '0' is reserved to the server, client numbering starts from '1'!");
			}
			throw new InvalidOperationException("Client with ID " + id + " is not connected to the server!");
		}

		/// <summary>
		/// Get client connection by IP address
		/// </summary>
		/// <exception cref="InvalidOperationException">When the IPAddress is not connected</exception>
		public TCPConnection GetConnection(IPAddress address) {
			foreach (var item in connectedClients) {
				if (item.Value.infoAboutOtherSide.Address == address.ToString()) {
					return item.Value;
				}
			}
			throw new InvalidOperationException("This IP address is not currently connected");
		}

		#endregion

		#region Listen for client connection

		/// <summary>
		/// Set listening for incoming data from connected client 'clientID'
		/// </summary>
		/// <exception cref="InvalidOperationException">When the ID is not connected</exception>
		public void SetListeningForData(byte clientID, bool state) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].ListeningForData = state;
				connectedClients[clientID].DataReception();
				return;
			}
			throw new InvalidOperationException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Set listening for incoming client connection attempts
		/// </summary>
		public bool IsListeningForClients {
			get => listenForClientConnections;
			set {
				if (!listenForClientConnections && value) {
					_ = StartServer(currentAddress, currentPort);
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
				TcpClient newlyConnected;
				try {
					newlyConnected = await clientConnectionListener.AcceptTcpClientAsync();
				}
				catch (SocketException e) {
					if (e.SocketErrorCode == SocketError.Interrupted) {
						listenForClientConnections = false;
						return;
					}
					continue;
				}

				if (connectedClients.Count >= byte.MaxValue) {
					OnFullCapacity?.Invoke(this, new FullCapacityEventArgs(ConnectedClients, this, newlyConnected));
				}

				if (connectedClients.Count >= byte.MaxValue) {
					continue;
				}

				byte id = (byte)(connectedClients.Count + 1);

				NetworkStream newStream = newlyConnected.GetStream();

				try {
					await newStream.WriteAsync(new[] { id }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				}
				catch {
					continue;
				}

				byte[] packetHeader = new byte[DataIDs.PACKET_TOTAL_HEADER_SIZE_COMPLEXITY];

				try {
					await newStream.ReadAsync(packetHeader, 0, packetHeader.Length);
				}
				catch {
					continue;
				}

				long packetLength = BitConverter.ToInt64(packetHeader, 0);

				byte[] clientInfo = new byte[packetLength];
				int pos = 0;

				try {
					while (pos != clientInfo.Length) {
						pos += await newStream.ReadAsync(clientInfo, pos, clientInfo.Length);
					}
				}
				catch {
					continue;
				}

				TCPClientInfo connectedClientInfo;
				try {
					connectedClientInfo = (TCPClientInfo)SimpleTCPHelper.GetObject(typeof(TCPClientInfo), clientInfo, ServerConfiguration);
				}
				catch {
					System.Diagnostics.Debug.WriteLine("Who are you!? Failed Handshake! -> Dropping connection");
					newlyConnected.Dispose();
					continue;
				}


				TCPClientInfo serverInfo = new("Server", true, SimpleTCPHelper.GetActiveIPv4Address().ToString()) {
					ID = 0
				};
				ServerToClientConnection conn = new(newlyConnected, serverInfo, connectedClientInfo, this, ServerConfiguration);
				conn.dataIDs.rerouter = this;
				conn.dataIDs.OnRerouteRequest += DataIDs_OnRerouteRequest;
				conn._OnClientDisconnected += ClientDisconnected;
				bool accepted = OnClientConnectionAttempt(connectedClientInfo);
				if (accepted) {
					try {
						await newStream.WriteAsync(new[] { id }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
						connectedClients.Add(id, conn);
						OnClientConnected?.Invoke(this, new ClientConnectedEventArgs(this, connectedClientInfo));
					}
					catch {
						/* If writing to the stream failed then the state does not change,
						   If the OnClientConnected function throws, the state is persisted */
					}
				}
				else {
					newStream.Write(new[] { byte.MaxValue }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				}
			}
		}

		#endregion

		#region Private Events

		private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			OnClientDisconnected?.Invoke(this, e);
		}

		/// <exception cref="InvalidOperationException">When the ID is not connected</exception>
		private void DataIDs_OnRerouteRequest(object sender, DataReroutedEventArgs e) {
			if (connectedClients.ContainsKey(e.ForwardedClient)) {
				connectedClients[e.ForwardedClient].SendData(e.PacketID, e.OriginClient, e.Data);
				OnDataRerouted?.Invoke(this, e);
				return;
			}
			throw new InvalidOperationException("Client with ID " + e.ForwardedClient + " is not connected to the server!");
		}

		#endregion

		#region Property Synchronization

		/// <summary>
		/// Define 'propertySyncPacketID' for synchronization of public property for client 'clientID' named 'propertyName' from instance of a class 'instance', publish changes by calling UpdateProp()
		/// </summary>
		/// <exception cref="InvalidOperationException">When the ID is not connected</exception>
		public void SyncProperty(byte clientID, object instance, string propertyName, byte propertySyncPacketID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].dataIDs.syncedProperties.Add(propertySyncPacketID, new PropertySynchronization(propertySyncPacketID, instance, propertyName));
				return;
			}
			throw new InvalidOperationException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Sends updated property value to connected 'clientID' with property id 'ID' and set value
		/// </summary>
		/// <exception cref="InvalidOperationException">When the ID is not connected</exception>
		public void UpdateProp(byte clientID, byte id, object value) {
			if (connectedClients.ContainsKey(clientID)) {
				byte[] rawData = SimpleTCPHelper.GetBytesFromObject(value, ServerConfiguration);
				byte[] merged = new byte[rawData.Length + 1];
				merged[0] = id;
				rawData.CopyTo(merged, 1);
				connectedClients[clientID].SendData(DataIDs.PROPERTY_SYNC_ID, SERVER_PACKET_ORIGIN_ID, merged);
				return;
			}
			throw new InvalidOperationException("Client with ID " + clientID + " is not connected to the server!");
		}

		#endregion

		#region Communication Definitions

		/// <summary>
		/// Provide a value to all connected clients
		/// </summary>
		public void ProvideValue<T>(byte packetID, Func<T> function) {
			foreach (TCPClientInfo info in ConnectedClients) {
				if (connectedClients[info.ID].dataIDs.IsIDReserved(packetID, out Type dataType, out string message)) {
					throw new PacketIDTakenException(packetID, dataType, message);
				}
			}
			(this as IValueProvider).ProvidedValues.Add(packetID, function);
		}

		/// <summary>
		/// Provide a value to a selected client
		/// </summary>
		public void ProvideValue<T>(byte clientID, byte packetID, Func<T> function) {
			if (connectedClients[clientID].dataIDs.IsIDReserved(packetID, out Type dataType, out string message)) {
				throw new PacketIDTakenException(packetID, dataType, message);
			}
			GetConnection(clientID).dataIDs.responseFunctionMap.Add(packetID, function);
			GetConnection(clientID).dataIDs.requestTypeMap.Add(packetID, typeof(T));
		}

		/// <summary>
		/// Request a value from a client
		/// </summary>
		public async Task<T> GetValue<T>(byte clientID, byte packetID) {
			TCPResponse resp = await GetConnection(clientID).requestCreator.Request(packetID, typeof(T));
			return (T)resp.GetObject;
		}

		/// <summary>
		/// Define rerouting of all packets from 'fromClient' coming to this server identified as 'packetID', to be sent to 'toClient'
		/// </summary>
		public void DefineRerouteID(byte fromClient, byte toClient, byte packetID) {
			ReroutingInfo info = new ReroutingInfo(toClient, packetID);
			GetConnection(fromClient).dataIDs.SetForRerouting(info);
		}

		/// <summary>
		/// Register custom packet with ID that will carry a TData type, delivered via the callback
		/// </summary>
		/// <exception cref="InvalidOperationException">The data is not marked as [Serializable] and is not defined in custom serialization rules</exception>
		public void DefineCustomPacket<TData>(byte clientID, byte packetID, Action<byte, TData> callback) {
			if (!typeof(TData).IsSerializable && !ServerConfiguration.ContainsSerializationRule(typeof(TData))) {
				throw new InvalidOperationException($"Attempting to define packet for type {typeof(TData).FullName}, but it is not marked [Serializable]");
			}
			GetConnection(clientID).dataIDs.DefineCustomPacket(packetID, callback);
		}

		#endregion

		#region Send To All custom data, string, and long

		/// <summary>
		/// Send custom data to all connected clients.
		/// </summary>
		/// <exception cref="InvalidOperationException">One of the clients does not understand the data being sent</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">When data fails to serialize</exception>
		public void SendToAll<TData>(byte packetID, TData data) {
			foreach (ServerToClientConnection info in connectedClients.Values) {
				info.SendData(packetID, info.infoAboutOtherSide.ID, SimpleTCPHelper.GetBytesFromObject(data, ServerConfiguration));
			}
		}

		/// <summary>
		/// Send <see cref="string"/> to all connected clients.
		/// </summary>
		public void SendToAll(string data) {
			foreach (ServerToClientConnection info in connectedClients.Values) {
				info.SendData(data);
			}
		}

		/// <summary>
		/// Send <see cref="Int64"/> to all connected clients.
		/// </summary>
		public void SendToAll(Int64 data) {
			foreach (ServerToClientConnection info in connectedClients.Values) {
				info.SendData(data);
			}
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;
		
		public void Dispose() {
			if (disposedValue) return;

			serverStartEvnt.Dispose();
			disposedValue = true;
		}

		#endregion
	}
}