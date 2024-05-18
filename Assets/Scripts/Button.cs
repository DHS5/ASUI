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
        #region Global Members

        [SerializeField] private bool m_selectAfterClick = false;

        #endregion

        #region Events

        public event Action<Button> Clicked;

        #endregion

        #region Behaviour

        private bool Click(bool simulate)
        {
            if (IsActive() && IsInteractable())
            {
                Clicked?.Invoke(this);
                if (simulate)
                {
                    if (m_selectAfterClick)
                        SimulateClick(Select);
                    else
                        SimulateClick();
                }
                else if (m_selectAfterClick)
                {
                    Select();
                }

                return true;
            }
            return false;
        }

        private Tween clickSimulationTween;
        protected virtual void SimulateClick(Action onComplete = null)
        {
            clickSimulationTween.Kill();
            SetCurrentState(State.PRESSED, false);
            DOVirtual.DelayedCall(0.1f, () => 
            {
                SetCurrentState(State.NORMAL, false);
                onComplete?.Invoke();
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
