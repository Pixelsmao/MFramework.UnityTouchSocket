/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated January 1, 2020. Replaces all prior versions.
 *
 * Copyright (c) 2013-2020, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THE SPINE RUNTIMES ARE PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES,
 * BUSINESS INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THE SPINE RUNTIMES, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

#if UNITY_2018_3 || UNITY_2019 || UNITY_2018_3_OR_NEWER
#define NEW_PREFAB_SYSTEM
#endif

#if UNITY_2018_1_OR_NEWER
#define PER_MATERIAL_PROPERTY_BLOCKS
#endif

#if UNITY_2017_1_OR_NEWER
#define BUILT_IN_SPRITE_MASK_COMPONENT
#endif

#if UNITY_2019_3_OR_NEWER
#define CONFIGURABLE_ENTER_PLAY_MODE
#endif

#define SPINE_OPTIONAL_RENDEROVERRIDE
#define SPINE_OPTIONAL_MATERIALOVERRIDE

using System.Collections.Generic;
using UnityEngine;

namespace Spine.Unity
{
    /// <summary>Base class of animated Spine skeleton components. This component manages and renders a skeleton.</summary>
#if NEW_PREFAB_SYSTEM
    [ExecuteAlways]
#else
	[ExecuteInEditMode]
#endif
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
    [HelpURL("http://esotericsoftware.com/spine-unity#SkeletonRenderer-Component")]
    public class SkeletonRenderer : MonoBehaviour, ISkeletonComponent, IHasSkeletonDataAsset
    {
        public SkeletonDataAsset skeletonDataAsset;

        #region Initialization settings
        /// <summary>Skin name to use when the Skeleton is initialized.</summary>
        [SpineSkin(defaultAsEmptyString: true)] public string initialSkinName;

        /// <summary>Enable this parameter when overwriting the Skeleton's skin from an editor script.
        /// Otherwise any changes will be overwritten by the next inspector update.</summary>
#if UNITY_EDITOR
        public bool EditorSkipSkinSync
        {
            get { return this.editorSkipSkinSync; }
            set { this.editorSkipSkinSync = value; }
        }
        protected bool editorSkipSkinSync = false;
#endif
        /// <summary>Flip X and Y to use when the Skeleton is initialized.</summary>
        public bool initialFlipX, initialFlipY;
        #endregion

        #region Advanced Render Settings

        /// <summary>Update mode to optionally limit updates to e.g. only apply animations but not update the mesh.</summary>
        public UpdateMode UpdateMode { get { return this.updateMode; } set { this.updateMode = value; } }
        protected UpdateMode updateMode = UpdateMode.FullUpdate;

        /// <summary>Update mode used when the MeshRenderer becomes invisible
        /// (when <c>OnBecameInvisible()</c> is called). Update mode is automatically
        /// reset to <c>UpdateMode.FullUpdate</c> when the mesh becomes visible again.</summary>
        public UpdateMode updateWhenInvisible = UpdateMode.FullUpdate;

        // Submesh Separation
        /// <summary>Slot names used to populate separatorSlots list when the Skeleton is initialized. Changing this after initialization does nothing.</summary>
        [UnityEngine.Serialization.FormerlySerializedAs("submeshSeparators")][SerializeField][SpineSlot] protected string[] separatorSlotNames = new string[0];

        /// <summary>Slots that determine where the render is split. This is used by components such as SkeletonRenderSeparator so that the skeleton can be rendered by two separate renderers on different GameObjects.</summary>
        [System.NonSerialized] public readonly List<Slot> separatorSlots = new List<Slot>();

        // Render Settings
        [Range(-0.1f, 0f)] public float zSpacing;
        /// <summary>Use Spine's clipping feature. If false, ClippingAttachments will be ignored.</summary>
        public bool useClipping = true;

        /// <summary>If true, triangles will not be updated. Enable this as an optimization if the skeleton does not make use of attachment swapping or hiding, or draw order keys. Otherwise, setting this to false may cause errors in rendering.</summary>
        public bool immutableTriangles = false;

