using System;
using Game.Core;
using Game.Runtime.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Game.Runtime.Input
{
    // Turns a pointer press (finger or mouse) into a board cell. No physics: the camera ray hits the y = 0 plane (§9.5).
    // Runs AFTER the EventSystem, so IsPointerOverGameObject knows about this frame's touch.
    [DefaultExecutionOrder(100)]
    public sealed class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera cam;

        private readonly Plane ground = new Plane(Vector3.up, Vector3.zero);
        private BoardLayout layout;

        public event Action<GridPos> CellTapped;

        public void SetLayout(BoardLayout boardLayout) => layout = boardLayout;

        // Reading input in Update is a deliberate exception to "no polling in Update" (§16.3).
        private void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = cam.ScreenPointToRay(pointer.position.ReadValue());
            if (!ground.Raycast(ray, out float distance)) return;

            CellTapped?.Invoke(layout.WorldToCell(ray.GetPoint(distance)));
        }
    }
}
