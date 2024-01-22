using System.Xml.Serialization;
using Torch;

namespace SKO.Bounty.Configurations
{
    public class SKOBountyConfig : ViewModel
    {
        public bool Debug { get; set; } = false;
        public bool Enabled { get; set; } = false;
        public bool AllowFriendlyToClaim { get; set; } = false;
        public double ContractRemovingCommission { get; set; }
        public long MinAcceptedReward { get; set; } = 10000;
        public int ContractingCommission { get; set; } = 10;
        public int ClaimingCommission { get; set; } = 10;
        public int FinishedContractsToKeep { get; set; } = 20;

        [XmlIgnore] public bool MustClaimContract { get; set; } = false;

        public void Validate()
        {
            if (ContractRemovingCommission < 0) ContractRemovingCommission = 0;
            else if (ContractRemovingCommission > 100) ContractRemovingCommission = 100;

            if (MinAcceptedReward < 0) MinAcceptedReward = 0;

            if (ContractingCommission < 0) ContractingCommission = 0;
            else if (ContractingCommission > 100) ContractingCommission = 100;

            if (ClaimingCommission < 0) ClaimingCommission = 0;
            else if (ClaimingCommission > 100) ClaimingCommission = 100;

            if (FinishedContractsToKeep < 0) FinishedContractsToKeep = 0;
            else if (FinishedContractsToKeep > 100) FinishedContractsToKeep = 100;
        }
    }
}