using Newtonsoft.Json;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TCPServer.RemoteService;

namespace TCPServer {
    internal class Program {
        //用于通信的Socket
        Socket socketSend;
        //用于监听的SOCKET
        Socket socketWatch;

        //文件传输
        Socket fileSocket;
        Socket fileSocetWatch;

        //创建监听连接的线程
        Thread AcceptSocketThread;
        //定义是否执行过exit命令
        private bool isExit = false;


        //创建用于监听屏幕连接的Socket
        Socket screenSocket;
        //创建用于发送屏幕数据的Socket
        Socket screenSend;

        //创建用于文件事件监听的Socket
        Socket listenFileSocket;
        //创建用于发送文件数据的Socket
        Socket sendFileSocket;

        private ScreenSender Screen = new ScreenSender();
        private bool flg = false;
        //文件信息本地路径
        private static String path = Environment.CurrentDirectory;
        Command Command = new Command();
        static void Main(string[] args) {
            ConnectRemotely connectRemotely = new ConnectRemotely("127.0.0.1", 51222);
            connectRemotely.Start();
            new Program().Start();
        }
        public void Start() {
            //绑定命令执行结果回调事件
            Command.Output += Command_Output;
            Command.Error += Command_Error;
            Command.Exited += Command_Exited;

            //当点击开始监听的时候在服务器端创建一个负责监听IP地址和端口号的Socket
            socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //获取ip地址
            IPAddress ip = IPAddress.Any;
            //创建端口号
            IPEndPoint point = new IPEndPoint(ip, 8888); //此时已经把IP地址和Port绑定了
            //绑定IP地址和端口号
            socketWatch.Bind(point);//所以此处只需绑定端口号就可以地址和端口号都绑定
            Console.WriteLine("监听成功" + " \r\n");

            fileSocetWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            fileSocetWatch.Bind(new IPEndPoint(ip, 8889));
            Console.WriteLine("文件传输监听成功" + "\r\n");

            screenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            screenSocket.Bind(new IPEndPoint(ip, 7878));
            Console.WriteLine("屏幕监听成功\r\n");

            listenFileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenFileSocket.Bind(new IPEndPoint(ip, 7979));
            Console.WriteLine("文件状态监听成功\r\n");

            //创建线程
            AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen));
            AcceptSocketThread.IsBackground = false;
            AcceptSocketThread.Start(socketWatch);

            //创建文件传输线程
            Thread fileSocketThread = new Thread(new ParameterizedThreadStart(StartFileListen));
            fileSocketThread.IsBackground = false;
            fileSocketThread.Start(fileSocetWatch);

            //开始屏幕传输
            Thread scrennThread = new Thread(new ParameterizedThreadStart(ListenScrenn));
            scrennThread.IsBackground = false;
            scrennThread.Start(screenSocket);

