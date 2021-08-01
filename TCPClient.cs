using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using SimpleTCP.Connections;
using SimpleTCP.Connections.Interfaces;
using SimpleTCP.Events;
using SimpleTCP.Exceptions;
using SimpleTCP.Structures;

namespace SimpleTCP {
	/// <summary>
	/// Client that can connect to a TCPServer
	/// </summary>
	public class TCPClient : IValueProvider, IDisposable {
		private readonly IPAddress address;
		private readonly ushort port;

		private readonly PostConnectSetup postConnectHandler;

		Dictionary<byte, Delegate> IValueProvider.ProvidedValues { get; } = new();

		/// <summary>
		/// Information about this client
		/// </summary>
		public TCPClientInfo Info { get; set; }

		public ClientConfiguration Configuration { get; }

		/// <summary>
		/// Get connection to the server, allows client to server communication, holds send and receive functionality
		/// </summary>
		public ClientToServerConnection? Connection { get; private set; }

		/// <summary>
		/// Is the client connected to a server
		/// </summary>
		public bool IsConnected => Connection != null;

		/// <summary>
		/// Event called whenever a synchronization packet is received
		/// </summary>
		public event EventHandler<OnPropertySynchronizationEventArgs>? OnPropertySynchronized;

		/// <summary>
		/// Event called whenever server disconnects this client
		/// </summary>
		public event EventHandler? OnClientDisconnected;

		/// <summary>
		/// Initialize new <see cref="TCPClient"/>
		/// </summary>
		/// <param name="ipAddress">The IP address to connect to</param>
		/// <param name="port">the port which to use</param>
		/// <param name="config">optional configuration for custom serialization of types</param>
		/// <exception cref="WebException"></exception>
		public TCPClient(string ipAddress, ushort port, ClientConfiguration? config = null)
			: this(new ConnectionData(ipAddress, port), config) { }

		/// <summary>
		/// Initialize new <see cref="TCPClient"/>
		/// </summary>
		/// <param name="ipAddress">The IP address to connect to</param>
		/// <param name="port">the port which to use</param>
		/// <param name="config">optional configuration for custom serialization of types</param>
		/// <exception cref="WebException"></exception>
		public TCPClient(IPAddress ipAddress, ushort port, ClientConfiguration? config = null)
			: this(new ConnectionData(ipAddress.ToString(), port), config) { }

		/// <summary>
		/// Initialize new <see cref="TCPClient"/> using a <see cref="ConnectionData"/> class
		/// </summary>
		/// <exception cref="WebException"></exception>
		public TCPClient(ConnectionData data, ClientConfiguration? config) {
			port = data.Port;
			if (IPAddress.TryParse(data.IPAddress, out address)) {
#if DEBUG
				Console.WriteLine("IP parsed successfully");
#endif
			}
			else {
				throw new WebException("Entered Invalid IP Address!", WebExceptionStatus.ConnectFailure);
			}
			Info = new TCPClientInfo(Environment.UserName, false, SimpleTCPHelper.GetActiveIPv4Address().ToString());
			Configuration = config ?? new ClientConfiguration();
			postConnectHandler = new PostConnectSetup(this);
		}

