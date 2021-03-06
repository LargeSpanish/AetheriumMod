﻿using Aetherium.Utils;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using static Aetherium.CoreModules.StatHooks;
using static Aetherium.Utils.ItemHelpers;
using static Aetherium.Utils.MathHelpers;

namespace Aetherium.Items
{
    public class ShieldingCore : ItemBase<ShieldingCore>
    {
        public static bool UseNewIcons;
        public static bool EnableParticleEffects;
        public static float BaseShieldingCoreArmorGrant;
        public static float AdditionalShieldingCoreArmorGrant;
        public static float BaseGrantShieldMultiplier;

        public override string ItemName => "Shielding Core";

        public override string ItemLangTokenName => "SHIELDING_CORE";

        public override string ItemPickupDesc => "While shielded, gain a temporary boost in <style=cIsUtility>armor</style>.";

        public override string ItemFullDescription => $"You gain <style=cIsUtility>{BaseShieldingCoreArmorGrant}</style> <style=cStack>(+{AdditionalShieldingCoreArmorGrant} per stack)</style> <style=cIsUtility>armor</style> while <style=cIsUtility>BLUE shields</style> are active." +
            $" The first stack of this item will grant <style=cIsUtility>{FloatToPercentageString(BaseGrantShieldMultiplier)}</style> of your max health as shield on pickup.";

        public override string ItemLore => OrderManifestLoreFormatter(

            ItemName,

            "7/4/2091",

            "UES Backlight/Sector 667/Outer Rim",

            "667********",

            ItemPickupDesc,

            "Light / Liquid-Seal / DO NOT DRINK FROM EXHAUST",

            "\nEngineer's report:\n\n" +
            "   Let me preface this with a bit of honesty, I do not know what the green goo inside my little turbine is. " +
            "I bought an aftermarket resonator from one of the junk dealers our ship passed, because I was running low on parts to repair our shield generators. " +
            "As soon as I slotted this thing in, I'm covered in this gross liquid that seems to dissipate into these sparkly crystals when exposed to air. " +
            "Normally this wouldn't be much of an issue since I'm in a suit, but the stuff was constantly attempting to fill the container it occupied so I had to create a seal for it. " +
            "That's when my suit diagnostics alarmed me that my shield's efficacy hit the roof.\n\n" +
            "Eureka moment, and a few design drafts later.\n" +
            "Now I'm selling these things like hotcakes and making a profit. So here's one for you.\n\n" +
            "P.S. Don't expose your skin to this stuff, it may cause over 200 known forms of cancer. That's our secret though, right?");

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override string ItemModelPath => "@Aetherium:Assets/Models/Prefabs/Item/ShieldingCore/ShieldingCore.prefab";
        public override string ItemIconPath => UseNewIcons ? "@Aetherium:Assets/Textures/Icons/Item/ShieldingCoreIconAlt.png" : "@Aetherium:Assets/Textures/Icons/Item/shieldingCoreIcon.png";

        public static GameObject ItemBodyModelPrefab;

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
            EnableParticleEffects = config.Bind<bool>("Item: " + ItemName, "Enable Particle Effects", true, "Should the particle effects for the models be enabled?").Value;
            BaseShieldingCoreArmorGrant = config.Bind<float>("Item: " + ItemName, "First Shielding Core Bonus to Armor", 15f, "How much armor should the first Shielding Core grant?").Value;
            AdditionalShieldingCoreArmorGrant = config.Bind<float>("Item: " + ItemName, "Additional Shielding Cores Bonus to Armor", 10f, "How much armor should each additional Shielding Core grant?").Value;
            BaseGrantShieldMultiplier = config.Bind<float>("Item: " + ItemName, "First Shielding Core Bonus to Max Shield", 0.08f, "How much should the starting shield be upon receiving the item?").Value;
        }

