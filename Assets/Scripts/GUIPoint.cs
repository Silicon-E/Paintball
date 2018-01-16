using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIPoint : MonoBehaviour {

	//NOTE: this is purely a container MonoBehaviour for reference by its corresponding Point.cs MonoBehaviour.

	public Slider slider;
	public Image fill; //The slider fill
	public Image dot; //The dot that indicates capturing
	public Image clash; //The half-dot that indicates blocking
	public Image arrow;
}
