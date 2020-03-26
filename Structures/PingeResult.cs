namespace Igor.TCP {
	public class PingerHost {
		public string IP { get; }
		public int Duration { get; }

		public PingerHost(string ip, int duration) {
			IP = ip;
			Duration = duration;
		}

		public override string ToString() => IP;
	}
}
