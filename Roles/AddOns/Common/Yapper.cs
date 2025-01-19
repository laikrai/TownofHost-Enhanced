using System.Timers;
using System;
using static TOHE.Options;

namespace TOHE.Roles.AddOns.Common;

public class Yapper : IAddon
{
    public CustomRoles Role => CustomRoles.Yapper;
    private const int Id = 40000;
    public static Dictionary<byte, Timer> YapperTimers = new();
    public AddonTypes Type => AddonTypes.Harmful;

    public static OptionItem TimeBetweenTalking;

    public void SetupCustomOption()
    {
        SetupAdtRoleOptions(Id, CustomRoles.Yapper, canSetNum: true, tab: TabGroup.Addons);
        TimeBetweenTalking = FloatOptionItem.Create(Id + 13, "Yapper_TimeBetweenTalking", new(5f, 60f, 2.5f), 30f, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Yapper])
             .SetValueFormat(OptionFormat.Seconds);
    }
    public void Init()
    {
        YapperTimers.Clear();
    }
    public void Add(byte playerId, bool gameIsLoading = true)
    {
        YapperTimers[playerId] = new Timer(TimeBetweenTalking.GetFloat() * 1000);
        YapperTimers[playerId].Elapsed += (sender, e) => Kill(playerId);
    }
    public void Remove(byte playerId)
    {
        YapperTimers[playerId].Dispose();
        YapperTimers.Remove(playerId);
    }
    public void OnFixedUpdate(PlayerControl player)
    {
        Timer timer = YapperTimers[player.PlayerId];
        timer.Stop();
    }
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
        Timer timer = YapperTimers[playerId];
        timer.Stop();
        timer.Start();
    }
}
