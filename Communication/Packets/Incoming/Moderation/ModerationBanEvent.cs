﻿using Plus.Database;
using Plus.HabboHotel.GameClients;
using Plus.HabboHotel.Moderation;

namespace Plus.Communication.Packets.Incoming.Moderation;

internal class ModerationBanEvent : IPacketEvent
{
    private readonly IGameClientManager _clientManager;
    private readonly IModerationManager _moderationManager;
    private readonly IDatabase _database;

    public ModerationBanEvent(IGameClientManager clientManager, IModerationManager moderationManager, IDatabase database)
    {
        _clientManager = clientManager;
        _moderationManager = moderationManager;
        _database = database;
    }

    public void Parse(GameClient session, ClientPacket packet)
    {
        if (!session.GetHabbo().GetPermissions().HasRight("mod_soft_ban"))
            return;
        var userId = packet.PopInt();
        var message = packet.PopString();
        var length = packet.PopInt() * 3600 + PlusEnvironment.GetUnixTimestamp();
        packet.PopString(); //unk1
        packet.PopString(); //unk2
        var ipBan = packet.PopBoolean();
        var machineBan = packet.PopBoolean();
        if (machineBan)
            ipBan = false;
        var habbo = PlusEnvironment.GetHabboById(userId);
        if (habbo == null)
        {
            session.SendWhisper("An error occoured whilst finding that user in the database.");
            return;
        }
        if (habbo.GetPermissions().HasRight("mod_tool") && !session.GetHabbo().GetPermissions().HasRight("mod_ban_any"))
        {
            session.SendWhisper("Oops, you cannot ban that user.");
            return;
        }
        message = message ?? "No reason specified.";
        using (var dbClient = _database.GetQueryReactor())
        {
            dbClient.RunQuery("UPDATE `user_info` SET `bans` = `bans` + '1' WHERE `user_id` = '" + habbo.Id + "' LIMIT 1");
        }
        if (ipBan == false && machineBan == false)
            _moderationManager.BanUser(session.GetHabbo().Username, ModerationBanType.Username, habbo.Username, message, length);
        else if (ipBan)
            _moderationManager.BanUser(session.GetHabbo().Username, ModerationBanType.Ip, habbo.Username, message, length);
        else
        {
            _moderationManager.BanUser(session.GetHabbo().Username, ModerationBanType.Ip, habbo.Username, message, length);
            _moderationManager.BanUser(session.GetHabbo().Username, ModerationBanType.Username, habbo.Username, message, length);
            _moderationManager.BanUser(session.GetHabbo().Username, ModerationBanType.Machine, habbo.Username, message, length);
        }
        var targetClient = _clientManager.GetClientByUsername(habbo.Username);
        if (targetClient != null)
            targetClient.Disconnect();
    }
}