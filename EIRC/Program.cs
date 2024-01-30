using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace IRCServerEmulator
{
    class Program
    {
        public static List<IRCClient> clients = new List<IRCClient>();
        internal static string serverHostname = "localhost";
        public static readonly object lockObject = new object();


        static void Main(string[] args)
        {
            TcpListener server = new TcpListener(System.Net.IPAddress.Any, 6667);
            server.Start();

            Console.WriteLine("IRC Server Emulator running on port 6667");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                IRCClient ircClient = new IRCClient(client);
                lock (lockObject)
                {
                    clients.Add(ircClient);
                }

                Thread clientThread = new Thread(new ThreadStart(ircClient.HandleClient));
                clientThread.Start();
            }
        }
    }

    class IRCClient
    {
        private TcpClient tcpClient;
        private StreamReader reader;
        private StreamWriter writer;
        private string Username;
        private bool disposed = false;
        private readonly object disposeLock = new object();

        public IRCClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            this.reader = new StreamReader(tcpClient.GetStream());
            this.writer = new StreamWriter(tcpClient.GetStream()) { AutoFlush = true };
        }

        public void HandleClient()
        {
            try
            {
                SendWelcomeMessage();

                while (true)
                {
                    string message = reader.ReadLine();
                    if (message == null)
                        break;

                    Console.WriteLine("Received: " + message);

                    ProcessCommand(message);
                }
            }
            catch (Exception ex)
            {
               
            }
            finally
            {
                DisposeResources();
                
            }
        }

        private void SendWelcomeMessage()
        {
            writer.WriteLine("Welcome to pierdolnik irc");
        }

        private void ProcessCommand(string message)
        {
            string[] parts = message.Split(' ');

            if (parts.Length < 2)
                return;

            string command = parts[0];
            string parameter = parts[1];
            Console.WriteLine(message);

            switch (command)
            {
                case "USER":
                    HandleUser(command, parameter);
                    break;
                case "JOIN":
                    HandleJoin(parameter);
                    break;
                case "PRIVMSG":
                    HandlePrivMsg(parts, message);
                    break;
                case "NICK":
                    HandleNick(parameter);
                    break;
            }
        }

        private void HandleUser(string command, string parameter)
        {
            Username = parameter;
            Console.WriteLine($"User {Username} identified");
            writer.WriteLine("");
        }
        private List<string> joinedChannels = new List<string>();

        public void JoinChannel(string channel)
        {
            if (!joinedChannels.Contains(channel))
            {
                joinedChannels.Add(channel);
                Console.WriteLine($"{Username} JOINed {channel}");
            }
        }

        public void LeaveChannel(string channel)
        {
            joinedChannels.Remove(channel);
            Console.WriteLine($"{Username} LEFT {channel}");
        }
        private void HandleJoin(string channel)
        {
            lock (Program.lockObject)
            {
                if (tcpClient.Connected)
                {
                    if(!channel.Contains("#"))
                    {
                        writer.WriteLine($"{Username}, You're restricted from joining this channel");
                        return;
                    }
                    JoinChannel(channel);
                    writer.WriteLine($":{Program.serverHostname} 331 {Username} {channel} :NO_TOPIC");
                    writer.WriteLine($":{Program.serverHostname} 332 {Username} {channel} :troll_face");
                    writer.WriteLine($":{Program.serverHostname} 333 {Username} {channel} {Username}!user@host 1234567890");
                    writer.WriteLine($":{Program.serverHostname} 353 {Username} = {channel} :{Username}");
                    writer.WriteLine($":{Program.serverHostname} 366 {Username} {channel} :End of /NAMES list.");
                    writer.WriteLine($":{Username}!user@host JOIN :{channel}");
                }
            }
        }
        private void HandleNick(string nick)
        {
            Username = nick.Replace("-osu", "");


        }
        private void HandlePrivMsg(string[] parts, string fullmsg)
        {
            if (parts.Length <= 1)
                return;
            string target = parts[1];
           
            var message = fullmsg.Split(':')[1].Replace(":","");

            

            lock(Program.lockObject)
            {
                if (tcpClient.Connected)
                {
                    
                    foreach (var user in Program.clients)
                    {
                        
                        if (user.tcpClient.Connected && user.Username != null && user.Username != Username)
                        {
                            if (user.IsInChannel(target))
                            {
                                
                                user.writer.WriteLine($":{Username} PRIVMSG {target} :{message}");
                            } else
                            {
                                
                                if (!parts[2].Contains("#"))
                                {
                                    
                                    Console.WriteLine(user.Username);
                                    Console.WriteLine(parts[1].Replace(":", ""));
                                    // then its priv msg
                                    if (user.Username == parts[1].Replace(":",""))
                                    {
                                        
                                        Console.WriteLine($":{Username} PRIVMSG {user.Username} :{message}");
                                        user.writer.WriteLine($":{Username} PRIVMSG {user.Username} :{message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public bool IsInChannel(string channel)
        {
            return joinedChannels.Contains(channel);
        }



        private void DisposeResources()
        {
            lock (disposeLock)
            {
                if (!disposed)
                {
                    tcpClient.Close();
                    reader.Dispose();
                    writer.Dispose();
                    disposed = true;
                }
            }
        }
    }
}
