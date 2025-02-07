// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UIError : Error
    {
        private static readonly string k_EntitlementErrorMessage = L10n.Tr("This package is not available to use because there is no license registered for your user. Please sign in with a licensed account. If the problem persists, please contact your administrator.");
        public static readonly UIError k_EntitlementError = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage);
        public static readonly UIError k_EntitlementWarning = new UIError(UIErrorCode.Forbidden, k_EntitlementErrorMessage, Attribute.IsWarning);

        [SerializeField]
        private UIErrorCode m_UIErrorCode;

        [SerializeField]
        private Attribute m_Attribute;

        [SerializeField]
        private int m_OperationErrorCode;

        [Flags]
        internal enum Attribute
        {
            None                = 0,
            IsDetailInConsole   = 1 << 0,
            IsWarning           = 1 << 1,
            IsClearable         = 1 << 2
        }

        public new UIErrorCode errorCode => m_UIErrorCode;

        public int operationErrorCode => m_OperationErrorCode;

        public Attribute attribute => m_Attribute;

        public bool HasAttribute(Attribute attribute)
        {
            return (m_Attribute & attribute) != 0;
        }

        public UIError(UIErrorCode errorCode, string message, int operationErrorCode) : this(errorCode, message, Attribute.None, operationErrorCode) {}

        public UIError(UIErrorCode errorCode, string message, Attribute attribute = Attribute.None, int operationErrorCode = -1) : base(NativeErrorCode.Unknown, message)
        {
            m_UIErrorCode = errorCode;
            m_Attribute = attribute;
            m_OperationErrorCode = operationErrorCode;
        }
    }
}
