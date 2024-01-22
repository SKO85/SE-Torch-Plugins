using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using SKO.Bounty.Configurations;
using SKO.Bounty.Data;
using SKO.Bounty.Models;
using SKO.Torch.Shared.Utils;
using VRage.Game.ModAPI;

namespace SKO.Bounty.Modules
{
    public static class BountyModule
    {
        public static string ConfigFile = "SKOBountyConfig.cfg";
        public static string ContractsFile = "SKOBountyContracts.xml";

        public static SKOBountyConfig Config = new SKOBountyConfig();
        public static BountyContracts Contracts = new BountyContracts();

        public static void InitializeEvents()
        {
            MyAPIGateway.Session.DamageSystem.RegisterDestroyHandler(96, (target, info) => OnDamage(target, ref info));
        }

        public static void DebugKillTypes(object obj)
        {
            if (obj == null)
                SKOBountyPlugin.Log.Warn($"> IMyCubeBlock: {obj is IMyCubeBlock}");

            SKOBountyPlugin.Log.Warn($"> IMyCubeBlock: {obj is IMyCubeBlock}");
            SKOBountyPlugin.Log.Warn($"> MyCubeBlock: {obj is MyCubeBlock}");
            SKOBountyPlugin.Log.Warn($"> IMyCubeGrid: {obj is IMyCubeGrid}");
            SKOBountyPlugin.Log.Warn($"> IMyPlayer: {obj is IMyPlayer}");
            SKOBountyPlugin.Log.Warn($"> IMyHandheldGunObject<MyGunBase>: {obj is IMyHandheldGunObject < MyGunBase >}");
            SKOBountyPlugin.Log.Warn(
                $"> IMyHandheldGunObject<MyToolBase>: {obj is IMyHandheldGunObject < MyToolBase >}");
            SKOBountyPlugin.Log.Warn(
                $"> IMyHandheldGunObject<MyToolBase>: {obj is IMyHandheldGunObject < MyToolBase >}");
            SKOBountyPlugin.Log.Warn($"> IMyGunObject: {obj is IMyGunObject < MyGunBase >}");
            SKOBountyPlugin.Log.Warn($"> IMyGunBaseUser: {obj is IMyGunBaseUser}");
            SKOBountyPlugin.Log.Warn($"> IMyMissile: {obj is IMyMissile}");
            SKOBountyPlugin.Log.Warn($"> Attacker-Type: {obj.GetType().FullName}");
        }

        public static void DebugKiller(IMyPlayer obj)
        {
            SKOBountyPlugin.Log.Warn($"> Killer null?: {obj == null}");
            if (obj != null)
            {
                SKOBountyPlugin.Log.Warn($"> Killer Name: {obj.DisplayName}");
                SKOBountyPlugin.Log.Warn($"> Killer EntityId: {obj.Identity.IdentityId}");
                SKOBountyPlugin.Log.Warn($"> Killer SteamId: {obj.SteamUserId}");
            }
        }

