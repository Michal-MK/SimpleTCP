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

		/// <summary>
		/// Getter for strongly typed registered serializers 
		/// </summary>
		/// <typeparam name="TData">The type for which to find a serializer</typeparam>
		/// <returns>The <see cref="ICustomSerializer"/> implementation</returns>
		public ICustomSerializer<TData> Get<TData>() {
			Type tDataType = typeof(TData);
			if (CustomSerializers.ContainsKey(tDataType)) {
				return (ICustomSerializer<TData>)CustomSerializers[tDataType];
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

		/// <summary>
		/// Is a serializer present for the provided <see cref="Type"/>
		/// </summary>
		/// <param name="type">The type to check for</param>
		/// <returns><see langword="true"/> if present, <see langword="false"/> otherwise</returns>
		public bool ContainsSerializationRule(Type type) => CustomSerializers.ContainsKey(type);
		
		/// <summary>
		/// Is a serializer present for the provided <see cref="Type"/>
		/// </summary>
		/// <typeparam name="TData">The type to check for</typeparam>
		/// <returns><see langword="true"/> if present, <see langword="false"/> otherwise</returns>
		public bool ContainsSerializationRule<TData>() => CustomSerializers.ContainsKey(typeof(TData));
	}
}