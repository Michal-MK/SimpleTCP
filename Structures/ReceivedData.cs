using System;

namespace Igor.TCP {

	/// <summary>
	/// Internal data holder that is constructed after parsing all of the bytes
	/// </summary>
	public struct ReceivedData {
		
		internal ReceivedData(Type dataType, byte senderID, byte dataID, object receivedObject) {
			DataType = dataType;
			SenderID = senderID;
			DataID = dataID;
			this.ReceivedObject = receivedObject;
		}

		/// <summary>
		/// Data type of this data object
		/// </summary>
		internal Type DataType { get; }

		/// <summary>
		/// ID of the endpoint this packet originated from
		/// </summary>
		internal byte SenderID { get; }

		/// <summary>
		/// ID of the data
		/// </summary>
		internal byte DataID { get; }

		/// <summary>
		/// Received bytes converted into a generic object. Casting to <see cref="DataType"/> is valid.
		/// </summary>
		internal object ReceivedObject { get; }
	}
}
