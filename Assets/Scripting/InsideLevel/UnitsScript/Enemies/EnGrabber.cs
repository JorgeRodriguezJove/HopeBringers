﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnGrabber : EnemyUnit
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

    public int turn2StunEnemy;

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
                    myCurrentEnemyState = enemyState.Attacking;
                    return;
                }
            }

            //Si llega hasta aqui significa que no había personajes en rango y termina
            myCurrentEnemyState = enemyState.Ended;
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
                    //Añado esto para eliminar a los personajes ocultos
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
                LM.TM.CalculatePathForMovementCost(myCurrentObjectiveTile.tileX, myCurrentObjectiveTile.tileZ, false, false);

                //No vale con igualar pathToObjective= LM.TM.currentPath porque entonces toma una referencia de la variable no de los valores.
                //Esto significa que si LM.TM.currentPath cambia de valor también lo hace pathToObjective
                for (int i = 0; i < LM.TM.currentPath.Count; i++)
                {
                    pathToObjective.Add(LM.TM.currentPath[i]);
                }

                myCurrentEnemyState = enemyState.Attacking;
            }
        }
    }

    public override void Attack()
    {
         
        //Si no he sido alertado, activo mi estado de alerta.
        //Al alertarme salo del void de ataque para hacer la busqueda normal de jugadores.
        if (!haveIBeenAlerted)
        {
            AlertEnemy();
            myCurrentEnemyState = enemyState.Searching;
            return;
        }

        CheckUnitToAttack();
        base.Attack();

        if (currentUnitsAvailableToAttack.Count == 0)
        {
            if (!hasMoved && !hasAttacked)
            {
                myCurrentEnemyState = enemyState.Moving;
            }

            else
            {
                myCurrentEnemyState = enemyState.Ended;

                //Espero 1 sec y cambio de estado a ended
                //StartCoroutine("AttackWait");
            }
        }
        else
        {

            if (!hasAttacked)
            {
                ColorAttackTile();

                if (currentFacingDirection == FacingDirection.North)
                {
                    //Calcula el vector al que se tiene que mover.
                    currentTileVectorToMove = myCurrentTile.tilesInLineUp[0].transform.position;
                    currentUnitsAvailableToAttack[0].transform.DOMove(currentTileVectorToMove, 0.1f);
                    currentUnitsAvailableToAttack[0].UpdateInformationAfterMovement(myCurrentTile.tilesInLineUp[0]);
                }

                else if (currentFacingDirection == FacingDirection.South)
                {
                    //Calcula el vector al que se tiene que mover.
                    currentTileVectorToMove = myCurrentTile.tilesInLineDown[0].transform.position;
                    currentUnitsAvailableToAttack[0].transform.DOMove(currentTileVectorToMove, 0.1f);
                    currentUnitsAvailableToAttack[0].UpdateInformationAfterMovement(myCurrentTile.tilesInLineDown[0]);

                }

                else if (currentFacingDirection == FacingDirection.East)
                {
                    //Calcula el vector al que se tiene que mover.
                    currentTileVectorToMove = myCurrentTile.tilesInLineRight[0].transform.position;
                    currentUnitsAvailableToAttack[0].transform.DOMove(currentTileVectorToMove, 0.1f);
                    currentUnitsAvailableToAttack[0].UpdateInformationAfterMovement(myCurrentTile.tilesInLineRight[0]);


                }

                else if (currentFacingDirection == FacingDirection.West)
                {
                    //Calcula el vector al que se tiene que mover.
                    currentTileVectorToMove = myCurrentTile.tilesInLineLeft[0].transform.position;
                    currentUnitsAvailableToAttack[0].transform.DOMove(currentTileVectorToMove, 0.1f);
                    currentUnitsAvailableToAttack[0].UpdateInformationAfterMovement(myCurrentTile.tilesInLineLeft[0]);
                }

                //Atacar al enemigo
                DoDamage(currentUnitsAvailableToAttack[0]);

                if(myTierLevel== TierLevel.Level2
                    && !currentUnitsAvailableToAttack[0].isDead)
                {
                    currentUnitsAvailableToAttack[0].isStunned = true;
                    if (currentUnitsAvailableToAttack[0].turnStunned < 0)
                    {
                        currentUnitsAvailableToAttack[0].turnStunned = 0;
                    }
                    currentUnitsAvailableToAttack[0].turnStunned += turn2StunEnemy;

                }
                //Animación de ataque
                ExecuteAnimationAttack();
                hasAttacked = true;
            }
            else
            {
                myCurrentEnemyState = enemyState.Ended;
            }
        }
    }

    int limitantNumberOfTilesToMove;

    public override void MoveUnit()
    {
        limitantNumberOfTilesToMove = 0;

        movementParticle.SetActive(true);

        ShowActionPathFinding(false);

        //Como el path guarda el tile en el que esta el enemigo y el tile en el que esta el personaje del jugador resto 2.
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
        CheckTileDirection(pathToObjective[pathToObjective.Count - 1]);

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
            unitModel.transform.DOLookAt(currentTileVectorToMove, timeDurationRotation, AxisConstraint.Y);

            //Espera entre casillas
            yield return new WaitForSeconds(currentTimeForMovement);
        }

        hasMoved = true;


        //Compruebo la dirección en la que se mueve para girar a la unidad
        CheckTileDirection(pathToObjective[pathToObjective.Count - 1]);
        myCurrentEnemyState = enemyState.Searching;

        movementParticle.SetActive(false);

        HideActionPathfinding();
        //ShowActionPathFinding(false);

    }

    //MEJORAR ESTO. PROBABLEMENTE NO NECESITO DOS FUNCIONES  PARA ESTO Y ADEMÁS SE REPITE EN EL PLAYER UNIT

    //Decidir rotación al moverse por los tiles.
    public void CheckTileDirection(IndividualTiles tileToCheck)
    {
        //Arriba o abajo
        if (tileToCheck.tileX == myCurrentTile.tileX)
        {
            //Arriba
            if (tileToCheck.tileZ > myCurrentTile.tileZ)
            {
                unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.North;
            }
            //Abajo
            else
            {
                unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.South;
            }
        }
        //Izquierda o derecha
        else
        {
            //Derecha
            if (tileToCheck.tileX > myCurrentTile.tileX)
            {
                unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.East;
            }
            //Izquierda
            else
            {
                unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
                currentFacingDirection = FacingDirection.West;
            }
        }
    }

    //Decidir rotación al terminar de moverse para atacar
    private void RotateLogic(FacingDirection newDirection)
    {
        //Roto al gigante
        if (newDirection == FacingDirection.North)
        {
            unitModel.transform.DORotate(new Vector3(0, 0, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.North;
        }

        else if (newDirection == FacingDirection.South)
        {
            unitModel.transform.DORotate(new Vector3(0, 180, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.South;
        }

        else if (newDirection == FacingDirection.East)
        {
            unitModel.transform.DORotate(new Vector3(0, 90, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.East;
        }

        else if (newDirection == FacingDirection.West)
        {
            unitModel.transform.DORotate(new Vector3(0, -90, 0), timeDurationRotation);
            currentFacingDirection = FacingDirection.West;
        }
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
                LM.TM.CalculatePathForMovementCost(myCurrentObjectiveTile.tileX, myCurrentObjectiveTile.tileZ, false, false);

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

   
    public void CheckUnitToAttack()
    {
        currentUnitsAvailableToAttack.Clear();
        previousTileHeight = 0;

        if (currentFacingDirection == FacingDirection.North)
        {
            if (attackRange <= myCurrentTile.tilesInLineUp.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineUp.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineUp[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineUp[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineUp[i].unitOnTile != null && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                        break;
                    }

                    else
                    {
                        continue;
                    }
                }
                else if (myCurrentTile.tilesInLineUp[i].unitOnTile != null)
                {
                    break;
                }


                if (myCurrentTile.tilesInLineUp[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.South)
        {
            if (attackRange <= myCurrentTile.tilesInLineDown.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineDown.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineDown[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineDown[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineDown[i].unitOnTile != null && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);
                        break;
                    }

                    else
                    {
                        continue;
                    }
                }
                else if (myCurrentTile.tilesInLineDown[i].unitOnTile != null)
                {
                    break;
                }

                if (myCurrentTile.tilesInLineDown[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.East)
        {
            if (attackRange <= myCurrentTile.tilesInLineRight.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineRight.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineRight[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineRight[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineRight[i].unitOnTile != null && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);
                        break;
                    }

                    else
                    {
                        continue;
                    }
                }
                else if (myCurrentTile.tilesInLineRight[i].unitOnTile != null)
                {
                    break;
                }

                if (myCurrentTile.tilesInLineRight[i].isEmpty)
                {
                    break;
                }
            }
        }

        if (currentFacingDirection == FacingDirection.West)
        {
            if (attackRange <= myCurrentTile.tilesInLineLeft.Count)
            {
                rangeVSTilesInLineLimitant = attackRange;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineLeft.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Guardo la altura mas alta en esta linea de tiles
                if (myCurrentTile.tilesInLineLeft[i].height > previousTileHeight)
                {
                    previousTileHeight = myCurrentTile.tilesInLineLeft[i].height;
                }

                //Si hay una unidad
                if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                    if (Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                        || Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);
                        break;
                    }

                    else
                    {
                        continue;
                    }
                }
                else if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null) {
                    break;
                }

                if (myCurrentTile.tilesInLineLeft[i].isEmpty)
                {
                    break;
                }
            }

        }

        //Marco las unidades disponibles para atacar de color rojo
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            CalculateDamage(currentUnitsAvailableToAttack[i]);
            currentUnitsAvailableToAttack[i].ColorAvailableToBeAttacked(damageWithMultipliersApplied);
            
        }
    }
}