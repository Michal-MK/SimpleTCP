using System;
using System.Collections.Generic;
using SimpleTCP.DataTransfer.Serialization;

namespace SimpleTCP.Structures {
	public class ClientConfiguration : SerializationConfiguration {
		public ClientConfiguration(Dictionary<Type, ICustomSerializer>? customSerializers = null) : base(customSerializers) { }

		/// <summary>
		/// Builder pattern class for creating <see cref="ClientConfiguration"/>s
		/// </summary>
		public class Builder {
			private readonly Dictionary<Type, ICustomSerializer> serializers = new();

			/// <summary>
			/// Start of the builder chain
			/// </summary>
			/// <returns>The reference to the builder</returns>
			public static Builder Create() {
				return new();
			}

			/// <summary>
			/// Convert the current builder state to the final <see cref="ClientConfiguration"/> instance 
			/// </summary>
			public ClientConfiguration Build() {
				return new(serializers);
			}

			/// <summary>
			/// Add a custom serializer for a type
			/// </summary>
			/// <param name="serializer">The serializer instance</param>
			/// <typeparam name="T">The type of the object to serialize</typeparam>
			public Builder AddSerializer<T>(ICustomSerializer<T> serializer) {
				serializers.Add(typeof(T), serializer);
				return this;
			}
		}
	}
}