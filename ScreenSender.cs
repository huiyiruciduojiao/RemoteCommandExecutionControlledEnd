using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace TCPServer {
    public class ScreenSender {
        // 获取屏幕尺寸
        static int screenWidth = 1920;
        static int screenHeight = 1080;
        // 创建位图对象
        Bitmap bitmap = null;

        // 创建Graphics对象
        Graphics graphics = null;

        Size size = new Size(screenWidth, screenHeight);
        public bool isError = false;
        public byte[] GetScreen() {
            // 创建位图对象
            bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);

            // 创建Graphics对象
            graphics = Graphics.FromImage(bitmap);

            // 获取屏幕截图
            graphics.CopyFromScreen(0, 0, 0, 0, size, CopyPixelOperation.SourceCopy);

            // 将截图数据存储在MemoryStream中
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Png);
            return memoryStream.ToArray();

        }
        public Bitmap Get() {
            // 创建位图对象
            bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb);

            // 创建Graphics对象
            graphics = Graphics.FromImage(bitmap);

            // 获取屏幕截图
            try {
                graphics.CopyFromScreen(0, 0, 0, 0, size, CopyPixelOperation.SourceCopy);
            }catch(Exception ex) {
                isError = true;
                Console.WriteLine(ex.Message);
            }

            graphics.Dispose();
            return bitmap;
        }
        public byte[] GetScreenData(Bitmap bitmap) {
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Jpeg);
            return memoryStream.ToArray();
        }

    }
}