        /// <summary>Multiply vertex color RGB with vertex color alpha. Set this to true if the shader used for rendering is a premultiplied alpha shader. Setting this to false disables single-batch additive slots.</summary>
        public bool pmaVertexColors = true;

        /// <summary>Clears the state of the render and skeleton when this component or its GameObject is disabled. This prevents previous state from being retained when it is enabled again. When pooling your skeleton, setting this to true can be helpful.</summary>
        public bool clearStateOnDisable = false;

        /// <summary>If true, second colors on slots will be added to the output Mesh as UV2 and UV3. A special "tint black" shader that interprets UV2 and UV3 as black point colors is required to render this properly.</summary>
        public bool tintBlack = false;

        /// <summary>If true, the renderer assumes the skeleton only requires one Material and one submesh to render. This allows the MeshGenerator to skip checking for changes in Materials. Enable this as an optimization if the skeleton only uses one Material.</summary>
        /// <remarks>This disables SkeletonRenderSeparator functionality.</remarks>
        public bool singleSubmesh = false;

#if PER_MATERIAL_PROPERTY_BLOCKS
        /// <summary> Applies only when 3+ submeshes are used (2+ materials with alternating order, e.g. "A B A").
        /// If true, GPU instancing is disabled at all materials and MaterialPropertyBlocks are assigned at each
        /// material to prevent aggressive batching of submeshes by e.g. the LWRP renderer, leading to incorrect
        /// draw order (e.g. "A1 B A2" changed to "A1A2 B").
        /// You can disable this parameter when everything is drawn correctly to save the additional performance cost.
        /// </summary>
        public bool fixDrawOrder = false;
#endif

        /// <summary>If true, the mesh generator adds normals to the output mesh. For better performance and reduced memory requirements, use a shader that assumes the desired normal.</summary>
        [UnityEngine.Serialization.FormerlySerializedAs("calculateNormals")] public bool addNormals = false;

        /// <summary>If true, tangents are calculated every frame and added to the Mesh. Enable this when using a shader that uses lighting that requires tangents.</summary>
        public bool calculateTangents = false;

#if BUILT_IN_SPRITE_MASK_COMPONENT
        /// <summary>This enum controls the mode under which the sprite will interact with the masking system.</summary>
        /// <remarks>Interaction modes with <see cref="UnityEngine.SpriteMask"/> components are identical to Unity's <see cref="UnityEngine.SpriteRenderer"/>,
        /// see https://docs.unity3d.com/ScriptReference/SpriteMaskInteraction.html. </remarks>
        public SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None;

        [System.Serializable]
        public class SpriteMaskInteractionMaterials
        {
            public bool AnyMaterialCreated
            {
                get
                {
                    return this.materialsMaskDisabled.Length > 0 ||
                        this.materialsInsideMask.Length > 0 ||
                        this.materialsOutsideMask.Length > 0;
                }
            }

            /// <summary>Material references for switching material sets at runtime when <see cref="SkeletonRenderer.maskInteraction"/> changes to <see cref="SpriteMaskInteraction.None"/>.</summary>
            public Material[] materialsMaskDisabled = new Material[0];
            /// <summary>Material references for switching material sets at runtime when <see cref="SkeletonRenderer.maskInteraction"/> changes to <see cref="SpriteMaskInteraction.VisibleInsideMask"/>.</summary>
            public Material[] materialsInsideMask = new Material[0];
            /// <summary>Material references for switching material sets at runtime when <see cref="SkeletonRenderer.maskInteraction"/> changes to <see cref="SpriteMaskInteraction.VisibleOutsideMask"/>.</summary>
            public Material[] materialsOutsideMask = new Material[0];
        }
        /// <summary>Material references for switching material sets at runtime when <see cref="SkeletonRenderer.maskInteraction"/> changes.</summary>
        public SpriteMaskInteractionMaterials maskMaterials = new SpriteMaskInteractionMaterials();

