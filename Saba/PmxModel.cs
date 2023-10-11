﻿using Saba.Helpers;
using System.Collections.Concurrent;
using System.Numerics;
using static Saba.PmxMorph;

namespace Saba;

public unsafe class PmxModel : MMDModel
{
    #region Enums
    private enum MorphType
    {
        None,
        Position,
        UV,
        Material,
        Bone,
        Group
    }
    #endregion

    #region Classes
    private class PmxMorph : MMDMorph
    {
        public MorphType MorphType { get; set; }

        public int DataIndex { get; set; }
    }

    private class PositionMorph
    {
        public int Index { get; set; }

        public Vector3 Position { get; set; }
    }

    private class PositionMorphData
    {
        public List<PositionMorph> MorphVertices { get; } = new();
    }

    private class UVMorph
    {
        public int Index { get; set; }

        public Vector4 UV { get; set; }
    }

    private class UVMorphData
    {
        public List<UVMorph> MorphUVs { get; } = new();
    }

    private class MaterialFactor
    {
        public Vector3 Diffuse { get; set; }

        public float Alpha { get; set; }

        public Vector3 Specular { get; set; }

        public float SpecularPower { get; set; }

        public Vector3 Ambient { get; set; }

        public Vector4 EdgeColor { get; set; }

        public float EdgeSize { get; set; }

        public Vector4 TextureCoefficient { get; set; }

        public Vector4 SphereTextureCoefficient { get; set; }

        public Vector4 ToonTextureCoefficient { get; set; }

        public MaterialFactor()
        {
        }

        public MaterialFactor(MaterialMorph pmxMat)
        {
            Diffuse = pmxMat.Diffuse.ToVector3();
            Alpha = pmxMat.Diffuse.W;
            Specular = pmxMat.Specular;
            SpecularPower = pmxMat.SpecularPower;
            Ambient = pmxMat.Ambient;
            EdgeColor = pmxMat.EdgeColor;
            EdgeSize = pmxMat.EdgeSize;
            TextureCoefficient = pmxMat.TextureCoefficient;
            SphereTextureCoefficient = pmxMat.SphereTextureCoefficient;
            ToonTextureCoefficient = pmxMat.ToonTextureCoefficient;
        }

        public void Mul(MaterialFactor val, float weight)
        {
            Diffuse = Vector3.Lerp(Diffuse, Diffuse * val.Diffuse, weight);
            Alpha = MathHelper.Lerp(Alpha, Alpha * val.Alpha, weight);
            Specular = Vector3.Lerp(Specular, Specular * val.Specular, weight);
            SpecularPower = MathHelper.Lerp(SpecularPower, SpecularPower * val.SpecularPower, weight);
            Ambient = Vector3.Lerp(Ambient, Ambient * val.Ambient, weight);
            EdgeColor = Vector4.Lerp(EdgeColor, EdgeColor * val.EdgeColor, weight);
            EdgeSize = MathHelper.Lerp(EdgeSize, EdgeSize * val.EdgeSize, weight);
            TextureCoefficient = Vector4.Lerp(TextureCoefficient, TextureCoefficient * val.TextureCoefficient, weight);
            SphereTextureCoefficient = Vector4.Lerp(SphereTextureCoefficient, SphereTextureCoefficient * val.SphereTextureCoefficient, weight);
            ToonTextureCoefficient = Vector4.Lerp(ToonTextureCoefficient, ToonTextureCoefficient * val.ToonTextureCoefficient, weight);
        }

        public void Add(MaterialFactor val, float weight)
        {
            Diffuse += val.Diffuse * weight;
            Alpha += val.Alpha * weight;
            Specular += val.Specular * weight;
            SpecularPower += val.SpecularPower * weight;
            Ambient += val.Ambient * weight;
            EdgeColor += val.EdgeColor * weight;
            EdgeSize += val.EdgeSize * weight;
            TextureCoefficient += val.TextureCoefficient * weight;
            SphereTextureCoefficient += val.SphereTextureCoefficient * weight;
            ToonTextureCoefficient += val.ToonTextureCoefficient * weight;
        }

        public static MaterialFactor InitMul()
        {
            MaterialFactor materialFactor = new()
            {
                Diffuse = Vector3.One,
                Alpha = 1.0f,
                Specular = Vector3.One,
                SpecularPower = 1.0f,
                Ambient = Vector3.One,
                EdgeColor = Vector4.One,
                EdgeSize = 1.0f,
                TextureCoefficient = Vector4.One,
                SphereTextureCoefficient = Vector4.One,
                ToonTextureCoefficient = Vector4.One
            };

            return materialFactor;
        }

        public static MaterialFactor InitAdd()
        {
            MaterialFactor materialFactor = new()
            {
                Diffuse = Vector3.Zero,
                Alpha = 0.0f,
                Specular = Vector3.Zero,
                SpecularPower = 0.0f,
                Ambient = Vector3.Zero,
                EdgeColor = Vector4.Zero,
                EdgeSize = 0.0f,
                TextureCoefficient = Vector4.Zero,
                SphereTextureCoefficient = Vector4.Zero,
                ToonTextureCoefficient = Vector4.Zero
            };

            return materialFactor;
        }
    }

    private class MaterialMorphData
    {
        public List<MaterialMorph> MaterialMorphs { get; } = new();
    }

