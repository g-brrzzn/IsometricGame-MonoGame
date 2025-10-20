using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using IsometricGame.Classes;
using System.Collections.Generic;
using System.Linq;

namespace IsometricGame
{
    public class MapLoader
    {
        public void LoadMap(string filePath)
        {
            if (!File.Exists(filePath))
            {
                System.Diagnostics.Debug.WriteLine($"Erro: Arquivo de mapa não encontrado em {filePath}");
                return;
            }

            string jsonContent = File.ReadAllText(filePath);
            MapData mapData = JsonConvert.DeserializeObject<MapData>(jsonContent);

            if (mapData == null)
            {
                System.Diagnostics.Debug.WriteLine($"Erro: Falha ao desserializar o mapa {filePath}");
                return;
            }

            // Mapeia ID para a entrada COMPLETA (para obtermos o assetName)
            Dictionary<int, TileMappingEntry> tileLookup = mapData.TileMapping
                                                              .ToDictionary(entry => entry.Id, entry => entry);

            foreach (var layer in mapData.Layers)
            {
                for (int i = 0; i < layer.Data.Count; i++)
                {
                    int tileId = layer.Data[i];
                    if (tileId == 0) continue;

                    int x = i % mapData.Width;
                    int y = i / mapData.Width;

                    if (tileLookup.TryGetValue(tileId, out TileMappingEntry tileInfo))
                    {
                        if (GameEngine.Assets.Images.TryGetValue(tileInfo.AssetName, out Texture2D texture))
                        {
                            Vector3 worldPos = new Vector3(x, y, layer.ZLevel);
                            var tileSprite = new Sprite(texture, worldPos);

                            // --- INÍCIO DA LÓGICA DE COLISÃO CORRETA ---
                            bool isSolid = false;
                            if (layer.ZLevel > 0)
                            {
                                // REGRA 1: Todo tile acima do chão (Z > 0) é sólido.
                                isSolid = true;
                            }
                            else // ZLevel é 0
                            {
                                // REGRA 2: No chão, APENAS água é sólida.
                                if (tileInfo.AssetName.Contains("water_"))
                                {
                                    isSolid = true;
                                }
                            }

                            if (isSolid)
                            {
                                // Registra a posição do tile sólido
                                GameEngine.SolidTiles[worldPos] = tileSprite;
                            }
                            // --- FIM DA LÓGICA DE COLISÃO ---

                            GameEngine.AllSprites.Add(tileSprite);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Aviso: Textura '{tileInfo.AssetName}' (ID: {tileId}) não encontrada no AssetManager.");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Aviso: Tile ID {tileId} não encontrado no tileMapping.");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine($"Mapa {filePath} carregado com sucesso. {GameEngine.SolidTiles.Count} tiles sólidos registrados.");
        }
    }
}