using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer.tools {
    public class ConnectTools {
        /// <summary>
        /// 获取客户端IP
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static string GetClientIP(Socket socket) {
            IPEndPoint iPEndPoint = socket.LocalEndPoint as IPEndPoint;
            if (iPEndPoint != null) {
                return iPEndPoint.Address.ToString();
            }
            return null;
        }
        /// <summary>
        /// 获取客户端端口
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        public static int GetClientPort(Socket socket) {
            IPEndPoint iPEndPoint = socket.LocalEndPoint as IPEndPoint;
            if (iPEndPoint != null) {
                return iPEndPoint.Port;
            }
            return 0;
        }

        /// <summary>
        /// 获取指定连接的网卡 MAC 地址
        /// </summary>
        /// <param name="socket">已连接的 Socket 对象</param>
        /// <returns>网卡的 MAC 地址字符串，如果没有找到则返回 null</returns>
        public static string? GetClientMACAddress(Socket socket) {
            if (socket == null) {
                return null;
            }
            // 获取本地端点的 IP 地址
            IPAddress localIpAddress = ((IPEndPoint)socket.LocalEndPoint).Address;

            // 获取所有网络接口
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            // 遍历网络接口列表
            foreach (var ni in networkInterfaces) {
                // 检查网络接口是否处于活动状态且不是环回接口
                if (ni.OperationalStatus == OperationalStatus.Up && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback) {
                    // 获取该接口的所有 IP 地址
                    foreach (UnicastIPAddressInformation ipInfo in ni.GetIPProperties().UnicastAddresses) {
                        // 如果找到了匹配的 IP 地址
                        if (ipInfo.Address.Equals(localIpAddress)) {
                            // 获取并返回 MAC 地址
                            return ni.GetPhysicalAddress().ToString();
                        }
                    }
                }
            }

            // 如果没有找到匹配的接口
            return null;
        }

        public static string GetClientOS() {
            return Environment.OSVersion.ToString();
        }
    }
}
