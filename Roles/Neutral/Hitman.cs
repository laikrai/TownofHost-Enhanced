using AmongUs.GameOptions;
using Il2CppSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TOHE.Roles.Neutral
{
    internal class Hitman : RoleBase
    {
        //===========================SETUP================================\\
        public override CustomRoles Role => CustomRoles.Hitman;
        private const int Id = 31200;
        public static byte? PlayerId;
        public static bool HasEnabled => PlayerId.HasValue;
        public override CustomRoles ThisRoleBase => CustomRoles.Impostor;
        public override Custom_RoleType ThisRoleType => Custom_RoleType.NeutralBenign;
        //==================================================================\\

        private static OptionItem HitmanKillCooldown;
        private static OptionItem HitmanCanVent;
        private static OptionItem HitmanHasImpostorVision;
        private static OptionItem HitmanArrowTowardsTarget;
        public static OptionItem HitmanKillsNeeded;
        private static OptionItem HitmanCanGetATargetAfterKillsNeeded;
        private static OptionItem TryHideMsg;

        public static byte? ClientPlayerId;
        public static byte? TargetPlayerId;

        public override void SetupCustomOption()
        {
            Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Hitman);
            HitmanKillCooldown = FloatOptionItem.Create(Id + 3, "HitmanKillCooldown", new(0f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman])
                .SetValueFormat(OptionFormat.Seconds);
            HitmanCanVent = BooleanOptionItem.Create(Id + 4, "HitmanCanVent", false, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman]);
            HitmanHasImpostorVision = BooleanOptionItem.Create(Id + 5, "HitmanHasImpostorVision", true, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman]);
            HitmanArrowTowardsTarget = BooleanOptionItem.Create(Id + 6, "HitmanArrowTowardsTarget", true, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman]);
            HitmanKillsNeeded = IntegerOptionItem.Create(Id + 7, "HitmanKillsNeeded", new(1, 10, 1), 3, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman]);
            HitmanCanGetATargetAfterKillsNeeded = BooleanOptionItem.Create(Id + 8, "HitmanCanGetATargetAfterKillsNeeded", true, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman]);
            TryHideMsg = BooleanOptionItem.Create(Id + 9, "HitmanTryHideMsg", true, TabGroup.NeutralRoles, false)
                .SetParent(Options.CustomRoleSpawnChances[CustomRoles.Hitman])
                .SetColor(Color.green);

        }
        public override void Init()
        {
            PlayerId = null;
            ClientPlayerId = null;
            TargetPlayerId = null;
        }
        public override void Add(byte playerId)
        {
            PlayerId = playerId;
            AbilityLimit = 0;
            Main.AllPlayerKillCooldown[playerId] = 300f;
        }
        public override bool CanUseKillButton(PlayerControl pc) => AbilityLimit >= HitmanKillsNeeded.GetInt() ? HitmanCanGetATargetAfterKillsNeeded.GetBool() ? true : false : true;
        public override void ApplyGameOptions(IGameOptions opt, byte playerId) => opt.SetVision(HitmanHasImpostorVision.GetBool());
        public override void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = HitmanKillCooldown.GetFloat();
        public override bool CanUseImpostorVentButton(PlayerControl pc) => HitmanCanVent.GetBool();
        private static byte? SelectClient() => ClientPlayerId = Main.AllAlivePlayerControls.OrderBy(_ => System.Guid.NewGuid()).First().PlayerId;
        public override void OnMeetingHudStart(PlayerControl pc)
        {
            ClientPlayerId = SelectClient();
            while (ClientPlayerId == PlayerId || Utils.GetPlayerById(ClientPlayerId.Value).Data.IsDead)
                ClientPlayerId = SelectClient();
            TargetPlayerId = null;
            MeetingHudStartPatch.AddMsg(Translator.GetString("HitmanClientSet"),ClientPlayerId.Value,Translator.GetString("HitmanMsgTitle"));
        }
        public override void AfterMeetingTasks()
        {
            if (TargetPlayerId == null)
            {
                var client = Utils.GetPlayerById(ClientPlayerId.Value);
                if (client.IsAlive())
                {
                    TargetPlayerId = client.PlayerId;
                }
                else
                {
                    TargetPlayerId = SelectClient();
                }
            }
            else if (TargetPlayerId == PlayerId)
                TargetPlayerId = ClientPlayerId;
            var target = Utils.GetPlayerById(TargetPlayerId.Value);
            if (!target.IsAlive())
                    TargetPlayerId = SelectClient();
        }
        public override void NotifyAfterMeeting()
        {
            TargetArrow.RemoveAllTarget(PlayerId.Value);
            _Player.Notify(string.Format(Translator.GetString("HitmanNotify"), Utils.GetPlayerById(TargetPlayerId.Value).name.RemoveHtmlTags()));
            if (HitmanArrowTowardsTarget.GetBool())
                TargetArrow.Add(PlayerId.Value, TargetPlayerId.Value);
        }

        public override string GetProgressText(byte playerId, bool comms) => Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hitman).ShadeColor(0.25f), $"({AbilityLimit}/{HitmanKillsNeeded.CurrentValue})");
        public override bool OnCheckMurderAsKiller(PlayerControl killer, PlayerControl target)
        {
            if (target.PlayerId != TargetPlayerId)
            {
                killer.RpcGuardAndKill(target);
                killer.Notify(Translator.GetString("HitmanWrongTarget"));
                killer.ResetKillCooldown();
                return false;
            }
            AbilityLimit++;
            SendSkillRPC();
            GetProgressText(PlayerId.Value, false);
            TargetArrow.RemoveAllTarget(PlayerId.Value);
            if (AbilityLimit >= HitmanKillsNeeded.CurrentValue)
            {
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Hitman);
                CustomWinnerHolder.WinnerIds.Add(PlayerId.Value);
            }
            Main.AllPlayerKillCooldown[PlayerId.Value] = 300f;
            return true;
        }
        public static bool CheckCommand(PlayerControl player, string msg, bool isUI = false)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            if (!GameStates.IsMeeting || player == null || GameStates.IsExilling) return false;
            if (player.PlayerId != ClientPlayerId) return false;
            if (TargetPlayerId != null) return false;

            msg = msg.ToLower().Trim();

            var commands = new[] { "tg", "тг", "目标", "target" };
            foreach (var cmd in commands)
            {
                if (msg.StartsWith("/" + cmd))
                {
                    var targetIdStr = msg.Split(' ').Skip(1).FirstOrDefault();
                    if (byte.TryParse(targetIdStr, out byte targetId) && Utils.GetPlayerById(targetId) != null && !Utils.GetPlayerById(targetId).Data.IsDead)
                    {
                        TargetPlayerId = targetId;
                        if (TryHideMsg.GetBool())
                            GuessManager.TryHideMsg();
                        player.ShowInfoMessage(isUI, string.Format(Translator.GetString("HitmanTargetSet"), Utils.GetPlayerById(targetId).name.RemoveHtmlTags()), Translator.GetString("HitmanMsgTitle"));
                        TargetPlayerId = targetId;
                        return true;
                    }
                    else
                    {
                        if (TryHideMsg.GetBool())
                            GuessManager.TryHideMsg();
                        player.ShowInfoMessage(isUI, Translator.GetString("HitmanTargetNotSet"));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