        private static IMyPlayer GetKiller(MyDamageInformation info)
        {
            IMyPlayer killer = null;
            var attackEntity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);
            if (attackEntity != null)
            {
                if (attackEntity is IMyCubeBlock)
                {
                    var cube = attackEntity is MyCubeBlock
                        ? attackEntity as MyCubeBlock
                        : attackEntity as MyFunctionalBlock;
                    if (cube.CubeGrid != null)
                    {
                        // First, try to find the controlling player (pilot).
                        if (cube.CubeGrid.HasMainCockpit() && cube.CubeGrid.MainCockpit != null)
                        {
                            var shipController = cube.CubeGrid.MainCockpit as IMyShipController;
                            if (shipController.ControllerInfo?.ControllingIdentityId > 0)
                                killer = PlayerUtils.GetPlayer(shipController.ControllerInfo.ControllingIdentityId);
                        }

                        // Ok, there is no-one controlling the ship so try get the owner of the grid.
                        if (killer == null) killer = PlayerUtils.GetPlayer(PlayerUtils.GetOwner(cube.CubeGrid));
                    }
                }
                // Grid:
                else if (attackEntity is IMyCubeGrid)
                {
                    killer = PlayerUtils.GetPlayer(PlayerUtils.GetOwner(attackEntity as MyCubeGrid));
                }

                // Player:
                else if (attackEntity is IMyPlayer)
                {
                    killer = attackEntity as IMyPlayer;
                }

                // Character:
                else if (attackEntity is IMyCharacter)
                {
                    killer = PlayerUtils.GetPlayer((attackEntity as MyCharacter).GetPlayerIdentityId());
                }

                // Rifle:
                else if (attackEntity is IMyHandheldGunObject<MyGunBase>)
                {
                    killer = PlayerUtils.GetPlayer((attackEntity as IMyHandheldGunObject<MyGunBase>).OwnerIdentityId);
                }

                // Hand Tools:
                else if (attackEntity is IMyHandheldGunObject<MyToolBase>)
                {
                    killer = PlayerUtils.GetPlayer((attackEntity as IMyHandheldGunObject<MyToolBase>).OwnerIdentityId);
                }

                // Missile
                else if (attackEntity is IMyMissile)
                {
                    var ownerId = (attackEntity as IMyMissile).Owner;
                    killer = PlayerUtils.GetPlayer(ownerId);

                    if (killer == null)
                    {
                        // Try get the entity weapon block first.
                        var entity = MyEntities.GetEntityByIdOrDefault<MyFunctionalBlock>(ownerId, allowClosed: true);
                        if (entity != null) killer = PlayerUtils.GetPlayer(entity.OwnerId);
                    }
                }

                // Something else:
                else
                {
                    SKOBountyPlugin.Log.Warn("Undetected kill type:");
                    SKOBountyPlugin.Log.Warn($"> DisplayName: {attackEntity.DisplayName}");
                    SKOBountyPlugin.Log.Warn($"> DisplayName: {attackEntity.Name}");
                    SKOBountyPlugin.Log.Warn($"> Type: {attackEntity.GetType()}");
                    SKOBountyPlugin.Log.Warn(
                        "> Report this to SKO on Discord so the types can be added as some might be missed or could be DLC-related.");
                }

                if (Config.Debug)
                {
                    DebugKillTypes(attackEntity);
                    DebugKiller(killer);
                }
            }

            return killer;
        }

        public static void OnDamage(object target, ref MyDamageInformation info)
        {
            if (target is MyCharacter)
            {
                var character = target as MyCharacter;
                if (character == null)
                    return;

                var targetPlayer = PlayerUtils.GetPlayer(character.GetPlayerIdentityId());
                if (targetPlayer == null)
                    return;

                var killer = GetKiller(info);

                // Check if there are contracts for this player.
                var contracts = GetPlayerContractsForTarget(targetPlayer.IdentityId);
                if (contracts != null && contracts.Count > 0)
                {
                    // Contracts found, now check who or what killed this player...
                    var attackEntity = MyAPIGateway.Entities.GetEntityById(info.AttackerId);

                    if (killer != null)
                    {
                        PlayerBountyContract claimedContract = null;
                        lock (Contracts)
                        {
                            foreach (var contract in contracts)
                                // Check if the contract can be claimed.
                                if (CanClaim(targetPlayer, killer, contract))
                                {
                                    contract.DateFinished = DateTime.UtcNow;
                                    contract.State = BountyContractState.Done;
                                    contract.KilledById = killer.IdentityId;

                                    claimedContract = contract;
                                    break;
                                }
                        }

                        // Do we have found a claimed contract?
                        if (claimedContract != null)
                        {
                            if (Config.Debug) SKOBountyPlugin.Log.Warn("Contract found!");

                            // Save the changes.
                            SaveContracts();

                            if (CreditsUtils.AddCredits(killer.IdentityId, claimedContract.RewardAmount))
                            {
                                if (Config.Debug)
                                    SKOBountyPlugin.Log.Warn(
                                        $"Credits added {killer.DisplayName}: {claimedContract.RewardAmount}!");

                                ChatUtils.SendTo(killer.IdentityId, $"Well done {killer.DisplayName}");
                                ChatUtils.SendTo(killer.IdentityId,
                                    $"You've claimed your reward for killing {targetPlayer.DisplayName}");
                                ChatUtils.SendTo(killer.IdentityId,
                                    $"Rewarded with {string.Format("{0:n0}", claimedContract.RewardAmount)} on your balance account.");

                                var contractor = PlayerUtils.GetPlayer(claimedContract.ContractorPlayerId);
                                if (contractor != null && contractor.Character != null)
                                {
                                    ChatUtils.SendTo(contractor.IdentityId, "Bounty Contract Terminated!");
                                    ChatUtils.SendTo(contractor.IdentityId,
                                        $"{killer.DisplayName} killed {targetPlayer.DisplayName}.");
                                    ChatUtils.SendTo(contractor.IdentityId,
                                        $"Reward of {string.Format("{0:n0}", claimedContract.RewardAmount)} has been payed out!");
                                }

                                MyAPIGateway.Parallel.Do(() =>
                                {
                                    MyAPIGateway.Parallel.Sleep(3000);
                                    ChatUtils.SendTo(0, $"Bounty Reward for {killer.DisplayName}!");
                                    ChatUtils.SendTo(0, $"{targetPlayer.DisplayName} has been killed!");
                                    ChatUtils.SendTo(0,
                                        $"Reward:  {string.Format("{0:n0}", claimedContract.RewardAmount)}");
                                });
                            }
                            else
                            {
                                if (Config.Debug) SKOBountyPlugin.Log.Warn("Cannot add credits!");
                            }
                        }
                        else
                        {
                            if (Config.Debug) SKOBountyPlugin.Log.Warn("No contract found!");
                        }
                    }
                    else
                    {
                        if (Config.Debug)
                        {
                            SKOBountyPlugin.Log.Warn("No killer found!");
                            SKOBountyPlugin.Log.Warn($"Killed Id: {info.AttackerId}");
                        }
                    }
                }
            }
        }

