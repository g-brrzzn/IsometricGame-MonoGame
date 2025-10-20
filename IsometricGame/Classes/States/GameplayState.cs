using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using IsometricGame.Classes;
using IsometricGame.Classes.Particles;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IsometricGame.States
{
    public class GameplayState : GameStateBase
    {
        private Explosion _hitExplosion;
        private Fall _backgroundFall;
        private MapLoader _mapLoader;

        public override void Start()
        {
            base.Start();
            GameEngine.ResetGame();

            _hitExplosion = new Explosion();
            _backgroundFall = new Fall(300);

            // --- INÍCIO DA MODIFICAÇÃO ---
            // 1. Carrega o mapa do arquivo JSON
            _mapLoader = new MapLoader();
            // Certifique-se que o caminho está correto RELATIVO À PASTA BIN/DEBUG/NET8.0/
            // Como map1.json está em Content/maps e configurado para copiar, este caminho deve funcionar.
            _mapLoader.LoadMap("Content/maps/map1.json");
            // --- FIM DA MODIFICAÇÃO ---


            // 2. Adiciona o Player em uma posição válida do mapa carregado
            Vector3 playerStartPos = new Vector3(2, 2, 0); // Exemplo: Posição (2,2) no chão (Z=0)
            GameEngine.Player = new Player(playerStartPos);
            GameEngine.AllSprites.Add(GameEngine.Player);

            // 3. Adiciona os Inimigos (talvez ajustar posições baseadas no novo mapa)
            SpawnEnemies(); // A lógica de spawn pode precisar de ajustes
        }

        private void SpawnEnemies()
        {
            // Ajuste os limites ou lógica se necessário para o tamanho/layout do mapa
            for (int i = 0; i < GameEngine.Level * 3; i++) // Reduzi a quantidade inicial
            {
                // Gera posições dentro dos limites do mapa (0 a 19, baseado no JSON)
                float x = GameEngine.Random.Next(0, 20);
                float y = GameEngine.Random.Next(0, 20);

                // Evita spawnar perto do player ou em locais "ocupados" (simplificado)
                if (Vector2.Distance(new Vector2(x, y), new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y)) < 5.0f)
                {
                    i--; // Tenta de novo
                    continue;
                }

                SpawnEnemy(typeof(Enemy1), new Vector3(x, y, 0)); // Spawn no Z=0
            }
        }

        // Aceita Vector3 (já estava correto aqui)
        private void SpawnEnemy(Type enemyType, Vector3 worldPos)
        {
            EnemyBase enemy = null;
            // O construtor de Enemy1 aceita Vector3 (verifique se Enemy1.cs foi atualizado)
            if (enemyType == typeof(Enemy1)) enemy = new Enemy1(worldPos);

            if (enemy != null)
            {
                GameEngine.AllEnemies.Add(enemy);
                GameEngine.AllSprites.Add(enemy);
            }
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float effectiveDt = dt * Constants.BaseSpeedMultiplier;
            if (input.IsKeyPressed("ESC"))
            {
                IsDone = true;
                NextState = "Pause";
                return;
            }
            if (GameEngine.Player == null)
            {
                if (!IsDone)
                {
                    IsDone = true;
                    NextState = "GameOver";
                }
                return;
            }
            GameEngine.Player.GetInput(input);
            _hitExplosion.Update(effectiveDt);
            // --- ATENÇÃO: Corrigido loop para evitar problemas de modificação da coleção ---
            // Usar um loop for reverso ou copiar a lista antes de iterar é mais seguro
            // se Kill() pudesse remover sprites de AllSprites diretamente, mas
            // como usamos RemoveAll depois, o loop original está OK.
            // Mantendo o loop original por enquanto.
            for (int i = GameEngine.AllSprites.Count - 1; i >= 0; i--)
            {
                // Adicionada verificação para garantir que o índice ainda é válido
                // caso CleanupSprites() fosse chamado dentro do loop (não é o caso aqui).
                if (i < GameEngine.AllSprites.Count)
                {
                    var sprite = GameEngine.AllSprites[i];
                    if (!sprite.IsRemoved)
                    {
                        sprite.Update(gameTime, effectiveDt);
                    }
                }
            }
            // --- FIM DA ATENÇÃO ---

            HandleCollisions(gameTime);
            CleanupSprites();
            if (GameEngine.Player == null)
            {
                if (!IsDone)
                {
                    IsDone = true;
                    NextState = "GameOver";
                }
                return;
            }
            if (GameEngine.AllEnemies.Count == 0)
            {
                GameEngine.Level++;
                SpawnEnemies();
            }
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
                // --- CORREÇÃO: Extrai XY para distância ---
                Vector2 enemyPosXY = new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y);
                // --- FIM DA CORREÇÃO ---

                for (int j = GameEngine.PlayerBullets.Count - 1; j >= 0; j--)
                {
                    var bullet = GameEngine.PlayerBullets[j];
                    if (bullet.IsRemoved) continue;

                    // --- CORREÇÃO: Extrai XY para distância ---
                    Vector2 bulletPosXY = new Vector2(bullet.WorldPosition.X, bullet.WorldPosition.Y);

                    if (Vector2.Distance(bulletPosXY, enemyPosXY) < (enemyCollisionRadius + bulletCollisionRadius))
                    {
                        if (enemy.Texture != null)
                            // Passa a ScreenPosition (que já é Vector2)
                            _hitExplosion.Create(enemy.ScreenPosition.X, enemy.ScreenPosition.Y - enemy.Origin.Y); // Usa Origin.Y em vez de Texture.Height/2
                        enemy.Damage(gameTime);
                        bullet.Kill();
                    }
                    // --- FIM DA CORREÇÃO ---
                }
            }
            if (!GameEngine.Player.IsRemoved)
            {
                // --- CORREÇÃO: Extrai XY para distância ---
                Vector2 playerPosXY = new Vector2(GameEngine.Player.WorldPosition.X, GameEngine.Player.WorldPosition.Y);
                // --- FIM DA CORREÇÃO ---

                for (int i = GameEngine.EnemyBullets.Count - 1; i >= 0; i--)
                {
                    var bullet = GameEngine.EnemyBullets[i];
                    if (bullet.IsRemoved) continue;

                    // --- CORREÇÃO: Extrai XY para distância ---
                    Vector2 bulletPosXY = new Vector2(bullet.WorldPosition.X, bullet.WorldPosition.Y);

                    if (Vector2.Distance(bulletPosXY, playerPosXY) < (playerCollisionRadius + bulletCollisionRadius))
                    {
                        GameEngine.Player.TakeDamage(gameTime);
                        bullet.Kill();
                        if (GameEngine.Player.IsRemoved) break;
                    }
                    // --- FIM DA CORREÇÃO ---
                }
                if (!GameEngine.Player.IsRemoved)
                {
                    for (int i = GameEngine.AllEnemies.Count - 1; i >= 0; i--)
                    {
                        var enemy = GameEngine.AllEnemies[i];
                        if (enemy.IsRemoved) continue;

                        // --- CORREÇÃO: Extrai XY para distância ---
                        Vector2 enemyPosXY = new Vector2(enemy.WorldPosition.X, enemy.WorldPosition.Y);

                        if (Vector2.Distance(enemyPosXY, playerPosXY) < (enemyCollisionRadius + playerCollisionRadius))
                        {
                            GameEngine.Player.TakeDamage(gameTime);
                            enemy.Kill();
                            GameEngine.ScreenShake = 5;
                            if (GameEngine.Player.IsRemoved) break;
                        }
                        // --- FIM DA CORREÇÃO ---
                    }
                }
            }
        }
        private void CleanupSprites()
        {
            GameEngine.AllSprites.RemoveAll(s => s.IsRemoved);
            GameEngine.AllEnemies.RemoveAll(e => e.IsRemoved);
            GameEngine.PlayerBullets.RemoveAll(b => b.IsRemoved);
            GameEngine.EnemyBullets.RemoveAll(b => b.IsRemoved);

            if (GameEngine.Player != null && GameEngine.Player.IsRemoved)
            {
                GameEngine.Player = null;
            }
        }

        public void DrawWorld(SpriteBatch spriteBatch)
        {
            foreach (var sprite in GameEngine.AllSprites)
            {
                if (!sprite.IsRemoved)
                    sprite.Draw(spriteBatch);
            }

            if (GameEngine.Player != null)
                GameEngine.Player.ExplosionEffect.Draw(spriteBatch); // Explosão do player

            _hitExplosion.Draw(spriteBatch); // Explosão de acerto no inimigo
        }
        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // O código de desenhar sprites foi movido para DrawWorld()

            var font = GameEngine.Assets.Fonts["captain_32"];

            // Usa coordenadas de tela
            Vector2 levelPos = new Vector2(Constants.InternalResolution.X - 100, 30);
            Vector2 lifePos = new Vector2(Constants.InternalResolution.X - 100, 60);

            // Usa a nova função DrawTextScreen
            DrawUtils.DrawTextScreen(spriteBatch, $"Level {GameEngine.Level}", font, levelPos, Color.White, 1.0f);
            if (GameEngine.Player != null)
                DrawUtils.DrawTextScreen(spriteBatch, $"Life  {GameEngine.Player.Life}", font, lifePos, Color.White, 1.0f);
            else
                DrawUtils.DrawTextScreen(spriteBatch, "Life  0", font, lifePos, Color.Red, 1.0f);
        }
    }
}