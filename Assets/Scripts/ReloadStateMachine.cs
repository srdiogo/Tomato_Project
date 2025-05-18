using StarterAssets;
using UnityEngine;
using UnityEngine.Animations;

public class ReloadStateMachine : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        ThirdPersonController controller = animator.gameObject.GetComponent<ThirdPersonController>();
        if (controller != null)
        {
            controller.ReloadFinish();
        }
    }
}
