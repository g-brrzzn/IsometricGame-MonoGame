
using IsometricGame.Classes;
using IsometricGame.Classes.Particles;
using IsometricGame.Map;using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
namespace IsometricGame.States
{
    public enum TransitionState
    {
        Idle,
        FadingOut,
        FadingIn
    }

    public class GameplayState : GameStateBase
    {
        private Explosion _hitExplosion;
        private Fall _backgroundFall;        private MapManager _mapManager;
        private Texture2D _cursorTexture;

        private TransitionState _transitionState = TransitionState.Idle;
        private float _fadeAlpha = 0f;        private float _fadeSpeed = 1.5f;        private bool _isTransitioning = false;
        private MapTrigger _pendingTrigger = null;        private Texture2D _pixelTexture;
        public GameplayState()
        {
            _mapManager = new MapManager();        }

        public override void Start()
        {
            base.Start();

            _hitExplosion = new Explosion();
            _backgroundFall = new Fall(300);
            _transitionState = TransitionState.Idle;
            _fadeAlpha = 0f;
            _isTransitioning = false;
            _pendingTrigger = null;

            LoadInitialMap("map1.json");

            _cursorTexture = GameEngine.Assets.Images["cursor"];
            if (!GameEngine.Assets.Images.TryGetValue("pixel", out _pixelTexture))
            {
                _pixelTexture = new Texture2D(Game1._graphicsManagerInstance.GraphicsDevice, 1, 1);
                _pixelTexture.SetData(new[] { Color.White });
                GameEngine.Assets.Images["pixel"] = _pixelTexture;                Debug.WriteLine("Aviso: Textura 'pixel' não encontrada no AssetManager. Criado pixel branco temporário.");
            }

            Game1.Instance.IsMouseVisible = false;
        }

        private void LoadInitialMap(string mapFileName)
        {
            GameEngine.ResetGame();            bool mapLoaded = _mapManager.LoadMap(mapFileName);

            if (!mapLoaded)
            {
                Debug.WriteLine("ERRO CRÍTICO: Mapa inicial não pôde ser carregado!");
                IsDone = true;
                NextState = "Menu";
                return;
            }

            Vector3 playerStartPos = new Vector3(2, 2, 0);
            GameEngine.Player = new Player(playerStartPos);
            GameEngine.AllSprites.Add(GameEngine.Player);

            SpawnEnemies();        }


        public override void End()
        {
            _mapManager.UnloadCurrentMap();            Game1.Instance.IsMouseVisible = true;
            ClearDynamicEntities();
            if (GameEngine.Player != null)
            {
                GameEngine.Player.Kill();                GameEngine.AllSprites.Remove(GameEngine.Player);                GameEngine.Player = null;            }

        }

        private void SpawnEnemies()
        {
            GameEngine.AllEnemies.Clear();
            GameEngine.AllSprites.RemoveAll(s => s is EnemyBase);

            Debug.WriteLine($"Spawning inimigos para Nível {GameEngine.Level} no mapa {_mapManager.CurrentMapName}");
            int mapWidth = _mapManager.GetCurrentMapData()?.OriginalMapData?.Width ?? 20;            int mapHeight = _mapManager.GetCurrentMapData()?.OriginalMapData?.Height ?? 20;

            for (int i = 0; i < GameEngine.Level * 3; i++)
            {
                int attempts = 0;
                Vector3 spawnPos;
                bool positionFound = false;
                while (attempts < 50 && !positionFound)                {
                    float x = GameEngine.Random.Next(0, mapWidth);
                    float y = GameEngine.Random.Next(0, mapHeight);
                    spawnPos = new Vector3(x, y, 0);
                    if ((GameEngine.Player == null || Vector2.Distance(new Vector2(x, y), new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y)) > 5.0f)
                        && !GameEngine.SolidTiles.ContainsKey(spawnPos)
                        && !GameEngine.SolidTiles.ContainsKey(spawnPos + new Vector3(0, 0, 1)))                    {
                        positionFound = true;
                        SpawnEnemy(typeof(Enemy1), spawnPos);
                    }
                    attempts++;
                }
                if (!positionFound) Debug.WriteLine($"Não foi possível encontrar posição válida para spawn de inimigo {i + 1} após {attempts} tentativas.");

            }
        }

