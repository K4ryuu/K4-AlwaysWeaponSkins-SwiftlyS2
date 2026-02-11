using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace K4AlwaysWeaponSkins;

public sealed partial class Plugin
{
	private HookResult OnItemPickup(EventItemPickup @event)
	{
		var player = @event.UserIdPlayer;
		if (!player.IsValid || player.IsFakeClient)
			return HookResult.Continue;

		var pawn = player.PlayerPawn;
		if (pawn?.WeaponServices == null)
			return HookResult.Continue;

		var itemName = @event.Item;
		var lockKey = $"{player.SteamID}_{itemName}";

		try
		{
			if (!Config.CurrentValue.ApplyToMapWeapons || string.IsNullOrEmpty(itemName) || _pickupLocks.Contains(lockKey))
				return HookResult.Continue;

			_pickupLocks.Add(lockKey);

			foreach (var weaponHandle in pawn.WeaponServices.MyWeapons.ToList())
			{
				if (!weaponHandle.IsValid || weaponHandle.Value == null)
					continue;

				var weapon = weaponHandle.Value.As<CCSWeaponBase>();
				if (weapon == null)
					continue;

				var vData = weapon.WeaponBaseVData;
				if (vData == null || !WeaponHelper.IsSkinnable(vData.WeaponType))
					continue;

				if (weapon.AttributeManager.Item.ItemDefinitionIndex != @event.DefIndex)
					continue;

				var prevOwner = weapon.PrevOwner.Value?.OriginalController.Value;
				if (prevOwner?.Address == player.Controller.Address)
					continue;

				bool shouldApply = (prevOwner != null && Config.CurrentValue.ApplyOnPreviousOwner) || (prevOwner == null && Config.CurrentValue.ApplyOnNoPreviousOwner);
				if (!shouldApply)
					continue;

				ScheduleWeaponReplacement(player, weapon);
				break;
			}
		}
		catch (Exception ex)
		{
			Core.Logger.LogError(ex, "Error in OnItemPickup");
		}

		return HookResult.Continue;
	}

	private void ScheduleWeaponReplacement(IPlayer player, CCSWeaponBase weapon)
	{
		var playerId = player.PlayerID;
		var weaponID = weapon.AttributeManager.Item.ItemDefinitionIndex;
		var weaponName = Core.Helpers.GetClassnameByDefinitionIndex(weaponID) ?? string.Empty;

		if (string.IsNullOrEmpty(weaponName))
			return;

		SaveAmmoState(playerId, weaponName, weapon);

		Core.Scheduler.NextWorldUpdate(() =>
		{
			var currentPlayer = Core.PlayerManager.GetPlayer(playerId);
			if (currentPlayer?.IsValid != true || currentPlayer.PlayerPawn?.ItemServices == null)
				return;

			weapon.AddEntityIOEvent("Kill", string.Empty);
			currentPlayer.PlayerPawn.ItemServices.GiveItem(weaponName);

			Core.Scheduler.NextWorldUpdate(() =>
			{
				if (currentPlayer.IsValid)
					RestoreAmmoState(currentPlayer, weaponName);
			});
		});
	}

	private void SaveAmmoState(int playerId, string weaponName, CCSWeaponBase weapon)
	{
		if (!_savedAmmo.ContainsKey(playerId))
			_savedAmmo[playerId] = [];

		_savedAmmo[playerId][weaponName] = new AmmoState(
			weapon.Clip1,
			weapon.Clip2,
			weapon.ReserveAmmo[0],
			weapon.ReserveAmmo[1]
		);
	}

	private void RestoreAmmoState(IPlayer player, string weaponName)
	{
		try
		{
			if (!_savedAmmo.TryGetValue(player.PlayerID, out var playerAmmo) ||
				!playerAmmo.TryGetValue(weaponName, out var ammo))
				return;

			var pawn = player.PlayerPawn;
			if (pawn?.WeaponServices == null)
				return;

			foreach (var weaponHandle in pawn.WeaponServices.MyWeapons.ToList())
			{
				if (!weaponHandle.IsValid || weaponHandle.Value == null)
					continue;

				var weapon = weaponHandle.Value.As<CCSWeaponBase>();
				if (weapon?.DesignerName != weaponName)
					continue;

				weapon.Clip1 = ammo.Clip1;
				weapon.Clip2 = ammo.Clip2;
				weapon.ReserveAmmo[0] = ammo.Reserve0;
				weapon.ReserveAmmo[1] = ammo.Reserve1;
				break;
			}

			playerAmmo.Remove(weaponName);
		}
		catch (Exception ex)
		{
			Core.Logger.LogError(ex, "Error restoring ammo");
		}
	}
}
