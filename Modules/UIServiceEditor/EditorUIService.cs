// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.UI;
using UnityEditor.ShortcutManagement;
using UnityEditor.StyleSheets;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor
{
    internal class EditorUIServiceImpl : IEditorUIService
    {
        // This is called on beforeProcessingInitializeOnLoad callback to ensure
        // the instance is set before InitializeOnLoad attributes are processed
        [RequiredByNativeCode]
        static void InitializeInstance()
        {
            if (EditorUIService.instance == null)
                EditorUIService.instance = new EditorUIServiceImpl();
        }

        private EditorUIServiceImpl()
        {}

        public IWindowBackend GetDefaultWindowBackend(IWindowModel model) => model is IEditorWindowModel ? new DefaultEditorWindowBackend() : new DefaultWindowBackend();

        public Type GetDefaultToolbarType() => typeof(DefaultMainToolbar);
        public void AddSubToolbar(SubToolbar subToolbar) => MainToolbarImguiContainer.AddDeprecatedSubToolbar(subToolbar);

        public IEditorElement CreateEditorElement(int editorIndex, IPropertyView iw, string title) => new EditorElement(editorIndex, iw) {name = title};

        public IEditorElement CreateCulledEditorElement(int editorIndex, IPropertyView iw, string title) => new EditorElement(editorIndex, iw, true) {name = title};

        public void PackageManagerOpen() => PackageManagerWindow.OpenPackageManager(null);

        public IShortcutManagerWindowView CreateShortcutManagerWindowView(IShortcutManagerWindowViewController viewController, IKeyBindingStateProvider bindingStateProvider) =>
            new ShortcutManagerWindowView(viewController, bindingStateProvider);

        public void ProgressWindowShowDetails(bool shouldReposition) => ProgressWindow.ShowDetails(shouldReposition);
        public void ProgressWindowHideDetails() => ProgressWindow.HideDetails();
        public bool ProgressWindowCanHideDetails() => ProgressWindow.canHideDetails;

        public void AddDefaultEditorStyleSheets(VisualElement ve) => UIElementsEditorUtility.AddDefaultEditorStyleSheets(ve);
        public string GetUIToolkitDefaultCommonDarkStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonDarkStyleSheetPath;
        public string GetUIToolkitDefaultCommonLightStyleSheetPath() => UIElementsEditorUtility.s_DefaultCommonLightStyleSheetPath;
        public StyleSheet GetUIToolkitDefaultCommonDarkStyleSheet() => UIElementsEditorUtility.GetCommonDarkStyleSheet();
        public StyleSheet GetUIToolkitDefaultCommonLightStyleSheet() => UIElementsEditorUtility.GetCommonLightStyleSheet();

        public StyleSheet CompileStyleSheetContent(string styleSheetContent, bool disableValidation, bool reportErrors)
        {
            var importer = new StyleSheetImporterImpl();
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            importer.disableValidation = disableValidation;
            importer.Import(styleSheet, styleSheetContent);
            if (reportErrors)
            {
                foreach (var err in importer.importErrors)
                    Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, styleSheet, err.ToString());
            }
            return styleSheet;
        }

        public BindableElement CreateFloatField(string name, Func<float, float> onValidateValue = null, bool isDelayed = false)
        {
            return SetupField(name, onValidateValue, isDelayed, new FloatField());
        }

        public BindableElement CreateDoubleField(string name, Func<double, double> onValidateValue = null, bool isDelayed = false)
        {
            return SetupField(name, onValidateValue, isDelayed, new DoubleField());
        }

        public BindableElement CreateIntField(string name, Func<int, int> onValidateValue = null, bool isDelayed = false)
        {
            return SetupField(name, onValidateValue, isDelayed, new IntegerField());
        }

        public BindableElement CreateLongField(string name, Func<long, long> onValidateValue = null, bool isDelayed = false)
        {
            return SetupField(name, onValidateValue, isDelayed, new LongField());
        }

        public BindableElement CreateVector2Field(string name, Func<Vector2, Vector2> onValidateValue)
        {
            return SetupField(name, onValidateValue, new Vector2Field());
        }

        public BindableElement CreateVector2IntField(string name, Func<Vector2Int, Vector2Int> onValidateValue)
        {
            return SetupField(name, onValidateValue, new Vector2IntField());
        }

        public BindableElement CreateVector3Field(string name, Func<Vector3, Vector3> onValidateValue)
        {
            return SetupField(name, onValidateValue, new Vector3Field());
        }

        public BindableElement CreateVector3IntField(string name, Func<Vector3Int, Vector3Int> onValidateValue)
        {
            return SetupField(name, onValidateValue, new Vector3IntField());
        }

        public BindableElement CreateVector4Field(string name, Func<Vector4, Vector4> onValidateValue)
        {
            return SetupField(name, onValidateValue, new Vector4Field());
        }

        public BindableElement CreateTextField(string name = null, bool isMultiLine = false, bool isDelayed = false)
        {
            var field = new TextField(name);
            field.multiline = isMultiLine;
            field.isDelayed = isDelayed;
            return field;
        }

        public BindableElement CreateColorField(string name, bool showAlpha, bool hdr)
        {
            var field = new ColorField(name);
            field.showAlpha = showAlpha;
            field.hdr = hdr;
            return field;
        }

        public BindableElement CreateGradientField(string name, bool hdr, ColorSpace colorSpace)
        {
            var field = new GradientField(name);
            field.colorSpace = colorSpace;
            field.hdr = hdr;
            return field;
        }

        private TFieldType SetupField<TFieldType, TValueType>(string name, Func<TValueType, TValueType> onValidate, bool isDelayed, TFieldType field) where TFieldType : TextInputBaseField<TValueType>
        {
            SetupField(name, onValidate, field);

            field.isDelayed = isDelayed;

            return field;
        }

        private TFieldType SetupField<TFieldType, TValueType>(string name, Func<TValueType, TValueType> onValidate, TFieldType field) where TFieldType : BaseField<TValueType>
        {
            field.label = name;

            if (onValidate != null)
            {
                field.onValidateValue += onValidate;
            }

            return field;
        }
    }
}
