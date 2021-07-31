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
			private Dictionary<Type, ICustomSerializer> serializers = new();


			public static Builder Create() {
				return new Builder();
			}

			public ServerConfiguration Build() {
				return new ServerConfiguration(clientToServerRequests, serializers);
			}

			public Builder AllowClientToServerRequests() {
				clientToServerRequests = true;
				return this;
			}
			
			public Builder AddSerializer<T>(ICustomSerializer<T> serializer) {
				serializers.Add(typeof(T), serializer);
				return this;
			}
		}
	}
}