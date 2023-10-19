using BattleBitAPI.Common;
using BattleBitAPI.Features;
using BBRAPIModules;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace BBRModules {
    [Module("A module that disallows spawning with certain weapons/gadget, also sending all items they cannot use when they try.", "1.0.0")]
    [RequireModule(typeof(PlaceholderLib))]
    public class LoadoutLimits : BattleBitModule {
        public static LoadoutLimitsConfig Configuration { get; set; } = null!;
        public LoadoutLimitsConfig ServerConfiguration { get; set; } = null!;

        public override void OnModulesLoaded() {
            PopulateGadgets();

            foreach (KeyValuePair<string, bool?> pair in Configuration.AllowedItems) {
                if (pair.Value == null)
                    throw new Exception($"Item {pair.Key} can only in the server configuration. Check the configurations/LoadoutLimits/Configuration.json file to fix this.");
            }
        }

        public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(RunnerPlayer player, OnPlayerSpawnArguments request) {
            List<string> errors = new();
            List<string> selections = new() {
                request.Loadout.PrimaryWeapon.ToolName,
                request.Loadout.SecondaryWeapon.ToolName,
                request.Loadout.HeavyGadgetName,
                request.Loadout.LightGadgetName,
                request.Loadout.ThrowableName
            };

            foreach (string selection in selections) {
                bool isAllowed = GetAllowedOf(selection);

                if (!isAllowed)
                    errors.Add(selection);
            }

            if (errors.Count == 0)
                return request;

            string errorsStr = string.Join("{/}, " + Configuration.EmphasisColor, errors);
            string response = new PlaceholderLib(Configuration.DeniedMessage)
                .AddParam("list", Configuration.EmphasisColor + errorsStr + "{/}")
                .Run();
            player.SayToChat(response);
            return null;
        }

        public void PopulateGadgets() {
            var gadgets = typeof(Gadgets).GetMembers(BindingFlags.Public | BindingFlags.Static);
            var weapons = typeof(Weapons).GetMembers(BindingFlags.Public | BindingFlags.Static);

            if (Configuration.AllowedItems.Count > 0)
                return;

            foreach (var memberInfo in weapons) {
                if (memberInfo.MemberType == MemberTypes.Field) {
                    var field = (FieldInfo)memberInfo;

                    if (field.FieldType == typeof(Weapon)) {
                        var weapon = (Weapon)field.GetValue(null);
                        Configuration.AllowedItems.Add(weapon.Name, true);
                        ServerConfiguration.AllowedItems.Add(weapon.Name, null);
                    }
                }
            }

            Configuration.AllowedItems.Add("G3", true);
            ServerConfiguration.AllowedItems.Add("G3", null);


            foreach (var memberInfo in gadgets) {
                if (memberInfo.MemberType == MemberTypes.Field) {
                    var field = (FieldInfo)memberInfo;
                    if (field.FieldType == typeof(Gadget)) {
                        var gadget = (Gadget)field.GetValue(null);
                        Configuration.AllowedItems.Add(gadget.Name, true);
                        ServerConfiguration.AllowedItems.Add(gadget.Name, null);
                    }
                }
            }

            Configuration.AllowedItems.Add("AntiGrenadeTrophy", true);
            ServerConfiguration.AllowedItems.Add("AntiGrenadeTrophy", null);

            Configuration.Save();
            ServerConfiguration.Save();
        }

        public bool GetAllowedOf(string name) {
            if (!Configuration.AllowedItems.ContainsKey(name)) {
                return true;
            }

            return Convert.ToBoolean(ServerConfiguration.AllowedItems[name] == null ? Configuration.AllowedItems[name] : ServerConfiguration.AllowedItems[name]);
        }
    }

    public class LoadoutLimitsConfig : ModuleConfiguration {
        public string EmphasisColor { get; set; } = "{#ffaaaa}";
        public string DeniedMessage { get; set; } = "{#ffaaaa}[SERVER]{/} You cannot use the following items: {list}";
        public Dictionary<string, bool?> AllowedItems { get; set; } = new();
    }
}
