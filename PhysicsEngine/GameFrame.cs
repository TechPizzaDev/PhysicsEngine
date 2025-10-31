using Hexa.NET.ImGui;
using MonoGame.Framework;
using MonoGame.Framework.Audio;
using MonoGame.Framework.Graphics;
using MonoGame.Framework.Input;
using PhysicsEngine.Collections;
using PhysicsEngine.Drawing;
using PhysicsEngine.Levels;
using System;
using System.IO;
using System.Numerics;
using System.Text;

namespace PhysicsEngine
{
    public partial class GameFrame : Game
    {
        struct RenderTarget
        {
            public RenderTarget2D? Texture;
            public float Scale;
            public DepthFormat DepthFormat;
        }

        public bool DrawDebugInfo = true;

        public float MinZoom = 0.125f;
        public float MaxZoom = 40f;

        private GraphicsDeviceManager _graphicsManager;
        private SpriteBatch _spriteBatch;

        private ImGuiRenderer _imguiRenderer;

        private StringBuilder _debugBuilder = new();

        private AssetRegistry _assets;

        private EnumMap<RenderPass, RenderTarget> _renderTargets;

        private Viewport _lastViewport;
        private InputState _inputState;

        private Vector2 _lastScroll;
        private Vector2 _scrollDelta;

        private Vector2 _lastMousePos;
        private Vector2 _mousePosDelta;

        private Matrix4x4 _playerMatrix;
        private Matrix4x4 _sceneTransformMatrix;
        private Matrix4x4 _inverseSceneTransform;
        private Matrix4x4 _sceneRenderMatrix;
        private Matrix4x4 _uiRenderMatrix;

        private Vector2 _cameraTarget;
        private float _scale = 1.0f;

        private Random _worldRng;
        public World _world;

        private WorldFactory[] _worldFactories =
        [
            WorldFactory.New<SandboxWorld>(),
            WorldFactory.New<Exercise1>(),
            WorldFactory.New<Exercise2>(),
        ];
        private int _selectedWorldFactory = 0;

        public GameFrame()
        {
            SoundEffect.Initialize();

            _graphicsManager = new GraphicsDeviceManager(this);
            _graphicsManager.HardwareModeSwitch = false;

            Content.RootDirectory = "Content";

            _renderTargets = new();
            _renderTargets.Fill(new RenderTarget() { Scale = 2f, DepthFormat = DepthFormat.Depth24Stencil8 });
            _renderTargets[RenderPass.Background].Scale = 0f;
            _renderTargets[RenderPass.UI].DepthFormat = DepthFormat.None;
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            IsMouseVisible = true;

            base.Initialize();

            _imguiRenderer = new ImGuiRenderer(this);
            unsafe
            {
                string? str = System.Runtime.InteropServices.Marshal.PtrToStringUTF8((nint) ImGui.GetVersion());
                Console.WriteLine("ImGui Version: " + str);
            }

            ImFontConfig config;
            config.FontDataOwnedByAtlas = 1;
            config.GlyphMinAdvanceX = 13.0f; // monospaced
            config.GlyphMaxAdvanceX = float.MaxValue;
            config.MergeMode = 1;
            config.RasterizerMultiply = 1.0f;
            config.RasterizerDensity = 1.0f;
            config.GlyphOffset = new Vector2(0, 1f);

            string path = Path.Combine(Content.RootDirectory, "Font Awesome 7 Free-Solid-900.otf");
            unsafe
            {
                var io = ImGui.GetIO();
                io.Fonts.AddFontDefault();
                io.Fonts.AddFontFromFileTTF(path, 13.0f, &config);
            }
            _imguiRenderer.RebuildFontAtlas();

            _lastViewport = GraphicsDevice.Viewport;
            ViewportChanged(_lastViewport);

            MouseState mouseState = Mouse.GetState();
            _lastScroll = mouseState.Scroll;
            _lastMousePos = mouseState.Position;

            ResetLevel();
        }

        private void ResetLevel()
        {
            _worldRng = new Random(1234);
            _world = _worldFactories[_selectedWorldFactory].Factory.Invoke(_worldRng);
            _cameraTarget = new(0, 0);
        }

        private void PrevLevel()
        {
            if (_selectedWorldFactory == 0)
                _selectedWorldFactory = _worldFactories.Length;
            _selectedWorldFactory--;
            ResetLevel();
        }

        private void NextLevel()
        {
            _selectedWorldFactory++;
            if (_selectedWorldFactory == _worldFactories.Length)
                _selectedWorldFactory = 0;
            ResetLevel();
        }

        private void PlayPauseLevel()
        {
            _world.TimeScale = -_world.TimeScale;
        }

        private void ViewportChanged(in Viewport viewport)
        {
            foreach (ref RenderTarget target in _renderTargets.AsSpan())
            {
                target.Texture?.Dispose();

                if (target.Scale <= 0f)
                    continue;

                Size size = new(
                    (int) (viewport.Width * target.Scale),
                    (int) (viewport.Height * target.Scale));
                target.Texture = CreateRenderTarget(size, target.DepthFormat);
            }
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

        #region Update

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
                ResetLevel();
            }

            if (_inputState.IsKeyPressed(Keys.Space))
            {
                PlayPauseLevel();
            }

