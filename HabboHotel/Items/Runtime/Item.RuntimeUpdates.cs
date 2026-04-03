using System.Drawing;
using System.Linq;
using Plus.Communication.Packets.Outgoing.Rooms.Notifications;
using Plus.HabboHotel.Rooms;
using Plus.HabboHotel.Users;

namespace Plus.HabboHotel.Items;

public partial class Item
{
    private void ProcessHopperUpdate()
    {
        RoomUser? user = null;
        RoomUser? user2 = null;
        var showHopperEffect = false;
        var keepDoorOpen = false;
        var pause = 0;

        if (InteractingUser > 0)
        {
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser, out user);

            if (user != null)
            {
                if (user.Coordinate == Coordinate)
                {
                    user.AllowOverride = false;
                    if (user.TeleDelay == 0)
                    {
                        var roomHopId = GetRoom().GetItemHopperFinder().GetAHopper(user.RoomId);
                        var nextHopperId = GetRoom().GetItemHopperFinder().GetHopperId(roomHopId);
                        var habbo = GetHabbo(user);
                        if (habbo != null)
                        {
                            habbo.IsHopping = true;
                            habbo.HopperId = nextHopperId;
                            _ = GetRoom().GetRoomService().PrepareRoom(user.GetClient()!, roomHopId, "");
                            InteractingUser = 0;
                        }
                    }
                    else
                    {
                        user.TeleDelay--;
                        showHopperEffect = true;
                    }
                }
                else if (user.Coordinate == SquareInFront)
                {
                    user.AllowOverride = true;
                    keepDoorOpen = true;

                    if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                        user.ClearMovement(true);

                    user.CanWalk = false;
                    user.AllowOverride = true;
                    user.MoveTo(Coordinate.X, Coordinate.Y, true);
                }
                else
                {
                    InteractingUser = 0;
                }
            }
            else
            {
                InteractingUser = 0;
            }
        }

        if (InteractingUser2 > 0)
        {
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser2, out user2);
            if (user2 != null)
            {
                keepDoorOpen = true;
                user2.UnlockWalking();
                user2.MoveTo(SquareInFront);
            }

