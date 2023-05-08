using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Toletus.Extensions;
using Toletus.LiteNet2.Command;
using Toletus.LiteNet2.Command.Enums;

namespace Toletus.LiteNet2.Base
{
    public class LiteNet2BoardBase
    {
        public static Action<string>? Log;

        public const int Port = 7878;

        public IPAddress Ip { get; set; }
        public IPAddress NetworkIp { get; set; }
        public int Id { get; set; }
        public bool HasFingerprintReader { get; set; }

        public delegate void IdentificationHandler(LiteNet2BoardBase liteNet2Board, Identification identification);
        public event Action<ResponseCommand> OnResponse;
        public event IdentificationHandler OnIdentification;
        public event Action<LiteNet2BoardBase, ConnectionStatus> OnConnectionStatusChanged;
        public event Action<string> OnStatus;
        public event Action<LiteNet2BoardBase, SendCommand> OnSend;

        protected TcpClient TcpClient;

        public bool Connected => TcpClient?.Client != null &&  TcpClient.Connected;

        public LiteNet2BoardBase(IPAddress ip, int? id = null)
        {
            Ip = ip;
            if (id.HasValue) Id = id.Value;
        }

        public override string ToString()
        {
            return $"LiteNet2 #{Id} {Ip}:{Port}";
        }

        public void Connect()
        {
            try
            {
                TcpClient = new TcpClient();
                TcpClient.Connect(Ip, Port);

                _ = Response();

                OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Connected);
            }
            catch (SocketException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private void CheckConnection()
        {
            Send(Commands.GetId);
        }

        public void Close()
        {
            TcpClient?.Close();
            OnConnectionStatusChanged?.Invoke(this, ConnectionStatus.Closed);
        }

        private async Task Response()
        {
            Log?.Invoke("Response start");

            var buffer = new byte[1024];
            try
            {
                var bytesLidos = 1;

                while (bytesLidos != 0)
                {
                    bytesLidos = await TcpClient.GetStream().ReadAsync(buffer, 0, buffer.Length);

                    var respFull = buffer.Take(bytesLidos).ToArray();

                    var skip = 0;
                    while (respFull.Length > skip)
                    {
                        var resp = respFull.Skip(skip).Take(20).ToArray();

                        var responseCommand = ProcessResponse(resp);

                        OnResponse?.Invoke(responseCommand);

                        skip += 20;
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                Log?.Invoke($"Response ObjectDisposedException {e.ToLogString(Environment.StackTrace)}");
            }
            catch (System.IO.IOException e)
            {
                Log?.Invoke($"Connection closed. Receive response finised. (IOException)");
                TcpClient.Close();
                throw;
            }
            catch (Exception e)
            {
                Log?.Invoke($"Response Exception {e.ToLogString(Environment.StackTrace)}");
                Close();
                throw;
            }
            finally
            {
                Log?.Invoke("Response finally");
            }
        }

        private ResponseCommand ProcessResponse(byte[] resp)
        {
            var response = new ResponseCommand(resp);

            switch (response.Command)
            {
                case Commands.NegativeIdentificationByFingerprintReader:
                case Commands.PositiveIdentificationByFingerprintReader:
                case Commands.IdentificationByBarCode:
                case Commands.IdentificationByRfId:
                case Commands.IdentificationByKeyboard:
                    response.Identification = ProcessIdentificationResponse(response);
                    break;
            }

            return response;
        }

        private Identification ProcessIdentificationResponse(ResponseCommand response)
        {
            Identification identification = null;

            switch (response.Command)
            {
                case Commands.IdentificationByKeyboard:
                    identification = new Identification(IdentificationDevice.Keyboard, int.Parse(response.DataString));
                    break;
                case Commands.IdentificationByBarCode:
                    identification = new Identification(IdentificationDevice.BarCode, int.Parse(response.DataString));
                    break;
                case Commands.IdentificationByRfId:
                    identification = new Identification(IdentificationDevice.Rfid, int.Parse(response.DataString));
                    break;
                case Commands.PositiveIdentificationByFingerprintReader:
                case Commands.NegativeIdentificationByFingerprintReader:
                    identification = new Identification(IdentificationDevice.EmbeddedFingerprint, int.Parse(response.Data.ToString()));
                    HasFingerprintReader = true;
                    break;
            }

            OnIdentification?.Invoke(this, identification);

            return identification;
        }

        public void Send(Commands command, int parameter)
        {
            Send(command, BitConverter.GetBytes(parameter));
        }

        public void Send(Commands command, byte parameter)
        {
            Send(command, new[] { parameter });
        }

        public void Send(Commands command, string parameter)
        {
            parameter = parameter.Truncate(16).PadRight(16, '\0');

            Send(command, Encoding.ASCII.GetBytes(parameter));
        }

        public void Send(Commands command, byte[] parameter = null)
        {
            var send = new SendCommand(command, parameter);

            Send(send);
        }

        public void Send(ushort comando, byte[] parameter = null)
        {
            var send = new SendCommand(comando, parameter);

            Send(send);
        }

        public void Send(SendCommand send)
        {
            OnSend?.Invoke(this, send);

            if (!Connected) 
            {
                TryReconnect();
                return;
            }

            var stream = TcpClient.GetStream();

            try
            {
                stream.Write(send.Payload, 0, send.Payload.Length);
            }
            catch (SocketException sex)
            {
                TcpClient?.Close();
                throw;
            }
            catch (System.IO.IOException iox)
            {
                TcpClient?.Close();
                throw;
            }
            catch (Exception e)
            {
                TcpClient?.Close();
                throw;
            }
        }

        public bool IsConnected2()
        {
            if (TcpClient.Client.Poll(0, SelectMode.SelectRead))
            {
                byte[] buff = new byte[1];
                if (TcpClient.Client.Receive(buff, SocketFlags.Peek) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        bool IsConnected()
        {
            if (TcpClient.Client.Connected)
            {
                if ((TcpClient.Client.Poll(0, SelectMode.SelectWrite)) && (!TcpClient.Client.Poll(0, SelectMode.SelectError)))
                {
                    byte[] buffer = new byte[1];
                    if (TcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool _reconnecting;

        private void TryReconnect()
        {
            Task.Run(async () =>
            {
                try
                {
                    if (_reconnecting) return;

                    OnStatus?.Invoke("Reconnecting");

                    while (!Connected)
                    {
                        _reconnecting = true;
                        await Task.Delay(200);
                        Connect();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    _reconnecting = false;
                }
            });
        }

        public void EventStatus(string status)
        {
            OnStatus?.Invoke(status);
        }
    }
}