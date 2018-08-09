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
	public class TCPClient : TCPConnection{
		private readonly IPAddress address;
		private readonly ushort port;

		private TcpClient server;


		internal RequestManager requestHandler { get; }
		public ResponseManager responseHandler { get; }


		public bool isListeningForData { get { return listeningForData; } }

		/// <summary>
		/// Initialize new TCPClient by connectiong to 'ipAddress' on port 'port'
		/// </summary>
		public TCPClient(string ipAddress, ushort port) : this(
			new ConnectionData(ipAddress, port)) {
		}

		/// <summary>
		/// Initialize new TCPClient by connectiong to a server defined in 'data'
		/// </summary>
		public TCPClient(ConnectionData data) : base(false) {
			this.port = data.port;
			if (IPAddress.TryParse(data.ipAddress, out address)) {
				server = new TcpClient();
				server.Connect(address, port);
				stream = server.GetStream();
				requestHandler = new RequestManager(this);
				responseHandler = new ResponseManager(dataIDs);
				new Thread(new ThreadStart(DataReception)) { Name = "DataReception" }.Start();
				listeningForData = true;

#if UNITY_ANDROID || UNITY_STANDALONE
				Debug.Log("Connection Established");
#else
				Console.WriteLine("Connection Established");
#endif
			}
			else {
				throw new Exception("Entered Invalid IP Address!");
			}
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property named 'propetyName' from instance of a class 'instance' 
		/// </summary>
		public void SyncPropery(object instance, string propertyName, byte propID) {
			PropertyInfo info = instance.GetType().GetProperty(propertyName);
			dataIDs.syncProps.Add(propID, new Tuple<object, PropertyInfo>(instance, info));
		}


		/// <summary>
		/// NIY, no guarantee of safety when using this TODO
		/// </summary>
		//TODO
		public void UpdatedProp(byte id, object value) {
			SendData(DataIDs.PropertySyncID, id, Helper.GetBytesFromObject<object>(value));
		}

		/// <summary>
		/// NIY, no guarantee of safety when using this TODO
		/// </summary>
		//TODO
		public void DefineRequestResponseEntry<TData>(byte ID, Func<TData> function) {
			dataIDs.requestDict.Add(ID, typeof(TData));
			dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// NIY, no guarantee of safety when using this TODO
		/// </summary>
		//TODO
		public void CancelRequestResponseID(byte ID) {
			dataIDs.requestDict.Remove(ID);
			dataIDs.responseDict.Remove(ID);
		}

		/// <summary>
		/// Define custom request by specifying its 'TData' type with selected 'ID'
		/// </summary>
		public void DefineRequestEntry<TData>(byte ID) {
			dataIDs.requestDict.Add(ID, typeof(TData));
		}

		/// <summary>
		/// Cancel custom request of 'TData' under 'ID'
		/// </summary>
		public void CancelRequestID(byte ID) {
			dataIDs.requestDict.Remove(ID);
		}

		/// <summary>
		/// Define response 'function' to be called when request packet with 'ID' is received
		/// </summary>
		public void DefineResponseEntry<TData>(byte ID, Func<TData> function) {
			dataIDs.responseDict.Add(ID, function);
		}

		/// <summary>
		/// Cancel response to request with 'ID'
		/// </summary>
		public void CancelResponseID(byte ID) {
			dataIDs.responseDict.Remove(ID);
		}

		/// <summary>
		/// Raises a new request with 'ID' and sends response via 'OnRequestHandeled' event
		/// </summary>
		public async Task<TCPResponse> RaiseRequestAsync(byte ID) {
			TCPResponse data = await requestHandler.Request(ID);
			return data;
			//OnRequestHandeled?.Invoke(ID, data);
		}

		/// <summary>
		/// Stops listening for incomming data
		/// </summary>
		public void StopListening() {
			listeningForData = false;
		}
	}
}