using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SimplestarGame
{
    public class WebGLUtil : MonoBehaviour
    {
        byte[] data;

        public byte[] GetData() { return this.data; }

        public IEnumerator ReadFile(string filePath)
        {
            this.data = null;
            UnityWebRequest webRequest = UnityWebRequest.Get(filePath);
            yield return webRequest.SendWebRequest();
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }
            this.data = webRequest.downloadHandler.data;
            yield return null;
        }
    }
}
