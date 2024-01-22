using System;
using System.Text;
using Sandbox.ModAPI;
using SKO.Bounty.Data;
using SKO.Bounty.Modules;
using SKO.Torch.Shared.Utils;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;

namespace SKO.Bounty.Commands
{
    [Category("bounty")]
    public class BountyCommands : CommandModule
    {
        [Command("info", "Profides information of the current Bounty Contracts")]
        [Permission(MyPromoteLevel.None)]
        public void BountyInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Commands:");
            sb.AppendLine("> !bounty add <target> <reward> - Adds a new contract for the target.");
            sb.AppendLine("> !bounty remove <target> - Removes an existing contract for the target.");
            sb.AppendLine("> !bounty list - Lists all active and finished contracts.");
            //sb.AppendLine($"> !bounty claim (Not implemented yet)");

            sb.AppendLine("");
            sb.AppendLine("Settings:");
            sb.AppendLine($"> Bounty Contracting Enabled: {BountyModule.Config.Enabled}");
            sb.AppendLine($"> Contracting Commission: {BountyModule.Config.ContractingCommission} %");
            sb.AppendLine($"> Contract Removing Commission: {BountyModule.Config.ContractRemovingCommission} %");
            sb.AppendLine("> Contract Claiming Commission: " +
                          (BountyModule.Config.MustClaimContract ? BountyModule.Config.ClaimingCommission : 0) + " %");
            sb.AppendLine(
                $"> Minimal Contract Reward: {string.Format("{0:n0}", BountyModule.Config.MinAcceptedReward)}");
            var m = new DialogMessage("Bounty Contract Info", "SKO's Bounty System", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }

        [Command("help", "Profides information of the current Bounty Contracts")]
        [Permission(MyPromoteLevel.None)]
        public void BountyHelp()
        {
            BountyInfo();
        }

