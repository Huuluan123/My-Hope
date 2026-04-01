using System.Collections;
using System.Threading;
using Unity.XGamingRuntime;
using UnityEngine;

namespace GdkSample_SignIn
{
    public class GDKGameRuntime : MonoBehaviour
    {
        public static string GameConfigSandbox { get; private set; } = "XDKS.1";

        // Documented as: "Specifies the SCID to be used for Save Game Storage."
        public static string GameConfigScid { get; private set; } = "00000000-0000-0000-0000-0000FFFFFFFF";

        // Documented as: "...a default value of 'FFFFFFFF' for this element. This allows for early iteration of your
        //   title without having to immediately acquire the Id from Partner Center. It is strongly recommended to change
        //   this Id as soon as you get your title building to avoid failures when attempting to do API calls."
        public static string GameConfigTitleId { get; private set; } = "FFFFFFFF";

        public static bool Initialized { get; private set; }
        public static GDKGameRuntime Instance { get; private set; }

        private CancellationTokenSource m_CancellationTokenSource;

        public static bool TryInitialize()
        {
            Debug.Log($"GDK Edition: {SDK.GetGdkEdition()}");

            if (Instance == null)
            {
                Debug.Log("GDK XGameRuntime Library not initialized.");
                // Instantiate the GDKGameRuntime object that will call Awake.
                new GameObject("GDKGamingRuntimeManager").AddComponent<GDKGameRuntime>();
                if (!Initialized)
                {
                    return false;
                }
            }

            Debug.Log($"GDK XGameRuntime Library initialized: {Initialized}");
            Debug.Log($"GDK Xbox Live API SCID: {GameConfigScid}");
            Debug.Log($"GDK TitleId: {GameConfigTitleId}");
            Debug.Log($"GDK Sandbox: {GameConfigSandbox}");
            return Initialized;
        }

        private void Awake()
        {
            InitializeOrDestroyInstance(this);
        }

        private void Start()
        {
            // SDK.XTaskQueueDispatch(0) needs to be call to pump GDK async completion events.
            // In this sample we are just using a coroutine to do that, which will run on the
            // main thread. The way it is written, this will only execute the completion
            // handlers for any async calls to process the results, so it is important that in
            // those completion handlers we don't do anything that would stall the main thread.
            // The actual async work that the GDK APIs do to execute the calls will be run
            // on an internal OS thread, unrelated to the Unity threads.
            // You can also run XTaskQueueDispatch on another thread, but keep in mind that
            // most Unity APIs are not thread safe, so you have to be careful what you call
            // in your async completion handlers, avoiding accessing Unity functions.
            // You could also use async/await C# patterns, but be aware Tasks are reference
            // types which incur allocations of objects. Depending on how the code is written,
            // running a tight loop that calls SDK.XTaskQueueDispatch(0) to pump GDK async
            // completion events when using Tasks might become costly because of those allocations.
            m_CancellationTokenSource = new CancellationTokenSource();
            Instance.StartCoroutine(DispatchTaskQueue(m_CancellationTokenSource.Token));
        }

        /// <summary>
        /// OnApplicationQuit/OnDestroy needs to handle cleanup especially if we have
        /// initialized the services from within the editor player
        /// </summary>
        private void OnApplicationQuit()
        {
            m_CancellationTokenSource.Cancel();
            m_CancellationTokenSource.Dispose();

            Debug.Log("Uninitializing Xbox Live API.");
            SDK.XBL.XblCleanup(null);
            Debug.Log("Closing default XTaskQueue.");
            SDK.CloseDefaultXTaskQueue();
            Debug.Log("Uninitializing XGame Runtime Library.");
            SDK.XGameRuntimeUninitialize();

            Initialized = false;
            Instance = null;
        }

