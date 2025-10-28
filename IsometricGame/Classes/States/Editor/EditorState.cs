
using IsometricGame.Classes;
using IsometricGame.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace IsometricGame.States.Editor
{
    public enum EditorMode
    {
        Tiles,
        Triggers
    }

    public class EditorState : GameStateBase
    {
        private MapData _currentMapData;
        private string _currentMapFileName = "new_map.json";
        private EditorMode _currentMode = EditorMode.Tiles;
        private Vector3 _cursorWorldPos = Vector3.Zero;
        private int _currentZLevel = 0;
        private bool _isDirty = false;
        private int _changesMade = 0;
        private const int AUTO_SAVE_THRESHOLD = 5;
        private Dictionary<Vector3, Sprite> _mapSprites = new Dictionary<Vector3, Sprite>();
        private List<TileMappingEntry> _tilePalette;
        private int _paletteIndex = 0;
        private int _selectedTileId = 0;
        private TileMappingEntry _selectedTileInfo = null;
        private MapTrigger _selectedTrigger = null;
        private EditorInputHandler _inputHandler;
        private EditorRenderer _renderer;
        private SpriteFont _font;
        private Texture2D _pixelTexture;
        private Texture2D _triggerIconTexture;
        private Action<string> _onSavePromptComplete;
        private Action<string> _onNewMapPromptComplete;
        private Action<string> _onAddTriggerIdComplete;

        public EditorState()
        {
            _inputHandler = new EditorInputHandler(this);
            InitializeInputCallbacks();
        }

        public override void Start()
        {
            base.Start();
            if (_renderer == null)
            {
                _font = GameEngine.Assets.Fonts["captain_32"];
                Game1.Instance.IsMouseVisible = false;
                Game1.Camera.SetZoom(1.5f);

                if (!GameEngine.Assets.Images.TryGetValue("pixel", out _pixelTexture))
                {
                    _pixelTexture = new Texture2D(Game1._graphicsManagerInstance.GraphicsDevice, 1, 1);
                    _pixelTexture.SetData(new[] { Color.White });
                    GameEngine.Assets.Images["pixel"] = _pixelTexture;
                    Debug.WriteLine("Warning (Editor): Texture 'pixel' not found. Created fallback.");
                }
                if (_triggerIconTexture == null) { _triggerIconTexture = _pixelTexture; }

                _renderer = new EditorRenderer(_font, _pixelTexture, _triggerIconTexture);

                _currentMode = EditorMode.Tiles;
                _selectedTrigger = null;
                _currentZLevel = 0;

                InitializePalette();
                UpdateSelectedTileInfo();
                LoadMap(_currentMapFileName);            }
        }

        public override void End()
        {
            Game1.Instance.IsMouseVisible = true;
        }
        public MapData GetCurrentMapData() => _currentMapData;
        public string GetCurrentMapFileName() => _currentMapFileName;
        public EditorMode GetCurrentMode() => _currentMode;
        public Vector3 GetCursorWorldPos() => _cursorWorldPos;
        public int GetCurrentZLevel() => _currentZLevel;
        public Dictionary<Vector3, Sprite> GetMapSprites() => _mapSprites;
        public TileMappingEntry GetSelectedTileInfo() => _selectedTileInfo;
        public int GetSelectedTileId() => _selectedTileId;
        public MapTrigger GetSelectedTrigger() => _selectedTrigger;
        public List<MapTrigger> GetCurrentMapTriggers() => _currentMapData?.Triggers;

        public override void Update(GameTime gameTime, InputManager input)
        {
            _inputHandler.HandleInput(input, gameTime);
            if (IsDone) return;
            UpdateCursorPosition(input);
        }

        private void UpdateCursorPosition(InputManager input)
        {
            Vector2 mouseScreenPos = input.InternalMousePosition;
            Vector2 mouseCameraWorld = Game1.Camera.ScreenToWorld(mouseScreenPos);
            Vector2 mouseIsoWorld = IsoMath.ScreenToWorld(mouseCameraWorld);
            _cursorWorldPos = new Vector3(MathF.Round(mouseIsoWorld.X), MathF.Round(mouseIsoWorld.Y), _currentZLevel);
        }

        public void DrawWorld(SpriteBatch spriteBatch)
        {
            _renderer?.DrawEditorWorld(spriteBatch, this);
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            _renderer?.DrawEditorUI(spriteBatch, this);
        }
        private void InitializeInputCallbacks()
        {
            _onSavePromptComplete = (fileName) =>
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    _currentMapFileName = fileName;
                    Debug.WriteLine($"Nome do arquivo definido como: {fileName}");
                    SaveMap(_currentMapFileName);
                }
                else { Debug.WriteLine("Salvamento cancelado: nome de arquivo vazio."); }
            };
            _onNewMapPromptComplete = (fileName) =>
            {
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    if (_isDirty)
                    {
                        Debug.WriteLine($"Salvando mapa anterior '{_currentMapFileName}' antes de criar novo...");
                        SaveMap(_currentMapFileName);                    }
                    _currentMapFileName = fileName;
                    CreateNewMap();                }
                else { Debug.WriteLine("Criação de novo mapa cancelada: nome vazio."); }
            };
            _onAddTriggerIdComplete = (id) =>
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    AddTriggerAt(_cursorWorldPos, id);
                }
                else { Debug.WriteLine("Adição de trigger cancelada: ID vazio."); }
            };
        }

        public void RequestExit()
        {
            if (_isDirty)
            {
                Debug.WriteLine("Salvando alterações pendentes antes de sair...");
                SaveMap(_currentMapFileName);
            }
            IsDone = true;
            NextState = "Menu";
        }

        public void RequestSaveMapWithPrompt()
        {
            GameEngine.OnTextInputComplete = _onSavePromptComplete;
            GameEngine.TextInputPrompt = "Salvar Mapa Como:";
            GameEngine.TextInputDefaultValue = _currentMapFileName;
            GameEngine.TextInputReturnState = "Editor";
            IsDone = true;
            NextState = "TextInput";
        }
        private void RequestSaveMap() { SaveMap(_currentMapFileName); }

        public void RequestLoadMapWithPrompt()
        {
            Debug.WriteLine("Funcionalidade 'Carregar Como...' (Ctrl+L) ainda não implementada.");
            LoadMap(_currentMapFileName);        }

        public void RequestNewMapWithPrompt()
        {
            GameEngine.OnTextInputComplete = _onNewMapPromptComplete;
            GameEngine.TextInputPrompt = "Nome do Novo Mapa:";
            GameEngine.TextInputDefaultValue = $"new_map_{DateTime.Now:yyyyMMddHHmmss}.json";
            GameEngine.TextInputReturnState = "Editor";
            IsDone = true;
            NextState = "TextInput";
        }

        public void RequestAddTriggerAtCursor()
        {
            Vector3 positionToPlace = _cursorWorldPos;
            GameEngine.OnTextInputComplete = (id) =>
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    AddTriggerAt(positionToPlace, id);                }
                else { Debug.WriteLine("Adição de trigger cancelada: ID vazio."); }
            };

            GameEngine.TextInputPrompt = "Digite o ID do Trigger:";
            GameEngine.TextInputDefaultValue = $"Trigger_{_currentMapData.Triggers.Count + 1}";
            GameEngine.TextInputReturnState = "Editor";
            IsDone = true;
            NextState = "TextInput";
        }
        public void SwitchEditorMode() { _currentMode = (_currentMode == EditorMode.Tiles) ? EditorMode.Triggers : EditorMode.Tiles; _selectedTrigger = null; Debug.WriteLine($"Mode switched to: {_currentMode}"); }
        public void SetCurrentZLevel(int z) { if (z >= 0 && z <= Constants.MaxZLevel) { _currentZLevel = z; _selectedTrigger = null; Debug.WriteLine($"Current Z-Level: {_currentZLevel}"); } }
        public void MoveCamera(Vector2 moveAmount) { Game1.Camera.Follow(Game1.Camera.Position + moveAmount); }
        public void ZoomCamera(float zoomFactor) { Game1.Camera.SetZoom(Game1.Camera.Zoom * zoomFactor); }
        public void SelectNextTileInPalette() { if (_tilePalette == null || _tilePalette.Count == 0) return; _paletteIndex = (_paletteIndex + 1) % _tilePalette.Count; _selectedTileId = _tilePalette[_paletteIndex].Id; UpdateSelectedTileInfo(); }
        public void SelectPreviousTileInPalette() { if (_tilePalette == null || _tilePalette.Count == 0) return; _paletteIndex = (_paletteIndex - 1 + _tilePalette.Count) % _tilePalette.Count; _selectedTileId = _tilePalette[_paletteIndex].Id; UpdateSelectedTileInfo(); }
        public void PlaceSelectedTileAtCursor() { PlaceTile(_cursorWorldPos, _selectedTileId); }
        public void EraseTileAtCursor() { PlaceTile(_cursorWorldPos, 0); }
        public void SelectTriggerAtCursor() { SelectTriggerNear(_cursorWorldPos); }
        public void RemoveSelectedTrigger() { if (_selectedTrigger != null) RemoveTrigger(_selectedTrigger); else Debug.WriteLine("No trigger selected to remove."); }

        private void InitializePalette()
        { /* ... (mesmo código de antes) ... */
            string sampleMapPath = Path.Combine("Content", "maps", "map1.json");
            _tilePalette = new List<TileMappingEntry>();
            if (File.Exists(sampleMapPath)) { try { string jsonContent = File.ReadAllText(sampleMapPath); var sampleMapData = JsonConvert.DeserializeObject<MapData>(jsonContent); if (sampleMapData?.TileMapping != null) { _tilePalette.AddRange(sampleMapData.TileMapping); } } catch (Exception ex) { Debug.WriteLine($"Erro ao carregar paleta de {sampleMapPath}: {ex.Message}"); } } else { Debug.WriteLine($"Arquivo de mapa de exemplo para paleta não encontrado: {sampleMapPath}"); }
            if (_tilePalette.Count == 0) { _tilePalette.Add(new TileMappingEntry { Id = 1, AssetName = "tile_grass1", Solid = false }); Debug.WriteLine("Paleta vazia. Adicionado tile padrão."); }
            _paletteIndex = 0;
            _selectedTileId = _tilePalette.Count > 0 ? _tilePalette[0].Id : 0;
        }
        private void UpdateSelectedTileInfo()
        { /* ... (mesmo código de antes) ... */
            if (_tilePalette == null || _tilePalette.Count == 0) { _selectedTileInfo = null; _selectedTileId = 0; return; }
            _selectedTileInfo = _tilePalette.FirstOrDefault(t => t.Id == _selectedTileId);
            if (_selectedTileInfo == null) { _paletteIndex = 0; _selectedTileId = _tilePalette[0].Id; _selectedTileInfo = _tilePalette[0]; }
            _paletteIndex = _tilePalette.FindIndex(t => t.Id == _selectedTileId); if (_paletteIndex < 0) _paletteIndex = 0;
        }
        private void CreateNewMap(int width = 30, int height = 30)
        { /* ... (mesmo código de antes) ... */
            _currentMapData = new MapData { Width = width, Height = height, TileMapping = new List<TileMappingEntry>(_tilePalette ?? new List<TileMappingEntry>()), Layers = new List<MapLayer>(), Triggers = new List<MapTrigger>() };
            _currentMapData.Layers.Add(new MapLayer { Name = $"Ground (Z=0)", ZLevel = 0, Data = Enumerable.Repeat(0, width * height).ToList() });
            RebuildMapSprites();
            _selectedTrigger = null;
            _isDirty = false;            _changesMade = 0;            Debug.WriteLine($"Novo mapa criado ({width}x{height}).");
        }
        private void RebuildMapSprites()
        { /* ... (mesmo código de antes) ... */
            _mapSprites.Clear(); if (_currentMapData?.Layers == null || _currentMapData.Width <= 0) return;
            Dictionary<int, TileMappingEntry> tileLookup = new Dictionary<int, TileMappingEntry>(); if (_currentMapData.TileMapping != null) { try { tileLookup = _currentMapData.TileMapping.ToDictionary(entry => entry.Id, entry => entry); } catch (ArgumentException ex) { Debug.WriteLine($"IDs duplicados no tileMapping: {ex.Message}"); } }
            foreach (var layer in _currentMapData.Layers.OrderBy(l => l.ZLevel)) { if (layer.Data == null) continue; for (int i = 0; i < layer.Data.Count; i++) { int tileId = layer.Data[i]; if (tileId == 0) continue; int x = i % _currentMapData.Width; int y = i / _currentMapData.Width; Vector3 worldPos = new Vector3(x, y, layer.ZLevel); if (tileLookup.TryGetValue(tileId, out var tileInfo) && GameEngine.Assets.Images.TryGetValue(tileInfo.AssetName, out var texture)) { _mapSprites[worldPos] = new Sprite(texture, worldPos); } } }
            Debug.WriteLine($"Sprites reconstruídos: {_mapSprites.Count} tiles.");
        }
        private void PlaceTile(Vector3 worldPos, int tileId)
        { /* ... (mesmo código de antes, mas com auto-save) ... */
            if (_currentMapData == null || _currentMapData.Width <= 0) return;
            if (worldPos.X < 0 || worldPos.X >= _currentMapData.Width || worldPos.Y < 0 || worldPos.Y >= _currentMapData.Height || worldPos.Z < 0 || worldPos.Z > Constants.MaxZLevel) return;
            int x = (int)worldPos.X; int y = (int)worldPos.Y; int z = (int)worldPos.Z;
            MapLayer targetLayer = _currentMapData.Layers?.FirstOrDefault(l => l.ZLevel == z);
            if (targetLayer == null) { if (tileId == 0) return; if (_currentMapData.Layers == null) _currentMapData.Layers = new List<MapLayer>(); targetLayer = new MapLayer { Name = $"Layer (Z={z})", ZLevel = z, Data = Enumerable.Repeat(0, _currentMapData.Width * _currentMapData.Height).ToList() }; _currentMapData.Layers.Add(targetLayer); _currentMapData.Layers.Sort((a, b) => a.ZLevel.CompareTo(b.ZLevel)); Debug.WriteLine($"Criada camada Z={z}"); }
            int expectedSize = _currentMapData.Width * _currentMapData.Height; if (targetLayer.Data == null || targetLayer.Data.Count != expectedSize) { Debug.WriteLine($"Corrigindo array 'Data' para camada Z={z}."); targetLayer.Data = Enumerable.Repeat(0, expectedSize).ToList(); }
            int index = y * _currentMapData.Width + x; if (index < 0 || index >= targetLayer.Data.Count) { Debug.WriteLine($"Índice inválido ({index}) ao colocar tile."); return; }
            if (targetLayer.Data[index] != tileId)
            {
                targetLayer.Data[index] = tileId;
                if (tileId == 0) { _mapSprites.Remove(worldPos); } else { TileMappingEntry newTileInfo = _tilePalette?.FirstOrDefault(t => t.Id == tileId); if (newTileInfo != null && GameEngine.Assets.Images.TryGetValue(newTileInfo.AssetName, out var texture)) { _mapSprites[worldPos] = new Sprite(texture, worldPos); } else { _mapSprites.Remove(worldPos); Debug.WriteLine($"Tile ID {tileId} inválido ou textura '{newTileInfo?.AssetName ?? "N/A"}' não encontrada."); } }
                _isDirty = true;
                _changesMade++;
                CheckAutoSave();
            }
        }
        private void SelectTriggerNear(Vector3 clickPos)
        { /* ... (mesmo código de antes) ... */
            if (_currentMapData?.Triggers == null) return; MapTrigger foundTrigger = null; float closestDistSq = 0.3f * 0.3f;
            foreach (var trigger in _currentMapData.Triggers) { if (Math.Abs(trigger.Position.Z - clickPos.Z) < 0.1f) { float distSq = Vector2.DistanceSquared(new Vector2(trigger.Position.X, trigger.Position.Y), new Vector2(clickPos.X, clickPos.Y)); if (distSq <= closestDistSq) { foundTrigger = trigger; closestDistSq = distSq; } } }
            _selectedTrigger = foundTrigger; Debug.WriteLine($"Trigger selecionado: {(_selectedTrigger?.Id ?? "None")}");
        }
        private void AddTriggerAt(Vector3 worldPos, string id)
        {
            if (_currentMapData == null || _currentMapData.Width <= 0) return; if (_currentMapData.Triggers == null) _currentMapData.Triggers = new List<MapTrigger>(); if (worldPos.X < 0 || worldPos.X >= _currentMapData.Width || worldPos.Y < 0 || worldPos.Y >= _currentMapData.Height) return;
            var newTrigger = new MapTrigger
            {
                Id = id,                Position = worldPos,
                TargetMap = "changeme.json",                TargetPosition = Vector3.Zero,                Radius = 0.5f            };
            _currentMapData.Triggers.Add(newTrigger);
            _selectedTrigger = newTrigger;
            Debug.WriteLine($"Novo trigger adicionado em {worldPos}. ID: {newTrigger.Id}");
            _isDirty = true;
            _changesMade++;
            CheckAutoSave();
        }
        private void RemoveTrigger(MapTrigger triggerToRemove)
        { /* ... (mesmo código de antes, mas com auto-save) ... */
            if (_currentMapData?.Triggers == null || triggerToRemove == null) return; bool removed = _currentMapData.Triggers.Remove(triggerToRemove); if (removed)
            {
                Debug.WriteLine($"Trigger '{triggerToRemove.Id ?? "(sem ID)"}' removido."); if (_selectedTrigger == triggerToRemove) _selectedTrigger = null;
                _isDirty = true;
                _changesMade++;
                CheckAutoSave();
            }
            else { Debug.WriteLine($"Falha ao remover trigger '{triggerToRemove.Id ?? "(sem ID)"}'."); }
        }
        private void SaveMap(string fileName)
        { /* ... (mesmo código de antes, com _isDirty = false) ... */
            if (_currentMapData == null) { Debug.WriteLine("SaveMap: Nenhum dado de mapa para salvar."); return; }
            _currentMapData.TileMapping = new List<TileMappingEntry>(_tilePalette ?? new List<TileMappingEntry>()); string directory = Path.Combine("Content", "maps"); string filePath = Path.Combine(directory, fileName); Debug.WriteLine($"Tentando salvar mapa em: {filePath}"); try
            {
                Directory.CreateDirectory(directory); string jsonContent = JsonConvert.SerializeObject(_currentMapData, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }); File.WriteAllText(filePath, jsonContent); Debug.WriteLine($"Mapa salvo com sucesso.");
                _isDirty = false;            }
            catch (Exception ex) { Debug.WriteLine($"Erro ao salvar mapa em {filePath}: {ex.Message}"); }
        }
        private void LoadMap(string fileName)
        { /* ... (mesmo código de antes, com reset de _isDirty) ... */
            string filePath = Path.Combine("Content", "maps", fileName); Debug.WriteLine($"Tentando carregar mapa: {filePath}");
            if (!File.Exists(filePath)) { Debug.WriteLine("Arquivo não encontrado. Criando novo mapa padrão."); _currentMapFileName = fileName; CreateNewMap(); return; }            try { string jsonContent = File.ReadAllText(filePath); var loadedData = JsonConvert.DeserializeObject<MapData>(jsonContent); if (loadedData == null) { Debug.WriteLine($"Falha ao desserializar {filePath}. Criando novo mapa."); _currentMapFileName = fileName; CreateNewMap(); } else { _currentMapData = loadedData; _currentMapFileName = fileName; if (_currentMapData.Layers == null) _currentMapData.Layers = new List<MapLayer>(); if (_currentMapData.Triggers == null) _currentMapData.Triggers = new List<MapTrigger>(); if (_currentMapData.TileMapping == null) _currentMapData.TileMapping = new List<TileMappingEntry>(); if (_currentMapData.TileMapping.Count > 0) { _tilePalette = new List<TileMappingEntry>(_currentMapData.TileMapping); Debug.WriteLine($"Paleta atualizada com base no mapa ({_tilePalette.Count} tiles)."); } else { Debug.WriteLine("Mapa sem tileMapping. Mantendo paleta anterior."); _currentMapData.TileMapping = new List<TileMappingEntry>(_tilePalette ?? new List<TileMappingEntry>()); } UpdateSelectedTileInfo(); RebuildMapSprites(); _selectedTrigger = null; Debug.WriteLine($"Mapa {fileName} carregado. {_currentMapData.Layers.Count} camadas, {_currentMapData.Triggers.Count} triggers."); } } catch (Exception ex) { Debug.WriteLine($"Erro ao carregar mapa {filePath}: {ex.Message}. Criando novo mapa."); _currentMapFileName = fileName; CreateNewMap(); }
            _isDirty = false;            _changesMade = 0;        }
        private void CheckAutoSave()
        {
            if (_isDirty && _changesMade > 0 && (_changesMade % AUTO_SAVE_THRESHOLD == 0))
            {
                Debug.WriteLine($"Auto-salvando... ({_changesMade} alterações totais).");
                SaveMap(_currentMapFileName);            }
        }

    }
}