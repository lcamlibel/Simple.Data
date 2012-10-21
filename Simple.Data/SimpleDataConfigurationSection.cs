using System.Configuration;
using System.Diagnostics;

namespace Simple.Data
{
    public class SimpleDataConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("traceLevel", DefaultValue = TraceLevel.Info, IsRequired = false)]
        public TraceLevel TraceLevel
        {
            get { return (TraceLevel) this["traceLevel"]; }
            set { this["traceLevel"] = value; }
        }
    }
}