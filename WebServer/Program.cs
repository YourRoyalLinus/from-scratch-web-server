using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServer
{
    class MyWebServer
    {
        public static void Main(string[] args)
        {
            var x = new WebServer.MyWebServer();
        }

        private TcpListener listener;
        private IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        private int port = 5050;

        public MyWebServer()
        {
            try
            {
                listener = new TcpListener(ipAddress, port);
                listener.Start();
                Console.WriteLine("Web Server has started... Press ^C to Quit");
                Thread th = new Thread(new ThreadStart(StartListen));
                th.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occued while listening: " + e.ToString());
            }
        }

        public string GetDefaultFileName(string sLocalDirectory)
        {
            string sLine = "";

            try
            {
                using (var sr = new StreamReader("Data/Default.Dat"))
                {
                    while ((sLine = sr.ReadLine()) != null)
                    {
                        if (File.Exists(sLocalDirectory + sLine) == true)
                            return sLine;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("An Exception occured: " + e.ToString());
            }

            return "";
        }

        public string GetLocalPath(string sWebServerRoot, string sDirName)
        {
            String sLine = "";
            string svirtualDir = "";
            string sRealDir;
            int iStartPos = 0;

            sDirName.Trim();
            sWebServerRoot = sWebServerRoot.ToLower();
            sDirName.ToLower();

            try
            {
                using (var sr = new StreamReader("Data/VDirs.Dat"))
                {
                    while((sLine = sr.ReadLine()) != null)
                    {
                        sLine.Trim();
                        if(sLine.Length > 0)
                        {
                            iStartPos = sLine.IndexOf(';');
                            sLine = sLine.ToLower();
                            svirtualDir = sLine.Substring(0, iStartPos);
                            sRealDir = sLine.Substring(iStartPos + 1);
                            if (svirtualDir == sDirName)
                                return sRealDir;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("An exception has occured: " + e.ToString());
            }

            return "";

        }

        public string GetMimeType(string sRequestedFile)
        {
            string sLine = "";
            string sMimeType = "";
            string sFileExt = "";
            string sMimeExt = "";

            sRequestedFile = sRequestedFile.ToLower();
            int iStartPos = sRequestedFile.IndexOf('.');
            sFileExt = sRequestedFile.Substring(iStartPos);

            try
            {
                using (var sr = new StreamReader("Data/Mimes.Dat"))
                {
                    while((sLine = sr.ReadLine()) != null)
                    {
                        sLine.Trim();
                        if (sLine.Length > 0)
                        {
                            iStartPos = sLine.IndexOf(';');
                            sLine = sLine.ToLower();
                            sMimeExt = sLine.Substring(0, iStartPos);
                            sMimeType = sLine.Substring(iStartPos + 1);
                            if (sMimeExt == sFileExt)
                                return sMimeType;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("An Exception has occurd: " + e.ToString());
            }

            return "";
        }

        public void SendHeader(string sHttpVersion, string sMimeHeader, int iTotBytes, string sStatusCode, ref Socket mySocket)
        {
            string sBuffer = "";

            if (sMimeHeader.Length == 0)
                sMimeHeader = "text/html";

            sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
            sBuffer = sBuffer + "Server: Ubuntu/18.04" + "\r\n";
            sBuffer = sBuffer + "Content-Type: " + sMimeHeader + "\r\n";
            sBuffer = sBuffer + "Accept-Ranges: bytes" + "\r\n";
            sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);
            SendToBrowser(bSendData, ref mySocket);
            Console.WriteLine("Total Bytes: " + iTotBytes.ToString());
        }

        public void SendToBrowser(string sData, ref Socket socket)
        {
            SendToBrowser(Encoding.ASCII.GetBytes(sData), ref socket);
        }

        public void SendToBrowser(Byte[] bSendData, ref Socket socket)
        {
            int numBytes = 0;

            try
            {
                if (socket.Connected)
                {
                    if ((numBytes = socket.Send(bSendData, bSendData.Length, 0)) == -1)
                        Console.WriteLine("Socket Error - cannot send packet");
                    else
                        Console.WriteLine("No. of bytes sent {0}", numBytes);

                }
                else
                    Console.WriteLine("Connection Dropped...");
            }
            catch(Exception e)
            {
                Console.WriteLine("Error Occured: {0}.", e.ToString());
            }
        }

        public void StartListen()
        {
            int iStartPos = 0;
            string sRequest = "";
            string sDirName = "";
            string sRequestedFile = "";
            string sErrorMessage = "";
            string sLocalDir = "";
            string sWebServerRoot = "/home/jon/Documents/projects/from-scratch-web-server/";
            string sPhysicalFilePath = "";
            string sFormattedMessage = "";
            string sHttpVersion = "";
            string sReponse = "";

            while(true)
            {
                Socket socket = listener.AcceptSocket();
                Console.WriteLine("Socket Type: " + socket.SocketType);
                if(socket.Connected)
                {
                    Console.WriteLine("\nClient Connected!\n=================\nClient IP {0}\n", socket.RemoteEndPoint);
                    Byte[] bReceive = new Byte[1024];
                    int i = socket.Receive(bReceive, bReceive.Length, 0);
                    string sBuffer = Encoding.ASCII.GetString(bReceive);
                    if (sBuffer.Substring(0, 3) != "GET")
                    {
                        Console.WriteLine("HTTP Method Not Supported. Only Supports \"GET\" method.");
                        socket.Close();
                        return;
                    }
                    iStartPos = sBuffer.IndexOf("HTTP", 1,StringComparison.CurrentCulture);
                    sHttpVersion = sBuffer.Substring(iStartPos, 8);
                    sRequest = sBuffer.Substring(0, iStartPos - 1);
                    sRequest.Replace("\\", "/");
                    if ((sRequest.IndexOf('.') < 1) && (!sRequest.EndsWith("/", StringComparison.CurrentCulture)))
                    { 
                        sRequest = sRequest + "/";
                    }
                    iStartPos = sRequest.LastIndexOf("/", StringComparison.CurrentCulture) + 1;
                    sRequestedFile = sRequest.Substring(iStartPos);
                    sDirName = sRequest.Substring(sRequest.IndexOf('/'), sRequest.LastIndexOf("/", StringComparison.CurrentCulture) - 3);
                }

                if (sDirName == "/")
                    sLocalDir = sWebServerRoot;
                else
                    sLocalDir = GetLocalPath(sWebServerRoot, sDirName);

                Console.WriteLine("Directory Requested: " + sLocalDir);

                if(sLocalDir.Length == 0)
                {
                    sErrorMessage = "<h2>ERROR! Requested Directory does not exist</h2><BR>";
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref socket);
                    SendToBrowser(sErrorMessage, ref socket);
                    socket.Close();
                    continue;
                }

                if(sRequestedFile.Length == 0)
                {
                    sRequestedFile = GetDefaultFileName(sLocalDir);
                    if(sRequestedFile == "")
                    {
                        sErrorMessage = "<h2>ERROR! No Default File Name Specified</h2>";
                        SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref socket);
                        SendToBrowser(sErrorMessage, ref socket);
                        socket.Close();
                        return;
                    }
                }

                string sMimeType = GetMimeType(sRequestedFile);
                sPhysicalFilePath = sLocalDir + sRequestedFile;
                Console.WriteLine("File Requested: " + sPhysicalFilePath);

                if (File.Exists(sPhysicalFilePath) == false)
                {
                    sErrorMessage = "<h2>404 ERROR! File Does Not Exist... </h2>";
                    SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref socket);
                    SendToBrowser(sErrorMessage, ref socket);
                    Console.WriteLine(sFormattedMessage);
                }
                else
                {
                    int iTotBytes = 0;
                    sReponse = "";
                    byte[] bytes;
                    using (var fs = new FileStream(sPhysicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var reader = new BinaryReader(fs))
                        {
                            bytes = new byte[fs.Length];
                            int read;
                            while ((read = reader.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                sReponse = sReponse + Encoding.ASCII.GetString(bytes, 0, read);
                                iTotBytes = iTotBytes + read;
                            }
                        }
                    }
                    SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", ref socket);
                    SendToBrowser(bytes, ref socket);
                }
                socket.Close();
            }
        }
    }
}