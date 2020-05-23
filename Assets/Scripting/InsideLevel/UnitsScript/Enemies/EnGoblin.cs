﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnGoblin : EnemyUnit
{
    [SerializeField]
    GameObject tier2AttackHorn;

    public override void SearchingObjectivesToAttack()
    {
        //Comportamiento normal
        if (!amIBeingPossesed)
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


                    myCurrentEnemyState = enemyState.Attacking;
                }
            }
        }

        //Comportamiento del dark lord
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

                    CallWaitCoroutine();
                    return;
                }

                //Como no puedo hacer traspaso, compruebo que ataques puedo hacer
                else
                {
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
    }

    public override void Attack()
    {
        Debug.Log("Llamada a función de ataque");
        //Si es Tier 2 Alerta a los enemigos en el área
        if (myTierLevel == TierLevel.Level2)
        {
            if (!haveIBeenAlerted)
            {
                //Le pido al TileManager los enemigos dentro de mi rango
                unitsInRange = LM.TM.GetAllUnitsInRangeWithoutPathfinding(rangeOfAction, GetComponent<UnitBase>());

                //Alerto a los enemigos a mi alcance
                for (int i = 0; i < unitsInRange.Count; i++)
                {
                    if (unitsInRange[i].GetComponent<EnemyUnit>())
                    {
                        unitsInRange[i].GetComponent<EnemyUnit>().AlertEnemy();
                    }
                }

                Instantiate(tier2AttackHorn, this.transform.position, tier2AttackHorn.transform.rotation);
            }

            hasAttacked = true;

            myCurrentEnemyState = enemyState.Ended;
        }
        
        //Si no he sido alertado, activo mi estado de alerta.
        //Al alertarme salo del void de ataque para hacer la busqueda normal de jugadores.
        if (!haveIBeenAlerted)
        {
            AlertEnemy();
            myCurrentEnemyState = enemyState.Searching;
            return;
        }

        for (int i = 0; i < myCurrentTile.neighbours.Count; i++)
        {
            //Si mi objetivo es adyacente a mi le ataco
            if (myCurrentTile.neighbours[i].unitOnTile != null && 
                currentUnitsAvailableToAttack.Count > 0 && 
                myCurrentTile.neighbours[i].unitOnTile == currentUnitsAvailableToAttack[0] && 
                Mathf.Abs(myCurrentTile.height - myCurrentTile.neighbours[i].height) <= maxHeightDifferenceToAttack)
            {
                //Las comprobaciones para atacar arriba y abajo son iguales. Salvo por la dirección en la que tiene que girar el goblin
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

                    ColorAttackTile();

                    //Atacar al enemigo
                    DoDamage(currentUnitsAvailableToAttack[0]);
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

                    ColorAttackTile();

                    //Atacar al enemigo
                    DoDamage(currentUnitsAvailableToAttack[0]);
                }

                base.Attack();

                //Animación de ataque
                hasAttacked = true;
                ExecuteAnimationAttack();
                //Se tiene que poner en wait hasta que acabe la animación de ataque
                myCurrentEnemyState = enemyState.Waiting;

                //Me pongo en waiting porque al salir del for va a entrar en la corrutina abajo.
                //myCurrentEnemyState = enemyState.Waiting;
                break;
            }
        }

        if (!hasMoved && !hasAttacked)
        {
            myCurrentEnemyState = enemyState.Moving;
        }

        //Si llega hasta aqui significa que ya se ha movido y no puede atacar
        if (hasMoved && !hasAttacked)
        {
            myCurrentEnemyState = enemyState.Ended;
        }
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

    public override void SearchingObjectivesToAttackShowActionPathFinding()
    {
        //Solo si no esta siendo poseido
        if (!amIBeingPossesed)
        {
            base.SearchingObjectivesToAttackShowActionPathFinding();
        }  
    }

    public override void FinishMyActions()
    {
        base.FinishMyActions();

        if (amIBeingPossesed)
        {
            attackCountThisTurn = 0;
            coneUsed = false;
            normalAttackUsed = false;
            areaUsed = false;
        }
    }

    //PARA MOVEUNIT SE USA LA BASE DEL ENEMIGO (Que es la lógica del goblin).
    //PASA LO MISMO CON ShowActionPathFinding(bool _shouldRecalculate) QUE MUESTRA LA ACCIÓN DEL ENEMIGO;
    //PASA LO MISMO CON ColorAttackTile();
    //PASA LO MISMO CON SearchingObjectivesToAttackShowActionPathFinding
}
