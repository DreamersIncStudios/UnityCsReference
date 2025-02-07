// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Build;
using System.Linq;
using UnityEditorInternal;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(QualitySettings))]
    internal class QualitySettingsEditor : ProjectSettingsBaseEditor
    {
        private class Content
        {
            public static readonly GUIContent kPlatformTooltip = EditorGUIUtility.TrTextContent("", "Allow quality setting on platform");
            public static readonly GUIContent kAddQualityLevel = EditorGUIUtility.TrTextContent("Add Quality Level");
            public static readonly GUIContent kStreamingMipmapsActive = EditorGUIUtility.TrTextContent("Texture Streaming", "When enabled, Unity only streams texture mipmaps relevant to the current Camera's position in a Scene. This reduces the total amount of memory Unity needs for textures. Individual textures must also have 'Streaming Mip Maps' enabled in their Import Settings.");
            public static readonly GUIContent kStreamingMipmapsMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "The amount of memory (in megabytes) to allocate for all loaded textures.");
            public static readonly GUIContent kStreamingMipmapsRenderersPerFrame = EditorGUIUtility.TrTextContent("Renderers Per Frame", "The number of renderers to process each frame. A lower number decreases the CPU load but delays mipmap loading.");
            public static readonly GUIContent kStreamingMipmapsAddAllCameras = EditorGUIUtility.TrTextContent("Add All Cameras", "When enabled, Unity uses texture streaming for every Camera in the Scene. Otherwise, Unity only uses texture streaming for Cameras that have an attached Streaming Controller component.");
            public static readonly GUIContent kStreamingMipmapsMaxLevelReduction = EditorGUIUtility.TrTextContent("Max Level Reduction", "The maximum number of mipmap levels a texture can drop.");
            public static readonly GUIContent kStreamingMipmapsMaxFileIORequests = EditorGUIUtility.TrTextContent("Max IO Requests", "The maximum number of texture file requests from the Texture Streaming system that can be active at the same time.");

            public static readonly GUIContent kIconTrash = EditorGUIUtility.TrIconContent("TreeEditor.Trash", "Delete Level");
            public static readonly GUIContent kSoftParticlesHint = EditorGUIUtility.TrTextContent("Soft Particles require either the Deferred Shading rendering path or Cameras that render depth textures.");
            public static readonly GUIContent kBillboardsFaceCameraPos = EditorGUIUtility.TrTextContent("Billboards Face Camera Position", "When enabled, terrain billboards face towards the camera position. Otherwise, they face towards the camera plane. This makes billboards look nicer when the camera rotates but it is more resource intensive to process.");
            public static readonly GUIContent kVSyncCountLabel = EditorGUIUtility.TrTextContent("VSync Count", "Specifies how Unity synchronizes rendering with the refresh rate of the display device.");
            public static readonly GUIContent kLODBiasLabel = EditorGUIUtility.TrTextContent("LOD Bias", "The bias Unity uses to determine which model to render when a GameObject’s on-screen size is between two LOD levels. Values between 0 and 1 favor the less detailed model. Values greater than 1 favor the more detailed model.");
            public static readonly GUIContent kMaximumLODLevelLabel = EditorGUIUtility.TrTextContent("Maximum LOD Level", "The highest LOD to use in the application.");
            public static readonly GUIContent kMipStrippingHint = EditorGUIUtility.TrTextContent("Where the maximum possible texture mip resolution for a platform is less than full, enabling Texture MipMap Stripping in Player Settings can reduce the package size.");

            public static readonly GUIContent kAsyncUploadTimeSlice = EditorGUIUtility.TrTextContent("Time Slice", "The amount of time (in milliseconds) Unity spends uploading Texture and Mesh data to the GPU per frame.");
            public static readonly GUIContent kAsyncUploadBufferSize = EditorGUIUtility.TrTextContent("Buffer Size", "The size (in megabytes) of the upload buffer Unity uses to stream Texture and Mesh data to GPU.");
            public static readonly GUIContent kAsyncUploadPersistentBuffer = EditorGUIUtility.TrTextContent("Persistent Buffer", "When enabled, the upload buffer persists even when there is nothing left to upload.");

            public static readonly GUIContent kRenderPipelineObject = EditorGUIUtility.TrTextContent("Render Pipeline Asset", "Specifies the Render Pipeline Asset to use for this quality level. It overrides the value set in the Graphics Settings Window.");
        }

        private class Styles
        {
            public static readonly GUIStyle kToggle = "OL Toggle";
            public static readonly GUIStyle kDefaultToggle = "OL ToggleWhite";

            public static readonly GUIStyle kListEvenBg = "ObjectPickerResultsOdd";
            public static readonly GUIStyle kListOddBg = "ObjectPickerResultsEven";
            public static readonly GUIStyle kDefaultDropdown = "QualitySettingsDefault";

            public const int kMinToggleWidth = 15;
            public const int kMaxToggleWidth = 20;
            public const int kHeaderRowHeight = 20;
            public const int kLabelWidth = 80;
        }

        public const int kMinAsyncRingBufferSize = 2;
        public const int kMaxAsyncRingBufferSize = 512;
        public const int kMinAsyncUploadTimeSlice = 1;
        public const int kMaxAsyncUploadTimeSlice = 33;

        private SerializedObject m_QualitySettings;
        private SerializedProperty m_QualitySettingsProperty;
        private SerializedProperty m_PerPlatformDefaultQualityProperty;
        private List<BuildPlatform> m_ValidPlatforms;

        public void OnEnable()
        {
            m_QualitySettings = new SerializedObject(target);
            m_QualitySettingsProperty = m_QualitySettings.FindProperty("m_QualitySettings");
            m_PerPlatformDefaultQualityProperty = m_QualitySettings.FindProperty("m_PerPlatformDefaultQuality");
            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms();
        }

        private struct QualitySetting
        {
            public string m_Name;
            public string m_PropertyPath;
            public List<string> m_ExcludedPlatforms;
        }

        private readonly int m_QualityElementHash = "QualityElementHash".GetHashCode();
        private class Dragging
        {
            public int m_StartPosition;
            public int m_Position;
        }

        private Dragging m_Dragging;
        private bool m_ShouldAddNewLevel;
        private int m_DeleteLevel = -1;
        private int DoQualityLevelSelection(int currentQualitylevel, IList<QualitySetting> qualitySettings, Dictionary<string, int> platformDefaultQualitySettings)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            var selectedLevel = currentQualitylevel;

            //Header row
            GUILayout.BeginHorizontal();

            Rect header = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            header.x += EditorGUI.indent;
            header.width -= EditorGUI.indent;
            GUI.Label(header, "Levels", EditorStyles.boldLabel);

            //Header row icons
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));
                var temp = EditorGUIUtility.TempContent(platform.smallIcon);
                temp.tooltip = platform.title.text;
                GUI.Label(iconRect, temp);
                temp.tooltip = "";
            }

            //Extra column for deleting setting button
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));

            GUILayout.EndHorizontal();

            //Draw the row for each quality setting
            var currentEvent = Event.current;
            for (var i = 0; i < qualitySettings.Count; i++)
            {
                GUILayout.BeginHorizontal();
                var bgStyle = i % 2 == 0 ? Styles.kListEvenBg : Styles.kListOddBg;
                bool selected = (selectedLevel == i);

                //Draw the selected icon if required
                Rect r = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth));

                switch (currentEvent.type)
                {
                    case EventType.Repaint:
                        bgStyle.Draw(r, GUIContent.none, false, false, selected, false);
                        GUI.Label(r, EditorGUIUtility.TempContent(qualitySettings[i].m_Name));
                        break;
                    case EventType.MouseDown:
                        if (r.Contains(currentEvent.mousePosition))
                        {
                            selectedLevel = i;
                            GUIUtility.keyboardControl = 0;
                            GUIUtility.hotControl = m_QualityElementHash;
                            GUI.changed = true;
                            m_Dragging = new Dragging {m_StartPosition = i, m_Position = i};
                            currentEvent.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            if (r.Contains(currentEvent.mousePosition))
                            {
                                m_Dragging.m_Position = i;
                                currentEvent.Use();
                            }
                        }
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == m_QualityElementHash)
                        {
                            GUIUtility.hotControl = 0;
                            currentEvent.Use();
                        }
                        break;
                    case EventType.KeyDown:
                        if (currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.DownArrow)
                        {
                            selectedLevel += currentEvent.keyCode == KeyCode.UpArrow ? -1 : 1;
                            selectedLevel = Mathf.Clamp(selectedLevel, 0, qualitySettings.Count - 1);
                            GUIUtility.keyboardControl = 0;
                            GUI.changed = true;
                            currentEvent.Use();
                        }
                        break;
                }

                //Build a list of the current platform selection and draw it.
                foreach (var platform in m_ValidPlatforms)
                {
                    bool isDefaultQuality = platformDefaultQualitySettings.ContainsKey(platform.name) &&  platformDefaultQualitySettings[platform.name] == i;

                    var toggleRect = GUILayoutUtility.GetRect(Content.kPlatformTooltip, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                    if (Event.current.type == EventType.Repaint)
                    {
                        bgStyle.Draw(toggleRect, GUIContent.none, false, false, selected, false);
                    }

                    var color = GUI.backgroundColor;
                    if (isDefaultQuality && !EditorApplication.isPlayingOrWillChangePlaymode)
                        GUI.backgroundColor = Color.green;

                    var supported = !qualitySettings[i].m_ExcludedPlatforms.Contains(platform.name);
                    var newSupported = GUI.Toggle(toggleRect, supported, Content.kPlatformTooltip, isDefaultQuality ? Styles.kDefaultToggle : Styles.kToggle);
                    if (supported != newSupported)
                    {
                        if (newSupported)
                            qualitySettings[i].m_ExcludedPlatforms.Remove(platform.name);
                        else
                            qualitySettings[i].m_ExcludedPlatforms.Add(platform.name);
                    }

                    GUI.backgroundColor = color;
                }

                //Extra column for deleting quality button
                var deleteButton = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth));
                if (Event.current.type == EventType.Repaint)
                {
                    bgStyle.Draw(deleteButton, GUIContent.none, false, false, selected, false);
                }
                if (GUI.Button(deleteButton, Content.kIconTrash, GUIStyle.none))
                    m_DeleteLevel = i;
                GUILayout.EndHorizontal();
            }

            //Add a spacer line to separate the levels from the defaults
            GUILayout.BeginHorizontal();
            DrawHorizontalDivider();
            GUILayout.EndHorizontal();

            //Default platform selection dropdowns
            GUILayout.BeginHorizontal();

            var defaultQualityTitle = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.ExpandWidth(false), GUILayout.Width(Styles.kLabelWidth), GUILayout.Height(Styles.kHeaderRowHeight));
            defaultQualityTitle.x += EditorGUI.indent;
            defaultQualityTitle.width -= EditorGUI.indent;
            GUI.Label(defaultQualityTitle, "Default", EditorStyles.boldLabel);

            // Draw default dropdown arrows
            foreach (var platform in m_ValidPlatforms)
            {
                var iconRect = GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle,
                    GUILayout.MinWidth(Styles.kMinToggleWidth),
                    GUILayout.MaxWidth(Styles.kMaxToggleWidth),
                    GUILayout.Height(Styles.kHeaderRowHeight));

                int position;
                if (!platformDefaultQualitySettings.TryGetValue(platform.name, out position))
                    platformDefaultQualitySettings.Add(platform.name, 0);

                position = EditorGUI.Popup(iconRect, position, qualitySettings.Select(x => x.m_Name).ToArray(), Styles.kDefaultDropdown);
                platformDefaultQualitySettings[platform.name] = position;
            }

            //Extra column for deleting setting button
            GUILayoutUtility.GetRect(GUIContent.none, Styles.kToggle, GUILayout.MinWidth(Styles.kMinToggleWidth), GUILayout.MaxWidth(Styles.kMaxToggleWidth), GUILayout.Height(Styles.kHeaderRowHeight));

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            //Add an extra row for 'Add' button
            GUILayout.BeginHorizontal();
            var addButtonRect = GUILayoutUtility.GetRect(Content.kAddQualityLevel, Styles.kToggle, GUILayout.ExpandWidth(true));

            if (GUI.Button(addButtonRect, Content.kAddQualityLevel))
                m_ShouldAddNewLevel = true;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            return selectedLevel;
        }

        private List<QualitySetting> GetQualitySettings()
        {
            // Pull the quality settings from the runtime.
            var qualitySettings = new List<QualitySetting>();

            foreach (SerializedProperty prop in m_QualitySettingsProperty)
            {
                var qs = new QualitySetting
                {
                    m_Name = prop.FindPropertyRelative("name").stringValue,
                    m_PropertyPath = prop.propertyPath
                };

                qs.m_PropertyPath = prop.propertyPath;

                var platforms = new List<string>();
                var platformsProp = prop.FindPropertyRelative("excludedTargetPlatforms");
                foreach (SerializedProperty platformProp in platformsProp)
                    platforms.Add(platformProp.stringValue);

                qs.m_ExcludedPlatforms = platforms;
                qualitySettings.Add(qs);
            }
            return qualitySettings;
        }

        private void SetQualitySettings(IEnumerable<QualitySetting> settings)
        {
            foreach (var setting in settings)
            {
                var property = m_QualitySettings.FindProperty(setting.m_PropertyPath);
                if (property == null)
                    continue;

                var platformsProp = property.FindPropertyRelative("excludedTargetPlatforms");
                if (platformsProp.arraySize != setting.m_ExcludedPlatforms.Count)
                    platformsProp.arraySize = setting.m_ExcludedPlatforms.Count;

                var count = 0;
                foreach (SerializedProperty platform in platformsProp)
                {
                    if (platform.stringValue != setting.m_ExcludedPlatforms[count])
                        platform.stringValue = setting.m_ExcludedPlatforms[count];
                    count++;
                }
            }
        }

        private void HandleAddRemoveQualitySetting(ref int selectedLevel, Dictionary<string, int> platformDefaults)
        {
            if (m_DeleteLevel >= 0)
            {
                if (m_DeleteLevel < selectedLevel || m_DeleteLevel == m_QualitySettingsProperty.arraySize - 1)
                {
                    selectedLevel = Mathf.Max(0, selectedLevel - 1);
                    QualitySettings.SetQualityLevel(selectedLevel);
                }

                //Always ensure there is one quality setting
                if (m_QualitySettingsProperty.arraySize > 1 && m_DeleteLevel >= 0 && m_DeleteLevel < m_QualitySettingsProperty.arraySize)
                {
                    m_QualitySettingsProperty.DeleteArrayElementAtIndex(m_DeleteLevel);

                    // Fix defaults offset
                    List<string> keys = new List<string>(platformDefaults.Keys);
                    foreach (var key in keys)
                    {
                        int value = platformDefaults[key];
                        if (value != 0 && value >= m_DeleteLevel)
                            platformDefaults[key]--;
                    }
                }

                m_DeleteLevel = -1;
            }

            if (m_ShouldAddNewLevel)
            {
                m_QualitySettingsProperty.arraySize++;
                var addedSetting = m_QualitySettingsProperty.GetArrayElementAtIndex(m_QualitySettingsProperty.arraySize - 1);
                var nameProperty = addedSetting.FindPropertyRelative("name");
                nameProperty.stringValue = "Level " + (m_QualitySettingsProperty.arraySize - 1);

                m_ShouldAddNewLevel = false;
            }
        }

        private Dictionary<string, int> GetDefaultQualityForPlatforms()
        {
            var defaultPlatformQualities = new Dictionary<string, int>();

            foreach (SerializedProperty prop in m_PerPlatformDefaultQualityProperty)
            {
                defaultPlatformQualities.Add(prop.FindPropertyRelative("first").stringValue, prop.FindPropertyRelative("second").intValue);
            }
            return defaultPlatformQualities;
        }

        private void SetDefaultQualityForPlatforms(Dictionary<string, int> platformDefaults)
        {
            if (m_PerPlatformDefaultQualityProperty.arraySize != platformDefaults.Count)
                m_PerPlatformDefaultQualityProperty.arraySize = platformDefaults.Count;

            var count = 0;
            foreach (var def in platformDefaults)
            {
                var element = m_PerPlatformDefaultQualityProperty.GetArrayElementAtIndex(count);
                var firstProperty = element.FindPropertyRelative("first");
                var secondProperty = element.FindPropertyRelative("second");

                if (firstProperty.stringValue != def.Key || secondProperty.intValue != def.Value)
                {
                    firstProperty.stringValue = def.Key;
                    secondProperty.intValue = def.Value;
                }
                count++;
            }
        }

        private static void DrawHorizontalDivider()
        {
            var spacerLine = GUILayoutUtility.GetRect(GUIContent.none,
                GUIStyle.none,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(1));
            var oldBgColor = GUI.backgroundColor;
            if (EditorGUIUtility.isProSkin)
                GUI.backgroundColor = oldBgColor * 0.7058f;
            else
                GUI.backgroundColor = Color.black;

            if (Event.current.type == EventType.Repaint)
                EditorGUIUtility.whiteTextureStyle.Draw(spacerLine, GUIContent.none, false, false, false, false);

            GUI.backgroundColor = oldBgColor;
        }

        void SoftParticlesHintGUI()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RenderingPath renderPath = mainCamera.actualRenderingPath;
            if (renderPath == RenderingPath.DeferredLighting || renderPath == RenderingPath.DeferredShading)
                return; // using deferred, all is good

            if ((mainCamera.depthTextureMode & DepthTextureMode.Depth) != 0)
                return; // already produces depth texture, all is good

            EditorGUILayout.HelpBox(Content.kSoftParticlesHint.text, MessageType.Warning, false);
        }

        void MipStrippingHintGUI()
        {
            if (PlayerSettings.mipStripping)
                return;

            EditorGUILayout.HelpBox(Content.kMipStrippingHint.text, MessageType.Info, false);
        }

        /**
         * Internal function that takes the shadow cascade splits property field, and dispatches a call to render the GUI.
         * It also transfers the result back
         */

        private void DrawCascadeSplitGUI<T>(ref SerializedProperty shadowCascadeSplit)
        {
            float[] cascadePartitionSizes = null;

            System.Type type = typeof(T);
            if (type == typeof(float))
                cascadePartitionSizes = new float[] { shadowCascadeSplit.floatValue };
            else if (type == typeof(Vector3))
            {
                Vector3 splits = shadowCascadeSplit.vector3Value;
                cascadePartitionSizes = new float[]
                {
                    Mathf.Clamp(splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[1] - splits[0], 0.0f, 1.0f),
                    Mathf.Clamp(splits[2] - splits[1], 0.0f, 1.0f)
                };
            }

            if (cascadePartitionSizes != null)
            {
                EditorGUI.BeginChangeCheck();
                ShadowCascadeSplitGUI.HandleCascadeSliderGUI(ref cascadePartitionSizes);
                if (EditorGUI.EndChangeCheck())
                {
                    if (type == typeof(float))
                        shadowCascadeSplit.floatValue = cascadePartitionSizes[0];
                    else
                    {
                        Vector3 updatedValue = new Vector3();
                        updatedValue[0] = cascadePartitionSizes[0];
                        updatedValue[1] = updatedValue[0] + cascadePartitionSizes[1];
                        updatedValue[2] = updatedValue[1] + cascadePartitionSizes[2];
                        shadowCascadeSplit.vector3Value = updatedValue;
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox("Changes made in play mode will not be saved.", MessageType.Warning, true);
            }

            m_QualitySettings.Update();

            var settings = GetQualitySettings();
            var defaults = GetDefaultQualityForPlatforms();
            var selectedLevel = QualitySettings.GetQualityLevel();
            if (selectedLevel >= m_QualitySettingsProperty.arraySize)
            {
                selectedLevel = m_QualitySettingsProperty.arraySize - 1;
            }

            EditorGUI.BeginChangeCheck();
            selectedLevel = DoQualityLevelSelection(selectedLevel, settings, defaults);
            if (EditorGUI.EndChangeCheck())
                QualitySettings.SetQualityLevel(selectedLevel);

            SetQualitySettings(settings);
            HandleAddRemoveQualitySetting(ref selectedLevel, defaults);
            SetDefaultQualityForPlatforms(defaults);
            GUILayout.Space(10.0f);
            DrawHorizontalDivider();
            GUILayout.Space(10.0f);

            var currentSettings = m_QualitySettingsProperty.GetArrayElementAtIndex(selectedLevel);
            var nameProperty = currentSettings.FindPropertyRelative("name");
            var pixelLightCountProperty = currentSettings.FindPropertyRelative("pixelLightCount");
            var shadowsProperty = currentSettings.FindPropertyRelative("shadows");
            var shadowResolutionProperty = currentSettings.FindPropertyRelative("shadowResolution");
            var shadowProjectionProperty = currentSettings.FindPropertyRelative("shadowProjection");
            var shadowCascadesProperty = currentSettings.FindPropertyRelative("shadowCascades");
            var shadowDistanceProperty = currentSettings.FindPropertyRelative("shadowDistance");
            var shadowNearPlaneOffsetProperty = currentSettings.FindPropertyRelative("shadowNearPlaneOffset");
            var shadowCascade2SplitProperty = currentSettings.FindPropertyRelative("shadowCascade2Split");
            var shadowCascade4SplitProperty = currentSettings.FindPropertyRelative("shadowCascade4Split");
            var shadowMaskUsageProperty = currentSettings.FindPropertyRelative("shadowmaskMode");
            var skinWeightsProperty = currentSettings.FindPropertyRelative("skinWeights");
            var textureQualityProperty = currentSettings.FindPropertyRelative("textureQuality");
            var anisotropicTexturesProperty = currentSettings.FindPropertyRelative("anisotropicTextures");
            var antiAliasingProperty = currentSettings.FindPropertyRelative("antiAliasing");
            var softParticlesProperty = currentSettings.FindPropertyRelative("softParticles");
            var realtimeReflectionProbes = currentSettings.FindPropertyRelative("realtimeReflectionProbes");
            var billboardsFaceCameraPosition = currentSettings.FindPropertyRelative("billboardsFaceCameraPosition");
            var vSyncCountProperty = currentSettings.FindPropertyRelative("vSyncCount");
            var lodBiasProperty = currentSettings.FindPropertyRelative("lodBias");
            var maximumLODLevelProperty = currentSettings.FindPropertyRelative("maximumLODLevel");
            var particleRaycastBudgetProperty = currentSettings.FindPropertyRelative("particleRaycastBudget");
            var asyncUploadTimeSliceProperty = currentSettings.FindPropertyRelative("asyncUploadTimeSlice");
            var asyncUploadBufferSizeProperty = currentSettings.FindPropertyRelative("asyncUploadBufferSize");
            var asyncUploadPersistentBufferProperty = currentSettings.FindPropertyRelative("asyncUploadPersistentBuffer");
            var resolutionScalingFixedDPIFactorProperty = currentSettings.FindPropertyRelative("resolutionScalingFixedDPIFactor");

            var customRenderPipeline = currentSettings.FindPropertyRelative("customRenderPipeline");

            bool usingSRP = GraphicsSettings.currentRenderPipeline != null;

            if (string.IsNullOrEmpty(nameProperty.stringValue))
                nameProperty.stringValue = "Level " + selectedLevel;

            GUILayout.Label(EditorGUIUtility.TempContent("Current Active Quality Level"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(nameProperty);

            if (usingSRP)
                EditorGUILayout.HelpBox("A Scriptable Render Pipeline is in use, some settings will not be used and are hidden", MessageType.Info);
            GUILayout.Space(10);

            GUILayout.Label(EditorGUIUtility.TempContent("Rendering"), EditorStyles.boldLabel);

            RenderPipelineAssetSelector.Draw(Content.kRenderPipelineObject, m_QualitySettings, customRenderPipeline);
            if (!usingSRP)
                EditorGUILayout.PropertyField(pixelLightCountProperty);

            // still valid with SRP
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(antiAliasingProperty);
            }

            if (!SupportedRenderingFeatures.active.overridesRealtimeReflectionProbes)
                EditorGUILayout.PropertyField(realtimeReflectionProbes);
            EditorGUILayout.PropertyField(resolutionScalingFixedDPIFactorProperty);

            EditorGUILayout.PropertyField(vSyncCountProperty, Content.kVSyncCountLabel);

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS ||
                EditorUserBuildSettings.activeBuildTarget == BuildTarget.tvOS)
            {
                if (vSyncCountProperty.intValue > 0)
                    EditorGUILayout.HelpBox(EditorGUIUtility.TrTextContent($"VSync Count '{vSyncCountProperty.enumLocalizedDisplayNames[vSyncCountProperty.enumValueIndex]}' is ignored on Android, iOS and tvOS.", EditorGUIUtility.GetHelpIcon(MessageType.Warning)));
            }

            bool shadowMaskSupported = SupportedRenderingFeatures.IsMixedLightingModeSupported(MixedLightingMode.Shadowmask);
            bool showShadowMaskUsage = shadowMaskSupported && !SupportedRenderingFeatures.active.overridesShadowmask;

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Textures"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(textureQualityProperty);
            if (EditorGUI.EndChangeCheck() && usingSRP)
            {
                RenderPipelineManager.CleanupRenderPipeline();
            }
            if (QualitySettings.IsTextureResReducedOnAnyPlatform())
            {
                MipStrippingHintGUI();
            }
            EditorGUILayout.PropertyField(anisotropicTexturesProperty);

            var streamingMipmapsActiveProperty = currentSettings.FindPropertyRelative("streamingMipmapsActive");
            EditorGUILayout.PropertyField(streamingMipmapsActiveProperty, Content.kStreamingMipmapsActive);
            if (streamingMipmapsActiveProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                var streamingMipmapsAddAllCameras = currentSettings.FindPropertyRelative("streamingMipmapsAddAllCameras");
                EditorGUILayout.PropertyField(streamingMipmapsAddAllCameras, Content.kStreamingMipmapsAddAllCameras);
                var streamingMipmapsBudgetProperty = currentSettings.FindPropertyRelative("streamingMipmapsMemoryBudget");
                EditorGUILayout.PropertyField(streamingMipmapsBudgetProperty, Content.kStreamingMipmapsMemoryBudget);
                var streamingMipmapsRenderersPerFrameProperty = currentSettings.FindPropertyRelative("streamingMipmapsRenderersPerFrame");
                EditorGUILayout.PropertyField(streamingMipmapsRenderersPerFrameProperty, Content.kStreamingMipmapsRenderersPerFrame);
                var streamingMipmapsMaxLevelReductionProperty = currentSettings.FindPropertyRelative("streamingMipmapsMaxLevelReduction");
                EditorGUILayout.PropertyField(streamingMipmapsMaxLevelReductionProperty, Content.kStreamingMipmapsMaxLevelReduction);
                var streamingMipmapsMaxFileIORequestsProperty = currentSettings.FindPropertyRelative("streamingMipmapsMaxFileIORequests");
                EditorGUILayout.PropertyField(streamingMipmapsMaxFileIORequestsProperty, Content.kStreamingMipmapsMaxFileIORequests);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Particles"), EditorStyles.boldLabel);
            if (!usingSRP)
            {
                EditorGUILayout.PropertyField(softParticlesProperty);
                if (softParticlesProperty.boolValue)
                    SoftParticlesHintGUI();
            }
            EditorGUILayout.PropertyField(particleRaycastBudgetProperty);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Terrain"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(billboardsFaceCameraPosition, Content.kBillboardsFaceCameraPos);

            if (!usingSRP || showShadowMaskUsage)
            {
                GUILayout.Space(10);

                GUILayout.Label(EditorGUIUtility.TempContent("Shadows"), EditorStyles.boldLabel);

                if (showShadowMaskUsage)
                    EditorGUILayout.PropertyField(shadowMaskUsageProperty);

                if (!usingSRP)
                {
                    EditorGUILayout.PropertyField(shadowsProperty);
                    EditorGUILayout.PropertyField(shadowResolutionProperty);
                    EditorGUILayout.PropertyField(shadowProjectionProperty);
                    EditorGUILayout.PropertyField(shadowDistanceProperty);
                    EditorGUILayout.PropertyField(shadowNearPlaneOffsetProperty);
                    EditorGUILayout.PropertyField(shadowCascadesProperty);

                    if (shadowCascadesProperty.intValue == 2)
                        DrawCascadeSplitGUI<float>(ref shadowCascade2SplitProperty);
                    else if (shadowCascadesProperty.intValue == 4)
                        DrawCascadeSplitGUI<Vector3>(ref shadowCascade4SplitProperty);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Async Asset Upload"), EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(asyncUploadTimeSliceProperty, Content.kAsyncUploadTimeSlice);
            EditorGUILayout.PropertyField(asyncUploadBufferSizeProperty, Content.kAsyncUploadBufferSize);
            EditorGUILayout.PropertyField(asyncUploadPersistentBufferProperty, Content.kAsyncUploadPersistentBuffer);

            asyncUploadTimeSliceProperty.intValue = Mathf.Clamp(asyncUploadTimeSliceProperty.intValue, kMinAsyncUploadTimeSlice, kMaxAsyncUploadTimeSlice);
            asyncUploadBufferSizeProperty.intValue = Mathf.Clamp(asyncUploadBufferSizeProperty.intValue, kMinAsyncRingBufferSize, kMaxAsyncRingBufferSize);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Level of Detail"), EditorStyles.boldLabel);

            if (!SupportedRenderingFeatures.active.overridesLODBias)
                EditorGUILayout.PropertyField(lodBiasProperty, Content.kLODBiasLabel);
            if (!SupportedRenderingFeatures.active.overridesMaximumLODLevel)
                EditorGUILayout.PropertyField(maximumLODLevelProperty, Content.kMaximumLODLevelLabel);

            GUILayout.Space(10);
            GUILayout.Label(EditorGUIUtility.TempContent("Meshes"), EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skinWeightsProperty);

            if (m_Dragging != null && m_Dragging.m_Position != m_Dragging.m_StartPosition)
            {
                m_QualitySettingsProperty.MoveArrayElement(m_Dragging.m_StartPosition, m_Dragging.m_Position);
                m_Dragging.m_StartPosition = m_Dragging.m_Position;
                selectedLevel = m_Dragging.m_Position;

                m_QualitySettings.ApplyModifiedProperties();
                QualitySettings.SetQualityLevel(Mathf.Clamp(selectedLevel, 0, m_QualitySettingsProperty.arraySize - 1));
            }

            m_QualitySettings.ApplyModifiedProperties();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Quality", "ProjectSettings/QualitySettings.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>()
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>())
                    .Concat(SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/QualitySettings.asset")));
            return provider;
        }
    }
}
