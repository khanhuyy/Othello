using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PausePanel : MonoBehaviour
{
    public void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
