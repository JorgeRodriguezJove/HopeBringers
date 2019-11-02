﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyUnit : UnitBase
{
    #region VARIABLES

    //[Header("STATE MACHINE")]

    //Posibles estados del enemigo
    protected enum enemyState {Waiting, Searching, Moving, Attacking, Ended}

    //Estado actual del enemigo
    protected enemyState myCurrentEnemyState;

    //Distancia en tiles con el enemigo más lejano
    protected int furthestAvailableUnitDistance;


    [Header("REFERENCIAS")]

    //Ahora mismo se setea desde el inspector
    [SerializeField]
    public GameObject LevelManagerRef;
    private LevelManager LM;

    #endregion

    #region INIT

    private void Awake()
    {
        //Referencia al LM y me incluyo en la lista de enemiogos
        LM = LevelManagerRef.GetComponent<LevelManager>();
        LM.enemiesOnTheBoard.Add(this);
        myCurrentTile.unitOnTile = this;
        initMaterial = GetComponent<MeshRenderer>().material;

        myCurrentEnemyState = enemyState.Waiting;
        currentHealth = maxHealth;
    }

    #endregion

    #region ENEMY_STATE

    public void MyTurnStart()
    {
        myCurrentEnemyState = enemyState.Searching;
    }

    private void Update()
    {
        switch (myCurrentEnemyState)
        {
            case (enemyState.Waiting):
                break;

            case (enemyState.Searching):
                SearchingObjectivesToAttack();
                break;

            case (enemyState.Moving):
                MoveUnit();
                break;

            case (enemyState.Attacking):
                Attack();
                break;

            case (enemyState.Ended):
                FinishMyActions();
                break;
        }
    }

    public virtual void SearchingObjectivesToAttack()
    {
        
    }


    public virtual void MoveUnit()
    {
       //Acordarse de que cada enemigo debe actualizar al moverse los tiles (vacíar el tile anterior y setear el nuevo tile y la unidad del nuevo tile)
    }


    public virtual void Attack()
    {
        
    }

    public virtual void FinishMyActions()
    {
        LM.NextEnemyInList();
        myCurrentEnemyState = enemyState.Waiting;
    }

    #endregion

    #region INTERACTION

    //Al clickar en una unidad aviso al LM
    private void OnMouseDown()
    {
        LM.SelectUnitToAttack(GetComponent<UnitBase>());
    }

    private void OnMouseEnter()
    {
        //Llamo a LevelManager para activar hover
        LM.CheckIfHoverShouldAppear(this);
    }

    private void OnMouseExit()
    {
        //Llamo a LevelManager para desactivar hover
        LM.HideHover(this);
    }

    #endregion

    #region DAMAGE

    public override void ReceiveDamage(int damageReceived)
    {
        currentHealth -= damageReceived;

        Debug.Log("Soy " + gameObject.name + "y me han hecho " + damageReceived + " de daño");
        Debug.Log("Mi vida actual es " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public override void Die()
    {
        Debug.Log("Soy " + gameObject.name + " y he muerto");
    }

    #endregion

    #region CHECKS

    //Esta función es exactamente igual que la del player con la excepción de que solo tiene en cuenta a personajes del jugador e ignora enemigos.
    protected void CheckCharactersInLine()
    {
        currentUnitsAvailableToAttack.Clear();

        if (currentFacingDirection == FacingDirection.North || GetComponent<EnCharger>())
        {
            if (range <= myCurrentTile.tilesInLineUp.Count)
            {
                rangeVSTilesInLineLimitant = range;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineUp.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Tanto la balista cómo el charger detiene su comprobación si hay un obstáculo
                if (myCurrentTile.tilesInLineUp[i].isObstacle)
                {
                    break;
                }

                //Sólo el charger para si encuentra un tile empty o con unidad
                else if (GetComponent<EnCharger>() && (myCurrentTile.tilesInLineUp[i].isEmpty || (myCurrentTile.tilesInLineUp[i].unitOnTile != null && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<EnemyUnit>())))
                {
                    break;
                }

                //Independientemente de que sea charger o balista este código sirve para ambos
                else if (myCurrentTile.tilesInLineUp[i].unitOnTile != null && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Almaceno la primera unidad en la lista de posibles unidades.
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                    furthestAvailableUnitDistance = i;
                    
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.East || GetComponent<EnCharger>())
        {
            if (range <= myCurrentTile.tilesInLineRight.Count)
            {
                rangeVSTilesInLineLimitant = range;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineRight.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineRight[i].unitOnTile != null && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Tanto la balista cómo el charger detiene su comprobación si hay un obstáculo
                    if (myCurrentTile.tilesInLineRight[i].isObstacle)
                    {
                        break;
                    }

                    //Sólo el charger para si encuentra un tile empty o con unidad
                    else if (GetComponent<EnCharger>() && (myCurrentTile.tilesInLineRight[i].isEmpty || (myCurrentTile.tilesInLineRight[i].unitOnTile != null && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<EnemyUnit>())))
                    {
                        break;
                    }

                    //Independientemente de que sea charger o balista este código sirve para ambos

                    //Si la distancia es mayor que la distancia con el enemigo ya guardado, me deshago de la unidad anterior y almaceno esta cómo objetivo.
                    else if (currentUnitsAvailableToAttack.Count == 0 || furthestAvailableUnitDistance < i)
                    {
                        currentUnitsAvailableToAttack.Clear();
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);
                        furthestAvailableUnitDistance = i;
                    }

                    //Si tienen la misma distancia almaceno a las dos
                    else if (furthestAvailableUnitDistance == i)
                    {
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);
                    }

                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.South || GetComponent<EnCharger>())
        {
            if (range <= myCurrentTile.tilesInLineDown.Count)
            {
                rangeVSTilesInLineLimitant = range;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineDown.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineDown[i].unitOnTile != null && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Tanto la balista cómo el charger detiene su comprobación si hay un obstáculo
                    if (myCurrentTile.tilesInLineDown[i].isObstacle)
                    {
                        break;
                    }

                    //Sólo el charger para si encuentra un tile empty o con unidad
                    else if (GetComponent<EnCharger>() && (myCurrentTile.tilesInLineDown[i].isEmpty || (myCurrentTile.tilesInLineDown[i].unitOnTile != null && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<EnemyUnit>())))
                    {
                        break;
                    }

                    //Independientemente de que sea charger o balista este código sirve para ambos

                    //Si la distancia es mayor que la distancia con el enemigo ya guardado, me deshago de la unidad anterior y almaceno esta cómo objetivo.
                    if (currentUnitsAvailableToAttack.Count == 0 || furthestAvailableUnitDistance < i )
                    {
                        currentUnitsAvailableToAttack.Clear();
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);
                        furthestAvailableUnitDistance = i;
                    }

                    //Si tienen la misma distancia almaceno a las dos
                    else if (furthestAvailableUnitDistance == i)
                    {
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);
                    }
                    
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.West || GetComponent<EnCharger>())
        {
            if (range <= myCurrentTile.tilesInLineLeft.Count)
            {
                rangeVSTilesInLineLimitant = range;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineLeft.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Tanto la balista cómo el charger detiene su comprobación si hay un obstáculo
                    if (myCurrentTile.tilesInLineLeft[i].isObstacle)
                    {
                        break;
                    }

                    //Sólo el charger para si encuentra un tile empty o con unidad
                    else if (GetComponent<EnCharger>() && (myCurrentTile.tilesInLineLeft[i].isEmpty || (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<EnemyUnit>())))
                    {
                        break;
                    }

                    //Independientemente de que sea charger o balista este código sirve para ambos

                    //Si la distancia es mayor que la distancia con el enemigo ya guardado, me deshago de la unidad anterior y almaceno esta cómo objetivo.
                    if (currentUnitsAvailableToAttack.Count == 0 || furthestAvailableUnitDistance < i)
                    {
                        currentUnitsAvailableToAttack.Clear();
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);
                        furthestAvailableUnitDistance = i;
                    }

                    //Si tienen la misma distancia almaceno a las dos
                    else if (furthestAvailableUnitDistance == i)
                    {
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);
                    }
                    break;
                }
            }
        }
    }

    #endregion
}