        private void SpawnEnemy(Type enemyType, Vector3 worldPos)
        {
            EnemyBase enemy = null;
            if (enemyType == typeof(Enemy1)) enemy = new Enemy1(worldPos);

            if (enemy != null)
            {
                GameEngine.AllEnemies.Add(enemy);
                GameEngine.AllSprites.Add(enemy);
            }
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            UpdateTransition(gameTime);

            if (_isTransitioning)
            {
                return;
            }


            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float effectiveDt = dt * Constants.BaseSpeedMultiplier;

            if (input.IsKeyPressed("ESC"))
            {
                IsDone = true;
                NextState = "Pause";
                return;
            }
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved)
            {
                if (!IsDone) { IsDone = true; NextState = "GameOver"; }
                return;
            }

            Vector2 mouseInternalPos = input.InternalMousePosition;
            Vector2 isoScreenPos = Game1.Camera.ScreenToWorld(mouseInternalPos);
            Vector2 targetWorldPos = IsoMath.ScreenToWorld(isoScreenPos);
            Vector2 cursorDrawPos = mouseInternalPos;
            GameEngine.TargetWorldPosition = targetWorldPos;
            GameEngine.CursorScreenPosition = cursorDrawPos;
            GameEngine.Player.GetInput(input);

            _hitExplosion.Update(effectiveDt);
            for (int i = GameEngine.AllSprites.Count - 1; i >= 0; i--)
            {
                if (i < GameEngine.AllSprites.Count)
                {
                    var sprite = GameEngine.AllSprites[i];
                    if (sprite != null && !sprite.IsRemoved)
                    {
                        sprite.Update(gameTime, effectiveDt);
                    }
                }
            }

