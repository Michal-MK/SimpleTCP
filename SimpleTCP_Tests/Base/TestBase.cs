using System.Threading;
using System.Threading.Tasks;

namespace SimpleTCP.Tests.Base {
	public class TestBase {
		protected readonly ManualResetEventSlim evnt = new();

		private const int DEFAULT_DELAY = 1000;

		protected async Task Wait() {
			await Task.WhenAny(Task.Delay(DEFAULT_DELAY), Task.Run(evnt.Wait));
		} 
	}
}