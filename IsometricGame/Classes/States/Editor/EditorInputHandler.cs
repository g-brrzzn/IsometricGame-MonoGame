
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace IsometricGame.States.Editor
{
	public class EditorInputHandler
	{
		private EditorState _editorState;
		public EditorInputHandler(EditorState editorState)
		{
			_editorState = editorState;
		}

		public void HandleInput(InputManager input, GameTime gameTime)
		{
			if (input.IsKeyPressed("ESC"))
			{
				_editorState.RequestExit();
				return;
			}

			HandleCameraInput(input, gameTime);

			if (input.IsKeyPressed("SWITCH_MODE"))
			{
				_editorState.SwitchEditorMode();
			}

			if (_editorState.GetCurrentMode() == EditorMode.Tiles)
			{
				HandleTileInput(input);
			}
			else			{
				HandleTriggerInput(input);
			}
			if (input.IsKeyDown("SAVE_MODIFIER") && input.IsKeyPressed("SAVE_ACTION"))
			{
				_editorState.RequestSaveMapWithPrompt();			}
			if (input.IsKeyDown("LOAD_MODIFIER") && input.IsKeyPressed("LOAD_ACTION"))
			{
				_editorState.RequestLoadMapWithPrompt();			}
			if (input.IsKeyDown("NEW_MODIFIER") && input.IsKeyPressed("NEW_ACTION"))
			{
				_editorState.RequestNewMapWithPrompt();			}
		}


		private void HandleCameraInput(InputManager input, GameTime gameTime)
		{
			float camSpeed = 250f * (float)gameTime.ElapsedGameTime.TotalSeconds / Game1.Camera.Zoom;
			Vector2 camMove = Vector2.Zero;
			if (input.IsKeyDown("LEFT")) camMove.X -= camSpeed;
			if (input.IsKeyDown("RIGHT")) camMove.X += camSpeed;
			if (input.IsKeyDown("UP")) camMove.Y -= camSpeed;
			if (input.IsKeyDown("DOWN")) camMove.Y += camSpeed;
			if (camMove != Vector2.Zero)
			{
				_editorState.MoveCamera(camMove);
			}

			if (input.IsKeyPressed("ZOOM_IN") || input.ScrollWheelDelta > 0)
				_editorState.ZoomCamera(1.15f);
			if (input.IsKeyPressed("ZOOM_OUT") || input.ScrollWheelDelta < 0)
				_editorState.ZoomCamera(1 / 1.15f);
		}

		private void HandleTileInput(InputManager input)
		{
			if (input.IsKeyPressed("NEXT_TILE")) { _editorState.SelectNextTileInPalette(); }
			if (input.IsKeyPressed("PREV_TILE")) { _editorState.SelectPreviousTileInPalette(); }

			Keys[] numberKeys = { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };
			for (int z = 0; z < numberKeys.Length; z++) { if (input.IsKeyPressed(numberKeys[z].ToString()) && z <= Constants.MaxZLevel) { _editorState.SetCurrentZLevel(z); break; } }

			if (input.IsLeftMouseButtonDown()) { _editorState.PlaceSelectedTileAtCursor(); }
			if (input.IsRightMouseButtonDown()) { _editorState.EraseTileAtCursor(); }
		}

		private void HandleTriggerInput(InputManager input)
		{
			if (input.IsLeftMouseButtonPressed()) { _editorState.SelectTriggerAtCursor(); }
			if (input.IsRightMouseButtonPressed())
			{
				_editorState.RequestAddTriggerAtCursor();			}

			if (input.IsKeyPressed("DELETE_TRIGGER")) { _editorState.RemoveSelectedTrigger(); }
			Keys[] numberKeys = { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };
			for (int z = 0; z < numberKeys.Length; z++) { if (input.IsKeyPressed(numberKeys[z].ToString()) && z <= Constants.MaxZLevel) { _editorState.SetCurrentZLevel(z); break; } }
		}
	}
}