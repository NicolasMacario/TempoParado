using UnityEngine;

namespace TempoParado
{
    // Todos os sistemas recebem o mesmo reinicio, independente da ordem de Update.
    public static class LabReset
    {
        private static int scheduledFrame = -1;
        public static bool Requested => LabInput.ResetPressed || Time.frameCount == scheduledFrame;
        public static void RequestNextFrame() { scheduledFrame = Time.frameCount + 1; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize() { scheduledFrame = -1; }
    }
}
