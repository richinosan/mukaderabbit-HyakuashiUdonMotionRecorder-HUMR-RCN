
/*******
 * OutputLogLoaderEditor.cs
 * 
 * Editor拡張。Editorフォルダの下に配置すること
 * 
 * OutputlogLoader.csをアタッチした際のInspectorに表示される項目を拡張している
 * 
 * FBXExporterの有無を確認し、存在するOutputLogをプルダウンに表示する
 * "LoadLogToExportAnim"のボタンを押すとOutputLog内の処理が実行される
 * 
 * *****/
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.EventSystems;

namespace HUMR
{
    [CustomEditor(typeof(OutputLogLoader))]
    public class OutputLogLoaderEditor : Editor
    {
        string path;
        bool foldout;

        public override void OnInspectorGUI()
        {
            string manifest = File.ReadAllText(@"Packages\manifest.json");
            if (!manifest.Contains("com.unity.formats.fbx"))
            {
                EditorGUILayout.HelpBox("FBX Exporterのインストールが必要です\n\nUnity上部の[Window]タブを開き、[Package Manager]をクリック\n開かれたPackagesタブの上部にある[Advanced]をクリック➞[Show preview packages]を選択\nPackagesタブ左のリストに[FBX Exporter]が出てくるので選択、右上の[Install]をクリックしてください", MessageType.Warning, true);
                return;
            }

            //元のInspector部分を表示
            base.OnInspectorGUI();

            //targetを変換して対象を取得
            OutputLogLoader targetScript = target as OutputLogLoader;

            EditorGUI.BeginChangeCheck();

            foldout = EditorGUILayout.Foldout(foldout, "Advanced : CustomOutputLogPath");
            if (foldout)
            {
                EditorGUI.indentLevel++;
                path = EditorGUILayout.TextField("OutputLogPath", path);
                EditorGUI.indentLevel--;
            }
            else
            {
                // Your Motion Data Path
                path = System.Environment.GetEnvironmentVariable("USERPROFILE");
                path += @"\AppData\LocalLow\VRChat\VRChat";
            }
            targetScript.OutputLogPath = path;

            string[] files = Directory.GetFiles(path, "*.txt");
            string[] str;
            for (int i = 0; i < files.Length; i++)
            {
                /*
                 *  Example:
                 *      C:\Users\YourUser\AppData\LocalLow\VRChat\VRChat\*.txt
                 *      split('\\') index = 7
                 */
                
                str = files[i].Split('\\');
                files[i] = str[7];
            }


            // ラベルの作成
            string label = "LoadOutputLog";
            // 初期値として表示する項目のインデックス番号
            int selectedIndex = targetScript.index;
            // プルダウンメニューの作成
            int index = files.Length > 0 ? EditorGUILayout.Popup(label, selectedIndex, files)
                : -1;

            if (EditorGUI.EndChangeCheck())
            {// 操作を Undo に登録
             // インデックス番号を登録
                targetScript.index = index;
            }

            GUILayout.Space(15);

            //PrivateMethodを実行する用のボタン
            if (GUILayout.Button("LoadLogToExportAnim"))
            {
                /*
                 *  Example:
                 *      Target Script Name = "Character Name"
                 *      Target File = "Motion Take Number.txt"
                 *      Set Animation Name = "Motion Character Name"
                 */
                SettingHUMR.Anim = string.Format("{0} {1}",files[index].Split('.')[0].Split(' ')[0] , targetScript.getname());
                ExecuteEvents.Execute<OutputLogLoaderinterface>(
                target: targetScript.gameObject,
                eventData: null,
                functor: (recieveTarget, y) => recieveTarget.LoadLogToExportAnim());
            }

        }
    }
}
# enfif