        private void CreateMaterials()
        {
            var metalNormal = Resources.Load<Texture2D>("@Aetherium:Assets/Textures/Material Textures/BlasterSwordTextureNormal.png");

            var coreFlapsMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/ShieldingCore/ShieldingCoreFlap.mat");
            coreFlapsMaterial.shader = AetheriumPlugin.HopooShader;
            coreFlapsMaterial.SetFloat("_Smoothness", 0.5f);
            coreFlapsMaterial.SetFloat("_SpecularStrength", 1);
            coreFlapsMaterial.SetFloat("_SpecularExponent", 10);
            coreFlapsMaterial.SetFloat("_ForceSpecOn", 1);

            var coreGemMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/ShieldingCore/ShieldingCoreGem.mat");
            coreGemMaterial.shader = AetheriumPlugin.HopooShader;
            coreGemMaterial.SetColor("_EmColor", new Color(59, 0, 79));
            coreGemMaterial.SetFloat("_EmPower", 0.00001f);
            coreGemMaterial.SetFloat("_Smoothness", 0.83f);

            var coreRivetsMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/ShieldingCore/ShieldingCoreRivets.mat");
            coreRivetsMaterial.shader = AetheriumPlugin.HopooShader;
            coreRivetsMaterial.SetFloat("_Smoothness", 1f);
            coreRivetsMaterial.SetTexture("_NormalTex", metalNormal);
            coreRivetsMaterial.SetFloat("_NormalStrength", 5f);
            coreRivetsMaterial.SetFloat("_SpecularStrength", 1);
            coreRivetsMaterial.SetFloat("_SpecularExponent", 10);
            coreRivetsMaterial.SetFloat("_ForceSpecOn", 1);

            var coreContainerMaterial = Resources.Load<Material>("@Aetherium:Assets/Textures/Materials/Item/ShieldingCore/ShieldingMetal.mat");
            coreContainerMaterial.shader = AetheriumPlugin.HopooShader;
            coreContainerMaterial.SetFloat("_Smoothness", 1f);
        }

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemBodyModelPrefab = Resources.Load<GameObject>(ItemModelPath);
            ItemBodyModelPrefab.AddComponent<ItemDisplay>();
            ItemBodyModelPrefab.GetComponent<ItemDisplay>().rendererInfos = ItemHelpers.ItemDisplaySetup(ItemBodyModelPrefab);
            if (EnableParticleEffects) { ItemBodyModelPrefab.AddComponent<ShieldingCoreVisualCueController>(); }

