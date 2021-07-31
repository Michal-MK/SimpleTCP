using System;
using System.Collections.Generic;

namespace Igor.TCP {
	public class ClientConfiguration : SerializationConfiguration {
		public ClientConfiguration(Dictionary<Type, ICustomSerializer> customSerializers = null) : base(customSerializers) { }

		public class Builder {
			private Dictionary<Type, ICustomSerializer> serializers = new();

			public static Builder Create() {
				return new();
			}

			public ClientConfiguration Build() {
				return new ClientConfiguration(serializers);
			}

			public Builder AddSerializer<T>(ICustomSerializer<T> serializer) {
				serializers.Add(typeof(T), serializer);
				return this;
			}
		}
	}
}