        /// <summary>Shader property ID used for the Stencil comparison function.</summary>
        public static readonly int STENCIL_COMP_PARAM_ID = Shader.PropertyToID("_StencilComp");
        /// <summary>Shader property value used as Stencil comparison function for <see cref="SpriteMaskInteraction.None"/>.</summary>
        public const UnityEngine.Rendering.CompareFunction STENCIL_COMP_MASKINTERACTION_NONE = UnityEngine.Rendering.CompareFunction.Always;
        /// <summary>Shader property value used as Stencil comparison function for <see cref="SpriteMaskInteraction.VisibleInsideMask"/>.</summary>
        public const UnityEngine.Rendering.CompareFunction STENCIL_COMP_MASKINTERACTION_VISIBLE_INSIDE = UnityEngine.Rendering.CompareFunction.LessEqual;
        /// <summary>Shader property value used as Stencil comparison function for <see cref="SpriteMaskInteraction.VisibleOutsideMask"/>.</summary>
        public const UnityEngine.Rendering.CompareFunction STENCIL_COMP_MASKINTERACTION_VISIBLE_OUTSIDE = UnityEngine.Rendering.CompareFunction.Greater;
#if UNITY_EDITOR
        private static bool haveStencilParametersBeenFixed = false;
#endif
#endif // #if BUILT_IN_SPRITE_MASK_COMPONENT
        #endregion

        #region Overrides
#if SPINE_OPTIONAL_RENDEROVERRIDE
        // These are API for anything that wants to take over rendering for a SkeletonRenderer.
        public bool disableRenderingOnOverride = true;
        public delegate void InstructionDelegate(SkeletonRendererInstruction instruction);
        private event InstructionDelegate generateMeshOverride;

        /// <summary>Allows separate code to take over rendering for this SkeletonRenderer component. The subscriber is passed a SkeletonRendererInstruction argument to determine how to render a skeleton.</summary>
        public event InstructionDelegate GenerateMeshOverride
        {
            add
            {
                generateMeshOverride += value;
                if (this.disableRenderingOnOverride && generateMeshOverride != null)
                {
                    this.Initialize(false);
                    if (this.meshRenderer)
                        this.meshRenderer.enabled = false;
                }
            }
            remove
            {
                generateMeshOverride -= value;
                if (this.disableRenderingOnOverride && generateMeshOverride == null)
                {
                    this.Initialize(false);
                    if (this.meshRenderer)
                        this.meshRenderer.enabled = true;
                }
            }
        }

        /// <summary> Occurs after the vertex data is populated every frame, before the vertices are pushed into the mesh.</summary>
        public event Spine.Unity.MeshGeneratorDelegate OnPostProcessVertices;
#endif

#if SPINE_OPTIONAL_MATERIALOVERRIDE
        [System.NonSerialized] private readonly Dictionary<Material, Material> customMaterialOverride = new Dictionary<Material, Material>();
        /// <summary>Use this Dictionary to override a Material with a different Material.</summary>
        public Dictionary<Material, Material> CustomMaterialOverride { get { return this.customMaterialOverride; } }
#endif

        [System.NonSerialized] private readonly Dictionary<Slot, Material> customSlotMaterials = new Dictionary<Slot, Material>();
        /// <summary>Use this Dictionary to use a different Material to render specific Slots.</summary>
        public Dictionary<Slot, Material> CustomSlotMaterials { get { return this.customSlotMaterials; } }
        #endregion

        #region Mesh Generator
        [System.NonSerialized] private readonly SkeletonRendererInstruction currentInstructions = new SkeletonRendererInstruction();
        private readonly MeshGenerator meshGenerator = new MeshGenerator();
        [System.NonSerialized] private readonly MeshRendererBuffers rendererBuffers = new MeshRendererBuffers();
        #endregion

        #region Cached component references
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        #endregion

        #region Skeleton
        [System.NonSerialized] public bool valid;
        [System.NonSerialized] public Skeleton skeleton;
        public Skeleton Skeleton
        {
            get
            {
                this.Initialize(false);
                return this.skeleton;
            }
        }
        #endregion

        public delegate void SkeletonRendererDelegate(SkeletonRenderer skeletonRenderer);

        /// <summary>OnRebuild is raised after the Skeleton is successfully initialized.</summary>
        public event SkeletonRendererDelegate OnRebuild;