    private class BoneMorph
    {
        public MMDNode Node { get; }

        public Vector3 Position { get; set; }

        public Quaternion Rotate { get; set; }

        public BoneMorph(MMDNode node)
        {
            Node = node;
        }
    }

    private class BoneMorphData
    {
        public List<BoneMorph> BoneMorphs { get; } = new();
    }

    private class GroupMorphData
    {
        public List<GroupMorph> GroupMorphs { get; } = new();
    }
    #endregion

    private readonly List<MMDMaterial> _materials;
    private readonly List<MMDMesh> _meshes;
    private readonly List<PmxNode> _nodes;
    private readonly List<PmxNode> _sortedNodes;
    private readonly List<MMDIkSolver> _ikSolvers;
    private readonly List<PmxMorph> _morphs;
    private readonly List<PositionMorphData> _positionMorphDatas;
    private readonly List<UVMorphData> _uvMorphDatas;
    private readonly List<MaterialMorphData> _materialMorphDatas;
    private readonly List<BoneMorphData> _boneMorphDatas;
    private readonly List<GroupMorphData> _groupMorphDatas;

    private Vector3[] positions = Array.Empty<Vector3>();
    private Vector3[] normals = Array.Empty<Vector3>();
    private Vector2[] uvs = Array.Empty<Vector2>();
    private VertexBoneInfo[] vertexBoneInfos = Array.Empty<VertexBoneInfo>();

    private uint[] indices = Array.Empty<uint>();

    private Vector3[] updatePositions = Array.Empty<Vector3>();
    private Vector3[] updateNormals = Array.Empty<Vector3>();
    private Vector2[] updateUVs = Array.Empty<Vector2>();
    private Matrix4x4[] updateTransforms = Array.Empty<Matrix4x4>();

    private Vector3[] morphPositions = Array.Empty<Vector3>();
    private Vector4[] morphUVs = Array.Empty<Vector4>();

    private MMDMaterial[] initMaterials = Array.Empty<MMDMaterial>();
    private MaterialFactor[] mulMaterialFactors = Array.Empty<MaterialFactor>();
    private MaterialFactor[] addMaterialFactors = Array.Empty<MaterialFactor>();

    private MMDPhysicsManager? physicsManager;

    private Kernel? kernel;

    public PmxModel()
    {
        _materials = new List<MMDMaterial>();
        _meshes = new List<MMDMesh>();
        _nodes = new List<PmxNode>();
        _sortedNodes = new List<PmxNode>();
        _ikSolvers = new List<MMDIkSolver>();
        _morphs = new List<PmxMorph>();
        _positionMorphDatas = new List<PositionMorphData>();
        _uvMorphDatas = new List<UVMorphData>();
        _materialMorphDatas = new List<MaterialMorphData>();
        _boneMorphDatas = new List<BoneMorphData>();
        _groupMorphDatas = new List<GroupMorphData>();
    }

