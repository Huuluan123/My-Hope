using System;
using Unity.XGamingRuntime;
using UnityEngine;
using UnityEngine.UI;

namespace GdkSample_SignIn
{
    public class SignInSceneManager : MonoBehaviour
    {
        public Text textGamertag;
        public Text textSandboxId;
        public Text textTitleId;
        public Text textScid;

        private XUserHandle _userHandle;
        private XUserChangeRegistrationToken _registrationToken;

        // Start is called before the first frame update
        private void Start()
        {
            if (!GDKGameRuntime.TryInitialize())
            {
                return;
            }

            textSandboxId.text = GDKGameRuntime.GameConfigSandbox;
            textTitleId.text = string.Format($"0x{GDKGameRuntime.GameConfigTitleId}");
            textScid.text = GDKGameRuntime.GameConfigScid;

            InitializeAndAddUser();
            SDK.XUserRegisterForChangeEvent(UserChangeEventCallback, out _registrationToken);
        }

        private void OnDestroy()
        {
            if (_userHandle != null)
            {
                SDK.XUserCloseHandle(_userHandle);
                _userHandle = null;
            }

            SDK.XUserUnregisterForChangeEvent(_registrationToken);
        }

        private void InitializeAndAddUser()
        {
            SDK.XUserAddAsync(XUserAddOptions.AddDefaultUserAllowingUI, AddUserComplete);
        }

        private void AddUserComplete(int hResult, XUserHandle userHandle)
        {
            if (HR.FAILED(hResult))
            {
                Debug.LogWarning($"FAILED: Could not signin a user, hResult={hResult:X} ({HR.NameOf(hResult)})");
                return;
            }

            _userHandle = userHandle;

            CompletePostSignInInitialization();
        }

        private void UserChangeEventCallback(IntPtr _, XUserLocalId userLocalId, XUserChangeEvent eventType)
        {
            if (eventType == XUserChangeEvent.SignedOut)
            {
                Debug.LogWarning("User logging out");
                textGamertag.text = "User logged out";

                if (_userHandle != null)
                {
                    SDK.XUserCloseHandle(_userHandle);
                    _userHandle = null;
                }

                InitializeAndAddUser();
            }
        }

        private void CompletePostSignInInitialization()
        {
            string gamertag = string.Empty;

            if (textGamertag == null)
            {
                Debug.LogError($"textGamertag is null, set Game Object.");
                return;
            }

            int hResult = SDK.XUserGetGamertag(_userHandle, XUserGamertagComponent.UniqueModern, out gamertag);
            if (HR.FAILED(hResult))
            {
                Debug.LogWarning($"FAILED: Could not get user tag, hResult=0x{hResult:X} ({HR.NameOf(hResult)})");
                return;
            }

            Debug.Log($"SUCCESS: XUserGetGamertag() returned: '{gamertag}'");
            textGamertag.text = gamertag;
        }
    }
} // namespace GdkSample_SignIn
