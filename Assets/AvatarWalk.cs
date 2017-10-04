using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarWalk : MonoBehaviour {

	// 目標に到達したとみなす半径
	public float targetR = 0.1f;

	// 目標に到達したとみなす角度差
	public float targetAngleDif = 1.0f;

	// 前方とみなすベクトルZ成分
	public float forwardNormalizedZ = 0.5f;

	// 秒間最高速度
	public float maxSpeed = 3.0f;

	// 最高速度に達するまでの秒数
	public float accSec = 0.5f;

	// ブレーキをかけ始める距離
	public float brakeStartDis = 0.5f;

	// 秒間回転角度
	public float rotateSpeed = 180.0f;

	// 移動目標
	Vector3 targetPos = new Vector3(0, 0, 0);

	// 現在の速度
	float speed = 0.0f;

	// 現在の回転向き
	Vector3 motionBlend = new Vector3(0, 0, 0);

	// Use this for initialization
	void Start () {
	}

	// 初期化
	public void Reset(Vector3 position)
	{
		// 初期位置
		gameObject.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 10;

		// 目標位置は初期位置
		targetPos = gameObject.transform.position;

		// 向きの初期化
		Quaternion rot = Quaternion.Euler(new Vector3(0, 180.0f, 0));
		gameObject.transform.rotation = rot;
	}

	// クリックした場所を目標位置にする
	void UpdateTargetPos()
	{
		// クリックした瞬間のみ更新
		if (!Input.GetMouseButtonDown(1)) {
			return;
		}

		// クリックした床の位置を取得
		RaycastHit hit;
		Camera camera = Camera.main;
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);
		if (!Physics.Raycast (ray, out hit)) {
			Debug.Log ("not hit");
			return;
		}
		Debug.Log ("hit:" + hit.point.x + ", " + hit.point.y + ", " + hit.point.z);
		targetPos = hit.point;
	}

	// 速度を更新
	float UpdateSpeedAndGetMoveDis()
	{
		// キャラから見た位置に変換
		Vector3 localTargetVec = gameObject.transform.InverseTransformPoint (targetPos);
		localTargetVec.y = 0;

		// 十分近いので到達したとみなす
		if (localTargetVec.magnitude < targetR) {
			targetPos = gameObject.transform.position;
			speed = 0;
			return 0;
		}

		// 十分前方かつ遠くなら加速
		float vecZ = localTargetVec.z;
		localTargetVec.Normalize ();
		if (vecZ > brakeStartDis && localTargetVec.z > forwardNormalizedZ) {
			// 加速度
			float acc = maxSpeed / accSec;

			// 最高速度に達するまでの時間
			float sec = (maxSpeed - speed) / acc;
			sec = System.Math.Min (sec, Time.deltaTime);

			// その間の移動距離
			float dis = speed * sec + 0.5f * acc * sec * sec;

			// 最終速度
			speed += acc * sec;

			// 合計移動距離
			return dis + speed * (Time.deltaTime - sec);
		}

		// 距離に応じた目標速度
		float targetSpeed = maxSpeed * System.Math.Min(brakeStartDis, System.Math.Max(0, vecZ)) / brakeStartDis;

		// その速度まで落ちるための加速度
		float brakeAcc = (targetSpeed - speed) / Time.deltaTime;

		// その速度まで落ちる間に進む距離
		float brakeDis = speed * Time.deltaTime + 0.5f * brakeAcc * Time.deltaTime * Time.deltaTime;

		// 速度更新
		speed = targetSpeed;

		// 進んだ距離を返す
		return brakeDis;
	}

	// 位置を更新
	void UpdatePosition(float moveDis)
	{
		// 移動前のベクトル
		Vector3 vec = targetPos - gameObject.transform.position;

		// 移動前のXZベクトル
		Vector3 vec0 = new Vector3 (vec.x, 0, vec.z);

		// XZ位置更新
		gameObject.transform.position += gameObject.transform.forward * moveDis;

		// 移動後のXZベクトル
		Vector3 vec1 = targetPos - gameObject.transform.position;
		vec1.y = 0;

		// Y位置更新
		Vector3 pos = gameObject.transform.position;
		if (vec0.magnitude < targetR) { // 十分近いので到着したとみなす
			pos.y = targetPos.y;
		} else { // 距離に応じたリニア補間
			float rate = vec1.magnitude / vec0.magnitude;
			pos.y += vec.y * (1 - rate);
		}
		gameObject.transform.position = pos;
	}

	// 向きを更新
	void UpdateRotate()
	{
		// 目標までのベクトルを取得
		Vector3 targetVec = targetPos - gameObject.transform.position;
		targetVec.y = 0.0f;

		// 十分近いので到着しているとみなす
		if (targetVec.magnitude < targetR) {
			return;
		}

		// 目標を向く角度
		Quaternion targetRot = Quaternion.LookRotation (targetVec);

		// 間の角度
		float angle = Quaternion.Angle(gameObject.transform.rotation, targetRot);

		// 十分近いので到着しているとみなす
		if (angle < targetAngleDif) {
			gameObject.transform.rotation = targetRot;
			return;
		}

		// 補間率
		float rate = rotateSpeed * Time.deltaTime / angle;
		rate = System.Math.Min (rate, 1);

		// 補間
		gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, targetRot, rate);
	}

	// Update is called once per frame
	void Update () {
		/*
		// クリックした場所を目標位置にする
		UpdateTargetPos();

		// 速度更新
		float moveDis = UpdateSpeedAndGetMoveDis();

		// 位置更新
		UpdatePosition(moveDis);

		// 角度更新
		UpdateRotate();

		// モーションブレンド制御
		Vector3 localTargetVec = gameObject.transform.InverseTransformPoint (targetPos);
		localTargetVec.y = 0;
		Vector3 motionBlendTarget = new Vector3 (0, 0, 0);
		if (localTargetVec.magnitude < targetR) {
		} else {
			localTargetVec.Normalize ();
			motionBlendTarget.x = localTargetVec.x;
			motionBlendTarget.z = speed / maxSpeed;
		}
		motionBlend = (motionBlendTarget - motionBlend) * 0.1f + motionBlend;
		GetComponent<Animator>().SetFloat("X", motionBlend.x);
		GetComponent<Animator>().SetFloat("Z", motionBlend.z);
		*/
	}
}
