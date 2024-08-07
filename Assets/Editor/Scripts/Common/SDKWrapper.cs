using Editor.Resources.Screens.Export;
using Editor.Scripts.Manager;
using UnityEngine;

namespace Editor.Scripts.Common
{
    public static class SDKWrapper
    {
        public static void ExportItem(bool toVrm0 = false)
        {
            ExecuteWithAuthCheck(() => ExportVrm.ExportItem(toVrm0));
        }

        private static void ExecuteWithAuthCheck(System.Action sdkFunction)
        {
            if (AuthManager.IsAuthenticated)
            {
                sdkFunction.Invoke();
            }
            else
            {
                Debug.LogError("User is not logged in. Cannot execute function.");
            }
        }
    }
}