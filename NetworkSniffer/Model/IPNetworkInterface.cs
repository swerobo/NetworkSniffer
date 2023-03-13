using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkSniffer.Model
{
    public class IPNetworkInterface
    {
        public string InterfaceName { get; set; }

        public string InterfaceAddress { get; set; }

        public string InterfaceNameAndAddress
        {
            get
            {
                return InterfaceName + " (" + InterfaceAddress + ")";
            }
        }
    }
}
