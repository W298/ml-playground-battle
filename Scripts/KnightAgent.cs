using UnityEngine;
using Unity.MLAgents.Actuators;

namespace Battle
{
    public class KnightAgent : BattleAgent
    {
        public GameObject Sword;
        public GameObject FrontShield;
        
        private void ResetShieldTransform()
        {
            FrontShield.transform.localPosition = new Vector3(0.174f, 0, 0.691f);
            FrontShield.transform.localRotation = Quaternion.Euler(0, 5.319f, 0);
        }
        
        private void SetShield(bool toEquip)
        {
            ResetShieldTransform();
            
            FrontShield.SetActive(toEquip);
            MoveSpeed = toEquip ? 0.5f : 1f;
        }

        protected override void Update()
        {
            base.Update();
            
            if (!IsAttackAnimationPlaying) ResetSwordTransform();
        }
        
        protected override void HandleAction(ActionBuffers actionBuffers)
        {
            if (IsDead) return;
            
            base.HandleAction(actionBuffers);

            switch (actionBuffers.DiscreteActions[3])
            {
                case 0:
                    SetShield(false);
                    break;
                case 1:
                    if (IsAttackAnimationPlaying) break;
                    SetShield(true);
                    break;
                case 2:
                    if (Stamina <= 0.3f || IsAttackAnimationPlaying) break;
                    SetShield(false);
                    AgentAnimator.SetBool("Attack", true);
                    Stamina = Mathf.Clamp(Stamina - 0.3f, 0f, 1f);
                    break;
            }
        }
        
        public void ResetSwordTransform()
        {
            Sword.transform.localPosition = new Vector3(-0.5f, -0.34f, 0.447f);
            Sword.transform.localRotation = new Quaternion(0.089f, -0.701f, -0.089f, 0.701f);
        }

        public override void ResetSelf()
        {
            base.ResetSelf();
            
            AgentAnimator.SetBool("Attack", false);

            ResetSwordTransform();
            SetShield(false);
        }
        
        public void EnableAttackValid()
        {
            IsAttackValid = true;
        }

        public void DisableAttackValid()
        {
            IsAttackValid = false;
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            base.Heuristic(actionsOut);
            
            var discreteActionsOut = actionsOut.DiscreteActions;
            
            if (Input.GetMouseButton(0))
            {
                discreteActionsOut[3] = 2;
            }
            else if (Input.GetMouseButton(1))
            {
                discreteActionsOut[3] = 1;
            }
            else
            {
                discreteActionsOut[3] = 0;
            }
        }
    }
}