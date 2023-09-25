using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

namespace Battle
{
    public class BattleAgent : Agent
    {
        public enum Team
        {
            Red,
            Yellow
        }

        private BattleEnv m_env;
        private Rigidbody m_rigidbody;
        private Camera m_mainCam;

        private bool m_blinked = false;
        private bool m_isDead = false;

        private Vector3 m_initialPosition;
        private Quaternion m_initialRotation;

        private Tuple<int, BattleAgent> m_lastAttackedInfo = null;

        protected Animator AgentAnimator;
        protected bool IsAttackAnimationPlaying => AgentAnimator.GetBool("Attack");

        public MeshRenderer BodyMeshRenderer;
        public Canvas Canvas;
        public RectTransform HpRect;
        public RectTransform StaminaRect;

        public Material OriginalMaterial;
        public Material BlinkMaterial;

        public Team AgentTeam = Team.Red;
        public float Hp = 1f;
        public float Stamina = 1f;

        public float MoveSpeed = 1f;
        public float RotateSpeed = 300f;
        public bool RandomSpawnPosition = false;

        public bool IsAttackValid = false;

        public bool IsDead
        {
            get => m_isDead;
            set
            {
                m_isDead = value;
                foreach (var col in GetComponentsInChildren<Collider>())
                {
                    col.enabled = !value;
                }
            }
        }

        private void AttackedBySword(BattleAgent attacker)
        {
            // Prevent Friendly Attack
            if (attacker.AgentTeam == AgentTeam) return;

            // Back Attack!
            if (Vector3.Angle(attacker.transform.forward, transform.forward) <= 30f)
            {
                attacker.AddReward(0.45f);
                this.AddReward(-0.3f);

                Hp -= 0.3f;
            }
            else
            {
                attacker.AddReward(0.3f);
                this.AddReward(-0.2f);

                Hp -= 0.2f;
            }

            var currentTimeStepRatio = (float)m_env.CurrentTimeStep / m_env.MaxEnvStep;
            if (Hp <= 0.01f)
            {
                Hp = 0f;

                // Agent Killed Reward
                attacker.SetReward(3 - currentTimeStepRatio);
                this.SetReward(-3 + currentTimeStepRatio);

                m_env.OnKilled(attacker, this);
            }

            m_lastAttackedInfo = new Tuple<int, BattleAgent>(m_env.CurrentTimeStep, attacker);
        }

        private void RotateToMousePosition()
        {
            var ray = m_mainCam.ScreenPointToRay(Input.mousePosition);
            Physics.Raycast(ray, out RaycastHit hitInfo, 1000, 1 << LayerMask.NameToLayer("Ground"));

            var lookDir = hitInfo.point - transform.position;
            lookDir.y = 0f;

            transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }

        protected virtual void HandleAction(ActionBuffers actionBuffers)
        {
            var forwardAxis = actionBuffers.DiscreteActions[0];
            var rightAxis = actionBuffers.DiscreteActions[1];
            var rotateAxis = actionBuffers.DiscreteActions[2];

            var moveDir = Vector3.zero;
            var rotateDir = Vector3.zero;

            switch (forwardAxis)
            {
                case 1:
                    moveDir = transform.forward * MoveSpeed;
                    break;
                case 2:
                    moveDir = transform.forward * -MoveSpeed;
                    break;
            }

            switch (rightAxis)
            {
                case 1:
                    moveDir = transform.right * MoveSpeed;
                    break;
                case 2:
                    moveDir = transform.right * -MoveSpeed;
                    break;
            }

            switch (rotateAxis)
            {
                case 1:
                    rotateDir = transform.up * -1f;
                    break;
                case 2:
                    rotateDir = transform.up * 1f;
                    break;
            }

            transform.Rotate(rotateDir, Time.deltaTime * RotateSpeed);
            m_rigidbody.AddForce(moveDir, ForceMode.VelocityChange);
        }

        protected virtual void Start()
        {
            m_env = FindObjectOfType<BattleEnv>();
            m_rigidbody = GetComponent<Rigidbody>();
            m_mainCam = Camera.main;

            m_initialPosition = transform.position;
            m_initialRotation = transform.rotation;

            AgentAnimator = GetComponent<Animator>();
        }

        protected virtual void Update()
        {
            Canvas.transform.SetPositionAndRotation(transform.position + Vector3.up * 2f, m_mainCam.transform.rotation);
            HpRect.localScale = new Vector3(Hp, 1, 1);
            StaminaRect.localScale = new Vector3(Stamina, 1, 1);
        }

        protected virtual void FixedUpdate()
        {
            if (!m_blinked && m_lastAttackedInfo != null && m_env.CurrentTimeStep - m_lastAttackedInfo.Item1 < 5f)
            {
                BodyMeshRenderer.material = BlinkMaterial;
                m_blinked = true;
            }
            else if (m_blinked && m_lastAttackedInfo != null && m_env.CurrentTimeStep - m_lastAttackedInfo.Item1 >= 5f)
            {
                BodyMeshRenderer.material = OriginalMaterial;
                m_blinked = false;
            }

            if (!IsAttackAnimationPlaying)
            {
                Stamina = Mathf.Clamp(Stamina + 0.005f, 0f, 1f);
            }
        }

        protected virtual void OnCollisionEnter(Collision other)
        {
            if (other.collider.CompareTag("Arrow"))
            {
                Debug.Log("Hit By Arrow");
                Destroy(other.collider.gameObject);
            }
            else if (other.collider.CompareTag("Sword"))
            {
                var attacker = other.collider.transform.parent.GetComponent<BattleAgent>();
                var isAttackDuplicated =
                    m_lastAttackedInfo != null &&
                    (m_env.CurrentTimeStep - m_lastAttackedInfo.Item1 <= 30f) &&
                    attacker == m_lastAttackedInfo.Item2;

                if (attacker.IsAttackValid && !isAttackDuplicated)
                {
                    AttackedBySword(attacker);
                }
            }
        }

        protected virtual void OnCollisionStay(Collision other)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                AddReward(-5f / m_env.MaxEnvStep);
            }
        }

        public void Pushed(Vector3 force)
        {
            m_rigidbody.AddForce(force, ForceMode.VelocityChange);
        }

        public virtual void ResetSelf()
        {
            IsAttackValid = false;
            IsDead = false;

            Hp = 1f;
            Stamina = 1f;
            MoveSpeed = 1f;

            m_lastAttackedInfo = null;
            m_blinked = false;
            BodyMeshRenderer.material = OriginalMaterial;

            m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.velocity = Vector3.zero;

            if (RandomSpawnPosition)
            {
                Vector3 spawnPos;
                do
                {
                    spawnPos = new Vector3(Random.value * 20 - 10, 0, Random.value * 20 - 10);
                } while (!m_env.CheckPosition(spawnPos));

                transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            }
            else
            {
                transform.SetPositionAndRotation(m_initialPosition, m_initialRotation);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // Living Penalty
            AddReward(-1f / m_env.MaxEnvStep);

            HandleAction(actionBuffers);
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(Hp);
            sensor.AddObservation(Stamina);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var discreteActionsOut = actionsOut.DiscreteActions;
            
            // Forward / Backward
            if (Input.GetKey(KeyCode.W))
            {
                discreteActionsOut[0] = 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                discreteActionsOut[0] = 2;
            }

            // Right / Left
            if (Input.GetKey(KeyCode.D))
            {
                discreteActionsOut[1] = 1;
            }

            if (Input.GetKey(KeyCode.A))
            {
                discreteActionsOut[1] = 2;
            }
            
            // Rotate
            RotateToMousePosition();
        }
    }
}