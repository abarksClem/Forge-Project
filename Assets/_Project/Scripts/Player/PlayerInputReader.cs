using UnityEngine;
using UnityEngine.InputSystem;

namespace Forge.Player {
    // All player controls in one place
    
    public class PlayerInputReader : MonoBehaviour {
        public InputAction Move { get; private set; }
        public InputAction Jump { get; private set; }
        public InputAction Attack { get; private set; }
        public InputAction Parry { get; private set; }
        public InputAction Dodge { get; private set; }
        public InputAction Heal { get; private set; }
        public InputAction Interact { get; private set; }

        private InputAction[] all;
        private void Awake() {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            Move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick/x");

            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Attack = Button("Attack", "<Keyboard>/j", "<Mouse>/leftButton", "<Gamepad>/buttonWest");
            Parry = Button("Parry", "<Keyboard>/k", "<Mouse>/rightButton", "<Gamepad>/rightShoulder");
            Dodge = Button("Dodge", "<Keyboard>/l", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            Heal = Button("Heal", "<Keyboard>/f", "<Gamepad>/buttonNorth");
            Interact = Button("Interact", "<Keyboard>/e", "<Keyboard>/w", "<Keyboard>/upArrow", "<Gamepad>/dpad/up");

            all = new[] { Move, Jump, Attack, Parry, Dodge, Heal, Interact };
        }

        private static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (string binding in bindings) action.AddBinding(binding);
            return action;
        }

        private void OnEnable()  { foreach (var a in all) a.Enable(); }
        private void OnDisable() { foreach (var a in all) a.Disable(); }
        private void OnDestroy() { foreach (var a in all) a.Dispose(); }
    }
}