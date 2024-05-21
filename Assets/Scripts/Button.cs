using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.ASUI
{
    public class Button : Selectable,
        IPointerClickHandler, ISubmitHandler
    {
        #region Events

        public event Action<Button> Clicked;

        #endregion

        #region Behaviour

        private void Click(bool simulate)
        {
            if (IsActive() && IsInteractable())
            {
                Clicked?.Invoke(this);
                if (simulate)
                {
                    SimulateClick();
                }
            }
        }

        private Tween clickSimulationTween;
        protected virtual void SimulateClick()
        {
            clickSimulationTween.Kill();
            SetCurrentState(State.PRESSED, false);
            DOVirtual.DelayedCall(0.1f, () => 
            {
                SetCurrentState(State.NORMAL, false);
            });
        }

        #endregion

        #region Interfaces

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Click(false);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Click(true);
        }

        #endregion

    }
}
