﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyPortraits : MonoBehaviour
{
	#region VARIABLES

	[HideInInspector]
	public Sprite enemyPortraitSprite;
	[HideInInspector]
	public EnemyUnit assignedEnemy;
	[HideInInspector]
	public UIManager UIM;

    //Refernecia al panel con el highlight
    [SerializeField]
    private GameObject highlightPanelRef;

    //Añadido para hacer comprobaciones de turnos
    [HideInInspector]
    private LevelManager LM;

    #endregion

    #region INIT
    private void Awake()
	{
		UIM = FindObjectOfType<UIManager>();

        //Añadido para hacer comprobaciones de turnos
        LM = FindObjectOfType<LevelManager>();
    }
	private void Start()
	{
		GetComponent<Image>().sprite = enemyPortraitSprite;
	}

	#endregion

	#region INTERACTION

	public void ShowEnemyPortraitFromPanel()
	{
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            if (UIM.LM.selectedCharacter == null && UIM.LM.selectedEnemy == null)
            {
                //Activo highlight de retrato y de personaje
                assignedEnemy.OnHoverEnterFunctionality();

                HighlightMyself();
            }
        }
	}

    //Función separada para que la llame el enemigo.
    public void HighlightMyself()
    {
        highlightPanelRef.SetActive(true);
    }

	public void HideEnemyPortraitFromPanel()
    {
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            if (UIM.LM.selectedEnemy == null)
            {
                assignedEnemy.OnHoverExitFunctionality();
                highlightPanelRef.SetActive(false);
            }
        }
	}

    //Función separada para que la llame el enemigo.
    public void UnHighlightMyself()
    {
        highlightPanelRef.SetActive(false);
    }

    public void SelectEnemyFromPanel()
	{
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            UIM.ShowCharacterImage(assignedEnemy);
            UIM.LM.selectedEnemy = assignedEnemy;
        }
	}

	#endregion
}