        /// <summary>OnMeshAndMaterialsUpdated is called at the end of LateUpdate after the Mesh and
        /// all materials have been updated.</summary>
        public event SkeletonRendererDelegate OnMeshAndMaterialsUpdated;

        public SkeletonDataAsset SkeletonDataAsset { get { return this.skeletonDataAsset; } } // ISkeletonComponent

        #region Runtime Instantiation
        public static T NewSpineGameObject<T>(SkeletonDataAsset skeletonDataAsset, bool quiet = false) where T : SkeletonRenderer
        {
            return SkeletonRenderer.AddSpineComponent<T>(new GameObject("New Spine GameObject"), skeletonDataAsset, quiet);
        }

        /// <summary>Add and prepare a Spine component that derives from SkeletonRenderer to a GameObject at runtime.</summary>
        /// <typeparam name="T">T should be SkeletonRenderer or any of its derived classes.</typeparam>
        public static T AddSpineComponent<T>(GameObject gameObject, SkeletonDataAsset skeletonDataAsset, bool quiet = false) where T : SkeletonRenderer
        {
            var c = gameObject.AddComponent<T>();
            if (skeletonDataAsset != null)
            {
                c.skeletonDataAsset = skeletonDataAsset;
                c.Initialize(false, quiet);
            }
            return c;
        }

        /// <summary>Applies MeshGenerator settings to the SkeletonRenderer and its internal MeshGenerator.</summary>
        public void SetMeshSettings(MeshGenerator.Settings settings)
        {
            this.calculateTangents = settings.calculateTangents;
            this.immutableTriangles = settings.immutableTriangles;
            this.pmaVertexColors = settings.pmaVertexColors;
            this.tintBlack = settings.tintBlack;
            this.useClipping = settings.useClipping;
            this.zSpacing = settings.zSpacing;

            this.meshGenerator.settings = settings;
        }
        #endregion


        public virtual void Awake()
        {
            this.Initialize(false);
            this.updateMode = this.updateWhenInvisible;
        }

#if UNITY_EDITOR && CONFIGURABLE_ENTER_PLAY_MODE
        public virtual void Start()
        {
            this.Initialize(false);
        }
#endif

        private void OnDisable()
        {
            if (this.clearStateOnDisable && this.valid)
                this.ClearState();
        }

        private void OnDestroy()
        {
            this.rendererBuffers.Dispose();
            this.valid = false;
        }

        /// <summary>
        /// Clears the previously generated mesh and resets the skeleton's pose.</summary>
        public virtual void ClearState()
        {
            var meshFilter = this.GetComponent<MeshFilter>();
            if (meshFilter != null) meshFilter.sharedMesh = null;
            this.currentInstructions.Clear();
            if (this.skeleton != null) this.skeleton.SetToSetupPose();
        }

        /// <summary>
        /// Sets a minimum buffer size for the internal MeshGenerator to prevent excess allocations during animation.
        /// </summary>
        public void EnsureMeshGeneratorCapacity(int minimumVertexCount)
        {
            this.meshGenerator.EnsureVertexCapacity(minimumVertexCount);
        }

