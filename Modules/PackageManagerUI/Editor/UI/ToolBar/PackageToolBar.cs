// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageToolbar : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageToolbar> {}

        private ApplicationProxy m_Application;
        private AssetStoreDownloadManager m_AssetStoreDownloadManager;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private PageManager m_PageManager;
        private UnityConnectProxy m_UnityConnectProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_AssetStoreDownloadManager = container.Resolve<AssetStoreDownloadManager>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
            m_PageManager = container.Resolve<PageManager>();
            m_UnityConnectProxy = container.Resolve<UnityConnectProxy>();
        }

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private ButtonDisableCondition m_DisableIfCompiling;
        private ButtonDisableCondition m_DisableIfInstallOrUninstallInProgress;
        private ButtonDisableCondition m_DisableIfNoNetwork;
        private ButtonDisableCondition m_DisableIfEntitlementsError;
        private ButtonDisableCondition m_DisableIfPackageDisabled;

        private PackageAddButton m_AddButton;
        private PackageGitUpdateButton m_GitUpdateButton;
        private PackageRemoveButton m_RemoveButton;
        private PackageResetButton m_ResetButton;

        private PackagePauseDownloadButton m_PauseButton;
        private PackageResumeDownloadButton m_ResumeButton;
        private PackageCancelDownloadButton m_CancelButton;

        private PackageImportButton m_ImportButton;
        private PackageDownloadButton m_DownloadButton;

        private PackageUnlockButton m_UnlockButton;
        private PackageSignInButton m_SignInButton;

        private VisualElement m_MainContainer;
        private VisualElement m_ProgressContainer;
        private PackageToolBarError m_ErrorContainer;

        private ProgressBar m_DownloadProgress;

        private VisualElement m_BuiltInActions;
        public VisualElement extensions { get; private set; }

        public PackageToolbar()
        {
            ResolveDependencies();

            m_MainContainer = new VisualElement { name = "toolbarMainContainer" };
            Add(m_MainContainer);

            var leftItems = new VisualElement();
            leftItems.AddToClassList("leftItems");
            m_MainContainer.Add(leftItems);

            extensions = new VisualElement { name = "extensionItems" };
            leftItems.Add(extensions);

            m_BuiltInActions = new VisualElement { name = "builtInActions" };
            m_BuiltInActions.AddToClassList("rightItems");
            m_MainContainer.Add(m_BuiltInActions);

            m_ProgressContainer = new VisualElement { name = "toolbarProgressContainer" };
            Add(m_ProgressContainer);

            m_DownloadProgress = new ProgressBar { name = "downloadProgress" };
            m_ProgressContainer.Add(m_DownloadProgress);

            m_ErrorContainer = new PackageToolBarError(m_PackageDatabase) { name = "toolbarErrorContainer" };
            Add(m_ErrorContainer);

            InitializeButtons();
        }

        private void InitializeButtons()
        {
            m_DisableIfCompiling = new ButtonDisableCondition(() => m_Application.isCompiling,
                L10n.Tr("You need to wait until the compilation is finished to perform this action."));
            m_DisableIfInstallOrUninstallInProgress = new ButtonDisableCondition(() => m_PackageDatabase.isInstallOrUninstallInProgress,
                L10n.Tr("You need to wait until other install or uninstall operations are finished to perform this action."));
            m_DisableIfNoNetwork = new ButtonDisableCondition(() => !m_Application.isInternetReachable,
                L10n.Tr("You need to restore your network connection to perform this action."));
            m_DisableIfEntitlementsError = new ButtonDisableCondition(() => m_Package?.hasEntitlementsError ?? false,
                L10n.Tr("You need to sign in with a licensed account to perform this action."));
            m_DisableIfPackageDisabled = new ButtonDisableCondition(() => m_Version?.HasTag(PackageTag.Disabled) ?? false,
                L10n.Tr("This package is no longer available and can not be downloaded or imported anymore."));

            m_UnlockButton = new PackageUnlockButton(m_PageManager);
            m_UnlockButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_UnlockButton.element);

            m_AddButton = new PackageAddButton(m_Application, m_PackageDatabase, m_PageManager);
            m_AddButton.SetCommonDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling, m_DisableIfEntitlementsError);
            m_AddButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_AddButton.element);

            m_GitUpdateButton = new PackageGitUpdateButton(m_PackageDatabase);
            m_GitUpdateButton.SetCommonDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_GitUpdateButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_GitUpdateButton.element);

            m_RemoveButton = new PackageRemoveButton(m_Application, m_PackageManagerPrefs, m_PackageDatabase, m_PageManager);
            m_RemoveButton.SetCommonDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_RemoveButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_RemoveButton.element);

            m_ResetButton = new PackageResetButton(m_Application, m_PackageDatabase, m_PageManager);
            m_ResetButton.SetCommonDisableConditions(m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling);
            m_ResetButton.onAction += RefreshBuiltInButtons;
            m_ResetButton.element.SetIcon("customizedIcon");
            m_BuiltInActions.Add(m_ResetButton.element);

            m_ImportButton = new PackageImportButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_ImportButton.SetCommonDisableConditions(m_DisableIfPackageDisabled, m_DisableIfCompiling);
            m_ImportButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_ImportButton.element);

            m_DownloadButton = new PackageDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_DownloadButton.SetCommonDisableConditions(m_DisableIfPackageDisabled, m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_DownloadButton.onAction += Refresh;
            m_BuiltInActions.Add(m_DownloadButton.element);

            m_SignInButton = new PackageSignInButton(m_UnityConnectProxy);
            m_SignInButton.SetCommonDisableConditions(m_DisableIfNoNetwork);
            m_SignInButton.onAction += RefreshBuiltInButtons;
            m_BuiltInActions.Add(m_SignInButton.element);

            // Since pause, resume, cancel buttons are only used to control the download progress, we want to put them in the progress container instead
            m_CancelButton = new PackageCancelDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_CancelButton.SetCommonDisableConditions(m_DisableIfCompiling);
            m_CancelButton.onAction += Refresh;
            m_ProgressContainer.Add(m_CancelButton.element);

            m_PauseButton = new PackagePauseDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_PauseButton.SetCommonDisableConditions(m_DisableIfCompiling);
            m_PauseButton.onAction += RefreshProgressControlButtons;
            m_ProgressContainer.Add(m_PauseButton.element);

            m_ResumeButton = new PackageResumeDownloadButton(m_AssetStoreDownloadManager, m_PackageDatabase);
            m_ResumeButton.SetCommonDisableConditions(m_DisableIfNoNetwork, m_DisableIfCompiling);
            m_ResumeButton.onAction += RefreshProgressControlButtons;
            m_ProgressContainer.Add(m_ResumeButton.element);
        }

        public void OnEnable()
        {
            m_Application.onFinishCompiling += Refresh;
            m_PackageDatabase.onPackageProgressUpdate += OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadPaused += OnDownloadProgress;
        }

        public void OnDisable()
        {
            m_Application.onFinishCompiling -= Refresh;
            m_PackageDatabase.onPackageProgressUpdate -= OnPackageProgressUpdate;

            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadPaused -= OnDownloadProgress;
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Package = package;
            m_Version = version;

            Refresh();
        }

        private void Refresh()
        {
            // Since only one of `errorContainer`, `progressContainer` or `mainContainer` can be visible at the same time
            // we can use `chain` refresh mechanism in the order of priority (error > progress > main)
            if (RefreshErrorContainer())
                return;

            if (RefreshProgressContainer())
                return;

            RefreshMainContainer();
        }

        // Returns true if error is visible and there's no need to further check other containers
        private bool RefreshErrorContainer()
        {
            var errorVisible = m_ErrorContainer.Refresh(m_Package, m_Version);
            if (errorVisible)
            {
                UIUtils.SetElementDisplay(m_MainContainer, false);
                UIUtils.SetElementDisplay(m_ProgressContainer, false);
            }
            return errorVisible;
        }

        // Returns true if the progress bar is visible and there's no need to further check other containers
        private bool RefreshProgressContainer(IOperation operation = null)
        {
            operation ??= m_AssetStoreDownloadManager.GetDownloadOperation(m_Version?.packageUniqueId);
            var progressVisible = operation != null && m_Version?.packageUniqueId == operation.packageUniqueId && m_DownloadProgress.UpdateProgress(operation);
            UIUtils.SetElementDisplay(m_ProgressContainer, progressVisible);
            if (progressVisible)
            {
                UIUtils.SetElementDisplay(m_ErrorContainer, false);
                UIUtils.SetElementDisplay(m_MainContainer, false);
                RefreshProgressControlButtons();
            }
            return progressVisible;
        }

        private void RefreshMainContainer()
        {
            UIUtils.SetElementDisplay(m_ErrorContainer, false);
            UIUtils.SetElementDisplay(m_ProgressContainer, false);
            UIUtils.SetElementDisplay(m_MainContainer, true);

            RefreshBuiltInButtons();
            RefreshExtensionItems();
        }

        private void RefreshBuiltInButtons()
        {
            m_SignInButton.Refresh(m_Package, m_Version);

            m_UnlockButton.Refresh(m_Package, m_Version);
            m_GitUpdateButton.Refresh(m_Package, m_Version);
            m_AddButton.Refresh(m_Package, m_Version);
            m_RemoveButton.Refresh(m_Package, m_Version);
            m_ResetButton.Refresh(m_Package, m_Version);

            m_ImportButton.Refresh(m_Package, m_Version);
            m_DownloadButton.Refresh(m_Package, m_Version);
        }

        private void RefreshProgressControlButtons()
        {
            m_PauseButton.Refresh(m_Package, m_Version);
            m_ResumeButton.Refresh(m_Package, m_Version);
            m_CancelButton.Refresh(m_Package, m_Version);
        }

        private void RefreshExtensionItems()
        {
            var disableCondition = new[] { m_DisableIfInstallOrUninstallInProgress, m_DisableIfCompiling }.FirstOrDefault(c => c.value);
            foreach (var item in extensions.Children())
                item.SetEnabled(disableCondition == null);
            extensions.tooltip = disableCondition?.tooltip ?? string.Empty;
        }

        private void OnPackageProgressUpdate(IPackage package)
        {
            RefreshBuiltInButtons();
            RefreshExtensionItems();
        }

        private void OnDownloadProgress(IOperation operation)
        {
            if (m_Version?.packageUniqueId != operation.packageUniqueId)
                return;

            // We call `RefreshProgressContainer` here instead of calling `Refresh` here directly when the download is progressing to save some time
            // We only want to do a proper refresh in cases where `RefreshProgressContainer` would return false (progress bar no longer visible)
            if (UIUtils.IsElementVisible(m_ProgressContainer) && RefreshProgressContainer(operation))
                return;

            Refresh();
        }
    }
}