    public override bool Load(string path, string mmdDataDir)
    {
        Destroy();

        PmxParsing? pmx = PmxParsing.ParsingByFile(path);
        if (pmx == null)
        {
            return false;
        }

        string dir = Path.GetDirectoryName(path)!;

        // 坐标点、法线、UV、骨骼信息
        Array.Resize(ref positions, pmx.Vertices.Length);
        Array.Resize(ref normals, pmx.Vertices.Length);
        Array.Resize(ref uvs, pmx.Vertices.Length);
        Array.Resize(ref vertexBoneInfos, pmx.Vertices.Length);
        for (int i = 0; i < pmx.Vertices.Length; i++)
        {
            PmxVertex vertex = pmx.Vertices[i];

            Vector3 position = vertex.Position * new Vector3(1.0f, 1.0f, -1.0f);
            Vector3 normal = vertex.Normal * new Vector3(1.0f, 1.0f, -1.0f);
            Vector2 uv = new(vertex.UV.X, -vertex.UV.Y);
            positions[i] = position;
            normals[i] = normal;
            uvs[i] = uv;

            VertexBoneInfo vertexBoneInfo = new();

            if (vertex.WeightType != PmxVertexWeight.SDEF)
            {
                vertexBoneInfo.BoneIndices[0] = vertex.BoneIndices[0];
                vertexBoneInfo.BoneIndices[1] = vertex.BoneIndices[1];
                vertexBoneInfo.BoneIndices[2] = vertex.BoneIndices[2];
                vertexBoneInfo.BoneIndices[3] = vertex.BoneIndices[3];

                vertexBoneInfo.BoneWeights[0] = vertex.BoneWeights[0];
                vertexBoneInfo.BoneWeights[1] = vertex.BoneWeights[1];
                vertexBoneInfo.BoneWeights[2] = vertex.BoneWeights[2];
                vertexBoneInfo.BoneWeights[3] = vertex.BoneWeights[3];
            }

            switch (vertex.WeightType)
            {
                case PmxVertexWeight.BDEF1:
                    vertexBoneInfo.SkinningType = SkinningType.Weight1;
                    break;
                case PmxVertexWeight.BDEF2:
                    vertexBoneInfo.SkinningType = SkinningType.Weight2;
                    vertexBoneInfo.BoneWeights[1] = 1.0f - vertex.BoneWeights[0];
                    break;
                case PmxVertexWeight.BDEF4:
                    vertexBoneInfo.SkinningType = SkinningType.Weight4;
                    break;
                case PmxVertexWeight.SDEF:
                    vertexBoneInfo.SkinningType = SkinningType.SDEF;
                    {
                        float w0 = vertex.BoneWeights[0];
                        float w1 = 1.0f - vertex.BoneWeights[0];

                        Vector3 center = vertex.SdefC * new Vector3(1.0f, 1.0f, -1.0f);
                        Vector3 r0 = vertex.SdefR0 * new Vector3(1.0f, 1.0f, -1.0f);
                        Vector3 r1 = vertex.SdefR1 * new Vector3(1.0f, 1.0f, -1.0f);
                        Vector3 rw = (r0 * w0) + (r1 * w1);
                        r0 = center + r0 - rw;
                        r1 = center + r1 - rw;
                        Vector3 cr0 = (center + r0) * 0.5f;
                        Vector3 cr1 = (center + r1) * 0.5f;

                        vertexBoneInfo.SDEF.BoneIndices[0] = vertex.BoneIndices[0];
                        vertexBoneInfo.SDEF.BoneIndices[1] = vertex.BoneIndices[1];
                        vertexBoneInfo.SDEF.BoneWeight = vertex.BoneWeights[0];
                        vertexBoneInfo.SDEF.C = center;
                        vertexBoneInfo.SDEF.R0 = cr0;
                        vertexBoneInfo.SDEF.R1 = cr1;
                    }
                    break;
                case PmxVertexWeight.QDEF:
                    vertexBoneInfo.SkinningType = SkinningType.DualQuaternion;
                    break;
                default:
                    vertexBoneInfo.SkinningType = SkinningType.Weight1;
                    break;
            }

            vertexBoneInfos[i] = vertexBoneInfo;
        }

        Array.Resize(ref updatePositions, positions.Length);
        Array.Resize(ref updateNormals, normals.Length);
        Array.Resize(ref updateUVs, uvs.Length);
        Array.Resize(ref morphPositions, positions.Length);
        Array.Resize(ref morphUVs, uvs.Length);

        // 面信息
        Array.Resize(ref indices, pmx.Faces.Length * 3);
        for (int i = 0; i < pmx.Faces.Length; i++)
        {
            PmxFace face = pmx.Faces[i];

            indices[i * 3 + 0] = face.Vertices[2];
            indices[i * 3 + 1] = face.Vertices[1];
            indices[i * 3 + 2] = face.Vertices[0];
        }

        // 纹理
        List<string> texturePaths = new();
        foreach (PmxTexture texture in pmx.Textures)
        {
            string texPath = Path.Combine(dir, texture.Name.FormatFilePath());
            if (File.Exists(texPath))
            {
                texturePaths.Add(texPath);
            }
            else
            {
                throw new FileNotFoundException($"Texture file not found: {texPath}");
            }
        }

        // 材质信息
        uint beginIndex = 0;
        foreach (PmxMaterial material in pmx.Materials)
        {
            MMDMaterial mat = new()
            {
                Name = material.Name,
                Diffuse = material.Diffuse.ToVector3(),
                Alpha = material.Diffuse.W,
                Specular = material.Specular,
                SpecularPower = material.SpecularPower,
                Ambient = material.Ambient,
                EdgeFlag = Convert.ToByte(material.DrawMode.HasFlag(PmxDrawModeFlags.DrawEdge)),
                EdgeSize = material.EdgeSize,
                EdgeColor = material.EdgeColor,
                SpTextureMode = SphereTextureMode.None,
                BothFace = material.DrawMode.HasFlag(PmxDrawModeFlags.BothFace),
                GroundShadow = material.DrawMode.HasFlag(PmxDrawModeFlags.GroundShadow),
                ShadowCaster = material.DrawMode.HasFlag(PmxDrawModeFlags.CastSelfShadow),
                ShadowReceiver = material.DrawMode.HasFlag(PmxDrawModeFlags.RecieveSelfShadow)
            };

            // 纹理
            if (material.TextureIndex != -1)
            {
                mat.Texture = texturePaths[material.TextureIndex];
            }

            // 卡通纹理
            if (material.ToonMode == PmxToonMode.Common)
            {
                if (material.ToonTextureIndex != -1)
                {
                    string texName = material.ToonTextureIndex + 1 < 10 ? $"0{material.ToonTextureIndex + 1}" : $"{material.ToonTextureIndex + 1}";

                    mat.ToonTexture = Path.Combine(mmdDataDir, $"toon{texName}.bmp");
                }
            }
            else if (material.ToonMode == PmxToonMode.Separate)
            {
                if (material.ToonTextureIndex != -1)
                {
                    mat.ToonTexture = texturePaths[material.ToonTextureIndex];
                }
            }

            // 球形纹理
            if (material.SphereTextureIndex != -1)
            {
                mat.SpTexture = texturePaths[material.SphereTextureIndex];
                mat.SpTextureMode = SphereTextureMode.None;
                if (material.SphereMode == PmxSphereMode.Mul)
                {
                    mat.SpTextureMode = SphereTextureMode.Mul;
                }
                else if (material.SphereMode == PmxSphereMode.Add)
                {
                    mat.SpTextureMode = SphereTextureMode.Add;
                }
                else if (material.SphereMode == PmxSphereMode.SubTexture)
                {
                    // TODO: SphareTexture が SubTexture の処理
                }
            }

            _materials.Add(mat);
            _meshes.Add(new MMDMesh(beginIndex, (uint)material.FaceVerticesCount, mat));

            beginIndex += (uint)material.FaceVerticesCount;
        }
        _materials.Copy(ref initMaterials);
        Array.Resize(ref mulMaterialFactors, initMaterials.Length);
        Array.Resize(ref addMaterialFactors, initMaterials.Length);

        // 骨骼信息
        foreach (PmxBone bone in pmx.Bones)
        {
            _nodes.Add(new PmxNode() { Index = _nodes.Count, Name = bone.Name });
        }

        for (int i = 0; i < pmx.Bones.Length; i++)
        {
            int boneIndex = pmx.Bones.Length - i - 1;
            PmxBone bone = pmx.Bones[boneIndex];
            PmxNode node = _nodes[boneIndex];

            // 检查节点是否循环
            bool isLoop = false;
            if (bone.ParentBoneIndex != -1)
            {
                MMDNode? parent = _nodes[bone.ParentBoneIndex];
                while (parent != null)
                {
                    if (parent == node)
                    {
                        isLoop = true;

                        Console.WriteLine($"This bone hierarchy is a loop: bone= {bone.Name}");

                        break;
                    }

                    parent = parent.Parent;
                }
            }

            // 检查父级节点索引位置
            if (bone.ParentBoneIndex != -1)
            {
                if (bone.ParentBoneIndex >= boneIndex)
                {
                    Console.WriteLine($"The parent index of this node is big: bone= {bone.Name}");
                }
            }

            if (bone.ParentBoneIndex != -1 && !isLoop)
            {
                PmxBone parentBone = pmx.Bones[bone.ParentBoneIndex];
                MMDNode parent = _nodes[bone.ParentBoneIndex];

                parent.AddChild(node);

                Vector3 localPos = bone.Position - parentBone.Position;
                localPos.Z *= -1.0f;
                node.Translate = localPos;
            }
            else
            {
                Vector3 localPos = bone.Position;
                localPos.Z *= -1.0f;
                node.Translate = localPos;
            }

            Matrix4x4 init = Matrix4x4.CreateTranslation(bone.Position * new Vector3(1.0f, 1.0f, -1.0f));

            node.Global = init;
            node.CalculateInverseInitTransform();

            node.DeformDepth = bone.DeformDepth;
            node.IsDeformAfterPhysics = bone.BoneFlags.HasFlag(PmxBoneFlags.DeformAfterPhysics);
            node.IsAppendRotate = bone.BoneFlags.HasFlag(PmxBoneFlags.AppendRotate);
            node.IsAppendTranslate = bone.BoneFlags.HasFlag(PmxBoneFlags.AppendTranslate);

            if ((node.IsAppendRotate || node.IsAppendTranslate) && bone.AppendBoneIndex != -1)
            {
                if (bone.AppendBoneIndex >= boneIndex)
                {
                    Console.WriteLine($"The parent(morph assignment) index of this node is big: bone= {bone.Name}");
                }
                node.AppendNode = _nodes[bone.AppendBoneIndex];
                node.IsAppendLocal = bone.BoneFlags.HasFlag(PmxBoneFlags.AppendLocal);
                node.AppendWeight = bone.AppendWeight;
            }
            node.SaveInitialTRS();
        }

        Array.Resize(ref updateTransforms, _nodes.Count);
        _sortedNodes.AddRange(_nodes.OrderBy(item => item.DeformDepth));

        // IK
        for (int i = 0; i < pmx.Bones.Length; i++)
        {
            PmxBone bone = pmx.Bones[i];
            if (bone.BoneFlags.HasFlag(PmxBoneFlags.IK))
            {
                MMDIkSolver solver = new();
                PmxNode ikNode = _nodes[i];

                solver.IkNode = ikNode;
                ikNode.IkSolver = solver;

                _ikSolvers.Add(solver);

                if (bone.IKTargetBoneIndex < 0 || bone.IKTargetBoneIndex >= _nodes.Count)
                {
                    Console.WriteLine($"IK target bone index is invalid: bone= {bone.Name}");

                    continue;
                }

                solver.IkTarget = _nodes[bone.IKTargetBoneIndex];

                foreach (PmxBone.IKLink ikLink in bone.IKLinks)
                {
                    PmxNode linkNode = _nodes[ikLink.BoneIndex];
                    if (ikLink.EnableLimit)
                    {
                        Vector3 limitMax = ikLink.LimitMin * new Vector3(-1.0f);
                        Vector3 limitMin = ikLink.LimitMax * new Vector3(-1.0f);

                        solver.AddIkChain(linkNode, true, limitMin, limitMax);
                    }
                    else
                    {
                        solver.AddIkChain(linkNode);
                    }
                    linkNode.EnableIK = true;
                }

                solver.IterateCount = (uint)bone.IKIterationCount;
                solver.LimitAngle = bone.IKLimit;
            }
        }

        // Morph
        foreach (Saba.PmxMorph pmxMorph in pmx.Morphs)
        {
            PmxMorph morph = new()
            {
                Name = pmxMorph.Name,
                Weight = 0.0f,
                MorphType = MorphType.None
            };

            if (pmxMorph.MorphType == PmxMorphType.Position)
            {
                morph.MorphType = MorphType.Position;
                morph.DataIndex = _positionMorphDatas.Count;

                PositionMorphData morphData = new();
                foreach (Saba.PmxMorph.PositionMorph vtx in pmxMorph.PositionMorphs)
                {
                    PositionMorph morphVtx = new()
                    {
                        Index = vtx.VertexIndex,
                        Position = vtx.Position * new Vector3(1.0f, 1.0f, -1.0f)
                    };

                    morphData.MorphVertices.Add(morphVtx);
                }
                _positionMorphDatas.Add(morphData);
            }
            else if (pmxMorph.MorphType == PmxMorphType.UV)
            {
                morph.MorphType = MorphType.UV;
                morph.DataIndex = _uvMorphDatas.Count;

                UVMorphData morphData = new();
                foreach (Saba.PmxMorph.UVMorph uv in pmxMorph.UVMorphs)
                {
                    UVMorph morphUV = new()
                    {
                        Index = uv.VertexIndex,
                        UV = uv.UV
                    };

                    morphData.MorphUVs.Add(morphUV);
                }
                _uvMorphDatas.Add(morphData);
            }
            else if (pmxMorph.MorphType == PmxMorphType.Material)
            {
                morph.MorphType = MorphType.Material;
                morph.DataIndex = _materialMorphDatas.Count;

                MaterialMorphData morphData = new();
                morphData.MaterialMorphs.AddRange(pmxMorph.MaterialMorphs);
                _materialMorphDatas.Add(morphData);
            }
            else if (pmxMorph.MorphType == PmxMorphType.Bone)
            {
                morph.MorphType = MorphType.Bone;
                morph.DataIndex = _boneMorphDatas.Count;

                BoneMorphData boneMorphData = new();
                foreach (Saba.PmxMorph.BoneMorph pmxBoneMorph in pmxMorph.BoneMorphs)
                {
                    BoneMorph boneMorph = new(_nodes[pmxBoneMorph.BoneIndex])
                    {
                        Position = pmxBoneMorph.Position * new Vector3(1.0f, 1.0f, -1.0f)
                    };

                    Matrix4x4 invZ = Matrix4x4.CreateScale(new Vector3(1.0f, 1.0f, -1.0f));
                    Matrix4x4 rot0 = Matrix4x4.CreateFromQuaternion(pmxBoneMorph.Quaternion);
                    Matrix4x4 rot1 = invZ * rot0 * invZ;

                    boneMorph.Rotate = Quaternion.CreateFromRotationMatrix(rot1);

                    boneMorphData.BoneMorphs.Add(boneMorph);
                }
                _boneMorphDatas.Add(boneMorphData);
            }
            else if (pmxMorph.MorphType == PmxMorphType.Group)
            {
                morph.MorphType = MorphType.Group;
                morph.DataIndex = _groupMorphDatas.Count;

                GroupMorphData groupMorphData = new();
                groupMorphData.GroupMorphs.AddRange(pmxMorph.GroupMorphs);
                _groupMorphDatas.Add(groupMorphData);
            }
            else
            {
                Console.WriteLine($"Not Supported Morp Type({pmxMorph.MorphType}): [{pmxMorph.Name}]");
            }

            _morphs.Add(morph);
        }

        // Physics
        physicsManager = new MMDPhysicsManager();

        foreach (PmxRigidBody pmxRB in pmx.RigidBodies)
        {
            MMDNode? node = null;

            if (pmxRB.BoneIndex != -1)
            {
                node = _nodes[pmxRB.BoneIndex];
            }

            MMDRigidBody rigidBody = new(pmxRB, this, node);

            physicsManager.AddRigidBody(rigidBody);
        }

        MMDRigidBody[] rigidBodies = physicsManager.RigidBodies;

        foreach (PmxJoint pmxJoint in pmx.Joints)
        {
            if (pmxJoint.RigidBodyIndexA != -1 && pmxJoint.RigidBodyIndexB != -1 && pmxJoint.RigidBodyIndexA != pmxJoint.RigidBodyIndexB)
            {
                MMDJoint joint = new(pmxJoint, rigidBodies[pmxJoint.RigidBodyIndexA], rigidBodies[pmxJoint.RigidBodyIndexB]);

                physicsManager.AddJoint(joint);
            }
        }

        kernel = Kernel.Create(File.ReadAllText("skinned_animation.cl"), "run", new string[] { "-cl-mad-enable" });

        ResetPhysics();

        Update();

        return true;
    }

