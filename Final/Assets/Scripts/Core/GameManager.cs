using UnityEngine;
using UnityEngine.SceneManagement;

namespace MGS2Tanker
{
    /// <summary>
    /// F1 (o A/Cruz) con el jugador muerto: reaparece con sus valores iniciales (vida llena, 2 cargadores de 15).
    /// F2 (o Select): reinicia la escena.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerHealth player;
        public Transform playerSpawn;

        Vector3 spawnPos;
        Quaternion spawnRot;

        void Awake()
        {
            Instance = this;
            if (!player) player = FindFirstObjectByType<PlayerHealth>();
            if (playerSpawn) { spawnPos = playerSpawn.position; spawnRot = playerSpawn.rotation; }
            else if (player) { spawnPos = player.transform.position; spawnRot = player.transform.rotation; }
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            if (GameInput.Restart != null && GameInput.Restart.WasPressedThisFrame())
            {
                RestartScene();
                return;
            }

            if (PauseManager.IsPaused) return;

            if (player && !player.IsAlive && GameInput.Respawn != null && GameInput.Respawn.WasPressedThisFrame())
                Respawn();
        }

        public void Respawn()
        {
            if (!player) return;
            player.ResetState();

            var weapon = player.GetComponent<PlayerWeapon>();
            if (weapon) weapon.ResetAmmo();

            var controller = player.GetComponent<PlayerController>();
            if (controller) controller.TeleportTo(spawnPos, spawnRot);
            else player.transform.SetPositionAndRotation(spawnPos, spawnRot);

            var trail = player.GetComponent<PlayerTrail>();
            if (trail) trail.Clear();

            if (AlertManager.Instance) AlertManager.Instance.ResetAll();

            var cam = FindFirstObjectByType<MGSCameraController>();
            if (cam) cam.Snap();

            Debug.Log("[Juego] Reaparición: vida y cargadores restaurados a sus valores iniciales.");
        }

        public void RestartScene()
        {
            PauseManager.SetPaused(false);
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            Debug.Log("[Juego] Reiniciando escena...");
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(scene.buildIndex);
#endif
        }
    }
}
