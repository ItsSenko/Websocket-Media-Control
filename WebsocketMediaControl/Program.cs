using WebSocketSharp;
using WebSocketSharp.Server;

namespace MediaControl
{
    public class WebsocketMediaControl
    {
        public static string path;
        public static void Main(string[] args)
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "Settings.txt");
            if (!File.Exists(path))
            {
                Logger.Log("Settings file does not exist, creating one...");

                Logger.Log("Please input the url for the websocket server. If left empty it will default to 'ws://localhost:3000'");
                string urlInput = Console.ReadLine();

                if (string.IsNullOrEmpty(urlInput))
                {
                    urlInput = "ws://localhost:3000";
                }

                File.WriteAllText(path, urlInput);
                Logger.Log("Settings Created");
            }

            string url = File.ReadAllText(path);

            Logger.Log("Starting Server");
            WebSocketServer server = new(url);
            server.AddWebSocketService<Server>("/");
            server.Start();

            Logger.Log($"Server started at {url}/");
            while (Console.ReadKey().Key != ConsoleKey.Q) { }
        }
    }
    public class Server : WebSocketBehavior
    {
        private MediaControl media;
        private bool isConnectionOpen;
        protected override void OnOpen()
        {
            Logger.Log("Client Connected");
            media = new MediaControl();
            media.Initialize();
            media.OnSongChanged += OnSongChanged;
            isConnectionOpen = true;
        }
        protected override void OnClose(CloseEventArgs e)
        {
            Logger.Log("Client Disconnected");
            isConnectionOpen = false;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Logger.Log($"Recieved {e.Data}");
            switch (e.Data)
            {
                case "next":
                    {
                        MediaControl.NextTrack();
                        Logger.Log($"Skipped to Next Song");
                        Send("log:Skipped to Next Song");
                        break;
                    }
                case "play":
                    {
                        MediaControl.PlayPause();
                        Logger.Log("Toggled Playing");
                        Send("log:Toggled Play Mode");
                        break;
                    }
                case "prev":
                    {
                        MediaControl.PrevTrack();
                        Logger.Log("Returned to Previous Track");
                        Send("log:Returned to Previous Track");
                        break;
                    }
            }
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Logger.Log($"ERROR: {e.Message}");
        }

        private void OnSongChanged(string newSong)
        {
            if (isConnectionOpen)
                Send($"song:{newSong}");
            if (!string.IsNullOrEmpty(newSong) && isConnectionOpen)
                Send($"log:Playing {newSong}");

            Logger.Log($"Song Changed to {newSong}");
        }
    }

    public class Logger
    {
        public static void Log(string message)
        {
            var time = DateTime.Now.ToString("[hh:mm:ss]");
            Console.WriteLine($"{time}: {message}");
        }
    }
}