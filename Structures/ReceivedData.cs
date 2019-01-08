using System;

namespace Igor.TCP {

	/// <summary>
	/// Internal data holder that is constructed after parsing all of the bytes
	/// </summary>
	public struct ReceivedData {

		/// <summary>
		/// Data type of this data object
		/// </summary>
		internal Type dataType { get; }

		/// <summary>
		/// ID of the endpoint this packet originated from
		/// </summary>
		internal byte senderID { get; }

		/// <summary>
		/// ID of the data
		/// </summary>
		internal byte dataID { get; }

		/// <summary>
		/// Received bytes converted into a generic object. Casting to <see cref="dataType"/> is valid.
		/// </summary>
		internal object receivedObject { get; }

		internal ReceivedData(Type dataType, byte senderID, byte dataID, object receivedObject) {
			this.dataType = dataType;
			this.senderID = senderID;
			this.dataID = dataID;
			this.receivedObject = receivedObject;
		}
	}
}
