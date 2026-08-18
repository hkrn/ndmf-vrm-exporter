using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace com.github.hkrn
{
    public sealed class PackageJson
    {
        public const string Name = "com.github.hkrn.ndmf-vrm-exporter";
        public string DisplayName { get; } = null!;
        public string Version { get; } = null!;

        public static PackageJson LoadFromString(string json)
        {
            return JsonConvert.DeserializeObject<PackageJson>(json, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
            })!;
        }
    }
}
