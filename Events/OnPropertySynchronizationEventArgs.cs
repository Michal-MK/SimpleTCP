using System;

namespace Igor.TCP {
	/// <summary>
	/// Event data holding basic information about an ongoing synchronization of a property
	/// </summary>
	public class OnPropertySynchronizationEventArgs : EventArgs {
		
		internal OnPropertySynchronizationEventArgs(byte synchronizationPacketID, PropertySynchronization sync) {
			SynchronizationPacketID = synchronizationPacketID;
			PropertyName = sync.Property.Name;
			Instance = sync.ClassInstance;
		}

		/// <summary>
		/// The ID of the synchronization packet
		/// </summary>
		public byte SynchronizationPacketID { get; }

		/// <summary>
		/// The name of the property that was synchronized
		/// </summary>
		public string PropertyName { get; }

		/// <summary>
		/// Instance of the object that the property belongs to, <see langword="null"/> if the property is static
		/// </summary>
		public object Instance { get; }
	}
}