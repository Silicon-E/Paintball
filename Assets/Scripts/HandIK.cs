using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandIK : MonoBehaviour {

	public Animator animator;
	//public Transform ikR;
	//public Transform ikL;
	public Transform handleR;
	public Transform handleL;

	void OnAnimatorIK()
	{
		animator.SetIKPositionWeight(AvatarIKGoal.RightHand,1);
		animator.SetIKRotationWeight(AvatarIKGoal.RightHand,1);  
		animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,1);
		animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,1);  

		animator.SetIKPosition(AvatarIKGoal.RightHand, handleR.position);
		animator.SetIKRotation(AvatarIKGoal.RightHand, handleR.rotation);
		animator.SetIKPosition(AvatarIKGoal.LeftHand, handleL.position);
		animator.SetIKRotation(AvatarIKGoal.LeftHand, handleL.rotation);

		//ikR.position = handleR.position;
		//ikL.position = handleL.position;
	}
}
