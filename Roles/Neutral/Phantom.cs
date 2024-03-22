﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TOHE.Options;

namespace TOHE.Roles.Neutral
{
    internal class Phantom : RoleBase
    {

        //===========================SETUP================================\\
        private const int Id = 14900;
        private static HashSet<byte> PlayerIds = [];
        public static bool HasEnabled => PlayerIds.Any();
        public override bool IsEnable => HasEnabled;
        public override CustomRoles ThisRoleBase => PhantomCanVent.GetBool() ? CustomRoles.Engineer : CustomRoles.Crewmate;
        //==================================================================\\

        public static OptionItem PhantomCanVent;
        public static OptionItem PhantomSnatchesWin;
        public static OptionItem PhantomCanGuess;
        public static void SetupCustomOptions()
        {
            SetupRoleOptions(14900, TabGroup.NeutralRoles, CustomRoles.Phantom);
            PhantomCanVent = BooleanOptionItem.Create(14902, "CanVent", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
            PhantomSnatchesWin = BooleanOptionItem.Create(14903, "SnatchesWin", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
            PhantomCanGuess = BooleanOptionItem.Create(14904, "CanGuess", false, TabGroup.NeutralRoles, false)
                .SetParent(CustomRoleSpawnChances[CustomRoles.Phantom]);
            OverrideTasksData.Create(14905, TabGroup.NeutralRoles, CustomRoles.Phantom);
        }
        public override void Init()
        {
            PlayerIds.Clear();
        }
        public override void Add(byte playerId)
        {
            PlayerIds.Add(playerId);
        }

    }
}
