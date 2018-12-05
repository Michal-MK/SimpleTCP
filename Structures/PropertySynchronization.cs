using System;
using System.Reflection;

namespace Igor.TCP {
	internal class PropertySynchronization {

		/// <summary>
		/// ID of the property packet
		/// </summary>
		internal byte packetID { get; }

		/// <summary>
		/// Instance of the class where to modify the property
		/// </summary>
		internal object classInstance { get; }

		/// <summary>
		/// The property to keep in sync
		/// </summary>
		internal PropertyInfo property { get; }

		/// <summary>
		/// Wrapper to get the type of the property
		/// </summary>
		internal Type propertyType { get { return property.PropertyType; } }

		/// <summary>
		/// Wrapper to get the type of the class instance
		/// </summary>
		internal Type classInstanceType { get { return property.DeclaringType; } }

		/// <summary>
		/// Default Constructor
		/// </summary>
		internal PropertySynchronization(byte packetID, object classInstance, string propertyName) {
			this.packetID = packetID;
			this.classInstance = classInstance;

			property = classInstance.GetType().GetProperty(propertyName);

			if (property == null) {
				if (classInstance.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) != null) {
					throw new InvalidOperationException("Only public properties may be synchronized");
				}
				throw new NotImplementedException("Attempting to sync a non-existing property!");
			}
		}
	}
}
