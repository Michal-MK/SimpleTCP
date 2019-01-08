using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

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


		internal const byte CLIENT_IDENTIFICATION_COMPLEXITY = 1;

		internal const byte PACKET_ID_COMPLEXITY = 1;

		internal const byte PACKET_TOTAL_SIZE_COMPLEXITY = 8;

		#endregion

		internal readonly Dictionary<byte, CustomPacket> customIDs = new Dictionary<byte, CustomPacket>();
		internal readonly Dictionary<byte, PropertySynchronization> syncedProperties = new Dictionary<byte, PropertySynchronization>();


		internal readonly Dictionary<byte, Type> requestTypeMap = new Dictionary<byte, Type>();
		internal readonly Dictionary<byte, Delegate> responseFunctionMap = new Dictionary<byte, Delegate>();


		internal static readonly Dictionary<byte, List<ReroutingInfo>> rerouter = new Dictionary<byte, List<ReroutingInfo>>();

		internal static readonly Dictionary<byte, Delegate> providedValues = new Dictionary<byte, Delegate>();

		private readonly BinaryFormatter bf = new BinaryFormatter();
		private RequestHandler responseManager;

		internal event EventHandler<DataReroutedEventArgs> OnRerouteRequest;

		private readonly TCPConnection _connection;

		internal DataIDs(TCPConnection connection) {
			responseManager = new RequestHandler(connection);
			_connection = connection;
		}

		internal Type IndetifyID(byte packetID, byte fromClient, byte[] data) {
			if (Reroute(packetID, fromClient, data)) {
				return null;
			}

			switch (packetID) {
				case StringID: {
					return typeof(string);
				}
				case Int64ID: {
					return typeof(Int64);
				}
				case RequestReceptionID: {
					byte requestID = data[0];
					TCPRequest request = new TCPRequest(requestID);

					if (!responseFunctionMap.ContainsKey(requestID)) {
						throw new NotImplementedException(string.Format("Server is requesting response for '{0}' byteID, but no such ID is defined!", request.packetID));
					}
					object obj = responseFunctionMap[requestID].DynamicInvoke(null);

					if (obj is byte[]) {
						responseManager.HandleRequest(request, obj as byte[]);
					}
					else {
						responseManager.HandleRequest(request, obj);
					}
					return typeof(TCPRequest);
				}
				case ResponseReceptionID: {
					TCPResponse response = new TCPResponse(data[0], new byte[data.Length - 1], responseManager.GetResponseType(data[0]));
					Array.Copy(data, 1, response.rawData, 0, data.Length - 1);
					return typeof(TCPResponse);
				}
				case PropertySyncID: {
					byte[] realData = new byte[data.Length - 1];
					Array.Copy(data, 1, realData, 0, realData.Length);
					syncedProperties[data[0]].property.SetValue(syncedProperties[data[0]].classInstance,
						SimpleTCPHelper.GetObject(syncedProperties[data[0]].propertyType, realData));
					return typeof(OnPropertySynchronizationEventArgs);
				}
				case ClientDisconnected: {
					return typeof(ClientDisconnectedPacket);
				}
				default: {
					if (customIDs.ContainsKey(packetID)) {
						return typeof(CustomPacket);
					}
					throw new UndefinedPacketException("Received unknown packet!", packetID, null);
				}
			}
		}

		/// <summary>
		/// Helper to find wheter the ID is already taken by something
		/// </summary>
		internal bool IsIDReserved(byte packetID, out Type dataType, out string message) {

			//Primitives provided by the server
			if (packetID == StringID) {
				dataType = typeof(string);
				message = "This ID is taken by a primitive String sending packet, use OnStringReceived event";
				return true;
			}
			if (packetID == Int64ID) {
				dataType = typeof(Int64);
				message = "This ID is taken by a primitive Int64 sending packet, use OnInt64Received event";
				return true;
			}

			//Internal stuff
			if (packetID > 240) {
				dataType = typeof(object);
				message = "IDs higher than 250 are reserved for internal use!";
				return true;
			}

			//Dictionary
			if (customIDs.ContainsKey(packetID)) {
				dataType = customIDs[packetID].dataType;
				message = "This ID is taken by a packet for general data transmit";
				return true;
			}
			if (providedValues.ContainsKey(packetID)) {
				dataType = typeof(object);
				message = "This ID is taken by a packet for general data transmit";
				return true;
			}
			dataType = null;
			message = "This ID is available!";
			return false;
		}

		private bool Reroute(byte ID, byte fromClient, byte[] data) {
			if (rerouter.ContainsKey(ID)) {
				ReroutingInfo info = null;
				for (int i = 0; i < rerouter[ID].Count; i++) {
					if (rerouter[ID][i].fromClient == fromClient && responseManager.connection.myInfo.clientID != rerouter[ID][i].toClient) {
						info = rerouter[ID][i];
					}
				}
				if (info == null) {
					return false;
				}
				OnRerouteRequest?.Invoke(this, new DataReroutedEventArgs(info.toClient, fromClient, (info.isUserDefined ? info.dataID : info.packetID), data, info.isUserDefined));
				return true;
			}
			return false;
		}

		internal void DefineCustomDataTypeForID<TData>(byte ID, Action<byte, TData> callback) {
			customIDs.Add(ID, new CustomPacket(ID, typeof(TData), new Action<byte, object>((b, o) => { callback(b, (TData)o); })));
		}

		internal void RemoveCustomDefinitionForID(byte ID) {
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