namespace Igor.TCP {
	/// <summary>
	/// Event args holding basic information abou an ongoing synchronization of a property
	/// </summary>
	public class OnPropertySynchronizationEventArgs {

		/// <summary>
		/// The ID of the synchronization packet
		/// </summary>
		public byte syncID { get; set; }

		/// <summary>
		/// The name of the property that was synchronized
		/// </summary>
		public string propertyName { get; set; }

		/// <summary>
		/// Instance of the object that the property belongs to, <see langword="null"/> if the property is static
		/// </summary>
		public object instance { get; set; }
	}
}
