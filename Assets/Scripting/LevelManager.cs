﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LevelManager : MonoBehaviour
{
    #region VARIABLES

    //INTERACCIÓN CON UNIDADES--------------------------------------------

    //Personaje actualmente seleccionado.
    //SERIALEZE ÚNICAMENTE PARA PROBAR
    [SerializeField]
    public PlayerUnit selectedCharacter;

    //Tiles que actualmente están dispoibles para el movimiento de la unidad seleccionada
    [SerializeField]
    List<IndividualTiles> tilesAvailableForMovement = new List<IndividualTiles>();

    //De momento se guarda aquí pero se podría contemplar que cada personaje tuviese un tiempo distinto.
    float timeForMovementAnimation = 0.2f;

    //Posición a la que tiene que moverse la unidad actualmente
    private Vector3 currentTileVectorToMove;

    //Tile en el que ha empezado a moverse el personaje seleccionado. Se usa para volver a ponerlo donde estaba.
    private IndividualTiles previousCharacterTile;

    //TURNOS Y FASES--------------------------------------------

    //Enum que indica si es la fase del jugador o del enemigo.
    [HideInInspector]
    public enum LevelState { PlayerPhase, ProcessingPlayerActions, EnemyPhase, ProcessingEnemiesActions};

    //SERIALEZE ÚNICAMENTE PARA PROBAR
    [SerializeField]
    public LevelState currentLevelState { get; private set; }

    //Cada unidad se encarga desde su script de incluirse en la lista
    //Lista con todas las unidades del jugador en el tablero
    public List<PlayerUnit> characthersOnTheBoard;
    //Lista con todas las unidades enemigas en el tablero
    public List<EnemyUnit> enemiesOnTheBoard;

    //REFERENCIAS--------------------------------------------

    //Referencia al Tile Manager
    private TileManager TM;
    private UIManager UIM;

    #endregion

    #region INIT

    private void Start()
    {
        TM = FindObjectOfType<TileManager>();
        UIM = FindObjectOfType<UIManager>();

        ReOrderUnits();

        currentLevelState = LevelState.PlayerPhase;
    }

    //Ordeno la lista de personajes del jugador y la lista de enemigos
    private void ReOrderUnits()
    {
        if (characthersOnTheBoard.Count > 0)
        {
            characthersOnTheBoard.Sort(delegate (PlayerUnit a, PlayerUnit b)
            {
                return (b.GetComponent<PlayerUnit>().speed).CompareTo(a.GetComponent<PlayerUnit>().speed);

            });
        }

        if (enemiesOnTheBoard.Count > 0)
        {
            enemiesOnTheBoard.Sort(delegate (EnemyUnit a, EnemyUnit b)
            {
                return (b.GetComponent<EnemyUnit>().speed).CompareTo(a.GetComponent<EnemyUnit>().speed);

            });
        }

    }

    //Cambia de fase. Si era la fase del player ahora es la del enemigo y viceversa
    //Se llama desde el UI Manager al pulsar el botón de end turn 
    public void ChangePhase()
    {
        currentLevelState = LevelState.EnemyPhase;
    }

    private void BeginPlayerPhase()
    {
        //Aparece cartel con turno del player
        //Resetear todas las variables tipo bool y demás de los players
        //
    }

    private void BeginEnemyPhase()
    {
        //Desaparece botón de end turn
        UIM.ActivateDeActivateEndButton();
        //Aparece cartel con turno del enemigo
        //Me aseguro de que el jugador no puede interactuar con sus pjs
        //
        //
    }


    #endregion

    #region UNIT_INTERACTION

    //Al clickar sobre una unidad del jugador se llama a esta función
    public void SelectUnit(int movementUds, PlayerUnit clickedUnit)
    {
        tilesAvailableForMovement.Clear();

        //Si es el turno del player compruebo si puedo hacer algo con la unidad.
        if (currentLevelState == LevelState.ProcessingPlayerActions)
        {
            //Si no hay unidad seleccionada significa que está seleccionando una unidad
            if (selectedCharacter == null)
            {
                //Si no se ha movido significa que la puedo mover y doy feedback de sus casillas de movimiento
                if (!clickedUnit.hasMoved)
                {
                    selectedCharacter = clickedUnit;
                    selectedCharacter.SelectedColor();
                    tilesAvailableForMovement = TM.checkAvailableTilesForMovement(movementUds, clickedUnit);

                    //ESTO DEBERÍA COMPROBARLO AL MOVERSE Y AL EMPEZAR EL TURNO
                    selectedCharacter.CheckUnitsInRangeToAttack();
                }

                //Si se ha movido pero no ha atacado, entonces le doy el feedback de ataque.
                else if (!clickedUnit.hasAttacked)
                {
                    selectedCharacter = clickedUnit;
                    selectedCharacter.SelectedColor();

                    selectedCharacter.CheckUnitsInRangeToAttack();
                }
            }

            //Si ya hay una seleccionada significa que está atacando a la unidad
            else
            {
                SelectUnitToAttack(clickedUnit);
            }
        }
    }

    //Función que se llama al clickar sobre un enemigo o sobre un aliado si ya tengo seleccionado un personaje
    public void SelectUnitToAttack(UnitBase clickedUnit)
    {
        Debug.Log("comprobaçao");
        //Compruebo si está en la lista de posibles targets
        for (int i = 0; i < selectedCharacter.currentUnitsAvailableToAttack.Count; i++)
        {
            if (clickedUnit == selectedCharacter.currentUnitsAvailableToAttack[i])
            {
                Debug.Log("adasda");
                selectedCharacter.Attack(clickedUnit);
            }
        }
    }


    public void DeSelectUnit()
    {
        if (selectedCharacter != null && !selectedCharacter.isMoving)
        {
            //Si no se ha movido lo deselecciono.
            for (int i = 0; i < tilesAvailableForMovement.Count; i++)
            {
                tilesAvailableForMovement[i].ColorDeselect();
            }
            tilesAvailableForMovement.Clear();
            selectedCharacter.InitialColor();
            selectedCharacter = null;
        }
    }

    public void UndoMove()
    {
        //ESTO HAY QUE CAMBIARLO PARA QUE GUARDE TANTO LA UNIDAD CÓMO EL TILE EN EL QUE ESTABA (QUIZÁS USAR UN DICCIONARIO)

        if (selectedCharacter != null && !selectedCharacter.isMoving)
        {
            //Si el personaje ya se ha movido lo vuelvo a poner donde estaba.
            if (selectedCharacter.hasMoved)
            {
                selectedCharacter.gameObject.transform.position = new Vector3(previousCharacterTile.transform.position.x, previousCharacterTile.transform.position.y + 1, previousCharacterTile.transform.position.z);

                selectedCharacter.myCurrentTile = previousCharacterTile;

                selectedCharacter.hasMoved = false;

                tilesAvailableForMovement = TM.checkAvailableTilesForMovement(selectedCharacter.movementUds, selectedCharacter);
            }
        }

        else if (previousCharacterTile != null)
        {

        }
    }


    //Quizás tendría más sentido que el move Unit estuviese en la propia unidad.
    public void MoveUnit(IndividualTiles tileToMove)
    {
        for (int i = 0; i < tilesAvailableForMovement.Count; i++)
        {
            if (tileToMove == tilesAvailableForMovement[i])
            {
                //Almaceno al tile para poder volver a colocar a la unidad en él.
                //ESTO HAY QUE CAMBIARLO YA QUE NO ME SIRVE CON ALMACENAR UNA ÚNICA POSICIÓN
                previousCharacterTile = selectedCharacter.myCurrentTile;

                //Calculo el path de la unidad
                TM.CalculatePathForMovementCost(tileToMove.tileX, tileToMove.tileZ);

                //Al terminar de moverse se deseleccionan los tiles
                for (int j = 0; j < tilesAvailableForMovement.Count; j++)
                {
                    tilesAvailableForMovement[j].ColorDeselect();
                }
                tilesAvailableForMovement.Clear();

                //Aviso a la unidad de que se tiene que mover
                selectedCharacter.MoveToTile(tileToMove, TM.currentPath);

            }
        }
    }

    #endregion

    private void Update()
    {
        switch (currentLevelState)
        {
            case (LevelState.PlayerPhase):
                BeginPlayerPhase();
                currentLevelState = LevelState.ProcessingPlayerActions;
                break;

            case (LevelState.ProcessingPlayerActions):
                break;

            case (LevelState.EnemyPhase):
                BeginEnemyPhase();
                currentLevelState = LevelState.ProcessingEnemiesActions;
                break;

            case (LevelState.ProcessingEnemiesActions):
                break;

        }

        //INPUT
        if (Input.GetMouseButtonDown(1))
        {
            DeSelectUnit();
        }
    }

}

