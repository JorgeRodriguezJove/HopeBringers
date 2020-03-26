﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Valkyrie : PlayerUnit
{
    public IndividualTiles previousTile;

    [Header("MEJORAS DE PERSONAJE")]

    [Header("Activas")]
    //ACTIVAS

    //bool para la mejora de la activa 1
    public bool canChooseEnemy;

    //bool para la activa 2
    public bool armorMode;

    //bool para la mejora de la activa 2
    public bool armorMode2;

    //int que indica cuanto añade de armadura
    public int numberOfArmorAdded;

    [Header("Pasivas")]
    //PASIVAS

    //bool para la pasiva 1
    public bool changePositions;

    //bool para la mejora de la pasiva 1
    public bool changePositions2;

    //El número de vida la unidad al que deja la valquiria cambiarse por dicha unidad
    public int numberCanChange;



    public override void Attack(UnitBase unitToAttack)
    {
        hasAttacked = true;


        if (unitToAttack.isMarked)
        {
            unitToAttack.isMarked = false;
            unitToAttack.monkMark.SetActive(false);
            currentHealth += FindObjectOfType<Monk>().healerBonus * unitToAttack.numberOfMarks;
            unitToAttack.numberOfMarks = 0;

            if (FindObjectOfType<Monk>().debuffMark2)
            {
                if (!unitToAttack.isStunned)
                {
                    StunUnit(unitToAttack, 1);
                }

            }
            else if (FindObjectOfType<Monk>().healerMark2)
            {
                ApplyBuffOrDebuffdamage(this, 1, 3);
              

            }
            UIM.RefreshTokens();

        }

        if (canChooseEnemy)
        {

            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Quito el color del tile
            myCurrentTile.ColorDeselect();

            for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
            {
                if (currentUnitsAvailableToAttack[i] == unitToAttack)
                {
                    break;
                }
                else if (currentUnitsAvailableToAttack[i] != null)
                {
                    DoDamage(currentUnitsAvailableToAttack[i]);
                }

            }


            //Hago daño antes de que se produzca el intercambio
            DoDamage(unitToAttack);

            //Intercambio
            previousTile = unitToAttack.myCurrentTile;

            currentTileVectorToMove = unitToAttack.myCurrentTile.transform.position;
            transform.DOMove(currentTileVectorToMove, 1);

            currentTileVectorToMove = myCurrentTile.transform.position;
            unitToAttack.transform.DOMove(currentTileVectorToMove, 1);

            unitToAttack.UpdateInformationAfterMovement(myCurrentTile);
            UpdateInformationAfterMovement(previousTile);
            unitToAttack.UpdateInformationAfterMovement(unitToAttack.myCurrentTile);

            //Hay que cambiarlo
            SoundManager.Instance.PlaySound(AppSounds.ROGUE_ATTACK);
            //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
            base.Attack(unitToAttack);
        }

        else if (armorMode)
        {
            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Quito el color del tile
            myCurrentTile.ColorDeselect();

            //TENEMOS QUE HABLAR SI LA ARMADURA DEPENDE DE LA currentHealth QUE TIENE O DE LA MAXHEALTH
            if (unitToAttack.GetComponent<PlayerUnit>())
            {

                if (armorMode2)
                {
                    currentArmor += numberOfArmorAdded;
                    if (currentArmor > currentHealth)
                    {
                        currentArmor = currentHealth;
                    }
                }

                unitToAttack.currentArmor += numberOfArmorAdded;
                if (unitToAttack.currentArmor > unitToAttack.currentHealth)
                {
                    unitToAttack.currentArmor = unitToAttack.currentHealth;
                }
            }
            else
            {
                //Hago daño
                DoDamage(unitToAttack);

            }

            //Intercambio
            previousTile = unitToAttack.myCurrentTile;

            currentTileVectorToMove = unitToAttack.myCurrentTile.transform.position;
            transform.DOMove(currentTileVectorToMove, 1);


            currentTileVectorToMove = myCurrentTile.transform.position;
            unitToAttack.transform.DOMove(currentTileVectorToMove, 1);

            unitToAttack.UpdateInformationAfterMovement(myCurrentTile);
            UpdateInformationAfterMovement(previousTile);
            unitToAttack.UpdateInformationAfterMovement(unitToAttack.myCurrentTile);

          
            //Hay que cambiarlo
            SoundManager.Instance.PlaySound(AppSounds.ROGUE_ATTACK);
            //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
            base.Attack(unitToAttack);


        }
        else
        {

            //Animación de ataque
            myAnimator.SetTrigger("Attack");

            //Quito el color del tile
            myCurrentTile.ColorDeselect();

            if (unitToAttack.GetComponent<PlayerUnit>())
            {


            }
            else
            {
                //Hago daño
                DoDamage(unitToAttack);

            }

            //Intercambio
            previousTile = unitToAttack.myCurrentTile;

            currentTileVectorToMove = unitToAttack.myCurrentTile.transform.position;
            transform.DOMove(currentTileVectorToMove, 1);

            currentTileVectorToMove = myCurrentTile.transform.position;
            unitToAttack.transform.DOMove(currentTileVectorToMove, 1);

            unitToAttack.UpdateInformationAfterMovement(myCurrentTile);
            UpdateInformationAfterMovement(previousTile);
            unitToAttack.UpdateInformationAfterMovement(unitToAttack.myCurrentTile);

           
            //Hay que cambiarlo
            SoundManager.Instance.PlaySound(AppSounds.ROGUE_ATTACK);
            //La base tiene que ir al final para que el bool de hasAttacked se active después del efecto.
            base.Attack(unitToAttack);
        }
    }

    protected override void DoDamage(UnitBase unitToDealDamage)
    {

        //Añado este if para el count de honor del samurai
        if (currentFacingDirection == FacingDirection.North && unitToDealDamage.currentFacingDirection == FacingDirection.South
       || currentFacingDirection == FacingDirection.South && unitToDealDamage.currentFacingDirection == FacingDirection.North
       || currentFacingDirection == FacingDirection.East && unitToDealDamage.currentFacingDirection == FacingDirection.West
       || currentFacingDirection == FacingDirection.West && unitToDealDamage.currentFacingDirection == FacingDirection.East
       )
        {
            LM.honorCount++;
        }
        base.DoDamage(unitToDealDamage);
    }

    public override void CheckUnitsAndTilesInRangeToAttack()
    {
        currentUnitsAvailableToAttack.Clear();
        currentTilesInRangeForAttack.Clear();
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

                //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                if (Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                    || Mathf.Abs(myCurrentTile.tilesInLineUp[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                {
                    //Si no hay obstáculo marco el tile para indicar el rango
                    if (!myCurrentTile.tilesInLineUp[i].isEmpty && !myCurrentTile.tilesInLineUp[i].isObstacle)
                    {
                        currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineUp[i]);
                    }

                    else
                    {
                        break;
                    }

                    //Si hay una unidad la guardo en posibles objetivos
                    if (myCurrentTile.tilesInLineUp[i].unitOnTile != null)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineUp[i].unitOnTile);
                        if (!canChooseEnemy)
                        {
                            break;
                        }

                    }

                    if (myCurrentTile.tilesInLineUp[i].isEmpty)
                    {
                        break;
                    }
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

                //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                if (Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                    || Mathf.Abs(myCurrentTile.tilesInLineDown[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                {
                    if (!myCurrentTile.tilesInLineDown[i].isEmpty && !myCurrentTile.tilesInLineDown[i].isObstacle)
                    {
                        currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineDown[i]);
                    }

                    else
                    {
                        break;
                    }

                    //Si hay una unidad la guardo en posibles objetivos
                    if (myCurrentTile.tilesInLineDown[i].unitOnTile != null)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineDown[i].unitOnTile);

                        if (!canChooseEnemy)
                        {
                            break;
                        }
                    }

                    if (myCurrentTile.tilesInLineDown[i].isEmpty)
                    {
                        break;
                    }
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

                //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                if (Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                    || Mathf.Abs(myCurrentTile.tilesInLineRight[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                {
                    if (!myCurrentTile.tilesInLineRight[i].isEmpty && !myCurrentTile.tilesInLineRight[i].isObstacle)
                    {
                        currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineRight[i]);
                    }
                    else
                    {
                        break;
                    }

                    //Si hay una unidad la guardo en posibles objetivos
                    if (myCurrentTile.tilesInLineRight[i].unitOnTile != null)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineRight[i].unitOnTile);

                        if (!canChooseEnemy)
                        {
                            break;
                        }
                    }

                    if (myCurrentTile.tilesInLineRight[i].isEmpty)
                    {
                        break;
                    }
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

                //Compruebo que la diferencia de altura con mi tile y con el tile anterior es correcto.
                if (Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - myCurrentTile.height) <= maxHeightDifferenceToAttack
                    || Mathf.Abs(myCurrentTile.tilesInLineLeft[i].height - previousTileHeight) <= maxHeightDifferenceToAttack)
                {
                    if (!myCurrentTile.tilesInLineLeft[i].isEmpty && !myCurrentTile.tilesInLineLeft[i].isObstacle)
                    {
                        currentTilesInRangeForAttack.Add(myCurrentTile.tilesInLineLeft[i]);
                    }

                    else
                    {
                        break;
                    }

                    //Si hay una unidad la guardo en posibles objetivos
                    if (myCurrentTile.tilesInLineLeft[i].unitOnTile != null)
                    {
                        //Almaceno la primera unidad en la lista de posibles unidades
                        currentUnitsAvailableToAttack.Add(myCurrentTile.tilesInLineLeft[i].unitOnTile);

                        if (!canChooseEnemy)
                        {
                            break;
                        }
                    }

                    if (myCurrentTile.tilesInLineLeft[i].isEmpty)
                    {
                        break;
                    }
                }
            }

        }

        //Marco las unidades disponibles para atacar de color rojo
        for (int i = 0; i < currentUnitsAvailableToAttack.Count; i++)
        {
            CalculateDamage(currentUnitsAvailableToAttack[i]);
            currentUnitsAvailableToAttack[i].ColorAvailableToBeAttacked(damageWithMultipliersApplied);
            currentUnitsAvailableToAttack[i].HealthBarOn_Off(true);
            currentUnitsAvailableToAttack[i].myCurrentTile.ColorInteriorRed();

        }

        for (int i = 0; i < currentTilesInRangeForAttack.Count; i++)
        {
            currentTilesInRangeForAttack[i].ColorBorderRed();
        }


    }

    public void ChangePosition(PlayerUnit unitToMove)
    {
        IndividualTiles valkyriePreviousTile = unitToMove.myCurrentTile;
        unitToMove.MoveToTilePushed(myCurrentTile);
        unitToMove.UpdateInformationAfterMovement(myCurrentTile);
        this.MoveToTilePushed(valkyriePreviousTile);
        UpdateInformationAfterMovement(valkyriePreviousTile);
        hasMoved = true;
        LM.UnitHasFinishedMovementAndRotation();
    }
}
