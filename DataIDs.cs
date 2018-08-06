using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	public class DataIDs {
		public const int PACKET_ID_COMPLEXITY = 1;
		public const byte TCPDataID = 0;
		public const byte StringID = 32;
		public const byte LongID = 64;
		public const byte UserDefined = 128;
		public const byte ServerStop = 250;
		public const byte ClientDisconnected = 251;
		public const byte RequestReceptionID = 254;
		public const byte ResponseReceptionID = 255;


		internal readonly Dictionary<byte, Type> idDict = new Dictionary<byte, Type>();


		internal readonly Dictionary<byte, Type> requestDict = new Dictionary<byte, Type>();
		internal readonly Dictionary<byte, Delegate> responseDict = new Dictionary<byte, Delegate>();


		private BinaryFormatter bf = new BinaryFormatter();
		internal TCPConnection connection;

		private ResponseManager responseManager;

		internal DataIDs(TCPConnection connection) {
			this.connection = connection;
			responseManager = new ResponseManager(this);
		}

		private byte ParseID(byte[] packetID) {
			return packetID[0];
		}

		public Type IndetifyID(byte[] ID, out object dataObj, MemoryStream ms) {
			byte id = ParseID(ID);
			switch (id) {
				case TCPDataID: {
					dataObj = bf.Deserialize(ms);
					return typeof(TCPData);
				}
				case StringID: {
					dataObj = System.Text.Encoding.UTF8.GetString(ms.ToArray());
					return typeof(string);
				}
				case LongID: {
					dataObj = BitConverter.ToInt64(ms.ToArray(), 0);
					return typeof(Int64);
				}
				case UserDefined: {
					byte[] data = ms.ToArray();
					Type t = idDict[data[0]];
					ms.Flush();
					ms.Write(data, 1, data.Length - 1);
					ms.Seek(0, SeekOrigin.Begin);
					dataObj = Helper.GetObject(t, ms.ToArray());
					return t;
				}
				case RequestReceptionID: {
					byte requestID = ms.ToArray()[0];
					TCPRequest request = new TCPRequest(requestID);

					if (!responseDict.ContainsKey(requestID)) {
						throw new Exception(string.Format("Server is requesting response for '{0}' byteID, but no such ID is defined!", request));
					}
					object obj = responseDict[requestID].DynamicInvoke(null);
					responseManager.HandleRequest(request, obj);
					dataObj = null;
					return typeof(TCPRequest);
				}
				case ResponseReceptionID: {
					byte[] dataReceived = ms.ToArray();
					TCPResponse response = new TCPResponse(dataReceived[0]);
					response.rawData = new byte[dataReceived.Length - 1];
					Array.Copy(dataReceived, 1, response.rawData, 0, dataReceived.Length - 1);
					dataObj = response;
					return typeof(TCPResponse);
				}

				default: {
					throw new NotSupportedException(string.Format("This identifier is not supported {{0}}",	ID[0]));
				}
			}
		}

		public void DefineCustomDataTypeForID<TData>(byte ID) {
			idDict.Add(ID, typeof(TData));
		}
		public void DefineCustomDataTypeForID(byte ID, Type t) {
			idDict.Add(ID, t);
		}

		public void RemoveCustomDefinitionForID(byte ID) {
			idDict.Remove(ID);
		}
	}
}