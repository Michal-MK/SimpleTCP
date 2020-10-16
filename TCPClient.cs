using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
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
			ClientInfo = new TCPClientInfo(Environment.UserName, false, SimpleTCPHelper.GetActiveIPv4Address().ToString());
		}

		public async Task<bool> ConnectAsync(int timeout) {
			TcpClient clientBase = new TcpClient();
			try {
				Task t1 = Task.Run(async () => await clientBase.ConnectAsync(address, port));
				Task t2 = Task.Delay(timeout);
				await Task.WhenAny(t1, t2);

				if (t2.Status == TaskStatus.RanToCompletion &&
					(t1.Status == TaskStatus.Faulted || t1.Status == TaskStatus.Canceled || t1.Status == TaskStatus.Running || t1.Status == TaskStatus.WaitingForActivation)) {
					return false;
				}
				else {
					await t1;
				}

				byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

				NetworkStream stream = clientBase.GetStream();
				await stream.ReadAsync(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);

				ClientInfo.ClientID = buffer[0]; // TODO smarter if length != 1

				byte[] clientInfoArray = SimpleTCPHelper.GetBytesFromObject(ClientInfo);
				byte[] header = BitConverter.GetBytes(clientInfoArray.LongLength);
				byte[] data = new byte[header.Length + clientInfoArray.Length];
				header.CopyTo(data, 0);
				clientInfoArray.CopyTo(data, header.Length);

				await stream.WriteAsync(data, 0, data.Length);

				TCPClientInfo serverInfo = new TCPClientInfo("Server", true, address.ToString()) {
					ClientID = 0
				};

				await stream.ReadAsync(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				bool accepted = buffer[0] == ClientInfo.ClientID;

				if (accepted) {
					Connection = new ClientToServerConnection(clientBase, ClientInfo, serverInfo, this);
				}
#if DEBUG
				if (accepted) {
					Console.WriteLine("Connection Established");
				}
				else {
					Console.WriteLine("Connection Rejected");
				}
#endif
				return accepted;
			}
			catch {
				return false;
			}
		}


		/// <summary>
		/// Quick setup of client meta-data
		/// </summary>
		/// <param name="clientName">If left empty Current user name is used</param>
		public TCPClientInfo SetUpClientInfo(string clientName) {
			ClientInfo = new TCPClientInfo(clientName, false, address.ToString());
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
			if (resp.DataType == typeof(NoResponseException)) {
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