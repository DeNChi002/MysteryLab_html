using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CameraSystem : MonoBehaviour
{
    public GameObject Target;

    public int C_LerpSpeed;

    public float XMaxRadian = 50f, XMinRadian = -40f;

    int XCameraSpeed, YCameraSpeed;

    //回転ベクトル
    Vector3 C_Rotate;

    //カメラオフセットの空オブジェクト
    GameObject CameraOffset;

    //カメラの原点となるからオブジェクト
    GameObject CameraOrigin;

    //プレイヤーの頭上に位置する空オブジェクト
    GameObject CameraUpOrigin;

    EnumStrage.CameraMode CamMode;

    //カメラの制御用ブール
    public static bool CameraControle = true;

    //オプション内容変更時のブール
    public static bool SetOption = false;

    void Start()
    {
        //CameraOffsetを作成
        CameraOffset = new GameObject("CameraOffset");

        //CameraOriginを作成
        CameraOrigin = new GameObject("CameraOrigin");

        //CameraUpOriginを作成
        CameraUpOrigin = new GameObject("CameraUpOrigin");

        //追従するターゲットを検索する
        Target = GameObject.FindGameObjectWithTag("Player");

        //オフセット座標をターゲットの座標にする
        CameraOffset.transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y + 1.0f, Target.transform.position.z);
        
        //カメラの原点にする
        CameraOrigin.transform.position = Camera.main.transform.position;

        //プレイヤーの頭上に位置する
        CameraUpOrigin.transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y + 1.3f, Target.transform.position.z + 0.3f);

        //自身をOffsetの子にする
        transform.parent = CameraOffset.transform;

        //CameraOriginをOffsetの子にする
        CameraOrigin.transform.parent = CameraOffset.transform;

        //CameraUpOriginをTargetの子にする
        CameraUpOrigin.transform.parent = Target.transform;

        //カメラモード情報を取得する
        CamMode = GetComponent<CameraModeManager>().CameraModeNum;

        //オプション項目から数値を取得
        XCameraSpeed = (int)Option.GameOption[(int)Option.OptionType.CameraXAxis];
        YCameraSpeed = (int)Option.GameOption[(int)Option.OptionType.CameraYAxis];
        C_LerpSpeed = 8;
    }

    public static void CameraOptionSet()
    {
        SetOption = true;        
    }

    private void OptionChange()
    {
        //オプション項目から数値を取得
        XCameraSpeed = (int)Option.GameOption[(int)Option.OptionType.CameraXAxis];
        YCameraSpeed = (int)Option.GameOption[(int)Option.OptionType.CameraYAxis];

        SetOption = false;
    }

    void Update()
    {
        if (CamMode == EnumStrage.CameraMode.GameCamera)
        {
            //カメラの操作ができる場合
            if (CameraControle)
            {
                CameraAngle();

                RaySystem();
            }
        }

        if (SetOption)
        {
            OptionChange();
        }
    }

    void LateUpdate()//Updateより遅く回す
    {
        OffsetMove();
    }

    //カメラのアングル制御
    private void CameraAngle()
    {
        //コントローラーの入力情報を取得
        Vector3 CalcRotate = new Vector2(InputManager.CameraVAxis, InputManager.CameraHAxis);

        //回転角度を加算
        C_Rotate += CalcRotate * XCameraSpeed * YCameraSpeed;

        //角度の制限
        C_Rotate.x = Mathf.Clamp(C_Rotate.x, -20f, 40f);

        //カメラの回転
        CameraOffset.transform.eulerAngles = C_Rotate;
    }

    //オフセットの移動 
    private void OffsetMove()
    {
        //ターゲットの座標を算出する
        Vector3 TargetPos = new Vector3(Target.transform.position.x, Target.transform.position.y + 1.0f, Target.transform.position.z);

        //オフセットの移動
        CameraOffset.transform.position = Vector3.Lerp(CameraOffset.transform.position, TargetPos, C_LerpSpeed * Time.deltaTime);
    }

    private void RaySystem()
    {
        RaycastHit hit;

        //オフセットからRayTargetの方向を取得
        Vector3 direction = CameraOrigin.transform.position - CameraOffset.transform.position;

        //デバッグ用(Rayの描画)
        //Debug.DrawRay(CameraOffset.transform.position, CameraOrigin.transform.position - CameraOffset.transform.position, Color.red, 1.0f);

        //OffsetからRayTargetまでの距離を計算
        float distance = Vector3.Distance(CameraOrigin.transform.position, CameraOffset.transform.position);

        //Rayを作成
        Ray ray = new Ray(CameraOffset.transform.position, direction);

        if (Physics.Raycast(ray, out hit, distance, 1 << LayerMask.NameToLayer("Wall")))
        {
            //Rayが衝突した場所を取得して、少し調整する
            Vector3 avoidPos = hit.point - direction.normalized * 0.01f;            
           
            //Rayが当たった場所からカメラまでの距離を計算する
            float PlayerToCamera = Vector3.Distance(hit.point, Target.transform.position);

            //プレイヤーとカメラの距離が2以下の時
            if (PlayerToCamera <= 2.0f)
            {
                //カメラをプレイヤーの頭上に移動させる
                transform.position = Vector3.Lerp(transform.position, CameraUpOrigin.transform.position, C_LerpSpeed * Time.deltaTime);
            }
            else
            {
                //カメラを衝突した場所に移動させる
                transform.position = Vector3.Lerp(transform.position, avoidPos, C_LerpSpeed * Time.deltaTime);

            }
        }
        else
        {          
            //カメラをカメラ原点に移動させる
            transform.position = Vector3.Lerp(transform.position, CameraOrigin.transform.position, C_LerpSpeed * Time.deltaTime);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraSystem))]
public class CameraSystemEditor : Editor
{
    int XMin, XMax;

    public override void OnInspectorGUI()
    {
        //インスタンス化
        CameraSystem Edit = target as CameraSystem;

        EditorGUILayout.LabelField("カメラ追従させるターゲット");
        Edit.Target = (GameObject)EditorGUILayout.ObjectField("Target", Edit.Target, typeof(Object), true);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("カメラの移動速度");
        Edit.C_LerpSpeed = EditorGUILayout.IntSlider("0～10", Edit.C_LerpSpeed, 0, 10);
    }
}
#endif