            InteractingUser2 = 0;
        }

        if (keepDoorOpen)
        {
            if (LegacyDataString != "1")
            {
                LegacyDataString = "1";
                UpdateState(false, true);
            }
        }
        else if (showHopperEffect)
        {
            if (LegacyDataString != "2")
            {
                LegacyDataString = "2";
                UpdateState(false, true);
            }
        }
        else if (LegacyDataString != "0")
        {
            if (pause == 0)
            {
                LegacyDataString = "0";
                UpdateState(false, true);
                pause = 2;
            }
            else
            {
                pause--;
            }
        }

        RequestUpdate(1, false);
    }

    private void ProcessBottleUpdate()
    {
        LegacyDataString = Random.Shared.Next(0, 8).ToString();
        UpdateState();
    }

    private void ProcessDiceUpdate()
    {
        if (LegacyDataString == "-1")
            LegacyDataString = RandomizeStrings(new[] { "1", "2", "3", "4", "5", "6" })[0];

        UpdateState();
    }

    private void ProcessHabboWheelUpdate()
    {
        LegacyDataString = Random.Shared.Next(1, 10).ToString();
        UpdateState();
    }

    private void ProcessLoveShufflerUpdate()
    {
        if (LegacyDataString == "0")
        {
            LegacyDataString = Random.Shared.Next(1, 5).ToString();
            RequestUpdate(20, false);
        }
        else if (LegacyDataString != "-1")
        {
            LegacyDataString = "-1";
        }

        UpdateState(false, true);
    }

    private void ProcessAlertUpdate()
    {
        if (LegacyDataString != "1")
            return;

        LegacyDataString = "0";
        UpdateState(false, true);
    }

    private void ProcessVendingMachineUpdate()
    {
        if (LegacyDataString != "1")
            return;

        GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser, out var user);
        if (user == null)
            return;

        user.UnlockWalking();
        if (Definition.VendingIds.Count > 0)
        {
            var randomDrink = Definition.VendingIds[Random.Shared.Next(0, Definition.VendingIds.Count)];
            user.CarryItem(randomDrink);
        }

        InteractingUser = 0;
        LegacyDataString = "0";
        UpdateState(false, true);
    }

    private void ProcessOneWayGateUpdate()
    {
        RoomUser? user = null;
        if (InteractingUser > 0)
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser, out user);

        if (user != null && user.X == GetX && user.Y == GetY)
        {
            LegacyDataString = "1";
            user.MoveTo(SquareBehind);
            user.InteractingGate = false;
            user.GateId = 0;
            RequestUpdate(1, false);
            UpdateState(false, true);
        }
        else if (user != null && user.Coordinate == SquareBehind)
        {
            user.UnlockWalking();
            LegacyDataString = "0";
            InteractingUser = 0;
            user.InteractingGate = false;
            user.GateId = 0;
            UpdateState(false, true);
        }
        else
        {
            if (LegacyDataString == "1")
            {
                LegacyDataString = "0";
                UpdateState(false, true);
            }

            if (user == null)
                InteractingUser = 0;
        }
    }

    private void ProcessVipGateUpdate()
    {
        RoomUser? user = null;
        if (InteractingUser > 0)
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser, out user);

        var deltaY = 0;
        var deltaX = 0;

        if (user != null && user.X == GetX && user.Y == GetY)
        {
            if (user.RotBody == 4)
                deltaY = 1;
            else if (user.RotBody == 0)
                deltaY = -1;
            else if (user.RotBody == 6)
                deltaX = -1;
            else if (user.RotBody == 2)
                deltaX = 1;

            user.MoveTo(user.X + deltaX, user.Y + deltaY);
            RequestUpdate(1, false);
        }
        else if (user != null && (user.Coordinate == SquareBehind || user.Coordinate == SquareInFront))
        {
            user.UnlockWalking();
            LegacyDataString = "0";
            InteractingUser = 0;
            UpdateState(false, true);
        }
        else
        {
            if (LegacyDataString == "1")
            {
                LegacyDataString = "0";
                UpdateState(false, true);
            }

            if (user == null)
                InteractingUser = 0;
        }
    }

    private void ProcessTeleportUpdate()
    {
        RoomUser? user = null;
        RoomUser? user2 = null;
        var keepDoorOpen = false;
        var showTeleEffect = false;

        if (InteractingUser > 0)
        {
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser, out user);

            if (user != null)
            {
                if (user.Coordinate == Coordinate)
                {
                    user.AllowOverride = false;
                    if (GetRoom().GetItemTeleporterFinder().IsTeleLinked(Id, GetRoom()))
                    {
                        showTeleEffect = true;
                        var teleId = GetRoom().GetItemTeleporterFinder().GetLinkedTele(Id);
                        var roomId = GetRoom().GetItemTeleporterFinder().GetTeleRoomId(teleId, GetRoom());

                        if (roomId == RoomId)
                        {
                            var item = GetRoom().GetRoomItemHandler().GetItem(teleId);
                            if (item == null)
                            {
                                user.UnlockWalking();
                            }
                            else
                            {
                                user.SetPos(item.GetX, item.GetY, item.GetZ);
                                user.SetRot(item.Rotation, false);
                                item.LegacyDataString = "2";
                                item.UpdateState(false, true);
                                item.InteractingUser2 = InteractingUser;
                                GetRoom().GetGameMap().RemoveUserFromMap(user, new(GetX, GetY));
                                InteractingUser = 0;
                            }
                        }
                        else if (user.TeleDelay == 0)
                        {
                            var habbo = GetHabbo(user);
                            if (habbo != null)
                            {
                                habbo.IsTeleporting = true;
                                habbo.TeleportingRoomId = roomId;
                                habbo.TeleporterId = teleId;
                                _ = GetRoom().GetRoomService().PrepareRoom(user.GetClient()!, roomId, "");
                                InteractingUser = 0;
                            }
                        }
                        else
                        {
                            user.TeleDelay--;
                            showTeleEffect = true;
                        }

                        GetRoom().GetGameMap().GenerateMaps();
                    }
                    else
                    {
                        user.UnlockWalking();
                        InteractingUser = 0;
                    }
                }
                else if (user.Coordinate == SquareInFront)
                {
                    user.AllowOverride = true;
                    keepDoorOpen = true;

                    if (user.IsWalking && (user.GoalX != GetX || user.GoalY != GetY))
                        user.ClearMovement(true);

                    user.CanWalk = false;
                    user.AllowOverride = true;
                    user.MoveTo(Coordinate.X, Coordinate.Y, true);
                }
                else
                {
                    InteractingUser = 0;
                }
            }
            else
            {
                InteractingUser = 0;
            }
        }

        if (InteractingUser2 > 0)
        {
            GetRoom().GetRoomUserManager().TryGetRoomUserByHabbo(InteractingUser2, out user2);
            if (user2 != null)
            {
                keepDoorOpen = true;
                user2.UnlockWalking();
                user2.MoveTo(SquareInFront);
            }

            InteractingUser2 = 0;
        }

        if (showTeleEffect)
        {
            if (LegacyDataString != "2")
            {
                LegacyDataString = "2";
                UpdateState(false, true);
            }
        }
        else if (keepDoorOpen)
        {
            if (LegacyDataString != "1")
            {
                LegacyDataString = "1";
                UpdateState(false, true);
            }
        }
        else if (LegacyDataString != "0")
        {
            LegacyDataString = "0";
            UpdateState(false, true);
        }

        RequestUpdate(1, false);
    }

    private int ParseLegacySeconds()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return 0;

        try
        {
            return int.Parse(LegacyDataString);
        }
        catch
        {
            return 0;
        }
    }

    private void ProcessScoreboardUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
        if (seconds > 0)
        {
            if (InteractionCountHelper == 1)
            {
                seconds--;
                InteractionCountHelper = 0;
                LegacyDataString = seconds.ToString();
                UpdateState();
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateCounter = 0;
        }
    }

    private void ProcessBanzaiCounterUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
        if (seconds > 0)
        {
            if (InteractionCountHelper == 1)
            {
                seconds--;
                InteractionCountHelper = 0;
                if (GetRoom().GetBanzai().IsBanzaiActive)
                {
                    LegacyDataString = seconds.ToString();
                    UpdateState();
                }
                else
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
            return;
        }

        UpdateCounter = 0;
        GetRoom().GetBanzai().BanzaiEnd();
    }

    private void ProcessBanzaiFloorUpdate()
    {
        if (Value != 3)
            return;

        if (InteractionCountHelper == 1)
        {
            InteractionCountHelper = 0;
            LegacyDataString = Definition.GetBanzaiFloorPulseState(Team);
        }
        else
        {
            LegacyDataString = "";
            InteractionCountHelper++;
        }

        UpdateState();
        InteractionCount++;
        UpdateCounter = InteractionCount < 16 ? 1 : 0;
    }

    private void ProcessBanzaiPuckUpdate()
    {
        if (InteractionCount > 4)
        {
            InteractionCount++;
            UpdateCounter = 1;
            return;
        }

        InteractionCount = 0;
        UpdateCounter = 0;
    }

    private void ProcessFreezeTileUpdate()
    {
        if (InteractingUser <= 0)
            return;

        LegacyDataString = "11000";
        UpdateState(false, true);
        GetRoom().GetFreeze().OnFreezeTiles(this, FreezePowerUp);
        InteractingUser = 0;
        InteractionCountHelper = 0;
    }

    private void ProcessCounterUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
        if (seconds > 0)
        {
            if (InteractionCountHelper == 1)
            {
                seconds--;
                InteractionCountHelper = 0;
                if (GetRoom().GetSoccer().GameIsStarted)
                {
                    LegacyDataString = seconds.ToString();
                    UpdateState();
                }
                else
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateNeeded = false;
            GetRoom().GetSoccer().StopGame();
        }
    }

    private void ProcessFreezeTimerUpdate()
    {
        if (string.IsNullOrEmpty(LegacyDataString))
            return;

        var seconds = ParseLegacySeconds();
        if (seconds > 0)
        {
            if (InteractionCountHelper == 1)
            {
                seconds--;
                InteractionCountHelper = 0;
                if (GetRoom().GetFreeze().GameIsStarted)
                {
                    LegacyDataString = seconds.ToString();
                    UpdateState();
                }
                else
                {
                    return;
                }
            }
            else
            {
                InteractionCountHelper++;
            }

            UpdateCounter = 1;
        }
        else
        {
            UpdateNeeded = false;
            GetRoom().GetFreeze().StopGame();
        }
    }

    private void ProcessPressurePadUpdate()
    {
        LegacyDataString = "1";
        UpdateState();
    }

    private void ProcessWiredResetUpdate()
    {
        if (LegacyDataString != "1")
            return;

        LegacyDataString = "0";
        UpdateState(false, true);
    }

    private void ProcessCannonUpdate()
    {
        if (LegacyDataString != "1")
            return;

        var room = GetRoom();
        foreach (var square in GetCannonTargetSquares())
        {
            var affectedUsers = room.GetGameMap().GetRoomUsers(square).ToList();
            if (affectedUsers.Count == 0)
                continue;

            foreach (var target in affectedUsers)
            {
                if (target == null || target.IsBot || target.IsPet)
                    continue;

                var targetClient = target.GetClient();
                var targetHabbo = GetHabbo(target);
                if (targetHabbo == null || targetClient == null)
                    continue;

                if (room.CheckRights(targetClient, true))
                    continue;

                target.ApplyEffect(4);
                targetClient.Send(new RoomNotificationComposer("Kicked from room", "You were hit by a cannonball!", "room_kick_cannonball", ""));
                target.ApplyEffect(0);
                _ = room.GetRoomService().LeaveRoom(targetClient);
            }
        }

        LegacyDataString = "2";
        UpdateState(false, true);
    }

    private List<Point> GetCannonTargetSquares()
    {
        var targetStart = Coordinate;
        var targetSquares = new List<Point>();

        switch (Rotation)
        {
            case 0:
                targetStart = new(GetX - 1, GetY);
                break;
            case 2:
                targetStart = new(GetX, GetY - 1);
                break;
            case 4:
                targetStart = new(GetX + 2, GetY);
                break;
            case 6:
                targetStart = new(GetX, GetY + 2);
                break;
        }

        if (!targetSquares.Contains(targetStart))
            targetSquares.Add(targetStart);

        for (var offset = 1; offset <= 3; offset++)
        {
            Point targetSquare = Rotation switch
            {
                0 => new(targetStart.X - offset, targetStart.Y),
                2 => new(targetStart.X, targetStart.Y - offset),
                4 => new(targetStart.X + offset, targetStart.Y),
                6 => new(targetStart.X, targetStart.Y + offset),
                _ => targetStart
            };

            if (!targetSquares.Contains(targetSquare))
                targetSquares.Add(targetSquare);
        }

        return targetSquares;
    }
}
