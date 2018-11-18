using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;

namespace Igor.TCP {
	/// <summary>
	/// Class holding packet IDs and received data resolution and unpacking
	/// </summary>
	public class DataIDs {

		#region Constant byte IDs and sizes of packets

		/// <summary>
		/// Packet ID for string
		/// </summary>
		public const byte StringID = 0;

		/// <summary>
		/// Packet ID for Int64 (long)
		/// </summary>
		public const byte Int64ID = 1;

		/// <summary>
		/// Packet ID for handling requests
		/// </summary>
		internal const byte RequestReceptionID = 250;

		/// <summary>
		/// Packet ID for handling responses to requests
		/// </summary>
		internal const byte ResponseReceptionID = 251;

		/// <summary>
		/// Packet ID for handling property synchronization
		/// </summary>
		internal const byte PropertySyncID = 252;

		/// <summary>
		/// Packet ID for sending client information to the other side
		/// </summary>
		public const byte ClientInformationID = 253;

		/// <summary>
		/// Packet ID to signalize client disconnect
		/// </summary>
		public const byte ClientDisconnected = 254;

		/// <summary>
		/// NIY, no guarantee of safety when using this TODO
		/// </summary>
		public const byte ServerStop = 255;

		/// <summary>
		/// How many bytes are used to identify a client
		/// </summary>
		public const byte CLIENT_IDENTIFICATION_COMPLEXITY = 1;

		/// <summary>
		/// How many bytes are used to identify a packet
		/// </summary>
		public const byte PACKET_ID_COMPLEXITY = 1;

		/// <summary>
		/// How many bytes are used to describe packets size, maximum is now 2^8 bytes
		/// </summary>
		public const byte PACKET_TOTAL_SIZE_COMPLEXITY = 8;

		#endregion

		internal readonly Dictionary<byte, Tuple<Type, Delegate>> customIDs = new Dictionary<byte, Tuple<Type, Delegate>>();
		internal readonly Dictionary<byte, Tuple<object, PropertyInfo>> syncedProperties = new Dictionary<byte, Tuple<object, PropertyInfo>>();


		internal readonly Dictionary<byte, Type> requestTypeMap = new Dictionary<byte, Type>();
		internal readonly Dictionary<byte, Delegate> responseFunctionMap = new Dictionary<byte, Delegate>();


		internal static readonly Dictionary<byte, List<ReroutingInfo>> rerouter = new Dictionary<byte, List<ReroutingInfo>>();

		private readonly BinaryFormatter bf = new BinaryFormatter();
		private ResponseManager responseManager;

		internal event EventHandler<DataReroutedEventArgs> OnRerouteRequest;

		internal DataIDs(TCPConnection connection) {
			responseManager = new ResponseManager(connection);
		}

		internal Type IndetifyID(byte ID, byte fromClient, byte[] data, out object dataObj) {
			if (Reroute(ID, fromClient, data)) {
				dataObj = null;
				return null;
			}

			switch (ID) {
				case StringID: {
					dataObj = System.Text.Encoding.UTF8.GetString(data);
					return typeof(string);
				}
				case Int64ID: {
					dataObj = BitConverter.ToInt64(data, 0);
					return typeof(Int64);
				}
				case RequestReceptionID: {
					byte requestID = data[0];
					TCPRequest request = new TCPRequest(requestID);

					if (!responseFunctionMap.ContainsKey(requestID)) {
						throw new NotImplementedException(string.Format("Server is requesting response for '{0}' byteID, but no such ID is defined!", request.packetID));
					}
					//if((responseManager.connection as ClientToServerConnection)?.HigherLevelDataReceived(new ReceivedData(typeof(TCPRequest),fromClient,ID,null))
					object obj = responseFunctionMap[requestID].DynamicInvoke(null);

					if (obj is byte[]) {
						responseManager.HandleRequest(request, obj as byte[]);
					}
					else {
						responseManager.HandleRequest(request, obj);
					}
					dataObj = null;
					return typeof(TCPRequest);
				}
				case ResponseReceptionID: {
					TCPResponse response = new TCPResponse(data[0], new byte[data.Length - 1], responseManager.GetResponseType(data[0]));
					Array.Copy(data, 1, response.rawData, 0, data.Length - 1);
					dataObj = response;
					return typeof(TCPResponse);
				}
				case PropertySyncID: {
					byte[] realData = new byte[data.Length - 1];
					Array.Copy(data, 1, realData, 0, realData.Length);
					syncedProperties[data[0]].Item2.SetValue(syncedProperties[data[0]].Item1, Helper.GetObject(syncedProperties[data[0]].Item2.PropertyType, realData));
					dataObj = null;
					return typeof(TCPRequest);
				}
				case ClientDisconnected: {
					dataObj = data;
					return typeof(TCPClient);
				}
				default: {
					if (customIDs.ContainsKey(ID)) {
						Type t = customIDs[ID].Item1;
						dataObj = Helper.GetObject(t, data);
						customIDs[ID].Item2.DynamicInvoke(dataObj, fromClient);
						return null;
					}
					throw new NotSupportedException(string.Format("This identifier is not supported '{0}'", ID));
				}
			}
		}


		private bool Reroute(byte ID, byte fromClient, byte[] data) {
			if (rerouter.ContainsKey(ID)) {
				ReroutingInfo info = null;
				for (int i = 0; i < rerouter[ID].Count; i++) {
					if (rerouter[ID][i].fromClient == fromClient) {
						info = rerouter[ID][i];
					}
				}
				if (info == null) {
					return false;
				}
				OnRerouteRequest?.Invoke(this, new DataReroutedEventArgs(info.toClient, (info.isUserDefined ? info.dataID : info.packetID), data, info.isUserDefined));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Register custom packet with 'ID' that will carry data of 'TData' type, delivered via 'callback' event
		/// </summary>
		public void DefineCustomDataTypeForID<TData>(byte ID, Action<TData, byte> callback) {
			customIDs.Add(ID, new Tuple<Type, Delegate>(typeof(TData), callback));
		}

		/// <summary>
		/// Remove previously registered custom packet by 'ID'
		/// </summary>
		public void RemoveCustomDefinitionForID(byte ID) {
			customIDs.Remove(ID);
		}

		internal static void AddToReroute(byte packetID, ReroutingInfo info) {
			if (!rerouter.ContainsKey(packetID)) {
				rerouter.Add(packetID, new List<ReroutingInfo>() { info });
			}
			else {
				rerouter[packetID].Add(info);
			}
		}
	}
}