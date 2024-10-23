using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer.tools {
    public class ConnectInfo {
        private string? ip = null;
        private uint port = 0;
        private string? mac = null;
        private string? oS = null;

        public string? Ip { get => ip; set => ip = value; }
        public uint Port { get => port; set => port = value; }
        public string? Mac { get => mac; set => mac = value; }
        public string? OS { get => oS; set => oS = value; }

        public ConnectInfo(string? ip, uint port, string? Mac, string? OS) {
            Ip = ip;
            Port = port;
            this.Mac = Mac;
            this.OS = OS;
        }
        public override string ToString() {
            return $"{Ip}:{Port} {Mac} {OS}";
        }
    }
}
