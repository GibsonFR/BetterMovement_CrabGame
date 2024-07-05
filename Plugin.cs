global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using UnityEngine;
global using UnhollowerRuntimeLib;

namespace BetterMovement
{
    [BepInPlugin("B69D014D-528E-4E15-8B93-E6549B23FF4D", "BetterMovement", "2.1.0")]
    public class Plugin : BasePlugin
    {
        public override void Load()
        {
            ClassInjector.RegisterTypeInIl2Cpp<BetterMovement>();
            Harmony.CreateAndPatchAll(typeof(Plugin));
            Log.LogInfo("Mod created by Gibson, discord : gib_son");
        }

        //Force your client to send your position and rotation to server more frequently
        public class BetterMovement : MonoBehaviour
        {
            private ulong hostSteamId = 0;
            private GameObject client = null;
            private Rigidbody clientBody = null;
            private float elapsed = 0f;
            private readonly float elapsedInterval = 1f / 30f;
            void Update()
            {
                if (SteamManager.Instance.IsLobbyOwner()) return; //Return early if you're hosting the lobby
                if (hostSteamId == 0) hostSteamId = SteamManager.Instance.field_Private_CSteamID_1.m_SteamID; //Get host ClientId
                if (client == null) client = GameObject.Find("/Player"); //Get the GameObject of client
                else if (clientBody == null && client != null) clientBody = client.GetComponent<Rigidbody>(); //Get the RigidBody of client


                elapsed += Time.deltaTime; //Add how much time has elapsed since the last call of this method

                //Check if client and hostSteamId are instanced to avoid errors
                if (client == null || hostSteamId == 0) return;

                //Check if its time to send a new packet (30 packets/secondes) to avoid overloading the host
                if (elapsed >= elapsedInterval) 
                {
                    elapsed = 0f;

                    Vector3 clientPosition = clientBody.transform.position; //Your current position
                    Vector3 clientRotation = Camera.main.transform.rotation.eulerAngles; //Your current rotation


                    ClientSend.PlayerRotation(clientRotation.x, clientRotation.y, hostSteamId); //Send a packet with your current and accurate Rotation to host
                    ClientSend.PlayerPosition(clientPosition, hostSteamId); //Send a packet with your current and accurate Position to host
                }
            }
        }

        //Set ClientId
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdate(SteamManager __instance)
        {
            if (Variables.clientIdSafe == 0)
            {
                Variables.clientId = (ulong)__instance.field_Private_CSteamID_0;
                Variables.clientIdSafe = Variables.clientId;
            }
        }

        //Fixing conflict with native packet sent
        [HarmonyPatch(typeof(ClientSend), nameof(ClientSend.PlayerRotation))]
        [HarmonyPrefix]
        public static void OnClientSendPlayerRotation(ref float __0, ref float __1, ulong __2)
        {
            if (__0 > 180) __0 -= 360;
            if (__0 < -180) __0 += 360;
            if (__1 > 180) __1 -= 360;
            if (__1 < -180) __1 += 360;
        }

        [HarmonyPatch(typeof(GameUI), "Awake")]
        [HarmonyPostfix]
        public static void UIAwakePatch(GameUI __instance)
        {
            GameObject obj = new();
            _ = obj.AddComponent<BetterMovement>();

            obj.transform.SetParent(__instance.transform);
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}