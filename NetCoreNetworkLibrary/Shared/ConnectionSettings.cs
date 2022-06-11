using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetCoreNetwork.Shared
{
    public class ConnectionSettings
    {
        public int PingInterval { get; set; } = 10;
        public int PingTimeout { get; set; } = 15;
        public int ConnectInterval { get; set; } = 20;
    }
}
