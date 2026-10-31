using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Runtime.Input
{
    // The Android back button arrives as the Escape key in the Input System (Esc in the editor).
    public sealed class BackButtonInput : MonoBehaviour
    {
        public event Action BackPressed;

        // Reading input in Update is a deliberate exception to "no polling in Update".
        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                BackPressed?.Invoke();
        }
    }
}
