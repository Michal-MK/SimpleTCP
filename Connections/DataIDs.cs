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
		/// Packet ID for <see langword="string"/>
		/// </summary>
		internal const byte STRING_ID = 0;

		/// <summary>
		/// Packet ID for Int64 (<see langword="long"/>)
		/// </summary>
		internal const byte INT64_ID = 1;

		/// <summary>
		/// Packet ID for handling requests
		/// </summary>
		internal const byte REQUEST_RECEPTION_ID = 250;

		/// <summary>
		/// Packet ID for handling responses to requests
		/// </summary>
		internal const byte RESPONSE_RECEPTION_ID = 251;

		/// <summary>
		/// Packet ID for handling property synchronization
		/// </summary>
		internal const byte PROPERTY_SYNC_ID = 252;

		/// <summary>
		/// Packet ID for sending client information to the other side
		/// </summary>
		internal const byte CLIENT_INFORMATION_ID = 253;

		/// <summary>
		/// Packet ID to signalize client disconnect
		/// </summary>
		internal const byte CLIENT_DISCONNECTED = 254;


		internal const byte CLIENT_IDENTIFICATION_COMPLEXITY = 1;

		internal const byte PACKET_ID_COMPLEXITY = 1;

		internal const byte PACKET_TOTAL_HEADER_SIZE_COMPLEXITY = 8;

		internal const byte PACKET_ID_UPPER_LIMIT = 240;

		#endregion

		internal readonly Dictionary<byte, CustomPacket> customIDs = new();
		internal readonly Dictionary<byte, PropertySynchronization> syncedProperties = new();

		internal readonly Dictionary<byte, Type> requestTypeMap = new();
		internal readonly Dictionary<byte, Delegate> responseFunctionMap = new();

		private readonly RequestHandler responseManager;

		internal event EventHandler<DataReroutedEventArgs> OnRerouteRequest;

		private readonly TCPConnection connection;

		internal IRerouteCapable rerouter = null;

		internal DataIDs(TCPConnection connection) {
			responseManager = new RequestHandler(connection);
			this.connection = connection;
		}

		/// <exception cref="NotImplementedException"></exception>
		/// <exception cref="UndefinedPacketEventArgs"></exception>
		internal Type IdentifyID(byte packetID, byte fromClient, byte[] data) {
			if (rerouter != null && Reroute(packetID, fromClient, data)) {
				return typeof(ReroutingInfo);
			}

			switch (packetID) {
				case STRING_ID: {
					return typeof(string);
				}
				case INT64_ID: {
					return typeof(Int64);
				}
				case REQUEST_RECEPTION_ID: {
					byte requestID = data[0];

					if (!responseFunctionMap.ContainsKey(requestID)) {
						responseManager.HandleRequest(new TCPRequest(requestID),
							$"A request was made under {requestID} {(connection.myInfo.IsServer ? "Server" : "Client")} (ID:{connection.myInfo.ID} can not respond!)");
					}
					else {
						TCPRequest request = new TCPRequest(requestID);
						object obj = responseFunctionMap[request.PacketID].DynamicInvoke(null);

						if (obj is byte[] array) {
							responseManager.HandleRequest(request, array);
						}
						else {
							responseManager.HandleRequest(request, obj);
						}
					}
					return typeof(TCPRequest);
				}
				case RESPONSE_RECEPTION_ID: {
					return typeof(TCPResponse);
				}
				case PROPERTY_SYNC_ID: {
					byte[] realData = new byte[data.Length - 1];
					byte dataID = data[0];
					Array.Copy(data, 1, realData, 0, realData.Length);
					syncedProperties[dataID].Property.SetValue(syncedProperties[dataID].ClassInstance,
						SimpleTCPHelper.GetObject(syncedProperties[dataID].PropertyType, realData, connection.serializationConfig));
					return typeof(OnPropertySynchronizationEventArgs);
				}
				case CLIENT_DISCONNECTED: {
					return typeof(TCPClientInfo);
				}
				default: {
					if (customIDs.ContainsKey(packetID)) {
						return typeof(CustomPacket);
					}
					return typeof(UndefinedPacketEventArgs);
				}
			}
		}

		/// <summary>
		/// Helper to find whether the ID is already taken by something
		/// </summary>
		internal bool IsIDReserved(byte packetID, out Type dataType, out string message) {

			//Primitives provided by the server
			if (packetID == STRING_ID) {
				dataType = typeof(string);
				message = $"This ID is taken by a primitive {nameof(String)} sending packet, use OnStringReceived event";
				return true;
			}
			if (packetID == INT64_ID) {
				dataType = typeof(Int64);
				message = $"This ID is taken by a primitive {nameof(Int64)} sending packet, use OnInt64Received event";
				return true;
			}

			//Internal stuff
			if (packetID > PACKET_ID_UPPER_LIMIT) {
				dataType = typeof(object);
				message = $"IDs higher than {PACKET_ID_UPPER_LIMIT} are reserved for internal use!";
				return true;
			}

			//Dictionary
			if (customIDs.ContainsKey(packetID)) {
				dataType = customIDs[packetID].DataType;
				message = "This ID is taken by a packet for general data transmit";
				return true;
			}
			if (connection.valueProvider.ProvidedValues.ContainsKey(packetID)) {
				dataType = typeof(object);
				message = "This ID is taken by a packet for general data transmit";
				return true;
			}
			dataType = null;
			message = "This ID is available!";
			return false;
		}

		private bool Reroute(byte id, byte fromClient, byte[] data) {
			if (rerouter.RerouteDefinitions.ContainsKey(id)) {
				ReroutingInfo info = null;
				for (int i = 0; i < rerouter.RerouteDefinitions[id].Count; i++) {
					if (responseManager.connection.myInfo.ID != rerouter.RerouteDefinitions[id][i].ToClient) {
						info = rerouter.RerouteDefinitions[id][i];
						break;
					}
				}
				if (info == null) {
					return false;
				}
				OnRerouteRequest?.Invoke(this, new DataReroutedEventArgs(info.ToClient, fromClient, info.PacketID, data));
				return true;
			}
			return false;
		}

		internal void DefineCustomPacket<TData>(byte id, Action<byte, TData> callback) {
			customIDs.Add(id, new CustomPacket(id, typeof(TData), (b, o) => { callback(b, (TData)o); }));
		}

		internal void RemoveCustomPacket(byte id) {
			customIDs.Remove(id);
		}

		internal void SetForRerouting(ReroutingInfo info) {
			if (!rerouter.RerouteDefinitions.ContainsKey(info.PacketID)) {
				rerouter.RerouteDefinitions.Add(info.PacketID, new List<ReroutingInfo>() { info });
			}
			else {
				if (rerouter.RerouteDefinitions[info.PacketID].Find((p) => p.ToClient == info.ToClient && p.PacketID == info.PacketID) == null) {
					rerouter.RerouteDefinitions[info.PacketID].Add(info);
					return;
				}
				throw new PacketIDTakenException(info.PacketID, null, "Attempted to add a rerouting definition, but such definition already exists!");
			}
		}
	}
}