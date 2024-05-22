using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.ASUI
{
    public class ToggleGroup : UIBehaviour
    {
        #region Global Members

        [SerializeField] private bool m_allowSwitchOff;

        [SerializeField] private Toggle m_activeToggle;

        [SerializeField] protected List<Toggle> m_toggles = new List<Toggle>();


        public bool AllowSwitchOff
        {
            get => m_allowSwitchOff;
            //set => m_allowSwitchOff = value;
        }

        public Toggle ActiveToggle
        {
            get => m_activeToggle;
        }
        protected void SetActiveToggle(Toggle toggle, bool callback)
        {
            if (m_activeToggle == toggle && toggle.IsOn) return;

            Toggle former = m_activeToggle;

            m_activeToggle = toggle;

            if (m_activeToggle != null)
                m_activeToggle.Set(true, callback);

            if (former != null && former.Group == this)
                former.Set(false, callback);

            ActiveToggleChanged?.Invoke(m_activeToggle);
        }

        #endregion

        #region Events

        public event Action<Toggle> ActiveToggleChanged;

        #endregion

        #region Core Behaviour

        protected override void Start()
        {
            base.Start();

            EnsureValidState();
        }

        #endregion


        #region Registration

        public void Register(Toggle toggle)
        {
            if (!m_toggles.Contains(toggle))
            {
                m_toggles.Add(toggle);
                EnsureValidState();
            }
        }
        public void Unregister(Toggle toggle)
        {
            if (m_toggles.Contains(toggle))
            {
                m_toggles.Remove(toggle);
                EnsureValidState();
            }
        }

        #endregion

        #region Callbacks

        internal void OnToggleValueChanged(Toggle toggle, bool value, bool callback)
        {
            Debug.Log(toggle + " value changed to " + value);
            if (value) OnToggleTurnedOn(toggle, callback);
            else OnToggleTurnedOff(toggle);
        }

        protected virtual void OnToggleTurnedOn(Toggle toggle, bool callback)
        {
            Debug.Log(toggle + " turned on");
            SetActiveToggle(toggle, callback);
        }
        protected virtual void OnToggleTurnedOff(Toggle toggle)
        {
            Debug.Log(toggle + " turned off");
            if (ActiveToggle == toggle)
                SetActiveToggle(null, false);
        }

        #endregion

        #region State

        public void EnsureValidState()
        {
            if (m_toggles.Count <= 0) return;

            IEnumerable<Toggle> activeToggles = ActiveToggles();
            int activeCount = activeToggles.Count();

            if (activeCount == 0 && !AllowSwitchOff)
            {
                SetActiveToggle(m_toggles[0], true);
                return;
            }

            if (ActiveToggle == null)
            {
                SetActiveToggle(activeToggles.First(), true);
            }

            if (activeCount > 1)
            {
                foreach (var t in activeToggles)
                {
                    if (t != ActiveToggle)
                        t.IsOn = false;
                }
            }
        }

        private IEnumerable<Toggle> ActiveToggles()
        {
            return m_toggles.Where((Toggle x) => x.IsOn);
        }

        public bool CanSwitchOff(Toggle toggle)
        {
            return AllowSwitchOff || ActiveToggle != toggle;
        }

        #endregion

        #region Actions

        /// <summary>
        /// If AllowSwitchOff, switch all toggles off
        /// Else, switch first toggle on and every others off
        /// </summary>
        /// <param name="notify"></param>
        public void ResetGroup(bool notify = true)
        {
            for (int i = 0; i < m_toggles.Count; i++)
            {
                m_toggles[i].Set(i == 0 && !AllowSwitchOff, notify);
            }
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        public void CleanToggleList()
        {
            m_toggles = m_toggles.Where(x => x != null && x.Group == this).ToList();
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            CleanToggleList();
            EnsureValidState();
        }

#endif

        #endregion
    }

#if UNITY_EDITOR

    
    [CustomEditor(typeof(ToggleGroup))]
    public class ToggleGroupEditor : Editor
    {
        protected ToggleGroup m_toggleGroup;

        SerializedProperty p_allowSwitchOff;
        SerializedProperty p_activeToggle;
        SerializedProperty p_toggles;

        protected virtual void OnEnable()
        {
            m_toggleGroup = (ToggleGroup)target;

            p_allowSwitchOff = serializedObject.FindProperty("m_allowSwitchOff");
            p_activeToggle = serializedObject.FindProperty("m_activeToggle");
            p_toggles = serializedObject.FindProperty("m_toggles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Ensure State"))
            {
                m_toggleGroup.CleanToggleList();
                m_toggleGroup.EnsureValidState();
            }            

            EditorGUILayout.Space(5f);

            EditorGUILayout.PropertyField(p_allowSwitchOff);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_activeToggle);
            EditorGUILayout.PropertyField(p_toggles);
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}