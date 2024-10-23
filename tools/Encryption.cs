using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TCPServer.tools {
    public class Encryption {

        public static byte[] EncryptionString(string str) {
            // TODO
            RSA rsa = RSA.Create("");
            byte[] bytes = rsa.Encrypt(Encoding.UTF8.GetBytes(str), RSAEncryptionPadding.OaepSHA256);
            return bytes;
        }

    }
}
