using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Dhs5.ASUI;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;



#if UNITY_EDITOR
using UnityEditor;
#endif

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

        [Space(10f)]

        [SerializeField] private ToggleGroup m_group;

        public bool IsOn
        {
            get => m_isOn;
            set
            {
                Set(value, true);
            }
        }

        public ToggleGroup Group
        {
            get => m_group;
            set
            {
                UnregisterToGroup();
                m_group = value;
                RegisterToGroup();
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
            RegisterToGroup();
        }
        protected override void OnDisable()
        {
            base.OnDisable();

            UnregisterToGroup();
        }

        #endregion


        #region Behaviour

        private void Click()
        {
            if (!IsActive() || !IsInteractable()) return;

            Clicked?.Invoke(this);
            IsOn = !IsOn;
        }

        public virtual void Set(bool value, bool callback)
        {
            if (m_isOn == value) return;
            if (!value && !CanBeSwitchedOff()) return;

            m_isOn = value;
            NotifyGroup(callback);

            SetGraphicState();
            if (callback) ValueChanged?.Invoke(this, m_isOn);
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

        private void RegisterToGroup()
        {
            if (m_group != null && IsActive())
            {
                m_group.Register(this);
            }
        }
        private void UnregisterToGroup()
        {
            if (m_group != null)
            {
                m_group.Unregister(this);
            }
        }

        private bool CanBeSwitchedOff()
        {
            return m_group == null || !m_group.IsActive() || m_group.CanSwitchOff(this);
        }

        private void NotifyGroup(bool callback)
        {
            if (m_group != null && m_group.IsActive())
                m_group.OnToggleValueChanged(this, m_isOn, callback);
        }

#if UNITY_EDITOR

        private void EnsureGroupState()
        {
            if (m_group != null && m_group.IsActive())
            {
                if (IsActive()) RegisterToGroup();
                else UnregisterToGroup();

                m_group.EnsureValidState();
            }
        }

#endif

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
            EnsureGroupState();
        }

#endif

        #endregion
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(Toggle))]
    public class ToggleEditor : SelectableEditor
    {
        protected Toggle m_toggle;

        SerializedProperty p_isOn;
        SerializedProperty p_onGraphic;
        SerializedProperty p_onGraphicHidingMethod;
        SerializedProperty p_offGraphic;
        SerializedProperty p_offGraphicHidingMethod;
        SerializedProperty p_group;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_toggle = (Toggle)target;

            p_isOn = serializedObject.FindProperty("m_isOn");
            p_onGraphic = serializedObject.FindProperty("m_onGraphic");
            p_onGraphicHidingMethod = serializedObject.FindProperty("m_onGraphicHidingMethod");
            p_offGraphic = serializedObject.FindProperty("m_offGraphic");
            p_offGraphicHidingMethod = serializedObject.FindProperty("m_offGraphicHidingMethod");
            p_group = serializedObject.FindProperty("m_group");
        }

        protected override void OnChildGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Toggle", EditorStyles.boldLabel);

            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth, rect.height), p_isOn.boolValue ? "ON" : "OFF");
            if (GUI.Button(new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height), "Switch"))
            {
                m_toggle.IsOn = !m_toggle.IsOn;
            }

            EditorGUILayout.PropertyField(p_onGraphic);
            EditorGUILayout.PropertyField(p_onGraphicHidingMethod, new GUIContent("Hiding Method"));
            EditorGUILayout.PropertyField(p_offGraphic);
            EditorGUILayout.PropertyField(p_offGraphicHidingMethod, new GUIContent("Hiding Method"));

            EditorGUILayout.PropertyField(p_group);
        }
    }

#endif
}