// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class BasePage : IPage
    {
        public event Action<IPackageVersion> onSelectionChanged = delegate {};
        public event Action<IEnumerable<VisualState>> onVisualStateChange = delegate {};
        public event Action<ListUpdateArgs> onListUpdate = delegate {};
        public event Action<IPage> onListRebuild = delegate {};
        public event Action<IPage> onSubPageAdded = delegate {};
        public event Action<PageFilters> onFiltersChange = delegate {};

        // keep track of a list of selected items by remembering the uniqueIds
        [SerializeField]
        protected List<string> m_SelectedUniqueIds = new List<string>();

        [SerializeField]
        private List<string> m_CollapsedGroups = new List<string>();

        [SerializeField]
        protected PackageFilterTab m_Tab;
        public PackageFilterTab tab => m_Tab;

        [SerializeField]
        protected PageFilters m_Filters;
        public PageFilters filters => m_Filters;

        [SerializeField]
        protected PageCapability m_Capability;
        public PageCapability capability => m_Capability;

        public bool isFullyLoaded => numTotalItems <= numCurrentItems;

        public abstract long numTotalItems { get; }

        public abstract long numCurrentItems { get; }

        public abstract IEnumerable<VisualState> visualStates { get; }
        public abstract IEnumerable<SubPage> subPages { get; }
        public abstract SubPage currentSubPage { get; set; }

        public abstract string contentType { get; set; }

        [NonSerialized]
        protected PackageDatabase m_PackageDatabase;
        protected void ResolveDependencies(PackageDatabase packageDatabase)
        {
            m_PackageDatabase = packageDatabase;
        }

        protected BasePage(PackageDatabase packageDatabase, PackageFilterTab tab, PageCapability capability)
        {
            ResolveDependencies(packageDatabase);

            m_Tab = tab;
            m_Capability = capability;
            if (m_Filters == null)
            {
                var defaultOrdering = m_Capability?.orderingValues?.FirstOrDefault();
                m_Filters = new PageFilters
                {
                    orderBy = defaultOrdering?.orderBy,
                    isReverseOrder = false
                };
            }
        }

        public bool ClearFilters()
        {
            var filters = m_Filters?.Clone() ?? new PageFilters();
            filters.status = string.Empty;
            filters.categories = new List<string>();
            filters.labels = new List<string>();

            return UpdateFilters(filters);
        }

        public virtual bool UpdateFilters(PageFilters filters)
        {
            if ((m_Filters == null && filters == null) || (m_Filters?.Equals(filters) ?? false))
                return false;

            m_Filters = filters?.Clone();
            onFiltersChange?.Invoke(m_Filters);
            return true;
        }

        public abstract void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdate, IEnumerable<IPackage> postUpdate);

        public abstract void Rebuild();

        protected void TriggerOnListUpdate(IEnumerable<IPackage> added, IEnumerable<IPackage> updated = null, IEnumerable<IPackage> removed = null)
        {
            var args = new ListUpdateArgs
            {
                page = this,
                added = added ?? Enumerable.Empty<IPackage>(),
                updated = updated ?? Enumerable.Empty<IPackage>(),
                removed = removed ?? Enumerable.Empty<IPackage>()
            };
            args.reorder = capability.supportLocalReordering && (args.added.Any() || args.updated.Any());
            onListUpdate?.Invoke(args);
        }

        protected void TriggerOnListRebuild()
        {
            onListRebuild?.Invoke(this);
        }

        public virtual void SetPackagesUserUnlockedState(IEnumerable<string> packageUniqueIds, bool unlocked)
        {
            // do nothing, only simple page needs implementation right now
        }

        public virtual void ResetUserUnlockedState()
        {
            // do nothing, only simple page needs implementation right now
        }

        public virtual bool GetDefaultLockState(IPackage package)
        {
            return false;
        }

        protected void TriggerOnVisualStateChange(IEnumerable<VisualState> visualStates)
        {
            onVisualStateChange?.Invoke(visualStates);
        }

        public virtual void TriggerOnSelectionChanged()
        {
            TriggerOnSelectionChanged(GetSelectedVersion());
        }

        public void TriggerOnSelectionChanged(IPackageVersion version)
        {
            onSelectionChanged?.Invoke(version);
        }

        public void TriggerOnSubPageAdded()
        {
            onSubPageAdded?.Invoke(this);
        }

        public abstract VisualState GetVisualState(string packageUniqueId);

        public VisualState GetSelectedVisualState()
        {
            var selectedUniqueId = m_SelectedUniqueIds.FirstOrDefault();
            return string.IsNullOrEmpty(selectedUniqueId) ? null : GetVisualState(selectedUniqueId);
        }

        public IPackageVersion GetSelectedVersion()
        {
            IPackage package;
            IPackageVersion version;
            GetSelectedPackageAndVersion(out package, out version);
            return version;
        }

        public void GetSelectedPackageAndVersion(out IPackage package, out IPackageVersion version)
        {
            var selected = GetVisualState(m_SelectedUniqueIds.FirstOrDefault());
            m_PackageDatabase.GetPackageAndVersion(selected?.packageUniqueId, selected?.selectedVersionId, out package, out version);
        }

        public void SetSelected(IPackage package, IPackageVersion version = null)
        {
            SetSelected(package?.uniqueId, version?.uniqueId ?? package?.versions.primary?.uniqueId);
        }

        public virtual void SetSelected(string packageUniqueId, string versionUniqueId)
        {
            var oldPackageUniqueId = m_SelectedUniqueIds.FirstOrDefault();
            var oldSelection = GetVisualState(oldPackageUniqueId);
            if (oldPackageUniqueId == packageUniqueId && oldSelection?.selectedVersionId == versionUniqueId)
                return;

            foreach (var uniqueId in m_SelectedUniqueIds)
            {
                var state = GetVisualState(uniqueId);
                if (state != null)
                    state.selectedVersionId = string.Empty;
            }
            m_SelectedUniqueIds.Clear();

            if (!string.IsNullOrEmpty(packageUniqueId) && !string.IsNullOrEmpty(versionUniqueId))
            {
                var selectedState = GetVisualState(packageUniqueId);
                if (selectedState != null)
                {
                    selectedState.selectedVersionId = versionUniqueId;
                    m_SelectedUniqueIds.Add(packageUniqueId);
                }
            }
            TriggerOnSelectionChanged();
            TriggerOnVisualStateChange(new[] { GetVisualState(oldPackageUniqueId), GetVisualState(packageUniqueId) }.Where(s => s != null));
        }

        public void SetExpanded(IPackage package, bool value)
        {
            SetExpanded(package?.uniqueId, value);
        }

        public abstract void SetExpanded(string packageUniqueId, bool value);

        public void SetSeeAllVersions(IPackage package, bool value)
        {
            SetSeeAllVersions(package?.uniqueId, value);
        }

        public abstract void SetSeeAllVersions(string packageUniqueId, bool value);

        public bool IsGroupExpanded(string groupName)
        {
            return !m_CollapsedGroups.Contains(groupName);
        }

        public void SetGroupExpanded(string groupName, bool value)
        {
            var groupExpanded = !m_CollapsedGroups.Contains(groupName);
            if (groupExpanded == value)
                return;
            if (value)
                m_CollapsedGroups.Remove(groupName);
            else
                m_CollapsedGroups.Add(groupName);
        }

        public bool Contains(IPackage package)
        {
            return Contains(package?.uniqueId);
        }

        public virtual string GetGroupName(IPackage package)
        {
            return GetDefaultGroupName(tab, package);
        }

        public static string GetDefaultGroupName(PackageFilterTab tab, IPackage package)
        {
            if (package.Is(PackageType.BuiltIn) || package.Is(PackageType.AssetStore))
                return string.Empty;

            if (package.Is(PackageType.Unity))
                return tab == PackageFilterTab.UnityRegistry ? string.Empty : PageManager.k_UnityPackageGroupName;

            return string.IsNullOrEmpty(package.versions.primary?.author) ?
                PageManager.k_OtherPackageGroupName :
                package.versions.primary.author;
        }

        public abstract bool Contains(string packageUniqueId);

        public abstract void LoadMore(long numberOfPackages);

        public abstract void Load(IPackage package, IPackageVersion version = null);

        public abstract void AddSubPage(SubPage subPage);
    }
}
