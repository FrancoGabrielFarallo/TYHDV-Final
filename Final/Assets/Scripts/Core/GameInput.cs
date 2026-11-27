using UnityEngine;
using UnityEngine.InputSystem;

namespace MGS2Tanker
{
    /// <summary>
    /// Entradas del juego definidas por código con el Input System (no hace falta ningún .inputactions).
    /// Soporta teclado + mouse, gamepad (Xbox / PlayStation / Switch Pro) y joysticks genéricos HID.
    ///
    ///  Acción          Teclado / Mouse        Gamepad
    ///  Mover           WASD / Flechas         Stick izq. / D-Pad
    ///  Caminar lento   Shift (mantener)       B / Círculo (mantener) o inclinar poco el stick
    ///  Apuntar         Click derecho          LT / L2 (o LB / L1)
    ///  Rotar apuntando Mouse                  Stick der. (o izq.)
    ///  Disparar        Click izquierdo        RT / R2
    ///  Recargar        R                      X / Cuadrado
    ///  Pausa           ESC                    Start / Options
    ///  Reaparecer      F1 (al morir)          A / Cruz (al morir)
    ///  Reiniciar       F2                     Select / Share
    /// </summary>
    public static class GameInput
    {
        public static InputAction Move { get; private set; }
        public static InputAction Look { get; private set; }
        public static InputAction Aim { get; private set; }
        public static InputAction Fire { get; private set; }
        public static InputAction Reload { get; private set; }
        public static InputAction Walk { get; private set; }
        public static InputAction Pause { get; private set; }
        public static InputAction Respawn { get; private set; }
        public static InputAction Restart { get; private set; }

        /// <summary>True si el último dispositivo usado fue un gamepad / joystick.</summary>
        public static bool UsingGamepad { get; private set; }

        static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            DisposeAll();
            UsingGamepad = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            Move = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");
            Move.AddBinding("<Gamepad>/dpad");
            Move.AddBinding("<Joystick>/stick");
            Move.Enable();

            Look = new InputAction("Look", InputActionType.Value, "<Gamepad>/rightStick", expectedControlType: "Vector2");
            Look.Enable();

            Aim = Button("Aim", "<Mouse>/rightButton", "<Gamepad>/leftTrigger", "<Gamepad>/leftShoulder");
            Fire = Button("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger", "<Joystick>/trigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonWest");
            Walk = Button("Walk", "<Keyboard>/leftShift", "<Keyboard>/rightShift", "<Gamepad>/buttonEast");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Respawn = Button("Respawn", "<Keyboard>/f1", "<Gamepad>/buttonSouth");
            Restart = Button("Restart", "<Keyboard>/f2", "<Gamepad>/select");
        }

        static InputAction Button(string name, params string[] paths)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) action.AddBinding(p);
            action.Enable();
            return action;
        }

        static void DisposeAll()
        {
            foreach (var a in new[] { Move, Look, Aim, Fire, Reload, Walk, Pause, Respawn, Restart })
            {
                if (a == null) continue;
                a.Disable();
                a.Dispose();
            }
            Move = Look = Aim = Fire = Reload = Walk = Pause = Respawn = Restart = null;
            initialized = false;
        }

        /// <summary>Detecta qué dispositivo se usó último (para saber si apuntar con mouse o con stick).</summary>
        public static void Tick()
        {
            var gp = Gamepad.current;
            if (gp != null)
            {
                if (gp.leftStick.ReadValue().sqrMagnitude > 0.04f || gp.rightStick.ReadValue().sqrMagnitude > 0.04f ||
                    gp.leftTrigger.ReadValue() > 0.3f || gp.rightTrigger.ReadValue() > 0.3f ||
                    gp.buttonSouth.isPressed || gp.buttonEast.isPressed || gp.buttonWest.isPressed ||
                    gp.dpad.ReadValue().sqrMagnitude > 0.04f)
                    UsingGamepad = true;
            }

            var js = Joystick.current;
            if (js != null && js.stick.ReadValue().sqrMagnitude > 0.04f) UsingGamepad = true;

            var mouse = Mouse.current;
            if (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.isPressed || mouse.rightButton.isPressed))
                UsingGamepad = false;

            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.isPressed) UsingGamepad = false;
        }
    }
}