        private static bool InitializeOrDestroyInstance(GDKGameRuntime newInstance)
        {
            if (Instance != newInstance && Instance != null)
            {
                Debug.LogWarning($"An instance of {newInstance.GetType().Name} already exist. Destroying {newInstance}...");
                Destroy(newInstance.gameObject);
                return false;
            }

            Instance = newInstance;
            GameConfigTitleId = GdkPlatformSettings.gameConfigTitleId;
            GameConfigScid = GdkPlatformSettings.gameConfigScid;
            DontDestroyOnLoad(Instance.gameObject);
            return InitializeRuntime();
        }

        private static bool InitializeRuntime(bool forceInitialization = false)
        {
            if (HR.FAILED(InitializeGamingRuntime(forceInitialization)) ||
                !InitializeXboxLive(GameConfigScid))
            {
                Initialized = false;
                return false;
            }

            // Not necessary but handy to know when debugging
            int hResult = SDK.XGameGetXboxTitleId(out var titleId);
            if (HR.FAILED(hResult))
            {
                Debug.Log($"FAILED: Could not get TitleID! hResult: 0x{hResult:x} ({HR.NameOf(hResult)})");
            }

            if (titleId.ToString("X").ToLower().Equals(GameConfigTitleId.ToLower()) == false)
            {
                Debug.LogWarning($"WARNING! Expected Title Id: {GameConfigTitleId} got: {titleId:X}");
            }

            GameConfigTitleId = titleId.ToString("X");

            hResult = SDK.XSystemGetXboxLiveSandboxId(out var sandboxId);
            if (HR.FAILED(hResult))
            {
                Debug.Log($"FAILED: Could not get SandboxID! HResult: 0x{hResult:x} ({HR.NameOf(hResult)})");
            }

            if (sandboxId.Equals(GameConfigSandbox) == false)
            {
                Debug.LogWarning($"WARNING! Expected sandbox Id: {GameConfigSandbox} got: {sandboxId}");
            }

            GameConfigSandbox = sandboxId;

            Debug.Log($"GDK Initialized, titleId: {GameConfigTitleId}, sandboxId: {GameConfigSandbox}");

            // Done!
            Initialized = true;
            return true;
        }

        /// <summary>
        /// Initializes the main Runtime Library.
        /// At the same time we will Creates the Async Dispatch thread which will handle all calls to work.
        /// </summary>
        private static int InitializeGamingRuntime(bool forceInitialization = false)
        {
            // We do not want stack traces for all log statements. (Exceptions logged
            // with Debug.LogException will still have stack traces though.):
            //Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            Debug.Log("Initializing XGame Runtime Library.");

            if (Initialized && forceInitialization == false)
            {
                Debug.Log("Gaming Runtime already initialized.");
                return 0;
            }

            int hResult = SDK.XGameRuntimeInitialize();
            if (HR.FAILED(hResult))
            {
                Debug.Log($"FAILED: Initialize XGameRuntime, HResult: 0x{hResult:X} ({HR.NameOf(hResult)})");
                return hResult;
            }

            hResult = SDK.CreateDefaultTaskQueue();
            if (HR.FAILED(hResult))
            {
                Debug.LogError($"FAILED: XTaskQueueCreate, HResult: 0x{hResult:X}");
                return hResult;
            }

            return 0;
        }

        /// <summary>
        /// Initializes the Xbox Live Basic API this is required for all Xbox Live API calls.
        /// </summary>
        /// <returns>The HResult value of initializing Xbox Live</returns>
        private static bool InitializeXboxLive(string scid)
        {
            Debug.Log($"Initializing Xbox Live API (SCID: {scid}).");

            int hResult = SDK.XBL.XblInitialize(scid);
            if (HR.FAILED(hResult) && hResult != HR.E_XBL_ALREADY_INITIALIZED)
            {
                Debug.Log($"FAILED: Initialize Xbox Live, HResult: 0x{hResult:X}, {HR.NameOf(hResult)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Async Dispatch Task Queue using Task.
        /// This executes all GDK Asynchronous block work natively.
        /// </summary>
        private static IEnumerator DispatchTaskQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                SDK.XTaskQueueDispatch(0);
                yield return null;
            }
        }
    }
}