        /// <summary>
        /// Initialize this component. Attempts to load the SkeletonData and creates the internal Skeleton object and buffers.</summary>
        /// <param name="overwrite">If set to <c>true</c>, it will overwrite internal objects if they were already generated. Otherwise, the initialized component will ignore subsequent calls to initialize.</param>
        public virtual void Initialize(bool overwrite, bool quiet = false)
        {
            if (this.valid && !overwrite)
                return;

            // Clear
            {
                // Note: do not reset meshFilter.sharedMesh or meshRenderer.sharedMaterial to null,
                // otherwise constant reloading will be triggered at prefabs.
                this.currentInstructions.Clear();
                this.rendererBuffers.Clear();
                this.meshGenerator.Begin();
                this.skeleton = null;
                this.valid = false;
            }

            if (this.skeletonDataAsset == null)
                return;

            var skeletonData = this.skeletonDataAsset.GetSkeletonData(false);
            if (skeletonData == null) return;
            this.valid = true;

            this.meshFilter = this.GetComponent<MeshFilter>();
            this.meshRenderer = this.GetComponent<MeshRenderer>();
            this.rendererBuffers.Initialize();

            this.skeleton = new Skeleton(skeletonData)
            {
                ScaleX = this.initialFlipX ? -1 : 1,
                ScaleY = this.initialFlipY ? -1 : 1
            };

            if (!string.IsNullOrEmpty(this.initialSkinName) && !string.Equals(this.initialSkinName, "default", System.StringComparison.Ordinal))
                this.skeleton.SetSkin(this.initialSkinName);

            this.separatorSlots.Clear();
            for (var i = 0; i < this.separatorSlotNames.Length; i++)
                this.separatorSlots.Add(this.skeleton.FindSlot(this.separatorSlotNames[i]));

            this.LateUpdate(); // Generate mesh for the first frame it exists.

            if (OnRebuild != null)
                OnRebuild(this);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string errorMessage = null;
                if (!quiet && MaterialChecks.IsMaterialSetupProblematic(this, ref errorMessage))
                    Debug.LogWarningFormat(this, "Problematic material setup at {0}: {1}", this.name, errorMessage);
            }
#endif
        }

        /// <summary>
        /// Generates a new UnityEngine.Mesh from the internal Skeleton.</summary>
        public virtual void LateUpdate()
        {
            if (!this.valid) return;

#if UNITY_EDITOR && NEW_PREFAB_SYSTEM
            // Don't store mesh or material at the prefab, otherwise it will permanently reload
            var prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(this);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) &&
                (prefabType == UnityEditor.PrefabAssetType.Regular || prefabType == UnityEditor.PrefabAssetType.Variant))
            {
                return;
            }
#endif

            if (this.updateMode != UpdateMode.FullUpdate) return;

#if SPINE_OPTIONAL_RENDEROVERRIDE
            var doMeshOverride = generateMeshOverride != null;
            if ((!this.meshRenderer.enabled) && !doMeshOverride) return;
#else
			const bool doMeshOverride = false;
			if (!meshRenderer.enabled) return;