    public override MMDNode[] GetNodes()
    {
        return _nodes.ToArray();
    }

    public override MMDMorph[] GetMorphs()
    {
        return _morphs.ToArray();
    }

    public override MMDIkSolver[] GetIkSolvers()
    {
        return _ikSolvers.ToArray();
    }

    public override MMDNode? FindNode(Predicate<MMDNode> predicate)
    {
        return _nodes.Find(predicate);
    }

    public override MMDMorph? FindMorph(Predicate<MMDMorph> predicate)
    {
        return _morphs.Find(predicate);
    }

    public override MMDIkSolver? FindIkSolver(Predicate<MMDIkSolver> predicate)
    {
        return _ikSolvers.Find(predicate);
    }

    public override int GetVertexCount()
    {
        return positions.Length;
    }

    public override unsafe Vector3* GetPositions()
    {
        return positions.GetData();
    }

    public override unsafe Vector3* GetNormals()
    {
        return normals.GetData();
    }

    public override unsafe Vector2* GetUVs()
    {
        return uvs.GetData();
    }

    public override unsafe Vector3* GetUpdatePositions()
    {
        return updatePositions.GetData();
    }

    public override unsafe Vector3* GetUpdateNormals()
    {
        return updateNormals.GetData();
    }

    public override unsafe Vector2* GetUpdateUVs()
    {
        return updateUVs.GetData();
    }