        public static bool CanClaim(IMyPlayer targetPlayer, IMyPlayer killer, PlayerBountyContract contract)
        {
            if (FactionUtils.HavePlayersSameFaction(targetPlayer.IdentityId, killer.IdentityId))
            {
                if (Config.Debug) SKOBountyPlugin.Log.Warn("CanClaim: Target player and killer have same faction.");
                return false;
            }

            if (FactionUtils.HavePlayersSameFaction(killer.IdentityId, contract.ContractorPlayerId))
            {
                if (Config.Debug) SKOBountyPlugin.Log.Warn("CanClaim: Killer and Contractor have same faction.");
                return false;
            }

            if (FactionUtils.IsFriendlyWith(killer.IdentityId, targetPlayer.IdentityId) && !Config.AllowFriendlyToClaim)
            {
                if (Config.Debug) SKOBountyPlugin.Log.Warn("CanClaim: Killer and Target are in peace (friendly).");
                return false;
            }

            if (Config.MustClaimContract && contract.ClaimedById > 0 && contract.ClaimedById != killer.IdentityId)
            {
                if (Config.Debug) SKOBountyPlugin.Log.Warn("CanClaim: MustClaim check...");

                return false;
            }

            if (Config.Debug) SKOBountyPlugin.Log.Warn("CanClaim: Allowed.");
            return true;
        }

        public static void LoadConfig()
        {
            lock (Config)
            {
                Config = ConfigUtils.Load<SKOBountyConfig>(SKOBountyPlugin.Instance, ConfigFile);
                Config.Validate();
            }
        }

        public static bool SaveConfig()
        {
            lock (Config)
            {
                Config.Validate();
                return ConfigUtils.Save(SKOBountyPlugin.Instance, Config, ConfigFile);
            }
        }

        public static void LoadContracts()
        {
            lock (Contracts)
            {
                Contracts = ConfigUtils.Load<BountyContracts>(SKOBountyPlugin.Instance, ContractsFile);
            }
        }

        public static bool SaveContracts()
        {
            lock (Contracts)
            {
                CleanContracts();
                return ConfigUtils.Save(SKOBountyPlugin.Instance, Contracts, ContractsFile);
            }
        }

        public static void CleanContracts()
        {
            var contracts = Contracts.PlayerContracts.Where(c => c.State == BountyContractState.Done).ToList();
            if (contracts.Count > Config.FinishedContractsToKeep)
            {
                var toRemove = contracts.Count - Config.FinishedContractsToKeep;
                contracts.RemoveRange(0, toRemove);
            }
        }

        public static bool AddPlayerContract(PlayerBountyContract contract)
        {
            if (ExistsPlayerContract(contract.TargetPlayerId, contract.ContractorPlayerId))
                return false;

            lock (Contracts)
            {
                Contracts.PlayerContracts.Add(contract);
            }

            return true;
        }

