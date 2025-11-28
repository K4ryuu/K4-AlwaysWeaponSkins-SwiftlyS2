namespace K4AlwaysWeaponSkins;

/// <summary>
/// Configuration for K4-AlwaysWeaponSkins plugin
/// </summary>
public sealed class PluginConfig
{
	/// <summary>
	/// Apply skins to weapons picked up from the map (not from loadout)
	/// </summary>
	public bool ApplyToMapWeapons { get; set; } = true;

	/// <summary>
	/// Apply skins when picking up weapons with no previous owner
	/// </summary>
	public bool ApplyOnNoPreviousOwner { get; set; } = true;

	/// <summary>
	/// Apply skins when picking up weapons that had a previous owner
	/// </summary>
	public bool ApplyOnPreviousOwner { get; set; } = true;
}
