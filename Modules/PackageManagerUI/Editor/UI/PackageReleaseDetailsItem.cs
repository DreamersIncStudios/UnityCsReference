// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageReleaseDetailsItem : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageReleaseDetailsItem> {}

        private ResourceLoader m_ResourceLoader;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        public PackageReleaseDetailsItem()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageReleaseDetailsItem.uxml");
            Add(root);
            cache = new VisualElementCache(root);
        }

        public PackageReleaseDetailsItem(string versionString, DateTime? publishedDate, string releaseNotes) : this()
        {
            var releaseDateString = publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));

            versionAndReleaseDateLabel.SetValueWithoutNotify(versionString + (releaseDateString != null ? $" - released on {releaseDateString}" : string.Empty));

            releaseNotesLabel.SetValueWithoutNotify(releaseNotes);

            lessButton.clicked += OnLessButtonClicked;
            moreButton.clicked += OnMoreButtonClicked;

            UIUtils.SetElementDisplay(releaseNotesContainer, false);
            UIUtils.SetElementDisplay(moreButton, !string.IsNullOrEmpty(releaseNotes));
        }

        public void OnLessButtonClicked()
        {
            UIUtils.SetElementDisplay(releaseNotesContainer, false);
            UIUtils.SetElementDisplay(moreButton, true);
        }

        public void OnMoreButtonClicked()
        {
            UIUtils.SetElementDisplay(releaseNotesContainer, true);
            UIUtils.SetElementDisplay(moreButton, false);
        }

        private VisualElementCache cache { get; set; }
        private SelectableLabel versionAndReleaseDateLabel { get { return cache.Get<SelectableLabel>("versionAndReleaseDate"); } }
        private VisualElement releaseNotesContainer { get { return cache.Get<VisualElement>("releaseNotesContainer"); } }
        private SelectableLabel releaseNotesLabel { get { return cache.Get<SelectableLabel>("releaseNotes"); } }
        private Button moreButton { get { return cache.Get<Button>("releaseDetailMore"); } }
        private Button lessButton { get { return cache.Get<Button>("releaseDetailLess"); } }
    }
}
