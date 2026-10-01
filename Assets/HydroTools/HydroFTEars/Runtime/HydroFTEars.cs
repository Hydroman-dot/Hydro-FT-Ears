using UnityEngine;
namespace Hydro.Tools.FTEars
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Hydro Tools/Hydro FT Ears")]
    public sealed class HydroFTEars : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public Transform leftEar, rightEar;
        public string eyeXLeft = "", eyeXRight = "", eyeY = "";
        public string expressionLeft = "", expressionRight = "", idleParameter = "";
        public string frownLeft = "", frownRight = "";
        public bool expressionSigned = true, idleEnabled = true;
        [Range(0, 5)] public float defaultIntensity = 0.65f;
        public Vector3 leftLook = new Vector3(0, 7, 0), rightLook = new Vector3(0, 7, 0);
        public Vector3 leftUp = new Vector3(-4, 0, 0), rightUp = new Vector3(-4, 0, 0);
        public Vector3 leftSmile = new Vector3(0, 0, -8), rightSmile = new Vector3(0, 0, 8);
        public Vector3 leftFrown = new Vector3(8, 0, 10), rightFrown = new Vector3(8, 0, -10);
        public Vector3 leftIdle = new Vector3(1.2f, 0, 1), rightIdle = new Vector3(1.2f, 0, -1);
        [Min(0.5f)] public float idlePeriod = 5;
        [HideInInspector] public string generatedFolder;
    }
}