            if ((_inputState.Modifiers & KeyModifiers.Alt) != 0)
            {
                if (_inputState.IsKeyPressed(Keys.Left))
                {
                    PrevLevel();
                }
                else if (_inputState.IsKeyPressed(Keys.Right))
                {
                    NextLevel();
                }
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
                _scale = Math.Clamp(_scale + scaleDelta, MinZoom, MaxZoom);

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
                Matrix4x4.CreateTranslation(_lastViewport.Width / 2f, _lastViewport.Height / 2f, 0);

            _sceneRenderMatrix =
                _sceneTransformMatrix *
                Matrix4x4.CreateScale(_renderTargets[RenderPass.Scene].Scale);

            var uiTarget = _renderTargets[RenderPass.UI];
            _uiRenderMatrix =
                _sceneTransformMatrix *
                Matrix4x4.CreateScale(uiTarget.Scale);

            Matrix4x4.Invert(_sceneTransformMatrix, out _inverseSceneTransform);

            UpdateState state = new()
            {
                Input = input,
                Time = time,
                Scale = _scale,
                InverseSceneTransform = _inverseSceneTransform,
            };

            Vector2 displaySize = uiTarget.Texture?.Size.ToVector2() ?? Vector2.Zero;
            _imguiRenderer.BeginLayout(input, time, displaySize, new Vector2(1f));

            ImGuiUpdate();

            _world.Update(state);

            _imguiRenderer.EndLayout();

            base.Update(time);
        }

        #endregion

        #region ImGui

        private void ImGuiUpdate()
        {
            if (ImGui.Begin("Levels"))
            {
                ImGuiLevels();
            }
            ImGui.End();
        }

        private void ImGuiLevels()
        {
            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("##level", _worldFactories[_selectedWorldFactory].Name))
            {
                for (int i = 0; i < _worldFactories.Length; i++)
                {
                    bool is_selected = _selectedWorldFactory == i;

                    if (ImGui.Selectable(_worldFactories[i].Name, is_selected))
                        _selectedWorldFactory = i;

                    if (is_selected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGui.SetItemTooltip("Level Selector");

            ImGui.SameLine();
            if (ImGui.ArrowButton("##prev_level", ImGuiDir.Left))
            {
                PrevLevel();
            }
            ImGui.SetItemTooltip("Previous Level");

            ImGui.SameLine();
            if (ImGui.ArrowButton("##next_level", ImGuiDir.Right))
            {
                NextLevel();
            }
            ImGui.SetItemTooltip("Next Level");

            ImGui.SameLine();
            if (ImGui.Button(FontAwesome7.ArrowsRotate))
            {
                ResetLevel();
            }
            ImGui.SetItemTooltip("Reload Level");

            ImGui.SameLine();
            int PlayPauseButton(Utf8Span icon, string tooltip)
            {
                if (ImGui.Button(icon))
                {
                    PlayPauseLevel();
                }
                ImGui.SetItemTooltip(tooltip);
                return 0;
            }

            _ = _world.TimeScale switch
            {
                < 0f => PlayPauseButton(FontAwesome7.CirclePlay, "Play"),
                > 0f => PlayPauseButton(FontAwesome7.CirclePause, "Pause"),
                _ => PlayPauseButton(FontAwesome7.CircleStop, "Timeless"),
            };
        }

        #endregion

        #region Draw

        protected override void Draw(in FrameTime time)
        {
            Viewport currentViewport = GraphicsDevice.Viewport;
            if (_lastViewport != currentViewport)
            {
                ViewportChanged(currentViewport);
                _lastViewport = currentViewport;
            }

            DrawState state = new()
            {
                Assets = _assets,
                SpriteBatch = _spriteBatch,
                Scale = _scale,
                Time = time,
                Viewport = currentViewport,
            };

            DoRenderPass(ref state, RenderPass.Background, _sceneRenderMatrix);
            DoRenderPass(ref state, RenderPass.Scene, _sceneRenderMatrix);

            DoRenderPass(ref state, RenderPass.UI, _uiRenderMatrix);
            RenderUserInterface(state);

            GraphicsDevice.SetRenderTarget(null, Color.Black.ToVector4());

            _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
            RectangleF dstRect = currentViewport.Bounds;
            foreach (ref RenderTarget target in _renderTargets.AsSpan())
            {
                if (target.Texture != null)
                    _spriteBatch.Draw(target.Texture, dstRect, null, Color.White);
            }
            _spriteBatch.End();

            _imguiRenderer.Draw();

            base.Draw(time);
        }

        private void DoRenderPass(ref DrawState state, RenderPass pass, Matrix4x4 transform)
        {
            ref RenderTarget target = ref _renderTargets[pass];
            GraphicsDevice.SetRenderTarget(target.Texture, Color.Transparent.ToVector4());
            state.RenderScale = target.Scale;
            state.RenderPass = pass;

            state.SpriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.NonPremultiplied,
                samplerState: SamplerState.PointClamp,
                rasterizerState: RasterizerState.CullNone,
                transformMatrix: transform);

            Matrix4x4.Invert(state.SpriteBatch.SpriteEffect.GetFinalMatrix(), out Matrix4x4 invProj);
            Vector2 viewMin = Vector2.Transform(new Vector2(-1, 1), invProj);
            Vector2 viewMax = Vector2.Transform(new Vector2(1, -1), invProj);
            state.WorldViewport = RectangleF.FromPoints(viewMin, viewMax);

            _world.Draw(state);

            state.SpriteBatch.End();
        }

        [Obsolete]
        private void RenderUserInterface(in DrawState state)
        {
            state.SpriteBatch.Begin(
                blendState: BlendState.NonPremultiplied,
                transformMatrix: Matrix4x4.CreateScale(_renderTargets[RenderPass.UI].Scale));

            if (DrawDebugInfo)
            {
                DrawDebug(new Vector2(4, state.Viewport.Height - 74));
            }

            state.SpriteBatch.End();
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

        #endregion
    }
}