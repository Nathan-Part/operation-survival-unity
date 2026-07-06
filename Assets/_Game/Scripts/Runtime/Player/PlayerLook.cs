using UnityEngine;

namespace Game.Runtime
{
    public class PlayerLook : MonoBehaviour
    {
        public Transform cameraPivot;
        public float sensitivity = 0.12f;

        float pitch;
        float kickPerSecond; // camera kick (web: cameraKick — remontée du pitch pendant KICK_DUR)
        float kickRemaining;

        /// Pitch en degrés, positif = regarder vers le bas (convention du pivot).
        public float Pitch => pitch;

        public void AddLook(Vector2 delta)
        {
            transform.Rotate(0f, delta.x * sensitivity, 0f);
            pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -85f, 85f);
            ApplyPitch();
        }

        /// Recul caméra : remonte le pitch de `pitchDegrees` étalé sur `seconds` (web: KICK_PITCH/KICK_DUR).
        public void Kick(float pitchDegrees, float seconds)
        {
            kickPerSecond = pitchDegrees / Mathf.Max(0.01f, seconds);
            kickRemaining = seconds;
        }

        void Update()
        {
            if (kickRemaining <= 0f) return;
            float dt = Mathf.Min(Time.deltaTime, kickRemaining);
            pitch = Mathf.Clamp(pitch - kickPerSecond * dt, -85f, 85f);
            kickRemaining -= dt;
            ApplyPitch();
        }

        void ApplyPitch()
        {
            if (cameraPivot != null)
                cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
        }
    }
}
