﻿using DarkSoulsModelViewerDX.GFXShaders;
using MeowDSIO;
//using MeowDSIO.DataTypes.FLVER;
using SoulsFormats;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkSoulsModelViewerDX
{
    public class FlverSubmeshRenderer : IDisposable
    {
        public BoundingBox Bounds;

        private struct FlverSubmeshRendererFaceSet
        {
            public int IndexCount;
            public IndexBuffer IndexBuffer;
            public bool BackfaceCulling;
            public bool IsTriangleStrip;
            public byte LOD;
        }

        List<FlverSubmeshRendererFaceSet> MeshFacesets = new List<FlverSubmeshRendererFaceSet>();

        private bool HasNoLODs = true;

        VertexBuffer VertBuffer;
        VertexBufferBinding VertBufferBinding;

        public string TexNameDiffuse { get; private set; } = null;
        public string TexNameSpecular { get; private set; } = null;
        public string TexNameNormal { get; private set; } = null;
        public string TexNameDOL1 { get; private set; } = null;
        public string TexNameDOL2 { get; private set; } = null;

        public Texture2D TexDataDiffuse { get; private set; } = null;
        public Texture2D TexDataSpecular { get; private set; } = null;
        public Texture2D TexDataNormal { get; private set; } = null;
        public Texture2D TexDataDOL1 { get; private set; } = null;
        public Texture2D TexDataDOL2 { get; private set; } = null;

        public GFXDrawStep DrawStep { get; private set; }

        public int VertexCount { get; private set; }

        public readonly Model Parent;

        public FlverSubmeshRenderer(Model parent, FLVER flvr, FLVER.Mesh mesh)
        {
            Parent = parent;

            var shortMaterialName = MiscUtil.GetFileNameWithoutDirectoryOrExtension(flvr.Materials[mesh.MaterialIndex].MTD);
            if (shortMaterialName.EndsWith("_Alp") ||
                shortMaterialName.Contains("_Edge") ||
                shortMaterialName.Contains("_Decal") ||
                shortMaterialName.Contains("_Cloth") ||
                shortMaterialName.Contains("_al") ||
                shortMaterialName.Contains("BlendOpacity"))
            {
                DrawStep = GFXDrawStep.AlphaEdge;
            }
            else
            {
                DrawStep = GFXDrawStep.Opaque;
            }

            foreach (var matParam in flvr.Materials[mesh.MaterialIndex].Params)
            {
                var paramNameCheck = matParam.Param.ToUpper();
                // DS3/BB
                if (paramNameCheck == "G_DIFFUSETEXTURE")
                    TexNameDiffuse = matParam.Value;
                else if (paramNameCheck == "G_SPECULARTEXTURE")
                    TexNameSpecular = matParam.Value;
                else if (paramNameCheck == "G_BUMPMAPTEXTURE")
                    TexNameNormal = matParam.Value;
                else if (paramNameCheck == "G_DOLTEXTURE1")
                    TexNameDOL1 = matParam.Value;
                else if (paramNameCheck == "G_DOLTEXTURE2")
                    TexNameDOL2 = matParam.Value;
                // DS1 params
                else if (paramNameCheck == "G_DIFFUSE")
                    TexNameDiffuse = matParam.Value;
                else if (paramNameCheck == "G_SPECULAR")
                    TexNameSpecular = matParam.Value;
                else if (paramNameCheck == "G_BUMPMAP")
                    TexNameNormal = matParam.Value;
            }

            var MeshVertices = new VertexPositionColorNormalTangentTexture[mesh.VertexGroups[0].Vertices.Count];
            for (int i = 0; i < mesh.VertexGroups[0].Vertices.Count; i++)
            {
                var vert = mesh.VertexGroups[0].Vertices[i];
                MeshVertices[i] = new VertexPositionColorNormalTangentTexture();

                MeshVertices[i].Position = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);

                if (vert.Normal != null && vert.Tangents != null && vert.Tangents.Count > 0)
                {
                    MeshVertices[i].Normal = Vector3.Normalize(new Vector3(vert.Normal.X, vert.Normal.Y, vert.Normal.Z));
                    MeshVertices[i].Tangent = Vector3.Normalize(new Vector3(vert.Tangents[0].X, vert.Tangents[0].Y, vert.Tangents[0].Z));
                    MeshVertices[i].Binormal = Vector3.Cross(Vector3.Normalize(MeshVertices[i].Normal), Vector3.Normalize(MeshVertices[i].Tangent)) * vert.Tangents[0].W;
                }

                if (vert.UVs.Count > 0)
                {
                    MeshVertices[i].TextureCoordinate = new Vector2(vert.UVs[0].X, vert.UVs[0].Y);
                    if (vert.UVs.Count > 1)
                    {
                        MeshVertices[i].TextureCoordinate2 = new Vector2(vert.UVs[1].X, vert.UVs[1].Y);
                    }
                    else
                    {
                        MeshVertices[i].TextureCoordinate2 = Vector2.Zero;
                    }
                }
                else
                {
                    MeshVertices[i].TextureCoordinate = Vector2.Zero;
                    MeshVertices[i].TextureCoordinate2 = Vector2.Zero;
                }
            }

            VertexCount = MeshVertices.Length;

            MeshFacesets = new List<FlverSubmeshRendererFaceSet>();

            foreach (var faceset in mesh.FaceSets)
            {
                bool is32bit = (faceset.IndexSize == 0x20);

                var newFaceSet = new FlverSubmeshRendererFaceSet()
                {
                    BackfaceCulling = faceset.CullBackfaces,
                    IsTriangleStrip = faceset.TriangleStrip,
                    IndexBuffer = new IndexBuffer(
                                GFX.Device,
                                is32bit ? IndexElementSize.ThirtyTwoBits : IndexElementSize.SixteenBits,
                                faceset.Vertices.Length,
                                BufferUsage.WriteOnly),
                    IndexCount = faceset.Vertices.Length,
                };

                if (faceset.Flags == FLVER.FaceSet.FSFlags.LodLevel1)
                {
                    newFaceSet.LOD = (byte)1;
                    HasNoLODs = false;
                }
                else if (faceset.Flags == FLVER.FaceSet.FSFlags.LodLevel2)
                {
                    newFaceSet.LOD = (byte)2;
                    HasNoLODs = false;
                }

                if (is32bit)
                {
                    newFaceSet.IndexBuffer.SetData(faceset.Vertices);
                }
                else
                {
                    newFaceSet.IndexBuffer.SetData(faceset.Vertices.Select(x => (ushort)x).ToArray());
                }

                MeshFacesets.Add(newFaceSet);
            }

            Bounds = BoundingBox.CreateFromPoints(MeshVertices.Select(x => x.Position));

            VertBuffer = new VertexBuffer(GFX.Device,
                typeof(VertexPositionColorNormalTangentTexture), MeshVertices.Length, BufferUsage.WriteOnly);
            VertBuffer.SetData(MeshVertices);

            VertBufferBinding = new VertexBufferBinding(VertBuffer, 0, 0);

            TryToLoadTextures();
        }

        public void TryToLoadTextures()
        {
            if (TexDataDiffuse == null && TexNameDiffuse != null)
                TexDataDiffuse = TexturePool.FetchTexture(TexNameDiffuse);

            if (TexDataSpecular == null && TexNameSpecular != null)
                TexDataSpecular = TexturePool.FetchTexture(TexNameSpecular);

            if (TexDataNormal == null && TexNameNormal != null)
                TexDataNormal = TexturePool.FetchTexture(TexNameNormal);

            if (TexDataDOL1 == null && TexNameDOL1 != null)
            {
                TexDataDOL1 = TexturePool.FetchTexture(TexNameDOL1);
            }

            if (TexDataDOL2 == null && TexNameDOL2 != null)
                TexDataDOL2 = TexturePool.FetchTexture(TexNameDOL2);
        }

        public void Draw<T>(int lod, IGFXShader<T> shader, bool forceNoBackfaceCulling = false)
            where T : Effect
        {
            if (GFX.EnableTextures && shader == GFX.FlverShader)
            {
                GFX.FlverShader.Effect.ColorMap = TexDataDiffuse ?? Main.DEFAULT_TEXTURE_DIFFUSE;
                GFX.FlverShader.Effect.SpecularMap = TexDataSpecular ?? Main.DEFAULT_TEXTURE_SPECULAR;
                GFX.FlverShader.Effect.NormalMap = TexDataNormal ?? Main.DEFAULT_TEXTURE_NORMAL;
                GFX.FlverShader.Effect.LightMap1 = TexDataDOL1 ?? Main.DEFAULT_TEXTURE_DIFFUSE;
                //GFX.FlverShader.Effect.LightMap2 = TexDataDOL2 ?? Main.DEFAULT_TEXTURE_DIFFUSE;
            }

            GFX.Device.SetVertexBuffers(VertBufferBinding, Parent.InstanceBufferBinding);

            //foreach (var technique in shader.Effect.Techniques)
            //{
            //    shader.Effect.CurrentTechnique = technique;
            foreach (EffectPass pass in shader.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                foreach (var faceSet in MeshFacesets)
                {
                    if (!HasNoLODs && faceSet.LOD != lod)
                        continue;

                    GFX.Device.Indices = faceSet.IndexBuffer;

                    GFX.BackfaceCulling = forceNoBackfaceCulling ? false : faceSet.BackfaceCulling;

                    GFX.Device.DrawInstancedPrimitives(faceSet.IsTriangleStrip ? PrimitiveType.TriangleStrip : PrimitiveType.TriangleList, 0, 0,
                        faceSet.IsTriangleStrip ? (faceSet.IndexCount - 2) : (faceSet.IndexCount / 3), Parent.InstanceCount);

                }
            }
            //}
        }

        public void Dispose()
        {
            for (int i = 0; i < MeshFacesets.Count; i++)
            {
                MeshFacesets[i].IndexBuffer.Dispose();
            }

            MeshFacesets = null;

            VertBuffer.Dispose();

            // Just leave the texture data as-is, since 
            // TexturePool handles memory cleanup


            //TexDataDiffuse?.Dispose();
            TexDataDiffuse = null;
            TexNameDiffuse = null;

            //TexDataNormal?.Dispose();
            TexDataNormal = null;
            TexNameNormal = null;

            //TexDataSpecular?.Dispose();
            TexDataSpecular = null;
            TexNameSpecular = null;

            TexDataDOL1 = null;
            TexNameDOL1 = null;

            TexDataDOL2 = null;
            TexNameDOL2 = null;
        }
    }
}
