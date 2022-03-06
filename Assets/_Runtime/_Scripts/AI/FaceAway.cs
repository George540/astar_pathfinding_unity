using UnityEngine;

namespace AI
{
    public class FaceAway : AIMovement
    {
        public override SteeringOutput GetKinematic(AIAgent agent)
        {
            var output = base.GetKinematic(agent);

            // TODO: calculate angular component
            Vector3 direction = agent.transform.position - agent.TargetPosition;

            if (direction.normalized == agent.transform.forward || Mathf.Approximately(direction.magnitude, 0))
            {
                return output;
            }
            output.angular = Quaternion.LookRotation(direction);

            return output;
        }

        public override SteeringOutput GetSteering(AIAgent agent)
        {
            var output = base.GetSteering(agent);

            // TODO: calculate angular component
            if (agent.lockY)
            {
                Vector3 from = Vector3.ProjectOnPlane(agent.transform.forward, Vector3.up);
                Vector3 to = Vector3.ProjectOnPlane(GetKinematic(agent).angular * Vector3.forward, Vector3.up);
                float angle = Vector3.SignedAngle(from, to, Vector3.up);
                output.angular = Quaternion.AngleAxis(angle, Vector3.up);
            }
            output.angular = Quaternion.FromToRotation(agent.transform.forward, GetKinematic(agent).angular * Vector3.forward);
			

            return output;
        }
    }
}
