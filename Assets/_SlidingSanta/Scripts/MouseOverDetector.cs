using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

namespace SgLib
{
    public class MouseOverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsMouseOver { get; private set; }

        public void OnPointerEnter(PointerEventData eventData)
        {
            IsMouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            IsMouseOver = false;
        }
    }
}

