using System;
using System.Collections.Generic;

namespace Igor.TCP {
	internal interface IRerouteCapable {
		Dictionary<byte, List<ReroutingInfo>> RerouteDefinitions { get; }
	}
}
