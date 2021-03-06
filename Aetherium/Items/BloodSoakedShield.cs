﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using UnityEngine;
using static Aetherium.CoreModules.StatHooks;
using static Aetherium.Utils.ItemHelpers;
using static Aetherium.Utils.MathHelpers;

namespace Aetherium.Items
{
    public class BloodSoakedShield : ItemBase<BloodSoakedShield>
    {
        public static bool UseNewIcons;
        public static float ShieldPercentageRestoredPerKill;
        public static float AdditionalShieldPercentageRestoredPerKillDiminishing;
        public static float MaximumPercentageShieldRestoredPerKill;
        public static float BaseGrantShieldMultiplier;

        public override string ItemName => "Blood Soaked Shield";
        public override string ItemLangTokenName => "BLOOD_SOAKED_SHIELD";
        public override string ItemPickupDesc => "Killing an enemy <style=cIsHealing>restores</style> a small portion of <style=cIsHealing>shield</style>.";

        public override string ItemFullDescription => $"Killing an enemy restores <style=cIsUtility>{FloatToPercentageString(ShieldPercentageRestoredPerKill)} max shield</style> " +
            $"<style=cStack>(+{FloatToPercentageString(AdditionalShieldPercentageRestoredPerKillDiminishing)} per stack hyperbolically.)</style> " +
            $"This item will grant <style=cIsUtility>{FloatToPercentageString(BaseGrantShieldMultiplier)}</style> of your max health as shield on pickup once.";

        public override string ItemLore => "An old gladiatorial round shield. The bloody spikes and Greek lettering give you an accurate picture of what it was used to do. Somehow, holding it makes you feel empowered.";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override string ItemModelPath => "@Aetherium:Assets/Models/Prefabs/Item/BloodSoakedShield/BloodSoakedShield.prefab";
        public override string ItemIconPath => UseNewIcons ? "@Aetherium:Assets/Textures/Icons/Item/BloodSoakedShieldIconAlt.png" : "@Aetherium:Assets/Textures/Icons/Item/BloodSoakedShieldIcon.png";

        public static GameObject ItemBodyModelPrefab;

        public BloodSoakedShield()
        {
        }

        public override void Init(ConfigFile config)
        {
            CreateConfig(config);
            CreateLang();
            CreateMaterials();
            CreateItem();
            Hooks();
        }

        private void CreateConfig(ConfigFile config)
        {
            UseNewIcons = config.Bind<bool>("Item: " + ItemName, "Use Alternative Icon Art?", true, "If set to true, will use the new icon art drawn by WaltzingPhantom, else it will use the old icon art.").Value;
            ShieldPercentageRestoredPerKill = config.Bind<float>("Item: " + ItemName, "Percentage of Shield Restored per Kill", 0.1f, "How much shield in percentage should be restored per kill? 0.1 = 10%").Value;
            AdditionalShieldPercentageRestoredPerKillDiminishing = config.Bind<float>("Item: " + ItemName, "Additional Shield Restoration Percentage per Additional BSS Stack (Diminishing)", 0.1f, "How much additional shield per kill should be granted with diminishing returns (hyperbolic scaling) on additional stacks? 0.1 = 10%").Value;
            MaximumPercentageShieldRestoredPerKill = config.Bind<float>("Item: " + ItemName, "Absolute Maximum Shield Restored per Kill", 0.5f, "What should our maximum percentage shield restored per kill be? 0.5 = 50%").Value;
            BaseGrantShieldMultiplier = config.Bind<float>("Item: " + ItemName, "Shield Granted on First BSS Stack", 0.08f, "How much should the starting shield be upon receiving the item? 0.08 = 8%").Value;
        }

