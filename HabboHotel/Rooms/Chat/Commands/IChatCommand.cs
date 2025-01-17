﻿using Plus.HabboHotel.GameClients;
using Plus.Utilities.DependencyInjection;

namespace Plus.HabboHotel.Rooms.Chat.Commands;

[Transient]
public interface IChatCommand
{
    string Key { get; }
    string PermissionRequired { get; }
    string Parameters { get; }
    string Description { get; }
    void Execute(GameClient session, Room room, string[] @params);
}