using System;
using System.Reflection;

namespace SimpleTCP.Structures {
	internal class PropertySynchronization {
		
		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <exception cref="InvalidOperationException">The property is not publicly visible</exception>
		/// <exception cref="ArgumentException">The property name does not exist</exception>
		internal PropertySynchronization(byte packetID, object classInstance, string propertyName) {
			PacketID = packetID;
			ClassInstance = classInstance;

			Property = classInstance.GetType().GetProperty(propertyName);

			if (Property == null) {
				if (classInstance.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) != null) {
					throw new InvalidOperationException("Only public non static properties may be synchronized");
				}
				throw new ArgumentException($"Attempting to sync a non-existing property! ({propertyName})");
			}
		}

		/// <summary>
		/// ID of the property packet
		/// </summary>
		internal byte PacketID { get; }

		/// <summary>
		/// Instance of the class where to modify the property
		/// </summary>
		internal object ClassInstance { get; }

		/// <summary>
		/// The property to keep in sync
		/// </summary>
		internal PropertyInfo Property { get; }

		/// <summary>
		/// Wrapper to get the type of the property
		/// </summary>
		internal Type PropertyType => Property.PropertyType;

		/// <summary>
		/// Wrapper to get the type of the class instance
		/// </summary>
		internal Type ClassInstanceType => Property.DeclaringType;
	}
}