        public static bool RemovePlayerContract(long targetId, long contractorId,
            BountyContractState state = BountyContractState.Active)
        {
            var contract = GetPlayerContract(targetId, contractorId, state);
            if (contract == null)
                return false;

            lock (Contracts)
            {
                return Contracts.PlayerContracts.Remove(contract);
            }
        }

        public static List<PlayerBountyContract> GetPlayerContractsForTarget(long targetId,
            BountyContractState state = BountyContractState.Active)
        {
            return Contracts.PlayerContracts.Where(c => c.State == state && c.TargetPlayerId == targetId).ToList();
        }

        public static bool ExistsPlayerContract(long targetId, long contractorId,
            BountyContractState state = BountyContractState.Active)
        {
            return GetPlayerContract(targetId, contractorId, state) != null;
        }

        public static PlayerBountyContract GetPlayerContract(long targetId, long contractorId,
            BountyContractState state = BountyContractState.Active)
        {
            var contract = Contracts.PlayerContracts
                .FirstOrDefault(c =>
                    c.State == state && c.TargetPlayerId == targetId && c.ContractorPlayerId == contractorId);
            return contract;
        }

        public static StringBuilder GetContractsListOutput()
        {
            var activeContracts = Contracts.PlayerContracts.Where(c => c.State == BountyContractState.Active)
                .OrderByDescending(c => c.DateCreated).ToList();
            var doneContracts = Contracts.PlayerContracts.Where(c => c.State == BountyContractState.Done)
                .OrderByDescending(c => c.DateFinished).ToList();

            var sb = new StringBuilder();
            if (activeContracts.Count > 0)
            {
                var table = new TextTable($"Active Contracts ({activeContracts.Count})");
                table.Columns.Add(new TextTableColumn("Contractor / Target / Claimed", maxWidth: 78));
                table.Columns.Add(new TextTableColumn("Reward", maxWidth: 27));
                table.Columns.Add(new TextTableColumn("Created", maxWidth: 23));

                foreach (var item in activeContracts)
                {
                    var contractor = PlayerUtils.GetIdentityById(item.ContractorPlayerId);
                    var target = PlayerUtils.GetIdentityById(item.TargetPlayerId);
                    var claimed = PlayerUtils.GetIdentityById(item.ClaimedById);

                    if (contractor != null && target != null)
                    {
                        var row = new TextTableRow();
                        var claimedBy = claimed != null ? claimed.DisplayName : "*";
                        row.Cells.Add($"{contractor.DisplayName} / {target.DisplayName} / {claimedBy}");
                        row.Cells.Add(item.RewardAmount.ToString());
                        row.Cells.Add(item.DateCreated.ToString("dd-MM HH:mm"));
                        table.Rows.Add(row);
                    }
                    // Remove this contract as something is not right... :)
                }

                sb.AppendLine(table.GetOutput().ToString());
            }
            else
            {
                sb.AppendLine("> Active Contracts (0):");
                sb.AppendLine("> There are no active bounty contracts at the moment.");
                sb.AppendLine("");
            }

            if (doneContracts.Count > 0)
            {
                var table = new TextTable($"Finished Contracts ({doneContracts.Count})");
                table.Columns.Add(new TextTableColumn("Contractor / Target / Killer", maxWidth: 78));
                table.Columns.Add(new TextTableColumn("Reward", maxWidth: 27));
                table.Columns.Add(new TextTableColumn("Finished", maxWidth: 23));

                foreach (var item in doneContracts)
                {
                    var contractor = PlayerUtils.GetIdentityById(item.ContractorPlayerId);
                    var target = PlayerUtils.GetIdentityById(item.TargetPlayerId);
                    var killer = PlayerUtils.GetIdentityById(item.KilledById);

                    if (contractor != null && target != null && killer != null)
                    {
                        var row = new TextTableRow();
                        row.Cells.Add($"{contractor.DisplayName} / {target.DisplayName} / {killer.DisplayName}");
                        row.Cells.Add(item.RewardAmount.ToString());
                        row.Cells.Add(item.DateCreated.ToString("dd-MM HH:mm"));
                        table.Rows.Add(row);
                    }
                    // Remove this contract as something is not right... :)
                }

                sb.AppendLine(table.GetOutput().ToString());
            }
            else
            {
                sb.AppendLine("> Finished Contracts (0):");
                sb.AppendLine("> There are no finished bounty contracts at the moment.");
                sb.AppendLine("");
            }

            return sb;
        }
    }
}