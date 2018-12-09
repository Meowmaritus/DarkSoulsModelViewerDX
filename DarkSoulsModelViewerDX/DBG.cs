﻿using DarkSoulsModelViewerDX.DebugPrimitives;
using DarkSoulsModelViewerDX.GFXShaders;
using MeowDSIO;
using MeowDSIO.DataFiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DarkSoulsModelViewerDX
{
    public class DBG
    {
        public static void CreateDebugPrimitives()
        {
            // This is the green grid, which is just hardcoded lel
            DbgPrim_Grid = new DbgPrimWireGrid(Color.Green, Color.Lime * 0.5f, 10, 1);
            DbgPrim_Grid.Transform = Transform.Default;


            // If you want to disable the grid on launch uncomment the next line.
            //ShowGrid = false;

            // If you want the menu to be CLOSED on launch uncomment the next line.
            DbgMenus.DbgMenuItem.MenuOpenState = DbgMenus.DbgMenuOpenState.Closed;


            // Put stuff below for testing:

           

        }

        

        private static List<IDbgPrim> Primitives = new List<IDbgPrim>();

        public static bool ShowPrimitiveNametags = true;
        public static bool ShowFancyTextLabels = false;
        public static float PrimitiveNametagSize = 1f;

        public static DbgPrimWireGrid DbgPrim_Grid;

        public static void ClearPrimitives()
        {
            foreach (var p in Primitives)
            {
                p.Dispose();
            }
            Primitives.Clear();
        }

        public static void AddPrimitive(IDbgPrim primitive)
        {
            Primitives.Add(primitive);
        }

        public static void DrawPrimitives()
        {
            if (ShowGrid)
                DbgPrim_Grid.Draw();

            foreach (var p in Primitives)
            {
                p.Draw();
            }

            GFX.SpriteBatch.Begin();
            if (ShowGrid)
                DbgPrim_Grid.LabelDraw();
            foreach (var p in Primitives)
            {
                if (ShowPrimitiveNametags)
                {
                    if (p.Name != null)
                        DrawTextOn3DLocation(p.Transform.Position, p.Name, p.NameColor, PrimitiveNametagSize, startAndEndSpriteBatchForMe: false);

                    p.LabelDraw();
                }
            }
            GFX.SpriteBatch.End();
        }

        public static bool ShowModelNames = true;
        public static bool ShowModelBoundingBoxes = false;
        public static bool ShowModelSubmeshBoundingBoxes = false;
        public static bool ShowGrid = true;

        public static SpriteFont DEBUG_FONT { get; private set; }
        const string DEBUG_FONT_NAME = "Content\\DbgMenuFont";

        public static SpriteFont DEBUG_FONT_SMALL { get; private set; }
        const string DEBUG_FONT_SMALL_NAME = "Content\\DbgMenuFontSmall";

        public static SpriteFont DEBUG_FONT_SIMPLE { get; private set; }
        const string DEBUG_FONT_SIMPLE_NAME = "Content\\DbgMenuFontSimple";



        private static VertexPositionColor[] DebugLinePositionBuffer = new VertexPositionColor[2];
        private static VertexBuffer DebugLineVertexBuffer;
        private static readonly int[] DebugLineIndexBuffer = new int[] { 0, 1 };

        private static VertexPositionColor[] DebugBoundingBoxPositionBuffer = new VertexPositionColor[8];
        private static VertexBuffer DebugBoundingBoxVertexBuffer;
        /*
         * 0 = Top Front Left
         * 1 = Top Front Right
         * 2 = Bottom Front Right
         * 3 = Bottom Front Left
         * 4 = Top Back Left
         * 5 = Top Back Right
         * 6 = Bottom Back Right
         * 7 = Bottom Back Left
         */
        private static readonly int[] DebugBoundingBoxIndexBuffer = new int[] 
        {
            0, 1, 1, 5, 5, 4, 4, 0, // Top Face
            2, 3, 3, 7, 7, 6, 6, 2, // Bottom Face
            0, 3, // Front Left Pillar
            1, 2, // Front Right Pillar
            4, 7, // Back Left Pillar
            5, 6, // Back Right Pillar
        };

        public static void LoadContent(ContentManager c)
        {
            DEBUG_FONT = c.Load<SpriteFont>(DEBUG_FONT_NAME);
            DEBUG_FONT_SMALL = c.Load<SpriteFont>(DEBUG_FONT_SMALL_NAME);
            DEBUG_FONT_SIMPLE = c.Load<SpriteFont>(DEBUG_FONT_SIMPLE_NAME);
        }

        //public static void DrawLine(Vector3 start, Vector3 end, Color startColor, Color? endColor = null, string startName = null, string endName = null)
        //{
        //    DebugLinePositionBuffer[0].Position = start;
        //    DebugLinePositionBuffer[0].Color = startColor;
        //    DebugLinePositionBuffer[1].Position = end;
        //    DebugLinePositionBuffer[1].Color = endColor ?? startColor;
        //    DebugLineIndexBuffer[0] = 0;
        //    DebugLineIndexBuffer[1] = 1;

        //    if (DebugLineVertexBuffer == null)
        //        DebugLineVertexBuffer = new VertexBuffer(GFX.Device, typeof(VertexPositionColor), 2, BufferUsage.None);

        //    GFX.World.ApplyViewToShader(GFX.DbgPrimShader);

        //    foreach (var pass in GFX.DbgPrimShader.Effect.CurrentTechnique.Passes)
        //    {
        //        pass.Apply();

        //        GFX.Device.SetVertexBuffer(DebugLineVertexBuffer);
        //        GFX.Device.DrawUserIndexedPrimitives(PrimitiveType.LineList,
        //            DebugLinePositionBuffer, 0, 2, DebugLineIndexBuffer, 0, 1);
        //    }

        //    if (startName != null)
        //    {
        //        DrawTextOn3DLocation(start, startName, startColor, 0.25f);
        //    }

        //    if (endName != null)
        //    {
        //        DrawTextOn3DLocation(end, endName, endColor ?? startColor, 0.25f);
        //    }
        //}

        public static void DrawTextOn3DLocation(Vector3 location, string text, Color color, float physicalHeight, bool startAndEndSpriteBatchForMe = true)
        {
            if (ShowFancyTextLabels)
                DrawTextOn3DLocation_Fancy(location, text, color, physicalHeight, startAndEndSpriteBatchForMe);
            else
                DrawTextOn3DLocation_Fast(location, text, color, physicalHeight, startAndEndSpriteBatchForMe);
        }

        private static void DrawTextOn3DLocation_Fast(Vector3 location, string text, Color color, float physicalHeight, bool startAndEndSpriteBatchForMe = true)
        {
            //// Project the 3d position first
            //Vector3 screenPos3D = GFX.Device.Viewport.Project(location,
            //    GFX.World.MatrixProjection, GFX.World.CameraTransform.CameraViewMatrix, GFX.World.MatrixWorld);

            //if (screenPos3D.Z >= 1 || screenPos3D.Z >= 1)
            //    return;

            //if (screenPos3D.X < 0 || screenPos3D.X > GFX.Device.Viewport.Width || screenPos3D.Y < 0 || screenPos3D.Y > GFX.Device.Viewport.Height)
            //    return;

            // Project the 3d position first
            Vector3 screenPos3D_Top = GFX.Device.Viewport.Project(location + new Vector3(0, physicalHeight / 2, 0),
                GFX.World.MatrixProjection, GFX.World.CameraTransform.CameraViewMatrix, GFX.World.MatrixWorld);

            Vector3 screenPos3D_Bottom = GFX.Device.Viewport.Project(location - new Vector3(0, physicalHeight / 2, 0),
                GFX.World.MatrixProjection, GFX.World.CameraTransform.CameraViewMatrix, GFX.World.MatrixWorld);

            //Vector3 camNormal = Vector3.Transform(Vector3.Forward, CAMERA_ROTATION);
            //Vector3 directionFromCam = Vector3.Normalize(location - Vector3.Transform(WORLD_VIEW.CameraPosition, CAMERA_WORLD));
            //var normDot = Vector3.Dot(directionFromCam, camNormal);

            if (screenPos3D_Top.Z >= 1 || screenPos3D_Bottom.Z >= 1)
                return;

            // Just to make it easier to use we create a Vector2 from screenPos3D
            Vector2 screenPos2D_Top = new Vector2(screenPos3D_Top.X, screenPos3D_Top.Y);
            Vector2 screenPos2D_Bottom = new Vector2(screenPos3D_Bottom.X, screenPos3D_Bottom.Y);
            Vector2 screenPos2D_Center = (screenPos2D_Top + screenPos2D_Bottom) / 2;

            if (screenPos2D_Center.X < 0 || screenPos2D_Center.X > GFX.Device.Viewport.Width || screenPos2D_Center.Y < 0 || screenPos2D_Center.Y > GFX.Device.Viewport.Height)
                return;

            Vector2 labelSpritefontSize = DEBUG_FONT.MeasureString(text);

            float labelHeightInPixels = (screenPos2D_Top - screenPos2D_Bottom).Length();



            //text += $"[DBG]{screenPos3D.Z}";

            float scale = labelHeightInPixels / labelSpritefontSize.Y;

            if (scale < 0.25f)
                return;

            //if (startAndEndSpriteBatchForMe)
            //    GFX.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);

            //GFX.SpriteBatch.DrawString(DEBUG_FONT_SIMPLE, text, 
            //    new Vector2(screenPos3D.X, screenPos3D.Y) - 
            //    new Vector2(GFX.Device.Viewport.X, GFX.Device.Viewport.Y), color);

            var textPos = new Vector2(screenPos2D_Center.X, screenPos2D_Center.Y) -
                new Vector2(GFX.Device.Viewport.X, GFX.Device.Viewport.Y);

            GFX.SpriteBatch.DrawString(DEBUG_FONT, text,
                new Vector2((int)textPos.X + 2, (int)textPos.Y + 2),
                Color.Black, 0, labelSpritefontSize / 2, scale, SpriteEffects.None,
                ((screenPos3D_Top.Z + screenPos3D_Bottom.Z) / 2) + 0.0001f);

            GFX.SpriteBatch.DrawString(DEBUG_FONT, text,
                new Vector2((int)textPos.X, (int)textPos.Y),
                color, 0, labelSpritefontSize / 2, scale, SpriteEffects.None,
                ((screenPos3D_Top.Z + screenPos3D_Bottom.Z) / 2));

            

            //if (startAndEndSpriteBatchForMe)
            //    GFX.SpriteBatch.End();

        }

        private static void DrawTextOn3DLocation_Fancy(Vector3 location, string text, Color color, float physicalHeight, bool startAndEndSpriteBatchForMe = true)
        {
            

            // Project the 3d position first
            Vector3 screenPos3D_Top = GFX.Device.Viewport.Project(location + new Vector3(0, physicalHeight / 2, 0),
                GFX.World.MatrixProjection, GFX.World.CameraTransform.CameraViewMatrix, GFX.World.MatrixWorld);

            Vector3 screenPos3D_Bottom = GFX.Device.Viewport.Project(location - new Vector3(0, physicalHeight / 2, 0),
                GFX.World.MatrixProjection, GFX.World.CameraTransform.CameraViewMatrix, GFX.World.MatrixWorld);

            //Vector3 camNormal = Vector3.Transform(Vector3.Forward, CAMERA_ROTATION);
            //Vector3 directionFromCam = Vector3.Normalize(location - Vector3.Transform(WORLD_VIEW.CameraPosition, CAMERA_WORLD));
            //var normDot = Vector3.Dot(directionFromCam, camNormal);

            if (screenPos3D_Top.Z >= 1 || screenPos3D_Bottom.Z >= 1)
                return;

            

            // Just to make it easier to use we create a Vector2 from screenPos3D
            Vector2 screenPos2D_Top = new Vector2(screenPos3D_Top.X, screenPos3D_Top.Y);

            Vector2 screenPos2D_Bottom = new Vector2(screenPos3D_Bottom.X, screenPos3D_Bottom.Y);

            Vector2 screenPos2D_Center = (screenPos2D_Top + screenPos2D_Bottom) / 2;

            if (screenPos2D_Center.X < 0 || screenPos2D_Center.X > GFX.Device.Viewport.Width || screenPos2D_Center.Y < 0 || screenPos2D_Center.Y > GFX.Device.Viewport.Height)
                return;

            Vector2 labelSpritefontSize = DEBUG_FONT.MeasureString(text);

            float labelHeightInPixels = (screenPos2D_Top - screenPos2D_Bottom).Length();



            //text += $"[DBG]{screenPos3D.Z}";

            float scale = labelHeightInPixels / labelSpritefontSize.Y;

            DrawOutlinedText(text, 
                (screenPos2D_Center) - new Vector2(GFX.Device.Viewport.X, GFX.Device.Viewport.Y),
                color, DEBUG_FONT,
                ((screenPos3D_Top.Z + screenPos3D_Bottom.Z) / 2), scale, labelSpritefontSize / 2, 
                startAndEndSpriteBatchForMe);
        }

        public static void DrawBoundingBox(BoundingBox bb, Color color, Transform transform)
        {
            DebugBoundingBoxPositionBuffer[0].Position = new Vector3(bb.Min.X, bb.Max.Y, bb.Max.Z);
            DebugBoundingBoxPositionBuffer[1].Position = new Vector3(bb.Max.X, bb.Max.Y, bb.Max.Z);
            DebugBoundingBoxPositionBuffer[2].Position = new Vector3(bb.Max.X, bb.Min.Y, bb.Max.Z);
            DebugBoundingBoxPositionBuffer[3].Position = new Vector3(bb.Min.X, bb.Min.Y, bb.Max.Z);
            DebugBoundingBoxPositionBuffer[4].Position = new Vector3(bb.Min.X, bb.Max.Y, bb.Min.Z);
            DebugBoundingBoxPositionBuffer[5].Position = new Vector3(bb.Max.X, bb.Max.Y, bb.Min.Z);
            DebugBoundingBoxPositionBuffer[6].Position = new Vector3(bb.Max.X, bb.Min.Y, bb.Min.Z);
            DebugBoundingBoxPositionBuffer[7].Position = new Vector3(bb.Min.X, bb.Min.Y, bb.Min.Z);

            for (int i = 0; i < DebugBoundingBoxPositionBuffer.Length; i++)
            {
                DebugBoundingBoxPositionBuffer[i].Color = color;
            }

            if (DebugBoundingBoxVertexBuffer == null)
                DebugBoundingBoxVertexBuffer = new VertexBuffer(GFX.Device,
                    typeof(VertexPositionColor), 8, BufferUsage.None);

            GFX.World.ApplyViewToShader(GFX.DbgPrimShader, transform);

            foreach (var pass in GFX.DbgPrimShader.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                GFX.Device.SetVertexBuffer(DebugLineVertexBuffer);
                GFX.Device.DrawUserIndexedPrimitives(PrimitiveType.LineList,
                    DebugBoundingBoxPositionBuffer, 0, 8, DebugBoundingBoxIndexBuffer, 0, 12);
            }

        }

        public static void DrawOutlinedText(
            string text, Vector2 pos, Color color, SpriteFont font = null,
            float depth = 0, float scale = 1, Vector2 scaleOrigin = default(Vector2), 
            bool startAndEndSpriteBatchForMe = true, bool disableSmoothing = false)
        {
            if (startAndEndSpriteBatchForMe)
                GFX.SpriteBatch.Begin(samplerState: disableSmoothing ? SamplerState.PointClamp : SamplerState.AnisotropicWrap);

            // Top, Bottom, Left, Right
            GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(1, 0), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(-1, 0), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(0, 1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(0, -1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);

            // Top-Left, Top-Right, Bottom-Left, Bottom-Right
            //GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(-1, 1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            //GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(-1, -1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            //GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(1, 1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);
            //GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos + new Vector2(1, -1), Color.Black, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth + 0.000001f);

            GFX.SpriteBatch.DrawString(font ?? DEBUG_FONT, text, pos, color, 0, scaleOrigin, Vector2.One * scale, SpriteEffects.None, depth);

            if (startAndEndSpriteBatchForMe)
                GFX.SpriteBatch.End();
        }

        //DSF2_TODO: Add FatcatDebug.DrawX
    }


}
