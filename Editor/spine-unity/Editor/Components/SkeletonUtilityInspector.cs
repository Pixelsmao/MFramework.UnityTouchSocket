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

using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    using Icons = SpineEditorUtilities.Icons;

    [CustomEditor(typeof(SkeletonUtility))]
    public class SkeletonUtilityInspector : UnityEditor.Editor
    {

        private SkeletonUtility skeletonUtility;
        private Skeleton skeleton;
        private SkeletonRenderer skeletonRenderer;
        private SkeletonGraphic skeletonGraphic;

#if !NEW_PREFAB_SYSTEM
		bool isPrefab;
#endif

        private readonly GUIContent SpawnHierarchyButtonLabel = new GUIContent("Spawn Hierarchy", Icons.skeleton);

        private void OnEnable()
        {
            this.skeletonUtility = (SkeletonUtility)this.target;
            this.skeletonRenderer = this.skeletonUtility.skeletonRenderer;
            this.skeletonGraphic = this.skeletonUtility.skeletonGraphic;
            this.skeleton = this.skeletonUtility.Skeleton;

            if (this.skeleton == null)
            {
                if (this.skeletonRenderer != null)
                {
                    this.skeletonRenderer.Initialize(false);
                    this.skeletonRenderer.LateUpdate();
                }
                else if (this.skeletonGraphic != null)
                {
                    this.skeletonGraphic.Initialize(false);
                    this.skeletonGraphic.LateUpdate();
                }
                this.skeleton = this.skeletonUtility.Skeleton;
            }

            if ((this.skeletonRenderer != null && !this.skeletonRenderer.valid) ||
                (this.skeletonGraphic != null && !this.skeletonGraphic.IsValid)) return;

#if !NEW_PREFAB_SYSTEM
			isPrefab |= PrefabUtility.GetPrefabType(this.target) == PrefabType.Prefab;
#endif
        }

        public override void OnInspectorGUI()
        {

#if !NEW_PREFAB_SYSTEM
			if (isPrefab) {
				GUILayout.Label(new GUIContent("Cannot edit Prefabs", Icons.warning));
				return;
			}
#endif

            this.serializedObject.Update();

            if ((this.skeletonRenderer != null && !this.skeletonRenderer.valid) ||
                (this.skeletonGraphic != null && !this.skeletonGraphic.IsValid))
            {
                GUILayout.Label(new GUIContent("Spine Component invalid. Check Skeleton Data Asset.", Icons.warning));
                return;
            }

            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("boneRoot"), SpineInspectorUtility.TempContent("Skeleton Root"));
            EditorGUILayout.PropertyField(this.serializedObject.FindProperty("flipBy180DegreeRotation"), SpineInspectorUtility.TempContent("Flip by Rotation", null,
                "If true, Skeleton.ScaleX and Skeleton.ScaleY are followed " +
                "by 180 degree rotation. If false, negative Transform scale is used. " +
                "Note that using negative scale is consistent with previous behaviour (hence the default), " +
                "however causes serious problems with rigidbodies and physics. Therefore, it is recommended to " +
                "enable this parameter where possible. When creating hinge chains for a chain of skeleton bones " +
                "via SkeletonUtilityBone, it is mandatory to have this parameter enabled."));

            var hasRootBone = this.skeletonUtility.boneRoot != null;

            if (!hasRootBone)
                EditorGUILayout.HelpBox("No hierarchy found. Use Spawn Hierarchy to generate GameObjects for bones.", MessageType.Info);

            using (new EditorGUI.DisabledGroupScope(hasRootBone))
            {
                if (SpineInspectorUtility.LargeCenteredButton(this.SpawnHierarchyButtonLabel))
                    this.SpawnHierarchyContextMenu();
            }

            if (hasRootBone)
            {
                if (SpineInspectorUtility.CenteredButton(new GUIContent("Remove Hierarchy")))
                {
                    Undo.RegisterCompleteObjectUndo(this.skeletonUtility, "Remove Hierarchy");
                    Undo.DestroyObjectImmediate(this.skeletonUtility.boneRoot.gameObject);
                    this.skeletonUtility.boneRoot = null;
                }
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private void SpawnHierarchyContextMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Follow all bones"), false, this.SpawnFollowHierarchy);
            menu.AddItem(new GUIContent("Follow (Root Only)"), false, this.SpawnFollowHierarchyRootOnly);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Override all bones"), false, this.SpawnOverrideHierarchy);
            menu.AddItem(new GUIContent("Override (Root Only)"), false, this.SpawnOverrideHierarchyRootOnly);

            menu.ShowAsContext();
        }

        public static void AttachIcon(SkeletonUtilityBone boneComponent)
        {
            var skeleton = boneComponent.hierarchy.Skeleton;
            var icon = boneComponent.bone.Data.Length == 0 ? Icons.nullBone : Icons.boneNib;

            foreach (var c in skeleton.IkConstraints)
                if (c.Target == boneComponent.bone)
                {
                    icon = Icons.constraintNib;
                    break;
                }

            typeof(EditorGUIUtility).InvokeMember("SetIconForObject", BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic, null, null, new object[2] {
                boneComponent.gameObject,
                icon
            });
        }

        private static void AttachIconsToChildren(Transform root)
        {
            if (root != null)
            {
                var utilityBones = root.GetComponentsInChildren<SkeletonUtilityBone>();
                foreach (var utilBone in utilityBones)
                    AttachIcon(utilBone);
            }
        }

        private void SpawnFollowHierarchy()
        {
            Undo.RegisterCompleteObjectUndo(this.skeletonUtility, "Spawn Hierarchy");
            Selection.activeGameObject = this.skeletonUtility.SpawnHierarchy(SkeletonUtilityBone.Mode.Follow, true, true, true);
            AttachIconsToChildren(this.skeletonUtility.boneRoot);
        }

        private void SpawnFollowHierarchyRootOnly()
        {
            Undo.RegisterCompleteObjectUndo(this.skeletonUtility, "Spawn Root");
            Selection.activeGameObject = this.skeletonUtility.SpawnRoot(SkeletonUtilityBone.Mode.Follow, true, true, true);
            AttachIconsToChildren(this.skeletonUtility.boneRoot);
        }

        private void SpawnOverrideHierarchy()
        {
            Undo.RegisterCompleteObjectUndo(this.skeletonUtility, "Spawn Hierarchy");
            Selection.activeGameObject = this.skeletonUtility.SpawnHierarchy(SkeletonUtilityBone.Mode.Override, true, true, true);
            AttachIconsToChildren(this.skeletonUtility.boneRoot);
        }

        private void SpawnOverrideHierarchyRootOnly()
        {
            Undo.RegisterCompleteObjectUndo(this.skeletonUtility, "Spawn Root");
            Selection.activeGameObject = this.skeletonUtility.SpawnRoot(SkeletonUtilityBone.Mode.Override, true, true, true);
            AttachIconsToChildren(this.skeletonUtility.boneRoot);
        }
    }

}