    public override int GetIndexCount()
    {
        return indices.Length;
    }

    public override unsafe uint* GetIndices()
    {
        return indices.GetData();
    }

    public override MMDMaterial[] GetMaterials()
    {
        return _materials.ToArray();
    }

    public override MMDMesh[] GetMeshes()
    {
        return _meshes.ToArray();
    }

    public override void InitializeAnimation()
    {
        ClearBaseAnimation();

        foreach (PmxNode node in _nodes)
        {
            node.AnimTranslate = Vector3.Zero;
            node.AnimRotate = Quaternion.Identity;
        }

        BeginAnimation();

        foreach (PmxNode node in _nodes)
        {
            node.UpdateLocalTransform();
        }

        foreach (PmxMorph morph in _morphs)
        {
            morph.Weight = 0.0f;
        }

        foreach (MMDIkSolver ikSolver in _ikSolvers)
        {
            ikSolver.Enable = true;
        }

        foreach (PmxNode node in _nodes.Where(item => item.Parent != null))
        {
            node.UpdateGlobalTransform();
        }

        foreach (PmxNode node in _sortedNodes)
        {
            if (node.AppendNode != null)
            {
                node.UpdateAppendTransform();
                node.UpdateGlobalTransform();
            }

            if (node.IkSolver != null)
            {
                node.IkSolver.Solve();

                node.UpdateGlobalTransform();
            }
        }

        foreach (PmxNode node in _nodes.Where(item => item.Parent == null))
        {
            node.UpdateGlobalTransform();
        }

        EndAnimation();
    }