            //开始监听文件显示
            Thread showFIle = new Thread(new ParameterizedThreadStart(ListenFileInfo));
            showFIle.IsBackground = false;
            showFIle.Start(listenFileSocket);
        }
        private void ListenFileInfo(object obj) {
            Socket socket = (Socket)obj;
            socket.Listen();
            while (true) {
                sendFileSocket = socket.Accept();
                string strIp = sendFileSocket.RemoteEndPoint.ToString();
                Console.WriteLine($"本地文件数据传输到{strIp}");
                flg = false;
                Thread sendFileIinfo = new Thread(new ParameterizedThreadStart(SendFileInfo));
                sendFileIinfo.IsBackground = true;
                sendFileIinfo.Start(sendFileSocket);
            }
        }

        private void SendFileInfo(object obj) {

            Socket socket = obj as Socket;
            FileInfoData[]? fileInfoDatas = null;
            DiskInfoData[]? diskInfoDatas = null;
            if (flg) {//需要文件系统信息
                diskInfoDatas = GetDiskInfoData();
            } else {//需要文件信息
                fileInfoDatas = GetFileInfoDatas();
            }
            FileNodeInfoData fileNodeInfoData = new FileNodeInfoData {
                FileInfoDatas = fileInfoDatas,
                FileSystemNode = diskInfoDatas,
                FileSystemOrFile = flg
            };
            Console.WriteLine("发送数据");
            string serializedData = JsonConvert.SerializeObject(fileNodeInfoData);

            Byte[] bytes = Encoding.UTF8.GetBytes(serializedData);
            //将数据发送到网络中
            socket.Send(bytes);

        }
        /// <summary>
        /// 根据工作路径获取该路径的FileInfoData数组
        /// </summary>
        /// <returns></returns>
        private FileInfoData[] GetFileInfoDatas() {

            //获取当前工作路径文件对象
            DirectoryInfo directory = new DirectoryInfo(path);

            FileInfo[] fileInfos = directory.GetFiles();
            Console.WriteLine("文件个数" + fileInfos.Length);

            DirectoryInfo[] directories = directory.GetDirectories();
            Console.WriteLine("文件夹个数" + directories.Length);
            FileInfoData[] fileInfoDatas = new FileInfoData[fileInfos.Length + directories.Length];

            Console.WriteLine(fileInfoDatas.Length);

            for (int i = 0; i < fileInfoDatas.Length; i++) {
                if (i < fileInfos.Length) {
                    fileInfoDatas[i] = new FileInfoData {
                        FilePath = fileInfos[i].FullName,
                        FileSize = fileInfos[i].Length,
                        FileName = fileInfos[i].Name,
                        EditTime = fileInfos[i].LastWriteTime.ToString(),
                        ReadTime = fileInfos[i].LastAccessTime.ToString(),
                        FileType = false
                    };
                } else {
                    int index = i - fileInfos.Length;
                    fileInfoDatas[i] = new FileInfoData {
                        FilePath = directories[index].FullName,
                        FileSize = directories[index].Name.Length,
                        FileName = directories[index].Name,
                        EditTime = directories[index].LastWriteTime.ToString(),
                        ReadTime = directories[index].LastAccessTime.ToString(),
                        FileType = true
                    };
                }
            }
            return fileInfoDatas;
        }
        /// <summary>
        /// 获取系统中所有文件系统信息
        /// </summary>
        /// <returns></returns>
        private DiskInfoData[] GetDiskInfoData() {
            DriveInfo[] allDirves = DriveInfo.GetDrives();
            DiskInfoData[] diskInfoDatas = new DiskInfoData[allDirves.Length];
            //将磁盘信息保存到对象中
            for (int i = 0; i < allDirves.Length; i++) {
                if (!allDirves[i].IsReady) {
                    continue;
                }
                //创建单个DiskInfoData对象
                DiskInfoData diskInfoData = new DiskInfoData {
                    DiskName = allDirves[i].Name,
                    DiskTotalSize = allDirves[i].TotalSize,
                    DiskTotalFreeSpace = allDirves[i].TotalFreeSpace,
                    DiskFormat = allDirves[i].DriveFormat,
                    DiskType = allDirves[i].DriveType.ToString()
                };
                //将单个DiskInfoData对象添加到数组中
                diskInfoDatas[i] = diskInfoData;
            }
            return diskInfoDatas;

        }

        private string CdShowFileInfoPath(string newPathSegment) {
            try {
                string newPath = Path.Combine(path, newPathSegment);
                newPath = Path.GetFullPath(newPath);
                if (!Directory.Exists(newPath)) {
                    path = Environment.CurrentDirectory;
                    return Environment.CurrentDirectory;
                } else {
                    path = newPath;
                    return newPath;
                }
            } catch (Exception e) {
                Console.WriteLine(e);
                path = Environment.CurrentDirectory;
                return Environment.CurrentDirectory;
            }
        }
        private void ListenScrenn(object obj) {
            Socket screenSocket = obj as Socket;//转换为Socket类型
            screenSocket.Listen();
            //设置单次发送最大字节数
            int maxBit = 65507;
            while (true) {
                //等待客户端的连接，并且创建一个用于通信的Socket
                socketSend = screenSocket.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = socketSend.RemoteEndPoint.ToString();

                string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";

                Console.WriteLine(strMsg);

                //定义发送屏幕数据的线程
                Thread threadReceive = new Thread(new ParameterizedThreadStart(SendScrenn));
                threadReceive.Priority = ThreadPriority.Highest;
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);
            }

        }
        private void SendScrenn(object obj) {
            Socket socket = obj as Socket;
            try {
                if (socket != null) {
                    Bitmap bitmap = null;
                    bool flage = false;
                    Screen.isError = false;
                    Task task1 = new Task(() => {
                        while (true) {
                            bitmap = Screen.Get();
                            if (flage || Screen.isError) {
                                break;
                            }
                        }
                    });
                    Task task2 = new Task(() => {
                        while (true) {
                            if (bitmap != null) {
                                try {
                                    if (Screen.isError) {
                                        socket.Close();
                                        break;
                                    }
                                    byte[] a = Screen.GetScreenData(bitmap);
                                    socket.Send(BitConverter.GetBytes(a.Length));
                                    Thread.Sleep(1);
                                    socket.Send(a);
                                } catch (Exception ex) {
                                    Console.WriteLine(ex);
                                    socket.Close();
                                    socket.Dispose();
                                    flage = true;
                                    break;
                                }
                            }
                        }

                    });
                    task1.Start();
                    task2.Start();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }
            //  Console.WriteLine("000");
        }
        private void StartListen(object obj) {
            Socket socketWatch = obj as Socket;//转换为Socket类型
            socketWatch.Listen();
            while (true) {
                //等待客户端的连接，并且创建一个用于通信的Socket
                socketSend = socketWatch.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = socketSend.RemoteEndPoint.ToString();

                string strMsg = "远程主机：" + socketSend.RemoteEndPoint + "连接成功";

                Console.WriteLine(strMsg);

                //定义接收客户端消息的线程
                Thread threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                threadReceive.IsBackground = true;
                threadReceive.Start(socketSend);

            }
        }
        private void StartFileListen(object obj) {
            Socket? socket = obj as Socket;
            socket.Listen();
            while (true) {
                fileSocket = socket.Accept();
                //获取远程主机的ip地址和端口号
                string strIp = fileSocket.RemoteEndPoint.ToString();

                string strMsg = "远程主机：" + fileSocket.RemoteEndPoint + "连接成功";

                Console.WriteLine(strMsg);

                //定义接收客户端消息的线程
                Thread threadReceive = new Thread(new ParameterizedThreadStart(DocumentOfAcceptance));
                threadReceive.IsBackground = true;
                threadReceive.Start(fileSocket);
            }
        }
        private void DocumentOfAcceptance(object obj) {
            Socket socket = obj as Socket;
            byte[] buffer = new byte[2048];
            //实际接收到的字节数
            int r = socket.Receive(buffer);
            string str = null;
            if (r > 0) {
                str = Encoding.UTF8.GetString(buffer, 0, r);
                string strReceiveMsg = "接收：" + socket.RemoteEndPoint + "发送的消息:" + str;
                Console.WriteLine(strReceiveMsg);
            }
            FileStream fs = null;
            try {
                // 判断文件是否存在，如果不存在则创建一个空文件
                if (!File.Exists(str)) {
                    using (File.Create(str)) {
                        Console.WriteLine($"文件 {str} 创建成功。");
                    }
                }
                using (fs = new FileStream(str, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
                    buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = socket.Receive(buffer, buffer.Length, SocketFlags.None)) > 0) {
                        fs.Write(buffer, 0, bytesRead);
                    }
                    Console.WriteLine("文件接收完成");
                    fs?.Close();
                    socket?.Close();

                }
            } catch (IOException ex) {
                Console.WriteLine($"文件 {str} 正在被使用，错误信息：{ex}");
                if (fs != null) {
                    fs.Close();
                }
                if (socket != null) {
                    socket.Close();
                }
            } finally {
                if (socket != null) {
                    socket.Close();
                }
                if (fs != null) {
                    Console.WriteLine("文件流关闭");
                    fs.Close();
                    fs = null;
                }
            }
        }
        /// <summary>
        /// 发送数据到网络中
        /// </summary>
        /// <param name="socket">连接对象</param>
        /// <param name="filePath">文件路径</param>
        private void SendFileData(Socket socket, string filePath) {
            try {
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
                    byte[] buffer = new byte[1024]; // 每次读取 1KB 数据
                    int bytesRead;

                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0) {
                        // 发送读取的数据块
                        socket.Send(buffer, bytesRead, SocketFlags.None);
                    }
                }

                Console.WriteLine("文件已发送成功：" + filePath);
            } catch (Exception ex) {
                Console.WriteLine("发送文件时出错：" + ex.Message);
            }
        }
        /// <summary>
        /// 服务器端不停地接收客户端发送的消息
        /// </summary>
        /// <param name="obj"></param>
        private void Receive(object obj) {
            try {
                Socket socketSend = obj as Socket;
                while (true) {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[2048];
                    //实际接收到的有效字节数
                    int count = socketSend.Receive(buffer);
                    if (count == 0) {//count 表示客户端关闭，要退出循环
                        break;
                    } else {
                        string str = Encoding.UTF8.GetString(buffer, 0, count);
                        string strReceiveMsg = "接收：" + socketSend.RemoteEndPoint + "发送的消息:" + str;
                        if (str.Equals("restart")) {
                            Command.Stop();
                            Command = null;
                            Command = new Command();
                            Command.Output += Command_Output;
                            Command.Error += Command_Error;
                            Command.Exited += Command_Exited;
                            isExit = false;
                            Reply("远程终端已重新启动\n", socketSend);

                        } else if (str.StartsWith("/cd ")) {
                            String newPath = str.Replace("/cd ", "");
                            flg = false;
                            path = CdShowFileInfoPath(newPath);
                            if (sendFileSocket != null) {
                                SendFileInfo(sendFileSocket);
                            }

                        } else if (str.Equals("/diskInfo")) {
                            Console.WriteLine("发送文件系统数据");
                            flg = true;
                            if (sendFileSocket != null) {
                                SendFileInfo(sendFileSocket);
                            }
                        } else {
                            if (isExit) {
                                Command = new Command();
                                Command.Output += Command_Output;
                                Command.Error += Command_Error;
                                Command.Exited += Command_Exited;
                                isExit = false;
                            }
                            Command.RunCMD(str, socketSend);
                            Console.WriteLine(strReceiveMsg);
                        }

                    }
                }
            } catch (Exception ex) {
                if (socketSend != null) {
                    socketSend.Close();
                }
            }

        }
        private void Reply(String strMsg, Socket socket) {
            try {
                if (socket != null) {

                    byte[] buffer = Encoding.UTF8.GetBytes(strMsg);
                    socket.Send(buffer);
                }
            } catch (Exception ex) {

            }
        }
        private void Command_Exited(Socket socket) {
            isExit = true;
            Reply("进程退出", socket);
        }
        private void Command_Error(string msg, Socket socket) {
            Reply(msg, socket);
        }
        private void Command_Output(string msg, Socket socket) {

            Reply(msg, socket);
        }

    }
}