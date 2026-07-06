using UnityEngine;

namespace Game.Runtime
{
    public class PlayerLook : MonoBehaviour
    {
        public Transform cameraPivot;
        public float sensitivity = 0.12f;

        float pitch;

        public void AddLook(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85f, 85f);
            if (cameraPivot != null)
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}
