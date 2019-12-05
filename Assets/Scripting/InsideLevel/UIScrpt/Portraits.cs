﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Portraits : MonoBehaviour
{
	#region VARIABLES

	[HideInInspector]
	public PlayerUnit assignedPlayer;
	[HideInInspector]
	private UIManager UIM;
    //Añadido para hacer comprobaciones de turnos
    [HideInInspector]
    private LevelManager LM;

    //AÑADIDO
    //Barra de vida y valor de la barra del personaje
    [SerializeField]
    public TextMeshProUGUI healthValue;
    [SerializeField]
    public Slider healthBar;

    //Los tokens son listas por si en el futuro hay personajes que necesitan más tokens. (De ser así habría que hacer más cambios)
    [SerializeField]
    public List<GameObject> attackTokens;
    [SerializeField]
    public List<GameObject> movementTokens;

	[HideInInspector]
	private Sprite selectedImage;
	[HideInInspector]
	private Sprite initImage;
    //Con este panel es más fácil cambiar el color que se quiere desde el editor, en vez de estar haciendo un nuevo sprite cada vez.
    [SerializeField]
    public GameObject selectedPanel;

	//Gameobject Image dónde va el sprite del personaje
	[SerializeField]
    public Image characterPortrait;


    //Bool que indica a los retratos si están clickados
    public bool isClicked;


    #endregion

    #region INIT

    private void Awake()
	{
		UIM = FindObjectOfType<UIManager>();
        //Añadido para hacer comprobaciones de turnos
        LM = FindObjectOfType<LevelManager>();
        //Se desactiva para que el UImanager active únicamente los necesarios en función del número de personajes.
        gameObject.SetActive(false);
		initImage = GetComponent<Image>().sprite;
	}

    private void Start()
    {
        RefreshHealth();
        RefreshSprites();
        RefreshTokens();
    }

    #endregion

    #region INTERACTION

    public void AssignClickerPlayer()
	{
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            UIM.PortraitCharacterSelect(assignedPlayer);
            isClicked = true;
        }
	}

	public void Highlight()
	{
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            UIM.HighlightCharacter(assignedPlayer);
            UIM.LM.ShowUnitHover(assignedPlayer.movementUds, assignedPlayer);
            selectedPanel.SetActive(true);
        }
    }

	public void Unhighlight()
	{
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            UIM.UnHighlightCharacter(assignedPlayer);
            UIM.LM.HideUnitHover(assignedPlayer);
            if (isClicked == false)
            {
                selectedPanel.SetActive(false);
            }

        }
    }

	public void HighlightPortrait()
	{
        //GetComponent<Image>().sprite = selectedImage;
        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            selectedPanel.SetActive(true);
        }
    }
	public void UnHighlightPortrait()
	{
        //GetComponent<Image>().sprite = initImage;

        if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
        {
            selectedPanel.SetActive(false);
        }
        
    }

	public void ShowCharacterImageFromPortrait()
	{
		if (UIM.LM.selectedCharacter == null)
		{
			UIM.ShowCharacterImage(assignedPlayer);
		}
		
	}

    #endregion

    #region REFRESH

    public void RefreshHealth()
    {
        healthBar.maxValue = assignedPlayer.maxHealth;
        healthBar.value = assignedPlayer.currentHealth;
        healthValue.text = assignedPlayer.currentHealth + "/" + assignedPlayer.maxHealth;
    }

    public void RefreshTokens()
    {
        for (int i = 0; i < attackTokens.Count; i++)
        {
            //attackTokens[i].SetActive(!assignedPlayer.hasAttacked);
			if (!assignedPlayer.hasAttacked)
			{
				attackTokens[i].GetComponent<Animator>().Play("TokenReset");
			}
			else
			{
				attackTokens[i].GetComponent<Animator>().Play("TokenFlip");
			}
        }
        for (int i = 0; i < movementTokens.Count; i++)
        {
            //movementTokens[i].SetActive(!assignedPlayer.hasMoved);
			if (!assignedPlayer.hasMoved)
			{
				movementTokens[i].GetComponent<Animator>().Play("TokenReset");
			}
			else
			{
				movementTokens[i].GetComponent<Animator>().Play("TokenFlip");
			}
		}
    }

    public void RefreshSprites()
    {
        characterPortrait.sprite = assignedPlayer.portraitImage;
    }

    #endregion

}
