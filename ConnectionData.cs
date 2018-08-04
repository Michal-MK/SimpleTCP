using System;

[Serializable]
public class ConnectionData {
	public string ipAddress { get; private set; }
	public ushort port { get; private set; }

	public event EventHandler<ConnectionData> OnConnectionDataParsed;

	public ConnectionData(string ipAddress, ushort port) {
		this.ipAddress = ipAddress;
		this.port = port;
		OnConnectionDataParsed(this, this);
	}

	public static ConnectionData Parse(Func<Tuple<string,ushort>> func) {
		Tuple<string, ushort> tuple = func.Invoke();
		return new ConnectionData(tuple.Item1, tuple.Item2);
	}
}
