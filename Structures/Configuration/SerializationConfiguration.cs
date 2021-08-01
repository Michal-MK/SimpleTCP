using System;
using System.Collections.Generic;

namespace Igor.TCP {
	public class SerializationConfiguration {
		public SerializationConfiguration(Dictionary<Type, ICustomSerializer> customSerializers) {
			CustomSerializers = customSerializers ?? new Dictionary<Type, ICustomSerializer>();
		}

		/// <summary>
		/// Custom serialization for provided types, used if provided, otherwise internal method of serialization is used
		/// (will be less efficient due to being generic)
		/// </summary>
		public Dictionary<Type, ICustomSerializer> CustomSerializers { get; }

		public ICustomSerializer<TData> Get<TData>(Type type) {
			if (CustomSerializers.ContainsKey(type)) {
				return (ICustomSerializer<TData>)CustomSerializers[type];
			}
			return null;
		}

		internal object DeserializeAsObject(Type type, byte[] data) {
			return CustomSerializers[type].GetType()
										  .GetMethod(nameof(ICustomSerializer<object>.Deserialize))
										  .Invoke(CustomSerializers[type], new object[] { data });
		}

		internal byte[] SerializeAsObject(Type type, object data) {
			return (byte[])CustomSerializers[type].GetType()
												  .GetMethod(nameof(ICustomSerializer<object>.Serialize))
												  .Invoke(CustomSerializers[type], new[] { data });
		}

		public bool ContainsSerializationRule(Type type) => CustomSerializers.ContainsKey(type);
	}
}