﻿using AlternativeTextures.Framework.Models;
using StardewModdingAPI;
using System;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;

namespace AlternativeTextures.Framework.Interfaces.API
{
    public interface IApi
    {
        void AddAlternativeTexture(AlternativeTextureModel model, string owner, List<Texture2D> textures);
    }

    public class Api : IApi
    {
        private readonly AlternativeTextures _framework;

        public Api(AlternativeTextures alternativeTexturesMod)
        {
            _framework = alternativeTexturesMod;
        }

        public void AddAlternativeTexture(AlternativeTextureModel model, string owner, Texture2D texture)
        {
            AddAlternativeTexture(model, owner, new List<Texture2D>() { texture });
        }

        public void AddAlternativeTexture(AlternativeTextureModel model, string owner, List<Texture2D> textures)
        {
            if (String.IsNullOrEmpty(owner))
            {
                _framework.Monitor.Log($"Unable to add AlternativeTextureModel {model.GetNameWithSeason()}: Owner property is not set.");
                return;
            }

            if (textures.Count() == 0)
            {
                _framework.Monitor.Log($"Unable to add AlternativeTextureModel {model.GetNameWithSeason()}: Textures property is empty.");
                return;
            }

            model.Owner = owner;
            model.Type = model.GetTextureType();

            var seasons = model.Seasons;
            for (int s = 0; s < 4; s++)
            {
                if ((seasons.Count() == 0 && s > 0) || (seasons.Count() > 0 && s >= seasons.Count()))
                {
                    continue;
                }

                // Parse the model and assign it the content pack's owner
                AlternativeTextureModel textureModel = model.ShallowCopy();

                // Override Grass Alternative Texture pack ItemNames to always be Grass, in order to be compatible with translations 
                textureModel.ItemName = textureModel.GetTextureType() == "Grass" ? "Grass" : textureModel.ItemName;

                // Add the UniqueId to the top-level Keywords
                textureModel.Keywords.Add(model.Owner);

                // Add the top-level Keywords to any ManualVariations.Keywords
                foreach (var variation in textureModel.ManualVariations)
                {
                    variation.Keywords.AddRange(textureModel.Keywords);
                }

                // Set the season (if any)
                textureModel.Season = seasons.Count() == 0 ? String.Empty : seasons[s];

                // Set the ModelName and TextureId
                textureModel.ModelName = String.IsNullOrEmpty(textureModel.Season) ? String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName) : String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName, "_", textureModel.Season);
                textureModel.TextureId = String.Concat(textureModel.Owner, ".", textureModel.ModelName);

                // Verify we are given a singular texture, if not then stitch them all together
                if (textures.Count() > 1)
                {
                    // Load in the first texture_#.png to get its dimensions for creating stitchedTexture
                    Texture2D baseTexture = textures.First();
                    Texture2D stitchedTexture = new Texture2D(Game1.graphics.GraphicsDevice, baseTexture.Width, baseTexture.Height * textures.Count());

                    // Now stitch together the split textures into a single texture
                    Color[] pixels = new Color[stitchedTexture.Width * stitchedTexture.Height];
                    for (int x = 0; x < textures.Count(); x++)
                    {
                        _framework.Monitor.Log($"Stitching together {textureModel.TextureId}: texture_{x}", LogLevel.Trace);

                        var offset = x * baseTexture.Width * baseTexture.Height;
                        var subTexture = textures.ElementAt(x);

                        Color[] subPixels = new Color[subTexture.Width * subTexture.Height];
                        subTexture.GetData(subPixels);
                        for (int i = 0; i < subPixels.Length; i++)
                        {
                            pixels[i + offset] = subPixels[i];
                        }
                    }

                    stitchedTexture.SetData(pixels);
                    textureModel.TileSheetPath = String.Empty;
                    textureModel.Texture = stitchedTexture;
                }
                else
                {
                    // Load in the single vertical texture
                    textureModel.TileSheetPath = String.Empty;
                    textureModel.Texture = textures.First();
                }

                // Track the texture model
                AlternativeTextures.textureManager.AddAlternativeTexture(textureModel);

                // Log it
                _framework.Monitor.Log(textureModel.ToString(), LogLevel.Trace);
            }
        }
    }
}