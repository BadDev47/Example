using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Guard : MonoBehaviour {

	public GameObject bullitPrefab;
	public Transform bullitSpawnPoint;
	public float detectionRadius;
	public float detectionAngle;
	public Transform target;

	public float fireDelay;
	public float aimSpeed;
	public float bullitSpeed;

	private Vector3 aimDirection;
	private Quaternion chestOriginalRotation;
	private Transform chest;
	private Animator animator;
	private bool fireReady = true;
	private bool reverse = true;

	private int frame = 0;
	private int stopFrame = 0;
	void Start(){
		animator = GetComponent<Animator> ();
		chest = animator.GetBoneTransform (HumanBodyBones.Chest);
		chestOriginalRotation = chest.localRotation;

		Manager.RegisterRecordEvent (Record);
		Manager.RegisterReverseEvent (Reverse);
	}

	void FixedUpdate(){
		
	}

	private void Fire(Vector3 aimVector){
		GameObject bullit = Instantiate (bullitPrefab) as GameObject;
		bullit.transform.position = bullitSpawnPoint.position;
		bullit.transform.rotation = bullitSpawnPoint.rotation * Quaternion.AngleAxis(90f,Vector3.right);
		Rigidbody bullitBody = bullit.GetComponent<Rigidbody> ();
		bullitBody.isKinematic = false;
		bullitBody.velocity = aimVector * bullitSpeed;
		bullit.GetComponent<Bullit> ().speed = aimVector * bullitSpeed;
	}

	private bool CheckFirePossible(){
		return aimDirection != Vector3.zero && Vector3.Angle (transform.forward, aimDirection) <= detectionAngle;
	}

	private void ReadyFire(){
		fireReady = true;
	}

	private Vector3 CalculateAimVector(Vector3 targetDirection){
		Vector3 targetSpeed = target.gameObject.GetComponent<Rigidbody> ().velocity;
		Vector3 tangent = targetSpeed;
		Vector3 normal = targetDirection;

		Vector3.OrthoNormalize(ref normal,ref tangent);
		Vector3 compX = Vector3.Project (targetSpeed,tangent);//x component of speed vector
		float speedLengthQuad = bullitSpeed * bullitSpeed; // quad length speed vector 
		float compXLengthQuad = compX.magnitude * compX.magnitude; // quad x component of speed vector
		if ((speedLengthQuad - compXLengthQuad) >= 0) {
			Vector3 compY = normal * Mathf.Sqrt (speedLengthQuad - compXLengthQuad);//y component of speed vector
			return (compX + compY).normalized;
		} else {
			return Vector3.zero;
		}
			
	}



	void OnAnimatorIK(int layer){
		if (!reverse) {
			Quaternion boneRotation = Quaternion.identity;
			if (animator.GetBool ("Detected")) {
				Vector3 startDirection = chest.InverseTransformDirection (bullitSpawnPoint.forward);
				Vector3 targetDirection = chest.InverseTransformDirection (aimDirection);
				Quaternion targetQuaternion = Quaternion.FromToRotation (startDirection, targetDirection);
				boneRotation = Quaternion.RotateTowards (chestOriginalRotation, chest.localRotation * targetQuaternion, aimSpeed);
			} else {
				boneRotation = Quaternion.RotateTowards (chestOriginalRotation, chest.localRotation, aimSpeed * 0.7f);
			}
			chestOriginalRotation = boneRotation;
			animator.SetBoneLocalRotation (HumanBodyBones.Chest, boneRotation);
			Debug.DrawRay (bullitSpawnPoint.position, aimDirection * 101f, Color.red);
			Debug.DrawRay (bullitSpawnPoint.position, bullitSpawnPoint.forward * 101f, Color.blue);
		}
	}

	void Record(){
		if (reverse) {
			animator.StopPlayback ();
			animator.StartRecording (10000);
			frame = 0;
			//Debug.Log (animator.recorderMode);
		}
		reverse = false;
		float radius = Vector3.Distance (transform.position, target.position);
		Vector3 directionToTarget = (target.position + new Vector3 (0f, 1f, 0f)) - bullitSpawnPoint.position;
		float angle = Vector3.Angle (transform.forward, directionToTarget);


		if (radius <= detectionRadius &&  angle <= detectionAngle) {
			animator.SetBool ("Detected", true);
			aimDirection = CalculateAimVector (directionToTarget);
			aimDirection = CheckFirePossible () ? aimDirection : directionToTarget  ;
			if (fireReady && Vector3.Angle (bullitSpawnPoint.forward, aimDirection) < 2 ) {
				fireReady = false;
				Invoke ("ReadyFire", fireDelay);
				animator.SetBool ("Fire", true);
				Fire (aimDirection);
			} else {
				animator.SetBool ("Fire", false);
			}

		} else {
			animator.SetBool ("Detected", false);
		}

		Debug.DrawRay (bullitSpawnPoint.position, aimDirection * 100f,Color.green);
		Debug.DrawRay (bullitSpawnPoint.position, bullitSpawnPoint.forward * 100f, Color.white);
		frame++;
	}

	void Reverse(int reverseSpeed){
		if (!reverse) {
			stopFrame = frame;
			animator.StopRecording ();
			animator.StartPlayback ();
		}

		animator.playbackTime = Mathf.Lerp (animator.recorderStartTime, animator.recorderStopTime, (float)frame / stopFrame);
		//Debug.Log (animator.recorderStartTime + " start");
		//Debug.Log (animator.recorderStopTime + "stop");
		//Debug.Log (animator.playbackTime + " time");
		reverse = true;
		frame--;
	}
}
