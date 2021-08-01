using System.Collections.Generic;
using SimpleTCP.Structures;

namespace SimpleTCP.Connections.Interaces {
	internal interface IRerouteCapable {
		Dictionary<byte, List<ReroutingInfo>> RerouteDefinitions { get; }
	}
}
