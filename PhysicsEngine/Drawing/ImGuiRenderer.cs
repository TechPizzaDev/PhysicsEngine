using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Hexa.NET.ImGui;
using MonoGame.Framework;
using MonoGame.Framework.Graphics;
using MonoGame.Framework.Input;
using MonoGame.Imaging;
using PhysicsEngine.Memory;

namespace PhysicsEngine.Drawing;

public class ImGuiRenderer
{
    public static readonly VertexDeclaration VertDecl = new(
        Unsafe.SizeOf<ImDrawVert>(),
        // Position
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        // UV
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        // Color
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    private Game _game;

    // Graphics
    private GraphicsDevice _graphicsDevice;

    private BasicEffect _effect;
    private RasterizerState _rasterizerState;

    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;

    private nint? _fontTextureId;

    // Input
    private Vector2 _scrollValue;
    private readonly float WHEEL_DELTA = 120;
    private Keys[] _allKeys = Enum.GetValues<Keys>();

    public ImGuiRenderer(Game game)
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasTextures;

        _game = game ?? throw new ArgumentNullException(nameof(game));
        _graphicsDevice = game.GraphicsDevice;

        _rasterizerState = new RasterizerState()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0
        };

        SetupInput();
    }

    #region ImGuiRenderer

    /// <summary>
    /// Creates a texture and loads the font data from ImGui. 
    /// Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
    /// </summary>
    public unsafe void RebuildFontAtlas()
    {
        /*
        // Get font texture from ImGui
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        // Create and register the texture as an XNA texture
        var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Rgba32);
        tex2d.SetData(new ReadOnlySpan<byte>(pixelData, width * height * bytesPerPixel));

        // Should a texture already have been build previously, unbind it first so it can be deallocated
        if (_fontTextureId.HasValue)
            UnbindTexture(_fontTextureId.Value);

        // Bind the new texture to an ImGui-friendly id
        _fontTextureId = BindTexture(tex2d);

        // Let ImGui know where to find the texture
        io.Fonts.SetTexID(_fontTextureId.Value);
        io.Fonts.ClearTexData(); // Clears CPU side texture data
        */
    }

    /// <summary>
    /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. 
    /// That pointer is then used by ImGui to let us know what texture to draw
    /// </summary>
    public ImTextureID BindTexture(Texture2D texture)
    {
        return GCHandle.ToIntPtr(GCHandle.Alloc(texture));
    }

    public Texture2D GetTexture(ImTextureID id)
    {
        if (GCHandle.FromIntPtr(id).Target is Texture2D tex)
        {
            return tex;
        }
        throw new InvalidOperationException();
    }

    /// <summary>
    /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
    /// </summary>
    public Texture2D UnbindTexture(ImTextureID id)
    {
        GCHandle handle = GCHandle.FromIntPtr(id);
        Texture2D? tex = handle.Target as Texture2D;
        handle.Free();

        if (tex == null)
            throw new InvalidOperationException();
        return tex;
    }

    /// <summary>
    /// Sets up ImGui for a new frame, should be called at frame start
    /// </summary>
    public void BeginLayout(in InputState input, in FrameTime time, Vector2 displaySize, Vector2 framebufferScale)
    {
        ImGui.GetIO().DeltaTime = time.ElapsedTotalSeconds;

        UpdateInput(input, displaySize, framebufferScale);

        ImGui.NewFrame();
    }

    /// <summary>
    /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, 
    /// should be called after the UI is drawn using ImGui.** calls
    /// </summary>
    public void EndLayout()
    {
        ImGui.EndFrame();
    }

    public void Draw()
    {
        ImGui.Render();

        ImDrawDataPtr drawData = ImGui.GetDrawData();

        // Catch up with texture updates. Most of the times, the list will have 1 element with an OK status, aka nothing to do.
        // (This almost always points to ImGui::GetPlatformIO().Textures[] but is part of ImDrawData to allow overriding or disabling texture updates).
        foreach (ImTextureDataPtr tex in drawData.Textures.AsSpan())
        {
            if (tex.Status != ImTextureStatus.Ok)
                UpdateTexture(tex);
        }

        RenderDrawData(drawData);
    }

    #endregion ImGuiRenderer

    #region Setup & Update

    /// <summary>
    /// Setup key input event handler.
    /// </summary>
    protected void SetupInput()
    {
        var io = ImGui.GetIO();

        _game.Window.TextInput += (s, a) =>
        {
            if (a.Character == new Rune('\t'))
                return;

            io.AddInputCharacter((uint) a.Character.Value);
        };
    }

    /// <summary>
    /// Updates the <see cref="Effect" /> to the current matrices and texture
    /// </summary>
    protected Effect UpdateEffect()
    {
        _effect ??= new BasicEffect(_graphicsDevice);

        var io = ImGui.GetIO();

        _effect.World = Matrix4x4.Identity;
        _effect.View = Matrix4x4.Identity;
        _effect.Projection = Matrix4x4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        _effect.TextureEnabled = true;
        _effect.VertexColorEnabled = true;

        return _effect;
    }

    protected void SetTexture(Effect effect, Texture2D texture)
    {
        ((BasicEffect) effect).Texture = texture;
    }

    /// <summary>
    /// Sends XNA input state to ImGui
    /// </summary>
    protected void UpdateInput(in InputState input, Vector2 displaySize, Vector2 framebufferScale)
    {
        if (!_game.IsActive)
            return;

        var mouse = input.NewMouseState;
        var keyboard = input.NewKeyState;

        var io = ImGui.GetIO();
        io.AddMousePosEvent(mouse.X, mouse.Y);
        io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
        io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
        io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

        Vector2 scroll = (mouse.Scroll - _scrollValue) / WHEEL_DELTA;
        io.AddMouseWheelEvent(scroll.X, scroll.Y);
        _scrollValue = mouse.Scroll;

        foreach (Keys key in _allKeys)
        {
            if (TryMapKeys(key, out ImGuiKey imguikey))
            {
                io.AddKeyEvent(imguikey, keyboard.IsKeyDown(key));
            }
        }

        io.DisplaySize = displaySize / framebufferScale;
        io.DisplayFramebufferScale = framebufferScale;
    }

    private bool TryMapKeys(Keys key, out ImGuiKey imguikey)
    {
        //Special case not handed in the switch...
        //If the actual key we put in is "None", return none and true. 
        //otherwise, return none and false.
        if (key == Keys.None)
        {
            imguikey = ImGuiKey.None;
            return true;
        }

        imguikey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey.Key0 + (key - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift => ImGuiKey.ModShift,
            Keys.LeftControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt => ImGuiKey.ModAlt,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            _ => ImGuiKey.None,
        };

        return imguikey != ImGuiKey.None;
    }

    #endregion Setup & Update

    #region Internals

    /// <summary>
    /// Gets the geometry as set up by ImGui and sends it to the graphics device
    /// </summary>
    private void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled
        var lastViewport = _graphicsDevice.Viewport;
        var lastScissorBox = _graphicsDevice.ScissorRectangle;

        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        var io = ImGui.GetIO();
        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Setup projection
        var displaySize = io.DisplaySize;
        _graphicsDevice.Viewport = new Viewport(0, 0, (int) displaySize.X, (int) displaySize.Y);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);

        // Restore modified state
        _graphicsDevice.Viewport = lastViewport;
        _graphicsDevice.ScissorRectangle = lastScissorBox;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
        if (_vertexBuffer == null || drawData.TotalVtxCount > _vertexBuffer.Capacity)
        {
            _vertexBuffer?.Dispose();

            int vertexBufferSize = (int) (drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new VertexBuffer(_graphicsDevice, VertDecl, vertexBufferSize, BufferUsage.None);
        }

        if (_indexBuffer == null || drawData.TotalIdxCount > _indexBuffer.Capacity)
        {
            _indexBuffer?.Dispose();

            int indexBufferSize = (int) (drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementType.Int16, indexBufferSize, BufferUsage.None);
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];
            ReadOnlySpan<ImDrawVert> vtxSpan = new(cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size);
            ReadOnlySpan<ushort> idxSpan = new(cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size);

            _vertexBuffer.SetData(vtxOffset * VertDecl.VertexStride, vtxSpan);
            _indexBuffer.SetData(idxOffset * sizeof(ushort), idxSpan);

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        Debug.Assert(vtxOffset == drawData.TotalVtxCount);
        Debug.Assert(idxOffset == drawData.TotalIdxCount);
    }

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData)
    {
        Effect effect = UpdateEffect();

        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmd* cmd_p = &cmdList.CmdBuffer.Data[cmd_i];
                if (cmd_p->UserCallback != null)
                {
                    var fn = (delegate*<ImDrawList*, ImDrawCmd*, void>) cmd_p->UserCallback;
                    fn(cmdList, cmd_p);
                    continue;
                }

                ref ImDrawCmd cmd = ref *cmd_p;
                if (cmd.ElemCount == 0)
                {
                    continue;
                }

                var texId = cmd.GetTexID();
                var tex = (Texture2D) GCHandle.FromIntPtr(texId).Target;

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int) cmd.ClipRect.X,
                    (int) cmd.ClipRect.Y,
                    (int) (cmd.ClipRect.Z - cmd.ClipRect.X),
                    (int) (cmd.ClipRect.W - cmd.ClipRect.Y)
                );

                SetTexture(effect, tex);

                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        baseVertex: (int) cmd.VtxOffset + vtxOffset,
                        startIndex: (int) cmd.IdxOffset + idxOffset,
                        primitiveCount: (int) cmd.ElemCount / 3
                    );
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    private unsafe void UpdateTexture(ImTextureDataPtr tex)
    {
        if (tex.Status == ImTextureStatus.WantCreate)
        {
            //IMGUI_DEBUG_LOG("UpdateTexture #%03d: WantCreate %dx%d\n", tex->UniqueID, tex->Width, tex->Height);
            Debug.Assert(tex.TexID.IsNull && tex.BackendUserData == null);
            Debug.Assert(tex.Format == ImTextureFormat.Rgba32);

            Texture2D backend_tex = new(_graphicsDevice, tex.Width, tex.Height);
            backend_tex.Name = $"ImTextureData [0x{(nuint) tex.Handle:X}]";
            
            uint* pixels = (uint*) tex.GetPixels();
            backend_tex.SetData(new Span<byte>(pixels, tex.GetSizeInBytes()));

            tex.SetTexID(BindTexture(backend_tex));
            tex.SetStatus(ImTextureStatus.Ok);
        }
        else if (tex.Status == ImTextureStatus.WantUpdates)
        {
            Texture2D backend_tex = GetTexture(tex.GetTexID());

            int pitch = tex.GetPitch();
            int bpp = tex.BytesPerPixel;
            foreach (ImTextureRect r in tex.Updates.AsSpan())
            {
                var box = new Rectangle(r.X, r.Y, r.W, r.H);
                int len = r.W * bpp + pitch * (r.H - 1);
                var span = new Span<byte>(tex.GetPixelsAt(r.X, r.Y), len);
                backend_tex.SetData(span, box, 0, 0, pitch);
            }
            tex.SetStatus(ImTextureStatus.Ok);
        }

        if (tex.Status == ImTextureStatus.WantDestroy && tex.UnusedFrames > 0)
        {
            UnbindTexture(tex.GetTexID()).Dispose();
            tex.SetTexID(ImTextureID.Null);
            tex.SetStatus(ImTextureStatus.Destroyed);
        }
    }

    #endregion Internals
}
