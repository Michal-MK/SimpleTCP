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

		Dictionary<byte, Delegate> IValueProvider.ProvidedValues { get; } = new Dictionary<byte, Delegate>();

		/// <summary>
		/// Information about this client
		/// </summary>
		public TCPClientInfo ClientInfo { get; set; }

		/// <summary>
		/// Get connection to the server, allows client to server communication, holds send and receive functionality
		/// </summary>
		public ClientToServerConnection Connection { get; private set; }

		/// <summary>
		/// Event called whenever a synchronization packet is received
		/// </summary>
		public event EventHandler<OnPropertySynchronizationEventArgs> OnPropertySynchronized;

		/// <summary>
		/// Initialize new <see cref="TCPClient"/> with an 'ipAddress' and a 'port'
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(string ipAddress, ushort port)
			: this(new ConnectionData(ipAddress, port)) {
		}

		/// <summary>
		/// Initialize new <see cref="TCPClient"/> with an 'ipAddress' and a 'port'
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(IPAddress ipAddress, ushort port)
			: this(new ConnectionData(ipAddress.ToString(), port)) {
		}

		/// <summary>
		/// Initialize new <see cref="TCPClient"/> using a <see cref="ConnectionData"/> class
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(ConnectionData data) {
			port = data.Port;
			if (IPAddress.TryParse(data.IPAddress, out address)) {
#if DEBUG
				Console.WriteLine("IP parsed successfully");
#endif
			}
			else {
				throw new WebException("Entered Invalid IP Address!", WebExceptionStatus.ConnectFailure);
			}
			ClientInfo = new TCPClientInfo(Environment.UserName, false, SimpleTCPHelper.GetActiveIPv4Address());
		}

		/// <summary>
		/// Connect to server with specified IP and port
		/// </summary>
		public void Connect(Action OnConnected) {
			TcpClient clientBase = new TcpClient();
			clientBase.Connect(address, port);
			byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];
			NetworkStream stream = clientBase.GetStream();
			stream.Read(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
			ClientInfo.ClientID = buffer[0];

			byte[] clientInfoArray = SimpleTCPHelper.GetBytesFromObject(ClientInfo);

			stream.Write(clientInfoArray, 0, clientInfoArray.Length);

			TCPClientInfo serverInfo = new TCPClientInfo("Server", true, address) {
				ClientID = 0
			};
			Connection = new ClientToServerConnection(clientBase, ClientInfo, serverInfo, this);
#if DEBUG
			Console.WriteLine("Connection Established");
#endif
			OnConnected?.Invoke();
		}


		/// <summary>
		/// Quick setup of client meta-data
		/// </summary>
		/// <param name="clientName">If left empty Current user name is used</param>
		public TCPClientInfo SetUpClientInfo(string clientName) {
			ClientInfo = new TCPClientInfo(clientName, false, address);
			return ClientInfo;
		}


		/// <summary>
		/// Set listening for incoming data from the server
		/// </summary>
		public bool IsListeningForData {
			get { return Connection.ListeningForData; }
			set {
				if (Connection.ListeningForData == value) {
					return;
				}
				Connection.ListeningForData = value;
				if (value) {
					Connection.DataReception();
				}
			}
		}

		/// <summary>
		/// Define 'propertyPacketID' for synchronization of public property named 'propetyName' from instance of a class 'instance' 
		/// </summary>
		public void SyncProperty(object instance, string propertyName, byte propertyPacketID) {
			Connection.dataIDs.syncedProperties.Add(propertyPacketID, new PropertySynchronization(propertyPacketID, instance, propertyName));
		}

		#region Communication Definitions

		/// <summary>
		/// Provide a value to the server
		/// </summary>
		public void ProvideValue<T>(byte packetID, Func<T> function) {
			(this as IValueProvider).ProvidedValues.Add(packetID, function);
			Connection.dataIDs.responseFunctionMap.Add(packetID, function);
			Connection.dataIDs.requestTypeMap.Add(packetID, typeof(T));
		}

		/// <summary>
		/// Request a provided value from the server
		/// </summary>
		/// <exception cref="NoResponseException"></exception>
		public async Task<T> GetValue<T>(byte packetID) {
			TCPResponse resp = await Connection.requestCreator.Request(packetID, typeof(T));
			if(resp.DataType == typeof(NoResponseException)) {
				throw new NoResponseException(resp);
			}
			return (T)resp.GetObject;
		}

		/// <summary>
		/// Register custom packet with ID that will carry a TData type, delivered via the callback
		/// </summary>
		public void DefineCustomPacket<TData>(byte packetID, Action<byte, TData> callback) {
			if (!typeof(TData).IsSerializable) {
				throw new InvalidOperationException($"Attempting to define packet for type {typeof(TData).FullName}, but it is not marked [Serializable]");
			}
			Connection.dataIDs.DefineCustomPacket(packetID, callback);
		}

		#endregion

		/// <summary>
		/// Disconnect from current server and dispose of this client
		/// </summary>
		/// <exception cref="NullReferenceException"></exception>
		public void Disconnect() {
			if (Connection != null) {
				Connection.DisconnectFromServer(ClientInfo.ClientID);
				Connection = null;
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