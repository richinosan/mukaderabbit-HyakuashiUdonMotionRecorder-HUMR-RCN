
/*******
 * OutputLogLoader.cs
 * 
 * メインの処理を行う。ログ出力時と同一のアバターをHierarchy上に置き、これをアタッチして使用することを想定している
 * PackageManagerからFBXExportorをインストールしておく必要あり
 * 
 * フォルダを構成して、OutputLog_xx_xx_xxからアニメーションを作成
 * そのアニメーションをアバターのアニメーターに入れてFBXとして出力
 * FBXをHumanoidにすることでHumanoidAnimationを得られるようにしている
 * 
 * *****/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEngine.EventSystems;

namespace HUMR
{
#if UNITY_EDITOR
    public interface OutputLogLoaderinterface : IEventSystemHandler
    {
        void LoadLogToExportAnim();
    }

    [RequireComponent(typeof(Animator))]
    public class OutputLogLoader : MonoBehaviour, OutputLogLoaderinterface
    {
        Animator animator;
        UnityEditor.Animations.AnimatorController controller;
        string[] files;
        [HideInInspector]
        public string OutputLogPath = "";
        [HideInInspector]
        public int index = 0;

        static int nHeaderStrNum = 19;//timestamp example/*2021.01.03 20:57:35*/
        static string strKeyWord = " Log        -  HUMR:";
        static string strAudioTimeKeyWord = " Log        -  ADTM:";
        static string strCNTWord = " Log        -  CUNT:";
        static string strTimeRugKeyWord = " Log        -  TMRG:";
        static float StartTime = 0;
        static float TimeRug = 0;
        [TooltipAttribute("GenericAnimationを出力する場合はチェックを入れてください(チェックがないと複数のAnimationを出力できません)")]
        public bool ExportGenericAnimation = false;
        [TooltipAttribute("モーションを出力したいユーザーの名前を書いてください")]
        public string DisplayName = "";
        [TooltipAttribute("撮影した回数で選択")]
        public int targetCUNT = 0;
        [TooltipAttribute("x座標を修正する場合はチェックを入れてください")]
        public bool isfix_x = false;
        [TooltipAttribute("x座標の修正指標です。")]
        public float fix_x = 0f;
        [TooltipAttribute("x座標を修正する場合はチェックを入れてください")]
        public bool isfix_y = true;
        [TooltipAttribute("y座標の修正指標です")]
        public float fix_y = 0f;
        [TooltipAttribute("z座標を修正する場合はチェックを入れてください")]
        public bool isfix_z = true;
        [TooltipAttribute("z座標の修正指標です")]
        public float fix_z = 0f;