		public async Task<bool> ConnectAsync(int timeout) {
			TcpClient clientBase = new();
			try {
				Task t1 = clientBase.ConnectAsync(address, port);
				Task t2 = Task.Delay(timeout);

				if (t2 == await Task.WhenAny(t1, t2)) {
					return false;
				}

				byte[] buffer = new byte[DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY];

				NetworkStream stream = clientBase.GetStream();
				await stream.ReadAsync(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);

				Info.ID = buffer[0];

				byte[] clientInfoArray = SimpleTCPHelper.GetBytesFromObject(Info, Configuration);
				byte[] header = BitConverter.GetBytes(clientInfoArray.LongLength);
				byte[] data = new byte[header.Length + clientInfoArray.Length];
				header.CopyTo(data, 0);
				clientInfoArray.CopyTo(data, header.Length);

				await stream.WriteAsync(data, 0, data.Length);

				TCPClientInfo serverInfo = new("Server", true, address.ToString()) {
					ID = 0
				};

				await stream.ReadAsync(buffer, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);
				bool accepted = buffer[0] == Info.ID;

				if (accepted) {
					Connection = new ClientToServerConnection(clientBase, serverInfo, this, Info, Configuration, OnClientDisconnected,
															  OnStringEventInvoke, OnLongEventInvoke, OnUnknownPacketEventInvoke);
					Connection.OnStringReceived += OnStringReceived;
					Connection.OnInt64Received += OnInt64Received;
					Connection.OnUndefinedPacketReceived += OnUndefinedPacketReceived;
					postConnectHandler.Setup();
				}
#if DEBUG
				Console.WriteLine($"Connection {(accepted ? "Established" : "Rejected")}");
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
			Info = new TCPClientInfo(clientName, false, address.ToString());
			return Info;
		}

		/// <summary>
		/// Define 'propertyPacketID' for synchronization of public property named 'propertyName' from instance of a class 'instance' 
		/// </summary>
		/// <exception cref="InvalidOperationException">The property is not publicly visible</exception>
		/// <exception cref="ArgumentException">The property name does not exist</exception>
		public void SyncProperty(object instance, string propertyName, byte propertyPacketID) {
			if (!IsConnected) {
				postConnectHandler.AddPropSync(instance, propertyName, propertyPacketID);
				return;
			}
			Connection!.dataIDs.syncedProperties.Add(propertyPacketID, new PropertySynchronization(propertyPacketID, instance, propertyName));
		}

		#region Communication Definitions

		/// <summary>
		/// Provide a value to the server
		/// </summary>
		public void ProvideValue<T>(byte packetID, Func<T> function) {
			if (!IsConnected) {
				postConnectHandler.AddProvideValue(packetID, function);
				return;
			}
			(this as IValueProvider).ProvidedValues.Add(packetID, function);
			Connection!.dataIDs.responseFunctionMap.Add(packetID, function);
			Connection!.dataIDs.requestTypeMap.Add(packetID, typeof(T));
		}

		/// <summary>
		/// Request a provided value from the server
		/// </summary>
		/// <exception cref="NoResponseException">The server returns an unexpected response (e.g. the type is not compatible)</exception>
		/// <exception cref="InvalidOperationException">The client is NOT connected to a server</exception>
		public async Task<T> GetValue<T>(byte packetID) {
			if (!IsConnected) throw new InvalidCastException("The client is not connected to a server therefore cannot request a value!");

			TCPResponse resp = await Connection!.requestCreator.Request(packetID, typeof(T));
			if (resp.DataType == typeof(NoResponseException)) {
				throw new NoResponseException(resp);
			}
			return (T)resp.GetObject;
		}

		#region SendData wrappers

		/// <summary>
		/// Send user defined byte[] 'data' under 'packetID'
		/// </summary>
		/// <exception cref="InvalidOperationException">The packet ID is not defined</exception>
		/// <exception cref="NotConnectedException">The client is not connected to a server</exception>
		public void SendData(byte packetID, byte[] data) {
			if (!IsConnected) throw new NotConnectedException();
			Connection!.SendData(packetID, data);
		}

		/// <summary>
		/// Send custom data type using internal serialization/deserialization mechanisms
		/// </summary>
		/// <exception cref="InvalidOperationException">The data is not marked as [Serializable]</exception>
		/// <exception cref="System.Runtime.Serialization.SerializationException">When data fails to serialize</exception>
		/// <exception cref="ArgumentNullException">The data to send is <see langword="null"/></exception>
		/// <exception cref="NotConnectedException">The client is not connected to a server</exception>
		public void SendData<TData>(byte packetID, TData data) {
			if (!IsConnected) throw new NotConnectedException();
			Connection!.SendData(packetID, data);
		}

		/// <summary>
		/// Send UTF-8 encoded string to connected device
		/// </summary>
		/// <exception cref="NotConnectedException">The client is not connected to a server</exception>
		public void SendData(string stringData) {
			if (!IsConnected) throw new NotConnectedException();
			Connection!.SendData(stringData);
		}

		/// <summary>
		/// Send Int64 (long) to connected device
		/// </summary>
		/// <exception cref="NotConnectedException">The client is not connected to a server</exception>
		public void SendData(long longData) {
			if (!IsConnected) throw new NotConnectedException();
			Connection!.SendData(longData);
		}

		#endregion

		#region EventHandler wrappers

		/// <summary>
		/// Called when successfully received data by <see cref="DataIDs.STRING_ID"></see> marked packet
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<string>>? OnStringReceived;

		private void OnStringEventInvoke(object sender, PacketReceivedEventArgs<string> args) {
			OnStringReceived?.Invoke(this, args);
		}

		/// <summary>
		/// Called when successfully received data marked as a <see cref="Int64"/>
		/// </summary>
		public event EventHandler<PacketReceivedEventArgs<Int64>>? OnInt64Received;

		private void OnLongEventInvoke(object sender, PacketReceivedEventArgs<long> args) {
			OnInt64Received?.Invoke(this, args);
		}

		/// <summary>
		/// Called when receiving a packet ID that is not defined internally and in the user's packet list
		/// </summary>
		public event EventHandler<UndefinedPacketEventArgs>? OnUndefinedPacketReceived;

		private void OnUnknownPacketEventInvoke(object sender, UndefinedPacketEventArgs args) {
			OnUndefinedPacketReceived?.Invoke(this, args);
		}

		#endregion

		/// <summary>
		/// Register custom packet with ID that will carry a TData type, delivered via the callback
		/// </summary>
		/// <exception cref="InvalidOperationException">The data is not marked as [Serializable] and is not defined in custom serialization rules</exception>
		public void DefineCustomPacket<TData>(byte packetID, Action<byte, TData> callback) {
			if (!typeof(TData).IsSerializable && !Configuration.ContainsSerializationRule(typeof(TData))) {
				throw new InvalidOperationException($"Attempting to define packet for type {typeof(TData).FullName}, but it is not marked [Serializable]");
			}
			if (!IsConnected) {
				postConnectHandler.AddCustomPacketDef(packetID, callback);
				return;
			}
			Connection!.dataIDs.DefineCustomPacket(packetID, callback);
		}

		#endregion

		/// <summary>
		/// Disconnect from current server and dispose of this client
		/// </summary>
		/// <exception cref="InvalidOperationException">The client is not connected to the server</exception>
		public void Disconnect() {
			if (IsConnected) {
				Connection!.DisconnectFromServer();
				Connection = null;
			}
			else {
				throw new InvalidOperationException("Attempting to disconnect from server while this client is not connected to anything...");
			}
		}

		internal void InvokeOnPropertySync(ClientToServerConnection con, OnPropertySynchronizationEventArgs args) {
			OnPropertySynchronized?.Invoke(con, args);
		}

		public void Dispose() {
			if (IsConnected) {
				Connection!.Dispose();
			}
		}
	}
}