#endif
            var currentInstructions = this.currentInstructions;
            var workingSubmeshInstructions = currentInstructions.submeshInstructions;
            var currentSmartMesh = this.rendererBuffers.GetNextMesh(); // Double-buffer for performance.

            bool updateTriangles;

            if (this.singleSubmesh)
            {
                // STEP 1. Determine a SmartMesh.Instruction. Split up instructions into submeshes. =============================================
                MeshGenerator.GenerateSingleSubmeshInstruction(currentInstructions, this.skeleton, this.skeletonDataAsset.atlasAssets[0].PrimaryMaterial);

                // STEP 1.9. Post-process workingInstructions. ==================================================================================
#if SPINE_OPTIONAL_MATERIALOVERRIDE
                if (this.customMaterialOverride.Count > 0) // isCustomMaterialOverridePopulated
                    MeshGenerator.TryReplaceMaterials(workingSubmeshInstructions, this.customMaterialOverride);
#endif

                // STEP 2. Update vertex buffer based on verts from the attachments. ===========================================================
                this.meshGenerator.settings = new MeshGenerator.Settings
                {
                    pmaVertexColors = this.pmaVertexColors,
                    zSpacing = this.zSpacing,
                    useClipping = this.useClipping,
                    tintBlack = this.tintBlack,
                    calculateTangents = this.calculateTangents,
                    addNormals = this.addNormals
                };
                this.meshGenerator.Begin();
                updateTriangles = SkeletonRendererInstruction.GeometryNotEqual(currentInstructions, currentSmartMesh.instructionUsed);
                if (currentInstructions.hasActiveClipping)
                {
                    this.meshGenerator.AddSubmesh(workingSubmeshInstructions.Items[0], updateTriangles);
                }
                else
                {
                    this.meshGenerator.BuildMeshWithArrays(currentInstructions, updateTriangles);
                }

            }
            else
            {
                // STEP 1. Determine a SmartMesh.Instruction. Split up instructions into submeshes. =============================================
                MeshGenerator.GenerateSkeletonRendererInstruction(currentInstructions, this.skeleton, this.customSlotMaterials, this.separatorSlots, doMeshOverride, this.immutableTriangles);

                // STEP 1.9. Post-process workingInstructions. ==================================================================================
#if SPINE_OPTIONAL_MATERIALOVERRIDE
                if (this.customMaterialOverride.Count > 0) // isCustomMaterialOverridePopulated
                    MeshGenerator.TryReplaceMaterials(workingSubmeshInstructions, this.customMaterialOverride);
#endif

#if SPINE_OPTIONAL_RENDEROVERRIDE
                if (doMeshOverride)
                {
                    this.generateMeshOverride(currentInstructions);
                    if (this.disableRenderingOnOverride) return;
                }
#endif

                updateTriangles = SkeletonRendererInstruction.GeometryNotEqual(currentInstructions, currentSmartMesh.instructionUsed);

                // STEP 2. Update vertex buffer based on verts from the attachments. ===========================================================
                this.meshGenerator.settings = new MeshGenerator.Settings
                {
                    pmaVertexColors = this.pmaVertexColors,
                    zSpacing = this.zSpacing,
                    useClipping = this.useClipping,
                    tintBlack = this.tintBlack,
                    calculateTangents = this.calculateTangents,
                    addNormals = this.addNormals
                };
                this.meshGenerator.Begin();
                if (currentInstructions.hasActiveClipping)
                    this.meshGenerator.BuildMesh(currentInstructions, updateTriangles);
                else
                    this.meshGenerator.BuildMeshWithArrays(currentInstructions, updateTriangles);
            }

            if (OnPostProcessVertices != null) OnPostProcessVertices.Invoke(this.meshGenerator.Buffers);

            // STEP 3. Move the mesh data into a UnityEngine.Mesh ===========================================================================
            var currentMesh = currentSmartMesh.mesh;
            this.meshGenerator.FillVertexData(currentMesh);

            this.rendererBuffers.UpdateSharedMaterials(workingSubmeshInstructions);

            var materialsChanged = this.rendererBuffers.MaterialsChangedInLastUpdate();
            if (updateTriangles)
            { // Check if the triangles should also be updated.
                this.meshGenerator.FillTriangles(currentMesh);
                this.meshRenderer.sharedMaterials = this.rendererBuffers.GetUpdatedSharedMaterialsArray();
            }
            else if (materialsChanged)
            {
                this.meshRenderer.sharedMaterials = this.rendererBuffers.GetUpdatedSharedMaterialsArray();
            }
            if (materialsChanged && (this.maskMaterials.AnyMaterialCreated))
            {
                this.maskMaterials = new SpriteMaskInteractionMaterials();
            }

            this.meshGenerator.FillLateVertexData(currentMesh);

            // STEP 4. The UnityEngine.Mesh is ready. Set it as the MeshFilter's mesh. Store the instructions used for that mesh. ===========
            this.meshFilter.sharedMesh = currentMesh;
            currentSmartMesh.instructionUsed.Set(currentInstructions);

#if BUILT_IN_SPRITE_MASK_COMPONENT
            if (this.meshRenderer != null)
            {
                this.AssignSpriteMaskMaterials();
            }
#endif

#if PER_MATERIAL_PROPERTY_BLOCKS
            if (this.fixDrawOrder && this.meshRenderer.sharedMaterials.Length > 2)
            {
                this.SetMaterialSettingsToFixDrawOrder();
            }
