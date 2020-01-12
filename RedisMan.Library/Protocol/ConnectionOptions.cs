using System;

namespace RedisMan.Library.Protocol
{
    public class ConnectionOptions
    {
        /*
         *                 { "h|hostname=", "the name of someone to greet.", value => { hostname = value; } },
                { "p|port=", "the number of times to repeat the greeting.", (int p) => port = p },
                { "a|password=", "increase debug message verbosity", value => { password = value; } },
                { "help", "show this message and exit", h => shouldShowHelp = h != null },
                */
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
    }
}
