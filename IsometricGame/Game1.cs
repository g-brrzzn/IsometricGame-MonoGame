using IsometricGame.Classes;
using IsometricGame.Classes.Particles;
using IsometricGame.States;
using IsometricGame.States.Editor;
using IsometricGame.States.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IsometricGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public static GraphicsDeviceManager _graphicsManagerInstance;
        private GameStateBase _currentState;
        private readonly Dictionary<string, GameStateBase> _states = new();

        public static InputManager InputManagerInstance { get; private set; }

        private RenderTarget2D _renderTarget;
        private Rectangle _renderDestination;
        private Vector2 _screenShakeOffset = Vector2.Zero;
        private double _frameCounter;
        private double _frameTimer;
        private string _fpsDisplay = "";
        public static Game1 Instance { get; private set; }
        public static Camera Camera { get; private set; }
        public static Fall MenuBackgroundFall { get; private set; }

        public Game1()
        {
            Instance = this;
            _graphics = new GraphicsDeviceManager(this);
            _graphicsManagerInstance = _graphics;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = Constants.WindowSize.X;
            _graphics.PreferredBackBufferHeight = Constants.WindowSize.Y;
            _graphics.IsFullScreen = Constants.SetFullscreen;

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / Constants.FrameRate);
        }

        public static void ApplySettings(Point newResolution, bool fullscreen)
        {
            _graphicsManagerInstance.PreferredBackBufferWidth = newResolution.X;
            _graphicsManagerInstance.PreferredBackBufferHeight = newResolution.Y;
            _graphicsManagerInstance.IsFullScreen = fullscreen;
            _graphicsManagerInstance.ApplyChanges();
        }

        private void CalculateRenderDestination()
        {
            var backBuffer = new Point(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            if (Constants.InternalResolution.X <= 0 || Constants.InternalResolution.Y <= 0)
            {
                _renderDestination = new Rectangle(0, 0, backBuffer.X, backBuffer.Y);
                return;
            }

            float scaleX = (float)backBuffer.X / Constants.InternalResolution.X;
            float scaleY = (float)backBuffer.Y / Constants.InternalResolution.Y;
            float scale = Math.Min(scaleX, scaleY);

            int renderWidth = (int)(Constants.InternalResolution.X * scale);
            int renderHeight = (int)(Constants.InternalResolution.Y * scale);

            _renderDestination = new Rectangle(
                (backBuffer.X - renderWidth) / 2,
                (backBuffer.Y - renderHeight) / 2,
                renderWidth,
                renderHeight
            );
        }

        protected override void Initialize()
        {
            _renderTarget = new RenderTarget2D(
                GraphicsDevice,
                Constants.InternalResolution.X,
                Constants.InternalResolution.Y
            );

            CalculateRenderDestination();
            Window.ClientSizeChanged += (s, e) => CalculateRenderDestination();

            GameEngine.Initialize();

            InputManagerInstance = new InputManager();

            Camera = new Camera(Constants.InternalResolution.X, Constants.InternalResolution.Y);
            MenuBackgroundFall = new Fall(150);
            Camera.SetZoom(2.0f);

            _states.Add("Menu", new MenuState());
            _states.Add("Game", new GameplayState());
            _states.Add("Pause", new PauseState());
            _states.Add("GameOver", new GameOverState());
            _states.Add("Options", new OptionsState());
            _states.Add("ExitConfirm", new ExitConfirmState());
            _states.Add("Editor", new EditorState());
            _states.Add("TextInput", new TextInputState());

            _states.Add("LevelUp", new LevelUpState());

            _currentState = _states["Menu"];
            _currentState.Start();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            GameEngine.Assets.LoadContent(Content, GraphicsDevice);
            var pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            if (GameEngine.Assets.Images.ContainsKey("pixel"))
                GameEngine.Assets.Images["pixel"] = pixel;
            else
                GameEngine.Assets.Images.Add("pixel", pixel);

            Explosion.PixelTexture = pixel;

            Bullet.LoadAssets(GameEngine.Assets);
            EnemyBase.LoadAssets(GameEngine.Assets);
            if (GameEngine.Assets.Music != null)
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Volume = 0.01f;
                MediaPlayer.Play(GameEngine.Assets.Music);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            InputManagerInstance.SetScreenConversion(_renderDestination, Constants.InternalResolution);
            InputManagerInstance.Update();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.F4))
            {
                Exit();
                return;
            }
            MenuBackgroundFall.Update(dt: (float)gameTime.ElapsedGameTime.TotalSeconds);
            GameStateBase previousState = _currentState;
            _currentState.Update(gameTime, InputManagerInstance);

            if (_currentState.IsDone)
            {
                if (previousState is GameplayState gameplayState)
                {
                    gameplayState.End();
                }
                if (!string.IsNullOrEmpty(_currentState.NextState))
                {
                    if (_currentState.NextState == "Exit") { Exit(); return; }
                    else if (_states.ContainsKey(_currentState.NextState))
                    {
                        _currentState = _states[_currentState.NextState];
                        _currentState.Start();
                    }
                    else { _currentState = _states["Menu"]; _currentState.Start(); }
                }
                else { _currentState = _states["Menu"]; _currentState.Start(); }
            }
            if (GameEngine.ScreenShake > 0) { GameEngine.ScreenShake--; _screenShakeOffset.X = GameEngine.Random.Next(-4, 5); _screenShakeOffset.Y = GameEngine.Random.Next(-4, 5); } else { _screenShakeOffset = Vector2.Zero; }
            if (_graphics.PreferredBackBufferWidth != Constants.WindowSize.X || _graphics.PreferredBackBufferHeight != Constants.WindowSize.Y || _graphics.IsFullScreen != Constants.SetFullscreen) { CalculateRenderDestination(); }

            if ((_currentState is GameplayState || _currentState is LevelUpState) && GameEngine.Player != null)
            {
                Camera.Follow(GameEngine.Player.ScreenPosition);
            }
            else if (!(_currentState is GameplayState))
            {
                Camera.Follow(Vector2.Zero);
            }

            _frameCounter++;
            _frameTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_frameTimer >= 1)
            {
                _fpsDisplay = $"FPS: {_frameCounter}";
                _frameCounter = 0;
                _frameTimer -= 1;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Constants.BackgroundColor);

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            if (_currentState is MenuState || _currentState is OptionsState || _currentState is GameOverState || _currentState is EditorState)
            {
                Color fallColor = Color.DarkSlateGray;
                if (_currentState is GameOverState) fallColor = Constants.GameColor;
                else if (!(_currentState is EditorState)) fallColor = Color.LightGray;

                MenuBackgroundFall.Draw(_spriteBatch, fallColor);
            }
            _spriteBatch.End();


            _spriteBatch.Begin(SpriteSortMode.BackToFront,
                               BlendState.AlphaBlend,
                               SamplerState.PointClamp,
                               null, null, null,
                               Camera.GetViewMatrix());

            if (_currentState is GameplayState gameplayStateWorld)
            {
                gameplayStateWorld.DrawWorld(_spriteBatch);
            }
            else if (_currentState is LevelUpState)
            {
                foreach (var sprite in GameEngine.AllSprites)
                {
                    if (sprite != null && !sprite.IsRemoved) sprite.Draw(_spriteBatch);
                }
            }
            else if (_currentState is EditorState editorStateWorld)
            {
                editorStateWorld.DrawWorld(_spriteBatch);
            }

            _spriteBatch.End();


            _spriteBatch.Begin(SpriteSortMode.Deferred,
                               BlendState.AlphaBlend,
                               SamplerState.PointClamp);
            _currentState.Draw(_spriteBatch, GraphicsDevice);
            if (Constants.ShowFPS && !string.IsNullOrEmpty(_fpsDisplay))
            {
                DrawUtils.DrawTextScreen(_spriteBatch, _fpsDisplay, GameEngine.Assets.Fonts["captain_32"], new Vector2(15, 10), Color.White, 0.0f);
            }

            _spriteBatch.End();

            if (_currentState is GameplayState gameplayStateOverlay)
            {
                _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
                gameplayStateOverlay.DrawTransitionOverlay(_spriteBatch, GraphicsDevice);
                _spriteBatch.End();
            }


            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            Rectangle destination = _renderDestination;
            destination.Offset(_screenShakeOffset);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_renderTarget, destination, Color.White);
            _spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}