#endif

            if (OnMeshAndMaterialsUpdated != null)
                OnMeshAndMaterialsUpdated(this);
        }

        public void OnBecameVisible()
        {
            var previousUpdateMode = this.updateMode;
            this.updateMode = UpdateMode.FullUpdate;
            if (previousUpdateMode != UpdateMode.FullUpdate)
                this.LateUpdate(); // OnBecameVisible is called after LateUpdate()
        }

        public void OnBecameInvisible()
        {
            this.updateMode = this.updateWhenInvisible;
        }

        public void FindAndApplySeparatorSlots(string startsWith, bool clearExistingSeparators = true, bool updateStringArray = false)
        {
            if (string.IsNullOrEmpty(startsWith)) return;

            this.FindAndApplySeparatorSlots(
                (slotName) => slotName.StartsWith(startsWith),
                clearExistingSeparators,
                updateStringArray
                );
        }

        public void FindAndApplySeparatorSlots(System.Func<string, bool> slotNamePredicate, bool clearExistingSeparators = true, bool updateStringArray = false)
        {
            if (slotNamePredicate == null) return;
            if (!this.valid) return;

            if (clearExistingSeparators)
                this.separatorSlots.Clear();

            var slots = this.skeleton.slots;
            foreach (var slot in slots)
            {
                if (slotNamePredicate.Invoke(slot.data.name))
                    this.separatorSlots.Add(slot);
            }

            if (updateStringArray)
            {
                var detectedSeparatorNames = new List<string>();
                foreach (var slot in this.skeleton.slots)
                {
                    var slotName = slot.data.name;
                    if (slotNamePredicate.Invoke(slotName))
                        detectedSeparatorNames.Add(slotName);
                }
                if (!clearExistingSeparators)
                {
                    var originalNames = this.separatorSlotNames;
                    foreach (var originalName in originalNames)
                        detectedSeparatorNames.Add(originalName);
                }

                this.separatorSlotNames = detectedSeparatorNames.ToArray();
            }

        }

        public void ReapplySeparatorSlotNames()
        {
            if (!this.valid)
                return;

            this.separatorSlots.Clear();
            for (int i = 0, n = this.separatorSlotNames.Length; i < n; i++)
            {
                var slot = this.skeleton.FindSlot(this.separatorSlotNames[i]);
                if (slot != null)
                {
                    this.separatorSlots.Add(slot);
                }
#if UNITY_EDITOR
                else if (!string.IsNullOrEmpty(this.separatorSlotNames[i]))
                {
                    Debug.LogWarning(this.separatorSlotNames[i] + " is not a slot in " + this.skeletonDataAsset.skeletonJSON.name);
                }
#endif
            }
        }

#if BUILT_IN_SPRITE_MASK_COMPONENT
        private void AssignSpriteMaskMaterials()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isUpdating)
            {
                this.EditorFixStencilCompParameters();
            }
#endif

            if (Application.isPlaying)
            {
                if (this.maskInteraction != SpriteMaskInteraction.None && this.maskMaterials.materialsMaskDisabled.Length == 0)
                    this.maskMaterials.materialsMaskDisabled = this.meshRenderer.sharedMaterials;
            }

            if (this.maskMaterials.materialsMaskDisabled.Length > 0 && this.maskMaterials.materialsMaskDisabled[0] != null &&
                this.maskInteraction == SpriteMaskInteraction.None)
            {
                this.meshRenderer.materials = this.maskMaterials.materialsMaskDisabled;
            }
            else if (this.maskInteraction == SpriteMaskInteraction.VisibleInsideMask)
            {
                if (this.maskMaterials.materialsInsideMask.Length == 0 || this.maskMaterials.materialsInsideMask[0] == null)
                {
                    if (!this.InitSpriteMaskMaterialsInsideMask())
                        return;
                }
                this.meshRenderer.materials = this.maskMaterials.materialsInsideMask;
            }
            else if (this.maskInteraction == SpriteMaskInteraction.VisibleOutsideMask)
            {
                if (this.maskMaterials.materialsOutsideMask.Length == 0 || this.maskMaterials.materialsOutsideMask[0] == null)
                {
                    if (!this.InitSpriteMaskMaterialsOutsideMask())
                        return;
                }
                this.meshRenderer.materials = this.maskMaterials.materialsOutsideMask;
            }
        }

        private bool InitSpriteMaskMaterialsInsideMask()
        {
            return this.InitSpriteMaskMaterialsForMaskType(STENCIL_COMP_MASKINTERACTION_VISIBLE_INSIDE, ref this.maskMaterials.materialsInsideMask);
        }

        private bool InitSpriteMaskMaterialsOutsideMask()
        {
            return this.InitSpriteMaskMaterialsForMaskType(STENCIL_COMP_MASKINTERACTION_VISIBLE_OUTSIDE, ref this.maskMaterials.materialsOutsideMask);
        }

        private bool InitSpriteMaskMaterialsForMaskType(UnityEngine.Rendering.CompareFunction maskFunction, ref Material[] materialsToFill)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return false;
            }