            HandleCollisions(gameTime);
            CleanupSprites();
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved)
            {
                if (!IsDone) { IsDone = true; NextState = "GameOver"; }
                return;
            }

            CheckForMapTransition();

            if (!GameEngine.AllEnemies.Any(e => !e.IsRemoved))            {
                Debug.WriteLine("Todos os inimigos derrotados! Spawning próxima onda...");
                SpawnEnemies();            }
        }

        private void UpdateTransition(GameTime gameTime)
        {
            float elapsedSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_transitionState == TransitionState.FadingOut)
            {
                _fadeAlpha += _fadeSpeed * elapsedSeconds;
                if (_fadeAlpha >= 1.0f)
                {
                    _fadeAlpha = 1.0f;
                    Debug.WriteLine("Fade Out completo. Iniciando troca de mapa...");
                    ClearDynamicEntities();
                    bool loaded = _mapManager.LoadMap(_pendingTrigger.TargetMap);

                    if (loaded)
                    {
                        if (GameEngine.Player != null && !GameEngine.Player.IsRemoved)
                        {
                            GameEngine.Player.WorldPosition = _pendingTrigger.TargetPosition;
                            GameEngine.Player.WorldVelocity = Vector2.Zero;                            GameEngine.Player.UpdateScreenPosition();                            Debug.WriteLine($"Player reposicionado para {_pendingTrigger.TargetPosition} no mapa '{_pendingTrigger.TargetMap}'");

                            if (!GameEngine.AllSprites.Contains(GameEngine.Player))
                            {
                                Debug.WriteLine("Adicionando jogador de volta a AllSprites após transição.");
                                GameEngine.AllSprites.Add(GameEngine.Player);
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Jogador não existe ou foi removido durante a transição, não será reposicionado.");
                        }

                        SpawnEnemies();                    }
                    else
                    {
                        Debug.WriteLine($"ERRO FATAL: Falha ao carregar mapa '{_pendingTrigger.TargetMap}'. Indo para o Menu.");
                        IsDone = true;                        NextState = "Menu";
                        _transitionState = TransitionState.Idle;                        _isTransitioning = false;
                        _pendingTrigger = null;
                        _fadeAlpha = 0f;                        return;                    }

                    _pendingTrigger = null;                    _transitionState = TransitionState.FadingIn;                    Debug.WriteLine("Troca de mapa concluída. Iniciando Fade In.");
                }
            }
            else if (_transitionState == TransitionState.FadingIn)
            {
                _fadeAlpha -= _fadeSpeed * elapsedSeconds;
                if (_fadeAlpha <= 0.0f)
                {
                    _fadeAlpha = 0.0f;                    _transitionState = TransitionState.Idle;                    _isTransitioning = false;                    Debug.WriteLine("Fade In completo. Transição finalizada.");
                }
            }
        }


        private void CheckForMapTransition()
        {
            if (_isTransitioning || GameEngine.Player == null || GameEngine.Player.IsRemoved) return;

            Vector3 playerPos = GameEngine.Player.WorldPosition;
            List<MapTrigger> triggers = _mapManager.GetCurrentTriggers();

            if (triggers == null || triggers.Count == 0) return;
            foreach (var trigger in triggers)
            {
                if (string.IsNullOrEmpty(trigger.TargetMap)) continue;
                float distanceSq = Vector2.DistanceSquared(
                    new Vector2(playerPos.X, playerPos.Y),
                    new Vector2(trigger.Position.X, trigger.Position.Y)
                );

                float triggerRadiusSq = trigger.Radius * trigger.Radius;

                if (distanceSq <= triggerRadiusSq)
                {
                    if (Math.Abs(playerPos.Z - trigger.Position.Z) < 0.1f)                    {
                        Debug.WriteLine($"Player ativou trigger '{trigger.Id ?? "N/A"}' para mapa '{trigger.TargetMap}'");
                        InitiateMapTransition(trigger);                        return;                    }
                }
            }
        }

        private void InitiateMapTransition(MapTrigger trigger)
        {
            if (_isTransitioning) return;
            Debug.WriteLine("Iniciando Fade Out para transição...");
            _isTransitioning = true;            _pendingTrigger = trigger;            _transitionState = TransitionState.FadingOut;
            _fadeAlpha = Math.Max(0f, _fadeAlpha);        }

        private void ClearDynamicEntities()
        {
            Debug.WriteLine("Limpando entidades dinâmicas (Inimigos, Balas)...");
            int enemyCount = 0;
            int bulletCount = 0;

            for (int i = GameEngine.AllSprites.Count - 1; i >= 0; i--)
            {
                var sprite = GameEngine.AllSprites[i];
                if (sprite is EnemyBase || sprite is Bullet)
                {
                    sprite.Kill();                    GameEngine.AllSprites.RemoveAt(i);
                    if (sprite is EnemyBase) enemyCount++;
                    else bulletCount++;
                }
            }

            GameEngine.AllEnemies.Clear();
            GameEngine.PlayerBullets.Clear();
            GameEngine.EnemyBullets.Clear();

            Debug.WriteLine($"Removidas {enemyCount} inimigos e {bulletCount} balas.");
        }

        private void HandleCollisions(GameTime gameTime)
        {
            if (GameEngine.Player == null || GameEngine.Player.IsRemoved) return;
            const float enemyCollisionRadius = 0.8f;
            const float bulletCollisionRadius = 0.3f;
            const float playerCollisionRadius = 0.6f;
            for (int i = GameEngine.AllEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = GameEngine.AllEnemies[i];
                if (enemy.IsRemoved) continue;
                Vector2 enemyPosXY = new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y);

                for (int j = GameEngine.PlayerBullets.Count - 1; j >= 0; j--)
                {
                    var bullet = GameEngine.PlayerBullets[j];
                    if (bullet.IsRemoved) continue;
                    Vector2 bulletPosXY = new Vector2(bullet.WorldPosition.X, bullet.WorldPosition.Y);

                    if (Vector2.DistanceSquared(bulletPosXY, enemyPosXY) < MathF.Pow(enemyCollisionRadius + bulletCollisionRadius, 2))
                    {
                        if (enemy.Texture != null)
                            _hitExplosion.Create(enemy.ScreenPosition.X, enemy.ScreenPosition.Y - enemy.Origin.Y);
                        enemy.Damage(gameTime);
                        bullet.Kill();                    }
                }
            }

            if (!GameEngine.Player.IsRemoved)
            {
                Vector2 playerPosXY = new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y);

                for (int i = GameEngine.EnemyBullets.Count - 1; i >= 0; i--)
                {
                    var bullet = GameEngine.EnemyBullets[i];
                    if (bullet.IsRemoved) continue;
                    Vector2 bulletPosXY = new Vector2(bullet.WorldPosition.X, bullet.WorldPosition.Y);

                    if (Vector2.DistanceSquared(bulletPosXY, playerPosXY) < MathF.Pow(playerCollisionRadius + bulletCollisionRadius, 2))
                    {
                        GameEngine.Player.TakeDamage(gameTime);
                        bullet.Kill();
                        if (GameEngine.Player.IsRemoved) break;                    }
                }

                if (!GameEngine.Player.IsRemoved)
                {
                    for (int i = GameEngine.AllEnemies.Count - 1; i >= 0; i--)
                    {
                        var enemy = GameEngine.AllEnemies[i];
                        if (enemy.IsRemoved) continue;
                        Vector2 enemyPosXY = new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y);

                        if (Vector2.DistanceSquared(enemyPosXY, playerPosXY) < MathF.Pow(enemyCollisionRadius + playerCollisionRadius, 2))
                        {
                            GameEngine.Player.TakeDamage(gameTime);
                            enemy.Kill();                            GameEngine.ScreenShake = 5;
                            if (GameEngine.Player.IsRemoved) break;                        }
                    }
                }
            }
        }

        private void CleanupSprites()
        {
            GameEngine.AllSprites.RemoveAll(s => s != null && s.IsRemoved && s != GameEngine.Player);

            GameEngine.AllEnemies.RemoveAll(e => e.IsRemoved);
            GameEngine.PlayerBullets.RemoveAll(b => b.IsRemoved);
            GameEngine.EnemyBullets.RemoveAll(b => b.IsRemoved);

            if (GameEngine.Player != null && GameEngine.Player.IsRemoved)
            {
                GameEngine.Player = null;
                Debug.WriteLine("Jogador removido (vida <= 0).");
            }
        }

        public void DrawWorld(SpriteBatch spriteBatch)
        {
            foreach (var sprite in GameEngine.AllSprites)
            {
                if (sprite != null && !sprite.IsRemoved)
                    sprite.Draw(spriteBatch);
            }

            _hitExplosion.Draw(spriteBatch);        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            var font = GameEngine.Assets.Fonts["captain_32"];
            Vector2 levelPos = new Vector2(Constants.InternalResolution.X - 100, 30);
            Vector2 lifePos = new Vector2(Constants.InternalResolution.X - 100, 60);

            DrawUtils.DrawTextScreen(spriteBatch, $"Level {GameEngine.Level}", font, levelPos, Color.White, 0.1f);            if (GameEngine.Player != null && !GameEngine.Player.IsRemoved)
                DrawUtils.DrawTextScreen(spriteBatch, $"Life  {GameEngine.Player.Life}", font, lifePos, Color.White, 0.1f);
            else
                DrawUtils.DrawTextScreen(spriteBatch, "Life  0", font, lifePos, Color.Red, 0.1f);

            if (_cursorTexture != null && !_isTransitioning)
            {
                Vector2 cursorPos = GameEngine.CursorScreenPosition;
                Vector2 origin = new Vector2(_cursorTexture.Width / 2f, _cursorTexture.Height / 2f);
                spriteBatch.Draw(
                    _cursorTexture, cursorPos, null, Color.White, 0f, origin, 1.0f, SpriteEffects.None, 0.0f
                );
            }
        }

        public void DrawTransitionOverlay(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (_fadeAlpha > 0.001f)            {
                float clampedAlpha = MathHelper.Clamp(_fadeAlpha, 0f, 1f);
                Color fadeColor = Color.Black * clampedAlpha;
                Rectangle screenRectangle = new Rectangle(0, 0, Constants.InternalResolution.X, Constants.InternalResolution.Y);

                if (_pixelTexture != null)                {
                    spriteBatch.Draw(_pixelTexture, screenRectangle, null, fadeColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0f);
                }
                else
                {
                    Debug.WriteLine("Erro: _pixelTexture é nula em DrawTransitionOverlay!");
                }
            }
        }

    }}