namespace dotBento.Domain;

/// <summary>Optional capabilities shared by bot, API and infrastructure services.</summary>
public sealed class FeatureConfig
{
    public bool MediaEnabled { get; set; } = true;
    public bool HoroscopeEnabled { get; set; } = true;
    public bool DonationRewardsEnabled { get; set; } = true;
    public bool VotingRewardsEnabled { get; set; } = true;
    public bool PatreonEnabled { get; set; } = true;
}
