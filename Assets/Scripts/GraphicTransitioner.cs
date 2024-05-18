using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using State = Dhs5.ASUI.Selectable.State;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.ASUI
{
    public class GraphicTransitioner : MonoBehaviour
    {
        #region Global Members

        [SerializeField] private State m_currentState;
        [SerializeField] private List<GraphicTransition> m_transitions;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            DoForEach(g => g.OnEnable());
        }
        private void OnDisable()
        {
            DoForEach(g => g.OnDisable());
        }

        #endregion

        #region Behaviour

        public void DoStateTransition(State state, bool instant)
        {
            m_currentState = state;
            DoForEach(g => g.DoStateTransition(state, instant));
        }

        public void ClearTransitions()
        {
            m_currentState = State.NORMAL;
            DoForEach(g => g.ClearTransitions());
        }

        #endregion


        #region Utility

        private void DoForEach(Action<GraphicTransition> action)
        {
            if (m_transitions != null && m_transitions.Count > 0)
            {
                foreach (var transition in m_transitions)
                {
                    action?.Invoke(transition);
                }
            }
        }

        #endregion


        #region Editor

#if UNITY_EDITOR

        private void OnValidate()
        {
            ClearTransitions();
            DoStateTransition(m_currentState, true);
        }

#endif
        #endregion
    }

    [Serializable]
    public class GraphicTransition
    {
        #region Structs

        [Serializable]
        public struct GraphicState<T>
        {
            public T initialValue;
            public T[] states;

            [Min(0f)] public float duration;

            public T this[State state]
            {
                get
                {
                    if (states == null) return initialValue;

                    int index = (int)state;
                    if (index >= states.Length) return initialValue;

                    return states[index];
                }
            }
        }

        #endregion

        #region Enums

        [Flags]
        public enum RectTransition
        {
            SCALE = 1 << 0,
            POSITION = 1 << 1,
        }
        [Flags]
        public enum ImageTransition
        {
            SCALE = 1 << 0,
            POSITION = 1 << 1,
            COLOR = 1 << 2,
            SPRITE = 1 << 3,
        }
        [Flags]
        public enum TextTransition
        {
            SCALE = 1 << 0,
            POSITION = 1 << 1,
            COLOR = 1 << 2,
            SIZE = 1 << 3,
            STYLE = 1 << 4,
        }

        #endregion

        #region Global Members

        [SerializeField] private GameObject obj;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image image;
        [SerializeField] private TextMeshProUGUI text;

        [SerializeField] private RectTransition rectTransition;
        [SerializeField] private ImageTransition imageTransition;
        [SerializeField] private TextTransition textTransition;

        [SerializeField] private GraphicState<Vector2> scaleState;
        [SerializeField] private GraphicState<Vector2> positionState;
        [SerializeField] private GraphicState<Color> colorState;
        [SerializeField] private GraphicState<Sprite> spriteState;
        [SerializeField] private GraphicState<float> sizeState;
        [SerializeField] private GraphicState<FontStyles> styleState;

        #endregion

        #region Core Behaviour

        internal void OnEnable()
        {
            
        }
        internal void OnDisable()
        {
            scaleTween.Kill();
            positionTween.Kill();
            colorTween.Kill();
            sizeTween.Kill();
        }

        #endregion

        #region Main Transition Methods

        public void DoStateTransition(State state, bool instant)
        {
            if (obj == null || !obj.activeInHierarchy) return;

            if (rectTransform != null) DoRectTransition(state, instant);
            else if (image != null) DoImageTransition(state, instant);
            else if (text != null) DoTextTransition(state, instant);
        }

        public void ClearTransitions()
        {
            scaleTween.Kill();
            positionTween.Kill();
            colorTween.Kill();
            sizeTween.Kill();

            if (rectTransform != null)
            {
                rectTransform.localScale = scaleState.initialValue;
                rectTransform.anchoredPosition = scaleState.initialValue;
            }
            else if (image != null)
            {
                image.rectTransform.localScale = scaleState.initialValue;
                image.rectTransform.anchoredPosition = scaleState.initialValue;
                image.color = colorState.initialValue;
                image.sprite = spriteState.initialValue;
            }
            else if (text != null)
            {
                text.rectTransform.localScale = scaleState.initialValue;
                text.rectTransform.anchoredPosition = scaleState.initialValue;
                text.color = colorState.initialValue;
                text.fontSize = sizeState.initialValue;
                text.fontStyle = styleState.initialValue;
            }
        }

        #endregion

        #region Component Transition Methods

        private void DoRectTransition(State state, bool instant)
        {
            if ((rectTransition & RectTransition.SCALE) != 0) DoScaleTransition(rectTransform, state, instant);
            if ((rectTransition & RectTransition.POSITION) != 0) DoPositionTransition(rectTransform, state, instant);
        }
        private void DoImageTransition(State state, bool instant)
        {
            if ((imageTransition & ImageTransition.SCALE) != 0) DoScaleTransition(image.rectTransform, state, instant);
            if ((imageTransition & ImageTransition.POSITION) != 0) DoPositionTransition(image.rectTransform, state, instant);
            if ((imageTransition & ImageTransition.COLOR) != 0) DoColorTransition(image, state, instant);
            if ((imageTransition & ImageTransition.SPRITE) != 0) DoSpriteTransition(image, state);
        }
        private void DoTextTransition(State state, bool instant)
        {
            if ((textTransition & TextTransition.SCALE) != 0) DoScaleTransition(text.rectTransform, state, instant);
            if ((textTransition & TextTransition.POSITION) != 0) DoPositionTransition(text.rectTransform, state, instant);
            if ((textTransition & TextTransition.COLOR) != 0) DoColorTransition(text, state, instant);
            if ((textTransition & TextTransition.SIZE) != 0) DoSizeTransition(text, state, instant);
            if ((textTransition & TextTransition.STYLE) != 0) DoStyleTransition(text, state);
        }

        #endregion

        #region Transition Precise Methods

        // --- SCALE ---
        private Tween scaleTween;
        private void DoScaleTransition(RectTransform target, State state, bool instant)
        {
            scaleTween.Kill();
            if (instant) target.localScale = scaleState[state];
            else scaleTween = target.DOScale(scaleState[state], scaleState.duration);
        }

        // --- POSITION ---
        private Tween positionTween;
        private void DoPositionTransition(RectTransform target, State state, bool instant)
        {
            positionTween.Kill();
            if (instant) target.anchoredPosition = positionState[state];
            else positionTween = target.DOAnchorPos(positionState[state], positionState.duration);
        }

        // --- COLOR ---
        private Tween colorTween;
        private void DoColorTransition(Graphic target, State state, bool instant)
        {
            colorTween.Kill();
            if (instant) target.color = colorState[state];
            colorTween = target.DOColor(colorState[state], colorState.duration);
        }

        // --- SPRITE ---
        private void DoSpriteTransition(Image target, State state)
        {
            target.overrideSprite = spriteState[state];
        }

        // --- SIZE ---
        private Tween sizeTween;
        private void DoSizeTransition(TextMeshProUGUI target, State state, bool instant)
        {
            sizeTween.Kill();
            if (instant) target.fontSize = sizeState[state];
            sizeTween = DOTween.To(() => target.fontSize, x => target.fontSize = x, sizeState[state], sizeState.duration);
        }
        
        // --- SIZE ---
        private void DoStyleTransition(TextMeshProUGUI target, State state)
        {
            target.fontStyle = styleState[state];
        }


        #endregion
    }



    #region Editor

