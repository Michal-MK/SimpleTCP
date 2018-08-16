using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Igor.TCP {
	/// <summary>
	/// TCP Server
	/// </summary>
	public class TCPServer {

		private Dictionary<byte, ConnectionInfo> connectedClients = new Dictionary<byte, ConnectionInfo>();

		internal bool listenForClientConnections = true;

		/// <summary>
		/// Called when client connects to this server
		/// </summary>
		public event EventHandler<ClientConnectedEventArgs> OnConnectionEstablished;

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		public void Start(ushort port) {
			StartServer(Helper.GetActivePIv4Address(), port);
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		public void Start(string ipAddress, ushort port) {
			if (IPAddress.TryParse(ipAddress, out IPAddress address)) {
				StartServer(IPAddress.Parse(ipAddress), port);
			}
			else {
				Console.WriteLine("Unable to parse ip address string '{0}'", ipAddress);
				return;
			}
		}

		/// <summary>
		/// Get client connection by ID
		/// </summary>
		public TCPConnection GetConnection(byte id) {
			return connectedClients[id].connection;
		}

		/// <summary>
		/// Get client connection by IP address
		/// </summary>
		public TCPConnection GetConnection(IPAddress address) {
			foreach (var item in connectedClients) {
				if (item.Value.connectedAddress == address) {
					return item.Value.connection;
				}
			}
			throw new InvalidOperationException("This IP address is not currently connected");
		}

		/// <summary>
		/// Get all connected clients
		/// </summary>
		public ConnectionInfo[] getConnectedClients {
			get {
				ConnectionInfo[] connections = new ConnectionInfo[connectedClients.Keys.Count];
				connectedClients.Values.CopyTo(connections, 0);
				return connections;
			}
		}


		/// <summary>
		/// Set listening for incomming data from connected client 'clientID'
		/// </summary>
		public void SetListeningForData(byte clientID, bool state) {
			connectedClients[clientID].connection.listeningForData = state;
		}

		/// <summary>
		/// Set listening for incomming client connection attempts
		/// </summary>
		public void SetIncommingClientConnection(bool state) {
			listenForClientConnections = state;
		}

		private void StartServer(IPAddress address, ushort port) {
			Console.WriteLine(address);
			new Thread(new ThreadStart(delegate () { ListenForClientConnection(address, port); })).Start();
		}

		private void ListenForClientConnection(IPAddress address, ushort port) {
			TcpListener listener = new TcpListener(address, port);
			listener.Start();
			while (listenForClientConnections) {
				TcpClient newlyConnected = listener.AcceptTcpClient();
				byte ID = (byte)connectedClients.Count;
				NetworkStream newStream = newlyConnected.GetStream();
				newStream.Write(new byte[] { ID }, 0, DataIDs.CLIENT_IDENTIFICATION_COMPLEXITY);

				TCPConnection connection = new TCPConnection(newlyConnected, true);
				connection.dataIDs.OnRerouteRequest += DataIDs_OnRerouteRequest;

				ConnectionInfo conn = new ConnectionInfo(((IPEndPoint)newlyConnected.Client.RemoteEndPoint).Address,
					ID,
					newlyConnected,
					connection);

				connectedClients.Add(ID, conn);
				OnConnectionEstablished?.Invoke(this, new ClientConnectedEventArgs(this, conn));
				new Thread(new ThreadStart(conn.connection.DataReception)) { Name = "DataReception" }.Start();
			}
		}

		private void DataIDs_OnRerouteRequest(object sender, DataReroutedEventArgs e) {
			GetConnection(e.forwardedClient).SendData(e.packetID, e.data);
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property for client 'clientID' named 'propetyName' from instance of a class 'instance', publish changes by calling UpdateProp()
		/// </summary>
		public void SyncPropery<TProp>(byte clientID, object instance, TProp property, byte propID) {
			PropertyInfo info = property.GetType().GetProperty(property.GetType().Name, typeof(TProp));
			connectedClients[clientID].connection.dataIDs.syncProps.Add(propID, new Tuple<object, PropertyInfo>(instance, info));
		}

		/// <summary>
		/// Sends updated property value to connected 'clientID' with property id 'ID' and set value;
		/// </summary>
		public void UpdateProp(byte clientID, byte ID, object value) {
			connectedClients[clientID].connection.SendData(DataIDs.PropertySyncID, ID, Helper.GetBytesFromObject<object>(value));
		}

		/// <summary>
		/// Shorthand for Request and response id definition, when both sides are senders 
		/// </summary>
		public void DefineTwoWayComunication<TData>(byte clientID, byte ID, Func<TData> function) {
			connectedClients[clientID].connection.dataIDs.requestDict.Add(ID, typeof(TData));
			connectedClients[clientID].connection.dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// Cancel two way comunication for client 'clientID' with id 'ID'
		/// </summary>
		public void CancelTwoWayComunication(byte clientID, byte ID) {
			connectedClients[clientID].connection.dataIDs.requestDict.Remove(ID);
			connectedClients[clientID].connection.dataIDs.responseDict.Remove(ID);
		}

		/// <summary>
		/// Define custom request by specifying its 'TData' type with selected 'ID'
		/// </summary>
		public void DefineRequestEntry<TData>(byte clientID, byte ID) {
			connectedClients[clientID].connection.dataIDs.requestDict.Add(ID, typeof(TData));
		}

		/// <summary>
		/// Cancel custom request of 'TData' under 'ID'
		/// </summary>
		public void CancelRequestID(byte clientID, byte ID) {
			connectedClients[clientID].connection.dataIDs.requestDict.Remove(ID);
		}

		/// <summary>
		/// Define response 'function' to be called when request packet with 'ID' is received
		/// </summary>
		public void DefineResponseEntry<TData>(byte clientID, byte ID, Func<TData> function) {
			connectedClients[clientID].connection.dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// Cancel response to request with 'ID'
		/// </summary>
		public void CancelResponseID(byte clientID, byte ID) {
			connectedClients[clientID].connection.dataIDs.responseDict.Remove(ID);
		}


		public void DefineRerouteID(byte fromClient, byte toClient, byte[] data) {

		}

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		public async Task<TCPResponse> RaiseRequestAsync(byte clientID, byte ID) {
			return await GetConnection(clientID).requestHandler.Request(ID);
		}
	}
}
