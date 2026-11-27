using System;
using UnityEngine;

namespace MGS2Tanker
{
    /// <summary>Pausa con ESC (o Start). Detiene el tiempo del proyecto; ESC de nuevo lo reanuda.</summary>
    public class PauseManager : MonoBehaviour
    {
        public static bool IsPaused { get; private set; }
        public static event Action<bool> OnPauseChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsPaused = false;
            OnPauseChanged = null;
        }

        void Update()
        {
            if (GameInput.Pause != null && GameInput.Pause.WasPressedThisFrame())
                SetPaused(!IsPaused);
        }

        public static void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            Debug.Log(paused ? "[Juego] PAUSA" : "[Juego] Reanudado");
            OnPauseChanged?.Invoke(paused);
        }

        void OnDestroy()
        {
            // Nunca dejar el proyecto con timeScale = 0 al cambiar/recargar escena.
            if (IsPaused) SetPaused(false);
        }
    }
}
