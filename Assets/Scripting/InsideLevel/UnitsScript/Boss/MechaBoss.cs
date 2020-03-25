﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MechaBoss : EnemyUnit
{
    //Guardo la primera unidad en la lista de currentUnitAvailbleToAttack para  no estar llamandola constantemente
    private UnitBase myCurrentObjective;
    private IndividualTiles myCurrentObjectiveTile;

    //Path de tiles a seguir hasta el objetivo
    [HideInInspector]
    private List<IndividualTiles> pathToObjective = new List<IndividualTiles>();

    //Lista que guarda los enmeigos y personajes que están dentro del rango de alerta del personaje (ya sea para comprobar personajes o alertar a enemigos)
    [HideInInspector]
    private List<UnitBase> unitsInRange = new List<UnitBase>();

    [SerializeField]
    GameObject particleShield;

    public override void SearchingObjectivesToAttack()
    {
        myCurrentObjective = null;
        myCurrentObjectiveTile = null;
        pathToObjective.Clear();

        if (isDead || hasAttacked)
        {
            myCurrentEnemyState = enemyState.Ended;
            return;
        }


        if (!haveIBeenAlerted)
        {
            //Comprobar las unidades que hay en mi rango de acción
            unitsInRange = LM.TM.GetAllUnitsInRangeWithoutPathfinding(rangeOfAction, GetComponent<UnitBase>());

            //Si hay personajes del jugador en mi rango de acción paso a attacking donde me alerto y hago mi accion
            for (int i = 0; i < unitsInRange.Count; i++)
            {
                if (unitsInRange[i].GetComponent<PlayerUnit>())
                {
                    AlertEnemy();
                    myCurrentEnemyState = enemyState.Searching;
                    return;
                }
            }

            //Si llega hasta aqui significa que no había personajes en rango y termina
            myCurrentEnemyState = enemyState.Attacking;
        }

        else if (hasMoved)
        {
            myCurrentEnemyState = enemyState.Attacking;
        }

        else
        {
            if (isCharging)
            {
                myCurrentEnemyState = enemyState.Attacking;
            }

            else
            {
                //Determinamos el enemigo más cercano.
                currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());

                //Si no hay enemigos termina su turno
                if (currentUnitsAvailableToAttack.Count == 0)
                {
                    myCurrentEnemyState = enemyState.Ended;
                }

                else if (currentUnitsAvailableToAttack.Count > 0)
                {
                    if (currentUnitsAvailableToAttack.Count == 1)
                    {
                        base.SearchingObjectivesToAttack();

                        if (currentUnitsAvailableToAttack.Count == 1)
                        {
                            myCurrentObjective = currentUnitsAvailableToAttack[0];
                            myCurrentObjectiveTile = myCurrentObjective.myCurrentTile;
                        }
                    }

                    //Si hay varios enemigos a la misma distancia, se queda con el que tenga más unidades adyacentes
                    else if (currentUnitsAvailableToAttack.Count > 1)
                    {
                        //Ordeno la lista de posibles objetivos según el número de unidades dyacentes
                        currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                        {
                            return (b.myCurrentTile.neighboursOcuppied).CompareTo(a.myCurrentTile.neighboursOcuppied);
                        });

                        //Elimino a todos los objetivos de la lista que no tengan el mayor número de enemigos adyacentes
                        for (int i = currentUnitsAvailableToAttack.Count - 1; i > 0; i--)
                        {
                            if (currentUnitsAvailableToAttack[0].myCurrentTile.neighboursOcuppied > currentUnitsAvailableToAttack[i].myCurrentTile.neighboursOcuppied)
                            {
                                currentUnitsAvailableToAttack.RemoveAt(i);
                            }
                        }

                        //Si sigue habiendo varios enemigos los ordeno segun la vida
                        if (currentUnitsAvailableToAttack.Count > 1)
                        {
                            //Añado esto para eliminar a los personajes ocultos
                            base.SearchingObjectivesToAttack();

                            //Ordeno la lista de posibles objetivos de menor a mayor vida actual
                            currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                            {
                                return (a.currentHealth).CompareTo(b.currentHealth);

                            });
                        }

                        myCurrentObjective = currentUnitsAvailableToAttack[0];
                        myCurrentObjectiveTile = myCurrentObjective.myCurrentTile;
                    }

                    //CAMBIAR ESTO (lm.tm)
                    LM.TM.CalculatePathForMovementCost(myCurrentObjectiveTile.tileX, myCurrentObjectiveTile.tileZ, false);

                    //No vale con igualar pathToObjective= LM.TM.currentPath porque entonces toma una referencia de la variable no de los valores.
                    //Esto significa que si LM.TM.currentPath cambia de valor también lo hace pathToObjective
                    for (int i = 0; i < LM.TM.currentPath.Count; i++)
                    {
                        pathToObjective.Add(LM.TM.currentPath[i]);
                    }


                    myCurrentEnemyState = enemyState.Moving;
                }
            }
        }
    }

    public override void Attack()
    {
        base.Attack();

        if (isCharging)
        {
            Debug.Log("disparo");
            ShootBeam();
            isCharging = false;
            myCurrentEnemyState = enemyState.Ended;
            particleShield.SetActive(true);
            return;

        }

        else
        {
            isCharging = true;
            Debug.Log("cargo");
            CheckTilesForBeam();
            myCurrentEnemyState = enemyState.Ended;
            particleShield.SetActive(false);
            return;
        }
    }

    bool isCharging;

    List<IndividualTiles> middleLineTilesInFront = new List<IndividualTiles>();
    List<IndividualTiles> lateralMidLineTiles = new List<IndividualTiles>();
    List<IndividualTiles> beamTiles = new List<IndividualTiles>();

    //Este check es diferente al resto, no comprueba si se puede hacer el ataque, si no cuales son los tiles que cargar (ya que este ataque no necesita que haya
    //tiles o condiciones concretas para realizarse a parte de estar cargado)
    private void CheckTilesForBeam()
    {
        middleLineTilesInFront.Clear();
        lateralMidLineTiles.Clear();
        beamTiles.Clear();
        currentUnitsAvailableToAttack.Clear();

        middleLineTilesInFront = myCurrentTile.GetTilesInFrontOfTheCharacter(currentFacingDirection, 60);

        for (int i = 0; i < middleLineTilesInFront.Count; i++)
        {
            beamTiles.Add(middleLineTilesInFront[i]);
            middleLineTilesInFront[i].ColorAttack();


            lateralMidLineTiles.Clear();

            lateralMidLineTiles = middleLineTilesInFront[i].GetLateralTilesBasedOnDirection(currentFacingDirection, 1);

            for (int j = 0; j < lateralMidLineTiles.Count; j++)
            {
                beamTiles.Add(lateralMidLineTiles[j]);

                lateralMidLineTiles[j].ColorAttack();
            }
        }

        myCurrentEnemyState = enemyState.Waiting;
    }

    private void ShootBeam()
    {
        for (int i = 0; i < beamTiles.Count; i++)
        {
            if (beamTiles[i].unitOnTile != null && beamTiles[i].unitOnTile.GetComponent<PlayerUnit>())
            {
                DoDamage(beamTiles[i].unitOnTile);
            }

            beamTiles[i].ColorDesAttack();
        }
    }

    public override void ReceiveDamage(int damageReceived, UnitBase unitAttacker)
    {
        if (isCharging)
        {
            base.ReceiveDamage(damageReceived, unitAttacker);
        }

        else
        {
            Debug.Log("Inmune");
        }
    }

    int limitantNumberOfTilesToMove;

    public override void MoveUnit()
    {
        limitantNumberOfTilesToMove = 0;

        movementParticle.SetActive(true);

        ShowActionPathFinding(false);

        //Como el path guarda el tile en el que esta el enemigo yel tile en el que esta el personaje del jugador resto 2.
        //Si esta resta se pasa del número de unidades que me puedo mover entonces solo voy a recorrer el número de tiles máximo que puedo llegar.
        if (pathToObjective.Count - 2 > movementUds)
        {
            limitantNumberOfTilesToMove = movementUds;
        }

        //Si esta resta por el contrario es menor o igual a movementUds significa que me voy mover el máximo o menos tiles.
        else
        {
            limitantNumberOfTilesToMove = pathToObjective.Count - 2;
        }

        //Compruebo la dirección en la que se mueve para girar a la unidad
        CheckTileDirection(myCurrentTile, pathToObjective[pathToObjective.Count - 1], true);

        myCurrentEnemyState = enemyState.Waiting;

        //Actualizo info de los tiles
        UpdateInformationAfterMovement(pathToObjective[limitantNumberOfTilesToMove]);

        StartCoroutine("MovingUnitAnimation");
    }

    IEnumerator MovingUnitAnimation()
    {
        //Animación de movimiento
        //Es -1 ya que no me interesa que se mueva hasta el tile en el que está la otra unidad
        for (int j = 1; j <= limitantNumberOfTilesToMove; j++)
        {
            //Calcula el vector al que se tiene que mover.
            currentTileVectorToMove = pathToObjective[j].transform.position;  //new Vector3(pathToObjective[j].transform.position.x, pathToObjective[j].transform.position.y, pathToObjective[j].transform.position.z);

            //Muevo y roto a la unidad
            transform.DOMove(currentTileVectorToMove, currentTimeForMovement);
            unitModel.transform.DOLookAt(currentTileVectorToMove, timeDurationRotation);

            //Espera entre casillas
            yield return new WaitForSeconds(currentTimeForMovement);
        }

        hasMoved = true;


        //Compruebo la dirección en la que se mueve para girar a la unidad
        CheckTileDirection(myCurrentTile ,pathToObjective[pathToObjective.Count - 1], true);
        myCurrentEnemyState = enemyState.Searching;

        movementParticle.SetActive(false);

        HideActionPathfinding();
        //ShowActionPathFinding(false);

    }

    //Esta función muestra la acción del enemigo.
    //Para esconderla hay otra función (esta en el EnemyUnit)
    public override void ShowActionPathFinding(bool _shouldRecalculate)
    {
        //Si se tiene que mostrar la acción por el hover calculamos el enemigo
        if (_shouldRecalculate)
        {
            pathToObjective.Clear();

            SearchingObjectivesToAttackShowActionPathFinding();
            if (myCurrentObjectiveTile != null)
            {
                //Cada enemigo realiza su propio path
                LM.TM.CalculatePathForMovementCost(myCurrentObjectiveTile.tileX, myCurrentObjectiveTile.tileZ, false);

                //No vale con igualar pathToObjective= LM.TM.currentPath porque entonces toma una referencia de la variable no de los valores.
                //Esto significa que si LM.TM.currentPath cambia de valor también lo hace pathToObjective
                for (int i = 0; i < LM.TM.currentPath.Count; i++)
                {
                    pathToObjective.Add(LM.TM.currentPath[i]);
                }
            }
        }

        //Si se va a mostrar la acción en el turno enemigo entonces no recalculo y directamente enseño la acción.
        //Esta parte es común para cuando se hace desde el hover como cuando se hace en turno enemigo.
        if (myCurrentObjectiveTile != null)
        {
            myLineRenderer.positionCount = 0;

            if (pathToObjective.Count - 2 > movementUds)
            {
                limitantNumberOfTilesToMove = movementUds;
            }
            else
            {
                limitantNumberOfTilesToMove = pathToObjective.Count - 2;
            }

            myLineRenderer.enabled = true;

            if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions && pathToObjective.Count > 2)
            {
                shaderHover.SetActive(true);
            }

            //Coge
            myLineRenderer.positionCount += (limitantNumberOfTilesToMove + 1);

            //myLineRenderer.SetVertexCount(LM.TM.currentPath.Count);

            for (int i = 0; i < limitantNumberOfTilesToMove + 1; i++)
            {
                Vector3 pointPosition = new Vector3(pathToObjective[i].transform.position.x, pathToObjective[i].transform.position.y + 0.5f, pathToObjective[i].transform.position.z);

                if (i < pathToObjective.Count - 1)
                {
                    myLineRenderer.SetPosition(i, pointPosition);

                    if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
                    {
                        shaderHover.transform.position = pointPosition;
                        if ((pathToObjective[i]) == currentUnitsAvailableToAttack[0].myCurrentTile)
                        {

                            //CalculateDamagePreviousAttack(currentUnitsAvailableToAttack[0], this, pathToObjective[1]);
                        }
                        else
                        {

                            damageWithMultipliersApplied = -999;
                        }

                        Vector3 positionToLook = new Vector3(myCurrentObjective.transform.position.x, myCurrentObjective.transform.position.y + 0.5f, myCurrentObjective.transform.position.z);
                        shaderHover.transform.DOLookAt(positionToLook, 0, AxisConstraint.Y);
                    }
                }
            }

            ///En el gigante es importante que esta función vaya después de colocar la sombra. Por si acaso asegurarse de que este if nunca se pone antes que el reposicionamiento de la sombra

            //A pesar de que ya se llama a esta función desde el levelManager en caso de hover, si se tiene que mostrar porque el goblin está atacando se tiene que llamar desde aqui (ya que no pasa por el level manager)
            //Tiene que ser en falso porque si no pongo la condicion la función se cree que el tileya estaba pintado de antes
            if (!_shouldRecalculate)
            {
                ColorAttackTile();
            }
        }
    }

    //Se llama desde el LevelManager. Al final del showAction se encarga de mostrar el tile al que va a atacar
    public override void ColorAttackTile()
    {
        //El +2 es porque pathToObjective tiene en cuenta tanto el tile inicial (ocupado por goblin) como el final (ocupado por player)
        if (pathToObjective.Count > 0 && pathToObjective.Count <= movementUds + 2 && myCurrentObjective != null)
        {
            wereTilesAlreadyUnderAttack.Add(myCurrentObjectiveTile.isUnderAttack);

            tilesAlreadyUnderAttack.Add(myCurrentObjectiveTile);

            myCurrentObjectiveTile.ColorAttack();
        }
    }

    //Bool que indica si almenos una de las unidades encontradas en rango de acción es un player
    bool keepSearching;

    //Esta función sirve para que busque los objetivos a atacar pero sin que haga cambios en el turn state del enemigo
    public override void SearchingObjectivesToAttackShowActionPathFinding()
    {

        myCurrentObjective = null;
        myCurrentObjectiveTile = null;

        //Si no ha sido alertado compruebo si hay players al alcance que van a hacer que se despierte y se mueva
        if (!haveIBeenAlerted)
        {
            //Comprobar las unidades que hay en mi rango de acción
            unitsInRange = LM.TM.GetAllUnitsInRangeWithoutPathfinding(rangeOfAction, GetComponent<UnitBase>());

            for (int i = 0; i < unitsInRange.Count; i++)
            {
                if (unitsInRange[i].GetComponent<PlayerUnit>())
                {
                    keepSearching = true;
                    currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());
                    break;
                }
            }
        }

        //Si ha sido alertado compruebo simplemente hacia donde se va a mover
        else
        {
            //Determinamos el enemigo más cercano.
            //currentUnitsAvailableToAttack = LM.TM.OnlyCheckClosestPathToPlayer();
            currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());
            //Debug.Log("Line 435 " + currentUnitsAvailableToAttack.Count);

            keepSearching = true;
        }

        if (keepSearching)
        {
            if (currentUnitsAvailableToAttack.Count == 1)
            {
                myCurrentObjective = currentUnitsAvailableToAttack[0];
                myCurrentObjectiveTile = myCurrentObjective.myCurrentTile;
            }

            //Si hay varios enemigos a la misma distancia, se queda con el que tenga más unidades adyacentes
            else if (currentUnitsAvailableToAttack.Count > 1)
            {
                //Ordeno la lista de posibles objetivos según el número de unidades dyacentes
                currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                {
                    return (b.myCurrentTile.neighboursOcuppied).CompareTo(a.myCurrentTile.neighboursOcuppied);
                });

                //Elimino a todos los objetivos de la lista que no tengan el mayor número de enemigos adyacentes
                for (int i = currentUnitsAvailableToAttack.Count - 1; i > 0; i--)
                {
                    if (currentUnitsAvailableToAttack[0].myCurrentTile.neighboursOcuppied > currentUnitsAvailableToAttack[i].myCurrentTile.neighboursOcuppied)
                    {
                        currentUnitsAvailableToAttack.RemoveAt(i);
                    }
                }

                //Si sigue habiendo varios enemigos los ordeno segun la vida
                if (currentUnitsAvailableToAttack.Count > 1)
                {

                    //Ordeno la lista de posibles objetivos de menor a mayor vida actual
                    currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                    {
                        return (a.currentHealth).CompareTo(b.currentHealth);

                    });
                }

                myCurrentObjective = currentUnitsAvailableToAttack[0];
                myCurrentObjectiveTile = myCurrentObjective.myCurrentTile;
            }
        }

        keepSearching = false;
    }

}
