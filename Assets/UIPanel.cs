using UnityEngine;

namespace UGUI
{
    public class UIPanel : MonoBehaviour
    {
        [Range(0, 1)]
        public float alpha = 1f;

        private float alphaPre = 1f;

        void Start()
        {
            CanvasRenderer[] canvasRenderers = GetComponentsInChildren<CanvasRenderer>();
            foreach (CanvasRenderer o in canvasRenderers)
            {
                Debug.Log("o = " + o);
                Debug.Log("o = " + o.gameObject.name);
            }
        }


        void FixedUpdate()
        {
            if (alphaPre != alpha)
            {
                alphaPre = alpha;
          
                CanvasRenderer[] canvasRenderers = GetComponentsInChildren<CanvasRenderer>();
                foreach (CanvasRenderer o in canvasRenderers)
                {
                    o.SetAlpha(alpha);
                }
            }
        }

    }

}

