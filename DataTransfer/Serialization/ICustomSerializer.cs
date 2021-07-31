namespace Igor.TCP {
	public interface ICustomSerializer<TData> : ICustomSerializer {
		TData Deserialize(byte[] data);

		byte[] Serialize(TData data);
	}

	public interface ICustomSerializer { }
}