using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.ASUI
{
    public class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler, IEndDragHandler
    {
        #region Axis

        public enum Axis
        {
            HORIZONTAL = 0,
            VERTICAL = 1,
        }

        public enum Direction
        {
            LEFT_TO_RIGHT,
            RIGHT_TO_LEFT,
            BOTTOM_TO_TOP,
            TOP_TO_BOTTOM
        }

        #endregion

        #region Global Members

        [SerializeField] private float m_value;
        [SerializeField] private float m_minValue;
        [SerializeField] private float m_maxValue = 1f;

        [SerializeField] private bool m_wholeNumbers;
        [SerializeField] private Direction m_direction;
        [SerializeField] private bool m_warpOnClick;

        [SerializeField, HideInInspector] private RectTransform m_fillContainer;
        [SerializeField, HideInInspector] private RectTransform m_fillRect;
        [SerializeField] private Image m_fillImage;
        [SerializeField, HideInInspector] private RectTransform m_handleContainer;
        [SerializeField] private RectTransform m_handle;

        private DrivenRectTransformTracker m_tracker;

        #endregion

        #region Accessors

        public float Value
        {
            get => m_wholeNumbers ? Mathf.RoundToInt(m_value) : m_value;
            set
            {
                Set(value, true);
            }
        }
        public float NormalizedValue
        {
            get
            {
                if (Mathf.Approximately(MinValue, MaxValue)) return 0f;

                return Mathf.InverseLerp(MinValue, MaxValue, Value);
            }
            set
            {
                Value = Mathf.Lerp(MinValue, MaxValue, value);
            }
        }

        public virtual float MinValue
        {
            get => m_minValue;
            set
            {
                if (m_minValue != value)
                {
                    m_minValue = value;
                }
            }
        }
        public virtual float MaxValue
        {
            get => m_maxValue;
            set
            {
                if (m_maxValue != value)
                {
                    m_maxValue = value;
                }
            }
        }

        protected float StepSize
        {
            get
            {
                if (!WholeNumbers) return (MaxValue - MinValue) * 0.1f;

                return 1f;
            }
        }

        public bool WholeNumbers
        {
            get => m_wholeNumbers;
            set
            {
                if (m_wholeNumbers != value)
                {
                    m_wholeNumbers = value;
                }
            }
        }
        public bool WarpOnClick
        {
            get => m_warpOnClick;
            set
            {
                if (m_warpOnClick != value)
                {
                    m_warpOnClick = value;
                }
            }
        }

        public Direction Dir
        {
            get => m_direction;
            set
            {
                if (m_direction != value)
                {
                    m_direction = value;
                }
            }
        }
        public Axis GetAxis() => Dir == Direction.LEFT_TO_RIGHT || Dir == Direction.RIGHT_TO_LEFT ? Axis.HORIZONTAL : Axis.VERTICAL;
        public bool Reversed => Dir == Direction.RIGHT_TO_LEFT || Dir == Direction.TOP_TO_BOTTOM;

        public Image FillImage
        {
            get => m_fillImage;
            set
            {
                if (m_fillImage != value)
                {
                    m_fillImage = value;
                    UpdateRectReferences();
                    UpdateVisuals();
                }
            }
        }
        public RectTransform Handle
        {
            get => m_handle;
            set
            {
                if (m_handle != value)
                {
                    m_handle = value;
                    UpdateRectReferences();
                    UpdateVisuals();
                }
            }
        }

        #endregion

        #region Events

        public event Action<Slider, float> ValueChanged;

        #endregion


        #region Core Behaviour

        protected override void OnEnable()
        {
            base.OnEnable();

            UpdateRectReferences();
        }
        protected override void OnDisable()
        {
            m_tracker.Clear();
            base.OnDisable();
        }

        #endregion

        #region Behaviour

        public void Set(float value, bool callback)
        {
            value = ClampedValue(value);
            if (value != m_value)
            {
                m_value = value;
                UpdateVisuals();
                if (callback) ValueChanged?.Invoke(this, value);
            }
        }

        #endregion


        #region Visuals

        protected virtual void UpdateVisuals()
        {
            if (!Application.isPlaying)
            {
                UpdateRectReferences();
            }

            int axis = (int)GetAxis();
            bool reversed = Reversed;

            m_tracker.Clear();
            if (m_fillImage != null)
            {
                if (m_fillImage.type == Image.Type.Filled)
                {
                    m_fillImage.fillAmount = NormalizedValue;
                }
                else if (m_fillContainer != null)
                {
                    m_tracker.Add(this, m_fillImage.rectTransform, DrivenTransformProperties.Anchors);
                    Vector2 anchorMin = Vector2.zero;
                    Vector2 anchorMax = Vector2.one;
                    
                    if (reversed)
                    {
                        anchorMin[axis] = 1f - NormalizedValue;
                    }
                    else
                    {
                        anchorMax[axis] = NormalizedValue;
                    }

                    m_fillRect.anchorMin = anchorMin;
                    m_fillRect.anchorMax = anchorMax;
                }
            }

            if (m_handle != null && m_handleContainer != null)
            {
                m_tracker.Add(this, m_handle, DrivenTransformProperties.Anchors);

                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                anchorMin[axis] = anchorMax[axis] = (reversed ? (1f - NormalizedValue) : NormalizedValue);

                m_handle.anchorMin = anchorMin;
                m_handle.anchorMax = anchorMax;
            }
        }

        #endregion

        #region Drag Behaviour

        private void UpdateDrag(PointerEventData eventData)
        {
            RectTransform rectTransform = m_handleContainer ?? m_fillContainer;
            int axis = (int)GetAxis();
            if (rectTransform != null && rectTransform.rect.size[axis] > 0f)
            {
                Vector2 position = eventData.position;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, position, eventData.pressEventCamera, out var localPoint))
                {
                    localPoint -= rectTransform.rect.position;
                    float num = Mathf.Clamp01((localPoint - m_offset)[axis] / rectTransform.rect.size[axis]);
                    NormalizedValue = (Reversed ? (1f - num) : num);
                }
            }
        }

        #endregion

        #region Interfaces

        private Vector2 m_offset;
        private bool m_dragIsValid;

        public void OnDrag(PointerEventData eventData)
        {
            if (CanDrag(eventData) && m_dragIsValid)
            {
                UpdateDrag(eventData);
            }
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            m_dragIsValid = false;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag(eventData)) return;

            base.OnPointerDown(eventData);

            m_offset = Vector2.zero;
            if (m_handleContainer != null && RectTransformUtility.RectangleContainsScreenPoint(m_handle, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                m_dragIsValid = true;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_handle, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localPoint))
                {
                    m_offset = localPoint;
                }
            }
            else if (WarpOnClick)
            {
                UpdateDrag(eventData);
                m_dragIsValid = true;
            }
            else
            {
                m_dragIsValid = false;
            }
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        #endregion


        #region Utility

        protected virtual bool CanDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        protected float ClampedValue(float value)
        {
            float clamped = Mathf.Clamp(value, MinValue, MaxValue);

            if (WholeNumbers) return Mathf.RoundToInt(clamped);

            return clamped;
        }

        protected void UpdateRectReferences()
        {
            if (m_fillImage != null && m_fillImage.transform.parent != null)
            {
                m_fillRect = m_fillImage.rectTransform;
                m_fillContainer = (RectTransform)m_fillImage.transform.parent;
            }
            if (m_handle != null)
            {
                m_handleContainer = (RectTransform)m_handle.transform.parent;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateVisuals();
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(Slider))]
    public class SliderEditor : SelectableEditor
    {
        protected SerializedProperty p_value;
        protected SerializedProperty p_minValue;
        protected SerializedProperty p_maxValue;

        protected SerializedProperty p_wholeNumbers;
        protected SerializedProperty p_direction;
        protected SerializedProperty p_warpOnClick;

        protected SerializedProperty p_fillImage;
        protected SerializedProperty p_handle;


        protected override void OnEnable()
        {
            base.OnEnable();

            p_value = serializedObject.FindProperty("m_value");
            p_minValue = serializedObject.FindProperty("m_minValue");
            p_maxValue = serializedObject.FindProperty("m_maxValue");

            p_wholeNumbers = serializedObject.FindProperty("m_wholeNumbers");
            p_direction = serializedObject.FindProperty("m_direction");
            p_warpOnClick = serializedObject.FindProperty("m_warpOnClick");

            p_fillImage = serializedObject.FindProperty("m_fillImage");
            p_handle = serializedObject.FindProperty("m_handle");
        }

        protected override void OnChildGUI()
        {
            EditorGUILayout.Space(15f);

            EditorGUILayout.LabelField("Slider", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(p_wholeNumbers);
            EditorGUILayout.PropertyField(p_direction);
            EditorGUILayout.PropertyField(p_warpOnClick);

            EditorGUILayout.Space(10f);

            bool wholeNumbers = p_wholeNumbers.boolValue;

            if (wholeNumbers)
            {
                p_value.floatValue = EditorGUILayout.IntSlider("Value", (int)p_value.floatValue, (int)p_minValue.floatValue, (int)p_maxValue.floatValue);
                EditorGUILayout.PropertyField(p_minValue);
                p_minValue.floatValue = Mathf.RoundToInt(p_minValue.floatValue);
                EditorGUILayout.PropertyField(p_maxValue);
                p_maxValue.floatValue = Mathf.RoundToInt(Mathf.Max(p_maxValue.floatValue, p_minValue.floatValue + 1f));
            }
            else
            {
                EditorGUILayout.Slider(p_value, p_minValue.floatValue, p_maxValue.floatValue);
                EditorGUILayout.PropertyField(p_minValue);
                EditorGUILayout.PropertyField(p_maxValue);
                p_maxValue.floatValue = Mathf.Max(p_maxValue.floatValue, p_minValue.floatValue + 0.01f);
            }

            EditorGUILayout.Space(10f);

            EditorGUILayout.PropertyField(p_fillImage);
            EditorGUILayout.PropertyField(p_handle);
        }
    }

#endif

    #endregion
}
