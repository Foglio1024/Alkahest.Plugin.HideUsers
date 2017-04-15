using Alkahest.Core;
using Alkahest.Core.Game;
using Alkahest.Core.Logging;
using Alkahest.Core.Net;
using Alkahest.Core.Net.Protocol;
using Alkahest.Core.Net.Protocol.Packets;
using Alkahest.Core.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace HideUsers
{
    public class HideUsersPlugin : IPlugin
    {
        public string Name => "hide-users";
        bool visible = true;
        EntityId currentPlayer;
        Dictionary<EntityId, SSpawnUserPacket> SpawnedUsers;
        Dictionary<EntityId, SAbnormalityBeginPacket> HeldAbnormals;
        static readonly Log _log = new Log(typeof(HideUsersPlugin));

        public HideUsersPlugin()
        {
            SpawnedUsers = new Dictionary<EntityId, SSpawnUserPacket>();
            HeldAbnormals = new Dictionary<EntityId, SAbnormalityBeginPacket>();
        }

        private bool HandleSpawnUser(GameClient client, Direction direction, SSpawnUserPacket packet)
        {

            if (!SpawnedUsers.ContainsKey(packet.Target))
            {
                SpawnedUsers.Add(packet.Target, packet);
                _log.Info("[ADDED {1}] Nearby users: {0}", SpawnedUsers.Count, packet.UserName);
            }
            if (visible)
            {
                client.SendToClient(packet);
            }

            return false;
        }
        private bool HandleUserDespawn(GameClient client, Direction direction, SDespawnUserPacket packet)
        {
            if (SpawnedUsers.TryGetValue(packet.Target, out SSpawnUserPacket rp))
            {
                SpawnedUsers.Remove(packet.Target);
                _log.Info("[REMOVED {1}] Spawned users: {0}", SpawnedUsers.Count, rp.UserName);
            }
            //_log.Basic("[DESPAWN] {0}.", packet.ToString());
            return true;
        }
        private bool HandleUserLocation(GameClient client, Direction direction, SUserLocationPacket packet)
        {
            if (SpawnedUsers.TryGetValue(packet.Source, out SSpawnUserPacket rp))
            {
                rp.Position = packet.Position;
                //_log.Basic("[MOVED {0}]", rp.UserName);
            }

            return true;

        }     
        
        private bool HandleAbnormBegin(GameClient client, Direction direction, SAbnormalityBeginPacket packet)
        {
            return true;
        }
        private bool HandleAbnormEnd(GameClient client, Direction direction, SAbnormalityEndPacket packet)
        {
            return true;
        }

        private bool HandleSpawnMe(GameClient client, Direction direction, SSpawnMePacket packet)
        {
            SpawnedUsers.Clear();
            currentPlayer = packet.Target;
            _log.Info("Packets cleared");
            return true;
        }
        private bool HandleLogin(GameClient client, Direction direction, SLoginPacket packet)
        {
            SpawnedUsers.Clear();
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
                    foreach (var pair in SpawnedUsers.ToList())
                    {
                        client.SendToClient(pair.Value);
                    }
                    _log.Basic("Showing characters.");
                }
                else if (m.Substring(7).Equals("off") && visible)
                {
                    visible = false;
                    foreach (var pair in SpawnedUsers.ToList())
                    {
                        var newPacket = new SDespawnUserPacket()
                        {
                            Target = pair.Value.Target,
                            Kind = DespawnKind.OutOfView,
                           
                        };

                        client.SendToClient(newPacket);
                    }
                    _log.Basic("Hiding characters.");
                    
                }
                return false;

            }
            else return true;
        }

        public void Start(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                proc.AddHandler<CChatPacket>(HandleChat);
                proc.AddHandler<SLoginPacket>(HandleLogin);
                proc.AddHandler<SSpawnMePacket>(HandleSpawnMe);
                proc.AddHandler<SSpawnUserPacket>(HandleSpawnUser);
                proc.AddHandler<SUserLocationPacket>(HandleUserLocation);
                proc.AddHandler<SDespawnUserPacket>(HandleUserDespawn);
                proc.AddHandler<SAbnormalityBeginPacket>(HandleAbnormBegin);
                proc.AddHandler<SAbnormalityEndPacket>(HandleAbnormEnd);
            }

            _log.Basic("Hide users test plugin started");
        }
        public void Stop(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                proc.RemoveHandler<CChatPacket>(HandleChat);
                proc.RemoveHandler<SLoginPacket>(HandleLogin);
                proc.RemoveHandler<SSpawnMePacket>(HandleSpawnMe);
                proc.RemoveHandler<SSpawnUserPacket>(HandleSpawnUser);
                proc.RemoveHandler<SUserLocationPacket>(HandleUserLocation);
                proc.RemoveHandler<SDespawnUserPacket>(HandleUserDespawn);
                proc.RemoveHandler<SAbnormalityBeginPacket>(HandleAbnormBegin);
                proc.RemoveHandler<SAbnormalityEndPacket>(HandleAbnormEnd);

            }

            _log.Basic("Hide users test plugin stopped");

        }

    }
}
