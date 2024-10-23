using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TCPServer.tools;

namespace TCPServer.RemoteService {
    public class ConnectRemotely {
        private Socket _socket;
        private uint _port;
        private string _ip;
        public ConnectRemotely(string ip, uint port) {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _port = port;
            _ip = ip;
            try {
                _socket.Connect(ip, (int)port);
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }
        public void Start() {
            //判断连接是否可用
            if (_socket == null || !_socket.Connected) {
                return;
            }
            //发送测试命令
            _socket.Send(Encoding.UTF8.GetBytes("hello"));

            string myIp = ConnectTools.GetClientIP(_socket);
            int myPort = ConnectTools.GetClientPort(_socket);
            string myMac = ConnectTools.GetClientMACAddress(_socket);
            string myOs = ConnectTools.GetClientOS();

            //创建连接信息对象
            ConnectInfo info = new ConnectInfo(myIp, (uint)myPort, myMac, myOs);
            //序列化json
            String json  = JsonSerializer.Serialize(info);
            Console.WriteLine(json);

            //提交连接信息
            _socket.Send(Encryption.EncryptionString(json));
            receiveMsg();
        }
        public void SendMsg(string msg) {
            _socket.Send(Encoding.UTF8.GetBytes(msg));
        }
        public void receiveMsg() {
            Task t = new Task(() => {
                while (true) {
                    try {
                        byte[] buffer = new byte[2048];
                        int len = _socket.Receive(buffer);
                        if (len == 0) {
                            break;
                        }
                        string msg = Encoding.UTF8.GetString(buffer, 0, len);
                        if (msg.Length == 0) {
                            continue;
                        }
                        Console.WriteLine(msg);

                    } catch (Exception e) {
                        Console.WriteLine(e.Message);
                    }

                }
            });
        }
    }
}
