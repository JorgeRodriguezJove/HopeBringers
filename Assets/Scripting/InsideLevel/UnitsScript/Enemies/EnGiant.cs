﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnGiant : EnemyUnit
{
    //Guardo la primera unidad en la lista de currentUnitAvailbleToAttack para  no estar llamandola constantemente
    private UnitBase myCurentObjective;
    private IndividualTiles myCurrentObjectiveTile;

    // Copia de la lista del goblin que en este caso uso para que la acción del gigante solo aparezca cuando hay players a su alrededor
    [HideInInspector]
    private List<UnitBase> unitsInRange = new List<UnitBase>();

    public override void SearchingObjectivesToAttack()
    {
        if (isDead || hasAttacked)
        {
            Debug.Log("dead");
            myCurrentEnemyState = enemyState.Ended;
            return;
        }

        //Determinamos el enemigo más cercano.
        currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());

        //Si no hay enemigos termina su turno
        if (currentUnitsAvailableToAttack.Count == 0)
        {
            myCurrentEnemyState = enemyState.Ended;
        }

        else if (currentUnitsAvailableToAttack.Count == 1)
        {
            myCurentObjective = currentUnitsAvailableToAttack[0];
            myCurrentObjectiveTile = myCurentObjective.myCurrentTile;
            myCurrentEnemyState = enemyState.Attacking;
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
            for (int i = currentUnitsAvailableToAttack.Count-1; i > 0; i--)
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

            myCurentObjective = currentUnitsAvailableToAttack[0];
            myCurrentObjectiveTile = myCurentObjective.myCurrentTile;

            myCurrentEnemyState = enemyState.Attacking;
        }
    }

  

    public override void Attack()
    {
        //Si no he sido alertado, activo mi estado de alerta.
        if (!haveIBeenAlerted)
        {
            AlertEnemy();
        }

        for (int i = 0; i < myCurrentTile.neighbours.Count; i++)
        {
            //Si mi objetivo es adyacente a mi le ataco

            if (myCurrentTile.neighbours[i].unitOnTile != null && myCurrentTile.neighbours[i].unitOnTile == currentUnitsAvailableToAttack[0])
            {
                //Las comprobaciones para atacar arriba y abajo son iguales. Salvo por la dirección en la que tiene que girar el gigante
                if (myCurrentObjectiveTile.tileX == myCurrentTile.tileX)
                {
                    //Arriba
                    if (myCurrentObjectiveTile.tileZ > myCurrentTile.tileZ)
                    {
                        RotateLogic(FacingDirection.North);
                    }
                    //Abajo
                    else
                    {
                        RotateLogic(FacingDirection.South);
                    }

                    //Atacar al enemigo
                    DoDamage(currentUnitsAvailableToAttack[0]);

                    //Comprobar si a sus lados hay unidades
                    if (myCurrentObjectiveTile.tilesInLineRight.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile != null)
                    {
                        DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile);
                    }

                    if (myCurrentObjectiveTile.tilesInLineLeft.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile != null)
                    {
                        DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile);
                    }
                }
                //Izquierda o derecha
                else
                {
                    //Arriba
                    if (myCurrentObjectiveTile.tileX > myCurrentTile.tileX)
                    {
                        RotateLogic(FacingDirection.East);
                    }
                    //Abajo
                    else
                    {
                        RotateLogic(FacingDirection.West);
                    }

                    //Atacar al enemigo
                    DoDamage(currentUnitsAvailableToAttack[0]);

                    //Comprobar si a sus lados hay unidades
                    if (myCurrentObjectiveTile.tilesInLineUp.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile != null)
                    {
                        DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile);
                    }

                    if (myCurrentObjectiveTile.tilesInLineDown.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile != null)
                    {
                        DoDamage(currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile);
                    }
                }

                hasAttacked = true;
                myAnimator.SetTrigger("Attack");
                //Me pongo en waiting porque al salir del for va a entrar en la corrutina abajo
                //myCurrentEnemyState = enemyState.Waiting;
                break;
            }
        }

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

    //IEnumerator AttackWait()
    //{
    //    yield return new WaitForSeconds(timeWaitAfterAttack);
    //    myCurrentEnemyState = enemyState.Ended;
    //}

    public override void MoveUnit()
    {
        //ShowActionPathFinding(true);
        movementParticle.SetActive(true);

        //Arriba o abajo
        if (myCurrentObjectiveTile.tileX == myCurrentTile.tileX)
        {
            //Arriba
            if (myCurrentObjectiveTile.tileZ > myCurrentTile.tileZ)
            {
                if (!myCurrentTile.tilesInLineUp[0].isEmpty && !myCurrentTile.tilesInLineUp[0].isObstacle && myCurrentTile.tilesInLineUp[0].unitOnTile == null)
                {
                    currentTileVectorToMove = myCurrentTile.tilesInLineUp[0].transform.position; //new Vector3(myCurrentTile.tilesInLineUp[0].tileX, myCurrentTile.tilesInLineUp[0].height, myCurrentTile.tilesInLineUp[0].tileZ);
                    MovementLogic(myCurrentTile.tilesInLineUp);
                    RotateLogic(FacingDirection.North);
                }
                else
                {
                    hasMoved = true;
                    myCurrentEnemyState = enemyState.Ended;
                }
            }
            //Abajo
            else
            {
                if (!myCurrentTile.tilesInLineDown[0].isEmpty && !myCurrentTile.tilesInLineDown[0].isObstacle && myCurrentTile.tilesInLineDown[0].unitOnTile == null)
                {
                    currentTileVectorToMove = myCurrentTile.tilesInLineDown[0].transform.position; // new Vector3(myCurrentTile.tilesInLineDown[0].tileX, myCurrentTile.tilesInLineDown[0].height, myCurrentTile.tilesInLineDown[0].tileZ);
                    MovementLogic(myCurrentTile.tilesInLineDown);
                    RotateLogic(FacingDirection.South);
                }

                else
                {
                    hasMoved = true;
                    myCurrentEnemyState = enemyState.Ended;
                }
            }
        }
        //Izquierda o derecha
        else if (myCurrentObjectiveTile.tileZ == myCurrentTile.tileZ)
        {
            //Derecha
            if (myCurrentObjectiveTile.tileX > myCurrentTile.tileX)
            {
                if (!myCurrentTile.tilesInLineRight[0].isEmpty && !myCurrentTile.tilesInLineRight[0].isObstacle && myCurrentTile.tilesInLineRight[0].unitOnTile == null)
                {
                    currentTileVectorToMove = myCurrentTile.tilesInLineRight[0].transform.position;  // new Vector3(myCurrentTile.tilesInLineRight[0].tileX, myCurrentTile.tilesInLineRight[0].height, myCurrentTile.tilesInLineRight[0].tileZ);
                    MovementLogic(myCurrentTile.tilesInLineRight);
                    RotateLogic(FacingDirection.East);
                }

                else
                {
                    hasMoved = true;
                    myCurrentEnemyState = enemyState.Ended;
                }
            }
            //Izquierda
            else
            {
                if (!myCurrentTile.tilesInLineLeft[0].isEmpty && !myCurrentTile.tilesInLineLeft[0].isObstacle && myCurrentTile.tilesInLineLeft[0].unitOnTile == null)
                {
                    currentTileVectorToMove = myCurrentTile.tilesInLineLeft[0].transform.position;   //new Vector3(myCurrentTile.tilesInLineLeft[0].tileX, myCurrentTile.tilesInLineLeft[0].height, myCurrentTile.tilesInLineLeft[0].tileZ);
                    MovementLogic(myCurrentTile.tilesInLineLeft);
                    RotateLogic(FacingDirection.West);
                }

                else
                {
                    hasMoved = true;
                    myCurrentEnemyState = enemyState.Ended;
                }
            }
        }

        //Diagonales
        else
        {
            //Diag derecha
            if (myCurrentObjectiveTile.tileX > myCurrentTile.tileX)
            {
               //Diag Arriba Derecha
               if(myCurrentObjectiveTile.tileZ > myCurrentTile.tileZ)
               {
                    //Si el tile de arriba esta libre me muevo a él
                    if (!myCurrentTile.tilesInLineUp[0].isEmpty && !myCurrentTile.tilesInLineUp[0].isObstacle && myCurrentTile.tilesInLineUp[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineUp[0].transform.position;  //new Vector3(myCurrentTile.tilesInLineUp[0].tileX, myCurrentTile.tilesInLineUp[0].height, myCurrentTile.tilesInLineUp[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineUp);
                        RotateLogic(FacingDirection.North);
                    }

                    //Si no compruebo el de la derecha para intentar moverme a él.
                    else if (!myCurrentTile.tilesInLineRight[0].isEmpty && !myCurrentTile.tilesInLineRight[0].isObstacle && myCurrentTile.tilesInLineRight[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineRight[0].transform.position;  //new Vector3(myCurrentTile.tilesInLineRight[0].tileX, myCurrentTile.tilesInLineRight[0].height, myCurrentTile.tilesInLineRight[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineRight);
                        RotateLogic(FacingDirection.East);
                    }

                    else
                    {
                        myCurrentEnemyState = enemyState.Ended;
                    }
                }

               //Diag Abajo Derecha
               else
               {
                    //Si el tile de abajo esta libre me muevo a él
                    if (!myCurrentTile.tilesInLineDown[0].isEmpty && !myCurrentTile.tilesInLineDown[0].isObstacle && myCurrentTile.tilesInLineDown[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineDown[0].transform.position; // new Vector3(myCurrentTile.tilesInLineDown[0].tileX, myCurrentTile.tilesInLineDown[0].height, myCurrentTile.tilesInLineDown[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineDown);
                        RotateLogic(FacingDirection.South);
                    }

                    //Si el tile de arriba esta libre me muevo a él
                    else if (!myCurrentTile.tilesInLineRight[0].isEmpty && !myCurrentTile.tilesInLineRight[0].isObstacle && myCurrentTile.tilesInLineRight[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineRight[0].transform.position; // new Vector3(myCurrentTile.tilesInLineRight[0].tileX, myCurrentTile.tilesInLineRight[0].height, myCurrentTile.tilesInLineRight[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineRight);
                        RotateLogic(FacingDirection.East);
                    }

                    else
                    {
                        myCurrentEnemyState = enemyState.Ended;
                    }
                }
            }
            else
            {
                //Diag Arriba Izquierda
                if (myCurrentObjectiveTile.tileZ > myCurrentTile.tileZ)
                {
                    //Si el tile de arriba esta libre me muevo a él
                    if (!myCurrentTile.tilesInLineUp[0].isEmpty && !myCurrentTile.tilesInLineUp[0].isObstacle && myCurrentTile.tilesInLineUp[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineUp[0].transform.position; // new Vector3(myCurrentTile.tilesInLineUp[0].tileX, myCurrentTile.tilesInLineUp[0].height, myCurrentTile.tilesInLineUp[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineUp);
                        RotateLogic(FacingDirection.North);
                    }

                    //Si el tile de arriba esta libre me muevo a él
                    else if (!myCurrentTile.tilesInLineLeft[0].isEmpty && !myCurrentTile.tilesInLineLeft[0].isObstacle && myCurrentTile.tilesInLineLeft[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineLeft[0].transform.position; // new Vector3(myCurrentTile.tilesInLineLeft[0].tileX, myCurrentTile.tilesInLineLeft[0].height, myCurrentTile.tilesInLineLeft[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineLeft);
                        RotateLogic(FacingDirection.West);
                    }

                    else
                    {
                        myCurrentEnemyState = enemyState.Ended;
                    }
                }

                //Diag Abajo Izquierda
                else
                {
                    //Si el tile de abajo esta libre me muevo a él
                    if (!myCurrentTile.tilesInLineDown[0].isEmpty && !myCurrentTile.tilesInLineDown[0].isObstacle && myCurrentTile.tilesInLineDown[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineDown[0].transform.position; // new Vector3(myCurrentTile.tilesInLineDown[0].tileX, myCurrentTile.tilesInLineDown[0].height, myCurrentTile.tilesInLineDown[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineDown);
                        RotateLogic(FacingDirection.South);
                    }

                    //Si el tile de arriba esta libre me muevo a él
                    else if (!myCurrentTile.tilesInLineLeft[0].isEmpty && !myCurrentTile.tilesInLineLeft[0].isObstacle && myCurrentTile.tilesInLineLeft[0].unitOnTile == null)
                    {
                        currentTileVectorToMove = myCurrentTile.tilesInLineLeft[0].transform.position; // new Vector3(myCurrentTile.tilesInLineLeft[0].tileX, myCurrentTile.tilesInLineLeft[0].height, myCurrentTile.tilesInLineLeft[0].tileZ);
                        MovementLogic(myCurrentTile.tilesInLineLeft);
                        RotateLogic(FacingDirection.West);
                    }

                    else
                    {
                        myCurrentEnemyState = enemyState.Ended;
                    }
                }
            }
        }


        //Comprueba la dirección en la que se encuentra el objetivo.
        //Si se encuentra justo en el mismo eje (movimiento torre), el gigante avanza en esa dirección.
        //Si se encuentra un bloqueo se queda en el sitio intentando avanzar contra el bloqueo.

        //Sin embargo si el objetivo se encuentra en diágonal (por ejemplo arriba a la derecha)
        //El gigante tiene que decidir una de las dos (DISEÑO REGLAS DE PATHFINDING)
        //Una vez decidida avanza en esta dirección hasta que no pueda más y si sigue estando en diagonal, avanza en la que había descartado antes.

        //Buscar de nuevo si puedo pegarle

        movementParticle.SetActive(false);
        myCurrentEnemyState = enemyState.Searching;

        //Espero después de moverme para que no vaya demasiado rápido
        //myCurrentEnemyState = enemyState.Waiting;
        //StartCoroutine("MovementWait");

    }

    //Lógica actual del movimiento. Básicamente es el encargado de mover al modelo y setear las cosas
    private void MovementLogic(List<IndividualTiles> ListWithNewTile)
    {
        //Muevo al gigante
        transform.DOMove(currentTileVectorToMove, timeMovementAnimation);

        ShowActionPathFinding(false);

        StartCoroutine("MovementWait");

        //Actualizo las variables de los tiles
        UpdateInformationAfterMovement(ListWithNewTile[0]);

        //Aviso de que se ha movido
        hasMoved = true;
    }

    IEnumerator MovementWait()
    {
        yield return new WaitForSeconds(timeWaitAfterMovement);
        HideActionPathfinding();
        //ShowActionPathFinding(false);
    }

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

   
    //Función que se encarga de hacer que el personaje este despierto/alerta

    //HACER LO MISMO QUE EN GOBLIN Y QUITAR QUE RECALCULE CUANDO ES EL TURNO ENEMIGO. MIRAR BIEN QUE HACE EXACTAMENTE CUANDO HAY QUE DESPINTAR PARA PONER EN FUNCION HIDEACTIONHOVER.
    public override void ShowActionPathFinding(bool _shouldShowAction)
    {
        SearchingObjectivesToAttackShowActionPathFinding();    

        if (currentUnitsAvailableToAttack.Count > 0)
        {
            //Cada enemigo realiza su propio path
            LM.TM.CalculatePathForMovementCost(myCurrentObjectiveTile.tileX, myCurrentObjectiveTile.tileZ);
            //Añadir variable para guardar el path

            if (_shouldShowAction)
            {
                myLineRenderer.enabled = true;

              
                if (LM.TM.currentPath.Count <=3)
                {
                   
                    if (myCurrentObjectiveTile.tileX == myCurrentTile.tileX)
                    {
                        myCurrentObjectiveTile.ColorAttack();

                        if (myCurrentObjectiveTile.tilesInLineRight.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineRight[0].ColorAttack();
                        }


                        if (myCurrentObjectiveTile.tilesInLineLeft.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile != null)
                        {
                        myCurrentObjectiveTile.tilesInLineLeft[0].ColorAttack();
                        }
                    }
                    //Izquierda o derecha
                    else
                    {
                        myCurrentObjectiveTile.ColorAttack();

                        //Comprobar si a sus lados hay unidades
                        if (myCurrentObjectiveTile.tilesInLineUp.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineUp[0].ColorAttack();
                        }

                        if (myCurrentObjectiveTile.tilesInLineDown.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineDown[0].ColorAttack();
                        }
                    }                
                }
               




                if (LM.currentLevelState == LevelManager.LevelState.ProcessingPlayerActions)
                {
                    shaderHover.SetActive(true);
                }
            }
            else
            {
                myLineRenderer.enabled = false;
                shaderHover.SetActive(false);


               myCurrentObjectiveTile.ColorDesAttack();

                if ( myCurrentObjectiveTile != null)
                {
                    if (myCurrentObjectiveTile.tileX == myCurrentTile.tileX)
                    {
                        if (myCurrentObjectiveTile.tilesInLineRight.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineRight[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineRight[0].ColorDesAttack();
                        }


                        if (myCurrentObjectiveTile.tilesInLineLeft.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineLeft[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineLeft[0].ColorDesAttack();
                        }
                        
                        
                    }
                    else
                    {
                        if (myCurrentObjectiveTile.tilesInLineUp.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineUp[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineUp[0].ColorDesAttack();
                        }

                        if (myCurrentObjectiveTile.tilesInLineDown.Count > 0 && currentUnitsAvailableToAttack[0].myCurrentTile.tilesInLineDown[0].unitOnTile != null)
                        {
                            myCurrentObjectiveTile.tilesInLineDown[0].ColorDesAttack();
                        }

                    }

                }
            }
            
            myLineRenderer.positionCount = 2;


            if (LM.TM.currentPath.Count > 2)
            {
                Vector3 iniPosition = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);

                myLineRenderer.SetPosition(0, iniPosition);

                Vector3 pointPosition = new Vector3(LM.TM.currentPath[1].transform.position.x, LM.TM.currentPath[1].transform.position.y + 0.5f, LM.TM.currentPath[1].transform.position.z);
                myLineRenderer.SetPosition(1, pointPosition);

                Vector3 spawnPoint = new Vector3(LM.TM.currentPath[1].transform.position.x, LM.TM.currentPath[1].transform.position.y + 0.25f, LM.TM.currentPath[1].transform.position.z);
                shaderHover.transform.position = spawnPoint;
                Vector3 unitDirection = new Vector3(LM.TM.currentPath[2].transform.position.x, LM.TM.currentPath[1].transform.position.y + 0.25f, LM.TM.currentPath[2].transform.position.z);

                shaderHover.transform.DORotate(unitDirection, 0f);
            }

            else
            {
                myLineRenderer.enabled = false;
                shaderHover.SetActive(false);
            }
        }
       
    }

    bool keepSearching;

    //Esta función sirve para que busque los objetivos a atacar pero sin que haga cambios en el turn state del enemigo
    public override void SearchingObjectivesToAttackShowActionPathFinding()
    {
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
                    Debug.Log(unitsInRange[i]);
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

            //Determinamos el enemigo más cercano.
            currentUnitsAvailableToAttack = LM.CheckEnemyPathfinding(GetComponent<EnemyUnit>());

            if (currentUnitsAvailableToAttack.Count == 0)
            {

            }

            else if (currentUnitsAvailableToAttack.Count == 1)
            {
                myCurentObjective = currentUnitsAvailableToAttack[0];
                myCurrentObjectiveTile = myCurentObjective.myCurrentTile;
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

                myCurentObjective = currentUnitsAvailableToAttack[0];
                myCurrentObjectiveTile = myCurentObjective.myCurrentTile;

            }
        }
    }
}
