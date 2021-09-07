﻿using AlternativeTextures;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.UI;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.Tools
{
    internal class ToolPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Tool);

        internal ToolPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.beginUsing), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(BeginUsingPrefix)));
        }

        private static bool DrawInMenuPrefix(Tool __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetPaintBucketTexture(), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetScissorsTexture(), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                var scale = __instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_SCALE) ? float.Parse(__instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE]) : 0f;
                var texture = AlternativeTextures.assetManager.GetPaintBrushEmptyTexture();
                if (!String.IsNullOrEmpty(__instance.modData[AlternativeTextures.PAINT_BRUSH_FLAG]))
                {
                    texture = AlternativeTextures.assetManager.GetPaintBrushFilledTexture();
                }
                spriteBatch.Draw(texture, location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * (scaleSize + scale), SpriteEffects.None, layerDepth);

                if (scale > 0f)
                {
                    __instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE] = (scale -= 0.01f).ToString();
                }
                return false;
            }

            return true;
        }

        private static bool BeginUsingPrefix(Tool __instance, ref bool __result, GameLocation location, int x, int y, Farmer who)
        {
            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                __result = true;
                return UsePaintBucket(location, x, y, who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                __result = true;
                return UseScissors(location, x, y, who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                __result = true;
                return CancelUsing(who);
            }

            return true;
        }

        private static bool UsePaintBucket(GameLocation location, int x, int y, Farmer who)
        {
            if (location is Farm farm)
            {
                var targetedBuilding = farm.getBuildingAt(new Vector2(x / 64, y / 64));
                if (targetedBuilding != null)
                {
                    // Assign default data if none exists
                    if (!targetedBuilding.modData.ContainsKey("AlternativeTextureName"))
                    {
                        var modelType = AlternativeTextureModel.TextureType.Building;
                        var instanceSeasonName = $"{modelType}_{targetedBuilding.buildingType}_{Game1.currentSeason}";
                        AssignDefaultModData(targetedBuilding, instanceSeasonName, true);
                    }

                    var modelName = targetedBuilding.modData["AlternativeTextureName"].Replace($"{targetedBuilding.modData["AlternativeTextureOwner"]}.", String.Empty);
                    if (targetedBuilding.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(targetedBuilding.modData["AlternativeTextureSeason"]))
                    {
                        modelName = modelName.Replace($"_{targetedBuilding.modData["AlternativeTextureSeason"]}", String.Empty);
                    }

                    if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                    {
                        Game1.addHUDMessage(new HUDMessage($"{modelName} has no alternative textures for this season!", 3));
                        return CancelUsing(who);
                    }

                    // Display texture menu
                    var buildingObj = new Object(100, 1, isRecipe: false, -1)
                    {
                        TileLocation = new Vector2(targetedBuilding.tileX, targetedBuilding.tileY),
                        modData = targetedBuilding.modData
                    };
                    Game1.activeClickableMenu = new PaintBucketMenu(buildingObj, GetTextureType(targetedBuilding), modelName, textureTileWidth: targetedBuilding.tilesWide);

                    return CancelUsing(who);
                }
            }

            var targetedObject = GetObjectAt(location, x, y);
            if (targetedObject != null)
            {
                // Assign default data if none exists
                if (!targetedObject.modData.ContainsKey("AlternativeTextureName"))
                {
                    var instanceSeasonName = $"{GetTextureType(targetedObject)}_{GetObjectName(targetedObject)}_{Game1.currentSeason}";
                    AssignDefaultModData(targetedObject, instanceSeasonName, true);
                }

                var modelName = targetedObject.modData["AlternativeTextureName"].Replace($"{targetedObject.modData["AlternativeTextureOwner"]}.", String.Empty);
                if (targetedObject.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(targetedObject.modData["AlternativeTextureSeason"]))
                {
                    modelName = modelName.Replace($"_{targetedObject.modData["AlternativeTextureSeason"]}", String.Empty);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage($"{modelName} has no alternative textures for this season!", 3));
                    return CancelUsing(who);
                }

                // Display texture menu
                Game1.activeClickableMenu = new PaintBucketMenu(targetedObject, GetTextureType(targetedObject), modelName);

                return CancelUsing(who);
            }

            var targetedTerrain = GetTerrainFeatureAt(location, x, y);
            if (targetedTerrain != null)
            {
                if (targetedTerrain is HoeDirt || targetedTerrain is GiantCrop || targetedTerrain is Grass)
                {
                    Game1.addHUDMessage(new HUDMessage($"You can't put paint on that!", 3));
                    return CancelUsing(who);
                }

                if (!targetedTerrain.modData.ContainsKey("AlternativeTextureName"))
                {
                    if (targetedTerrain is Flooring flooring)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Flooring}_{GetFlooringName(flooring)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is Tree tree)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Tree}_{GetTreeTypeString(tree)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is FruitTree fruitTree)
                    {
                        Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>("Data\\fruitTrees");
                        var saplingIndex = data.FirstOrDefault(d => int.Parse(d.Value.Split('/')[0]) == fruitTree.treeType).Key;
                        var saplingName = Game1.objectInformation.ContainsKey(saplingIndex) ? Game1.objectInformation[saplingIndex].Split('/')[0] : String.Empty;

                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.FruitTree}_{saplingName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else
                    {
                        return CancelUsing(who);
                    }
                }

                var modelName = targetedTerrain.modData["AlternativeTextureName"].Replace($"{targetedTerrain.modData["AlternativeTextureOwner"]}.", String.Empty);
                if (targetedTerrain.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(targetedTerrain.modData["AlternativeTextureSeason"]))
                {
                    modelName = modelName.Replace($"_{targetedTerrain.modData["AlternativeTextureSeason"]}", String.Empty);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage($"{modelName} has no alternative textures for this season!", 3));
                    return CancelUsing(who);
                }

                // Display texture menu
                var terrainObj = new Object(100, 1, isRecipe: false, -1)
                {
                    TileLocation = targetedTerrain.currentTileLocation,
                    modData = targetedTerrain.modData
                };
                Game1.activeClickableMenu = new PaintBucketMenu(terrainObj, GetTextureType(targetedTerrain), modelName);

                return CancelUsing(who);
            }

            return CancelUsing(who);
        }

        private static bool UseScissors(GameLocation location, int x, int y, Farmer who)
        {
            var character = GetCharacterAt(location, x, y);
            if (character != null)
            {
                // Assign default data if none exists
                if (!character.modData.ContainsKey("AlternativeTextureName"))
                {
                    var modelType = AlternativeTextureModel.TextureType.Character;
                    var instanceSeasonName = $"{modelType}_{GetCharacterName(character)}_{Game1.currentSeason}";
                    AssignDefaultModData(character, instanceSeasonName, true);
                }

                var modelName = character.modData["AlternativeTextureName"].Replace($"{character.modData["AlternativeTextureOwner"]}.", String.Empty);
                if (character.modData.ContainsKey("AlternativeTextureSeason") && !String.IsNullOrEmpty(character.modData["AlternativeTextureSeason"]))
                {
                    modelName = modelName.Replace($"_{character.modData["AlternativeTextureSeason"]}", String.Empty);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage($"{modelName} has no alternative textures for this season!", 3));
                    return CancelUsing(who);
                }

                // Display texture menu
                var obj = new Object(100, 1, isRecipe: false, -1)
                {
                    Name = character.Name,
                    displayName = character.displayName,
                    TileLocation = character.getTileLocation(),
                    modData = character.modData
                };
                Game1.activeClickableMenu = new PaintBucketMenu(obj, GetTextureType(character), modelName, uiTitle: "Scissors");

                return CancelUsing(who);
            }
            return CancelUsing(who);
        }

        private static bool CancelUsing(Farmer who)
        {
            who.CanMove = true;
            who.UsingTool = false;
            return false;
        }
    }
}
