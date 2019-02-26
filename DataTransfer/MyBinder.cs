using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Igor.TCP {
	sealed class MyBinder : SerializationBinder {
		public override Type BindToType(string assemblyName, string typeName) {
			Type ttd = null;
			string currentExecutingAssembly = Assembly.GetExecutingAssembly().FullName;
			bool isSystemType = false;
			if (!assemblyName.Contains("PublicKeyToken=null")) {
				isSystemType = true;
			}

			string originalAssemblyName = assemblyName.Split(',')[0];
			string wantedAssemblyName;
			if (isSystemType)
				wantedAssemblyName = originalAssemblyName;
			else
				wantedAssemblyName = currentExecutingAssembly.Split(',')[0];

			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				if (assembly.FullName.Split(',')[0] == (isSystemType ? originalAssemblyName : wantedAssemblyName)) {
					if (typeName.Contains("PublicKeyToken")) {
						string[] split = typeName.Split(',');
						split[1] = " " + wantedAssemblyName;
						typeName = string.Join(",", split);
					}
					ttd = assembly.GetType(typeName);
					break;
				}
			}
			return ttd;
		}
	}
}