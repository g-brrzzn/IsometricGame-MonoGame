using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;
namespace IsometricGame
{
    public class AssetManager
    {
        public Dictionary<string, Texture2D> Images { get; private set; } = new Dictionary<string, Texture2D>();
        public Dictionary<string, SoundEffect> Sounds { get; private set; } = new Dictionary<string, SoundEffect>();
        public Dictionary<string, SpriteFont> Fonts { get; private set; } = new Dictionary<string, SpriteFont>();
        public Song Music { get; private set; }
        private Texture2D CreateRectangleTexture(GraphicsDevice device, int width, int height, Color color)
        {
            var texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = color;
            }
            texture.SetData(data);
            return texture;
        }
        private Texture2D CreateDiamondTexture(GraphicsDevice device, int width, int height, Color color)
        {
            var texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];
            float midX = (width - 1) / 2.0f;
            float midY = (height - 1) / 2.0f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dx = Math.Abs(x - midX);
                    float dy = Math.Abs(y - midY);
                    if ((dx / (width / 2.0f)) + (dy / (height / 2.0f)) <= 1.0f)
                    {
                        data[y * width + x] = color;
                    }
                    else
                    {
                        data[y * width + x] = Color.Transparent;
                    }
                }
            }
            texture.SetData(data);
            return texture;
        }

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            var playerSprite = CreateDiamondTexture(graphicsDevice, 16, 32, Constants.PlayerColorGreen);
            Images["player_idle_south"] = playerSprite;
            Images["player_idle_west"] = playerSprite;
            Images["player_idle_north"] = playerSprite;
            Images["player_idle_east"] = playerSprite;

            Images["icon"] = playerSprite;
            Images["bullet_player"] = CreateRectangleTexture(graphicsDevice, 8, 8, Color.Yellow);
            Images["bullet_enemy"] = CreateRectangleTexture(graphicsDevice, 8, 8, Color.Magenta);

            var enemySprite = CreateDiamondTexture(graphicsDevice, 16, 32, Color.DarkRed);
            Images["enemy1_idle_south"] = enemySprite;
            Images["enemy1_idle_west"] = enemySprite;
            Images["enemy1_idle_north"] = enemySprite;
            Images["enemy1_idle_east"] = enemySprite;

            // --- CARREGAMENTO DE TILES (LIMPO) ---
            // (Certifique-se que todos estes arquivos existem no seu Content.mgcb)

            // Grama
            Images["tile_grass1"] = content.Load<Texture2D>("sprites/tiles/grass_tile1");
            Images["tile_grass2"] = content.Load<Texture2D>("sprites/tiles/grass_tile2");
            Images["tile_grass3"] = content.Load<Texture2D>("sprites/tiles/grass_tile3");

            // Terra
            Images["tile_dirt1"] = content.Load<Texture2D>("sprites/tiles/dirt_tile1");
            Images["tile_dirt2"] = content.Load<Texture2D>("sprites/tiles/dirt_tile2");
            Images["tile_dirt3"] = content.Load<Texture2D>("sprites/tiles/dirt_tile3");

            // Água
            Images["water_tile1"] = content.Load<Texture2D>("sprites/tiles/water_tile1");
            Images["water_tile2"] = content.Load<Texture2D>("sprites/tiles/water_tile2");
            Images["water_tile3"] = content.Load<Texture2D>("sprites/tiles/water_tile3");

            // Rocha
            Images["rock_tile1"] = content.Load<Texture2D>("sprites/tiles/rock_tile1");
            Images["rock_tile2"] = content.Load<Texture2D>("sprites/tiles/rock_tile2");
            Images["rock_tile3"] = content.Load<Texture2D>("sprites/tiles/rock_tile3");

            // Parede (Mapeado para rock_tile3 conforme seu código anterior)
            Images["tile_wall"] = content.Load<Texture2D>("sprites/tiles/rock_tile3");
            // --- FIM DO CARREGAMENTO DE TILES ---

            Sounds["shoot"] = content.Load<SoundEffect>("sound/shoot");
            Sounds["hit"] = content.Load<SoundEffect>("sound/hit");
            Sounds["menu_select"] = content.Load<SoundEffect>("sound/impactMetal_002");
            Sounds["menu_confirm"] = content.Load<SoundEffect>("sound/forceField_001");

            Music = content.Load<Song>("sound/victory");

            Fonts["captain_32"] = content.Load<SpriteFont>("Captain32");
            Fonts["captain_42"] = content.Load<SpriteFont>("Captain42");
            Fonts["captain_80"] = content.Load<SpriteFont>("Captain80");
        }
    }
}