using IsometricGame.Classes;
using IsometricGame.Classes.Particles;
using IsometricGame.Classes.Upgrades;using IsometricGame.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IsometricGame.States
{
    public enum TransitionState { Idle, FadingOut, FadingIn }

    public class GameplayState : GameStateBase
    {
        private Explosion _hitExplosion;
        private Fall _backgroundFall;
        private MapManager _mapManager;
        private Texture2D _cursorTexture;
        private Texture2D _pixelTexture;

        private TransitionState _transitionState = TransitionState.Idle;
        private float _fadeAlpha = 0f;
        private float _fadeSpeed = 1.5f;
        private bool _isTransitioning = false;
        private MapTrigger _pendingTrigger = null;

        public GameplayState()
        {
            _mapManager = new MapManager();
        }

        public override void Start()
        {
            base.Start();

            _hitExplosion = new Explosion();
            _backgroundFall = new Fall(300);

            _cursorTexture = GameEngine.Assets.Images["cursor"];
            if (GameEngine.Assets.Images.ContainsKey("pixel"))
                _pixelTexture = GameEngine.Assets.Images["pixel"];

            Game1.Instance.IsMouseVisible = false;

            if (GameEngine.Player != null && !GameEngine.Player.IsRemoved)
            {
                if (GameEngine.Player.Experience >= GameEngine.Player.ExperienceToNextLevel)
                {
                    GameEngine.Player.ConfirmLevelUp();
                }
                return;            }

            _transitionState = TransitionState.Idle;
            _fadeAlpha = 0f;
            LoadInitialMap("map1.json");
        }

        private void LoadInitialMap(string mapFileName)
        {
            GameEngine.ResetGame();
            if (!_mapManager.LoadMap(mapFileName))
            {
                IsDone = true; NextState = "Menu"; return;
            }

            GameEngine.Player = new Player(new Vector3(5, 5, 0));
            GameEngine.AllSprites.Add(GameEngine.Player);
        }

        public override void End()
        {
            if (NextState == "Pause" || NextState == "LevelUp") return;

            _mapManager.UnloadCurrentMap();
            Game1.Instance.IsMouseVisible = true;
            ClearDynamicEntities();
            GameEngine.Player = null;
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            UpdateTransition(gameTime);
            if (_isTransitioning) return;

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

            GameEngine.Player.GetInput(input);

            if (GameEngine.Player.Experience >= GameEngine.Player.ExperienceToNextLevel)
            {
                GameEngine.CurrentUpgradeOptions = UpgradeManager.GetRandomOptions(3);

                IsDone = true;
                NextState = "LevelUp";
                return;
            }

            int desiredEnemies = 10 + (GameEngine.Level * 5);
            int currentEnemies = GameEngine.AllEnemies.Count(e => !e.IsRemoved);
            if (currentEnemies < desiredEnemies)
            {
                if (GameEngine.Random.NextDouble() < 0.1) SpawnHordeEnemy();
            }

            _hitExplosion.Update(effectiveDt);

            for (int i = GameEngine.AllSprites.Count - 1; i >= 0; i--)
            {
                var sprite = GameEngine.AllSprites[i];
                if (sprite != null && !sprite.IsRemoved)
                    sprite.Update(gameTime, effectiveDt);
            }

            HandleCollisions(gameTime);
            CleanupSprites();
            CheckForMapTransition();
        }

        private void SpawnHordeEnemy()
        {
            int mapW = _mapManager.GetCurrentMapData()?.OriginalMapData?.Width ?? 30;
            int mapH = _mapManager.GetCurrentMapData()?.OriginalMapData?.Height ?? 30;

            int x = GameEngine.Random.Next(1, mapW - 1);
            int y = GameEngine.Random.Next(1, mapH - 1);
            Vector3 pos = new Vector3(x, y, 0);

            if (!GameEngine.SolidTiles.ContainsKey(pos))
            {
                float dist = Vector2.Distance(new Vector2(pos.X, pos.Y), new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y));
                if (dist > 6.0f)
                {
                    var enemy = new Enemy1(pos);
                    GameEngine.AllEnemies.Add(enemy);
                    GameEngine.AllSprites.Add(enemy);
                }
            }
        }

        private void HandleCollisions(GameTime gameTime)
        {
            if (GameEngine.Player == null) return;

            for (int i = GameEngine.AllEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = GameEngine.AllEnemies[i];
                if (enemy.IsRemoved) continue;

                if (Vector2.DistanceSquared(new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y),
                                            new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y)) < 0.8f)
                {
                    GameEngine.Player.TakeDamage(gameTime);
                }

                for (int j = GameEngine.PlayerBullets.Count - 1; j >= 0; j--)
                {
                    var bullet = GameEngine.PlayerBullets[j];
                    if (bullet.IsRemoved) continue;

                    if (bullet.HitList.Contains(enemy)) continue;

                    if (Vector2.DistanceSquared(new Vector2(bullet.WorldPosition.X, bullet.WorldPosition.Y),
                                                new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y)) < 0.5f)
                    {
                        if (enemy.Texture != null)
                            _hitExplosion.Create(enemy.ScreenPosition.X, enemy.ScreenPosition.Y - enemy.Origin.Y);

                        enemy.Damage(gameTime);

                        bullet.HitList.Add(enemy);

                        bullet.PiercingLeft--;
                        if (bullet.PiercingLeft < 0)
                        {
                            bullet.Kill();
                        }
                    }
                }
            }
        }


        private void UpdateTransition(GameTime gameTime)
        {
            if (_transitionState == TransitionState.Idle) return;
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_transitionState == TransitionState.FadingOut)
            {
                _fadeAlpha += _fadeSpeed * elapsed;
                if (_fadeAlpha >= 1f)
                {
                    _fadeAlpha = 1f;
                    ClearDynamicEntities();
                    if (_mapManager.LoadMap(_pendingTrigger.TargetMap))
                    {
                        GameEngine.Player.WorldPosition = _pendingTrigger.TargetPosition;
                        GameEngine.Player.UpdateScreenPosition();
                        if (!GameEngine.AllSprites.Contains(GameEngine.Player)) GameEngine.AllSprites.Add(GameEngine.Player);
                        _transitionState = TransitionState.FadingIn;
                    }
                    else { IsDone = true; NextState = "Menu"; }
                }
            }
            else if (_transitionState == TransitionState.FadingIn)
            {
                _fadeAlpha -= _fadeSpeed * elapsed;
                if (_fadeAlpha <= 0f) { _fadeAlpha = 0f; _transitionState = TransitionState.Idle; _isTransitioning = false; }
            }
        }

        private void CheckForMapTransition()
        {
            if (_isTransitioning || GameEngine.Player == null) return;
            var triggers = _mapManager.GetCurrentTriggers();
            foreach (var tr in triggers)
            {
                if (Vector2.DistanceSquared(new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y), new Vector2(tr.Position.X, tr.Position.Y)) < tr.Radius * tr.Radius)
                {
                    _isTransitioning = true; _pendingTrigger = tr; _transitionState = TransitionState.FadingOut; break;
                }
            }
        }

        private void ClearDynamicEntities()
        {
            for (int i = GameEngine.AllSprites.Count - 1; i >= 0; i--)
            {
                var s = GameEngine.AllSprites[i];
                if (s is EnemyBase || s is Bullet || s is ExperienceGem)
                {
                    s.Kill();
                    GameEngine.AllSprites.RemoveAt(i);
                }
            }
            GameEngine.AllEnemies.Clear();
            GameEngine.PlayerBullets.Clear();
            GameEngine.EnemyBullets.Clear();
        }

        private void CleanupSprites()
        {
            GameEngine.AllSprites.RemoveAll(s => s.IsRemoved);
            GameEngine.AllEnemies.RemoveAll(e => e.IsRemoved);
            GameEngine.PlayerBullets.RemoveAll(b => b.IsRemoved);
            GameEngine.EnemyBullets.RemoveAll(b => b.IsRemoved);
        }

        public void DrawWorld(SpriteBatch spriteBatch)
        {
            foreach (var sprite in GameEngine.AllSprites)
                if (!sprite.IsRemoved) sprite.Draw(spriteBatch);
            _hitExplosion.Draw(spriteBatch);
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            DrawHUD(spriteBatch);
            if (_cursorTexture != null && !_isTransitioning)
            {
                Vector2 mousePos = Game1.InputManagerInstance.InternalMousePosition;
                spriteBatch.Draw(_cursorTexture, mousePos, Color.White);
            }
        }

        private void DrawHUD(SpriteBatch spriteBatch)
        {
            if (GameEngine.Player == null) return;

            int screenW = Constants.InternalResolution.X;
            int barW = 600;
            int barH = 20;
            int barX = (screenW - barW) / 2;
            int barY = 20;

            if (_pixelTexture != null)
            {
                spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, barW, barH), Color.Black * 0.6f);
                float pct = (float)GameEngine.Player.Experience / (float)GameEngine.Player.ExperienceToNextLevel;
                pct = MathHelper.Clamp(pct, 0f, 1f);
                spriteBatch.Draw(_pixelTexture, new Rectangle(barX, barY, (int)(barW * pct), barH), Color.Cyan);
            }

            var font = GameEngine.Assets.Fonts["captain_32"];
            string lvlText = $"LVL {GameEngine.Player.Level}";
            DrawUtils.DrawTextScreen(spriteBatch, lvlText, font, new Vector2(barX - 80, barY - 5), Color.White, 0f);
            DrawUtils.DrawTextScreen(spriteBatch, $"HP: {GameEngine.Player.Life}/{GameEngine.Player.MaxLife}", font, new Vector2(barX + barW + 20, barY - 5), Color.Red, 0f);
        }

        public void DrawTransitionOverlay(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            if (_fadeAlpha > 0f && _pixelTexture != null)
            {
                spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, Constants.InternalResolution.X, Constants.InternalResolution.Y), Color.Black * _fadeAlpha);
            }
        }
    }
}