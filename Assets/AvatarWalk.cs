using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

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

	// カメラ中央へ
	float cameraTargetWait = 0.0f;

	// 現在の回転向き
	Vector3 motionBlend = new Vector3(0, 0, 0);

	// ターゲット位置確認用キューブ
	GameObject cube;

	// Use this for initialization
	void Start () {
		// キューブを探しておく
		GameObject root = transform.parent.transform.gameObject;
		cube = root.transform.Find ("Cube").gameObject;
	}

	// 平面
	class AvatarPlane {
		public Vector3 center { get; private set; }
		public Matrix4x4 matrix { get; private set; }
		public Quaternion rotate { get; private set; }
		public Vector3 extent { get; private set; }
		public void Setup(ARPlaneAnchor arPlaneAnchor) {
			center = new Vector3(arPlaneAnchor.center.x,arPlaneAnchor.center.y, -arPlaneAnchor.center.z);
			matrix = arPlaneAnchor.transform;
			rotate = UnityARMatrixOps.GetRotation (arPlaneAnchor.transform);
			extent = arPlaneAnchor.extent;
		}
	}
	AvatarPlane plane;

	// 土台の平面
	public void SetPlane(ARPlaneAnchor arPlaneAnchor)
	{
		Debug.Log ("SetPlane:" + arPlaneAnchor.identifier);

		// キャラの初期化済みか？
		bool isFirst = (plane == null);
		if (isFirst) {
			plane = new AvatarPlane ();
		}

		// 平面記録
		plane.Setup (arPlaneAnchor);

		// 以下はキャラの初期化
		if (!isFirst) {
			return;
		}

		// 初期位置を中心位置に
		gameObject.transform.localPosition = plane.center;

		// 目標位置は初期位置
		targetPos = gameObject.transform.localPosition;

		// 向きの初期化
		Quaternion rot = Quaternion.Euler(new Vector3(0, 180.0f, 0));
		gameObject.transform.localRotation = rot;
	}

	// クリックした場所を目標位置にする
	void UpdateTargetPos()
	{
		// 平面が必要
		if (plane == null) {
			return;
		}

		// 一定間隔ごとにカメラ中央へ
		Camera camera = Camera.main;
		GameObject waitGO = GameObject.Find ("InputField");
		UnityEngine.UI.InputField wait = (waitGO == null) ? null : waitGO.GetComponent<UnityEngine.UI.InputField> ();
		if (wait != null) {
			float waitSec = -1;
			if (float.TryParse (wait.text, out waitSec) && waitSec >= 0) {
				cameraTargetWait += Time.deltaTime;
				if (cameraTargetWait > waitSec) {
					CalcTargetPos (camera.transform.position, camera.transform.position + camera.transform.forward);
					cameraTargetWait = 0;
				}
				return;
			}
		}
		cameraTargetWait = 0;

		// クリックした瞬間のみ更新
		if (!Input.GetMouseButtonDown (0)) {
			return;
		}
		Debug.Log ("Click!");

		// クリックした床の位置を取得
		RaycastHit hit;
		Ray ray = camera.ScreenPointToRay (Input.mousePosition);
		Vector3 pos3a = camera.transform.position;
		Vector3 pos3b = pos3a + ray.direction;

		// そこをターゲットに
		CalcTargetPos(pos3a, pos3b);
	}

	// pos3aからpos3bへ向かうベクトルと床平面との交点を目標位置にする
	void CalcTargetPos(Vector3 pos3a, Vector3 pos3b)
	{
		// 床平面と直線との交点を求める
		Debug.Log ("pos3a " + pos3a.x + "," + pos3a.y + "," + pos3a.z);
		Debug.Log ("pos3b " + pos3b.x + "," + pos3b.y + "," + pos3b.z);
		GameObject root = transform.parent.transform.gameObject;
		//Vector4 pos0 = plane.matrix.inverse * new Vector4(pos3a.x, pos3a.y, pos3a.z, 1);
		//Vector4 pos1 = plane.matrix.inverse * new Vector4(pos3b.x, pos3b.y, pos3b.z, 1);
		Vector3 pos0 = root.transform.InverseTransformPoint (pos3a);
		Vector3 pos1 = root.transform.InverseTransformPoint (pos3b);
		Debug.Log ("pos0 " + pos0.x + "," + pos0.y + "," + pos0.z);
		Debug.Log ("pos1 " + pos1.x + "," + pos1.y + "," + pos1.z);
		Vector3 vec = pos1 - pos0;
		vec.Normalize ();
		Debug.Log ("vec " + vec.x + "," + vec.y + "," + vec.z);
		if (vec.y > -0.01f) { // 平行または離れるっぽい
			return;
		}
		float scale = pos0.y / vec.y;
		Debug.Log ("scale " + scale);
		Vector3 pos = new Vector3 (pos0.x, pos0.y, pos0.z) - vec * scale;
		Debug.Log ("pos " + pos.x + "," + pos.y + "," + pos.z);
		targetPos = pos;
	}

	// targetPosを平面の範囲内にクリップする
	void ClipTargetPos()
	{
		// 平面の範囲内にクリップするか
		GameObject clipGO = GameObject.Find("Clip");
		UnityEngine.UI.Toggle toggle = (clipGO == null) ? null : clipGO.GetComponent<UnityEngine.UI.Toggle> ();
		if (toggle == null || !toggle.isOn) {
			return;
		}

		// クリップ処理
		Vector3 p = targetPos - plane.center;
		if (System.Math.Abs(p.x) > System.Math.Abs(plane.extent.x)) {
			p *= System.Math.Abs(plane.extent.x / p.x);
		}
		if (System.Math.Abs(p.z) > System.Math.Abs(plane.extent.z)) {
			p *= System.Math.Abs(plane.extent.z / p.z);
		}
		targetPos = p + plane.center;
	}

	// 速度を更新
	float UpdateSpeedAndGetMoveDis()
	{
		// キャラから見た位置に変換
		Matrix4x4 matrix = Matrix4x4.TRS(gameObject.transform.localPosition, gameObject.transform.localRotation, new Vector3(1, 1, 1));
		Vector3 localTargetVec = matrix.inverse.MultiplyPoint (targetPos);
		localTargetVec.y = 0;

		// 十分近いので到達したとみなす
		if (localTargetVec.magnitude < targetR) {
			targetPos = gameObject.transform.localPosition;
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
		Vector3 vec = targetPos - gameObject.transform.localPosition;

		// 移動前のXZベクトル
		Vector3 vec0 = new Vector3 (vec.x, 0, vec.z);

		// XZ位置更新
		Vector3 forward = vec0;
		forward.Normalize ();
		gameObject.transform.localPosition += forward * moveDis;

		// 移動後のXZベクトル
		Vector3 vec1 = targetPos - gameObject.transform.localPosition;
		vec1.y = 0;

		// Y位置更新
		Vector3 pos = gameObject.transform.localPosition;
		if (vec0.magnitude < targetR) { // 十分近いので到着したとみなす
			pos.y = targetPos.y;
		} else { // 距離に応じたリニア補間
			float rate = vec1.magnitude / vec0.magnitude;
			pos.y += vec.y * (1 - rate);
		}
		gameObject.transform.localPosition = pos;
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
		float angle = Quaternion.Angle(gameObject.transform.localRotation, targetRot);

		// 十分近いので到着しているとみなす
		if (angle < targetAngleDif) {
			gameObject.transform.localRotation = targetRot;
			return;
		}

		// 補間率
		float rate = rotateSpeed * Time.deltaTime / angle;
		rate = System.Math.Min (rate, 1);

		// 補間
		gameObject.transform.localRotation = Quaternion.Slerp(gameObject.transform.localRotation, targetRot, rate);
	}

	// Update is called once per frame
	void Update () {
		// クリックした場所を目標位置にする
		UpdateTargetPos();
		ClipTargetPos ();

		// デバッグ用にキューブの位置更新
		cube.transform.localPosition = targetPos;
		GameObject cubeGO = GameObject.Find("CubeToggle");
		UnityEngine.UI.Toggle toggle = (cubeGO == null) ? null : cubeGO.GetComponent<UnityEngine.UI.Toggle> ();
		if (toggle != null) {
			cube.SetActive (toggle.isOn);
		}

		// 速度更新
		float moveDis = UpdateSpeedAndGetMoveDis();

		// 位置更新
		UpdatePosition(moveDis);

		// 角度更新
		UpdateRotate();

		// モーションブレンド制御
		Matrix4x4 matrix = Matrix4x4.TRS(gameObject.transform.localPosition, gameObject.transform.localRotation, new Vector3(1, 1, 1));
		Vector3 localTargetVec = matrix.inverse.MultiplyPoint (targetPos);
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
	}

	// 顔の向き制御
	void LateUpdate()
	{
		HeadLookController hl = gameObject.GetComponent<HeadLookController> ();
		if (hl == null) {
			return;
		}
		hl.target = Camera.main.transform.position;
	}
}