        [Command("list", "Profides information of the current Bounty Contracts")]
        [Permission(MyPromoteLevel.None)]
        public void BountyList()
        {
            // Check if character is available.
            if (Context.Player == null || Context.Player.Character == null)
            {
                Context.Respond("Please get out of cockpit or respawn to use this command.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("> Warning: Please check !bounty info for any commission rates.");
            sb.AppendLine();
            sb.AppendLine();

            var contractListOutput = BountyModule.GetContractsListOutput();
            sb.AppendLine(contractListOutput.ToString());

            var m = new DialogMessage("Bounty Contract List", "SKO's Bounty System", sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
        }

        [Command("claim", "Claims a contract")]
        [Permission(MyPromoteLevel.None)]
        public void Claim(string contractorNameOrId, string targetNameOrId)
        {
            if (!BountyModule.Config.Enabled)
            {
                Context.Respond("Bounty System is not enabled.");
                return;
            }

            if (!BountyModule.Config.MustClaimContract)
            {
                Context.Respond("You don't need to claim a contract.");
                Context.Respond("Just go and eliminate the contracted target.");
                return;
            }

            if (Context.Player == null || Context.Player.Character == null)
            {
                Context.Respond("Please get out of cockpit or respawn to use this command.");
                return;
            }

            var contractor = PlayerUtils.GetIdentityByNameOrId(contractorNameOrId);
            if (contractor == null)
            {
                Context.Respond($"Can't find contractor with name or id {contractorNameOrId}");
                return;
            }

            var target = PlayerUtils.GetIdentityByNameOrId(targetNameOrId);
            if (target == null)
            {
                Context.Respond($"Can't find target with name or id {targetNameOrId}");
                return;
            }

            var contract = BountyModule.GetPlayerContract(target.IdentityId, contractor.IdentityId);
            if (contract == null)
            {
                Context.Respond("Contract does not exist.");
                return;
            }

            if (contract.ClaimedById > 0)
            {
                if (contract.ClaimedById == Context.Player.IdentityId)
                {
                    Context.Respond("This contract is already claimed by you.");
                    return;
                }

                Context.Respond("This contract is already claimed by someone else.");
                return;
            }

            // Check if commission is needed?
            long creditsRequired = 0;
            if (BountyModule.Config.ClaimingCommission > 0)
            {
                var commissionRate = (decimal)BountyModule.Config.ClaimingCommission / 100;
                var commissionRequired = Math.Round(contract.RewardAmount * commissionRate, 2);
                creditsRequired = (long)Math.Round(contract.RewardAmount + commissionRequired);
            }

            // Claim the contract.
            lock (BountyModule.Contracts)
            {
                contract.ClaimedById = Context.Player.IdentityId;
            }

            // Check balance.
            if (creditsRequired > 0 && !CreditsUtils.HasSufficientCredits(Context.Player.IdentityId, creditsRequired))
            {
                Context.Respond("You don't have sufficient credits to pay the commission!");
                Context.Respond("Check '!bounty info' for more information.");
                return;
            }

            if (creditsRequired > 0 && CreditsUtils.RemoveCredits(Context.Player.IdentityId, creditsRequired))
            {
                Context.Respond("Contract claim commission payed!");
                Context.Respond($"Amount: {string.Format("{0:n0}", creditsRequired)}");
            }

            if (BountyModule.SaveContracts())
            {
                Context.Respond("You've claimed the contract! Happy hunting!");
                ChatUtils.SendToAll($"Contract claimed by {Context.Player.DisplayName}");
            }
            else
            {
                Context.Respond("An error occured while saving the claim.");

                if (creditsRequired > 0)
                {
                    CreditsUtils.AddCredits(Context.Player.IdentityId, creditsRequired);
                    Context.Respond("Contract claim commission refunded!");
                }
            }
        }

        [Command("add", "Adds a Bounty to the list.")]
        [Permission(MyPromoteLevel.None)]
        public void AddBounty(string playerNameOrId, long rewardAmount)
        {
            if (!BountyModule.Config.Enabled)
            {
                Context.Respond("Bounty System is not enabled.");
                return;
            }

            // Check if character is available.
            if (Context.Player == null || Context.Player.Character == null)
            {
                Context.Respond("Please get out of cockpit or respawn to use this command.");
                return;
            }

            if (rewardAmount < BountyModule.Config.MinAcceptedReward)
            {
                Context.Respond($"Minimal accepted reward: {BountyModule.Config.MinAcceptedReward}");
                return;
            }

            // Get target player.
            var player = PlayerUtils.GetIdentityByNameOrId(playerNameOrId);
            if (player == null)
            {
                Context.Respond($"Player {playerNameOrId} does not exist.");
                return;
            }

            // Check bounty on self and faction member
            if (player.IdentityId == Context.Player.IdentityId)
            {
                Context.Respond("You can't add a Bounty Contract on yourself :)");
                return;
            }

            if (FactionUtils.HavePlayersSameFaction(player.IdentityId, Context.Player.IdentityId))
            {
                Context.Respond("You can't add a Bounty on a faction member :)");
                return;
            }

            // Contract already exists?
            if (BountyModule.ExistsPlayerContract(player.IdentityId, Context.Player.IdentityId))
            {
                Context.Respond($"Bounty Contract already exists for player {player.DisplayName}.");
                return;
            }

            var commissionRate = (decimal)BountyModule.Config.ContractingCommission / 100;
            var commissionRequired = Math.Round(rewardAmount * commissionRate, 2);
            var creditsRequired = (long)Math.Round(rewardAmount + commissionRequired);

            // Check balance.
            if (CreditsUtils.HasSufficientCredits(Context.Player.IdentityId, creditsRequired))
            {
                // Define new contract.
                var newContract = new PlayerBountyContract
                {
                    TargetPlayerId = player.IdentityId,
                    ContractorPlayerId = Context.Player.IdentityId,
                    DateCreated = DateTime.UtcNow,
                    RewardAmount = rewardAmount,
                    State = BountyContractState.Active
                };

                // Remove credits from balance as we hold it in the contract.
                if (CreditsUtils.RemoveCredits(Context.Player.IdentityId, creditsRequired))
                {
                    // If contract has been added and saved...
                    if (BountyModule.AddPlayerContract(newContract) && BountyModule.SaveContracts())
                    {
                        Context.Respond("Contract created!.");

                        Context.Respond($"You payed: {string.Format("{0:n0}", creditsRequired)}");
                        Context.Respond($"Reward: {string.Format("{0:n0}", rewardAmount)}");
                        Context.Respond($"Commission: {string.Format("{0:n0}", commissionRequired)}");

                        MyAPIGateway.Parallel.Do(() =>
                        {
                            MyAPIGateway.Parallel.Sleep(3000);
                            ChatUtils.SendToAll($@"New Bounty Contract!
Target: {player.DisplayName}
Contractor: {Context.Player.DisplayName}
Reward: {string.Format("{0:n0}", rewardAmount)}
Eliminate the target and claim your reward!
");
                            SoundUtils.SendToAll();
                        });

                        // If the player is online and not dead.
                        if (player.Character != null)
                            MyAPIGateway.Parallel.Do(() =>
                            {
                                MyAPIGateway.Parallel.Sleep(3000);
                                NotificationUtils.SendTo(player.IdentityId,
                                    $"There is a bounty on your head {player.DisplayName}!", 10);
                            });
                    }
                    else
                    {
                        // Rollback credits.
                        Context.Respond("Can't add the contract.");
                        Context.Respond("Try again later...");
                        CreditsUtils.AddCredits(Context.Player.IdentityId, creditsRequired);
                    }
                }
            }
            else
            {
                Context.Respond("Insufficent credits!");
                Context.Respond($"Total needed: {string.Format("{0:n0}", creditsRequired)}");
                Context.Respond($"Reward: {string.Format("{0:n0}", rewardAmount)}");
                Context.Respond($"Commission: {string.Format("{0:n0}", commissionRequired)}");
            }
        }

        [Command("remove", "Removes a bounty from the list.")]
        [Permission(MyPromoteLevel.None)]
        public void RemoveBounty(string playerNameOrId)
        {
            if (!BountyModule.Config.Enabled)
            {
                Context.Respond("Bounty System is not enabled.");
                return;
            }

            // Check if character is available.
            if (Context.Player == null || Context.Player.Character == null)
            {
                Context.Respond("Please get out of cockpit or respawn to use this command.");
                return;
            }

            // Get target player.
            var target = PlayerUtils.GetIdentityByNameOrId(playerNameOrId);
            if (target == null)
            {
                Context.Respond($"Player {playerNameOrId} does not exist.");
                return;
            }

            if (PlayerUtils.IsNpc(target.IdentityId) || target.IdentityId == 0)
            {
                Context.Respond("You can't add bounty contracts on an NPC!");
                return;
            }

            // Contract already exists?
            if (!BountyModule.ExistsPlayerContract(target.IdentityId, Context.Player.IdentityId))
            {
                Context.Respond($"No contracts found for {target.DisplayName}.");
                return;
            }

            var contract = BountyModule.GetPlayerContract(target.IdentityId, Context.Player.IdentityId);
            var creditsToReturn = (long)(contract.RewardAmount -
                                         contract.RewardAmount *
                                         (BountyModule.Config.ContractRemovingCommission / 100));

            if (BountyModule.RemovePlayerContract(target.IdentityId, Context.Player.IdentityId) &&
                BountyModule.SaveContracts())
            {
                if (CreditsUtils.AddCredits(Context.Player.IdentityId, creditsToReturn))
                {
                    Context.Respond($"The contract on {target.DisplayName} has been removed!");
                    if (BountyModule.Config.ContractRemovingCommission > 0)
                        Context.Respond($"Comission payed: {BountyModule.Config.ContractRemovingCommission}%");
                    Context.Respond($"Refunded: {string.Format("{0:n0}", creditsToReturn)}");

                    MyAPIGateway.Parallel.Do(() =>
                    {
                        MyAPIGateway.Parallel.Sleep(3000);
                        ChatUtils.SendToAll($"Removed bounty contract for {target.DisplayName}!");
                        if (target.Character != null)
                            NotificationUtils.SendTo(target.IdentityId,
                                $"A bounty contract on your head just got removed {target.DisplayName}!", 10);
                        SoundUtils.SendToAll();
                    });
                }
            }
            else
            {
                Context.Respond("Can't remove contract. Try again later.");
            }
        }

        [Command("reload", "Reloads config and contracts.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Reload()
        {
            BountyModule.LoadConfig();
            BountyModule.LoadContracts();

            Context.Respond("Reloaded config and contracts.");
        }

        [Command("save", "Save contracts.")]
        [Permission(MyPromoteLevel.Admin)]
        public void Save()
        {
            BountyModule.SaveContracts();
            Context.Respond("Contracts saved.");
        }

        [Command("config", "Show the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void Config()
        {
            if (BountyModule.Config != null)
            {
                Context.Respond("SKO-Bounty Config:");
                Context.Respond($"> Debug: {BountyModule.Config.Debug}");
                Context.Respond($"> Enabled: {BountyModule.Config.Enabled}");
                Context.Respond($"> MinAcceptedReward: {BountyModule.Config.MinAcceptedReward}");
                Context.Respond($"> MustClaimContract: {BountyModule.Config.MustClaimContract}");
                Context.Respond($"> ContractRemovingCommission: {BountyModule.Config.ContractRemovingCommission}");
                Context.Respond($"> ContractingCommission: {BountyModule.Config.ContractingCommission}");
                Context.Respond($"> ClaimingCommission: {BountyModule.Config.ClaimingCommission}");
                Context.Respond($"> FinishedContractsToKeep: {BountyModule.Config.FinishedContractsToKeep}");
            }
        }
    }
}