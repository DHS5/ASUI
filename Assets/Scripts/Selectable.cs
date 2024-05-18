using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dhs5.ASUI
{
    public class Selectable : UIBehaviour,
        IMoveHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        #region State Enum

        public const int STATE_COUNT = 7;
        public enum State
        {
            NORMAL = 0,
            HIGHLIGHTED = 1,
            PRESSED = 2,
            SELECTED = 3,
            SELEC_HIGHLIGHTED = 4,
            DISABLED = 5,
            DISAB_HIGHLIGHTED = 6
        }

        #endregion

        #region Public Properties

        public bool HasSelection { get; private set; }
        public bool IsPointerDown { get; private set; }
        public bool IsPointerInside { get; private set; }


        public bool Interactable
        {
            get => m_interactable;
            set
            {
                if (m_interactable != value)
                {
                    m_interactable = value;
                    if (!m_interactable && EventSystemUtility.IsSelection(gameObject))
                    {
                        EventSystemUtility.SetSelection(null);
                    }
                    OnTransitionPropertyChanged();
                }
            }
        }
        public UnityEngine.UI.Navigation Navigation
        {
            get => m_navigation;
            set
            {
                if (!EqualityComparer<UnityEngine.UI.Navigation>.Default.Equals(m_navigation, value))
                {
                    m_navigation = value;
                    OnNavigationPropertyChanged();
                }
            }
        }
        public GraphicTransitioner Transitioner
        {
            get => m_graphicTransitioner;
            set
            {
                if (m_graphicTransitioner != value)
                {
                    m_graphicTransitioner = value;
                    OnTransitionPropertyChanged();
                }
            }
        }

        public State ActualState
        {
            get
            {
                if (!IsInteractable())
                    return IsPointerInside ? State.DISAB_HIGHLIGHTED : State.DISABLED;
                if (IsPointerDown)
                    return State.PRESSED;
                if (HasSelection)
                    return IsPointerInside ? State.SELEC_HIGHLIGHTED : State.SELECTED;
                if (IsPointerInside)
                    return State.HIGHLIGHTED;
                return State.NORMAL;
            }
        }
        public State CurrentState => m_currentState;
        protected virtual void SetCurrentState(State state, bool instant)
        {
            if (m_currentState != state)
            {
                m_currentState = state;
                OnStateChanged();
                DoStateTransition(instant);
            }
        }

        #endregion

        #region Protected Properties

        protected int m_currentIndex = -1;

        #endregion

        #region Private Serialized Members

        [SerializeField] private bool m_interactable = true;

        [SerializeField] private UnityEngine.UI.Navigation m_navigation;

        [SerializeField] private GraphicTransitioner m_graphicTransitioner;

        [SerializeField] private State m_currentState;

        #endregion

        #region State Accessor Methods

        public virtual bool IsInteractable()
        {
            return m_interactable && m_groupsAllowInteraction;
        }

        #endregion

        #region Static Members

        protected static Selectable[] _selectables = new Selectable[10];

        public static Selectable[] AllSelectables
        {
            get
            {
                Selectable[] temp = new Selectable[SelectableCount];
                Array.Copy(_selectables, temp, SelectableCount);
                return temp;
            }
        }
        public static int SelectableCount { get; private set; }
        public static int AllSelectablesNoAlloc(Selectable[] selectables)
        {
            int copyCount = selectables.Length < SelectableCount ? selectables.Length : SelectableCount;

            Array.Copy(_selectables, selectables, copyCount);

            return copyCount;
        }

        #endregion

        #region Events

        public event Action<Selectable, State> StateChanged;

        public event Action<Selectable> Selected;
        public event Action<Selectable> Deselected;

        public event Action<Selectable, MoveDirection> Moved;

        public event Action<Selectable> PointerEntered;
        public event Action<Selectable> PointerExited;
        public event Action<Selectable> PointerDowned;
        public event Action<Selectable> PointerUped;

        #endregion


        #region Core Behaviour

        protected override void Awake()
        {
            if (m_graphicTransitioner == null) m_graphicTransitioner = GetComponent<GraphicTransitioner>();
        }

        private bool m_enableCalled;
        protected override void OnEnable()
        {
            if (m_enableCalled) return;

            base.OnEnable();

            if (SelectableCount == _selectables.Length)
            {
                Selectable[] temp = new Selectable[_selectables.Length * 2];
                Array.Copy(_selectables, temp, _selectables.Length);
                _selectables = temp;
            }

            if (EventSystemUtility.IsSelection(gameObject))
            {
                HasSelection = true;
            }

            m_currentIndex = SelectableCount;
            _selectables[m_currentIndex] = this;
            SelectableCount++;
            IsPointerDown = false;
            m_groupsAllowInteraction = ParentGroupAllowsInteraction();
            SetCurrentState(ActualState, true);

            m_enableCalled = true;
        }
        protected override void OnDisable()
        {
            if (!m_enableCalled) return;

            SelectableCount--;
            // Update the last elements index to be this index
            _selectables[SelectableCount].m_currentIndex = m_currentIndex;
            // Swap the last element and this element
            _selectables[m_currentIndex] = _selectables[SelectableCount];
            // null out last element.
            _selectables[SelectableCount] = null;

            ClearState();
            base.OnDisable();

            m_enableCalled = false;
        }


        protected override void OnDidApplyAnimationProperties()
        {
            OnTransitionPropertyChanged();
        }
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            // If our parenting changes figure out if we are under a new CanvasGroup.
            OnCanvasGroupChanged();
        }
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ClearState();
            }
        }

        #endregion

        #region Initialization

        protected virtual void ClearState()
        {
            HasSelection = false;
            IsPointerDown = false;
            IsPointerInside = false;

            SetCurrentState(ActualState, true);
        }

        #endregion


        #region Interfaces

        // --- SELECTION ---
        public virtual void OnSelect(BaseEventData eventData)
        {
            HasSelection = true;
            Selected?.Invoke(this);
            EvaluateState();
        }
        public virtual void OnDeselect(BaseEventData eventData)
        {
            HasSelection = false;
            Deselected?.Invoke(this);
            EvaluateState();
        }

        // --- NAVIGATION ---

        public virtual void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    Navigate(eventData, FindSelectableOnRight());
                    break;

                case MoveDirection.Up:
                    Navigate(eventData, FindSelectableOnUp());
                    break;

                case MoveDirection.Left:
                    Navigate(eventData, FindSelectableOnLeft());
                    break;

                case MoveDirection.Down:
                    Navigate(eventData, FindSelectableOnDown());
                    break;
            }
            Moved?.Invoke(this, eventData.moveDir);
        }

        // --- POINTER ---

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            IsPointerInside = true;
            PointerEntered?.Invoke(this);
            EvaluateState();
        }
        public virtual void OnPointerExit(PointerEventData eventData)
        {
            IsPointerInside = false;
            PointerExited?.Invoke(this);
            EvaluateState();
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            PointerDowned?.Invoke(this);

            if (IsInteractable())
            {
                EventSystemUtility.SetSelection(gameObject, eventData);
            }

            IsPointerDown = true;
            EvaluateState();
        }
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            PointerUped?.Invoke(this);

            IsPointerDown = false;
            EvaluateState();
        }

        #endregion


        #region State Evaluation Methods

        protected virtual void EvaluateState()
        {
            if (!IsActive())// || !IsInteractable())
                return;

            SetCurrentState(ActualState, false);
        }

        #endregion

        #region Callbacks

        protected virtual void OnTransitionPropertyChanged()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                SetCurrentState(ActualState, true);
            else
