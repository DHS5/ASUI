using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.ASUI
{
    public static class EventSystemUtility
    {
        //public enum PostEventSelectionState
        //{
        //    NONE = 0,
        //    LEAVE_ON_EVENT_TARGET = 1,
        //    BACK_TO_LAST = 2,
        //}

        public static bool CheckForEventSystem(out EventSystem eventSystem)
        {
            eventSystem = EventSystem.current;
            return eventSystem != null;
        }

        public static void SetSelection(GameObject go)
        {
            if (CheckForEventSystem(out EventSystem eventSystem) && eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(go);
            }
        }
        public static void SetSelection(GameObject go, BaseEventData data)
        {
            if (CheckForEventSystem(out EventSystem eventSystem) && eventSystem.alreadySelecting)
            {
                eventSystem.SetSelectedGameObject(go, data);
            }
        }
        public static bool IsSelection(GameObject go)
        {
            if (CheckForEventSystem(out EventSystem eventSystem))
            {
                return eventSystem.currentSelectedGameObject == go;
            }
            return false;
        }

        //public static void SimulateClick(GameObject go, PostEventSelectionState state)
        //{
        //    if (CheckForEventSystem(out EventSystem eventSystem))
        //    {
        //        GameObject current = eventSystem.currentSelectedGameObject;
        //
        //        ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
        //
        //        DOVirtual.DelayedCall(0.1f, () =>
        //        {
        //            ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
        //
        //            switch (state)
        //            {
        //                case PostEventSelectionState.NONE:
        //                    ExecuteEvents.Execute(null, new PointerEventData(EventSystem.current) { selectedObject = null }, ExecuteEvents.selectHandler); break;
        //                case PostEventSelectionState.LEAVE_ON_EVENT_TARGET:
        //                    break;
        //                case PostEventSelectionState.BACK_TO_LAST:
        //                    ExecuteEvents.Execute(current, new PointerEventData(EventSystem.current) { selectedObject = current }, ExecuteEvents.selectHandler); break;
        //            }
        //        });
        //    }
        //}

        public static void SimulateHoverEnter(GameObject go)
        {
            if (CheckForEventSystem(out EventSystem eventSystem))
            {
                ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            }
        }
        public static void SimulateHoverExit(GameObject go)
        {
            if (CheckForEventSystem(out EventSystem eventSystem))
            {
                ExecuteEvents.Execute(go, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
            }
        }
    }
}
