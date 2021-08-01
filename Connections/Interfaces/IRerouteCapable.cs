using System.Collections.Generic;
using SimpleTCP.Structures;

namespace SimpleTCP.Connections.Interfaces {
	internal interface IRerouteCapable {
		Dictionary<byte, List<ReroutingInfo>> RerouteDefinitions { get; }
	}
}