#if UNITY_EDITOR

    #region Graphic State Drawer

    [CustomPropertyDrawer(typeof(GraphicTransition.GraphicState<>))]
    public class GraphicStateDrawer : PropertyDrawer
    {
        SerializedProperty p_states;
        SerializedProperty p_initialValue;

        static GUIContent[] contents = new GUIContent[]
        {
            new GUIContent(((State)0).ToString()),
            new GUIContent(((State)1).ToString()),
            new GUIContent(((State)2).ToString()),
            new GUIContent(((State)3).ToString()),
            new GUIContent(((State)4).ToString()),
            new GUIContent(((State)5).ToString()),
            new GUIContent(((State)6).ToString()),
        };

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_states = property.FindPropertyRelative("states");
            p_initialValue = property.FindPropertyRelative("initialValue");

            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, 18f);

            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
            rect.y += 20f;

            if (property.isExpanded)
            {
                if (p_states.arraySize != Selectable.STATE_COUNT)
                {
                    p_states.arraySize = Selectable.STATE_COUNT;
                    InitArray(p_states, p_initialValue);
                }

                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(rect, p_initialValue, true);
                EditorGUI.EndDisabledGroup();
                rect.y += 20f;

                for (int i = 0; i < p_states.arraySize; i++)
                {
                    EditorGUI.PropertyField(rect, p_states.GetArrayElementAtIndex(i), contents[i], true);
                    rect.y += 20f;
                }
                rect.y += 10f;

                EditorGUI.PropertyField(rect, property.FindPropertyRelative("duration"));
            }

            EditorGUI.EndProperty();
        }

        #region Height

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? ExpandedHeight() : 20f;
        }

        protected float ExpandedHeight() => 20f + Selectable.STATE_COUNT * 20f + 30f + 20f;

        #endregion

        #region Init

        private void InitArray(SerializedProperty property, SerializedProperty initialValueProperty)
        {
            switch (property.GetArrayElementAtIndex(0).propertyType)
            {
                case SerializedPropertyType.Color:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).colorValue = initialValueProperty.colorValue;
                    }
                    break;
                case SerializedPropertyType.Float:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).floatValue = initialValueProperty.floatValue;
                    }
                    break;
                case SerializedPropertyType.Vector2:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).vector2Value = initialValueProperty.vector2Value;
                    }
                    break;
                case SerializedPropertyType.Generic:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).boxedValue = initialValueProperty.boxedValue;
                    }
                    break;
                case SerializedPropertyType.Enum:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).enumValueIndex = initialValueProperty.enumValueIndex;
                    }
                    break;
                case SerializedPropertyType.ObjectReference:
                    for (int i = 0; i < property.arraySize; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = initialValueProperty.objectReferenceValue;
                    }
                    break;
            }
        }

        #endregion
    }

    #endregion

    #region Graphic Transition Drawer

    [CustomPropertyDrawer(typeof(GraphicTransition))]
    public class GraphicTransitionDrawer : PropertyDrawer
    {
        #region Global Members

        SerializedProperty p_obj;
        SerializedProperty p_rectTransform;
        SerializedProperty p_image;
        SerializedProperty p_text;

        SerializedProperty p_rectTransition;
        SerializedProperty p_imageTransition;
        SerializedProperty p_textTransition;

        SerializedProperty p_scaleState;
        SerializedProperty p_positionState;
        SerializedProperty p_colorState;
        SerializedProperty p_spriteState;
        SerializedProperty p_sizeState;
        SerializedProperty p_styleState;

        GraphicTransition.RectTransition RectTransition => (GraphicTransition.RectTransition)p_rectTransition.enumValueFlag;
        GraphicTransition.ImageTransition ImageTransition => (GraphicTransition.ImageTransition)p_imageTransition.enumValueFlag;
        GraphicTransition.TextTransition TextTransition => (GraphicTransition.TextTransition)p_textTransition.enumValueFlag;

        #endregion

        #region GUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            p_obj = property.FindPropertyRelative("obj");
            
            EditorGUI.BeginProperty(position, label, property);

            Rect rect = new Rect(position.x, position.y, position.width, 18f);

            if (p_obj.objectReferenceValue != null) property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label, true);
            EditorGUI.BeginChangeCheck();
            bool newObj = false;
            EditorGUI.PropertyField(rect, p_obj, new GUIContent(" "), true);
            if (EditorGUI.EndChangeCheck())
            {
                ClearAll(property);
                newObj = true;
                property.isExpanded = true;
            }
            rect.y += 20f;

            UnityEngine.Object obj = p_obj.objectReferenceValue;
            if (obj == null)
            {
                property.isExpanded = false;
            }
            else if (property.isExpanded)
            {
                GameObject go = obj as GameObject;
                if (go.TryGetComponent(out Image image))
                {
                    OnImageTransformGUI(rect, property, image, newObj);
                }
                else if (go.TryGetComponent(out TextMeshProUGUI text))
                {
                    OnTextTransformGUI(rect, property, text, newObj);
                }
                else if (go.TryGetComponent(out RectTransform rectTransform))
                {
                    OnRectTransformGUI(rect, property, rectTransform, newObj);
                }
            }


            EditorGUI.EndProperty();
        }

        private void OnRectTransformGUI(Rect rect, SerializedProperty property, RectTransform rectTransform, bool newObj)
        {
            p_rectTransform = property.FindPropertyRelative("rectTransform");
            p_image = property.FindPropertyRelative("image");
            p_text = property.FindPropertyRelative("text");
            p_rectTransition = property.FindPropertyRelative("rectTransition");
            p_scaleState = property.FindPropertyRelative("scaleState");
            p_positionState = property.FindPropertyRelative("positionState");

            if (newObj)
            {
                p_scaleState.FindPropertyRelative("initialValue").vector2Value = rectTransform.localScale;
                p_positionState.FindPropertyRelative("initialValue").vector2Value = rectTransform.anchoredPosition;
            }

            if (p_rectTransform.objectReferenceValue != rectTransform)
                p_rectTransform.objectReferenceValue = rectTransform;
            if (p_image.objectReferenceValue != null)
                p_image.objectReferenceValue = null;
            if (p_text.objectReferenceValue != null)
                p_text.objectReferenceValue = null;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(rect, p_rectTransform, true);
            rect.y += 30f;
            EditorGUI.EndDisabledGroup();

            EditorGUI.PropertyField(rect, p_rectTransition, true);
            rect.y += 20f;

            if ((RectTransition & GraphicTransition.RectTransition.SCALE) != 0)
            {
                EditorGUI.PropertyField(rect, p_scaleState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_scaleState);
            }
            if ((RectTransition & GraphicTransition.RectTransition.POSITION) != 0)
            {
                EditorGUI.PropertyField(rect, p_positionState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_positionState);
            }
        }
        private void OnImageTransformGUI(Rect rect, SerializedProperty property, Image image, bool newObj)
        {
            p_rectTransform = property.FindPropertyRelative("rectTransform");
            p_image = property.FindPropertyRelative("image");
            p_text = property.FindPropertyRelative("text");
            p_imageTransition = property.FindPropertyRelative("imageTransition");
            p_scaleState = property.FindPropertyRelative("scaleState");
            p_positionState = property.FindPropertyRelative("positionState");
            p_colorState = property.FindPropertyRelative("colorState");
            p_spriteState = property.FindPropertyRelative("spriteState");

            if (newObj)
            {
                p_scaleState.FindPropertyRelative("initialValue").vector2Value = image.rectTransform.localScale;
                p_positionState.FindPropertyRelative("initialValue").vector2Value = image.rectTransform.anchoredPosition;
                p_colorState.FindPropertyRelative("initialValue").colorValue = image.color;
                p_spriteState.FindPropertyRelative("initialValue").objectReferenceValue = image.sprite;
            }

            if (p_rectTransform.objectReferenceValue != null)
                p_rectTransform.objectReferenceValue = null;
            if (p_image.objectReferenceValue != image)
                p_image.objectReferenceValue = image;
            if (p_text.objectReferenceValue != null)
                p_text.objectReferenceValue = null;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(rect, p_image, true);
            rect.y += 30f;
            EditorGUI.EndDisabledGroup();

            EditorGUI.PropertyField(rect, p_imageTransition, true);
            rect.y += 20f;

            if ((ImageTransition & GraphicTransition.ImageTransition.SCALE) != 0)
            {
                EditorGUI.PropertyField(rect, p_scaleState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_scaleState);
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.POSITION) != 0)
            {
                EditorGUI.PropertyField(rect, p_positionState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_positionState);
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.COLOR) != 0)
            {
                EditorGUI.PropertyField(rect, p_colorState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_colorState);
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.SPRITE) != 0)
            {
                EditorGUI.PropertyField(rect, p_spriteState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_spriteState);
            }
        }
        private void OnTextTransformGUI(Rect rect, SerializedProperty property, TextMeshProUGUI text, bool newObj)
        {
            p_rectTransform = property.FindPropertyRelative("rectTransform");
            p_image = property.FindPropertyRelative("image");
            p_text = property.FindPropertyRelative("text");
            p_textTransition = property.FindPropertyRelative("textTransition");
            p_scaleState = property.FindPropertyRelative("scaleState");
            p_positionState = property.FindPropertyRelative("positionState");
            p_colorState = property.FindPropertyRelative("colorState");
            p_sizeState = property.FindPropertyRelative("sizeState");
            p_styleState = property.FindPropertyRelative("styleState");

            if (newObj)
            {
                p_scaleState.FindPropertyRelative("initialValue").vector2Value = text.rectTransform.localScale;
                p_positionState.FindPropertyRelative("initialValue").vector2Value = text.rectTransform.anchoredPosition;
                p_colorState.FindPropertyRelative("initialValue").colorValue = text.color;
                p_sizeState.FindPropertyRelative("initialValue").floatValue = text.fontSize;
                p_styleState.FindPropertyRelative("initialValue").enumValueIndex = (int)text.fontStyle;
            }

            if (p_rectTransform.objectReferenceValue != null)
                p_rectTransform.objectReferenceValue = null;
            if (p_image.objectReferenceValue != null)
                p_image.objectReferenceValue = null;
            if (p_text.objectReferenceValue != text)
                p_text.objectReferenceValue = text;

            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(rect, p_text, true);
            rect.y += 30f;
            EditorGUI.EndDisabledGroup();

            EditorGUI.PropertyField(rect, p_textTransition, true);
            rect.y += 20f;

            if ((TextTransition & GraphicTransition.TextTransition.SCALE) != 0)
            {
                EditorGUI.PropertyField(rect, p_scaleState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_scaleState);
            }
            if ((TextTransition & GraphicTransition.TextTransition.POSITION) != 0)
            {
                EditorGUI.PropertyField(rect, p_positionState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_positionState);
            }
            if ((TextTransition & GraphicTransition.TextTransition.COLOR) != 0)
            {
                EditorGUI.PropertyField(rect, p_colorState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_colorState);
            }
            if ((TextTransition & GraphicTransition.TextTransition.SIZE) != 0)
            {
                EditorGUI.PropertyField(rect, p_sizeState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_sizeState);
            }
            if ((TextTransition & GraphicTransition.TextTransition.STYLE) != 0)
            {
                EditorGUI.PropertyField(rect, p_styleState, true);
                rect.y += EditorGUI.GetPropertyHeight(p_styleState);
            }
        }

        #endregion

        #region Height

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return property.isExpanded ? GetExpandedHeight(property) : 20f;
        }

        private float GetExpandedHeight(SerializedProperty property)
        {
            if (property.FindPropertyRelative("image").objectReferenceValue != null)
            {
                return GetImageHeight(property);
            }
            if (property.FindPropertyRelative("text").objectReferenceValue != null)
            {
                return GetTextHeight(property);
            }
            if (property.FindPropertyRelative("rectTransform").objectReferenceValue != null)
            {
                return GetRectHeight(property);
            }
            return 20f;
        }

        private float GetImageHeight(SerializedProperty property)
        {
            float sum = 70f;
            p_imageTransition = property.FindPropertyRelative("imageTransition");

            if ((ImageTransition & GraphicTransition.ImageTransition.SCALE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("scaleState"));
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.POSITION) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("positionState"));
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.COLOR) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("colorState"));
            }
            if ((ImageTransition & GraphicTransition.ImageTransition.SPRITE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spriteState"));
            }

            return sum;
        }
        private float GetTextHeight(SerializedProperty property)
        {
            float sum = 70f;
            p_textTransition = property.FindPropertyRelative("textTransition");

            if ((TextTransition & GraphicTransition.TextTransition.SCALE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("scaleState"));
            }
            if ((TextTransition & GraphicTransition.TextTransition.POSITION) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("positionState"));
            }
            if ((TextTransition & GraphicTransition.TextTransition.COLOR) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("colorState"));
            }
            if ((TextTransition & GraphicTransition.TextTransition.SIZE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sizeState"));
            }
            if ((TextTransition & GraphicTransition.TextTransition.STYLE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("styleState"));
            }

            return sum;
        }
        private float GetRectHeight(SerializedProperty property)
        {
            float sum = 70f;
            p_rectTransition = property.FindPropertyRelative("rectTransition");

            if ((RectTransition & GraphicTransition.RectTransition.SCALE) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("scaleState"));
            }
            if ((RectTransition & GraphicTransition.RectTransition.POSITION) != 0)
            {
                sum += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("positionState"));
            }

            return sum;
        }

        #endregion

        #region Clear Value

        private void ClearAll(SerializedProperty property)
        {
            p_scaleState = property.FindPropertyRelative("scaleState");
            p_positionState = property.FindPropertyRelative("positionState");
            p_colorState = property.FindPropertyRelative("colorState");
            p_spriteState = property.FindPropertyRelative("spriteState");
            p_sizeState = property.FindPropertyRelative("sizeState");
            p_styleState = property.FindPropertyRelative("styleState");

            p_scaleState.FindPropertyRelative("states").ClearArray();
            p_positionState.FindPropertyRelative("states").ClearArray();
            p_colorState.FindPropertyRelative("states").ClearArray();
            p_spriteState.FindPropertyRelative("states").ClearArray();
            p_sizeState.FindPropertyRelative("states").ClearArray();
            p_styleState.FindPropertyRelative("states").ClearArray();
        }

        #endregion
    }

    #endregion

#endif

    #endregion
}

