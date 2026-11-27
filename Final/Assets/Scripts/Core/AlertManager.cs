using System;
using UnityEngine;

namespace MGS2Tanker
{
    public enum AlertPhase { None, Alert, Evasion }

    /// <summary>
    /// Fases como en MGS2:
    ///  - ALERT: los soldados de la sala del jugador lo persiguen. El contador se reinicia cada vez que alguien lo ve.
    ///  - EVASION: cuando el contador de ALERT llega a 0, buscan en la última posición conocida.
    ///  - Normal: cuando termina EVASION vuelven a patrullar.
    /// La alerta se dispara si: lo ve una Surveillance Camera, lo ve un Soldier, o un Soldier herido sobrevive 3 s.
    /// </summary>
    public class AlertManager : MonoBehaviour
    {
        public static AlertManager Instance { get; private set; }

        [Tooltip("Segundos de ALERT sin que nadie vea al jugador antes de pasar a EVASION.")]
        public float alertDuration = 20f;
        [Tooltip("Segundos de EVASION antes de volver a la normalidad.")]
        public float evasionDuration = 25f;
        [Tooltip("Si el jugador no está dentro de ninguna sala, se alerta a los soldados dentro de este radio.")]
        public float fallbackRadius = 20f;

        public AlertPhase Phase { get; private set; } = AlertPhase.None;
        public float Timer { get; private set; }
        public Vector3 LastKnownPlayerPos { get; private set; }
        public Room AlertRoom { get; private set; }

        public event Action<AlertPhase> OnPhaseChanged;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Dispara (o refuerza) la alerta en la sala donde está el jugador.</summary>
        public void RaiseAlert(Vector3 playerPos, string source = null)
        {
            LastKnownPlayerPos = playerPos;
            Room room = Room.Find(playerPos);
            bool isNew = Phase != AlertPhase.Alert;

            Timer = alertDuration;
            if (isNew)
            {
                AlertRoom = room;
                SetPhase(AlertPhase.Alert);
                Debug.Log($"[ALERTA] ¡Intruso detectado en {(room ? room.displayName : "zona sin sala")}!" +
                          (string.IsNullOrEmpty(source) ? "" : $" (por {source})"));
            }

            foreach (var s in SoldierController.All)
            {
                if (!s.IsAlive || s.IsAlerted) continue;
                bool sameRoom = room != null
                    ? room.Contains(s.transform.position)
                    : Vector3.Distance(s.transform.position, playerPos) <= fallbackRadius;
                if (sameRoom) s.EnterAlert(playerPos);
            }
        }

        /// <summary>Un soldado en alerta vio al jugador: reinicia el contador.</summary>
        public void ReportPlayerSeen(Vector3 playerPos)
        {
            if (Phase == AlertPhase.Alert)
            {
                Timer = alertDuration;
                LastKnownPlayerPos = playerPos;
            }
            else RaiseAlert(playerPos);
        }

        void Update()
        {
            if (Phase == AlertPhase.None) return;
            Timer -= Time.deltaTime;
            if (Timer > 0f) return;

            if (Phase == AlertPhase.Alert) BeginEvasion();
            else EndAlert();
        }

        void BeginEvasion()
        {
            Timer = evasionDuration;
            SetPhase(AlertPhase.Evasion);
            Debug.Log("[ALERTA] -> EVASION: los soldados buscan en la última posición conocida.");
            foreach (var s in SoldierController.All)
                if (s.IsAlive && s.IsAlerted) s.EnterEvasion(LastKnownPlayerPos);
        }

        void EndAlert()
        {
            Timer = 0f;
            SetPhase(AlertPhase.None);
            AlertRoom = null;
            Debug.Log("[ALERTA] Fin de la evasión: los soldados vuelven a patrullar.");
            foreach (var s in SoldierController.All)
                if (s.IsAlive) s.OnEvasionEnded();
        }

        /// <summary>Se usa al reaparecer: todo vuelve a la normalidad.</summary>
        public void ResetAll()
        {
            Timer = 0f;
            AlertRoom = null;
            SetPhase(AlertPhase.None);
            foreach (var s in SoldierController.All)
                if (s.IsAlive) s.ReturnToPatrol();
        }

        void SetPhase(AlertPhase p)
        {
            if (Phase == p) return;
            Phase = p;
            OnPhaseChanged?.Invoke(p);
        }
    }
}
