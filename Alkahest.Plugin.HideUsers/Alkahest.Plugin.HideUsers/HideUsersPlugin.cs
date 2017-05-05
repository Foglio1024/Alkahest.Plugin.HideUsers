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
using System.Timers;

namespace Alkahest.Plugins.HideUsers
{
    public class HideUsersPlugin : IPlugin
    {
        public string Name => "hide-users";
        bool visible = true;
        bool firstViewDistance = true;
        uint oldViewDistance = 1;
        Timer delay;
        EntityId currentPlayer;
        Dictionary<EntityId, SSpawnUserPacket> SpawnedUsers;
        Dictionary<EntityId, SAbnormalityBeginPacket> HeldAbnormals;
        static readonly Log _log = new Log(typeof(HideUsersPlugin));

        public HideUsersPlugin()
        {
            delay = new Timer(1000);
        }

        private bool HandleSpawnUser(GameClient client, Direction direction, SSpawnUserPacket packet)
        {
            if (!visible) return false;
            else return true;
        }
        private bool HandleChat(GameClient client, Direction direction, CChatPacket packet)
        {
            var m = packet.Message.Replace("<FONT>", "");
            m = m.Replace("</FONT>", "");

            if (m.StartsWith(".users"))
            {
                if (m.Substring(7).Equals("on") && !visible)
                {
                    delay.Elapsed += (s, ev) =>
                    {
                        client.SendToServer(new CSetVisibleRangePacket { Range = oldViewDistance });
                        delay.Stop();
                    };
                    visible = true;
                    client.SendToServer(new CSetVisibleRangePacket { Range = 0 });
                    delay.Start();

                    _log.Basic("Showing characters.");
                }
                else if (m.Substring(7).Equals("off") && visible)
                {
                    delay.Elapsed += (s, ev) =>
                    {
                        client.SendToServer(new CSetVisibleRangePacket { Range = oldViewDistance });
                        delay.Stop();
                    };
                    visible = false;
                    client.SendToServer(new CSetVisibleRangePacket { Range = 0 });
                    delay.Start();
                    _log.Basic("Hiding characters.");
                }
                return false;

            }
            else return true;
        }
        private bool HandleSetVisibleRange(GameClient client, Direction direction, CSetVisibleRangePacket packet)
        {
            if (firstViewDistance)
            {
                oldViewDistance = packet.Range;
                firstViewDistance = false;
            }
            return true;
        }

        public void Start(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                proc.AddHandler<SSpawnUserPacket>(HandleSpawnUser);
                proc.AddHandler<CSetVisibleRangePacket>(HandleSetVisibleRange);
                proc.AddHandler<CChatPacket>(HandleChat);
            }
            _log.Basic("Hide users plugin started");
        }
        public void Stop(GameProxy[] proxies)
        {
            foreach (var proc in proxies.Select(x => x.Processor))
            {
                proc.RemoveHandler<SSpawnUserPacket>(HandleSpawnUser);
                proc.RemoveHandler<CSetVisibleRangePacket>(HandleSetVisibleRange);
                proc.RemoveHandler<CChatPacket>(HandleChat);
            }

            _log.Basic("Hide users plugin stopped");

        }

    }
}
