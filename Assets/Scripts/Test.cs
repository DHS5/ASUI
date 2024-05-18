using Dhs5.ASUI;
using UnityEngine;
using UnityEngine.EventSystems;

public class Test : Selectable, ISubmitHandler
{
    //[SerializeField] private Button button;
    //[SerializeField] private Graphic graphic;

    public void OnSubmit(BaseEventData eventData)
    {
        //button.OnSubmit(eventData);
        //EventSystemUtility.SimulateClick(button, EventSystemUtility.PostEventSelectionState.BACK_TO_LAST);
    }

    public void OnButtonClick()
    {
        Debug.Log("click");
    }
}