#endif
                SetCurrentState(ActualState, false);
        }
        protected virtual void OnNavigationPropertyChanged() { }
        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, m_currentState);
        }

        #endregion


        #region Canvas Group

        private bool m_groupsAllowInteraction = true;
        private readonly List<CanvasGroup> m_canvasGroupCache = new List<CanvasGroup>();

        protected override void OnCanvasGroupChanged()
        {
            var parentGroupAllowsInteraction = ParentGroupAllowsInteraction();

            if (parentGroupAllowsInteraction != m_groupsAllowInteraction)
            {
                m_groupsAllowInteraction = parentGroupAllowsInteraction;
                OnTransitionPropertyChanged();
            }
        }

        bool ParentGroupAllowsInteraction()
        {
            Transform t = transform;
            while (t != null)
            {
                t.GetComponents(m_canvasGroupCache);
                for (var i = 0; i < m_canvasGroupCache.Count; i++)
                {
                    if (m_canvasGroupCache[i].enabled && !m_canvasGroupCache[i].interactable)
                        return false;

                    if (m_canvasGroupCache[i].ignoreParentGroups)
                        return true;
                }

                t = t.parent;
            }

            return true;
        }

        #endregion

        #region Transition

        protected virtual void DoStateTransition(bool instant)
        {
            if (!gameObject.activeInHierarchy || !Transitioner)
                return;

            Transitioner.DoStateTransition(m_currentState, instant);
        }

        protected virtual void ClearAllTransitions()
        {
            if (!Transitioner) return;

            Transitioner.ClearTransitions();
        }

        #endregion

        #region Navigation

        public Selectable FindSelectable(Vector3 dir)
        {
            dir = dir.normalized;
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
            Vector3 pos = transform.TransformPoint(GetPointOnRectEdge(transform as RectTransform, localDir));
            float maxScore = Mathf.NegativeInfinity;
            float maxFurthestScore = Mathf.NegativeInfinity;
            float score = 0;

            bool wantsWrapAround = Navigation.wrapAround && (Navigation.mode == UnityEngine.UI.Navigation.Mode.Vertical || Navigation.mode == UnityEngine.UI.Navigation.Mode.Horizontal);

            Selectable bestPick = null;
            Selectable bestFurthestPick = null;

            for (int i = 0; i < SelectableCount; ++i)
            {
                Selectable sel = _selectables[i];

                if (sel == this)
                    continue;

                if (!sel.IsInteractable() || sel.Navigation.mode == UnityEngine.UI.Navigation.Mode.None)
                    continue;

                var selRect = sel.transform as RectTransform;
                Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
                Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;

                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // If element is in wrong direction and we have wrapAround enabled check and cache it if furthest away.
                if (wantsWrapAround && dot < 0)
                {
                    score = -dot * myVector.sqrMagnitude;

                    if (score > maxFurthestScore)
                    {
                        maxFurthestScore = score;
                        bestFurthestPick = sel;
                    }

                    continue;
                }

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                score = dot / myVector.sqrMagnitude;

                if (score > maxScore)
                {
                    maxScore = score;
                    bestPick = sel;
                }
            }

            if (wantsWrapAround && null == bestPick) return bestFurthestPick;

            return bestPick;
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }

        private static void Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
                eventData.selectedObject = sel.gameObject;
        }

        public virtual Selectable FindSelectableOnLeft()
        {
            if (Navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
            {
                //return Navigation.selectOnLeft;
            }
            if ((Navigation.mode & UnityEngine.UI.Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.left);
            }
            return null;
        }
        public virtual Selectable FindSelectableOnRight()
        {
            if (Navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
            {
                //return Navigation.selectOnRight;
            }
            if ((Navigation.mode & UnityEngine.UI.Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.right);
            }
            return null;
        }
        public virtual Selectable FindSelectableOnUp()
        {
            if (Navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
            {
                //return Navigation.selectOnUp;
            }
            if ((Navigation.mode & UnityEngine.UI.Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.up);
            }
            return null;
        }
        public virtual Selectable FindSelectableOnDown()
        {
            if (Navigation.mode == UnityEngine.UI.Navigation.Mode.Explicit)
            {
                //return Navigation.selectOnDown;
            }
            if ((Navigation.mode & UnityEngine.UI.Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.down);
            }
            return null;
        }

        #endregion


        #region Public Action Methods

        public void Select()
        {
            if (!HasSelection)
                EventSystemUtility.SetSelection(gameObject);
        }

        #endregion


        #region Editor

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            // OnValidate can be called before OnEnable, this makes it unsafe to access other components
            // since they might not have been initialized yet.
            // OnSetProperty potentially access Animator or Graphics. (case 618186)
            if (IsActive())
            {
                if (!Interactable && EventSystemUtility.IsSelection(gameObject))
                    EventSystemUtility.SetSelection(null);

                // And now go to the right state.
                SetCurrentState(ActualState, true);
            }
        }

        protected override void Reset()
        {
            m_graphicTransitioner = GetComponent<GraphicTransitioner>();
        }

#endif

        #endregion
    }

    #region Editor

#if UNITY_EDITOR

    [CustomEditor(typeof(Selectable), true), CanEditMultipleObjects]
    public class SelectableEditor : Editor
    {
        #region Global Members

        protected Selectable m_selectable;

        private string[] m_PropertyPathToExcludeForChildClasses;

        protected SerializedProperty p_script;
        protected SerializedProperty p_interactable;
        protected SerializedProperty p_transitioner;
        protected SerializedProperty p_currentState;
        protected SerializedProperty p_navigation;

        #endregion

        #region Core Behaviour

        private void OnEnable()
        {
            m_selectable = target as Selectable;

            p_script = serializedObject.FindProperty("m_Script");
            p_interactable = serializedObject.FindProperty("m_interactable");
            p_transitioner = serializedObject.FindProperty("m_graphicTransitioner");
            p_currentState = serializedObject.FindProperty("m_currentState");
            p_navigation = serializedObject.FindProperty("m_navigation");

            m_PropertyPathToExcludeForChildClasses = new string[]
            {
                p_script.propertyPath,
                p_interactable.propertyPath,
                p_transitioner.propertyPath,
                p_currentState.propertyPath,
                p_navigation.propertyPath,
            };
        }

        #endregion

        #region GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnNavigationGUI();

            OnSelectableBaseEditor();

            OnChildGUI();

            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnNavigationGUI() { }

        protected virtual void OnSelectableBaseEditor()
        {
            EditorGUILayout.PropertyField(p_interactable);

            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField("Transition", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(p_transitioner);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(p_currentState);
            EditorGUI.EndDisabledGroup();
        }

        protected virtual void OnChildGUI()
        {
            EditorGUILayout.Space(15f);
            EditorGUILayout.LabelField(serializedObject.targetObject.GetType().Name, EditorStyles.boldLabel);
            DrawPropertiesExcluding(serializedObject, m_PropertyPathToExcludeForChildClasses);
        }

        #endregion
    }

#endif

    #endregion
}
