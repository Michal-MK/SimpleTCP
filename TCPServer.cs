using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

#if UNITY_STANDALONE || UNITY_ANDROID
using UnityEngine;
#endif

namespace Igor.TCP {
	/// <summary>
	/// TCP Server
	/// </summary>
	public class TCPServer : TCPConnection {

		private TcpClient connected;

		internal RequestManager requestHandler { get; }

		/// <summary>
		/// Access to datatypes for custom packets
		/// </summary>
		public ResponseManager responseHandler { get; }

		/// <summary>
		/// Called when cliet connects to this server
		/// </summary>
		public event EventHandler<TCPServer> OnConnectionEstablished;

		/// <summary>
		/// Initialize new Server
		/// </summary>
		public TCPServer() : base(true) {
			requestHandler = new RequestManager(this);
			responseHandler = new ResponseManager(dataIDs);
		}

		/// <summary>
		/// Start server using specified 'port' and internally found IP
		/// </summary>
		public void Start(ushort port) {
			Thread t = new Thread(() => { StartServer(Helper.GetActivePIv4Address(), port); }) { Name = "Actual server" };
			t.Start();
		}

		/// <summary>
		/// Start server using specified 'port' and explicitly specified 'ipAddress'
		/// </summary>
		public void Start(string ipAddress, ushort port) {
			Thread t = new Thread(() => { StartServer(IPAddress.Parse(ipAddress), port); }) { Name = "Actual server" };
			t.Start();
		}

		/// <summary>
		/// Stops listening for incomming data
		/// </summary>
		public void StopListening() {
			listeningForData = false;
		}


		private void StartServer(IPAddress address, ushort port) {
			Console.WriteLine(address);
			TcpListener listener = new TcpListener(address, port);
			listener.Start();
			connected = listener.AcceptTcpClient();
			stream = connected.GetStream();
			Console.WriteLine("Client connected");
			listeningForData = true;
			OnConnectionEstablished?.Invoke(this, this);
			new Thread(new ThreadStart(DataReception)) { Name = "DataReception" }.Start();
		}

		/// <summary>
		/// Define 'propID' for synchronization of public property named 'propetyName' from instance of a class 'instance', publish change by calling UpdateProp()
		/// </summary>
		public void SyncPropery<TProp>(object instance, TProp property, byte propID) {
			PropertyInfo info = property.GetType().GetProperty(property.GetType().Name, typeof(TProp));
			dataIDs.syncProps.Add(propID, new Tuple<object, PropertyInfo>(instance, info));
		}

		/// <summary>
		/// Sends updated property value to connected client
		/// </summary>
		public void UpdateProp(byte id, object value) {
			SendData(DataIDs.PropertySyncID, id, Helper.GetBytesFromObject<object>(value));
		}

		/// <summary>
		/// NIY, no guarantee of safety when using this 
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
			return await requestHandler.Request(ID);
			//OnRequestHandeled?.Invoke(ID, data);
		}
	}
}
