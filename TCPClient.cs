using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Igor.TCP {
	/// <summary>
	/// Client that can connect to a TCPServer
	/// </summary>
	public class TCPClient : IValueProvider {
		private readonly IPAddress address;
		private readonly ushort port;

		Dictionary<byte, Delegate> IValueProvider.providedValues { get; } = new Dictionary<byte, Delegate>();

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
		/// Event called whenever a synchronization packet is received
		/// </summary>
		public event EventHandler<OnPropertySynchronizationEventArgs> OnPropertySynchronized;

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

			TCPClientInfo serverInfo = new TCPClientInfo("Server", true, address) {
				clientID = 0
			};
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
			set {
				if(getConnection.listeningForData == value) {
					return;
				}
				getConnection.listeningForData = value;
				if (value) {
					getConnection.DataReception();
				}
			}
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property named 'propetyName' from instance of a class 'instance' 
		/// </summary>
		public void SyncProperty(object instance, string propertyName, byte propertyPacketID) {
			getConnection.dataIDs.syncedProperties.Add(propertyPacketID, new PropertySynchronization(propertyPacketID, instance, propertyName));
		}

		#region Communication Definitions

		/// <summary>
		/// Provide a value to all connected clients
		/// </summary>
		public void ProvideValue<T>(byte packetID, Func<T> function) {
			(this as IValueProvider).providedValues.Add(packetID, function);
			getConnection.dataIDs.responseFunctionMap.Add(packetID, function);
			getConnection.dataIDs.requestTypeMap.Add(packetID, typeof(T));
		}

		/// <summary>
		/// Request a value from a client
		/// </summary>
		public async Task<T> GetValue<T>(byte packetID) {
			TCPResponse resp = await getConnection.requestCreator.Request(packetID);
			return (T)resp.getObject;
		}

		/// <summary>
		/// Register custom packet with ID that will carry a TData type, delivered via the callback
		/// </summary>
		public void DefineCustomPacket<TData>(byte packetID, Action<byte, TData> callback) {
			if (!typeof(TData).IsSerializable) {
				throw new InvalidOperationException($"Attempting to define packet for type {typeof(TData).FullName}, but it is not marked [Serializable]");
			}
			getConnection.dataIDs.DefineCustomPacket(packetID, callback);
		}

		#endregion

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
				throw new NullReferenceException("Attempting to disconnect from server while this client is not connected to anything...");
			}
		}

		internal void InvokeOnPropertySync(ClientToServerConnection con, OnPropertySynchronizationEventArgs args) {
			OnPropertySynchronized?.Invoke(con, args);
		}
	}
}