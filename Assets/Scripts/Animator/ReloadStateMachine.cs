using StarterAssets;
using UnityEngine;
using UnityEngine.Animations;

public class ReloadStateMachine : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Character controller = animator.gameObject.GetComponent<Character>();
        if (controller != null)
        {
            controller.ReloadFinish();
        }
    }
}
