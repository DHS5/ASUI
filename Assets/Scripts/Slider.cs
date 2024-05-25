using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dhs5.ASUI
{
    public class Slider : Selectable, IDragHandler, IInitializePotentialDragHandler
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
        [SerializeField] private float m_maxValue;

        [SerializeField] private bool m_wholeNumbers;
        [SerializeField] private Direction m_direction;
        [SerializeField] private bool m_warpOnClick;

        [SerializeField] private RectTransform m_fillContainer;
        [SerializeField] private RectTransform m_fillRect;
        [SerializeField] private Image m_fillImage;
        [SerializeField] private RectTransform m_handleContainer;
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

        public void OnDrag(PointerEventData eventData)
        {
            if (CanDrag(eventData))
            {
                UpdateDrag(eventData);
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag(eventData)) return;

            base.OnPointerDown(eventData);

            m_offset = Vector2.zero;
            if (m_handleContainer != null && RectTransformUtility.RectangleContainsScreenPoint(m_handle, eventData.pointerPressRaycast.screenPosition, eventData.enterEventCamera))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_handle, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out var localPoint))
                {
                    m_offset = localPoint;
                }
            }
            else if (WarpOnClick)
            {
                UpdateDrag(eventData);
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

            UpdateRectReferences();
        }

#endif

        #endregion
    }
}
