using System;
using System.Collections.Generic;

namespace Igor.TCP {
	/// <summary>
	/// Server configuration class
	/// </summary>
	public class ServerConfiguration : SerializationConfiguration {
		/// <summary>
		/// Default constructor, select which values you want to modify, the rest is set to defaults
		/// </summary>
		public ServerConfiguration(bool allowClientsToRaiseRequestsToServer = false, Dictionary<Type, ICustomSerializer> serializers = null) : base(serializers) {
			ClientCanRequestFromServer = allowClientsToRaiseRequestsToServer;
		}

		/// <summary>
		/// Allows clients to make requests to the server
		/// </summary>
		public bool ClientCanRequestFromServer { get; }

		public class Builder {
			private bool clientToServerRequests;
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
			public ServerConfiguration Build() {
				return new(clientToServerRequests, serializers);
			}

			/// <summary>
			/// Allows clients to make requests to the server
			/// </summary>
			public Builder AllowClientToServerRequests() {
				clientToServerRequests = true;
				return this;
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