using MonoGame.Framework;
using MonoGame.Framework.Audio;
using MonoGame.Framework.Graphics;
using MonoGame.Framework.Input;
using System;
using System.Numerics;
using System.Text;

namespace PhysicsEngine
{
    public partial class GameFrame : Game
    {
        public bool DrawDebugInfo = true;

        public float MinZoom = 0.125f;
        public float MaxZoom = 4f;

        private GraphicsDeviceManager _graphicsManager;
        private SpriteBatch _spriteBatch;

        private ImGuiRenderer _imguiRenderer;

        private StringBuilder _debugBuilder = new();

        private AssetRegistry _assets;

        private float _backgroundRenderScale = 0.333f;
        private RenderTarget2D _backgroundTarget;

        private float _sceneRenderScale = 2f;
        private RenderTarget2D _sceneTarget;

        private float _uiRenderScale = 2f;
        private RenderTarget2D _uiTarget;

        private Viewport _lastViewport;

        private Vector2 _lastScroll;
        private Vector2 _scrollDelta;

        private Vector2 _lastMousePos;
        private Vector2 _mousePosDelta;

        private Matrix4x4 _playerMatrix;
        private Matrix4x4 _sceneTransformMatrix;
        private Matrix4x4 _inverseSceneTransformMatrix;
        private Matrix4x4 _sceneRenderMatrix;
        private Matrix4x4 _uiRenderMatrix;

        private Vector2 _cameraTarget = new(0, 200);
        private float _scale = 1.0f;

        private Random _worldRng = new(1234);
        public World _world;

        public GameFrame()
        {
            SoundEffect.Initialize();

            _graphicsManager = new GraphicsDeviceManager(this);
            _graphicsManager.HardwareModeSwitch = false;

            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            base.Initialize();

            _imguiRenderer = new ImGuiRenderer(this);
            _imguiRenderer.RebuildFontAtlas();

            _lastViewport = GraphicsDevice.Viewport;
            ViewportChanged(_lastViewport);

            MouseState mouseState = Mouse.GetState();
            _lastScroll = mouseState.Scroll;
            _lastMousePos = mouseState.Position;

            _world = new World(_worldRng);
        }

        private void ViewportChanged(in Viewport viewport)
        {
            _backgroundTarget?.Dispose();
            _backgroundTarget = CreateRenderTarget(
                new Size(
                    (int) (viewport.Width * _backgroundRenderScale),
                    (int) (viewport.Height * _backgroundRenderScale)),
                DepthFormat.Depth24Stencil8);

            _sceneTarget?.Dispose();
            _sceneTarget = CreateRenderTarget(
                new Size(
                    (int) (viewport.Width * _sceneRenderScale),
                    (int) (viewport.Height * _sceneRenderScale)),
                DepthFormat.Depth24Stencil8);

            _uiTarget?.Dispose();
            _uiTarget = CreateRenderTarget(
                new Size(
                    (int) (viewport.Width * _uiRenderScale),
                    (int) (viewport.Height * _uiRenderScale)),
                DepthFormat.None);

            Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
                0, viewport.Width, viewport.Height, 0, -1000, 1000);
        }

        private RenderTarget2D CreateRenderTarget(Size size, DepthFormat depthFormat)
        {
            return new RenderTarget2D(
                GraphicsDevice,
                size.Width,
                size.Height,
                false,
                SurfaceFormat.Rgba32,
                depthFormat,
                0,
                RenderTargetUsage.PlatformContents);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _assets = new AssetRegistry(Content);
        }

        protected override void UnloadContent()
        {
        }

        private InputState _inputState;

        protected override void Update(in FrameTime time)
        {
            MouseState mouseState = Mouse.GetState();
            KeyboardState keyState = Keyboard.GetState();

            _inputState = new InputState(
                _inputState.NewMouseState,
                mouseState,
                _inputState.NewKeyState,
                keyState);

            if (_inputState.IsKeyPressed(Keys.Escape))
            {
                Exit();
                return;
            }

            if (_inputState.IsKeyPressed(Keys.F11))
            {
                _graphicsManager.ToggleFullScreen();
                _graphicsManager.ApplyChanges();
            }

            if (_inputState.IsKeyPressed(Keys.F5))
            {
                _world = new World(_worldRng);
            }

            if (_inputState.NewKeyState.IsKeyDown(Keys.F6))
            {
                _world.SpawnCircle();
            }

            InputState input = _inputState;

            _scrollDelta = input.NewMouseState.Scroll - _lastScroll;
            _scrollDelta.X = -_scrollDelta.X;
            _lastScroll = input.NewMouseState.Scroll;

            _mousePosDelta = input.NewMouseState.Position - _lastMousePos;
            _lastMousePos = input.NewMouseState.Position;

            if ((input.NewKeyState.Modifiers & KeyModifiers.Control) != 0)
            {
                float scaleDelta = _scrollDelta.Y * 0.001f * ((_scale + 1) / 2f);
                _scale = MathHelper.Clamp(_scale + scaleDelta, MinZoom, MaxZoom);

                if ((input.NewMouseState.LeftButton & ButtonState.Pressed) != 0)
                    _cameraTarget += _mousePosDelta / new Vector2(_scale, -_scale);
            }
            else
            {
                if ((mouseState.MiddleButton & ButtonState.Pressed) != 0)
                    _cameraTarget += _mousePosDelta / new Vector2(_scale, -_scale);
            }

            _playerMatrix =
                Matrix4x4.CreateTranslation(new Vector3(_cameraTarget, 0)) *
                Matrix4x4.CreateScale(_scale, -_scale, _scale);

            _sceneTransformMatrix =
                _playerMatrix *
                Matrix4x4.CreateTranslation(_lastViewport.Width / 2f, _lastViewport.Height, 0);

            _sceneRenderMatrix =
                _sceneTransformMatrix *
                Matrix4x4.CreateScale(_sceneRenderScale);

            _uiRenderMatrix =
                _sceneTransformMatrix *
                Matrix4x4.CreateScale(_uiRenderScale);

            Matrix4x4.Invert(_sceneTransformMatrix, out _inverseSceneTransformMatrix);

            _imguiRenderer.BeginLayout(input, time, _lastViewport.Bounds.Size.ToVector2(), new Vector2(1));

            _world.Update(input, time, _inverseSceneTransformMatrix);

            _imguiRenderer.EndLayout();

            base.Update(time);
        }