    public override void BeginAnimation()
    {
        foreach (PmxNode node in _nodes)
        {
            node.BeginUpdateTransform();
        }

        Array.Fill(morphPositions, Vector3.Zero);

        Array.Fill(morphUVs, Vector4.Zero);
    }

    public override void EndAnimation()
    {
        foreach (PmxNode node in _nodes)
        {
            node.EndUpdateTransform();
        }
    }

    public override void UpdateMorphAnimation()
    {
        // Morph の処理
        BeginMorphMaterial();

        foreach (PmxMorph morph in _morphs)
        {
            Morph(morph, morph.Weight);
        }

        EndMorphMaterial();
    }

    public override void UpdateNodeAnimation(bool afterPhysicsAnim)
    {
        foreach (PmxNode pmxNode in _sortedNodes)
        {
            if (pmxNode.IsDeformAfterPhysics != afterPhysicsAnim)
            {
                continue;
            }

            pmxNode.UpdateLocalTransform();
        }

        foreach (PmxNode pmxNode in _sortedNodes)
        {
            if (pmxNode.IsDeformAfterPhysics != afterPhysicsAnim)
            {
                continue;
            }

            if (pmxNode.Parent == null)
            {
                pmxNode.UpdateGlobalTransform();
            }
        }

        foreach (PmxNode pmxNode in _sortedNodes)
        {
            if (pmxNode.IsDeformAfterPhysics != afterPhysicsAnim)
            {
                continue;
            }

            if (pmxNode.AppendNode != null)
            {
                pmxNode.UpdateAppendTransform();
                pmxNode.UpdateGlobalTransform();
            }

            if (pmxNode.IkSolver != null)
            {
                pmxNode.IkSolver.Solve();
                pmxNode.UpdateGlobalTransform();
            }
        }

        foreach (PmxNode pmxNode in _sortedNodes)
        {
            if (pmxNode.IsDeformAfterPhysics != afterPhysicsAnim)
            {
                continue;
            }

            if (pmxNode.Parent == null)
            {
                pmxNode.UpdateGlobalTransform();
            }
        }
    }

    public override void ResetPhysics()
    {
        if (physicsManager == null)
        {
            return;
        }

        MMDRigidBody[] rigidBodies = physicsManager.RigidBodies;

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.SetActivation(false);
            rb.ResetTransform();
        }

