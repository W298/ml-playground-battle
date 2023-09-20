using UnityEngine;

namespace Battle
{
    public class KnightAttackReturnBehaviour : StateMachineBehaviour
    {
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetBool("Attack", false);
            animator.GetComponent<KnightAgent>().ResetSwordTransform();
        }
    }
}
