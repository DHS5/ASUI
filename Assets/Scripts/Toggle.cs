using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dhs5.ASUI
{
    public class Toggle : Selectable, IPointerClickHandler, ISubmitHandler
    {
        #region Enums

        public enum HidingMethod
        {
            ALPHA_0 = 0,
            DISABLE = 1,
            SET_INACTIVE = 2,
        }

        #endregion

        #region Global Members

        [SerializeField] private bool m_isOn;

        [Space(10f)]

        [SerializeField] private UnityEngine.UI.Graphic m_onGraphic;
        [SerializeField] private HidingMethod m_onGraphicHidingMethod;
        [Space(5f)]
        [SerializeField] private UnityEngine.UI.Graphic m_offGraphic;
        [SerializeField] private HidingMethod m_offGraphicHidingMethod;

        public bool IsOn
        {
            get => m_isOn;
            set
            {
                Set(value, true);
            }
        }

        #endregion

        #region Events

        public event Action<Toggle> Clicked;
        public event Action<Toggle, bool> ValueChanged;

        #endregion

        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            SetGraphicState();
        }

        #endregion


        #region Behaviour

        private void Click()
        {
            if (!IsActive() || !IsInteractable()) return;

            Clicked?.Invoke(this);
            IsOn = !IsOn;
        }

        public virtual void Set(bool value, bool notify)
        {
            if (m_isOn == value) return;

            m_isOn = value;

            SetGraphicState();
            if (notify) ValueChanged?.Invoke(this, m_isOn);
        }

        #endregion

        #region Graphic Behaviour

#if UNITY_EDITOR
        private void ClearGraphicState()
        {
            if (m_onGraphic != null)
            {
                m_onGraphic.canvasRenderer.SetAlpha(1f);
                m_onGraphic.enabled = true;
                m_onGraphic.gameObject.SetActive(true);
            }
            if (m_offGraphic != null)
            {
                m_offGraphic.canvasRenderer.SetAlpha(1f);
                m_offGraphic.enabled = true;
                m_offGraphic.gameObject.SetActive(true);
            }
        }
#endif

        private void SetGraphicState()
        {
            if (m_onGraphic != null)
            {
                switch (m_onGraphicHidingMethod)
                {
                    case HidingMethod.ALPHA_0:
                        m_onGraphic.canvasRenderer.SetAlpha(m_isOn ? 1f : 0f); break;
                    case HidingMethod.DISABLE:
                        m_onGraphic.enabled = m_isOn; break;
                    case HidingMethod.SET_INACTIVE:
                        m_onGraphic.gameObject.SetActive(m_isOn); break;
                }
            }
            if (m_offGraphic != null)
            {
                switch (m_offGraphicHidingMethod)
                {
                    case HidingMethod.ALPHA_0:
                        m_offGraphic.canvasRenderer.SetAlpha(m_isOn ? 0f : 1f); break;
                    case HidingMethod.DISABLE:
                        m_offGraphic.enabled = !m_isOn; break;
                    case HidingMethod.SET_INACTIVE:
                        m_offGraphic.gameObject.SetActive(!m_isOn); break;
                }
            }
        }

        #endregion

        #region Group

        #endregion

        #region Interfaces

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            
            Click();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Click();
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            ClearGraphicState();
            SetGraphicState();
        }

#endif

        #endregion
    }
}
