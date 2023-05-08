using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Toletus.LiteNet2.Base.Util
{
    public class LiteNetUtil
    {
        const string ToletusLiteNet2 = "TOLETUS LiteNet2";
        private static List<LiteNet2BoardBase> _liteNets;

        static LiteNetUtil()
        {
            UdpUtil.OnUdpResponse += OnUdpResponse;
        }

        public static List<LiteNet2BoardBase> Search(IPAddress iPAdress)
        {
            _liteNets = new List<LiteNet2BoardBase>();

            UdpUtil.Send(iPAdress, 7878, "prc");

            foreach (var liteNet in _liteNets)
                liteNet.NetworkIp = iPAdress;

            return _liteNets;
        }

        public static LiteNet2BoardBase Search(string networkInterfaceName, int? id)
        {
            var liteNets = Search(networkInterfaceName);

            return liteNets?.FirstOrDefault(c => id == null || c.Id == id);
        }

        public static List<LiteNet2BoardBase> Search(string networkInterfaceName)
        {
            var ip = NetworkInterfaceUtil.GetNetworkInterfaceIpAddressByName(networkInterfaceName);

            return ip == null ? null : Search(ip);
        }

        private static void OnUdpResponse(UdpClient udpClient, Task<UdpReceiveResult> response)
        {
            var device = Encoding.ASCII.GetString(response.Result.Buffer);

            if (!device.Contains(ToletusLiteNet2))
                return;

            var id = (ushort)Convert.ToInt16(device.Split('@')[1]);

            var liteNet = new LiteNet2BoardBase(response.Result.RemoteEndPoint.Address, id);

            _liteNets.Add(liteNet);
        }
    }
}