using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Igor.TCP {
	internal class DataIDs {
		public const int PACKET_ID_COMPLEXITY = 1;
		public const byte TCPDataID = 0;
		public const byte StringID = 32;
		public const byte LongID = 64;
		public const byte RequestReceptionID = 254;
		public const byte ResponseReceptionID = 255;


		internal readonly Dictionary<byte, Delegate> idDict = new Dictionary<byte, Delegate>();


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
				case RequestReceptionID: {
					TCPRequest request = (TCPRequest)bf.Deserialize(ms);
					if (!idDict.ContainsKey(request.packetID)) {
						throw new Exception(string.Format("Server is requesting response for '{0}' byteID, but no such ID is defined!", request.packetID));
					}
					object obj = idDict[request.packetID].DynamicInvoke(null);
					responseManager.HandleRequest(request, obj);
					dataObj = null;
					return typeof(TCPRequest);
				}
				case ResponseReceptionID: {
					dataObj = bf.Deserialize(ms);
					return typeof(TCPResponse);
				}

				default: {
					throw new NotSupportedException(string.Format("This identifier is not supported {{0}}",
						ID[0]));
				}
			}
		}

		internal void AddNew<TData>(byte ID, Func<TData> func) {
			idDict.Add(ID, func);
			responseManager.AddType(ID, typeof(TData));
		}

		internal void RemoveFunc(byte ID) {
			idDict.Remove(ID);
			responseManager.RemoveType(ID);
		}
	}
}