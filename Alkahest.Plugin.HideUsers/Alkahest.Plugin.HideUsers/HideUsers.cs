using Alkahest.Core;
using Alkahest.Core.Logging;
using Alkahest.Core.Net;
using Alkahest.Core.Net.Protocol;
using Alkahest.Core.Net.Protocol.Packets;
using Alkahest.Core.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Alkahest.Plugin.HideUsers
{
    public class HideUsers : IPlugin
    {
        public string Name => "HideUsersTest";
        bool visible = true;
        readonly Dictionary<string, RawPacketHandler> _rawHandlers;
        Dictionary<ulong, RawPacket> Packets;
        static readonly Log _log = new Log(typeof(HideUsers));

        public HideUsers()
        {
            _rawHandlers = new Dictionary<string, RawPacketHandler>
            {
                { "S_SPAWN_USER", HandleSpawnUser },
                //{"S_USER_LOCATION", HandleUserLocation },
                {"S_DESPAWN_USER", HandleUserDespawn }
            };
            Packets = new Dictionary<ulong, RawPacket>();
        }

        private bool HandleUserDespawn(GameClient client, Direction direction, RawPacket packet)
        {
            var sr = new BinaryReader(new MemoryStream(packet.Payload));
            var EntityId = sr.ReadUInt64();
            if (Packets.TryGetValue(EntityId, out RawPacket rp))
            {
                Packets.Remove(EntityId);
                _log.Info("[R] Spawned users: {0}", Packets.Count);
            }
            //_log.Basic("[DESPAWN] {0}.", packet.ToString());
            return true;
        }

        private bool HandleUserLocation(GameClient client, Direction direction, RawPacket packet)
        {

            var sr = new BinaryReader(new MemoryStream(packet.Payload));
            var EntityId = sr.ReadUInt64();
            var x = sr.ReadSingle();
            var y = sr.ReadSingle();
            var z = sr.ReadSingle();
            var w = sr.ReadInt16();
            sr.Dispose();
            sr.Close();
            if (Packets.TryGetValue(EntityId, out RawPacket rp))
            {
                var sw = new BinaryWriter(new MemoryStream(Packets[EntityId].Payload));
                sw.BaseStream.Position = 24 + 16;
                sw.Write(x);
                sw.Write(y);
                sw.Write(z);
                sw.Write(w);
                sw.Dispose();
                sw.Close();
            }

            return true;
        }

        private bool HandleSpawnUser(GameClient client, Direction direction, RawPacket packet)
        {

            var sr = new BinaryReader(new MemoryStream(packet.Payload));
            sr.ReadBytes(24);
            sr.ReadBytes(8);

            var EntityId = sr.ReadUInt64();
            var x = sr.ReadSingle();
            var y = sr.ReadSingle();
            var z = sr.ReadSingle();
            var w = sr.ReadUInt16();
            if (!Packets.ContainsKey(EntityId))
            {
                Packets.Add(EntityId, packet);
                _log.Info("[A] Spawned users: {0}", Packets.Count);
            }
            if (visible)
            {
                client.SendToClient(packet);
            }

            return false;
        }

        public void Start(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                foreach (var pair in _rawHandlers)
                    proc.AddRawHandler(pair.Key, pair.Value);

                proc.AddHandler<CChatPacket>(HandleChat);
                proc.AddHandler<SLoginPacket>(HandleLogin);
                proc.AddHandler<SSpawnMePacket>(HandleSpawnMe);
            }

            _log.Basic("Hide users test plugin started");
        }

        private bool HandleSpawnMe(GameClient client, Direction direction, SSpawnMePacket packet)
        {
            Packets.Clear();
            _log.Info("Packets cleared");
            return true;
        }

        private bool HandleLogin(GameClient client, Direction direction, SLoginPacket packet)
        {
            Packets.Clear();
            return true;
        }

        private bool HandleChat(GameClient client, Direction direction, CChatPacket packet)
        {
            var m = packet.Message.Replace("<FONT>", "");
            m = m.Replace("</FONT>", "");

            if (m.StartsWith(".users"))
            {
                if (m.Substring(7).Equals("on") && !visible)
                {
                    visible = true;
                    foreach (var pair in Packets.ToList())
                    {
                        client.SendToClient(pair.Value);
                    }
                    _log.Basic("Showing characters.");
                }
                else if (m.Substring(7).Equals("off") && visible)
                {
                    visible = false;
                    foreach (var pair in Packets.ToList())
                    {
                        var newPacket = new RawPacket("S_DESPAWN_USER");
                        var newPayload = pair.Value.Payload.Slice(32, 8).Concat(new List<byte>() { 1, 0, 0, 0 });
                        newPacket.Payload = newPayload.ToArray();
                        client.SendToClient(newPacket);
                    }
                    _log.Basic("Hiding characters.");
                }
                return false;

            }
            else return true;
        }



        public void Stop(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                foreach (var pair in _rawHandlers)
                    proc.RemoveRawHandler(pair.Key, pair.Value);

                proc.RemoveHandler<CChatPacket>(HandleChat);
            }

            _log.Basic("Hide users test plugin stopped");

        }

    }
}
