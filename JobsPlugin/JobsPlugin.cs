using DarkRift.Server;
using System;

namespace JobsPlugin
{
    public class JobsPlugin : Plugin
    {
        public JobsPlugin(PluginLoadData pluginLoadData) : base(pluginLoadData)
        {
        }

        public override bool ThreadSafe => false;

        public override Version Version => new Version("1.0.0");
    }
}
