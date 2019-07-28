namespace Igor.TCP {
	class GeneralFeelTests {
		public void Test() {
			TCPClient client = new TCPClient("172.0.0.1", 522);
			TCPServer server = new TCPServer(new ServerConfiguration());
		}
	}
}
