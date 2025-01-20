using System.Timers;
using System;
using static TOHE.Options;
using UnityEngine;

namespace TOHE.Roles.AddOns.Common;

public class Yapper : MonoBehaviour, IAddon
{
    public CustomRoles Role => CustomRoles.Yapper;
    private const int Id = 40000;
    public static Dictionary<byte, float> timesToTalk = new();
    public AddonTypes Type => AddonTypes.Harmful;

    public static OptionItem TimeBetweenTalking;
    public static OptionItem ShouldNotifyEveryXSeconds;
    public static OptionItem NotifyEveryXSeconds;

    private static Dictionary<byte, float> lastNotificationTime = new();

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Yapper, canSetNum: true, tab: TabGroup.Addons);
        TimeBetweenTalking = FloatOptionItem.Create(Id + 13, "Yapper_TimeBetweenTalking", new(5f, 60f, 2.5f), 30f, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Yapper])
            .SetValueFormat(OptionFormat.Seconds);
        ShouldNotifyEveryXSeconds = BooleanOptionItem.Create(Id + 14, "Yapper_ShouldNotifyEveryXSeconds", true, TabGroup.Addons, false)
            .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Yapper]);
        NotifyEveryXSeconds = FloatOptionItem.Create(Id + 15, "Yapper_NotifyEveryXSeconds", new(5f, 60f, 5f), 10f, TabGroup.Addons, false)
            .SetParent(ShouldNotifyEveryXSeconds)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public void Init() => timesToTalk.Clear();

    public void Add(byte playerId, bool gameIsLoading = true)
    {
        timesToTalk.Add(playerId, TimeBetweenTalking.GetFloat());
        lastNotificationTime[playerId] = TimeBetweenTalking.GetFloat();
    }

    public void Remove(byte playerId)
    {
        timesToTalk.Remove(playerId);
        lastNotificationTime.Remove(playerId);
    }

    public void OnFixedUpdate(PlayerControl player) => timesToTalk[player.PlayerId] = 0f; // only happens outside of meetings

    public static void Kill(byte playerId)
    {
        PlayerControl player = Utils.GetPlayerById(playerId);
        player.SetDeathReason(PlayerState.DeathReason.Suicide);
        player.SetRealKiller(player);
        GuessManager.RpcGuesserMurderPlayer(player);
        Main.PlayersDiedInMeeting.Add(playerId);
        MurderPlayerPatch.AfterPlayerDeathTasks(player, PlayerControl.LocalPlayer, true);
        Utils.SendMessage(string.Format(Translator.GetString("Yapper_Died"), player.name));
    }

    public static void ResetTimer(byte playerId)
    {
        timesToTalk[playerId] = TimeBetweenTalking.GetFloat();
        lastNotificationTime[playerId] = TimeBetweenTalking.GetFloat();
    }

    public static void UpdateTimer(byte playerId)
    {
        timesToTalk[playerId] -= Time.deltaTime;

        if (ShouldNotifyEveryXSeconds.GetBool() && timesToTalk[playerId] < lastNotificationTime[playerId] - NotifyEveryXSeconds.GetFloat())
        {
            lastNotificationTime[playerId] = timesToTalk[playerId];
            NotifyPlayer(playerId);
        }

        if (timesToTalk[playerId] < 0)
            Kill(playerId);
    }

    private static void NotifyPlayer(byte playerId)
    {
        PlayerControl player = Utils.GetPlayerById(playerId);
        Utils.SendMessage(string.Format(Translator.GetString("Yapper_Notify"), timesToTalk[playerId].ToString("F1")), playerId, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Yapper), Translator.GetString("Yapper")));
    }
}
