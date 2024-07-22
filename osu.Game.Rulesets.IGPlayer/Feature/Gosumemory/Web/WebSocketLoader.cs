using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Rulesets.IGPlayer.Feature.Gosumemory.Data;

namespace osu.Game.Rulesets.IGPlayer.Feature.Gosumemory.Web
{
    public partial class WebSocketLoader : CompositeDrawable
    {
        public readonly DataRoot DataRoot = new DataRoot();

        public WebSocketLoader()
        {
            AlwaysPresent = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Logging.Log("WS LOAD!");
            Schedule(startServer);
        }

        public void Restart()
        {
            stopServer();
            startServer();
        }

        public void Broadcast(string text)
        {
            if (Server == null) throw new NullDependencyException("Server not initialized");

            Server.MulticastText(text);
        }

        private void stopServer()
        {
            if (Server == null) return;

            Server.Stop();
            Server.Dispose();

            try
            {
                OnServerStop?.Invoke(Server);
            }
            catch (Exception e)
            {
                Logging.Log($"Error occurred calling OnServerStop: {e.Message}");
                Logging.Log(e.StackTrace ?? "<No stacktrace>");
            }

            Server = null;
        }

        private void startServer()
        {
            Logging.Log("Initializing WebSocket Server...");

            try
            {
                var ip = IPAddress.Loopback;
                int port = 24050;

                this.Server = new GosuServer(ip, port);

                Server.Start();

                try
                {
                    OnServerStart?.Invoke(Server);
                }
                catch (Exception e)
                {
                    Logging.Log($"Error occurred calling OnServerStart: {e.Message}");
                    Logging.Log(e.StackTrace ?? "<No stacktrace>");
                }

                Logging.Log("Done!");
                Logging.Log($"WS Server opened at http://{Server.Address}:{Server.Port}");
            }
            catch (Exception e)
            {
                Logging.Log($"无法启动WebSocket服务器: {e}", level: LogLevel.Important);
                Logging.Log(e.ToString());
            }
        }

        public Action<GosuServer>? OnServerStart;
        public Action<GosuServer>? OnServerStop;

        protected override void Dispose(bool isDisposing)
        {
            stopServer();

            base.Dispose(isDisposing);
        }

        public GosuServer? Server;

        public partial class AssetManager
        {
            private readonly List<Storage> storages = new List<Storage>();

            public AssetManager(List<Storage> storages)
            {
                SetStorages(storages);
            }

            public void SetStorages(List<Storage> newList)
            {
                storages.Clear();
                storages.AddRange(newList);
            }

            public byte[] FindAsset(string relativePath)
            {
                foreach (var storage in storages)
                {
                    if (!storage.Exists(relativePath)) continue;

                    string fullPath = storage.GetFullPath(relativePath);
                    return File.ReadAllBytes(fullPath);
                }

                return [];
            }
        }

        public partial class GosuServer : WsServer
        {
            public GosuServer(IPAddress address, int port)
                : base(address, port)
            {
            }

            public readonly AssetManager AssetManager = new AssetManager(new List<Storage>());

            private Storage staticsStorage;

            public void SetStorage(Storage statics, Storage caches)
            {
                this.staticsStorage = statics;

                AssetManager.SetStorages([statics, caches]);
            }

            public Storage? GetStorage()
            {
                return this.staticsStorage;
            }

            protected override TcpSession CreateSession() { return new GosuSession(this); }

            protected override void OnError(SocketError error)
            {
                Logging.Log($"Chat WebSocket server caught an error with code {error}");
            }

            public void AddCustomHandler(string path, string urlPath, FileCache.InsertHandler handler)
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(100);
                this.Cache.InsertPath(path, urlPath, "*.*", timeout, handler);
            }

            public byte[]? FindStaticOrAsset(string path)
            {
                return this.AssetManager.FindAsset(path);
            }

            public new void AddStaticContent(string path, string prefix = "/", string filter = "*.*", TimeSpan? timeout = null)
            {
                throw new Exception("Deprecated operation");
            }
        }
    }
}
