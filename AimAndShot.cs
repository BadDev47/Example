using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimAndShot : MonoBehaviour {

	public GameObject bullitPrefab; //префаб снаряда
	public Transform bullitSpawnPoint; // точка спавна снаряда
	public float detectionRadius; // радиус обнаружения цели
	public float detectionAngle; // угол обзора стрелка
	public Transform target; // transform цели

	public float fireDelay; // минимальное время между выстрелами
	public float aimSpeed; // скорость наведения на цель
	public float bullitSpeed; // скорость полета снаряда

	private Vector3 aimDirection; // Вектор прицеливания
	private Quaternion chestOriginalRotation; // изначальный поворот chest кости стрелка 
	private Transform chest; // transform chest кости стрелка
	private Animator animator;
	private bool fireReady = true; // готовность к стрельбе

	void Start(){
		animator = GetComponent<Animator> ();
		chest = animator.GetBoneTransform (HumanBodyBones.Chest);
		chestOriginalRotation = chest.localRotation;
        
	}

    void FixedUpdate()
    {

        float radius = Vector3.Distance(transform.position, target.position); // определения расстояния до цели
        Vector3 directionToTarget = (target.position + new Vector3(0f, 1f, 0f)) - bullitSpawnPoint.position; //создание вектор от позиции стрелка до позиции цели
        float angle = Vector3.Angle(transform.forward, directionToTarget);// определения угла между вектором вперед стрелка и directionToTarget


        if (radius <= detectionRadius && angle <= detectionAngle)//если цель достаточно близко и находиться в поле зрения
        {

            animator.SetBool("Detected", true);
            aimDirection = CalculateAimVector(directionToTarget);// расчет вектора прицеливания
            aimDirection = aimDirection != Vector3.zero ? aimDirection : directionToTarget;

            if (fireReady && Vector3.Angle(bullitSpawnPoint.forward, aimDirection) < 2) // если готов к стрельбе и угол между направлением оружия и вектором наведения меньше 2 градусов
            {
                fireReady = false;
                Invoke("ReadyFire", fireDelay);
                animator.SetBool("Fire", true);
                Fire(aimDirection);
            }
            else
            {
                animator.SetBool("Fire", false);
            }

        }
        else
        {
            animator.SetBool("Detected", false);
        }

    }

    private void Fire(Vector3 aimVector){
		GameObject bullit = Instantiate (bullitPrefab) as GameObject; // создание снаряда

        bullit.transform.position = bullitSpawnPoint.position; // присвоение снаряду позиции и поворота
		bullit.transform.rotation = bullitSpawnPoint.rotation * Quaternion.AngleAxis(90f,Vector3.right);

        Rigidbody bullitBody = bullit.GetComponent<Rigidbody> ();// присвоение снаряду скорости
		bullitBody.isKinematic = false;
		bullitBody.velocity = aimVector * bullitSpeed;
		bullit.GetComponent<Bullit> ().speed = aimVector * bullitSpeed;
	}

	private void ReadyFire(){
		fireReady = true;
	}

	private Vector3 CalculateAimVector(Vector3 directionToTarget){ // функция расчета вектора прицеливания
		Vector3 targetSpeed = target.gameObject.GetComponent<Rigidbody> ().velocity; // скорость цели

        Vector3 normal = directionToTarget;//вектор от стрелка до цели
        Vector3 tangent = targetSpeed; 
		

		Vector3.OrthoNormalize(ref normal,ref tangent);// ортонормирование векторов, теперь между normal и tangent угол 90 градусов
		Vector3 compX = Vector3.Project (targetSpeed,tangent);//проецирование вектора скорости цели на tangent, x компонент вектора прицеливания
		float speedLengthQuad = bullitSpeed * bullitSpeed; // квадрат скорости пули
		float compXLengthQuad = compX.magnitude * compX.magnitude; // квадрат длины x компонента вектора прицеливания
		if ((speedLengthQuad - compXLengthQuad) > 0) { //если разность квадратов <= 0 то выстрел не возможен 
			Vector3 compY = normal * Mathf.Sqrt (speedLengthQuad - compXLengthQuad);//у компонент вектора прицеливания
			return (compX + compY).normalized;// получения вектора прицеливания из компонентов
		} else {
			return Vector3.zero; // вывод Vector3.zero если невозможна стрельба
		}
			
	}



	void OnAnimatorIK(int layer){
		
			Quaternion boneRotation = Quaternion.identity; // иниацилизация переменной поворота кости
			if (animator.GetBool ("Detected")) {
				Vector3 startDirection = chest.InverseTransformDirection (bullitSpawnPoint.forward);// перевод векторов из глобальной системы координат в локальную
				Vector3 targetDirection = chest.InverseTransformDirection (aimDirection); 
				Quaternion targetQuaternion = Quaternion.FromToRotation (startDirection, targetDirection);// получения поворота из локальных векторов направления оружия и вектора от стрелка до цели
				boneRotation = Quaternion.RotateTowards (chestOriginalRotation, chest.localRotation * targetQuaternion, aimSpeed); // поворот кости chest на найденый поворот
			} else {
				boneRotation = Quaternion.RotateTowards (chestOriginalRotation, chest.localRotation, aimSpeed * 0.7f);
			}
			chestOriginalRotation = boneRotation;
			animator.SetBoneLocalRotation (HumanBodyBones.Chest, boneRotation);

	}

	
}