            Vector3 generalScale = new Vector3(0.2f, 0.2f, 0.2f);
            ItemDisplayRuleDict rules = new ItemDisplayRuleDict(new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.2f, -0.22f),
                    localAngles = new Vector3(180f, 0f, 0f),
                    localScale = new Vector3(0.17f, 0.17f, 0.17f)
                }
            });
            rules.Add("mdlHuntress", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0.05f, 0.14f, -0.12f),
                    localAngles = new Vector3(0f, 160f, -20f),
                    localScale = new Vector3(0.14f, 0.14f, 0.14f)
                }
            });
            rules.Add("mdlToolbot", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 2.5f, -2.5f),
                    localAngles = new Vector3(0f, -180f, 0f),
                    localScale = new Vector3(1.5f, 1.5f, 1.5f)
                }
            });
            rules.Add("mdlEngi", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.22f, -0.3f),
                    localAngles = new Vector3(0f, 180, 180f),
                    localScale = generalScale
                }
            });
            rules.Add("mdlMage", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.1f, -0.35f),
                    localAngles = new Vector3(-10f, 180f, 180f),
                    localScale = new Vector3(0.14f, 0.14f, 0.14f)
                }
            });
            rules.Add("mdlMerc", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.19f, -0.32f),
                    localAngles = new Vector3(-15f, -180f, 180f),
                    localScale = new Vector3(0.17f, 0.17f, 0.17f)
                }
            });
            rules.Add("mdlTreebot", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "FlowerBase",
                    localPos = new Vector3(0f, 0.9f, 0f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.7f, 0.7f, 0.7f)
                }
            });
            rules.Add("mdlLoader", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.25f, -0.4f),
                    localAngles = new Vector3(-10f, 180, 0f),
                    localScale = generalScale
                }
            });
            rules.Add("mdlCroco", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 3.5f, 3.5f),
                    localAngles = new Vector3(-45f, 0f, 0f),
                    localScale = new Vector3(2, 2, 2)
                }
            });
            rules.Add("mdlCaptain", new ItemDisplayRule[]
            {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chest",
                    localPos = new Vector3(0f, 0.2f, -0.25f),
                    localAngles = new Vector3(0f, -180f, 0f),
                    localScale = generalScale
                }
            });
            return rules;
        }

        public override void Hooks()
        {
            GetStatCoefficients += GrantBaseShield;
            On.RoR2.CharacterBody.FixedUpdate += ShieldedCoreValidator;
            GetStatCoefficients += ShieldedCoreArmorCalc;
        }

        private void GrantBaseShield(CharacterBody sender, StatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                HealthComponent healthC = sender.GetComponent<HealthComponent>();
                args.baseShieldAdd += healthC.fullHealth * BaseGrantShieldMultiplier;
            }
        }

        //private void GrantBaseShield(ILContext il)
        //{
        //    //Provided by Harb from their HarbCrate mod. Thanks Harb!
        //    ILCursor c = new ILCursor(il);
        //    int shieldsLoc = 33;
        //    c.GotoNext(
        //        MoveType.Before,
        //        x => x.MatchLdloc(out shieldsLoc),
        //        x => x.MatchCallvirt<CharacterBody>("set_maxShield")
        //    );
        //    c.Emit(OpCodes.Ldloc, shieldsLoc);
        //    c.EmitDelegate<Func<CharacterBody, float, float>>((self, shields) =>
        //    {
        //        var InventoryCount = GetCount(self);
        //        if (InventoryCount > 0)
        //        {
        //            shields += self.maxHealth * 0.04f;
        //        }
        //        return shields;
        //    });
        //    c.Emit(OpCodes.Stloc, shieldsLoc);
        //    c.Emit(OpCodes.Ldarg_0);
        //}

        private void ShieldedCoreValidator(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            orig(self);

            var shieldComponent = self.GetComponent<ShieldedCoreComponent>();
            if (!shieldComponent) { shieldComponent = self.gameObject.AddComponent<ShieldedCoreComponent>(); }

            var newInventoryCount = GetCount(self);
            var IsShielded = self.healthComponent.shield > 0;

            bool IsDifferent = false;
            if (shieldComponent.cachedInventoryCount != newInventoryCount)
            {
                IsDifferent = true;
                shieldComponent.cachedInventoryCount = newInventoryCount;
            }
            if (shieldComponent.cachedIsShielded != IsShielded)
            {
                IsDifferent = true;
                shieldComponent.cachedIsShielded = IsShielded;
            }

            if (!IsDifferent) return;

            self.statsDirty = true;
        }

        private void ShieldedCoreArmorCalc(CharacterBody sender, StatHookEventArgs args)
        {
            var ShieldedCoreComponent = sender.GetComponent<ShieldedCoreComponent>();
            if (ShieldedCoreComponent && ShieldedCoreComponent.cachedIsShielded && ShieldedCoreComponent.cachedInventoryCount > 0)
            {
                args.armorAdd += BaseShieldingCoreArmorGrant + (AdditionalShieldingCoreArmorGrant * (ShieldedCoreComponent.cachedInventoryCount - 1));
            }
        }

        public class ShieldedCoreComponent : MonoBehaviour
        {
            public int cachedInventoryCount = 0;
            public bool cachedIsShielded = false;
        }

        public class ShieldingCoreVisualCueController : MonoBehaviour
        {
            public ItemDisplay ItemDisplay;
            public ParticleSystem[] ParticleSystem;
            public CharacterMaster OwnerMaster;
            public CharacterBody OwnerBody;
            public void FixedUpdate()
            {

                if (!OwnerMaster || !ItemDisplay || ParticleSystem.Length != 3)
                {
                    ItemDisplay = this.GetComponentInParent<ItemDisplay>();
                    if (ItemDisplay)
                    {
                        ParticleSystem = ItemDisplay.GetComponentsInChildren<ParticleSystem>();
                        //Debug.Log("Found ItemDisplay: " + itemDisplay);
                        var characterModel = ItemDisplay.GetComponentInParent<CharacterModel>();

                        if (characterModel)
                        {
                            var body = characterModel.body;
                            if (body)
                            {
                                OwnerMaster = body.master;
                            }
                        }
                    }
                }

                if (OwnerMaster && !OwnerBody)
                {
                    var body = OwnerMaster.GetBody();
                    if (body)
                    {
                        OwnerBody = body;
                    }
                    if (!body)
                    {
                        if (ParticleSystem.Length == 3)
                        {
                            for(int i = 0; i < ParticleSystem.Length; i++)
                            {
                                UnityEngine.Object.Destroy(ParticleSystem[i]);
                            }
                        }
                        UnityEngine.Object.Destroy(this);
                    }
                }

                if (OwnerBody && ParticleSystem.Length == 3)
                {
                    foreach (ParticleSystem particleSystem in ParticleSystem)
                    {
                        if (OwnerBody.healthComponent.shield > 0)
                        {
                            if (!particleSystem.isPlaying && ItemDisplay.visibilityLevel != VisibilityLevel.Invisible)
                            {
                                particleSystem.Play();
                            }
                            else
                            {
                                if (particleSystem.isPlaying && ItemDisplay.visibilityLevel == VisibilityLevel.Invisible)
                                {
                                    particleSystem.Stop();
                                    particleSystem.Clear();
                                }
                            }
                        }
                        else
                        {
                            if (particleSystem.isPlaying)
                            {
                                particleSystem.Stop();
                            }
                        }
                    }
                }
            }
        }
    }
}