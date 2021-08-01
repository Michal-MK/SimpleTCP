namespace SimpleTCP.Structures {
	public class PingerHost {
		public PingerHost(string ip, int duration) {
			IP = ip;
			Duration = duration;
		}

		public string IP { get; }

		public int Duration { get; }

		public override string ToString() => IP;
	}
}