        private void CreateMaterials()
        {
            
            var metalNormal = Resources.Load<Texture2D>("@Aetherium:Assets/Textures/Material Textures/BlasterSwordTextureNormal.png");

            var handleMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/BloodSoakedShield/BloodSoakedShieldHandle.mat");
            handleMaterial.shader = AetheriumPlugin.HopooShader;

            var shieldMainMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/BloodSoakedShield/BloodSoakedShieldMain.mat");
            shieldMainMaterial.shader = AetheriumPlugin.HopooShader;
            shieldMainMaterial.SetTexture("_MainTex", null);
            shieldMainMaterial.SetFloat("_Smoothness", 1f);
            shieldMainMaterial.SetTexture("_NormalTex", metalNormal);
            shieldMainMaterial.SetFloat("_NormalStrength", 2);

            var shieldOmegaMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/BloodSoakedShield/BloodSoakedShieldOmega.mat");
            shieldOmegaMaterial.shader = AetheriumPlugin.HopooShader;
            shieldOmegaMaterial.SetFloat("_Smoothness", 0.5f);
            shieldOmegaMaterial.SetColor("_EmColor", new Color(140, 87, 2));
            shieldOmegaMaterial.SetFloat("_EmPower", 0.00001f);

            var spikesMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/BloodSoakedShield/BloodSoakedShieldSpikes.mat");
            spikesMaterial.shader = AetheriumPlugin.HopooShader;
            spikesMaterial.SetTexture("_NormalTex", metalNormal);
            spikesMaterial.SetFloat("_NormalStrength", 5);
            spikesMaterial.SetFloat("_Smoothness", 0.5f);
            spikesMaterial.SetTexture("_EmTex", Resources.Load<Texture2D>("@Aetherium:Assets/Textures/Material Textures/13836-diffuse.jpg"));
            spikesMaterial.SetColor("_EmColor", new Color(255, 0, 0));
            spikesMaterial.SetFloat("_EmPower", 0.0001f);

        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = Resources.Load<GameObject>(ItemModelPath);
            ItemBodyModelPrefab.AddComponent<RoR2.ItemDisplay>();
            ItemBodyModelPrefab.GetComponent<RoR2.ItemDisplay>().rendererInfos = ItemDisplaySetup(ItemBodyModelPrefab);

            Vector3 generalScale = new Vector3(0.3f, 0.3f, 0.3f);
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0, 0.23f, -0.05f),
                    localAngles = new Vector3(0, -180, -90),
                    localScale = generalScale
                }
            });
            rules.Add("mdlHuntress", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "UpperArmR",
                    localPos = new Vector3(0, 0.2f, -0.05f),
                    localAngles = new Vector3(0, 180, -90),
                    localScale = new Vector3(0.2f, 0.2f, 0.2f)
                }
            });
            rules.Add("mdlToolbot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0, 0, 0.65f),
                    localAngles = new Vector3(0, 0, 270),
                    localScale = new Vector3(2, 2, 2)
                }
            });
            rules.Add("mdlEngi", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(-0.014f, 0.127f, -0.08f),
                    localAngles = new Vector3(0, 160, 180),
                    localScale = new Vector3(0.3f, 0.3f, 0.3f)
                }
            });
            rules.Add("mdlMage", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0f, 0.15f, 0.07f),
                    localAngles = new Vector3(0, 0, 180),
                    localScale = new Vector3(0.32f, 0.32f, 0.32f)
                }
            });
            rules.Add("mdlMerc", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.036f, 0.21f, -0.041f),
                    localAngles = new Vector3(350, 180, 90),
                    localScale = generalScale
                }
            });
            rules.Add("mdlTreebot", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "WeaponPlatform",
                    localPos = new Vector3(-0.16f, -0.1f, 0.1f),
                    localAngles = new Vector3(0, -90, -90),
                    localScale = new Vector3(0.5f, 0.5f, 0.5f)
                }
            });
            rules.Add("mdlLoader", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "MechLowerArmL",
                    localPos = new Vector3(0, 0.2f, -0.09f),
                    localAngles = new Vector3(0, 180, 90),
                    localScale = new Vector3(0.32f, 0.32f, 0.32f)
                }
            });
            rules.Add("mdlCroco", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmR",
                    localPos = new Vector3(0.7f, 3, 0.7f),
                    localAngles = new Vector3(0, 45, 270),
                    localScale = new Vector3(2, 2, 2)
                }
            });
            rules.Add("mdlCaptain", new RoR2.ItemDisplayRule[]
            {
                new RoR2.ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "LowerArmL",
                    localPos = new Vector3(-0.058f, 0.23f, 0),
                    localAngles = new Vector3(10, -90, 90),
                    localScale = new Vector3(0.3f, 0.3f, 0.3f)
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            GetStatCoefficients += GrantBaseShield;
            On.RoR2.GlobalEventManager.OnCharacterDeath += GrantShieldReward;
        }

        private void GrantBaseShield(CharacterBody sender, StatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                HealthComponent healthC = sender.GetComponent<HealthComponent>();
                args.baseShieldAdd += healthC.fullHealth * BaseGrantShieldMultiplier;
            }
        }

        private void GrantShieldReward(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, RoR2.GlobalEventManager self, RoR2.DamageReport damageReport)
        {
            if (damageReport?.attackerBody)
            {
                int inventoryCount = GetCount(damageReport.attackerBody);
                if (inventoryCount > 0)
                {
                    var percentage = ShieldPercentageRestoredPerKill + (MaximumPercentageShieldRestoredPerKill - MaximumPercentageShieldRestoredPerKill / (1 + AdditionalShieldPercentageRestoredPerKillDiminishing * (inventoryCount - 1)));
                    damageReport.attackerBody.healthComponent.RechargeShield(damageReport.attackerBody.healthComponent.fullShield * percentage);
                }
            }
            orig(self, damageReport);
        }
    }
}