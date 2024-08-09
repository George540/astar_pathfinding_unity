using UnityEngine;

namespace AI
{
    public abstract class AIMovement : MonoBehaviour
    {
        public bool debug;

        public virtual SteeringOutput GetKinematic(AIAgent agent)
        {
            return new SteeringOutput { angular = agent.transform.rotation };
        }

        public virtual SteeringOutput GetSteering(AIAgent agent)
        {
            return new SteeringOutput { angular = Quaternion.identity };
        }
    }
}
