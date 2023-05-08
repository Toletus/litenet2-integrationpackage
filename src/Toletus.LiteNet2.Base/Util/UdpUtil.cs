using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Toletus.LiteNet2.Base.Util
{
    public class UdpUtil
    {
        public delegate void UdpResponseHandler(UdpClient udpClient, Task<UdpReceiveResult> response);
        public static event UdpResponseHandler OnUdpResponse;

        public static void Send(IPAddress iPAdress, int port, string content)
        {
            UdpClient udpClient;
            try
            {
                udpClient = new UdpClient(new IPEndPoint(iPAdress, 0));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetType().Name);
                return;
            }

            var endPoint = new IPEndPoint(IPAddress.Broadcast, port);
            var contentBytes = Encoding.ASCII.GetBytes(content);

            udpClient.SendAsync(contentBytes, contentBytes.Length, endPoint);

            ProcessResponse(udpClient);

            udpClient.Close();
            udpClient.Dispose();
        }

        private static void ProcessResponse(UdpClient udpClient)
        {
            Task d = Task.Delay(1000), curentTask;

            var response = udpClient.ReceiveAsync();

            var taskList = new List<Task>
            {
                d,
                response
            };

            while ((curentTask = Task.WhenAny(taskList).Result) != d)
            {
                if (curentTask != response) continue;

                taskList.Remove(response);

                OnUdpResponse?.Invoke(udpClient, response);

                response = udpClient.ReceiveAsync();
                taskList.Add(response);
            }
        }
    }
}