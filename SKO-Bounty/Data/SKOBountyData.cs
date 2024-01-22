using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SKO.Bounty.Data
{
    public class BountyContracts
    {
        public HashSet<PlayerBountyContract> PlayerContracts { get; set; } = new HashSet<PlayerBountyContract>();
    }

    public abstract class BountyContract
    {
        [XmlAttribute] public long ContractorPlayerId { get; set; }

        [XmlAttribute] public long RewardAmount { get; set; }

        [XmlAttribute] public long ClaimedById { get; set; }

        [XmlAttribute] public long KilledById { get; set; }

        [XmlAttribute] public DateTime DateCreated { get; set; }

        [XmlAttribute] public DateTime DateFinished { get; set; }

        [XmlAttribute] public BountyContractState State { get; set; }
    }

    public class PlayerBountyContract : BountyContract
    {
        [XmlAttribute] public long TargetPlayerId { get; set; }
    }

    public enum BountyContractState
    {
        Active,
        Done
    }
}