using UnityEngine;

namespace _Runtime._Scripts
{
    public class PlayerState : MonoBehaviour
    {
        public enum EPlayerState
        {
            Frozen,
            Unfrozen,
            Targeted,
            Rescuer,
            Tagged
        }
    }
}
