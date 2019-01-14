using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	/// <summary>
	/// Class holding packet IDs and received data resolution and unpacking
	/// </summary>
	internal class DataIDs {

		#region Constant byte IDs and sizes of packets

		/// <summary>
		/// Packet ID for string
		/// </summary>
		internal const byte StringID = 0;

		/// <summary>
		/// Packet ID for Int64 (long)
		/// </summary>
		internal const byte Int64ID = 1;

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
		internal const byte ClientInformationID = 253;

		/// <summary>
		/// Packet ID to signalize client disconnect
		/// </summary>
		internal const byte ClientDisconnected = 254;


		internal const byte CLIENT_IDENTIFICATION_COMPLEXITY = 1;

		internal const byte PACKET_ID_COMPLEXITY = 1;

		internal const byte PACKET_TOTAL_SIZE_COMPLEXITY = 8;

		#endregion

		internal readonly Dictionary<byte, CustomPacket> customIDs = new Dictionary<byte, CustomPacket>();
		internal readonly Dictionary<byte, PropertySynchronization> syncedProperties = new Dictionary<byte, PropertySynchronization>();

		internal readonly Dictionary<byte, Type> requestTypeMap = new Dictionary<byte, Type>();
		internal readonly Dictionary<byte, Delegate> responseFunctionMap = new Dictionary<byte, Delegate>();

		private RequestHandler responseManager;

		internal event EventHandler<DataReroutedEventArgs> OnRerouteRequest;

		private readonly TCPConnection _connection;

		internal IRerouteCapable rerouter = null;

		internal DataIDs(TCPConnection connection) {
			responseManager = new RequestHandler(connection);
			_connection = connection;
		}

		/// <exception cref="NotImplementedException"></exception>
		/// <exception cref="UndefinedPacketException"></exception>
		internal Type IndetifyID(byte packetID, byte fromClient, byte[] data) {
			if (rerouter != null && Reroute(packetID, fromClient, data)) {
				return typeof(ReroutingInfo);
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

					if (!responseFunctionMap.ContainsKey(requestID)) {
						throw new NotImplementedException(string.Format("Server is requesting response for '{0}' byteID, but no such ID is defined!", requestID));
					}

					TCPRequest request = new TCPRequest(requestID);

					object obj = responseFunctionMap[request.packetID].DynamicInvoke(null);

					if (obj is byte[]) {
						responseManager.HandleRequest(request, obj as byte[]);
					}
					else {
						responseManager.HandleRequest(request, obj);
					}
					return typeof(TCPRequest);
				}
				case ResponseReceptionID: {
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
		/// Helper to find whether the ID is already taken by something
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
			if (_connection.valueProvider.providedValues.ContainsKey(packetID)) {
				dataType = typeof(object);
				message = "This ID is taken by a packet for general data transmit";
				return true;
			}
			dataType = null;
			message = "This ID is available!";
			return false;
		}

		private bool Reroute(byte ID, byte fromClient, byte[] data) {
			if (rerouter.rerouteDefinitions.ContainsKey(ID)) {
				ReroutingInfo info = null;
				for (int i = 0; i < rerouter.rerouteDefinitions[ID].Count; i++) {
					if (responseManager.connection.myInfo.clientID != rerouter.rerouteDefinitions[ID][i].toClient) {
						info = rerouter.rerouteDefinitions[ID][i];
						break;
					}
				}
				if (info == null) {
					return false;
				}
				OnRerouteRequest?.Invoke(this, new DataReroutedEventArgs(info.toClient, fromClient, info.packetID, data));
				return true;
			}
			return false;
		}

		internal void DefineCustomPacket<TData>(byte ID, Action<byte, TData> callback) {
			customIDs.Add(ID, new CustomPacket(ID, typeof(TData), new Action<byte, object>((b, o) => { callback(b, (TData)o); })));
		}

		internal void RemoveCustoMPacket(byte ID) {
			customIDs.Remove(ID);
		}

		internal void SetForRerouting(ReroutingInfo info) {
			if (!rerouter.rerouteDefinitions.ContainsKey(info.packetID)) {
				rerouter.rerouteDefinitions.Add(info.packetID, new List<ReroutingInfo>() { info });
			}
			else {
				if (rerouter.rerouteDefinitions[info.packetID].Find((p) => { return p.toClient == info.toClient && p.packetID == info.packetID; }) != null) {
					rerouter.rerouteDefinitions[info.packetID].Add(info);
				}
				throw new PacketIDTakenException(info.packetID, null, "Attempted to add a rerouting definition, but such definition already exists!");
			}
		}
	}
}