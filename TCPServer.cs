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

		private TcpListener clientConnectionListener;
		private bool listenForClientConnections = false;

		private readonly Dictionary<byte, ServerToClientConnection> connectedClients = new Dictionary<byte, ServerToClientConnection>();

		private IPAddress currentAddress;
		private ushort currentPort;

		private readonly ManualResetEventSlim serverStartEvnt = new ManualResetEventSlim();

		Dictionary<byte, Delegate> IValueProvider.ProvidedValues { get; } = new Dictionary<byte, Delegate>();
		Dictionary<byte, List<ReroutingInfo>> IRerouteCapable.RerouteDefinitions { get; } = new Dictionary<byte, List<ReroutingInfo>>();


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
		/// Create new <see cref="TCPServer"/>
		/// </summary>
		public TCPServer(ServerConfiguration configuration) {
			ServerConfiguration = configuration;
		}

		#region Start/Stop Server

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		public async Task Start(ushort port) {
			currentAddress = SimpleTCPHelper.GetActiveIPv4Address();
			await StartServer(currentAddress, port);
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		/// <exception cref="ServerStartException"></exception>
		public async Task Start(string ipAddress, ushort port) {
			if (IPAddress.TryParse(ipAddress, out currentAddress)) {
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
			currentPort = port;
			try {
				return Task.Run(() => {
					Console.WriteLine(address);
					listenForClientConnections = true;
					new Thread(new ThreadStart(delegate () { ListenForClientConnection(address, port); })) { Name = "Client Connection Listener", IsBackground = true }.Start();
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
					connection.DisconnectClient(connection.infoAboutOtherSide.ClientID);
				}
			});
		}

		#endregion

		#region Getting connections to the server

		/// <summary>
		/// Get client connection by 'ID'
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
				if (item.Value.infoAboutOtherSide.Address == address) {
					return item.Value;
				}
			}
			throw new NullReferenceException("This IP address is not currently connected");
		}

		/// <summary>
		/// Get all connected clients
		/// </summary>
		public TCPClientInfo[] ConnectedClients => connectedClients.Select(s => s.Value.infoAboutOtherSide).ToArray();

		#endregion

		#region Listen for client connection

		/// <summary>
		/// Set listening for incoming data from connected client 'clientID'
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SetListeningForData(byte clientID, bool state) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].ListeningForData = state;
				connectedClients[clientID].DataReception();
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

		private void ListenForClientConnection(IPAddress address, ushort port) {
			clientConnectionListener = new TcpListener(address, port);
			clientConnectionListener.Start();
			serverStartEvnt.Set();
			TcpClient newlyConnected = null;
			while (listenForClientConnections) {
				try {
					newlyConnected = clientConnectionListener.AcceptTcpClient();
				}
				catch (SocketException e) {
					if (e.SocketErrorCode == SocketError.Interrupted) {
						listenForClientConnections = false;
						return;
					}
				}
				byte ID = (byte)(connectedClients.Count + 1);
				NetworkStream newStream = newlyConnected.GetStream();
				newStream.Write(new byte[] { ID }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				byte[] clientInfo = new byte[1024];

				int bytesRead = newStream.Read(clientInfo, 0, clientInfo.Length);
				TCPClientInfo connectedClientInfo = (TCPClientInfo)SimpleTCPHelper.GetObject(typeof(TCPClientInfo), clientInfo);
				TCPClientInfo serverInfo = new TCPClientInfo("Server", true, SimpleTCPHelper.GetActiveIPv4Address()) {
					ClientID = 0
				};
				ServerToClientConnection conn = new ServerToClientConnection(newlyConnected, serverInfo, connectedClientInfo, this);
				conn.dataIDs.rerouter = this;
				conn.dataIDs.OnRerouteRequest += DataIDs_OnRerouteRequest;
				conn._OnClientDisconnected += ClientDisconnected;
				connectedClients.Add(ID, conn);
				OnClientConnected?.Invoke(this, new ClientConnectedEventArgs(this, connectedClientInfo));
			}
		}

		#endregion

		#region Private Events

		private void ClientDisconnected(object sender, ClientDisconnectedEventArgs e) {
			OnClientDisconnected?.Invoke(this, e);
			connectedClients.Remove(e.ClientInfo.ClientID);
		}

		private void DataIDs_OnRerouteRequest(object sender, DataReroutedEventArgs e) {
			if (connectedClients.ContainsKey(e.forwardedClient)) {
				connectedClients[e.forwardedClient].SendData(e.packetID, e.originClient, e.data);
				OnDataRerouted?.Invoke(this, e);
				return;
			}
			throw new NullReferenceException("Client with ID " + e.forwardedClient + " is not connected to the server!");
		}

		#endregion

		#region Property Synchronization

		/// <summary>
		/// Define 'propertySyncPacketID' for synchronization of public property for client 'clientID' named 'propetyName' from instance of a class 'instance', publish changes by calling UpdateProp()
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void SyncProperty(byte clientID, object instance, string propertyName, byte propertySyncPacketID) {
			if (connectedClients.ContainsKey(clientID)) {
				connectedClients[clientID].dataIDs.syncedProperties.Add(propertySyncPacketID, new PropertySynchronization(propertySyncPacketID, instance, propertyName));
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		/// <summary>
		/// Sends updated property value to connected 'clientID' with property id 'ID' and set value
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void UpdateProp(byte clientID, byte ID, object value) {
			if (connectedClients.ContainsKey(clientID)) {
				byte[] rawData = SimpleTCPHelper.GetBytesFromObject(value);
				byte[] merged = new byte[rawData.Length + 1];
				merged[0] = ID;
				rawData.CopyTo(merged, 1);
				connectedClients[clientID].SendData(DataIDs.PropertySyncID, SERVER_PACKET_ORIGIN_ID, merged);
				return;
			}
			throw new NullReferenceException("Client with ID " + clientID + " is not connected to the server!");
		}

		#endregion

		#region Communication Definitions

		/// <summary>
		/// Provide a value to all connected clients
		/// </summary>
		public void ProvideValue<T>(byte packetID, Func<T> function) {
			foreach (TCPClientInfo info in ConnectedClients) {
				if (connectedClients[info.ClientID].dataIDs.IsIDReserved(packetID, out Type dataType, out string message)) {
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
		public void DefineCustomPacket<TData>(byte clientID, byte packetID, Action<byte, TData> callback) {
			if (!typeof(TData).IsSerializable) {
				throw new InvalidOperationException($"Attempting to define packet for type {typeof(TData).FullName}, but it is not marked [Serializable]");
			}
			GetConnection(clientID).dataIDs.DefineCustomPacket(packetID, callback);
		}

		#endregion

		#region Send To All custom data, string, and long

		/// <summary>
		/// Send custom data to all connected clients.
		/// </summary>
		/// <exception cref="UndefinedPacketEventArgs"></exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException"></exception>
		public void SendToAll<TData>(byte packetID, TData data) {
			foreach (ServerToClientConnection info in connectedClients.Values) {
				info.SendData(packetID, info.infoAboutOtherSide.ClientID, SimpleTCPHelper.GetBytesFromObject(data));
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
		private bool disposedValue = false; // To detect redundant calls

		/// <summary>
		/// Dispose of the object
		/// </summary>
		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {

				}
				serverStartEvnt.Dispose();
				disposedValue = true;
			}
		}

		/// <summary>
		/// Destructor
		/// </summary>
		~TCPServer() {
			Dispose(false);
		}

		/// <summary>
		/// Dispose of the object
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
