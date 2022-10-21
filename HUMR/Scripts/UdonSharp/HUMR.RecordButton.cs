
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using UnityEngine.Video;

namespace HUMR
{
    public class RecordButton : UdonSharpBehaviour
    {
        [SerializeField] Text tx;
        [SerializeField] GameObject go;
        [SerializeField] AudioSource AuS;
        [SerializeField] AudioClip Music;
        [Range(0, 1200)][SerializeField] float time;
        int cnt = 0;
        bool isPress = false;
        private void Start()
        {
            AuS.clip = Music;
        }
        void Press()
        {
            if (!isPress)
            {
                go.SetActive(true);
                tx.text = ("Recording");
                isPress = true;
                AuS.time = time;
                AuS.Play();
                Debug.Log("ADTM:" + AuS.time);
                Debug.Log("CUNT:" + cnt);
                var player = Networking.LocalPlayer;
                cnt++;
            }
            else
            {
                go.SetActive(false);
                tx.text = ("Recorded");
                isPress = false;
            }
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown("joystick button 2"))
            {
                Press();
            }
        }
    }
}