        protected override void Draw(in FrameTime time)
        {
            Viewport currentViewport = GraphicsDevice.Viewport;
            if (_lastViewport != currentViewport)
            {
                ViewportChanged(currentViewport);
                _lastViewport = currentViewport;
            }

            GraphicsDevice.SetRenderTarget(_backgroundTarget, Color.Transparent.ToVector4());
            RenderBackground(_spriteBatch, time, currentViewport);

            GraphicsDevice.SetRenderTarget(_sceneTarget, Color.Transparent.ToVector4());
            RenderScene(_spriteBatch);

            GraphicsDevice.SetRenderTarget(_uiTarget, Color.Transparent.ToVector4());
            RenderUserInterface(_spriteBatch, time, currentViewport);

            GraphicsDevice.SetRenderTarget(null, Color.Black.ToVector4());

            _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
            RectangleF dstRect = currentViewport.Bounds;
            _spriteBatch.Draw(_backgroundTarget, dstRect, _backgroundTarget.Bounds, Color.White);
            _spriteBatch.Draw(_sceneTarget, dstRect, _sceneTarget.Bounds, Color.White);
            _spriteBatch.Draw(_uiTarget, dstRect, _uiTarget.Bounds, Color.White);
            _spriteBatch.End();

            _imguiRenderer.Draw();

            base.Draw(time);
        }

        private void RenderBackground(SpriteBatch spriteBatch, in FrameTime time, in Viewport viewport)
        {
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.NonPremultiplied,
                samplerState: SamplerState.PointClamp,
                rasterizerState: RasterizerState.CullClockwise,
                transformMatrix: _sceneRenderMatrix);

            _world.Draw(RenderPass.Background, _assets, spriteBatch, _scale);

            spriteBatch.End();
        }

        private void RenderScene(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.NonPremultiplied,
                samplerState: SamplerState.PointClamp,
                rasterizerState: RasterizerState.CullClockwise,
                transformMatrix: _sceneRenderMatrix);

            _world.Draw(RenderPass.Scene, _assets, spriteBatch, _scale);

            spriteBatch.End();
        }

        private void RenderUserInterface(
            SpriteBatch spriteBatch,
            in FrameTime time,
            in Viewport viewport)
        {
            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.NonPremultiplied,
                samplerState: SamplerState.PointClamp,
                transformMatrix: _uiRenderMatrix);

            _world.Draw(RenderPass.UserInterface, _assets, spriteBatch, 1f);

            spriteBatch.End();

            spriteBatch.Begin(
                blendState: BlendState.NonPremultiplied,
                transformMatrix: Matrix4x4.CreateScale(_uiRenderScale));

            if (DrawDebugInfo)
            {
                DrawDebug(new Vector2(4, viewport.Height - 74));
            }

            spriteBatch.End();
        }

        private void DrawDebug(Vector2 position)
        {
            long totalMem = Environment.WorkingSet;
            long gcMem = GC.GetTotalMemory(false);
            double totalMb = totalMem / (1024 * 1024.0);
            double gcMb = gcMem / (1024 * 1024.0);

            int gc0 = GC.CollectionCount(0);
            int gc1 = GC.CollectionCount(1);
            int gc2 = GC.CollectionCount(2);

            int totalMbDecimals = (int) Math.Max(0, 4 - Math.Log10(totalMb));
            int gcMbDecimals = (int) Math.Max(0, 3 - Math.Log10(gcMb));

            _debugBuilder.Append("GC Counts: [")
                .Append(gc0).Append(',')
                .Append(gc1).Append(',')
                .Append(gc2).Append(']').AppendLine();
            _debugBuilder.Append("GC Heap: ").Append(Math.Round(gcMb, gcMbDecimals)).AppendLine();
            _debugBuilder.Append("Memory: ").Append(Math.Round(totalMb, totalMbDecimals)).AppendLine();

            _spriteBatch.DrawShadedString(
                _assets.Font_Consolas, _debugBuilder, position, new Vector2(0.5f), Vector2.One, Color.Cyan, Color.Gray, Color.Black);

            _debugBuilder.Clear();
        }
    }
}