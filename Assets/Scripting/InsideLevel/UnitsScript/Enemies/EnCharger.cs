﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnCharger : EnemyUnit
{
    //Referencia al tile de daño que instancia en tier 2
    [SerializeField]
    public GameObject tileDamageRef;

    //Icono que aparece sobre los tiles donde va a dejar fuego.
    [SerializeField]
    public GameObject iconFireShadow;

    [HideInInspector]
    public List<GameObject> listIconsFire;

    [SerializeField]
    private GameObject particleAttack;

    public override void SearchingObjectivesToAttack()
    {
        if (!amIBeingPossesed)
        {

            if (isDead || hasAttacked)
            {
                myCurrentEnemyState = enemyState.Ended;
                return;
            }

            //Busca enemigos en sus lineas
            CheckCharactersInLine(false, null);

            //Si esta oculto lo quito de la lista de objetivos
            for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
            {
                if (currentUnitsAvailableToAttack[i].isHidden)
                {
                    currentUnitsAvailableToAttack.RemoveAt(i);
                    i--;
                }
            }

            //Si coincide que hay varios personajes a la misma distancia, me quedo con el que tiene menos vida
            if (currentUnitsAvailableToAttack.Count > 1)
            {
                //Ordeno la lista de posibles objetivos de menor a mayor vida actual
                currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                {
                    return (a.currentHealth).CompareTo(b.currentHealth);

                });
            }

            if (currentUnitsAvailableToAttack.Count > 0)
            {
                //Resto uno para mover a la unidad al tile anterior al que está ocupado por el personaje.
                furthestAvailableUnitDistance -= 1;

                myCurrentEnemyState = enemyState.Attacking;
            }

            else
            {
                myCurrentEnemyState = enemyState.Ended;
            }
        }

        else
        {
            myCurrentObjective = null;
            myCurrentObjectiveTile = null;
            pathToObjective.Clear();
            coneTiles.Clear();
            tilesToCheck.Clear();
            currentUnitsAvailableToAttack.Clear();

            if (isDead || attackCountThisTurn >= 2)
            {
                myCurrentEnemyState = enemyState.Ended;
                return;
            }

            else
            {
                if (attackCountThisTurn >= 2)
                {
                    if (!hasMoved)
                    {
                        myCurrentEnemyState = enemyState.Moving;
                        return;
                    }

                    else
                    {
                        myCurrentEnemyState = enemyState.Ended;
                        return;
                    }
                }

                if (areaCharged)
                {
                    //Explotar área
                    Debug.Log("0.Area Explota");
                    DoAreaAttack();

                    areaCharged = false;
                    attackCountThisTurn++;

                    //bossPortrait.FlipAttackTokens();
                }

                #region EXCLUSIVE_DARKLORD_CODE

                //Nunca lo tiene que hacer porque no va a poseer
                //if (currentlyPossesing)
                //{
                //    //Resto al contador para explotar al enemigo
                //    Debug.Log("Aqui tengo que restar para explotar al enemigo");
                //    myCurrentEnemyState = enemyState.Ended;
                //    return;
                //}

                /////Comprueba si puede hacer el traspaso de alma
                //if (amITheOriginalDarkLord && currentCooldownSoulSkill <= 0 && LM.enemiesOnTheBoard.Count > 1 && !LM.enemiesOnTheBoard[1].isDead)
                //{
                //    ///Haz traspaso de alma
                //    Debug.Log("0.5 Traspaso de alma");
                //    DoSoulAttack();
                //    myCurrentEnemyState = enemyState.Ended;
                //    return;
                //}

                #endregion

                //Como no puedo hacer traspaso, compruebo que ataques puedo hacer
                if (CheckCono())
                {
                    Debug.Log("1.Cono");
                    DoConeAttack();

                    coneUsed = true;
                    attackCountThisTurn++;

                    CallWaitCoroutine();
                    return;
                }

                //Si he usado el cono lo primero que compruebo es si puedo hacer el área
                if (coneUsed)
                {
                    if (CheckArea())
                    {
                        //Do area
                        Debug.Log("1.5. Área");
                        DoAreaAttack();

                        attackCountThisTurn++;

                        CallWaitCoroutine();
                        return;
                    }
                }

                //No se puede poner else porque puede no usar el cono y el área o no 
                if (CheckNormal())
                {
                    //Do físico
                    Debug.Log("2. Físico");
                    DoNormalAttack();

                    normalAttackUsed = true;
                    attackCountThisTurn++;

                    CallWaitCoroutine();
                    return;
                }

                if (CheckArea())
                {
                    //Do Área
                    Debug.Log("2.5 Area");
                    DoAreaAttack();

                    attackCountThisTurn++;

                    CallWaitCoroutine();
                    return;
                }

                else if (normalAttackUsed)
                {
                    //Do Stun
                    Debug.Log("3. Stun");
                    DoStunAttack();

                    attackCountThisTurn++;

                    CallWaitCoroutine();
                    return;
                }

                else if (!hasMoved)
                {
                    Debug.Log("6. Movimiento");
                    currentUnitsAvailableToAttack.Clear();
                    //tilesToCheck.Clear();
                    //coneTiles.Clear();

                    ///Comprueba si se ha movido (si no, se mueve y repite todas las comprobaciones menos el traspaso)

                    //Determinamos el enemigo más cercano.
                    currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());

                    //Si esta oculto lo quito de la lista de objetivos
                    for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
                    {
                        if (currentUnitsAvailableToAttack[i].isHidden)
                        {
                            currentUnitsAvailableToAttack.RemoveAt(i);
                            i--;
                        }
                    }

                    //Si no hay enemigos termina su turno
                    if (currentUnitsAvailableToAttack.Count == 0)
                    {
                        myCurrentEnemyState = enemyState.Ended;
                        return;
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
                        //myCurrentEnemyState = enemyState.Attacking;
                    }
                }

                else
                {
                    Debug.Log("Ended with: " + attackCountThisTurn + " attackCountThisTurn");

                    myCurrentEnemyState = enemyState.Ended;
                    return;
                }
            }
        }

    }

    //HACER COMO EN LA BALISTA Y OPTIMIZAR EL CHECK PARA QUE GUARDE LOS TILES QUE AFECTA EN UNA LISTA EN VEZ DE ESTAR BUSCANDO EN TILES EN LINEA TODO EL RATO.
    //JORGE RECUERDA MIRAR ESTO----------------------------------------------------------------------------------------------
    public override void Attack()
    {
        //Si no he sido alertado, activo mi estado de alerta.
        if (!haveIBeenAlerted)
        {
            AlertEnemy();
        }

        movementParticle.SetActive(true);

        //Importante este clear no puede ir dentro de SpecialCheckRotation();
        pathToObjective.Clear();
        SpecialCheckRotation(myCurrentTile, true);

        //Seteo la rotación decidida en SpecialCheckRotation();
        unitModel.transform.DORotate(rotationChosenAfterMovement, timeDurationRotation);
        currentFacingDirection = facingDirectionAfterMovement;

        StartCoroutine("MovingUnitAnimation");
    }

    //ESTE ES DIFERENTE AL DEL ENEMY UNIT PORQUE HABRÍA QUE CAMBIAR VARIAS COSAS DE LA LÓGICA PARA QUE FUNCIONASE EL OTRO.
    //La balista y el charger al ser de los primeros y ser bastante distintos voy a intentar no tocarlos mucho.
    new IEnumerator MovingUnitAnimation()
    {
        particleAttack.SetActive(true);

        myCurrentEnemyState = enemyState.Waiting;

        //Animación de movimiento
        for (int j = 0; j < pathToObjective.Count; j++)
        {
            //Calcula el vector al que se tiene que mover.
            currentTileVectorToMove = pathToObjective[j].transform.position;

            //Muevo y roto a la unidad
            transform.DOMove(currentTileVectorToMove, currentTimeForMovement);

            //Espera entre casillas
            yield return new WaitForSeconds(currentTimeForMovement);

            //Si es tier 2 instancia fuego
            if (myTierLevel == TierLevel.Level2)
            {
                if (j > 0)
                {
                    Instantiate(tileDamageRef, pathToObjective[j - 1].transform.position, tileDamageRef.transform.rotation);
                }
            }
        }

        base.Attack();

        //Actualizo toda la información al terminar de moverme
        hasMoved = true;
        movementParticle.SetActive(false);

        if (furthestAvailableUnitDistance >= 0)
        {
            UpdateInformationAfterMovement(pathToObjective[furthestAvailableUnitDistance]);
        }

        //Hago daño a la unidad
        DoDamage(currentUnitsAvailableToAttack[0]);

        //Push
        if (currentFacingDirection == FacingDirection.North)
        {
            currentUnitsAvailableToAttack[0].ExecutePush(1, myCurrentTile.tilesInLineUp, damageMadeByPush, damageMadeByFall);
        }

        else if (currentFacingDirection == FacingDirection.South)
        {
            currentUnitsAvailableToAttack[0].ExecutePush(1, myCurrentTile.tilesInLineDown, damageMadeByPush, damageMadeByFall);
        }

        else if (currentFacingDirection == FacingDirection.East)
        {
            currentUnitsAvailableToAttack[0].ExecutePush(1, myCurrentTile.tilesInLineRight, damageMadeByPush, damageMadeByFall);
        }

        else if (currentFacingDirection == FacingDirection.West)
        {
            currentUnitsAvailableToAttack[0].ExecutePush(1, myCurrentTile.tilesInLineLeft, damageMadeByPush, damageMadeByFall);
        }

        particleAttack.SetActive(false);

        myCurrentEnemyState = enemyState.Ended;
    }

    public override void FinishMyActions()
    {
        base.FinishMyActions();

        if (amIBeingPossesed)
        {
            attackCountThisTurn = 0;
            coneUsed = false;
            normalAttackUsed = false;
        }
    }

    //Función que pinta o despinta los tiles a los que está atcando la ballesta
    public void FeedbackTilesToAttack(bool shouldColorTiles)
    {
       
        pathToObjective.Clear();    
        myLineRenderer.positionCount = 0;     
     
        if (currentUnitsAvailableToAttack.Count > 0){

            //Arriba o abajo
            if (currentUnitsAvailableToAttack[0].myCurrentTile.tileX == myCurrentTile.tileX)
            {
                //Arriba
                if (currentUnitsAvailableToAttack[0].myCurrentTile.tileZ > myCurrentTile.tileZ)
                {
                    for (int i = 0; i <= furthestAvailableUnitDistance; i++)
                    {
                        if (myCurrentTile.tilesInLineUp[i].unitOnTile != null)
                        {
                            return;
                        }

                        pathToObjective.Add(myCurrentTile.tilesInLineUp[i]);

                        if (shouldColorTiles)
                        {
                            myLineRenderer.enabled = true;

                            myCurrentTile.tilesInLineUp[i].ColorAttack();
                           
                            sombraHoverUnit.transform.DORotate(new Vector3(0, 0, 0), 0);                         
                        }
                        else
                        {
                            myLineRenderer.enabled = false;
                            myCurrentTile.tilesInLineUp[i].ColorDesAttack();
                        }
                    }
                }
                //Abajo
                else
                {
                    for (int i = 0; i <= furthestAvailableUnitDistance; i++)
                    {
                        if (myCurrentTile.tilesInLineDown[i].unitOnTile != null)
                        {
                            return;
                        }

                        pathToObjective.Add(myCurrentTile.tilesInLineDown[i]);

                        if (shouldColorTiles)
                        {
                            myLineRenderer.enabled = true;

                            myCurrentTile.tilesInLineDown[i].ColorAttack();
                            sombraHoverUnit.transform.DORotate(new Vector3(0, 180, 0), 0);                            
                        }
                        else
                        {
                            myLineRenderer.enabled = false;
                            myCurrentTile.tilesInLineDown[i].ColorDesAttack();
                        }
                    }

                }
            }
            //Izquierda o derecha
            else
            {
                //Derecha
                if (currentUnitsAvailableToAttack[0].myCurrentTile.tileX > myCurrentTile.tileX)
                {
                    for (int i = 0; i <= furthestAvailableUnitDistance; i++)
                    {
                        if (myCurrentTile.tilesInLineRight[i].unitOnTile != null)
                        {
                            return;
                        }

                        pathToObjective.Add(myCurrentTile.tilesInLineRight[i]);

                        if (shouldColorTiles)
                        {
                            myLineRenderer.enabled = true;
                            myCurrentTile.tilesInLineRight[i].ColorAttack();
                            sombraHoverUnit.transform.DORotate(new Vector3(0, 90, 0), 0);

                          
                        }
                        else
                        {
                            myLineRenderer.enabled = false;
                            myCurrentTile.tilesInLineRight[i].ColorDesAttack();
                        }
                    }
                }
                //Izquierda
                else
                {
                    for (int i = 0; i <= furthestAvailableUnitDistance; i++)
                    {
                        if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null)
                        {
                            return;
                        }

                        pathToObjective.Add(myCurrentTile.tilesInLineLeft[i]);

                        if (shouldColorTiles)
                        {
                            myLineRenderer.enabled = true;
                            myCurrentTile.tilesInLineLeft[i].ColorAttack();
                            sombraHoverUnit.transform.DORotate(new Vector3(0, -90, 0), 0);

                           


                        }
                        else
                        {
                            myLineRenderer.enabled = false;
                            myCurrentTile.tilesInLineLeft[i].ColorDesAttack();
                        }
                    }
                }
            }
        }

        else if (currentUnitsAvailableToAttack.Count == 0)
        {
            if (myCurrentTile.tilesInLineUp.Count > 0)
            {                                           
                for (int i = 0; i < myCurrentTile.tilesInLineUp.Count; i++)
                {
                    if (shouldColorTiles)
                    {
                       
                        
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.tilesInLineUp[i - 1].height) > maxHeightDifferenceToAttack 
                            || (myCurrentTile.tilesInLineUp[i].isObstacle)
                            || (myCurrentTile.tilesInLineUp[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineUp[i].ColorAttack();
                    }
                    else
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.tilesInLineUp[i - 1].height) > maxHeightDifferenceToAttack
                              || (myCurrentTile.tilesInLineUp[i].isObstacle)
                              || (myCurrentTile.tilesInLineUp[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineUp[i].ColorDesAttack();
                    }
                }
            }

            if (myCurrentTile.tilesInLineDown.Count > 0)
            {
                for (int i = 0; i < myCurrentTile.tilesInLineDown.Count; i++)
                {
                    if (shouldColorTiles)
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.tilesInLineDown[i - 1].height) > maxHeightDifferenceToAttack 
                            || (myCurrentTile.tilesInLineDown[i].isObstacle)
                             || (myCurrentTile.tilesInLineDown[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineDown[i].ColorAttack();
                    }
                    else
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.tilesInLineDown[i - 1].height) > maxHeightDifferenceToAttack
                              || (myCurrentTile.tilesInLineDown[i].isObstacle)
                               || (myCurrentTile.tilesInLineDown[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineDown[i].ColorDesAttack();
                    }
                }

            }

            if (myCurrentTile.tilesInLineRight.Count > 0)
            {

                for (int i = 0; i < myCurrentTile.tilesInLineRight.Count; i++)
                {
                    if (shouldColorTiles)
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.tilesInLineRight[i - 1].height) > maxHeightDifferenceToAttack 
                            || (myCurrentTile.tilesInLineRight[i].isObstacle)
                            || (myCurrentTile.tilesInLineRight[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineRight[i].ColorAttack();
                    }
                    else
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.tilesInLineRight[i - 1].height) > maxHeightDifferenceToAttack
                              || (myCurrentTile.tilesInLineRight[i].isObstacle)
                              || (myCurrentTile.tilesInLineRight[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineRight[i].ColorDesAttack();
                    }
                }

            }

            if (myCurrentTile.tilesInLineLeft.Count > 0)
            {

                for (int i = 0; i < myCurrentTile.tilesInLineLeft.Count; i++)
                {
                    if (shouldColorTiles)
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.tilesInLineLeft[i - 1].height) > maxHeightDifferenceToAttack 
                           || (myCurrentTile.tilesInLineLeft[i].isObstacle)
                           || (myCurrentTile.tilesInLineLeft[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineLeft[i].ColorAttack();
                    }
                    else
                    {
                        if (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.tilesInLineLeft[i - 1].height) > maxHeightDifferenceToAttack
                              || (myCurrentTile.tilesInLineLeft[i].isObstacle)
                              || (myCurrentTile.tilesInLineLeft[i].isEmpty))
                        {
                            break;
                        }
                        myCurrentTile.tilesInLineLeft[i].ColorDesAttack();
                    }
                }
            }
        }
    }

    public List<IndividualTiles> tilesBehindObjective = new List<IndividualTiles>();

    //Esta dirección no se aplica al charger a nivel lógico, simplemente me sirve para saber en que dirección esta el objetivo.
    FacingDirection temporalFacingDirectionWhileHover;

    //El bool y el tile es solo para la balista pero comparte la funcion con el charger y tiene que tenerlo
    public override void CheckCharactersInLine(bool _NoUsarEsteBooleano, IndividualTiles _noUsarEsteTile)
    {
        if (!isDead)
        {
            if (myCurrentTile == null)
            {
                InitializeUnitOnTile();
            }

            tilesBehindObjective.Clear();
            currentUnitsAvailableToAttack.Clear();

            //Busco objetivos en los tiles de arriba

            //Seteo número de tiles a comprobar en función del rango y del número de tiles disponibles
            if (rangeOfAction <= myCurrentTile.tilesInLineUp.Count)
            {
                rangeVSTilesInLineLimitant = rangeOfAction;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineUp.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                //Paro de comprobar si hay un obstáculo, un tile vacío o una unidad enemiga.
                if (myCurrentTile.tilesInLineUp[i].isObstacle   ||
                    myCurrentTile.tilesInLineUp[i].isEmpty      ||
                    (myCurrentTile.tilesInLineUp[i].unitOnTile != null && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<EnemyUnit>()) ||
                    (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.tilesInLineUp[i - 1].height) > maxHeightDifferenceToMove))
                {
                    break;
                }

                //Si por el contrario encuentro una unidad del jugador a mi altura, la añado a la lista de objetivos (en el resto de direcciones antes compruebo si es la unidad más lejana)
                else if (myCurrentTile.tilesInLineUp[i].unitOnTile != null && myCurrentTile.tilesInLineUp[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    //Almaceno la primera unidad en la lista de posibles unidades.
                    currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                    furthestAvailableUnitDistance = i;

                    if (i+1 < myCurrentTile.tilesInLineUp.Count)
                    {
                        tilesBehindObjective.Add(myCurrentTile.tilesInLineUp[i+1]);
                    }


                    temporalFacingDirectionWhileHover = FacingDirection.North;

                    //Break ya que sólo me interesa la primera unidad de la linea
                    break;
                }
            }

            //Tiles derecha
            if (rangeOfAction <= myCurrentTile.tilesInLineRight.Count)
            {
                rangeVSTilesInLineLimitant = rangeOfAction;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineRight.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineRight[i].isObstacle ||
                    myCurrentTile.tilesInLineRight[i].isEmpty ||
                    (myCurrentTile.tilesInLineRight[i].unitOnTile != null && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<EnemyUnit>()) ||
                    (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.tilesInLineRight[i - 1].height) > maxHeightDifferenceToMove))
                {
                    break;
                }

                else if (myCurrentTile.tilesInLineRight[i].unitOnTile != null && myCurrentTile.tilesInLineRight[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Compruebo que unidad está más lejos
                    if (currentUnitsAvailableToAttack.Count == 0 || furthestAvailableUnitDistance < i)
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

                    if (i + 1 < myCurrentTile.tilesInLineRight.Count)
                    {
                        tilesBehindObjective.Add(myCurrentTile.tilesInLineRight[i + 1]);
                    }

                    temporalFacingDirectionWhileHover = FacingDirection.East;

                    //Break ya que sólo me interesa la primera unidad de la linea
                    break;
                }
            }

            //Tiles abajo
            if (rangeOfAction <= myCurrentTile.tilesInLineDown.Count)
            {
                rangeVSTilesInLineLimitant = rangeOfAction;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineDown.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineDown[i].isObstacle ||
                    myCurrentTile.tilesInLineDown[i].isEmpty ||
                    (myCurrentTile.tilesInLineDown[i].unitOnTile != null && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<EnemyUnit>()) ||
                    (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.tilesInLineDown[i - 1].height) > maxHeightDifferenceToMove))
                {
                    break;
                }

                else if (myCurrentTile.tilesInLineDown[i].unitOnTile != null && myCurrentTile.tilesInLineDown[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Compruebo que unidad está más lejos
                    if (currentUnitsAvailableToAttack.Count == 0 || furthestAvailableUnitDistance < i)
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

                    if (i + 1 < myCurrentTile.tilesInLineDown.Count)
                    {
                        tilesBehindObjective.Add(myCurrentTile.tilesInLineDown[i + 1]);
                    }

                    temporalFacingDirectionWhileHover = FacingDirection.South;

                    //Break ya que sólo me interesa la primera unidad de la linea
                    break;
                }
            }

            //Tiles abajo
            if (rangeOfAction <= myCurrentTile.tilesInLineLeft.Count)
            {
                rangeVSTilesInLineLimitant = rangeOfAction;
            }
            else
            {
                rangeVSTilesInLineLimitant = myCurrentTile.tilesInLineLeft.Count;
            }

            for (int i = 0; i < rangeVSTilesInLineLimitant; i++)
            {
                if (myCurrentTile.tilesInLineLeft[i].isObstacle ||
                   myCurrentTile.tilesInLineLeft[i].isEmpty ||
                   (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<EnemyUnit>()) ||
                   (i > 0 && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.tilesInLineLeft[i - 1].height) > maxHeightDifferenceToMove))
                {
                    break;
                }

                else if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null && myCurrentTile.tilesInLineLeft[i].unitOnTile.GetComponent<PlayerUnit>() && Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack)
                {
                    //Compruebo que unidad está más lejos
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

                    if (i + 1 < myCurrentTile.tilesInLineLeft.Count)
                    {
                        tilesBehindObjective.Add(myCurrentTile.tilesInLineLeft[i + 1]);
                    }

                    temporalFacingDirectionWhileHover = FacingDirection.West;

                    //Break ya que sólo me interesa la primera unidad de la linea
                    break;
                }
            }

            //Si coincide que hay varios personajes a la misma distancia, me quedo con el que tiene menos vida
            if (currentUnitsAvailableToAttack.Count > 1)
            {
                //Ordeno la lista de posibles objetivos de menor a mayor vida actual
                currentUnitsAvailableToAttack.Sort(delegate (UnitBase a, UnitBase b)
                {
                    return (a.currentHealth).CompareTo(b.currentHealth);

                });
            }
        }
    }

    IndividualTiles tileWhereObjectiveShadowWillEnd;

    //Tiene que ser count-1 excepto cuando count es 1
    IndividualTiles pathToObjectiveTileReference;

    public void ShowPushResult()
    {
        if (pathToObjective.Count > 0)
        {
            pathToObjectiveTileReference = pathToObjective[pathToObjective .Count - 1];   
        }

        else
        {
            pathToObjectiveTileReference = myCurrentTile;
        }
       
        currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(true);

        //COMPROBAR
        SetShadowRotation(currentUnitsAvailableToAttack[0], currentUnitsAvailableToAttack[0].myCurrentTile, myCurrentTile);

        if (temporalFacingDirectionWhileHover == FacingDirection.North)
        {
            if (CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineUp, damageMadeByPush, damageMadeByFall) == currentUnitsAvailableToAttack[0].myCurrentTile) 
            {
                currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(false);
            }
            else
            {
                tileWhereObjectiveShadowWillEnd = CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineUp, damageMadeByPush, damageMadeByFall);

                currentUnitsAvailableToAttack[0].sombraHoverUnit.transform.position = tileWhereObjectiveShadowWillEnd.transform.position;

                tileWhereObjectiveShadowWillEnd.ColorAttack();
            }
         
        }

        else if (temporalFacingDirectionWhileHover == FacingDirection.South)
        {
            if (CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineDown, damageMadeByPush, damageMadeByFall) == currentUnitsAvailableToAttack[0].myCurrentTile)
            {
                currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(false);
            }

            else
            {
                tileWhereObjectiveShadowWillEnd = CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineDown, damageMadeByPush, damageMadeByFall);
                currentUnitsAvailableToAttack[0].sombraHoverUnit.transform.position = tileWhereObjectiveShadowWillEnd.transform.position;
                tileWhereObjectiveShadowWillEnd.ColorAttack();
            }   
        }

        else if (temporalFacingDirectionWhileHover == FacingDirection.East)
        {
            if (CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineRight, damageMadeByPush, damageMadeByFall) == currentUnitsAvailableToAttack[0].myCurrentTile)
            {
                currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(false);
            }
            else
            {
                tileWhereObjectiveShadowWillEnd = CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineRight, damageMadeByPush, damageMadeByFall);
                currentUnitsAvailableToAttack[0].sombraHoverUnit.transform.position = tileWhereObjectiveShadowWillEnd.transform.position;
                tileWhereObjectiveShadowWillEnd.ColorAttack();
            }
           
        }

        else if (temporalFacingDirectionWhileHover == FacingDirection.West)
        {
            if (CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineLeft, damageMadeByPush, damageMadeByFall) == currentUnitsAvailableToAttack[0].myCurrentTile)
            {
                currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(false);
            }
            else
            {
                tileWhereObjectiveShadowWillEnd = CalculatePushLogic(1, pathToObjectiveTileReference.tilesInLineLeft, damageMadeByPush, damageMadeByFall);
                currentUnitsAvailableToAttack[0].sombraHoverUnit.transform.position = tileWhereObjectiveShadowWillEnd.transform.position;
                tileWhereObjectiveShadowWillEnd.ColorAttack();
            }
        }
    }

    public void HideShowResult()
    {
        if (currentUnitsAvailableToAttack.Count >0 )
        {
            currentUnitsAvailableToAttack[0].sombraHoverUnit.SetActive(false);
            if(tileWhereObjectiveShadowWillEnd!= null)
            {
                tileWhereObjectiveShadowWillEnd.ColorDesAttack();
            }
        }

        //Oculto todo el feedback de ataque del enemigo empujado y de los que reciben el choque
        for (int i = 0; i < enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem.Count; i++)
        {
            enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem[i].ResetColor();
            enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem[i].DisableCanvasHover();
            enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem[i].myCurrentTile.ColorDesAttack();
            enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem[i].hoverImpactIcon.SetActive(false);
        }

        for (int i = 0; i < enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies.Count; i++)
        {
            enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies[i].ResetColor();
            enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies[i].DisableCanvasHover();
            enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies[i].myCurrentTile.ColorDesAttack();
            enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies[i].hoverImpactIcon.SetActive(false);
        }

        enemiesThatHaveBeenDamageBecauseOfBeingPushedAgainstThem.Clear();
        enemiesThatHaveBeenDamageBecauseHaveBeenPushAgainstObstaclesOrEnemies.Clear();
    }

    //Esta función sirve para que busque los objetivos a atacar pero sin que haga cambios en el turn state del enemigo
    public override void SearchingObjectivesToAttackShowActionPathFinding()
    {
        if (!amIBeingPossesed)
        {
            myLineRenderer.positionCount += (pathToObjective.Count);
            for (int j = 0; j < pathToObjective.Count; j++)
            {
                Vector3 pointPosition = new Vector3(pathToObjective[j].transform.position.x, pathToObjective[j].transform.position.y + 0.5f, pathToObjective[j].transform.position.z);
                if (j < pathToObjective.Count)
                {
                    if (j == 0)
                    {
                        myLineRenderer.SetPosition(0, myCurrentTile.transform.position);
                    }
                    else
                    {
                        myLineRenderer.SetPosition(j, pointPosition);
                    }

                }
            }
        }
    }

    public void InstantiateIconsFire()
    {
        for (int i = 0; i < pathToObjective.Count; i++)
        {
            GameObject icon = Instantiate(iconFireShadow);
            icon.transform.position = new Vector3(pathToObjective[i].transform.position.x, pathToObjective[i].transform.position.y + 0.5f, pathToObjective[i].transform.position.z);
            listIconsFire.Add(icon);

        }
    }

    public void RemoveIconsFire()
    {
        for (int i = 0; i < listIconsFire.Count; i++)
        {
            Destroy(listIconsFire[i]);
        }

        listIconsFire.Clear();
    }

    #region DARK_LORD

    #region CHECK_ATTACK_TO_CHOOSE

    bool CheckArea()
    {
        currentUnitsAvailableToAttack.Clear();
        tilesToCheck.Clear();

        //Guardo los tiles que rodean al señor oscuro
        tilesToCheck = LM.TM.GetSurroundingTiles(myCurrentTile, 1, true, false);

        for (int i = 0; i < tilesToCheck.Count; i++)
        {
            if (tilesToCheck[i].unitOnTile != null && tilesToCheck[i].unitOnTile.GetComponent<PlayerUnit>())
            {
                currentUnitsAvailableToAttack.Add(tilesToCheck[i].unitOnTile);
            }
        }

        //Si esta oculto lo quito de la lista de objetivos
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            if (currentUnitsAvailableToAttack[i].isHidden)
            {
                currentUnitsAvailableToAttack.RemoveAt(i);
                i--;
            }
        }

        ///Comprueba si tiene + de 1 objetivo para hacer área
        if (currentUnitsAvailableToAttack.Count > 1)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    bool CheckNormal()
    {
        currentUnitsAvailableToAttack.Clear();
        tilesToCheck.Clear();

        if (normalAttackUsed)
        {
            return false;
        }

        else
        {
            //Guardo los dos tiles en frente del personaje
            tilesToCheck = myCurrentTile.GetTilesInFrontOfTheCharacter(currentFacingDirection, normalAttackRange);

            //Tengo que pintarlo en otro for, porque el siguiente hace break
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                tilesToPaint.Add(tilesToCheck[i]);
                tilesToCheck[i].ColorAttack();
            }

            //Compruebo si en los 2 tiles de delante hay al menos un enemigo
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                if (tilesToCheck[i].unitOnTile != null &&
                    tilesToCheck[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    currentUnitsAvailableToAttack.Add(tilesToCheck[i].unitOnTile);
                    Debug.Log("El primer enemigo a mi alcance es" + currentUnitsAvailableToAttack[0]);
                    break;
                }
            }

            //Si esta oculto lo quito de la lista de objetivos
            for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
            {
                if (currentUnitsAvailableToAttack[i].isHidden)
                {
                    currentUnitsAvailableToAttack.RemoveAt(i);
                    i--;
                }
            }

            if (currentUnitsAvailableToAttack.Count >= 1)
            {
                return true;
            }

            else
            {
                for (int i = 0; i < tilesToCheck.Count; i++)
                {
                    tilesToPaint.Remove(tilesToCheck[i]);
                    tilesToCheck[i].ColorDesAttack();
                }

                return false;
            }
        }
    }

    bool CheckCono()
    {
        currentUnitsAvailableToAttack.Clear();
        tilesToCheck.Clear();
        coneTiles.Clear();

        if (coneUsed)
        {
            return false;
        }

        else
        {
            //Guardo los tiles de la línea central del cono
            tilesToCheck = myCurrentTile.GetTilesInFrontOfTheCharacter(currentFacingDirection, coneRange);

            //Guardo todos los tiles del cono
            coneTiles = LM.TM.GetConeTiles(tilesToCheck, currentFacingDirection);

            //Compruebo cada tile del área del cono en busca de personajes
            for (int i = 0; i < coneTiles.Count; i++)
            {
                tilesToPaint.Add(coneTiles[i]);
                coneTiles[i].ColorAttack();

                if (coneTiles[i].unitOnTile != null &&
                    coneTiles[i].unitOnTile.GetComponent<PlayerUnit>())
                {
                    currentUnitsAvailableToAttack.Add(coneTiles[i].unitOnTile);
                }
            }

            //Si esta oculto lo quito de la lista de objetivos
            for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
            {
                if (currentUnitsAvailableToAttack[i].isHidden)
                {
                    currentUnitsAvailableToAttack.RemoveAt(i);
                    i--;
                }
            }

            //Si hay al menos 2 unidades en rango de cono
            if (currentUnitsAvailableToAttack.Count > 1)
            {
                return true;
            }

            //Si hay sólo 1 unidad pero no está en el rango del ataque normal hago el cono
            else if (currentUnitsAvailableToAttack.Count == 1 && !CheckNormal())
            {
                return true;
            }

            //Si no hay nadie o sólo hay 1 en rango de normal NO HAGO CONO
            else
            {
                for (int i = 0; i < coneTiles.Count; i++)
                {
                    tilesToPaint.Remove(coneTiles[i]);
                    coneTiles[i].ColorDesAttack();
                }

                return false;
            }
        }
    }

    #endregion

    #region ATTACKS

    private void DoAreaAttack()
    {
        //Ataque
        if (areaCharged)
        {
            //Tiles
            for (int i = 0; i < tilesInArea.Count; i++)
            {
                //AQUI FEEDBACK ATAQUE (PARTÍCULAS)


                //Quitar feedback tiles
                tilesInArea[i].ColorDesAttack();

                //Daño
                if (tilesInArea[i].unitOnTile != null)
                {
                    DoDamage(tilesInArea[i].unitOnTile);
                }
            }

            tilesInArea.Clear();
            areaCharged = false;
        }

        //Carga
        else
        {
            for (int i = 0; i < tilesToCheck.Count; i++)
            {
                //Feedback tiles cargados
                tilesToCheck[i].ColorAttack();
                tilesInArea.Add(tilesToCheck[i]);
            }

            areaCharged = true;
        }
    }

    private void DoNormalAttack()
    {
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            DoDamage(currentUnitsAvailableToAttack[i]);
        }
    }

    private void DoConeAttack()
    {
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            tilesListToPull.Clear();
            DoDamage(currentUnitsAvailableToAttack[i]);

            #region CheckPullDirection
            //La función para empujar excluye el primer tile por lo que hay que añadir el tile en el que esta la unidad y luego ya coger la lsita fcon los tiles en esa dirección

            if (currentFacingDirection == FacingDirection.North)
            {
                tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile);

                for (int j = 0; j < currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineDown.Count; j++)
                {
                    tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineDown[j]);
                }
            }

            if (currentFacingDirection == FacingDirection.South)
            {
                tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile);

                for (int j = 0; j < currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineUp.Count; j++)
                {
                    tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineUp[j]);
                }
            }

            if (currentFacingDirection == FacingDirection.East)
            {
                tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile);

                for (int j = 0; j < currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineLeft.Count; j++)
                {
                    tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineLeft[j]);
                }
            }

            if (currentFacingDirection == FacingDirection.West)
            {
                tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile);

                for (int j = 0; j < currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineRight.Count; j++)
                {
                    tilesListToPull.Add(currentUnitsAvailableToAttack[i].myCurrentTile.tilesInLineRight[j]);
                }
            }

            currentUnitsAvailableToAttack[i].ExecutePush(1, tilesListToPull, damageMadeByPush, damageMadeByFall);

            #endregion

        }
    }

    private void DoStunAttack()
    {
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            // Stun (currentUnitsAvailableToAttack[i]);
        }

        Debug.Log("AQUI FALTA FUNCIÓN DE STUN");
    }

    #endregion

    private void CallWaitCoroutine()
    {
        //bossPortrait.FlipAttackTokens();
        //Salgo de la comprobación de acciones para volver a empezar
        StartCoroutine("WaitBeforeNextAction");
        myCurrentEnemyState = enemyState.Waiting;
    }

    IEnumerator WaitBeforeNextAction()
    {
        yield return new WaitForSeconds(2f);

        //Limpiar tiles de ataque anteriores

        for (int i = 0; i < tilesToPaint.Count; i++)
        {
            tilesToPaint[i].ColorDesAttack();
        }

        tilesToPaint.Clear();
        tilesToCheck.Clear();
        coneTiles.Clear();

        myCurrentEnemyState = enemyState.Searching;
    }

    //ESTAS DOS FUNCIONES TIENEN OVERRIDE PARA QUE NO SE HAGAN SI ESTA SIENDO POSEIDO

    public override void ShowActionPathFinding(bool _shouldRecalculate)
    {
        //Solo muestro feedback si no estoy siendo poseido
        if (!amIBeingPossesed)
        {
            base.ShowActionPathFinding(_shouldRecalculate);
        }
    }

    #endregion

}
