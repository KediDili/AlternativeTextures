﻿using AlternativeTextures;
using AlternativeTextures.Framework.Models;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.StandardObjects
{
    internal class GiantCropPatch : PatchTemplate
    {
        private readonly Type _object = typeof(GiantCrop);

        internal GiantCropPatch(IMonitor modMonitor) : base(modMonitor)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(GiantCrop.draw), new[] { typeof(SpriteBatch), typeof(Vector2) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
            harmony.Patch(AccessTools.Constructor(typeof(GiantCrop), new[] { typeof(int), typeof(Vector2) }), postfix: new HarmonyMethod(GetType(), nameof(GiantCropPostfix)));
        }

        private static bool DrawPrefix(GiantCrop __instance, float ___shakeTimer, SpriteBatch spriteBatch, Vector2 tileLocation)
        {
            if (__instance.modData.ContainsKey("AlternativeTextureName"))
            {
                var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(__instance.modData["AlternativeTextureName"]);
                if (textureModel is null)
                {
                    return true;
                }

                var textureVariation = Int32.Parse(__instance.modData["AlternativeTextureVariation"]);
                if (textureVariation == -1)
                {
                    return true;
                }

                var textureOffset = textureVariation * textureModel.TextureHeight;
                spriteBatch.Draw(textureModel.Texture, Game1.GlobalToLocal(Game1.viewport, tileLocation * 64f - new Vector2((___shakeTimer > 0f) ? ((float)Math.Sin(Math.PI * 2.0 / (double)___shakeTimer) * 2f) : 0f, 64f)), new Rectangle(0, textureOffset, 48, 63), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y + 2f) * 64f / 10000f);

                return false;
            }

            return true;
        }

        private static void GiantCropPostfix(GiantCrop __instance)
        {
            var harvestName = Game1.objectInformation.ContainsKey(__instance.parentSheetIndex) ? Game1.objectInformation[__instance.parentSheetIndex].Split('/')[0] : String.Empty;
            harvestName = $"{AlternativeTextureModel.TextureType.GiantCrop}_{harvestName}";

            if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(harvestName))
            {
                AssignModData(__instance, harvestName, false);
            }
        }
    }
}