﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UpgradeManager : MonoBehaviour {

	private static float TimeBetweenBitGeneration = 1f; // Time in seconds after which to generate bits

	private UpgradeState upgradeState;
	public UpgradeState UpgradeState {
		get {
			return upgradeState;
		}
	}

	private Dictionary<int, Upgrade> upgradeCache;
	private float timeToGenerate;

	void Start() {
		upgradeCache = new Dictionary<int, Upgrade>();
		timeToGenerate = TimeBetweenBitGeneration;

		RecalculateUpgradeState();
	}

	void Update() {
		timeToGenerate -= Time.deltaTime;

		if (timeToGenerate <= 0f) {
			timeToGenerate = TimeBetweenBitGeneration;

			#if UNITY_EDITOR
			Debug.Log("Generating: " + (upgradeState.GeneratedBPS * GameManager.Instance.GameState.StoredBits));
			#endif

			GameManager.Instance.StorageUnitManager.AddBits(
				Mathf.RoundToInt(upgradeState.GeneratedBPS * GameManager.Instance.GameState.StoredBits)
			);
		}
	}

	public string GetDescription(int type, float value) {
		for (int i = 0; i < GameManager.Instance.GameConfiguration.UpgradeTypes.Length; i++) {
			UpgradeType upgradeType = GameManager.Instance.GameConfiguration.UpgradeTypes[i];
			if (upgradeType.Id == type) {
				return upgradeType.Description.Replace("@", (value * 100).ToString("0.0") + "%");
			}
		}

		return "";
	}

	public bool IsTierUnlocked(int tierId) {
		return tierId <= GameManager.Instance.GameState.NextTargetId;
	}

	public int PurchasedCount(int upgradeId) {
		for (int i = 0; i < GameManager.Instance.GameState.PurchasedUpgrades.Count; i++) {
			if (GameManager.Instance.GameState.PurchasedUpgrades[i].UpgradeId == upgradeId) {
				return GameManager.Instance.GameState.PurchasedUpgrades[i].QuantityPurchased;
			}
		}

		return 0;
	}

	public void PurchaseUpgrade(int upgradeId) {
		bool found = false;
		for (int i = 0; i < GameManager.Instance.GameState.PurchasedUpgrades.Count; i++) {
			PurchasedUpgrade purchasedUpgrade = GameManager.Instance.GameState.PurchasedUpgrades[i];
			if (purchasedUpgrade.UpgradeId == upgradeId) {
				GameManager.Instance.GameState.PurchasedUpgrades[i].QuantityPurchased ++;// = new PurchasedUpgrade(upgradeId, purchasedUpgrade.QuantityPurchased + 1);
				found = true;
				break;
			}
		}

		// Not found, add it.
		if (!found) {
			GameManager.Instance.GameState.PurchasedUpgrades.Add(new PurchasedUpgrade(upgradeId, 1));
		}

		RecalculateUpgradeState();
	}

	Upgrade UpgradeFromId(int upgradeId) {
		if (upgradeCache.ContainsKey(upgradeId)) {
			return upgradeCache[upgradeId];
		}

		for (int i = 0; i < GameManager.Instance.GameConfiguration.UpgradeTiers.Length; i++) {
			for (int x = 0; x < GameManager.Instance.GameConfiguration.UpgradeTiers[i].Upgrades.Length; x++) {
				Upgrade upgrade = GameManager.Instance.GameConfiguration.UpgradeTiers[i].Upgrades[x];
				if (upgrade.Id == upgradeId) {
					upgradeCache.Add(upgrade.Id, upgrade);
					return upgrade;
				}
			}
		}

		return null;
	}

	UpgradeType UpgradeTypeFromId(int typeId) {
		for (int i = 0; i < GameManager.Instance.GameConfiguration.UpgradeTypes.Length; i++) {
			UpgradeType type = GameManager.Instance.GameConfiguration.UpgradeTypes[i];
			if (type.Id == typeId) {
				return type;
			}
		}

		return null;
	}

	public void RecalculateUpgradeState() {
		upgradeState = new UpgradeState();

		List<PurchasedUpgrade> purchasedUpgrades = GameManager.Instance.GameState.PurchasedUpgrades;
		for (int i = 0; i < purchasedUpgrades.Count; i++) {
			Upgrade upgrade = UpgradeFromId(purchasedUpgrades[i].UpgradeId);
			float value = upgrade.Value * purchasedUpgrades[i].QuantityPurchased;

			switch(upgrade.Type) {
			case UpgradeType.Automation:
				upgradeState.InboundBPS += value;
				break;
			case UpgradeType.Botnet:
				upgradeState.OutboundBPSForAttack += value;
				break;
			case UpgradeType.Network:
				upgradeState.OutboundBPS += value;
				break;
			case UpgradeType.Compression:
				upgradeState.StorageCapacity += value;
				break;
			case UpgradeType.Replication:
				upgradeState.GeneratedBPS += value;
				break;
			}
		}

		Debug.Log(upgradeState);
	}

}
