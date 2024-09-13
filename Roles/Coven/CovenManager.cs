﻿using AmongUs.GameOptions;
using Hazel;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Double;
using TOHE.Roles.Neutral;
using InnerNet;
using static TOHE.Options;
using static TOHE.Translator;
using static TOHE.Utils;
using static UnityEngine.GraphicsBuffer;

namespace TOHE;
public abstract class CovenManager : RoleBase
{
    public static PlayerControl necroHolder;

    public enum VisOptionList
    {
        On,
        CovenPerRole
    }
    public enum VentOptionList
    {
        On,
        CovenPerRole
    }

    private static readonly Dictionary<CustomRoles, OptionItem> CovenImpVisOptions = [];
    private static readonly Dictionary<CustomRoles, OptionItem> CovenVentOptions = [];
    public static void RunSetUpImpVisOptions(int Id)
    {
            foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven()).ToArray())
            {
                SetUpImpVisOption(cov, Id, true, CovenImpVisMode);
                Id++;
            }
    }
    public static void RunSetUpVentOptions(int Id)
    {
            foreach (var cov in CustomRolesHelper.AllRoles.Where(x => x.IsCoven() && (x is not CustomRoles.Medusa or CustomRoles.PotionMaster /* or CustomRoles.Sacrifist */)).ToArray())
            {
                SetUpVentOption(cov, Id, true, CovenVentMode);
                Id++;
            }
    }
    private static void SetUpImpVisOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenImpVisOptions[role] = BooleanOptionItem.Create(Id, "%role%HasImpVis", defaultValue, TabGroup.CovenRoles, false).SetParent(CovenImpVisMode);
        CovenImpVisOptions[role].ReplacementDictionary = replacementDic;
    }
    private static void SetUpVentOption(CustomRoles role, int Id, bool defaultValue = true, OptionItem parent = null)
    {
        var roleName = GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", ColorString(GetRoleColor(role), roleName) } };
        CovenVentOptions[role] = BooleanOptionItem.Create(Id, "%role%CanVent", defaultValue, TabGroup.CovenRoles, false).SetParent(CovenVentMode);
        CovenVentOptions[role].ReplacementDictionary = replacementDic;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    => HasNecronomicon(seen) ? ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣") : string.Empty;
    public override string GetMarkOthers(PlayerControl seer, PlayerControl target, bool isForMeeting = false)
    {
        if (!(seer == target) && HasNecronomicon(target) && seer.IsPlayerCoven() && !HasNecronomicon(seer))
        {
            return ColorString(GetRoleColor(CustomRoles.CovenLeader), "♣");
        }
        return string.Empty;
    }
    private void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleSkill, SendOption.Reliable, -1);
        writer.WriteNetObject(_Player); 
        writer.Write(necroHolder.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public override void ReceiveRPC(MessageReader reader, PlayerControl NaN)
    {
        byte NecroId = reader.ReadByte();
        necroHolder = GetPlayerById(NecroId);
    }
    public override void ApplyGameOptions(IGameOptions opt, byte playerId)
    {
        if (!CovenHasImpVis.GetBool())
            opt.SetVision(false);
        else if (CovenImpVisMode.GetValue() == 0)
            opt.SetVision(true);
        else
        {
            CovenImpVisOptions.TryGetValue(GetPlayerById(playerId).GetCustomRole(), out var option);
            opt.SetVision(option.GetBool());
        }
    }
    public override bool CanUseImpostorVentButton(PlayerControl pc)
    {
        if (!CovenCanVent.GetBool())
            return false;
        else if (CovenVentMode.GetValue() == 0)
            return true;
        else
        {
            CovenVentOptions.TryGetValue(pc.GetCustomRole(), out var option);
            return option.GetBool();
        }
    }
    //public override bool KnowRoleTarget(PlayerControl seer, PlayerControl target) => target.IsPlayerCoven() && seer.IsPlayerCoven();
    //public override bool OthersKnowTargetRoleColor(PlayerControl seer, PlayerControl target) => KnowRoleTarget(seer, target);
    public static void GiveNecronomicon()
    {
        var pcList = Main.AllAlivePlayerControls.Where(pc => pc.IsPlayerCoven() && pc.IsAlive()).ToList();
        if (pcList.Any())
        {
            PlayerControl rp = pcList.RandomElement();
            necroHolder = rp;
            necroHolder.Notify(GetString("NecronomiconNotification"));
        }
    }
    public override void OnCoEndGame()
    {
        necroHolder = null;
    }
    public override void OnFixedUpdate(PlayerControl pc)
    {
        if (!necroHolder.IsAlive())
        {
            GiveNecronomicon();
        }
    }
    public static bool HasNecronomicon(PlayerControl pc) => necroHolder.Equals(pc);
}
