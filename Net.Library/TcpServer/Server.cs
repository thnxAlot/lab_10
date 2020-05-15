using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace SomeProject.Library.Server
{
    public class Server
    {
        TcpListener serverListener;
        static int current_clients=0;
        int max_clients = 5;
        static int counter_files =0;

        public Server()
        {
            serverListener = new TcpListener(IPAddress.Loopback, 8080);
        }

        public bool TurnOffListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Stop();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn off listener: " + e.Message);
                return false;
            }
        }

        

        public async Task TurnOnListener()
        {
            try
            {
                if (serverListener != null)

                    serverListener.Start();

                Console.WriteLine("Waiting for connections");
                ThreadPool.SetMaxThreads(max_clients, max_clients);
                ThreadPool.SetMinThreads(1, 1);
                while (true)
                    ThreadPool.QueueUserWorkItem(new WaitCallback(get_client), serverListener.AcceptTcpClient());
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn on listener: " + e.Message);
            }
        }



        

        static void get_client(object client)
        {
            int cur = Interlocked.Increment(ref current_clients);
            Console.WriteLine("New connection " + cur);

            OperationResult result = set_type((TcpClient)client).Result;

            if (result.Result == Result.Fail)
                Console.WriteLine("Unexpected error: " + result.Message);
            else
                Console.WriteLine("New message from client: " + result.Message);
            Interlocked.Decrement(ref current_clients);
        }

        
        public async static Task<OperationResult> set_type(TcpClient client)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder();
                
                byte[] data = new byte[500];
                string msg = "";
                NetworkStream stream = client.GetStream();
                
                int bytes = await stream.ReadAsync(data, 0, data.Length);
                recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                if(recievedMessage.ToString()=="")
                    Console.WriteLine("empty");
                //else
                //Console.WriteLine(recievedMessage.ToString());
                string mes = recievedMessage.ToString();
                string predic;
                try
                {
                    predic = mes.Substring(0, 6);
                }
                catch
                {
                    predic = "none";
                }
                if(predic=="fle|_|")
                {
                    
                    Console.WriteLine(mes.Substring(6, mes.IndexOf("|", mes.IndexOf("|", mes.IndexOf("|", 0) + 1) + 1) - 
                        mes.IndexOf("|", mes.IndexOf("|", 0) + 1)-1));
                    SendMessageToClient(stream, ReceiveFileFromClient(stream, mes.Substring(6, mes.IndexOf("|", mes.IndexOf("|", mes.IndexOf("|", 0) + 1) + 1) -
                        mes.IndexOf("|", mes.IndexOf("|", 0) + 1) - 1), mes.Substring(mes.IndexOf("|", mes.IndexOf("|", mes.IndexOf("|", 0) + 1) + 1) + 1)).Message);
                }
                else
                {
                    //Console.WriteLine("msg");
                    if (stream.DataAvailable)
                    {
                        //Console.WriteLine("new portion");
                        msg = ReceiveMessageFromClient(stream, recievedMessage);
                    }
                    else
                    {
                        //Console.WriteLine("all");
                        msg = recievedMessage.ToString();
                    }   
                    SendMessageToClient(stream, msg);
                }
                
                client.Close();

                return new OperationResult(Result.OK, msg);
            }
            catch (Exception e)
            {
                Console.WriteLine("error");
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        

        private static string ReceiveMessageFromClient(NetworkStream stream, StringBuilder recievedMessage)
        {
            try
            {
                byte[] data = new byte[256];
                while(stream.DataAvailable)
                {
                    int bytes = stream.Read(data, 0, data.Length);

                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }

                Console.WriteLine("->> " + recievedMessage.ToString());



                return recievedMessage.ToString();

            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        
        private static OperationResult ReceiveFileFromClient(NetworkStream stream, string file,string first_portion)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder().Append(file);
                byte[] data = new byte[5];

                if (!Directory.Exists(DateTime.Now.ToString("yyyy-MM-dd")))
                    Directory.CreateDirectory(DateTime.Now.ToString("yyyy-MM-dd"));
                int cur_number_of_files = Interlocked.Increment(ref counter_files);
                Console.WriteLine(file + " file");
                //Console.WriteLine(msg + " msg");
                string file_name = "File" + cur_number_of_files + "." + file.Split('.')[file.Split('.').Length - 1];
                Console.WriteLine(file_name);
                FileStream fstream = new FileStream(DateTime.Now.ToString("yyyy-MM-dd") + "\\" + "File" + cur_number_of_files + "." + file.Split('.')[file.Split('.').Length - 1], FileMode.Create);
                //StreamWriter writer = new StreamWriter();
                byte[] first = Encoding.UTF8.GetBytes(first_portion);
                fstream.Write(first,0,first.Length);
                int len = first.Length;
                while (stream.DataAvailable)
                {
                    int ammount = stream.Read(data, 0, data.Length);
                    fstream.Write(data, 0, ammount);
                    len += ammount;
                }
                fstream.Close();

                return new OperationResult(Result.OK, file_name);
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        public static OperationResult SendMessageToClient(NetworkStream stream, string message)
        {
            try
            {
                

                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                stream.Close();
        
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
            return new OperationResult(Result.OK, "");
        }
    }
}