        physicsManager.Physics.Update(1.0f / 60.0f);

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.ReflectGlobalTransform();
        }

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.CalcLocalTransform();
        }

        foreach (PmxNode node in _nodes.Where(item => item.Parent == null))
        {
            node.UpdateGlobalTransform();
        }

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.Reset(physicsManager.Physics);
        }
    }

    public override void UpdatePhysicsAnimation(float elapsed)
    {
        if (physicsManager == null)
        {
            return;
        }

        MMDRigidBody[] rigidBodies = physicsManager.RigidBodies;

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.SetActivation(true);
        }

        physicsManager.Physics.Update(elapsed);

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.ReflectGlobalTransform();
        }

        foreach (MMDRigidBody rb in rigidBodies)
        {
            rb.CalcLocalTransform();
        }

        foreach (PmxNode node in _nodes.Where(item => item.Parent == null))
        {
            node.UpdateGlobalTransform();
        }
    }

    public override void Update()
    {
        Matrix4x4* transforms = updateTransforms.GetData();
        foreach (PmxNode node in _nodes)
        {
            *transforms = node.InverseInit * node.Global;

            transforms++;
        }

        Parallel.ForEach(Partitioner.Create(0, positions.Length), range =>
        {
            Update(range.Item1, range.Item2);
        });
    }

    public override void Destroy()
    {
        _materials.Clear();
        _meshes.Clear();
        _nodes.Clear();
        _sortedNodes.Clear();
        _ikSolvers.Clear();
        _morphs.Clear();
        _positionMorphDatas.Clear();
        _uvMorphDatas.Clear();
        _materialMorphDatas.Clear();
        _boneMorphDatas.Clear();
        _groupMorphDatas.Clear();

        positions = Array.Empty<Vector3>();
        normals = Array.Empty<Vector3>();
        uvs = Array.Empty<Vector2>();
        vertexBoneInfos = Array.Empty<VertexBoneInfo>();

        indices = Array.Empty<uint>();

        updatePositions = Array.Empty<Vector3>();
        updateNormals = Array.Empty<Vector3>();
        updateUVs = Array.Empty<Vector2>();
        updateTransforms = Array.Empty<Matrix4x4>();

        morphPositions = Array.Empty<Vector3>();
        morphUVs = Array.Empty<Vector4>();

        initMaterials = Array.Empty<MMDMaterial>();

        physicsManager?.Dispose();

        kernel?.Dispose();
    }

    public override void Dispose()
    {
        Destroy();

        GC.SuppressFinalize(this);
    }

    private void BeginMorphMaterial()
    {
        MaterialFactor initMul = MaterialFactor.InitMul();

        MaterialFactor initAdd = MaterialFactor.InitAdd();

        for (int i = 0; i < _materials.Count; i++)
        {
            mulMaterialFactors[i] = initMul;
            mulMaterialFactors[i].Diffuse = initMaterials[i].Diffuse;
            mulMaterialFactors[i].Alpha = initMaterials[i].Alpha;
            mulMaterialFactors[i].Specular = initMaterials[i].Specular;
            mulMaterialFactors[i].SpecularPower = initMaterials[i].SpecularPower;
            mulMaterialFactors[i].Ambient = initMaterials[i].Ambient;

            addMaterialFactors[i] = initAdd;
        }
    }

    private void Morph(PmxMorph morph, float weight)
    {
        switch (morph.MorphType)
        {
            case MorphType.Position:
                MorphPosition(_positionMorphDatas[morph.DataIndex], weight);
                break;
            case MorphType.UV:
                MorphUV(_uvMorphDatas[morph.DataIndex], weight);
                break;
            case MorphType.Material:
                MorphMaterial(_materialMorphDatas[morph.DataIndex], weight);
                break;
            case MorphType.Bone:
                MorphBone(_boneMorphDatas[morph.DataIndex], weight);
                break;
            case MorphType.Group:
                GroupMorphData groupMorphData = _groupMorphDatas[morph.DataIndex];
                foreach (GroupMorph groupMorph in groupMorphData.GroupMorphs)
                {
                    if (groupMorph.MorphIndex == -1)
                    {
                        continue;
                    }

                    Morph(_morphs[groupMorph.MorphIndex], groupMorph.Weight * weight);
                }
                break;
            default: break;
        }
    }

    private void EndMorphMaterial()
    {
        for (int i = 0; i < _materials.Count; i++)
        {
            MaterialFactor matFactor = mulMaterialFactors[i];
            matFactor.Add(addMaterialFactors[i], 1.0f);

            _materials[i].Diffuse = matFactor.Diffuse;
            _materials[i].Alpha = matFactor.Alpha;
            _materials[i].Specular = matFactor.Specular;
            _materials[i].SpecularPower = matFactor.SpecularPower;
            _materials[i].Ambient = matFactor.Ambient;
            _materials[i].TextureMulFactor = mulMaterialFactors[i].TextureCoefficient;
            _materials[i].TextureAddFactor = addMaterialFactors[i].TextureCoefficient;
            _materials[i].SpTextureMulFactor = mulMaterialFactors[i].SphereTextureCoefficient;
            _materials[i].SpTextureAddFactor = addMaterialFactors[i].SphereTextureCoefficient;
            _materials[i].ToonTextureMulFactor = mulMaterialFactors[i].ToonTextureCoefficient;
            _materials[i].ToonTextureAddFactor = addMaterialFactors[i].ToonTextureCoefficient;
        }
    }

    private void MorphPosition(PositionMorphData positionMorphData, float weight)
    {
        if (weight == 0)
        {
            return;
        }

        foreach (PositionMorph morphVtx in positionMorphData.MorphVertices)
        {
            morphPositions[morphVtx.Index] += morphVtx.Position * weight;
        }
    }

    private void MorphUV(UVMorphData uVMorphData, float weight)
    {
        if (weight == 0)
        {
            return;
        }

        foreach (UVMorph morphUV in uVMorphData.MorphUVs)
        {
            morphUVs[morphUV.Index] += morphUV.UV * weight;
        }
    }

    private void MorphMaterial(MaterialMorphData materialMorphData, float weight)
    {
        foreach (MaterialMorph matMorph in materialMorphData.MaterialMorphs)
        {
            if (matMorph.MaterialIndex != -1)
            {
                int mi = matMorph.MaterialIndex;
                switch (matMorph.OpType)
                {
                    case PmxOpType.Mul:
                        mulMaterialFactors[mi].Mul(new MaterialFactor(matMorph), weight);
                        break;
                    case PmxOpType.Add:
                        mulMaterialFactors[mi].Add(new MaterialFactor(matMorph), weight);
                        break;
                    default: break;
                }
            }
            else
            {
                for (int i = 0; i < _materials.Count; i++)
                {
                    switch (matMorph.OpType)
                    {
                        case PmxOpType.Mul:
                            mulMaterialFactors[i].Mul(new MaterialFactor(matMorph), weight);
                            break;
                        case PmxOpType.Add:
                            mulMaterialFactors[i].Add(new MaterialFactor(matMorph), weight);
                            break;
                        default: break;
                    }
                }
            }
        }
    }

    private void MorphBone(BoneMorphData boneMorphData, float weight)
    {
        foreach (BoneMorph boneMorph in boneMorphData.BoneMorphs)
        {
            MMDNode node = _nodes.Find(item => item == boneMorph.Node)!;

            Vector3 t = Vector3.Lerp(Vector3.Zero, boneMorph.Position, weight);
            node.Translate += t;

            Quaternion q = Quaternion.Slerp(node.Rotate, boneMorph.Rotate, weight);
            node.Rotate = q;
        }
    }

    private void Update(int begin, int end)
    {
        int length = end - begin;

        Vector3* position = positions.GetData() + begin;
        Vector3* normal = normals.GetData() + begin;
        Vector2* uv = uvs.GetData() + begin;
        Vector3* morphPos = morphPositions.GetData() + begin;
        Vector4* morphUV = morphUVs.GetData() + begin;
        VertexBoneInfo* vtxInfo = vertexBoneInfos.GetData() + begin;
        Matrix4x4* transforms = updateTransforms.GetData();
        Vector3* updatePosition = updatePositions.GetData() + begin;
        Vector3* updateNormal = updateNormals.GetData() + begin;
        Vector2* updateUV = updateUVs.GetData() + begin;

        for (int i = 0; i < length; i++)
        {
            Matrix4x4 m = Matrix4x4.Identity;

            switch (vtxInfo->SkinningType)
            {
                case SkinningType.Weight1:
                    m = transforms[vtxInfo->BoneIndices[0]];
                    break;
                case SkinningType.Weight2:
                    m = transforms[vtxInfo->BoneIndices[0]] * vtxInfo->BoneWeights[0] +
                        transforms[vtxInfo->BoneIndices[1]] * vtxInfo->BoneWeights[1];
                    break;
                case SkinningType.Weight4:
                    m = transforms[vtxInfo->BoneIndices[0]] * vtxInfo->BoneWeights[0] +
                        transforms[vtxInfo->BoneIndices[1]] * vtxInfo->BoneWeights[1] +
                        transforms[vtxInfo->BoneIndices[2]] * vtxInfo->BoneWeights[2] +
                        transforms[vtxInfo->BoneIndices[3]] * vtxInfo->BoneWeights[3];
                    break;
                case SkinningType.SDEF:
                    {
                        int i0 = vtxInfo->SDEF.BoneIndices[0];
                        int i1 = vtxInfo->SDEF.BoneIndices[1];
                        float w0 = vtxInfo->SDEF.BoneWeight;
                        float w1 = 1.0f - vtxInfo->SDEF.BoneWeight;
                        Vector3 center = vtxInfo->SDEF.C;
                        Vector3 cr0 = vtxInfo->SDEF.R0;
                        Vector3 cr1 = vtxInfo->SDEF.R1;
                        Quaternion q0 = Quaternion.CreateFromRotationMatrix(_nodes[i0].Global);
                        Quaternion q1 = Quaternion.CreateFromRotationMatrix(_nodes[i1].Global);
                        Matrix4x4 m0 = transforms[i0];
                        Matrix4x4 m1 = transforms[i1];

                        Vector3 pos = *position + *morphPos;
                        Matrix4x4 rot_mat = Matrix4x4.CreateFromQuaternion(Quaternion.Slerp(q0, q1, w1));

                        *updatePosition = Vector3.Transform(pos - center, rot_mat) + Vector3.Transform(cr0, m0) * w0 + Vector3.Transform(cr1, m1) * w1;
                        *updateNormal = Vector3.Transform(*normal, rot_mat);
                    }
                    break;
                case SkinningType.DualQuaternion:
                    break;
                default: break;
            }

            if (vtxInfo->SkinningType != SkinningType.SDEF)
            {
                *updatePosition = Vector3.Transform(*position + *morphPos, m);
                *updateNormal = Vector3.Normalize(Vector3.Transform(*normal, m));
            }

            *updateUV = *uv + new Vector2((*morphUV).X, (*morphUV).Y);

            position++;
            normal++;
            uv++;
            morphPos++;
            morphUV++;
            vtxInfo++;
            updatePosition++;
            updateNormal++;
            updateUV++;
        }
    }
}
