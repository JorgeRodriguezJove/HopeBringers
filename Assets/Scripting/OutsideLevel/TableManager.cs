﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
    #region VARIABLES

    //Cámara del mapa
    [SerializeField]
    private GameObject mapCamera;
    //Cámara de la selección de unidades
    [SerializeField]
    private GameObject selectCamera;
    //Cámara de la progresión de unidades
    [SerializeField]
    private GameObject progresionCamera;
    [SerializeField]
    private GameObject upgradesCamera;


    //Referencia al personaje que esta siendo mejorado actualemente
    private CharacterData currentCharacterUpgrading;

    //Objeto que se mueve entre los niveles e indica el nivel actual.
    [SerializeField]
    private GameObject levelIndicator;

    //Referencia al nivel clickado. 
    [HideInInspector]
    public LevelNode lastLevelClicked;

    //Nombre del nivel actual que hay que cargar si el jugador le da a ready
    [HideInInspector]
    public string currentClickedSceneName;

    [Header("Referencias")]
    [SerializeField]
    private UITableManager UIMM;

    //Referencia al primer nivel para que se setee al principio
    [SerializeField]
    private LevelNode level1;

    #endregion

    #region INIT

    private void Start()
    {
        GameManager.Instance.unitsForCurrentLevel.Clear();
        GameManager.Instance.characterDataForCurrentLevel.Clear();

        OnLevelClicked(level1);
    }

    #endregion

    #region CAMERA_MOVEMENT

    //Al seleccionar un nivel se setea todo para que aparezca la parte de selección de unidades
    public void OnLevelClicked(LevelNode levelClicked)
    {
        //Se mueve el indicador del nivel
        levelIndicator.transform.position = new Vector3(levelClicked.transform.position.x, levelIndicator.transform.position.y , levelClicked.transform.position.z);

        //El UI Manager se encarga de actualizar la info del nivel
        UIMM.ShowInfoOnLevelClick(levelClicked.LevelTitle);

        //Aparece el botón para entrar en la selección de nivel (quizás no es tanto que aparezca como simplmente guardar la informacion para cuando le de)
        lastLevelClicked = levelClicked;
    }

    public void MoveToSelection()
    {
        mapCamera.SetActive(false);
        selectCamera.SetActive(true);

        currentClickedSceneName = lastLevelClicked.sceneName;
    }

    public void MoveToProgresion()
    {
        mapCamera.SetActive(false);
        progresionCamera.SetActive(true);
    }

    public void MoveToUpgrades()
    {
        //Botón de atrás ahora cambia

        progresionCamera.SetActive(false);
        upgradesCamera.SetActive(true);
    }

    public void BackToProgresion()
    {
        upgradesCamera.SetActive(false);
        progresionCamera.SetActive(true);
    }

    public void BackToMap()
    {
        ///EN EL FUTURO MOVER LA FICHA AQUÍ y cambiar el nombre del capítulo arriba a la izquierda y poner confirmación del nivel

        //Movimiento de cámara
        mapCamera.SetActive(true);
        selectCamera.SetActive(false);
        progresionCamera.SetActive(false);

        //Reseteo personajes seleccionados
        GameManager.Instance.unitsForCurrentLevel.Clear();
        GameManager.Instance.characterDataForCurrentLevel.Clear();
        currentCharacterUpgrading = null;
    }

    #endregion

    //Esto en el futuro se hará arrastrando al personaje pero de momento lo hago con click
    public void OnClickCharacter(CharacterData unitClicked)
    {
        if (selectCamera.activeSelf)
        {
            GameManager.Instance.unitsForCurrentLevel.Add(unitClicked.myUnit);
            GameManager.Instance.characterDataForCurrentLevel.Add(unitClicked);
        }

        else if (progresionCamera.activeSelf)
        {
            MoveToUpgrades();
            UIMM.MoveToUpgradesUI(unitClicked);
            currentCharacterUpgrading = unitClicked;
        }
        
    }

    public void BuyUpgrade(UpgradeNode upgradeClicked)
    {
        //Comprobar si tengo exp suficiente
        if (currentCharacterUpgrading.powerLevel <= GameManager.Instance.CurrentExp)
        {
            //Gastar Exp
            GameManager.Instance.CurrentExp -= currentCharacterUpgrading.powerLevel;

            //Avisar al nodo de mejora de que ha sido comprado  y Desbloquear los siguientes nodos
            upgradeClicked.UpgradeBought();

            //Aumentar power level del personaje
            //Añadir id a la lista del personaje
            currentCharacterUpgrading.UpgradeAcquired(upgradeClicked.upgradeCost,upgradeClicked.idUpgrade);
        }


        else
        {
            //¿Dar feedback de que no hay suficiente exp?
        }



    }


}