        float index_x = 0f;
        float index_y = 0f;
        float index_z = 0f;
        public string getname()
        {
            return gameObject.name;
        }
        public void LoadLogToExportAnim()
        {
            if (DisplayName == "")
            {
                Debug.LogWarning("DisplayName is null");
                return;
            }
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            string humrPath = @"Assets/HUMR";
            CreateDirectoryIfNotExist(humrPath);

            ControllerSetUp(humrPath);

            string[] files = Directory.GetFiles(OutputLogPath, "*.txt");

            string[] strOutputLogLines = File.ReadAllLines(files[index]);
            int nTargetCounter = 0;
            List<int> newTargetLines = new List<int>();//ファイルの中での新しく始まった対象の行を格納する
            newTargetLines.Add(0);
            List<int> newLogLines = new List<int>();//抽出したログの中で新しく始まった行を格納する
            newLogLines.Add(0);
            Dictionary<int, List<string>> CUNT = new Dictionary<int, List<string>>(); // CUNTのintに格納する
            int nowCUNT = -1;
            float beforetime = 0;
            for (int j = 0; j < strOutputLogLines.Length; j++) {

                if (strOutputLogLines[j].Contains(strCNTWord))
                {
                    nowCUNT++;
                    CUNT.Add(nowCUNT, new List<string>());
                }
                if (strOutputLogLines[j].Contains(strAudioTimeKeyWord))
                {
                    StartTime = float.Parse(strOutputLogLines[j].Substring(nHeaderStrNum + (strAudioTimeKeyWord).Length));
                }
                if (strOutputLogLines[j].Contains(strTimeRugKeyWord))
                {
                    TimeRug = float.Parse(strOutputLogLines[j].Substring(nHeaderStrNum + (strTimeRugKeyWord).Length));
                    // ない場合は0
                }
                //対象のログの行を抽出
                if (strOutputLogLines[j].Contains(strKeyWord + DisplayName))
                {
                    // User-nameの照合
                    if (strOutputLogLines[j].Length > nHeaderStrNum + (strKeyWord + DisplayName).Length)
                    {
                        //記録終わりを検知
                        string strTmpOLL = strOutputLogLines[j].Substring(nHeaderStrNum + (strKeyWord + DisplayName).Length);
                        for (int k = 0; k < strTmpOLL.Length; k++)
                        {
                            if (strTmpOLL[k] == ',')
                            {
                                float currenttime = float.Parse(strTmpOLL.Substring(0, k));
                                if (currenttime < beforetime)
                                {
                                    newLogLines.Add(nTargetCounter);
                                    newTargetLines.Add(j);
                                }
                                beforetime = currenttime;
                                break;
                            }
                        }
                        CUNT[nowCUNT].Add(strOutputLogLines[j]);
                        nTargetCounter++;//目的の行が何行あるか。
                    }
                    else
                    {
                        Debug.LogWarning("Length is not correct");
                    }
                }
            }
            newLogLines.Add(nTargetCounter);
            newTargetLines.Add(strOutputLogLines.Length);
            // Keyframeの生成
            if (nTargetCounter == 0)
            {
                Debug.Log("Not exist Motion Data");
                return;
            }
            for (int i = 0;i< CUNT[targetCUNT].Count();i++)
            {
                CUNT[targetCUNT][i] = CUNT[targetCUNT][i].Substring(nHeaderStrNum + (strKeyWord + DisplayName).Length);
            }
            TimeRug = float.Parse(CUNT[targetCUNT].First().Split(',')[0]);
            if (isfix_x)
            {
                index_x = fix_x;
                Debug.Log(index_x);
            }
            if (isfix_y)
            {
                index_y = fix_y;
                Debug.Log(index_y);
            }
            if (isfix_z)
            {
                index_z = fix_z;
                Debug.Log(index_z);
            }
            string[] strSplitedOutPutLog;

            Keyframe[][] Keyframes = new Keyframe[4 * (HumanTrait.BoneName.Length + 1/*time + hip position*/) - 1/*time*/][];//[要素数]
            for (int j = 0; j < Keyframes.Length; j++)
            {
                Keyframes[j] = new Keyframe[CUNT[targetCUNT].Count()];//[行数]
            }
            int nTargetLineCounter = 0;
            foreach (var i in CUNT[targetCUNT])
            {
                strSplitedOutPutLog = i.Split(',');

                float key_time = float.Parse(strSplitedOutPutLog[0]) - TimeRug + StartTime;
                Vector3 rootScale = animator.transform.localScale;
                Vector3 hippos = new Vector3(float.Parse(strSplitedOutPutLog[1]) + index_x, float.Parse(strSplitedOutPutLog[2]) + index_y, float.Parse(strSplitedOutPutLog[3])+ index_z);
                transform.rotation = Quaternion.identity;//Avatarがrotation(0,0,0)でない可能性があるため
                hippos = Quaternion.Inverse(animator.GetBoneTransform((HumanBodyBones)0).parent.localRotation) * hippos;//armatureがrotation(0,0,0)でない可能性があるため
                hippos = new Vector3(hippos.x / rootScale.x, hippos.y / rootScale.y, hippos.z / rootScale.z); //いる？
                Keyframes[0][nTargetLineCounter] = new Keyframe(key_time, hippos.x);
                Keyframes[1][nTargetLineCounter] = new Keyframe(key_time, hippos.y);
                Keyframes[2][nTargetLineCounter] = new Keyframe(key_time, hippos.z);
                Quaternion[] boneWorldRotation = new Quaternion[HumanTrait.BoneName.Length];
                for (int j = 0; j < HumanTrait.BoneName.Length; j++)
                {
                    boneWorldRotation[j] = new Quaternion(float.Parse(strSplitedOutPutLog[j * 4 + 4]), float.Parse(strSplitedOutPutLog[j * 4 + 5]), float.Parse(strSplitedOutPutLog[j * 4 + 6]), float.Parse(strSplitedOutPutLog[j * 4 + 7]));
                }
                for (int j = 0; j < HumanTrait.BoneName.Length; j++)
                {

                    if (animator.GetBoneTransform((HumanBodyBones)j) == null)
                    {
                        continue;
                    }
                    animator.GetBoneTransform((HumanBodyBones)j).rotation = boneWorldRotation[j];
                }

                for (int j = 0; j < HumanTrait.BoneName.Length; j++)
                {
                    if (animator.GetBoneTransform((HumanBodyBones)j) == null)
                    {
                        continue;
                    }
                    Quaternion localrot = animator.GetBoneTransform((HumanBodyBones)j).localRotation;
                    Keyframes[j * 4 + 3][nTargetLineCounter] = new Keyframe(key_time, localrot.x);
                    Keyframes[j * 4 + 4][nTargetLineCounter] = new Keyframe(key_time, localrot.y);
                    Keyframes[j * 4 + 5][nTargetLineCounter] = new Keyframe(key_time, localrot.z);
                    Keyframes[j * 4 + 6][nTargetLineCounter] = new Keyframe(key_time, localrot.w);
                }
                nTargetLineCounter++;
            }


            //AnimationClipにAnimationCurveを設定
            AnimationClip clip = new AnimationClip();
    #region AnimationCurveの生成
            AnimationCurve[] AnimCurves = new AnimationCurve[Keyframes.Length];

            for (int j = 0; j < AnimCurves.Length; j++)//[行数-1]
            {
                AnimCurves[j] = new AnimationCurve(Keyframes[j]);
            }
            // AnimationCurveの追加
            clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)0)), typeof(Transform), "localPosition.x", AnimCurves[0]);
            clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)0)), typeof(Transform), "localPosition.y", AnimCurves[1]);
            clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)0)), typeof(Transform), "localPosition.z", AnimCurves[2]);
            for (int j = 0; j < (AnimCurves.Length - 3) / 4; j++)//[骨数]
            {
                if (animator.GetBoneTransform((HumanBodyBones)j) == null)
                {
                    continue;
                }
                clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)j)),
                    typeof(Transform), "localRotation.x", AnimCurves[j * 4 + 3]);
                clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)j)),
                    typeof(Transform), "localRotation.y", AnimCurves[j * 4 + 4]);
                clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)j)),
                    typeof(Transform), "localRotation.z", AnimCurves[j * 4 + 5]);
                clip.SetCurve(GetHierarchyPath(animator.GetBoneTransform((HumanBodyBones)j)),
                    typeof(Transform), "localRotation.w", AnimCurves[j * 4 + 6]);
            }
            clip.EnsureQuaternionContinuity();//これをしないとQuaternion補間してくれない
    #endregion

    #region GenericAnimation出力
            string animFolderPath = humrPath + @"/GenericAnimations";
            CreateDirectoryIfNotExist(animFolderPath);
            string displaynameFolderPath = animFolderPath + "/" + DisplayName;
            CreateDirectoryIfNotExist(displaynameFolderPath);
            string animationName = files[index].Substring(files[index].Length - 13).Remove(9) + "_" + targetCUNT.ToString();//回数の番号いれるtodo
            string animPath = displaynameFolderPath + "/" + animationName + ".anim";
            Debug.Log(animPath);

            if (ExportGenericAnimation)
            {
                if (File.Exists(animPath))
                {
                    AssetDatabase.DeleteAsset(animPath);
                    Debug.LogWarning("Same Name Generic Animation is existing. Overwritten!!");
                    foreach (var layer in controller.layers)//アニメーションを消したことにより空のアニメーションステートが出来てたら削除
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            if (state.state.motion == null)
                            {
                                layer.stateMachine.RemoveState(state.state);
                            }
                        }
                    }
                }
                AssetDatabase.CreateAsset(clip, AssetDatabase.GenerateUniqueAssetPath(animPath));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            //アニメーションをアバターのアニメーターに入れる
            controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
    #endregion

    #region FBXとして出力
            string[] str = files[index].Split('\\');
            files[index] = str[7];
            animator.runtimeAnimatorController = controller;
            string exportFolderPath = humrPath + @"/FBXs";
            CreateDirectoryIfNotExist(exportFolderPath);
            string displaynameFBXFolderPath = exportFolderPath + "/" + ValidName(DisplayName);
            CreateDirectoryIfNotExist(displaynameFBXFolderPath);
            UnityEditor.Formats.Fbx.Exporter.ModelExporter.ExportObject(displaynameFBXFolderPath + "/" + files[index], this.gameObject);
    #endregion
        }
        //ファイル名やパスに使えない文字を‗に置換
        string ValidName(string str)
        {
            string strValid = str;
            char[] chInvalid = Path.GetInvalidFileNameChars();

            foreach (char c in chInvalid)
            {
                strValid = strValid.Replace(c, '_');
            }
            return strValid;
        }
        void ControllerSetUp(string humrPath)
        {
            string tmpAniConPath = humrPath + @"/AnimationController";
            if (controller == null)
            {
                CreateDirectoryIfNotExist(tmpAniConPath);
                controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(tmpAniConPath + "/TmpAniCon.controller");
            }
            else if (AssetDatabase.GetAssetPath(controller).Equals(tmpAniConPath + "/TmpAniCon.controller"))
            {
                foreach (var layer in controller.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        layer.stateMachine.RemoveState(state.state);
                    }
                }
            }
            else
            {
                foreach (var layer in controller.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        if (state.state.motion == null)
                        {
                            layer.stateMachine.RemoveState(state.state);
                        }
                    }
                }
            }
        }

        void CreateDirectoryIfNotExist(string path)
        {
            //存在するかどうか判定しなくても良いみたいだが気持ち悪いので
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        string GetHierarchyPath(Transform self)
        {
            string path = self.gameObject.name;
            Transform parent = self.parent;
            while (parent.parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

    }
#endif
}