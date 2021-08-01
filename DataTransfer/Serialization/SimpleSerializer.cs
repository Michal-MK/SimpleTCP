using System;

namespace SimpleTCP.DataTransfer.Serialization {
	public class SimpleSerializer<TData> : ICustomSerializer<TData> {
		
		private readonly Func<TData, byte[]> serializationFunc;
		private readonly Func<byte[], TData> deserializationFunc;

		public SimpleSerializer(Func<byte[], TData> deserialization, Func<TData, byte[]> serialization) {
			serializationFunc = serialization;
			deserializationFunc = deserialization;
		}

		public byte[] Serialize(TData data) {
			return serializationFunc(data);
		}

		public TData Deserialize(byte[] data) {
			return deserializationFunc(data);
		}
	}
}