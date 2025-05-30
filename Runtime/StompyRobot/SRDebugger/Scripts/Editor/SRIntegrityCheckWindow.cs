﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    [InitializeOnLoad]
    internal class SRIntegrityCheckWindow : EditorWindow
    {
        private List<IntegrityIssue> _results;
        private Vector2 _scrollPosition;

        private bool _applyingFix;
        private static bool _isOpen;

        static SRIntegrityCheckWindow()
        {
            // Delay call to prevent any UI stalls after compile complete.
            EditorApplication.delayCall += () =>
            {
                if (!_isOpen && SRDebugEditor.QuickIntegrityCheck().Any())
                {
                    Debug.Log("[SRDebugger] Some issues have been detected with SRDebugger, opening integrity check window.");
                    Open();
                }
            };
        }

        public static void Open()
        {
            var window = GetWindow<SRIntegrityCheckWindow>(true, "SRDebugger Integrity Check", true);
            window.minSize = new Vector2(640, 400);
            window.Show();
        }

        private void OnEnable()
        {
            _isOpen = true;
            this.RefreshIntegrityCheck();
        }

        private void OnDisable()
        {
            _isOpen = false;
        }

        public void RefreshIntegrityCheck()
        {
            this._results = SRDebugEditor.QuickIntegrityCheck().ToList();
        }

        private void OnGUI()
        {
            // Draw header area 
            SRInternalEditorUtil.BeginDrawBackground();
            SRInternalEditorUtil.DrawLogo(SRInternalEditorUtil.GetLogo());
            SRInternalEditorUtil.EndDrawBackground();

            // Draw header/content divider
            EditorGUILayout.BeginVertical(SRInternalEditorUtil.Styles.SettingsHeaderBoxStyle);
            EditorGUILayout.EndVertical();

            GUILayout.Label(
                "SRDebugger automatically scans your project to find common issues with the SRDebugger installation.");

            EditorGUILayout.Space();

            // TODO: Enable button when there are some more 'expensive' integrity checks. For now no point as alt the checks are really quick
            if (GUILayout.Button("Refresh"))
            {
                this.RefreshIntegrityCheck();
            }

            if (this._applyingFix)
            {
                if (!EditorApplication.isCompiling && !EditorApplication.isUpdating)
                {
                    this._applyingFix = false;
                    this.RefreshIntegrityCheck();
                }

                EditorGUI.BeginDisabledGroup(this._applyingFix);
            }

            EditorGUILayout.Space();

            if (this._results == null)
            {
                this._results = new List<IntegrityIssue>();
            }

            EditorGUILayout.TextArea("Issues Detected: " + this._results.Count, EditorStyles.boldLabel);

            EditorGUILayout.Separator();

            EditorGUILayout.Space();

            if (this._results.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues have been found!", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("It is highly recommended to backup your project before using this tool.", MessageType.Warning);

                this._scrollPosition = GUILayout.BeginScrollView(this._scrollPosition, false, false,
                    GUILayout.Width(this.position.width));

                this.DrawIssuesList();

                EditorGUILayout.EndScrollView();

            }

            if (this._applyingFix)
            {
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawIssuesList()
        {
            EditorGUILayout.BeginVertical();

            for (var i = 0; i < this._results.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                GUILayout.Label(this._results[i].Title, EditorStyles.boldLabel);
                GUILayout.Label(this._results[i].Description, SRInternalEditorUtil.Styles.ParagraphLabel);

                var fixes = this._results[i].GetFixes();
                if (fixes.Count > 0)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.BeginVertical();

                    GUILayout.Label("Possible Fixes:", EditorStyles.miniBoldLabel);

                    foreach (var fix in fixes)
                    {
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        GUILayout.Space(10);

                        EditorGUILayout.BeginVertical();

                        GUILayout.Label(fix.Name, EditorStyles.boldLabel);

                        GUILayout.Label(fix.Description, SRInternalEditorUtil.Styles.ParagraphLabelItalic);

                        if (fix.IsAutoFix && GUILayout.Button("Apply Fix", GUILayout.Width(90)))
                        {
                            fix.Execute();
                            this._applyingFix = true;
                        }

                        GUILayout.Space(2);

                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndVertical();
        }
    }
}