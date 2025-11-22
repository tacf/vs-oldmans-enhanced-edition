using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using HarmonyLib;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

namespace OldMansEnhancedEdition.Features.UI;

#nullable disable
public class InteractionProgress(ICoreClientAPI api) : IFeature, IRenderer
{

    public double RenderOrder => 0.98;
    public int RenderRange => 10;

    public EnumAppSide Side => EnumAppSide.Client;


    private const int Color = ColorUtil.WhiteArgb;
    private const float AlphaIn = 0.2F;
    private const float AlphaOut = 0.4F;

    private const float Size = 48; // Outer size of square
    private const float Thickness = 3; // Outline thickness

    private MeshRef squareMesh = null;

    private float alpha = 0.0F;

    private float _progress;
    
    private bool _shouldRender = false;
    
    private bool shouldRender
    {
        get => _shouldRender;
        set
        {
            if (value)
            {
                alpha = float.Max(AlphaIn,
                    alpha); // even after shouldRender == false we hide only after fading out -- weird hack to start back up
            }

            _shouldRender = value;
        }
    }

    public bool Initialize()
    {
        if (ModConfig.Instance.InteractionProgresOverlay == false)
            return false;
        api.Event.RegisterRenderer(this, EnumRenderStage.Ortho);
        api.Event.RegisterGameTickListener(OnGameTick, 0);
        UpdateSquareMesh(1); // Unsure why but this not being here makes it crash at some point. 
        return true;
    }

    private void OnGameTick(float obj)
    {
        EntityPlayer pEntity = api.World.Player.Entity;
        BlockSelection blockSel = pEntity.BlockSelection;
        if (!pEntity.Controls.LeftMouseDown || blockSel == null)
        {
            shouldRender = false;
            return;
        }

        shouldRender = CanMeshBeRendered(blockSel);
    }

    private bool CanMeshBeRendered(BlockSelection blockSel)
    {
        Traverse traverse = Traverse.Create(api.World as ClientMain).Field("damagedBlocks");
        Dictionary<BlockPos, BlockDamage> blocksDamaged = (Dictionary<BlockPos, BlockDamage>)traverse.GetValue();
        float remainingResistance = 0f;
        if (blocksDamaged.TryGetValue(blockSel.Position, out BlockDamage blockDamage))
        {
            remainingResistance = blockDamage.RemainingResistance;
        }
        else
        {
            return false;
        }
        
        float remainingPercentage = remainingResistance / blockSel.Block.Resistance * 100;
        if (remainingPercentage == 0f)
            return false;
        _progress =  float.Lerp(remainingPercentage, _progress, 0.5f);
        return true;
    }

    private void UpdateSquareMesh(float progress)
    {
        MeshData data = new MeshData(64, 128, false, false, true, false);

        const float half = Size / 2f;

        // Path clockwise
        // To prevent overlaps we start segments at 'start + thickness' and stop at the edge
        // to prevent weirdness when turning and have a beveled tip, we ensure the tip only extends up to
        // the end of the segment.
        // a a a a a
        // d       b
        // d       b
        // d       b
        // c c c c b
        // ( Attempt to draw the logic in Ascii :D -- minus the shrinking tip -- sus)
        // segment 'a' is a special case as to not start offseted

        Vec2f[] outerVertex = new Vec2f[]
        {
            new Vec2f(-half, -half), // top-left
            new Vec2f(half, -half + Thickness), // top-right
            new Vec2f(half - Thickness, half), // bottom-right
            new Vec2f(-half, half - Thickness), // bottom-left
            new Vec2f(-half, -half) // close loop (top left) -- Unused in the loop logic
        };

        Vec2f[] outerEndVertex = new Vec2f[]
        {
            new Vec2f(-half, -half + Thickness), // top-left -- Unused in the loop logic
            new Vec2f(half, -half), // top-right
            new Vec2f(half, half), // bottom-right
            new Vec2f(-half, half), // bottom-left
            new Vec2f(-half, -half + Thickness) // close loop (top-left)
        };


        // Remove 4 * thickness since using the cut points
        float perimeter = Size * 4 - (4 * Thickness);
        float target = perimeter * ((100f - progress) / 100f);
        if (perimeter - target < 6f) target = perimeter;


        float acc = 0f;

        for (int i = 0; i < 4; i++)
        {
            Vec2f start = outerVertex[i];
            Vec2f end = outerEndVertex[i + 1];
            float segLen = (end - start).Length();


            if (acc + segLen <= target)
            {
                // This segment is fully filled
                AddSegment(data, start, end);
            }
            else
            {
                // Partially filled segment
                float remain = target - acc;

                if (remain > 0)
                {
                    Vec2f diff = end - start;
                    Vec2f dir = diff / diff.Length();
                    Vec2f cutPoint = start + dir * remain;
                    AddSegment(data, start, cutPoint, tipDiagonal: true);
                }

                break;
            }

            acc += segLen;
        }

        if (squareMesh != null) api.Render.UpdateMesh(squareMesh, data);
        else squareMesh = api.Render.UploadMesh(data);
    }

