using UnityEngine;

namespace UnluckSoftware
{
    public class GUIResetEXAMPLE : MonoBehaviour
    {
        public Texture2D aimTexture;

        public void OnGUI()
        {
            GUI.Label(new Rect(20.0f, 20.0f, 350.0f, 18.0f), "Use WASD and mouse to move and shoot");
            if (GUI.Button(new Rect(20.0f, 40.0f, 125.0f, 18.0f), "Reset All"))
                ResetAll();

            // Draw aim crosshair in the center of the screen
            if (aimTexture != null)
            {
                float x = (Screen.width * 0.5f) - (aimTexture.width * 0.5f);
                float y = (Screen.height * 0.5f) - (aimTexture.height * 0.5f);
                GUI.DrawTexture(new Rect(x, y, aimTexture.width, aimTexture.height), aimTexture);
            }
        }

        public void ResetAll()
        {
            FraggedController[] frags = FindObjectsOfType(typeof(FraggedController)) as FraggedController[];
            foreach (FraggedController frag in frags)
            {
                frag.ResetFrags();
            }
        }
    }
}