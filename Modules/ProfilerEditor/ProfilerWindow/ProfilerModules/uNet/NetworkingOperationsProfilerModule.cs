// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Network Operations", typeof(LocalizationResource), IconPath = "Profiler.NetworkOperations")]
    internal class NetworkingOperationsProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 9;

        [SerializeField]
        SplitterState m_NetworkSplit;

        static SVC<Color> s_SeparatorColor = new SVC<Color>("--theme-profiler-border-color-darker", Color.black);
        static List<ProfilerCounterData> s_CounterData = new List<ProfilerCounterData>();

        public NetworkingOperationsProfilerModule() : base()
        {
            InitCounterOverride();
        }

        private void InitCounterOverride()
        {
            if (NetworkingOperationsProfilerOverrides.getCustomChartCounters != null)
            {
                s_CounterData.Clear();

                var chartCounters = NetworkingOperationsProfilerOverrides.getCustomChartCounters.Invoke();
                if (chartCounters != null)
                {
                    // If the Capcity is the same value a re-alloc will not happen
                    s_CounterData.Capacity = chartCounters.Count;
                    chartCounters.ToProfilerCounter(s_CounterData);
                }

                SetCounters(s_CounterData, s_CounterData);
            }
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            if (NetworkingOperationsProfilerOverrides.getCustomChartCounters != null)
            {
                s_CounterData.Clear();

                var chartCounters = NetworkingOperationsProfilerOverrides.getCustomChartCounters.Invoke();
                if (chartCounters != null)
                {
                    // If the Capcity is the same value a re-alloc will not happen
                    s_CounterData.Capacity = chartCounters.Count;
                    chartCounters.ToProfilerCounter(s_CounterData);
                }

                return s_CounterData;
            }

            return base.CollectDefaultChartCounters();
        }

        internal override ProfilerArea area => ProfilerArea.NetworkOperations;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartNetworkOperations";

        private protected override ProfilerChart InstantiateChart(float defaultChartScale, float chartMaximumScaleInterpolationValue)
        {
            var chart = base.InstantiateChart(defaultChartScale, chartMaximumScaleInterpolationValue);
            chart.m_SharedScale = true;
            return chart;
        }

        internal override void OnEnable()
        {
            base.OnEnable();

            if (m_NetworkSplit == null || !m_NetworkSplit.IsValid())
                m_NetworkSplit = SplitterState.FromRelative(new[] { 20f, 80f }, new[] { 100f, 100f }, null);
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawDetailsView(Rect position)
        {
            if (NetworkingOperationsProfilerOverrides.drawDetailsViewOverride != null)
                NetworkingOperationsProfilerOverrides.drawDetailsViewOverride.Invoke(position, ProfilerWindow.GetActiveVisibleFrameIndex());
            else
                DrawNetworkOperationsPane(position);
        }

        [SerializeField]
        private String[] msgNames =
        {
            "UserMessage", "ObjectDestroy", "ClientRpc", "ObjectSpawn", "Owner", "Command", "LocalPlayerTransform", "SyncEvent", "SyncVars", "SyncList", "ObjectSpawnScene", "NetworkInfo", "SpawnFinished", "ObjectHide", "CRC", "ClientAuthority"
        };

        private bool[] msgFoldouts = { true, true, true, true, true, true, true, true, true, true, true, true, true, true, true };

        void DrawNetworkOperationsPane(Rect position)
        {
            SplitterGUILayout.BeginHorizontalSplit(m_NetworkSplit);
            var overviewLabel = GUIContent.Temp(ProfilerDriver.GetOverviewText(area,
                ProfilerWindow.GetActiveVisibleFrameIndex()));
            var labelRect = GUILayoutUtility.GetRect(overviewLabel, EditorStyles.wordWrappedLabel);

            GUI.Label(labelRect, overviewLabel, EditorStyles.wordWrappedLabel);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Operation Detail");
            EditorGUILayout.LabelField("Over 5 Ticks");
            EditorGUILayout.LabelField("Over 10 Ticks");
            EditorGUILayout.LabelField("Total");
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel += 1;

            for (short msgId = 0; msgId < msgNames.Length; msgId++)
            {
#pragma warning disable CS0618
                if (!NetworkDetailStats.m_NetworkOperations.ContainsKey(msgId))
#pragma warning restore
                    continue;

                msgFoldouts[msgId] = EditorGUILayout.Foldout(msgFoldouts[msgId], msgNames[msgId] + ":");
                if (msgFoldouts[msgId])
                {
                    EditorGUILayout.BeginVertical();
#pragma warning disable CS0618
                    var detail = NetworkDetailStats.m_NetworkOperations[msgId];
#pragma warning restore

                    EditorGUI.indentLevel += 1;

                    foreach (var entryName in detail.m_Entries.Keys)
                    {
                        int tick = (int)Time.time;
                        var entry = detail.m_Entries[entryName];

                        if (entry.m_IncomingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("IN:" + entryName);
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_IncomingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }

                        if (entry.m_OutgoingTotal > 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("OUT:" + entryName);
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetFiveTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingSequence.GetTenTick(tick).ToString());
                            EditorGUILayout.LabelField(entry.m_OutgoingTotal.ToString());
                            EditorGUILayout.EndHorizontal();
                        }
                    }

                    EditorGUI.indentLevel -= 1;
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUI.indentLevel -= 1;
            GUILayout.EndScrollView();
            SplitterGUILayout.EndHorizontalSplit();

            // Draw separator
            var lineRect = new Rect(m_NetworkSplit.realSizes[0] + labelRect.xMin, position.y - EditorGUI.kWindowToolbarHeight - 1, 1,
                position.height + EditorGUI.kWindowToolbarHeight);
            EditorGUI.DrawRect(lineRect, s_SeparatorColor);
        }
    }
}