    /// <summary>
    /// Adds a quad segment of the square outline. If tipDiagonal=true,
    /// the end of the strip is cut with a diagonal triangle.
    /// </summary>
    private void AddSegment(MeshData data, Vec2f start, Vec2f end, bool tipDiagonal = false)
    {
        Vec2f diff = end - start;
        Vec2f dir = diff / diff.Length();
        Vec2f normal = new Vec2f(-dir.Y, dir.X); // outward normal

        // Outer and inner edge
        Vec2f sOut = start + normal * Thickness;
        Vec2f sIn = start;
        Vec2f eOut = end + normal * Thickness;
        Vec2f eIn = end;

        int baseIndex = data.VerticesCount;

        // Add four vertices
        data.AddVertexSkipTex(sOut.X, sOut.Y, 0);
        data.AddVertexSkipTex(sIn.X, sIn.Y, 0);
        data.AddVertexSkipTex(eOut.X, eOut.Y, 0);
        data.AddVertexSkipTex(eIn.X, eIn.Y, 0);

        // Add quad
        data.AddIndices(new[] { baseIndex, baseIndex + 1, baseIndex + 2 });
        data.AddIndices(new[] { baseIndex + 2, baseIndex + 1, baseIndex + 3 });

        // If this is the "tip", add a diagonal cut
        if (true) return;

        // TODO: Figure out the crashing from wrong indexing here
        
        // --- compute a tip point that lies AHEAD along the segment and OFFSET outward ---
        // capLen: how far forward along the segment the tip extends (never more than the segment)
        float capLen = Math.Min(Thickness, (end - start).Length() * 0.5f);
        // diag = inner end moved forward by capLen and slightly outward by half-thickness
        Vec2f diag = eOut + dir * capLen - normal * (Thickness);


        int tipIndex = data.VerticesCount;
        data.AddVertexSkipTex(diag.X, diag.Y, 0);

        data.AddIndices(new[] { baseIndex + 3, baseIndex + 2, tipIndex });
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (!shouldRender && alpha < AlphaIn) return;
        
        UpdateSquareMesh(_progress);
        
        IRenderAPI rend = api.Render;
        IShaderProgram shader = rend.CurrentActiveShader;
        
        alpha = Math.Max(0.0F, Math.Min(0.8F, alpha + (deltaTime / (shouldRender ? AlphaIn : -AlphaOut))));
        
        Vec4f color = ColorUtil.ToRGBAVec4f(Color);
        color.A = alpha;
        
        
        shader.Uniform("rgbaIn", color);
        shader.Uniform("extraGlow", 0);
        shader.Uniform("applyColor", 0);
        shader.Uniform("tex2d", 0);
        shader.Uniform("noTexture", 1.0F);
        shader.UniformMatrix("projectionMatrix", rend.CurrentProjectionMatrix);

        bool mg = api.Input.MouseGrabbed;

        int x = mg ? rend.FrameWidth / 2 : api.Input.MouseX;
        int y = mg ? rend.FrameHeight / 2 : api.Input.MouseY;

        rend.GlPushMatrix();
        rend.GlTranslate(x, y, 0);
        rend.GlScale(1, 1, 0);
        shader.UniformMatrix("modelViewMatrix", rend.CurrentModelviewMatrix);
        rend.GlPopMatrix();

        rend.RenderMesh(squareMesh);
    }

    public void Dispose()
    {
        Teardown();
    }


    public void Teardown()
    {
        if (squareMesh != null)
            api.Render.DeleteMesh(squareMesh);
    }
}