#endif

            var originalMaterials = this.maskMaterials.materialsMaskDisabled;
            materialsToFill = new Material[originalMaterials.Length];
            for (var i = 0; i < originalMaterials.Length; i++)
            {
                var newMaterial = new Material(originalMaterials[i]);
                newMaterial.SetFloat(STENCIL_COMP_PARAM_ID, (int)maskFunction);
                materialsToFill[i] = newMaterial;
            }
            return true;
        }

#if UNITY_EDITOR
        private void EditorFixStencilCompParameters()
        {
            if (!haveStencilParametersBeenFixed && this.HasAnyStencilComp0Material())
            {
                haveStencilParametersBeenFixed = true;
                this.FixAllProjectMaterialsStencilCompParameters();
            }
        }

        private void FixAllProjectMaterialsStencilCompParameters()
        {
            var materialGUIDS = UnityEditor.AssetDatabase.FindAssets("t:material");
            foreach (var guid in materialGUIDS)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat.HasProperty(STENCIL_COMP_PARAM_ID) && mat.GetFloat(STENCIL_COMP_PARAM_ID) == 0)
                    {
                        mat.SetFloat(STENCIL_COMP_PARAM_ID, (int)STENCIL_COMP_MASKINTERACTION_NONE);
                    }
                }
            }
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.SaveAssets();
        }

        private bool HasAnyStencilComp0Material()
        {
            if (this.meshRenderer == null)
                return false;

            foreach (var mat in this.meshRenderer.sharedMaterials)
            {
                if (mat != null && mat.HasProperty(STENCIL_COMP_PARAM_ID))
                {
                    var currentCompValue = mat.GetFloat(STENCIL_COMP_PARAM_ID);
                    if (currentCompValue == 0)
                        return true;
                }
            }
            return false;
        }
#endif // UNITY_EDITOR

#endif //#if BUILT_IN_SPRITE_MASK_COMPONENT

#if PER_MATERIAL_PROPERTY_BLOCKS
        private MaterialPropertyBlock reusedPropertyBlock;
        public static readonly int SUBMESH_DUMMY_PARAM_ID = Shader.PropertyToID("_Submesh");

        /// <summary>
        /// This method was introduced as a workaround for too aggressive submesh draw call batching,
        /// leading to incorrect draw order when 3+ materials are used at submeshes in alternating order.
        /// Otherwise, e.g. when using Lightweight Render Pipeline, deliberately separated draw calls
        /// "A1 B A2" are reordered to "A1A2 B", regardless of batching-related project settings.
        /// </summary>
        private void SetMaterialSettingsToFixDrawOrder()
        {
            if (this.reusedPropertyBlock == null) this.reusedPropertyBlock = new MaterialPropertyBlock();

            var hasPerRendererBlock = this.meshRenderer.HasPropertyBlock();
            if (hasPerRendererBlock)
            {
                this.meshRenderer.GetPropertyBlock(this.reusedPropertyBlock);
            }

            for (var i = 0; i < this.meshRenderer.sharedMaterials.Length; ++i)
            {
                if (!this.meshRenderer.sharedMaterials[i])
                    continue;

                if (!hasPerRendererBlock) this.meshRenderer.GetPropertyBlock(this.reusedPropertyBlock, i);
                // Note: this parameter shall not exist at any shader, then Unity will create separate
                // material instances (not in terms of memory cost or leakage).
                this.reusedPropertyBlock.SetFloat(SUBMESH_DUMMY_PARAM_ID, i);
                this.meshRenderer.SetPropertyBlock(this.reusedPropertyBlock, i);

                this.meshRenderer.sharedMaterials[i].enableInstancing = false;
            }
        }
#endif
    }
}
