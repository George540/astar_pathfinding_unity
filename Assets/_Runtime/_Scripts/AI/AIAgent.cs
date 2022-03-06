using System;
using System.Text;
using _Runtime._Scripts;
using UnityEngine;

namespace AI
{
    public class AIAgent : MonoBehaviour
    {
        public float maxSpeed;
        public float maxDegreesDelta;
        public bool lockY = true;
        public bool debug;
        public bool isChangingOrientation = false;

        public enum EBehaviorType { Kinematic, Steering }
        public EBehaviorType behaviorType;
        public PlayerState.EPlayerState _ePlayerState;

        [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Animator animator;

        public Transform trackedTarget;
        public Vector3 targetPosition;

        public GridGraph _graph;

        public Vector3 TargetPosition
        {
            get => trackedTarget != null ? trackedTarget.position : transform.position;
        }
        public Vector3 TargetForward
        {
            get => trackedTarget != null ? trackedTarget.forward : Vector3.forward;
        }
        public Vector3 TargetVelocity
        {
            get
            {
                Vector3 v = Vector3.zero;
                if (trackedTarget != null)
                {
                    AIAgent targetAgent = trackedTarget.GetComponent<AIAgent>();
                    if (targetAgent != null)
                        v = targetAgent.Velocity;
                }

                return v;
            }
        }

        public Vector3 Velocity { get; set; }

        public void TrackTarget(Transform targetTransform)
        {
            trackedTarget = targetTransform;
        }

        public void UnTrackTarget()
        {
            trackedTarget = null;
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            // Uncomment this if you wanna test Wrapping positions
            // Note: There is no functionality of pursuer calculating wrapping distance :/
            CheckPosition();
            if (GameManager.Instance != null)
            {
                CheckIfUnfrozen();
            }

            if (debug)
                Debug.DrawRay(transform.position, Velocity, Color.red);

            if (behaviorType == EBehaviorType.Kinematic)
            {
                // TODO: average all kinematic behaviors attached to this object to obtain the final kinematic output and then apply it
				GetKinematicAvg(out Vector3 kinematicAvg, out Quaternion rotation);

                Velocity = kinematicAvg.normalized * maxSpeed;

                transform.position += Velocity * Time.deltaTime;

                rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                // INTERPOLATION OF ROTATION IS HERE
                transform.rotation = isChangingOrientation ? Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 1000f) : rotation;
            }
            else
            {
                // TODO: combine all steering behaviors attached to this object to obtain the final steering output and then apply it
				GetSteeringSum(out Vector3 steeringASum, out Quaternion rotation);

                Vector3 acceleration = steeringASum / 1;
                Velocity += acceleration * Time.deltaTime;
                Velocity = Vector3.ClampMagnitude(Velocity, maxSpeed);

                transform.position += Velocity * Time.deltaTime;

                rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, transform.rotation * rotation, maxDegreesDelta * Time.deltaTime);
            }

            animator.SetBool("walking", Velocity.magnitude > 0);
            animator.SetBool("running", Velocity.magnitude > maxSpeed/2);
        }

        private void GetKinematicAvg(out Vector3 kinematicAvg, out Quaternion rotation)
        {
            kinematicAvg = Vector3.zero;
            Vector3 eulerAvg = Vector3.zero;
            AIMovement[] movements = GetComponents<AIMovement>();
            int count = 0;
            foreach (AIMovement movement in movements)
            {
                kinematicAvg += movement.GetKinematic(this).linear;
                eulerAvg += movement.GetKinematic(this).angular.eulerAngles;

                ++count;
            }

            if (count > 0)
            {
                kinematicAvg /= count;
                eulerAvg /= count;
                rotation = Quaternion.Euler(eulerAvg);
            }
            else
            {
                kinematicAvg = Velocity;
                rotation = transform.rotation;
            }
        }

        private void GetSteeringSum(out Vector3 steeringForceSum, out Quaternion rotation)
        {
            steeringForceSum = Vector3.zero;
            rotation = Quaternion.identity;
            AIMovement[] movements = GetComponents<AIMovement>();
            foreach (AIMovement movement in movements)
            {
                steeringForceSum += movement.GetSteering(this).linear;
                rotation *= movement.GetSteering(this).angular;
            }
        }

        public void SetMaterial(Material mat)
        {
            _skinnedMeshRenderer.material = mat;
        }

        // Wraps agent's position within a certain perimeter.
        // If agent exceeds X-Z limits, it wraps on the other side
        void CheckPosition()
        {
            var offset = 40f;
            if (GameManager.Instance != null)
            {
                offset = GameManager.Instance.maxOffset;
            }
            if (transform.position.x < -offset)
            {
                transform.position = new Vector3(offset - 1, transform.position.y, transform.position.z);
            }
            else if (transform.position.x > offset)
            {
                transform.position = new Vector3(-offset + 1, transform.position.y, transform.position.z);
            }
            
            if (transform.position.z < -offset)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, offset - 1);
            }
            else if (transform.position.z > offset)
            {
                transform.position = new Vector3(transform.position.x, transform.position.y, -offset + 1);
            }
        }

        void CheckIfUnfrozen()
        {
            if (trackedTarget == null) return;
            
            if (_ePlayerState == PlayerState.EPlayerState.Rescuer &&
                trackedTarget.gameObject.GetComponent<AIAgent>()._ePlayerState == PlayerState.EPlayerState.Frozen)
            {
                if (Vector3.Distance(trackedTarget.position, transform.position) <=
                    GameManager.Instance._tagDistance)
                {
                    // Make frozen guy unfreeze and keep wandering
                    Destroy(trackedTarget.gameObject.GetComponent<Stop>());
                    var target = trackedTarget.GetComponent<AIAgent>();
                    target.behaviorType = EBehaviorType.Steering;
                    target.gameObject.AddComponent<Wander>();
                    target.gameObject.AddComponent<LookWhereYouAreGoing>();
                    target.SetMaterial(GameManager.Instance._cMaterials[1]);
                    SetMaterial(GameManager.Instance._cMaterials[1]);
                    target._ePlayerState = PlayerState.EPlayerState.Unfrozen;
                    target.UnTrackTarget();
                        
                    // Make agent who unfreezes to keep wandering
                    if (gameObject.GetComponent<Arrive>() != null)
                    {
                        Destroy(gameObject.GetComponent<Arrive>());
                    }
                    if (gameObject.GetComponent<LookWhereYouAreGoing>() != null)
                    {
                        Destroy(gameObject.GetComponent<LookWhereYouAreGoing>());
                    }
                    _ePlayerState = PlayerState.EPlayerState.Unfrozen;
                    gameObject.AddComponent<Wander>();
                    UnTrackTarget();
                }
            }
        }
        
        public bool HasArrivedAtTarget()
        {
            return gameObject.TryGetComponent<Arrive>(out var arrive) && arrive.HasArrivedAtTarget(this);
        }
    }
}